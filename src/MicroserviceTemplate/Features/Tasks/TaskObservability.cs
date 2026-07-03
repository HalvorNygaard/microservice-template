using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using MicroserviceTemplate.Common;

namespace MicroserviceTemplate.Features.Tasks;

internal static partial class TaskObservability
{
    [LoggerMessage(
        EventId = 2001,
        EventName = nameof(TaskCreated),
        Level = LogLevel.Information,
        Message = "Created task {TaskId} with status {Status}")]
    internal static partial void TaskCreated(this ILogger logger, Guid taskId, string status);

    [LoggerMessage(
        EventId = 2002,
        EventName = nameof(TaskUpdated),
        Level = LogLevel.Information,
        Message = "Updated task {TaskId} with status {Status}")]
    internal static partial void TaskUpdated(this ILogger logger, Guid taskId, string status);

    [LoggerMessage(
        EventId = 2003,
        EventName = nameof(TaskDeleted),
        Level = LogLevel.Information,
        Message = "Deleted task {TaskId}")]
    internal static partial void TaskDeleted(this ILogger logger, Guid taskId);

    private static readonly Counter<long> TasksCreated = MicroserviceTelemetry.Meter.CreateCounter<long>(
        MicroserviceTelemetry.Name("tasks.created"),
        description: "Number of tasks created.");

    private static readonly Counter<long> TasksChanged = MicroserviceTelemetry.Meter.CreateCounter<long>(
        MicroserviceTelemetry.Name("tasks.changed"),
        description: "Number of task changes by operation and outcome.");

    private static readonly Histogram<double> TaskOperationDuration = MicroserviceTelemetry.Meter.CreateHistogram<double>(
        MicroserviceTelemetry.Name("tasks.operation.duration"),
        unit: "ms",
        description: "Duration of task operations.");

    private static readonly Histogram<int> TaskQueryResults = MicroserviceTelemetry.Meter.CreateHistogram<int>(
        MicroserviceTelemetry.Name("tasks.query_results"),
        unit: "{tasks}",
        description: "Number of tasks returned by task queries.");

    internal static void RecordTaskCreated(string status)
    {
        TasksCreated.Add(1, StatusTag(status));
        TasksChanged.Add(
            1,
            MicroserviceTelemetry.OperationTag("create"),
            MicroserviceTelemetry.OutcomeTag("created"),
            StatusTag(status));
    }

    internal static void RecordTaskChanged(string operation, string outcome) =>
        TasksChanged.Add(
            1,
            MicroserviceTelemetry.OperationTag(operation),
            MicroserviceTelemetry.OutcomeTag(outcome));

    internal static void RecordTaskChanged(string operation, string outcome, string status) =>
        TasksChanged.Add(
            1,
            MicroserviceTelemetry.OperationTag(operation),
            MicroserviceTelemetry.OutcomeTag(outcome),
            StatusTag(status));

    internal static void RecordTaskQuery(int resultCount, bool cacheHit)
    {
        TaskQueryResults.Record(resultCount, MicroserviceTelemetry.CacheHitTag(cacheHit));
    }

    internal static void RecordTaskOperationDuration(string operation, TimeSpan elapsed) =>
        TaskOperationDuration.Record(
            elapsed.TotalMilliseconds,
            MicroserviceTelemetry.OperationTag(operation));

    internal static Activity? StartTaskActivity(string operation) =>
        MicroserviceTelemetry.StartActivity(MicroserviceTelemetry.Name($"tasks.{operation}"), "tasks", operation);

    internal static Activity? StartTaskQueryActivity(string operation, int? limit = null)
    {
        Activity? activity = StartTaskActivity(operation);
        if (limit is not null)
        {
            activity?.SetTag(MicroserviceTelemetry.LimitAttributeName, limit.Value);
        }

        return activity;
    }

    private static KeyValuePair<string, object?> StatusTag(string status) =>
        new(MicroserviceTelemetry.Name("status"), status);
}
