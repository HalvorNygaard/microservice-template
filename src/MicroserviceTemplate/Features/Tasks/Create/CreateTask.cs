using System.ComponentModel.DataAnnotations;
using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MicroserviceTemplate.Features.Tasks.Create;

public sealed class CreateTask
{
    private CreateTask() { }

    public sealed record Request(
        [property: Required, StringLength(200, MinimumLength = 3), RegularExpression(@"^\s*\S.{1,}\S\s*$")] string Title,
        [property: Required, StringLength(2000, MinimumLength = 10), RegularExpression(@"^\s*\S.{8,}\S\s*$")] string Description,
        DateTimeOffset? DueDate = null);

    internal static void Map(RouteGroupBuilder group) =>
        group.MapPost("", Handle)
            .WithName("CreateTask")
            .WithSummary("Create a task")
            .ProducesValidationProblem()
            .ProducesCommonProblems();

    internal static async Task<Created<TaskRepresentation>> Handle(
        Request request,
        ApplicationDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<CreateTask> logger,
        CancellationToken cancellationToken)
    {
        TaskItem task = TaskItem.Create(request.Title, request.Description, request.DueDate, timeProvider);
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        TaskObservability.RecordChange("create", task.Status);
        logger.TaskCreated(task.Id, task.Status.ToString());
        return TypedResults.Created($"{TasksFeature.RoutePrefix}/{task.Id}", task.ToRepresentation());
    }
}
