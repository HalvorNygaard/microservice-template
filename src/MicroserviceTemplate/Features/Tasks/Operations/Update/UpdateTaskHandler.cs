using System.Diagnostics;
using MicroserviceTemplate.Common;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Features.Tasks.Models;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MicroserviceTemplate.Features.Tasks.Operations.Update;

public sealed class UpdateTaskHandler(
    ApplicationDbContext dbContext,
    ITaskCacheService taskCache,
    ILogger<UpdateTaskHandler> logger)
{
    public async Task<UpdateTaskResponse?> Handle(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        return await TaskObservability.StartTaskActivity("update").ObserveAsync(async activity =>
        {
            var task = await dbContext.Tasks.FindAsync([id], cancellationToken);

            if (task is null)
            {
                activity.SetOutcome("not_found");
                TaskObservability.RecordTaskChanged("update", "not_found");
                TaskObservability.RecordTaskOperationDuration("update", stopwatch.Elapsed);
                return null;
            }

            task.Title = request.Title;
            task.Description = request.Description;
            task.Status = request.Status;
            task.DueDate = request.DueDate;
            task.UpdateTimestamp();

            await dbContext.SaveChangesAsync(cancellationToken);

            await taskCache.InvalidateTaskAsync(id, cancellationToken);

            string status = task.Status.ToString();
            activity.SetOutcome("updated");
            TaskObservability.RecordTaskChanged("update", "updated", status);
            TaskObservability.RecordTaskOperationDuration("update", stopwatch.Elapsed);
            logger.TaskUpdated(task.Id, status);

            return ToResponse(task);
        });
    }

    private static UpdateTaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);
}
