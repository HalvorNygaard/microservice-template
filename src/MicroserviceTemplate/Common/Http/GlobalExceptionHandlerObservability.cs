using System.Diagnostics;
using System.Diagnostics.Metrics;
using ModernMicroservice.Common;

namespace ModernMicroservice.Common.Http;

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
        description: "Number of exceptions handled by status code and error category.");

    internal static void RecordExceptionHandled(int statusCode, string errorCategory) =>
        ExceptionsHandled.Add(
            1,
            MicroserviceTelemetry.StatusCodeTag(statusCode),
            MicroserviceTelemetry.ErrorCategoryTag(errorCategory));

    internal static void EnrichCurrentActivity(int statusCode, string errorCategory)
    {
        Activity? activity = Activity.Current;
        activity?.SetTag(MicroserviceTelemetry.ErrorCategoryAttributeName, errorCategory);
        activity?.SetTag(MicroserviceTelemetry.StatusCodeAttributeName, statusCode);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            activity?.SetStatus(ActivityStatusCode.Error, errorCategory);
        }
    }
}
