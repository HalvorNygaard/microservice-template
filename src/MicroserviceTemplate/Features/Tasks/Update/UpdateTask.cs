using System.ComponentModel.DataAnnotations;
using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MicroserviceTemplate.Features.Tasks.Update;

public sealed class UpdateTask
{
    private UpdateTask() { }

    public sealed record Request(
        [property: Required, StringLength(200, MinimumLength = 3), RegularExpression(@"^\s*\S.{1,}\S\s*$")] string Title,
        [property: Required, StringLength(2000, MinimumLength = 10), RegularExpression(@"^\s*\S.{8,}\S\s*$")] string Description,
        [property: EnumDataType(typeof(TaskItemStatus))] TaskItemStatus Status,
        DateTimeOffset? DueDate,
        Guid Version);

    internal static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}", Handle)
            .WithName("UpdateTask")
            .WithSummary("Update a task")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesCommonProblems();

    internal static async Task<Results<Ok<TaskRepresentation>, ProblemHttpResult>> Handle(
        Guid id,
        Request request,
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<UpdateTask> logger,
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

        string? transitionError = task.Update(
            request.Title,
            request.Description,
            request.Status,
            request.DueDate,
            timeProvider);
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

        TaskObservability.RecordChange("update", task.Status);
        logger.TaskUpdated(task.Id, task.Status.ToString());
        return TypedResults.Ok(task.ToRepresentation());
    }

    private static ProblemHttpResult VersionConflict(Guid id) =>
        ApiProblems.Conflict(
            "Task version conflict",
            $"Task {id} changed after it was read. Reload it and retry with the latest version.",
            "Task.VersionConflict");
}
