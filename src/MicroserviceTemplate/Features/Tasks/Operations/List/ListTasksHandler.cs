using System.Linq.Expressions;
using System.Text.Json;
using MicroserviceTemplate.Features.Tasks.Models;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.Operations.List;

public sealed class ListTasksHandler(
    ApplicationDbContext dbContext,
    ITaskCacheService taskCache)
{
    private static readonly Expression<Func<TaskItem, ListTasksResponse>> ToResponseProjection = task => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    public async Task<IEnumerable<ListTasksResponse>> Handle(
        ListTasksRequest _,
        CancellationToken cancellationToken)
    {
        var cached = await taskCache.GetListAsync(cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<ListTasksResponse>>(cached) ?? [];
        }

        var dtos = await dbContext.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .Select(ToResponseProjection)
            .ToListAsync(cancellationToken);

        await taskCache.SetListAsync(JsonSerializer.Serialize(dtos), cancellationToken);

        return dtos;
    }
}
