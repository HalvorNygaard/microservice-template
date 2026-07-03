using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using MicroserviceTemplate.Common;
using MicroserviceTemplate.Features.Tasks;
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
        var stopwatch = Stopwatch.StartNew();
        return await TaskObservability.StartTaskActivity("read").ObserveAsync(async activity =>
        {
            var cached = await taskCache.GetTaskAsync(request.Id, cancellationToken);
            if (cached is not null)
            {
                var cachedTask = JsonSerializer.Deserialize<ReadTaskResponse>(cached);
                activity?.SetTag(MicroserviceTelemetry.CacheHitAttributeName, true);
                activity?.SetTag(MicroserviceTelemetry.ResultCountAttributeName, cachedTask is null ? 0 : 1);
                activity.SetOutcome(cachedTask is null ? "not_found" : "cache_hit");
                TaskObservability.RecordTaskQuery(cachedTask is null ? 0 : 1, cacheHit: true);
                TaskObservability.RecordTaskOperationDuration("read", stopwatch.Elapsed);
                return cachedTask;
            }

            var dto = await dbContext.Tasks
                .Where(task => task.Id == request.Id)
                .Select(ToResponseProjection)
                .FirstOrDefaultAsync(cancellationToken);

            if (dto is null)
            {
                activity?.SetTag(MicroserviceTelemetry.CacheHitAttributeName, false);
                activity?.SetTag(MicroserviceTelemetry.ResultCountAttributeName, 0);
                activity.SetOutcome("not_found");
                TaskObservability.RecordTaskQuery(0, cacheHit: false);
                TaskObservability.RecordTaskOperationDuration("read", stopwatch.Elapsed);
                return null;
            }

            await taskCache.SetTaskAsync(request.Id, JsonSerializer.Serialize(dto), cancellationToken);

            activity?.SetTag(MicroserviceTelemetry.CacheHitAttributeName, false);
            activity?.SetTag(MicroserviceTelemetry.ResultCountAttributeName, 1);
            activity.SetOutcome("database");
            TaskObservability.RecordTaskQuery(1, cacheHit: false);
            TaskObservability.RecordTaskOperationDuration("read", stopwatch.Elapsed);
            return dto;
        });
    }
}
