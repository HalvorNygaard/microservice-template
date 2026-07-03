using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Common.Http;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (problemDetails, logLevel) = exception switch
        {
            ApplicationProblemException applicationProblem => (applicationProblem.ToProblemDetails(), LogLevel.Warning),
            BadHttpRequestException => (CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid Request",
                "The request is invalid.",
                "Validation",
                "Request.Invalid"), LogLevel.Warning),
            JsonException => (CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid JSON",
                "The request body contains invalid JSON.",
                "Validation",
                "Request.InvalidJson"), LogLevel.Warning),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested => (CreateProblemDetails(
                StatusCodes.Status499ClientClosedRequest,
                "Client Closed Request",
                "The request was canceled by the client.",
                "Request",
                "Request.Canceled"), LogLevel.Information),
            TimeoutException => (CreateProblemDetails(
                StatusCodes.Status504GatewayTimeout,
                "Gateway Timeout",
                "The server timed out while processing the request.",
                "Request",
                "Request.Timeout"), LogLevel.Warning),
            DbUpdateConcurrencyException => (CreateProblemDetails(
                StatusCodes.Status409Conflict,
                "Concurrency Conflict",
                "The record was modified by another user. Please try again.",
                "Conflict",
                "Resource.ConcurrencyConflict"), LogLevel.Warning),
            _ => (CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An error occurred while processing your request.",
                "Failure",
                "Server.Error"), LogLevel.Error)
        };

        logger.ExceptionHandled(
            logLevel,
            exception,
            exception.GetType().Name,
            httpContext.TraceIdentifier,
            httpContext.Request.Path.Value ?? "/");

        int statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        string errorType = problemDetails.Extensions["errorType"]?.ToString() ?? "Unknown";
        GlobalExceptionHandlerObservability.RecordExceptionHandled(statusCode, errorType);
        GlobalExceptionHandlerObservability.EnrichCurrentActivity(statusCode, errorType);

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, problemDetails, cancellationToken: cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string errorType,
        string errorCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = GetTypeUri(statusCode),
            Extensions =
            {
                ["errorType"] = errorType,
                ["errorCode"] = errorCode
            }
        };
    }

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
