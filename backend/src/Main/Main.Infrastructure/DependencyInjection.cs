using System.ClientModel;
using System.ComponentModel.DataAnnotations;

using Amazon.S3;

using Main.Application.Abstractions.AI;
using Main.Application.Abstractions.Data;
using Main.Application.Abstractions.Ephemeral;
using Main.Application.Abstractions.Generators;
using Main.Application.Abstractions.Instructions;
using Main.Application.Abstractions.Memory;
using Main.Application.Abstractions.Services;
using Main.Application.Abstractions.SharedChats;
using Main.Application.Abstractions.Storage;
using Main.Application.Abstractions.Stream;
using Main.Application.Abstractions.Workflows;
using Main.Infrastructure.AI;
using Main.Infrastructure.AI.Filters;
using Main.Infrastructure.AI.Helpers;
using Main.Infrastructure.AI.Helpers.Interfaces;
using Main.Infrastructure.AI.Plugins;
using Main.Infrastructure.AI.Search;
using Main.Infrastructure.Consumers;
using Main.Infrastructure.Data;
using Main.Infrastructure.Ephemeral;
using Main.Infrastructure.Generators;
using Main.Infrastructure.Instructions;
using Main.Infrastructure.Jobs;
using Main.Infrastructure.Memory;
using Main.Infrastructure.Options;
using Main.Infrastructure.Preferences;
using Main.Infrastructure.SharedChats;
using Main.Infrastructure.Storage;
using Main.Infrastructure.Stream;
using Main.Infrastructure.Workflows;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

using OpenAI;
using OpenAI.Embeddings;

using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;
using SharedKernel.Infrastructure;
using SharedKernel.Infrastructure.Messaging;
using SharedKernel.Infrastructure.Options;

using TickerQ.DependencyInjection;

using StreamReader = Main.Infrastructure.Stream.StreamReader;

namespace Main.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection
        AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services
            .AddServices()
            .AddSharedKernelInfrastructure(configuration)
            .AddDatabase(configuration, environment)
            .AddAuthorization()
            .AddStorage(configuration)
            .AddMessaging(configuration)
            .AddAi(configuration)
            .AddBackgroundJobs();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IIdGenerator, IdGenerator>();

        services.AddScoped<IUserPreferenceResolver, UserPreferenceResolver>();
        services.AddScoped<ISharedChatReadStore, SharedChatReadStore>();
        services.AddScoped<IFavoriteModelsReadStore, FavoriteModelsReadStore>();

        services.AddSingleton<IWorkflowScheduleService, WorkflowScheduleService>();
        services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        DatabaseOptions databaseOptions = new();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);

        bool enableSensitiveLogging = databaseOptions.EnableSensitiveDataLogging && environment.IsDevelopment();

        services.AddDbContext<MainDbContext>(options =>
        {
            options
                .UseNpgsql(databaseOptions.ConnectionString, npgSqlOptions =>
                {
                    npgSqlOptions.UseVector();
                })
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging(enableSensitiveLogging);
        });

        services.AddScoped<IMainDbContext>(sp => sp.GetRequiredService<MainDbContext>());

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        services.AddHealthChecks()
            .AddNpgSql
            (
                connectionString: databaseOptions.ConnectionString,
                name: "main-postgresql",
                tags: ["ready", "live"]
            );

        return services;
    }

    private static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAWSService<IAmazonS3>();

        services.AddSingleton<IStorageService, StorageService>();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RabbitMqOptions rabbitMqOptions = new();
        configuration.GetSection(RabbitMqOptions.SectionName).Bind(rabbitMqOptions);

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<UserSignedUpConsumer>()
                .Endpoint(e => e.Name = "main-user-signed-up");
            bus.AddConsumer<UserDeletedConsumer>()
                .Endpoint(e => e.Name = "main-user-deleted");
            bus.AddConsumer<UserDisplayNameChangedConsumer>()
                .Endpoint(e => e.Name = "main-user-display-name-changed");
            bus.AddConsumer<UserEmailAddressChangedConsumer>()
                .Endpoint(e => e.Name = "main-user-email-address-changed");
            bus.AddConsumer<ChatStartedConsumer>();
            bus.AddConsumer<AssistantMessageGeneratedConsumer>();
            bus.AddConsumer<MessageSentConsumer>();
            bus.AddConsumer<EphemeralChatStartedConsumer>();
            bus.AddConsumer<AssistantEphemeralMessageGeneratedConsumer>();
            bus.AddConsumer<EphemeralMessageSentConsumer>();
            bus.AddConsumer<WorkflowRunRequestedConsumer>();

            bus.AddEntityFrameworkOutbox<MainDbContext>(outbox =>
            {
                outbox.UsePostgres();

                outbox.UseBusOutbox();

                outbox.QueryDelay = TimeSpan.FromSeconds(1);
            });

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.UseMessageRetry(retry =>
                {
                    retry.Ignore<ArgumentException>();
                    retry.Ignore<ValidationException>();
                    retry.Exponential
                    (
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromMinutes(1),
                        intervalDelta: TimeSpan.FromSeconds(2)
                    );
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IMessageBus, MessageBus>();

        return services;
    }

    private static IServiceCollection AddAi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpenRouterOptions>()
            .Bind(configuration.GetSection(OpenRouterOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        OpenRouterOptions openRouterOptions = new();
        configuration.GetSection(OpenRouterOptions.SectionName).Bind(openRouterOptions);

        services.AddOptions<TavilyOptions>()
            .Bind(configuration.GetSection(TavilyOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        TavilyOptions tavilyOptions = new();
        configuration.GetSection(TavilyOptions.SectionName).Bind(tavilyOptions);

        services.AddHttpClient<IWebSearchService, TavilySearchService>();

        services.AddSingleton<OpenAIClient>(_ =>
        {
            OpenAIClientOptions options = new()
            {
                Endpoint = new Uri(openRouterOptions.BaseUrl)
            };

            return new OpenAIClient
            (
                credential: new ApiKeyCredential(openRouterOptions.ApiKey),
                options: options
            );
        });

        services.AddSingleton<EmbeddingClient>(_ =>
        {
            OpenAIClientOptions options = new()
            {
                Endpoint = new Uri(openRouterOptions.BaseUrl)
            };

            OpenAIClient client = new(
                credential: new ApiKeyCredential(openRouterOptions.ApiKey),
                options: options
            );

            return client.GetEmbeddingClient(openRouterOptions.EmbeddingModel);
        });

        services.AddSingleton<IStreamPublisher, StreamPublisher>();
        services.AddSingleton<IChatLockService, ChatLockService>();

        services.AddScoped<IInstructionStore, InstructionStore>();
        services.AddScoped<IMemoryStore, MemoryStore>();
        services.AddScoped<IMemoryImportService, MemoryImportService>();

        services.AddScoped<PluginUserContext>();
        services.AddScoped<PluginStreamContext>();
        services.AddScoped<MemoryPlugin>();
        services.AddScoped<WebSearchPlugin>();
        services.AddScoped<ToolCallStreamFilter>();

        services.AddScoped<Kernel>(sp =>
        {
            MemoryPlugin memoryPlugin = sp.GetRequiredService<MemoryPlugin>();
            WebSearchPlugin webSearchPlugin = sp.GetRequiredService<WebSearchPlugin>();
            ToolCallStreamFilter toolCallStreamFilter = sp.GetRequiredService<ToolCallStreamFilter>();

            IKernelBuilder builder = Kernel.CreateBuilder();
            builder.Plugins.AddFromObject(memoryPlugin, "memory");
            builder.Plugins.AddFromObject(webSearchPlugin, "search");

            builder.Services.AddSingleton<IAutoFunctionInvocationFilter>(toolCallStreamFilter);

            return builder.Build();
        });

        services.AddScoped<ITitleGenerator, TitleGenerator>();
        services.AddScoped<IChatHistoryBuilder, ChatHistoryBuilder>();
        services.AddScoped<IStreamFinalizer, StreamFinalizer>();
        services.AddScoped<INativeChatCompletionService, NativeChatCompletionService>();

        services.AddSingleton<IStreamReader, StreamReader>();

        services.AddSingleton<IModelRegistry, ModelRegistry>();

        services.AddScoped<IEphemeralChatStore, EphemeralChatStore>();

        return services;
    }

    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddScoped<ICronJobHelper, CronJobHelper>();

        services.AddTickerQ();

        return services;
    }
}