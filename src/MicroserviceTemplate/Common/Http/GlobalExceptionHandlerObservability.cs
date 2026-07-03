using System.Diagnostics;
using System.Diagnostics.Metrics;
using MicroserviceTemplate.Common;

namespace MicroserviceTemplate.Common.Http;

internal static partial class GlobalExceptionHandlerObservability
{
    [LoggerMessage(
        EventId = 1000,
        EventName = nameof(ExceptionHandled),
        Message = "Exception handled: {ExceptionType}. TraceId={TraceId} Path={Path}")]
    internal static partial void ExceptionHandled(
        this ILogger<GlobalExceptionHandler> logger,
        LogLevel level,
        Exception exception,
        string exceptionType,
        string traceId,
        string path);

    private static readonly Counter<long> ExceptionsHandled = MicroserviceTelemetry.Meter.CreateCounter<long>(
        MicroserviceTelemetry.Name("exceptions.handled"),
        description: "Number of exceptions handled by status code and error type.");

    internal static void RecordExceptionHandled(int statusCode, string errorType) =>
        ExceptionsHandled.Add(
            1,
            MicroserviceTelemetry.StatusCodeTag(statusCode),
            MicroserviceTelemetry.ErrorTypeTag(errorType));

    internal static void EnrichCurrentActivity(int statusCode, string errorType)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag(MicroserviceTelemetry.ErrorTypeAttributeName, errorType);
        activity?.SetTag(MicroserviceTelemetry.StatusCodeAttributeName, statusCode);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            activity?.SetStatus(ActivityStatusCode.Error, errorType);
        }
    }
}
