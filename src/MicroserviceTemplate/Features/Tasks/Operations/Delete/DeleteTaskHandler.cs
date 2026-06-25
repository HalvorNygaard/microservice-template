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
        var task = await dbContext.Tasks.FindAsync([request.Id], cancellationToken);

        if (task is null)
        {
            return false;
        }

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        await taskCache.InvalidateTaskAsync(request.Id, cancellationToken);

        logger.LogInformation("Deleted task {TaskId}", request.Id);

        return true;
    }
}
