using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MicroserviceTemplate.Common;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MicroserviceTemplate.Configurations.Setup;

public static class MicroserviceSetup
{
    private const string OtlpEndpointConfigurationKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string TraceSamplingRatioConfigurationKey = "OpenTelemetry:Tracing:SamplingRatio";
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
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(CreateResourceBuilder(builder));
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;

            if (UseOtlpExporter(builder))
            {
                logging.AddOtlpExporter();
            }
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
        }

        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
    }

    private static void ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        var useOtlpExporter = UseOtlpExporter(builder);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, builder))
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(MicroserviceTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (useOtlpExporter)
                {
                    metrics.AddOtlpExporter();
                }
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(MicroserviceTelemetry.ActivitySourceName, builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath, StringComparison.Ordinal)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath, StringComparison.Ordinal)
                    )
                    .AddHttpClientInstrumentation()
                    .AddNpgsql();

                ConfigureSampling(tracing, builder);

                if (useOtlpExporter)
                {
                    tracing.AddOtlpExporter();
                }
            });
    }

    private static ResourceBuilder CreateResourceBuilder(WebApplicationBuilder builder)
    {
        return ConfigureResource(ResourceBuilder.CreateDefault(), builder);
    }

    private static ResourceBuilder ConfigureResource(ResourceBuilder resourceBuilder, WebApplicationBuilder builder)
    {
        AssemblyName assemblyName = typeof(MicroserviceSetup).Assembly.GetName();
        return resourceBuilder
            .AddService(
                serviceName: builder.Environment.ApplicationName,
                serviceVersion: assemblyName.Version?.ToString(),
                serviceInstanceId: Environment.MachineName)
            .AddAttributes([
                new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)
            ]);
    }

    private static void ConfigureSampling(TracerProviderBuilder tracing, WebApplicationBuilder builder)
    {
        double? samplingRatio = builder.Configuration.GetValue<double?>(TraceSamplingRatioConfigurationKey);
        if (samplingRatio is null)
        {
            return;
        }

        tracing.SetSampler(new ParentBasedSampler(
            new TraceIdRatioBasedSampler(Math.Clamp(samplingRatio.Value, 0, 1))));
    }

    private static bool UseOtlpExporter(WebApplicationBuilder builder)
    {
        return !string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointConfigurationKey]);
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
