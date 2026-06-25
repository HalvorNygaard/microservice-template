using MicroserviceTemplate.Features.Tasks.Models;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MicroserviceTemplate.Features.Tasks.Operations.Create;

public sealed class CreateTaskHandler(
    ApplicationDbContext dbContext,
    ITaskCacheService taskCache,
    ILogger<CreateTaskHandler> logger)
{
    public async Task<CreateTaskResponse> Handle(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            DueDate = request.DueDate
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        await taskCache.InvalidateListAsync(cancellationToken);

        logger.LogInformation("Created task {TaskId} with title '{Title}'", task.Id, task.Title);

        return ToResponse(task);
    }

    private static CreateTaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);
}
