using System.Diagnostics;
using MicroserviceTemplate.Common;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Features.Tasks.Models;
using MicroserviceTemplate.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MicroserviceTemplate.Features.Tasks.Operations.Create;

public sealed class CreateTaskHandler(
    ApplicationDbContext dbContext,
    ILogger<CreateTaskHandler> logger)
{
    public async Task<CreateTaskResponse> Handle(
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        return await TaskObservability.StartTaskActivity("create").ObserveAsync(async activity =>
        {
            var task = new TaskItem
            {
                Title = request.Title,
                Description = request.Description,
                Status = request.Status,
                DueDate = request.DueDate
            };

            dbContext.Tasks.Add(task);
            await dbContext.SaveChangesAsync(cancellationToken);

            string status = task.Status.ToString();
            activity.SetOutcome("created");
            TaskObservability.RecordTaskCreated(status);
            TaskObservability.RecordTaskOperationDuration("create", stopwatch.Elapsed);
            logger.TaskCreated(task.Id, status);

            return ToResponse(task);
        });
    }

    private static CreateTaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);
}
