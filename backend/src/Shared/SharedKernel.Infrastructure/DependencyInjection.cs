using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using SharedKernel.Application.Authentication;
using SharedKernel.Infrastructure.Authentication;
using SharedKernel.Infrastructure.Caching;
using SharedKernel.Infrastructure.Observability;
using SharedKernel.Infrastructure.Options;
using SharedKernel.Infrastructure.Time;

namespace SharedKernel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedKernelInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddServices()
            .AddAuthenticationInternal(configuration)
            .AddOpenTelemetrySetup(configuration)
            .AddValkeySetup(configuration);

        return services;
    }
    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(this IServiceCollection services,
        IConfiguration configuration)

    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddSingleton<IJwtTokenValidator, JwtTokenValidator>();
        services.AddSingleton<ISecretHasher, SecretHasher>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        JwtOptions jwtOptions = new();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwtOptions);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new()
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        string? accessToken = context.Request.Query["access_token"];

                        if (!string.IsNullOrEmpty(accessToken)
#pragma warning disable CA1307
                            && context.HttpContext.Request.Path.StartsWithSegments("/v1/hubs"))
#pragma warning restore CA1307
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddSharedHealthChecks(this IServiceCollection services,
        IConfiguration configuration)

    {
        ArgumentNullException.ThrowIfNull(configuration);

        ValkeyOptions valkeyOptions = new ValkeyOptions();
        configuration.GetSection(ValkeyOptions.SectionName).Bind(valkeyOptions);

        SerilogOptions serilogOptions = new SerilogOptions();
        configuration.GetSection(SerilogOptions.SectionName).Bind(serilogOptions);

        OpenTelemetryOptions openTelemetryOptions = new OpenTelemetryOptions();
        configuration.GetSection(OpenTelemetryOptions.SectionName).Bind(openTelemetryOptions);

        string seqHealthUrl = serilogOptions.Seq.HealthCheckUrl ?? serilogOptions.Seq.ServerUrl + "/api";

        string jaegerHealthUrl = openTelemetryOptions.Exporter.HealthCheckUrl ?? openTelemetryOptions.Exporter.Endpoint;

        services.AddHealthChecks()
            .AddRedis
            (
                redisConnectionString: valkeyOptions.ConnectionString,
                name: "valkey",
                tags: ["ready"]
            )
            .AddUrlGroup
            (
                new Uri(seqHealthUrl),
                name: "seq",
                tags: ["ready"]
            )
            .AddUrlGroup
            (
                new Uri(jaegerHealthUrl),
                name: "jaeger",
                tags: ["ready"]
            );

        return services;
    }
}