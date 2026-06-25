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
        var task = await dbContext.Tasks.FindAsync([id], cancellationToken);

        if (task is null)
        {
            return null;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.UpdateTimestamp();

        await dbContext.SaveChangesAsync(cancellationToken);

        await taskCache.InvalidateTaskAsync(id, cancellationToken);

        logger.LogInformation("Updated task {TaskId}", task.Id);

        return ToResponse(task);
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
