using FastEndpoints;
using FastEndpoints.Swagger;

using Gateway.Api.Authentication;
using Gateway.Api.Caching;
using Gateway.Api.HttpClients;
using Gateway.Api.Options;
using Gateway.Api.RateLimiting;
using Gateway.Api.Transforms;

using SharedKernel.Api;
using SharedKernel.Infrastructure;

namespace Gateway.Api;

internal static class DependencyInjection
{
    internal static IServiceCollection AddGatewayApi(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSharedKernelApi()
            .AddSharedKernelInfrastructure(configuration)
            .AddSharedHealthChecks(configuration)
            .AddRateLimitingSetup(configuration);

        services.AddHttpContextAccessor();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                    .WithMethods("GET", "POST", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders("Content-Type", "Authorization")
                    .AllowCredentials();
            });
        });

        services.AddOptions<GatewayApiOptions>()
            .Bind(configuration.GetSection(GatewayApiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        GatewayApiOptions gatewayApiOptions = new();
        configuration.GetSection(GatewayApiOptions.SectionName).Bind(gatewayApiOptions);

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms<AuthTransformProvider>();

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
                s.Title = gatewayApiOptions.Title;
                s.Description = gatewayApiOptions.Description;
                s.Version = gatewayApiOptions.Version;
            };
        });

        services.AddScoped<ITokenCacheService, TokenCacheService>();

        services.AddHttpClient<IAuthServiceClient, AuthServiceClient>(client =>
        {
            client.BaseAddress = new Uri(gatewayApiOptions.AuthServiceBaseUrl);
        });

        services.AddScoped<ISessionTokenOrchestrator, SessionTokenOrchestrator>();

        return services;
    }
}