using System.Linq.Expressions;
using System.Text.Json;
using MicroserviceTemplate.Features.Tasks.Models;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.Operations.Read;

public sealed class ReadTaskHandler(
    ApplicationDbContext dbContext,
    ITaskCacheService taskCache)
{
    private static readonly Expression<Func<TaskItem, ReadTaskResponse>> ToResponseProjection = task => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    public async Task<ReadTaskResponse?> Handle(
        ReadTaskRequest request,
        CancellationToken cancellationToken)
    {
        var cached = await taskCache.GetTaskAsync(request.Id, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<ReadTaskResponse>(cached);
        }

        var dto = await dbContext.Tasks
            .Where(task => task.Id == request.Id)
            .Select(ToResponseProjection)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
        {
            return null;
        }

        await taskCache.SetTaskAsync(request.Id, JsonSerializer.Serialize(dto), cancellationToken);

        return dto;
    }
}
