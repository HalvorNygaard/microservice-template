using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModernMicroservice.Common;
using ModernMicroservice.Common.Http;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ModernMicroservice.Configurations.Setup;

internal static class MicroserviceSetup
{
    private const string OtlpEndpointConfigurationKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    internal static WebApplicationBuilder AddMicroserviceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConfigureLogging(builder);
        ConfigureOpenTelemetry(builder);
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
        }
        else
        {
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
                options.UseUtcTimestamp = true;
            });
        }

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });
    }

    private static void ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        OpenTelemetryBuilder openTelemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter(MicroserviceTelemetry.MeterName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context =>
                        !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                        && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                .AddHttpClientInstrumentation());

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointConfigurationKey]))
        {
            openTelemetry.UseOtlpExporter();
        }
    }

    internal static WebApplication UseMicroserviceDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseRequestTimeouts();
        app.MapHealthChecks(HealthEndpointPath)
            .WithRequestTimeout(ApplicationRequestTimeouts.HealthCheckPolicy);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        }).WithRequestTimeout(ApplicationRequestTimeouts.HealthCheckPolicy);

        return app;
    }
}
