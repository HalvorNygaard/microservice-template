using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace MicroserviceTemplate.Common.Http;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddApplicationProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                Activity? activity = Activity.Current;
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = activity?.TraceId.ToString()
                    ?? context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["requestId"] = context.HttpContext.TraceIdentifier;

                if (activity is not null)
                {
                    context.ProblemDetails.Extensions["spanId"] = activity.SpanId.ToString();
                }
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }
}

internal static class ApiProblems
{
    internal static ProblemHttpResult NotFound(string detail, string errorCode) =>
        TypedResults.Problem(Create(StatusCodes.Status404NotFound, "Not Found", detail, "NotFound", errorCode));

    internal static ProblemHttpResult Conflict(string title, string detail, string errorCode) =>
        TypedResults.Problem(Create(StatusCodes.Status409Conflict, title, detail, "Conflict", errorCode));

    private static ProblemDetails Create(
        int status,
        string title,
        string detail,
        string errorType,
        string errorCode) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Extensions =
            {
                ["errorType"] = errorType,
                ["errorCode"] = errorCode
            }
        };
}
