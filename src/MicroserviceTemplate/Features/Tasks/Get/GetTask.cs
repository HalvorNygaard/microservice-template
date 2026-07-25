using ModernMicroservice.Common.Http;
using ModernMicroservice.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ModernMicroservice.Features.Tasks.Get;

internal sealed class GetTask
{
    private GetTask() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle)
            .WithName("GetTask")
            .WithSummary("Get a task")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesCommonProblems()
            .WithRequestTimeout(ApplicationRequestTimeouts.ApiPolicy);

    internal static async Task<Results<Ok<TaskRepresentation>, ProblemHttpResult>> Handle(
        Guid id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        TaskRepresentation? task = await dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Id == id)
            .Select(TaskMappings.Projection)
            .FirstOrDefaultAsync(cancellationToken);

        return task is null
            ? ApiProblems.NotFound($"Task {id} was not found.", "Task.NotFound")
            : TypedResults.Ok(task);
    }
}
