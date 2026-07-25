using ModernMicroservice.Common.Http;
using ModernMicroservice.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ModernMicroservice.Features.Tasks.Delete;

internal sealed class DeleteTask
{
    private DeleteTask() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle)
            .WithName("DeleteTask")
            .WithSummary("Delete a task")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesCommonProblems()
            .WithRequestTimeout(ApplicationRequestTimeouts.ApiPolicy);

    internal static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid id,
        ApplicationDbContext dbContext,
        ILogger<DeleteTask> logger,
        CancellationToken cancellationToken)
    {
        TaskItem? task = await dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return ApiProblems.NotFound($"Task {id} was not found.", "Task.NotFound");
        }

        dbContext.Tasks.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        TaskObservability.RecordChange("delete", task.Status);
        logger.TaskDeleted(task.Id);
        return TypedResults.NoContent();
    }
}
