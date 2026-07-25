using System.Diagnostics;
using ModernMicroservice.Common.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ModernMicroservice.UnitTests.Platform;

public sealed class HttpFoundationTests
{
    [Test]
    public void RequestTimeoutsHaveSimpleBoundedDefaults()
    {
        using ServiceProvider provider = BuildTimeoutServices(
            new Dictionary<string, string?>(StringComparer.Ordinal));
        RequestTimeoutOptions options = provider.GetRequiredService<IOptions<RequestTimeoutOptions>>().Value;

        options.Policies[ApplicationRequestTimeouts.ApiPolicy].Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.Policies[ApplicationRequestTimeouts.HealthCheckPolicy].Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        foreach (RequestTimeoutPolicy policy in options.Policies.Values)
        {
            policy.TimeoutStatusCode.ShouldBe(StatusCodes.Status504GatewayTimeout);
            policy.WriteTimeoutResponse.ShouldNotBeNull();
        }
    }

    [Test]
    public void RequestTimeoutsRejectOutOfBoundsConfiguration()
    {
        using ServiceProvider provider = BuildTimeoutServices(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["RequestTimeouts:HealthCheckTimeout"] = "00:00:31"
        });

        Should.Throw<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<ApplicationRequestTimeoutOptions>>().Value);
    }

    [Test]
    public void ProblemDetailsAddCorrelationWithoutOverwritingAnExplicitType()
    {
        Activity? previousActivity = Activity.Current;
        try
        {
            Activity.Current = null;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplicationProblemDetails();
            using ServiceProvider provider = services.BuildServiceProvider();
            ProblemDetailsOptions options =
                provider.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value;
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://example.test/problems/task-not-found"
            };

            options.CustomizeProblemDetails!(new ProblemDetailsContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "request-only" },
                ProblemDetails = problem
            });

            problem.Type.ShouldBe("https://example.test/problems/task-not-found");
            problem.Extensions["code"].ShouldBe("Resource.NotFound");
            problem.Extensions["requestId"].ShouldBe("request-only");
            problem.Extensions.ContainsKey("traceId").ShouldBeFalse();
            problem.Extensions.ContainsKey("spanId").ShouldBeFalse();
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    private static ServiceProvider BuildTimeoutServices(
        IReadOnlyDictionary<string, string?> values)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddApplicationRequestTimeouts(configuration);
        return services.BuildServiceProvider();
    }
}
