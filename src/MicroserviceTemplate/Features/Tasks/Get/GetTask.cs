using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.Get;

public sealed class GetTask
{
    private GetTask() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle)
            .WithName("GetTask")
            .WithSummary("Get a task")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesCommonProblems();

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
