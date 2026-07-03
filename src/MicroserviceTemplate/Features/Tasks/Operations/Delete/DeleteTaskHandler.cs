using System.Diagnostics;
using MicroserviceTemplate.Common;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MicroserviceTemplate.Features.Tasks.Operations.Delete;

public sealed class DeleteTaskHandler(
    ApplicationDbContext dbContext,
    ITaskCacheService taskCache,
    ILogger<DeleteTaskHandler> logger)
{
    public async Task<bool> Handle(
        DeleteTaskRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        return await TaskObservability.StartTaskActivity("delete").ObserveAsync(async activity =>
        {
            var task = await dbContext.Tasks.FindAsync([request.Id], cancellationToken);

            if (task is null)
            {
                activity.SetOutcome("not_found");
                TaskObservability.RecordTaskChanged("delete", "not_found");
                TaskObservability.RecordTaskOperationDuration("delete", stopwatch.Elapsed);
                return false;
            }

            dbContext.Tasks.Remove(task);
            await dbContext.SaveChangesAsync(cancellationToken);

            await taskCache.InvalidateTaskAsync(request.Id, cancellationToken);

            activity.SetOutcome("deleted");
            TaskObservability.RecordTaskChanged("delete", "deleted");
            TaskObservability.RecordTaskOperationDuration("delete", stopwatch.Elapsed);
            logger.TaskDeleted(request.Id);

            return true;
        });
    }
}
