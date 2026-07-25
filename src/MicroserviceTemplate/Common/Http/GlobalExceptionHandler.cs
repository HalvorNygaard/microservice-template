using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ModernMicroservice.Common.Http;

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
                "Request.Invalid"), LogLevel.Warning),
            _ => (CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An error occurred while processing your request.",
                "Server.Error"), LogLevel.Error)
        };

        if (logger.IsEnabled(logLevel))
        {
            logger.ExceptionHandled(
                logLevel,
                exception,
                exception.GetType().Name,
                httpContext.TraceIdentifier,
                httpContext.Request.Path.Value ?? "/");
        }

        int statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        string errorCategory = ApiProblemTypes.ErrorCategoryFor(statusCode);
        GlobalExceptionHandlerObservability.RecordExceptionHandled(statusCode, errorCategory);
        GlobalExceptionHandlerObservability.EnrichCurrentActivity(statusCode, errorCategory);

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
        string code)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Extensions =
            {
                ["code"] = code
            }
        };
    }

}
