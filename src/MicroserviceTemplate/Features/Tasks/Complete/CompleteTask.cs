using ModernMicroservice.Common.Http;
using ModernMicroservice.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ModernMicroservice.Features.Tasks.Complete;

internal sealed class CompleteTask
{
    private CompleteTask() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapPost("/{id:guid}/complete", Handle)
            .WithName("CompleteTask")
            .WithSummary("Complete a task")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesCommonProblems()
            .WithRequestTimeout(ApplicationRequestTimeouts.ApiPolicy);

    internal static async Task<Results<Ok<TaskRepresentation>, ProblemHttpResult>> Handle(
        Guid id,
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

        if (task.Complete(timeProvider))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            TaskObservability.RecordChange("complete", task.Status);
            logger.TaskCompleted(task.Id);
        }

        return TypedResults.Ok(task.ToRepresentation());
    }
}
