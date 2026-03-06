using System.ComponentModel.DataAnnotations;

using Amazon.SimpleEmailV2;

using FastEndpoints;
using FastEndpoints.Swagger;

using MassTransit;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using Notifications.Api.Consumers;
using Notifications.Api.Consumers.Audit;
using Notifications.Api.Data;
using Notifications.Api.Options;
using Notifications.Api.Services;

using SharedKernel.Api;
using SharedKernel.Application.Data;
using SharedKernel.Application.Pipelines;
using SharedKernel.Infrastructure;
using SharedKernel.Infrastructure.Options;

using StackExchange.Redis;

namespace Notifications.Api;

internal static class DependencyInjection
{
    public static IServiceCollection
        AddNotificationsApi(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment) =>
        services
            .AddSharedKernelApi()
            .AddSharedKernelInfrastructure(configuration)
            .AddDatabase(configuration, environment)
            .AddMessaging(configuration)
            .AddSimpleEmailService(configuration)
            .AddNotificationsMediator()
            .AddRealTimeNotifications(configuration)
            .AddNotificationsEndpoints();

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

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options
                .UseNpgsql(databaseOptions.ConnectionString)
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging(enableSensitiveLogging);
        });

        services.AddScoped<INotificationDbContext>(sp => sp.GetRequiredService<NotificationDbContext>());

        services.AddHealthChecks()
            .AddNpgSql
            (
                connectionString: databaseOptions.ConnectionString,
                name: "notifications-postgresql",
                tags: ["ready"]
            );

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
            bus.AddConsumer<UserSignedUpConsumer>()
                .Endpoint(e => e.Name = "notifications-user-signed-up");

            bus.AddConsumer<UserDeletedConsumer>()
                .Endpoint(e => e.Name = "notifications-user-deleted");
            bus.AddConsumer<UserDisplayNameChangedConsumer>()
                .Endpoint(e => e.Name = "notifications-user-display-name-changed");
            bus.AddConsumer<UserEmailAddressChangedConsumer>()
                .Endpoint(e => e.Name = "notifications-user-email-address-changed");

            bus.AddConsumer<UserSignedUpAuditConsumer>()
                .Endpoint(e => e.Name = "notifications-user-signed-up-audit");

            bus.AddConsumer<LoginRequestedConsumerAudit>();
            bus.AddConsumer<UserEmailAddressChangedAuditConsumer>();
            bus.AddConsumer<RecoveryInitiatedAuditConsumer>();
            bus.AddConsumer<EmailChangeRequestedAuditConsumer>();
            bus.AddConsumer<UserDeletionRequestedAuditConsumer>();
            bus.AddConsumer<UserDeletionCanceledAuditConsumer>();
            bus.AddConsumer<UserDeletedAuditConsumer>()
                .Endpoint(e => e.Name = "notifications-user-deleted-audit");
            bus.AddConsumer<WorkflowRunNotificationRequestedConsumer>();

            bus.AddEntityFrameworkOutbox<NotificationDbContext>(outbox =>
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

        return services;
    }

    private static IServiceCollection AddSimpleEmailService(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDefaultAWSOptions(configuration.GetAWSOptions());

        services.AddAWSService<IAmazonSimpleEmailServiceV2>();

        services.AddScoped<IEmailService, SesEmailService>();

        return services;
    }

    private static IServiceCollection AddNotificationsMediator(this IServiceCollection services)
    {
        services.AddMediator((Mediator.MediatorOptions options) =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationPipeline<,>), typeof(LoggingPipeline<,>)];
        });

        return services;
    }

    private static IServiceCollection AddRealTimeNotifications(this IServiceCollection services,
        IConfiguration configuration)
    {
        ValkeyOptions valkeyOptions = new();
        configuration.GetSection(ValkeyOptions.SectionName).Bind(valkeyOptions);

        ISignalRServerBuilder signalRBuilder = services.AddSignalR();

        if (valkeyOptions.Enabled)
        {
            signalRBuilder.AddStackExchangeRedis(valkeyOptions.ConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("notifications");
            });
        }

        services.AddSingleton<INotificationRealtimePublisher, NotificationRealtimePublisher>();

        return services;
    }

    private static IServiceCollection AddNotificationsEndpoints(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        services.AddFastEndpoints(options =>
        {
            options.Assemblies =
            [
                typeof(DependencyInjection).Assembly
            ];
        });

        services.SwaggerDocument(o =>
        {
            o.MaxEndpointVersion = 1;
            o.DocumentSettings = s =>
            {
                s.Title = "Notifications API";
                s.Description = "Notifications service endpoints";
                s.Version = "v1";
            };
        });

        return services;
    }
}