using System.Diagnostics;
using System.Linq.Expressions;
using MicroserviceTemplate.Common;
using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Features.Tasks.Models;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.Operations.List;

public sealed class ListTasksHandler(
    ApplicationDbContext dbContext)
{
    private static readonly Expression<Func<TaskItem, ListTasksResponse>> ToResponseProjection = task => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);

    public async Task<PagedResult<ListTasksResponse>> Handle(
        ListTasksRequest request,
        CancellationToken cancellationToken)
    {
        int pageNumber = request.ResolvedPageNumber;
        int pageSize = request.ResolvedPageSize;
        int skip = (pageNumber - 1) * pageSize;
        var stopwatch = Stopwatch.StartNew();
        return await TaskObservability.StartTaskQueryActivity("list", pageSize).ObserveAsync(async activity =>
        {
            IQueryable<TaskItem> query = dbContext.Tasks.AsNoTracking();
            int totalCount = await query.CountAsync(cancellationToken);
            List<ListTasksResponse> tasks = await query
                .OrderByDescending(task => task.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(ToResponseProjection)
                .ToListAsync(cancellationToken);

            var result = PagedResult<ListTasksResponse>.Create(tasks, pageNumber, pageSize, totalCount);

            activity?.SetTag(MicroserviceTelemetry.CacheHitAttributeName, false);
            activity?.SetTag(MicroserviceTelemetry.ResultCountAttributeName, tasks.Count);
            activity.SetOutcome("database");
            TaskObservability.RecordTaskQuery(tasks.Count, cacheHit: false);
            TaskObservability.RecordTaskOperationDuration("list", stopwatch.Elapsed);
            return result;
        });
    }
}
