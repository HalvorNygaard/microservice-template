using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Common.Http;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var (problemDetails, logLevel) = exception switch
        {
            BadHttpRequestException => (CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Invalid Request",
                "The request is invalid.",
                "Validation",
                "Request.Invalid"), LogLevel.Warning),
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

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
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
            Extensions =
            {
                ["errorType"] = errorType,
                ["errorCode"] = errorCode
            }
        };
    }

}
