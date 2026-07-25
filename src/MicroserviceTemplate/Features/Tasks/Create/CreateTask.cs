using System.ComponentModel.DataAnnotations;
using ModernMicroservice.Common.Http;
using ModernMicroservice.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ModernMicroservice.Features.Tasks.Create;

public sealed record CreateTaskRequest(
    [property: Required, StringLength(200, MinimumLength = 3), RegularExpression(@"^\s*\S.{1,}\S\s*$")] string Title,
    [property: Required, StringLength(2000, MinimumLength = 10), RegularExpression(@"^\s*\S.{8,}\S\s*$")] string Description);

internal sealed class CreateTask
{
    private CreateTask() { }

    internal static void Map(RouteGroupBuilder group) =>
        group.MapPost("", Handle)
            .WithName("CreateTask")
            .WithSummary("Create a task")
            .ProducesValidationProblem()
            .ProducesCommonProblems()
            .WithRequestTimeout(ApplicationRequestTimeouts.ApiPolicy);

    internal static async Task<Created<TaskRepresentation>> Handle(
        CreateTaskRequest request,
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<CreateTask> logger,
        CancellationToken cancellationToken)
    {
        TaskItem task = TaskItem.Create(request.Title, request.Description, timeProvider);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        TaskObservability.RecordChange("create", task.Status);
        logger.TaskCreated(task.Id, task.Status);
        return TypedResults.Created($"{TasksFeature.RoutePrefix}/{task.Id}", task.ToRepresentation());
    }
}
