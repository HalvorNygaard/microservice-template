using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace ModernMicroservice.Common.Http;

internal static class ProblemDetailsExtensions
{
    internal static IServiceCollection AddApplicationProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                Activity? activity = Activity.Current;
                int statusCode = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                context.ProblemDetails.Status = statusCode;
                context.ProblemDetails.Title ??= ReasonPhrases.GetReasonPhrase(statusCode);
                if (string.IsNullOrWhiteSpace(context.ProblemDetails.Type)
                    || IsFrameworkRfcType(context.ProblemDetails.Type))
                {
                    context.ProblemDetails.Type = ApiProblemTypes.ForStatus(statusCode);
                }

                context.ProblemDetails.Extensions.TryAdd("code", ApiProblemTypes.ErrorCodeFor(statusCode));
                context.ProblemDetails.Extensions["requestId"] = context.HttpContext.TraceIdentifier;

                if (activity is not null && activity.TraceId != default)
                {
                    context.ProblemDetails.Extensions["traceId"] = activity.TraceId.ToString();
                    context.ProblemDetails.Extensions["spanId"] = activity.SpanId.ToString();
                }
                else
                {
                    context.ProblemDetails.Extensions.Remove("traceId");
                    context.ProblemDetails.Extensions.Remove("spanId");
                }
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    private static bool IsFrameworkRfcType(string type) =>
        Uri.TryCreate(type, UriKind.Absolute, out Uri? uri)
        && uri.Host.Equals("tools.ietf.org", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.StartsWith("/html/rfc", StringComparison.OrdinalIgnoreCase);
}

internal static class ApiProblemTypes
{
    internal const string Root = "/problems/";
    private const string CommonRoot = Root + "common/";
    private const string ServiceRoot = Root + "apiservice/";
    internal const string BadRequest = CommonRoot + "bad-request";
    internal const string NotFound = CommonRoot + "not-found";
    internal const string MethodNotAllowed = CommonRoot + "method-not-allowed";
    internal const string UnsupportedMediaType = CommonRoot + "unsupported-media-type";
    internal const string ContentTooLarge = CommonRoot + "content-too-large";
    internal const string RateLimitExceeded = CommonRoot + "rate-limit-exceeded";
    internal const string RequestTimeout = CommonRoot + "request-timeout";
    internal const string InternalServerError = CommonRoot + "internal-server-error";

    internal static string ForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => BadRequest,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status405MethodNotAllowed => MethodNotAllowed,
        StatusCodes.Status413PayloadTooLarge => ContentTooLarge,
        StatusCodes.Status415UnsupportedMediaType => UnsupportedMediaType,
        StatusCodes.Status429TooManyRequests => RateLimitExceeded,
        StatusCodes.Status504GatewayTimeout => RequestTimeout,
        >= 400 and < 500 => BadRequest,
        _ => InternalServerError
    };

    internal static string ErrorCategoryFor(int statusCode) => statusCode switch
    {
        >= 500 => "Failure",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Request"
    };

    internal static string ErrorCodeFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Request.Invalid",
        StatusCodes.Status404NotFound => "Resource.NotFound",
        StatusCodes.Status409Conflict => "Resource.Conflict",
        StatusCodes.Status504GatewayTimeout => "Request.Timeout",
        >= 400 and < 500 => "Request.Rejected",
        _ => "Server.Error"
    };

    internal static string ForServiceCode(string code)
    {
        var slug = new StringBuilder(code.Length + 8);
        for (int index = 0; index < code.Length; index++)
        {
            char character = code[index];
            if (character == '.')
            {
                slug.Append('-');
                continue;
            }

            if (char.IsUpper(character) &&
                index > 0 &&
                char.IsLower(code[index - 1]))
            {
                slug.Append('-');
            }

            slug.Append(char.ToLowerInvariant(character));
        }

        return ServiceRoot + Uri.EscapeDataString(slug.ToString());
    }
}

internal static class ApiProblems
{
    internal static ProblemHttpResult NotFound(string detail, string code) =>
        TypedResults.Problem(Create(
            StatusCodes.Status404NotFound,
            "Not Found",
            detail,
            ApiProblemTypes.ForServiceCode(code),
            code));

    private static ProblemDetails Create(
        int status,
        string title,
        string detail,
        string type,
        string code) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = type,
            Extensions =
            {
                ["code"] = code
            }
        };
}
