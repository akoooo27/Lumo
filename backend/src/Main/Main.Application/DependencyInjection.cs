using FluentValidation;

using Main.Application.Abstractions.Services;

using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Application.Pipelines;

namespace Main.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) =>
        services
            .AddServices()
            .AddMessaging()
            .AddFluentValidation();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IChatAccessValidator, ChatAccessValidator>();
        services.AddScoped<IEphemeralChatAccessValidator, EphemeralChatAccessValidator>();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
            options.PipelineBehaviors = [typeof(ValidationPipeline<,>), typeof(LoggingPipeline<,>)];
        });

        return services;
    }

    private static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        return services;
    }
}