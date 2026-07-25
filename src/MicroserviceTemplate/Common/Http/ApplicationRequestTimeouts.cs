using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ModernMicroservice.Common.Http;

internal sealed class ApplicationRequestTimeoutOptions
{
    internal const string SectionName = "RequestTimeouts";

    public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

internal static class ApplicationRequestTimeouts
{
    internal const string ApiPolicy = "api";
    internal const string HealthCheckPolicy = "health-check";

    internal static IServiceCollection AddApplicationRequestTimeouts(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApplicationRequestTimeoutOptions>()
            .Bind(configuration.GetSection(ApplicationRequestTimeoutOptions.SectionName))
            .Validate(
                options => IsBounded(options.ApiTimeout, TimeSpan.FromMinutes(5)),
                "API timeout must be greater than zero and at most five minutes.")
            .Validate(
                options => IsBounded(options.HealthCheckTimeout, TimeSpan.FromSeconds(30)),
                "Health-check timeout must be greater than zero and at most thirty seconds.")
            .ValidateOnStart();

        services.AddRequestTimeouts();
        services.AddOptions<RequestTimeoutOptions>()
            .Configure<IOptions<ApplicationRequestTimeoutOptions>>((policies, configured) =>
            {
                ApplicationRequestTimeoutOptions options = configured.Value;
                policies.AddPolicy(ApiPolicy, CreatePolicy(options.ApiTimeout));
                policies.AddPolicy(HealthCheckPolicy, CreatePolicy(options.HealthCheckTimeout));
            });

        return services;
    }

    private static bool IsBounded(TimeSpan timeout, TimeSpan maximum) =>
        timeout > TimeSpan.Zero && timeout <= maximum;

    private static RequestTimeoutPolicy CreatePolicy(TimeSpan timeout) => new()
    {
        Timeout = timeout,
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
        WriteTimeoutResponse = WriteTimeoutProblemAsync
    };

    private static async Task WriteTimeoutProblemAsync(HttpContext context)
    {
        IProblemDetailsService problemDetails = context.RequestServices
            .GetRequiredService<IProblemDetailsService>();
        _ = await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "The request timed out.",
                Type = ApiProblemTypes.RequestTimeout,
                Extensions =
                {
                    ["code"] = "Request.Timeout"
                }
            }
        });
    }
}
