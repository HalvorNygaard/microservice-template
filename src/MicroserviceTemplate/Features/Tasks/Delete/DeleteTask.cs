using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MicroserviceTemplate.Features.Tasks.Delete;

public sealed class DeleteTask
{
    private DeleteTask() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle)
            .WithName("DeleteTask")
            .WithSummary("Delete a task")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesCommonProblems();

    internal static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        Guid id,
        Guid version,
        ApplicationDbContext dbContext,
        ILogger<DeleteTask> logger,
        CancellationToken cancellationToken)
    {
        TaskItem? task = await dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return ApiProblems.NotFound($"Task {id} was not found.", "Task.NotFound");
        }

        if (task.Version != version)
        {
            return ApiProblems.Conflict(
                "Task version conflict",
                $"Task {id} changed after it was read. Reload it before deleting.",
                "Task.VersionConflict");
        }

        dbContext.Tasks.Remove(task);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return ApiProblems.Conflict(
                "Task version conflict",
                $"Task {id} changed after it was read. Reload it before deleting.",
                "Task.VersionConflict");
        }

        TaskObservability.RecordChange("delete", task.Status);
        logger.TaskDeleted(task.Id);
        return TypedResults.NoContent();
    }
}
