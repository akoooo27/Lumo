using System.ComponentModel.DataAnnotations;

using Amazon.S3;

using Auth.Application.Abstractions.Authentication;
using Auth.Application.Abstractions.Data;
using Auth.Application.Abstractions.Generators;
using Auth.Application.Abstractions.Storage;
using Auth.Infrastructure.Authentication;
using Auth.Infrastructure.Data;
using Auth.Infrastructure.Generators;
using Auth.Infrastructure.Jobs;
using Auth.Infrastructure.Options;
using Auth.Infrastructure.Storage;

using MassTransit;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

using SharedKernel.Application.Data;
using SharedKernel.Application.Messaging;
using SharedKernel.Infrastructure;
using SharedKernel.Infrastructure.Messaging;
using SharedKernel.Infrastructure.Options;

using TickerQ.DependencyInjection;

namespace Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection
        AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services
            .AddSharedKernelInfrastructure(configuration)
            .AddServices()
            .AddDatabase(configuration, environment)
            .AddAuthenticationInternal()
            .AddAuthorization()
            .AddStorage(configuration)
            .AddMessaging(configuration)
            .AddBackgroundJobs();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IIdGenerator, IdGenerator>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        DatabaseOptions databaseOptions = new();
        configuration.GetSection(DatabaseOptions.SectionName).Bind(databaseOptions);

        bool enableSensitiveLogging = databaseOptions.EnableSensitiveDataLogging && environment.IsDevelopment();

        services.AddDbContext<AuthDbContext>(options =>
        {
            options
                .UseNpgsql(databaseOptions.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging(enableSensitiveLogging);
        });

        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        services.AddHealthChecks()
            .AddNpgSql
            (
                connectionString: databaseOptions.ConnectionString,
                name: "auth-postgresql",
                tags: ["ready", "live"]
            );

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(this IServiceCollection services)
    {
        services.AddSingleton<JsonWebTokenHandler>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<IAttemptTracker, AttemptTracker>();

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
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RabbitMqOptions rabbitMqOptions = new();
        configuration.GetSection(RabbitMqOptions.SectionName).Bind(rabbitMqOptions);

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<AuthDbContext>(outbox =>
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

    private static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddScoped<ICronJobHelper, CronJobHelper>();

        services.AddTickerQ();

        return services;
    }
}