using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MicroserviceTemplate.Common;

internal static class MicroserviceTelemetry
{
    internal const string MeterName = "MicroserviceTemplate";
    internal const string ActivitySourceName = "MicroserviceTemplate";
    internal static readonly string AttributePrefix = ActivitySourceName.ToLowerInvariant();
    internal static readonly string FeatureAttributeName = Name("feature");
    internal static readonly string OperationAttributeName = Name("operation");
    internal static readonly string OutcomeAttributeName = Name("outcome");
    internal static readonly string ResultCountAttributeName = Name("result_count");
    internal static readonly string LimitAttributeName = Name("limit");
    internal static readonly string StatusCodeAttributeName = Name("status_code");
    internal static readonly string ErrorTypeAttributeName = Name("error_type");
    internal static readonly string CacheHitAttributeName = Name("cache_hit");

    internal static readonly Meter Meter = new(MeterName);
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    internal static string Name(string name) => $"{AttributePrefix}.{name}";

    internal static void SetOutcome(this Activity? activity, string outcome) =>
        activity?.SetTag(OutcomeAttributeName, outcome);

    internal static Activity? StartActivity(string name, string feature, string operation)
    {
        Activity? activity = ActivitySource.StartActivity(name, ActivityKind.Internal);
        activity?.SetTag(FeatureAttributeName, feature);
        activity?.SetTag(OperationAttributeName, operation);
        return activity;
    }

    internal static bool RecordFailureAndPropagate(this Activity? activity, Exception exception)
    {
        if (exception is OperationCanceledException)
        {
            activity.SetOutcome("canceled");
            return false;
        }

        string exceptionType = exception.GetType().Name;
        activity.SetOutcome("failed");
        activity?.SetTag("exception.type", exceptionType);
        activity?.SetStatus(ActivityStatusCode.Error, exceptionType);
        activity?.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                ["exception.type"] = exceptionType
            }));
        return false;
    }

    internal static KeyValuePair<string, object?> FeatureTag(string feature) => new(FeatureAttributeName, feature);

    internal static KeyValuePair<string, object?> OperationTag(string operation) => new(OperationAttributeName, operation);

    internal static KeyValuePair<string, object?> OutcomeTag(string outcome) => new(OutcomeAttributeName, outcome);

    internal static KeyValuePair<string, object?> StatusCodeTag(int statusCode) => new(StatusCodeAttributeName, statusCode);

    internal static KeyValuePair<string, object?> ErrorTypeTag(string errorType) => new(ErrorTypeAttributeName, errorType);

    internal static KeyValuePair<string, object?> CacheHitTag(bool cacheHit) => new(CacheHitAttributeName, cacheHit);
}

internal static class ActivityExtensions
{
    internal static async Task<T> ObserveAsync<T>(
        this Activity? activity,
        Func<Activity?, Task<T>> operation)
    {
        using (activity)
        {
            try
            {
                return await operation(activity);
            }
            catch (Exception exception) when (activity.RecordFailureAndPropagate(exception))
            {
                throw;
            }
        }
    }

    internal static async Task ObserveAsync(
        this Activity? activity,
        Func<Activity?, Task> operation)
    {
        using (activity)
        {
            try
            {
                await operation(activity);
            }
            catch (Exception exception) when (activity.RecordFailureAndPropagate(exception))
            {
                throw;
            }
        }
    }
}
