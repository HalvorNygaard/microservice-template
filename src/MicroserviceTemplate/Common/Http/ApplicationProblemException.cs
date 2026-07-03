using Microsoft.AspNetCore.Mvc;

namespace MicroserviceTemplate.Common.Http;

public sealed class ApplicationProblemException(
    int statusCode,
    string title,
    string detail,
    string errorType,
    string errorCode) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string Detail { get; } = detail;
    public string ErrorType { get; } = errorType;
    public string ErrorCode { get; } = errorCode;

    public ProblemDetails ToProblemDetails() => new()
    {
        Status = StatusCode,
        Title = Title,
        Detail = Detail,
        Type = GetTypeUri(StatusCode),
        Extensions =
        {
            ["errorType"] = ErrorType,
            ["errorCode"] = ErrorCode
        }
    };

    public static ApplicationProblemException BadRequest(string title, string detail, string errorCode) =>
        new(StatusCodes.Status400BadRequest, title, detail, "Validation", errorCode);

    private static string GetTypeUri(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        StatusCodes.Status403Forbidden => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
        StatusCodes.Status404NotFound => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
        StatusCodes.Status409Conflict => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
        StatusCodes.Status412PreconditionFailed => "https://datatracker.ietf.org/doc/html/rfc7232#section-4.2",
        StatusCodes.Status499ClientClosedRequest => "https://www.nginx.com/resources/wiki/start/topics/tutorials/config_pitfalls/#499-client-closed-request",
        StatusCodes.Status504GatewayTimeout => "https://datatracker.ietf.org/doc/html/rfc9110#status.504",
        _ => "https://datatracker.ietf.org/doc/html/rfc9110#status.500"
    };
}
