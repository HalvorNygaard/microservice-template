using System.Diagnostics.Metrics;
using MicroserviceTemplate.Common;

namespace MicroserviceTemplate.Features.Tasks;

internal static partial class TaskObservability
{
    private static readonly Counter<long> TaskChanges = MicroserviceTelemetry.Meter.CreateCounter<long>(
        MicroserviceTelemetry.Name("tasks.changes"),
        description: "Number of durable task changes.");

    internal static void RecordChange(string operation, TaskItemStatus status) =>
        TaskChanges.Add(
            1,
            MicroserviceTelemetry.OperationTag(operation),
            new KeyValuePair<string, object?>(MicroserviceTelemetry.Name("status"), status.ToString()));

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Created task {TaskId} with status {Status}")]
    internal static partial void TaskCreated(this ILogger logger, Guid taskId, string status);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Updated task {TaskId} with status {Status}")]
    internal static partial void TaskUpdated(this ILogger logger, Guid taskId, string status);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Completed task {TaskId}")]
    internal static partial void TaskCompleted(this ILogger logger, Guid taskId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Deleted task {TaskId}")]
    internal static partial void TaskDeleted(this ILogger logger, Guid taskId);
}
