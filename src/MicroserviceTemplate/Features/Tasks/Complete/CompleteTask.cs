using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.Complete;

public sealed class CompleteTask
{
    private CompleteTask() { }

    public sealed record Request(Guid Version);

    internal static void Map(RouteGroupBuilder group) =>
        group.MapPost("/{id:guid}/complete", Handle)
            .WithName("CompleteTask")
            .WithSummary("Complete a task")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesCommonProblems();

    internal static async Task<Results<Ok<TaskRepresentation>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<CompleteTask> logger,
        CancellationToken cancellationToken)
    {
        TaskItem? task = await dbContext.Tasks.FindAsync([id], cancellationToken);
        if (task is null)
        {
            return ApiProblems.NotFound($"Task {id} was not found.", "Task.NotFound");
        }

        if (task.Version != request.Version)
        {
            return VersionConflict(id);
        }

        string? transitionError = task.Complete(timeProvider);
        if (transitionError is not null)
        {
            return ApiProblems.Conflict(
                "Invalid task transition",
                transitionError,
                "Task.InvalidTransition");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return VersionConflict(id);
        }

        TaskObservability.RecordChange("complete", task.Status);
        logger.TaskCompleted(task.Id);
        return TypedResults.Ok(task.ToRepresentation());
    }

    private static ProblemHttpResult VersionConflict(Guid id) =>
        ApiProblems.Conflict(
            "Task version conflict",
            $"Task {id} changed after it was read. Reload it and retry with the latest version.",
            "Task.VersionConflict");
}
