using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace MicroserviceTemplate.Configurations.Setup;

public static class MicroserviceSetup
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static WebApplicationBuilder AddMicroserviceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureLogging(builder);
        ConfigureOpenTelemetry(builder);
        AddDefaultHealthChecks(builder);
        ConfigureHttpClientDefaults(builder);

        return builder;
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
    }

    private static void ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (useOtlpExporter)
                {
                    metrics.AddOtlpExporter();
                }
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath, StringComparison.Ordinal)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath, StringComparison.Ordinal)
                    )
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                if (useOtlpExporter)
                {
                    tracing.AddOtlpExporter();
                }
            });
    }

    private static void AddDefaultHealthChecks(WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("postgresdb");
        var redisConnectionString = builder.Configuration.GetConnectionString("cache");

        var healthChecks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecks.AddNpgSql(connectionString, name: "postgresql", tags: ["ready"]);
        }

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecks.AddRedis(redisConnectionString, name: "redis", tags: ["ready"]);
        }
    }

    private static void ConfigureHttpClientDefaults(WebApplicationBuilder builder)
    {
        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });
    }

    public static WebApplication UseMicroserviceDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
