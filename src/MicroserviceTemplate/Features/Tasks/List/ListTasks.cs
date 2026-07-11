using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.List;

public sealed class ListTasks
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageNumber = 100_000;
    public const int MaxPageSize = 100;

    private ListTasks() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapGet("", Handle)
            .WithName("ListTasks")
            .WithSummary("List tasks")
            .ProducesCommonProblems();

    internal static async Task<Ok<PagedResult<TaskRepresentation>>> Handle(
        int? pageNumber,
        int? pageSize,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        int resolvedPageNumber = Math.Clamp(pageNumber ?? DefaultPageNumber, 1, MaxPageNumber);
        int resolvedPageSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        int skip = (resolvedPageNumber - 1) * resolvedPageSize;

        IQueryable<TaskItem> query = dbContext.Tasks.AsNoTracking();
        int totalCount = await query.CountAsync(cancellationToken);
        List<TaskRepresentation> tasks = await query
            .OrderByDescending(task => task.CreatedAt)
            .ThenByDescending(task => task.Id)
            .Skip(skip)
            .Take(resolvedPageSize)
            .Select(TaskMappings.Projection)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(PagedResult<TaskRepresentation>.Create(
            tasks,
            resolvedPageNumber,
            resolvedPageSize,
            totalCount));
    }
}
