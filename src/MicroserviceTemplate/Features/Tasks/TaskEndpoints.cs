using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Configurations.Setup;
using MicroserviceTemplate.Features.Tasks.Operations.Create;
using MicroserviceTemplate.Features.Tasks.Operations.Delete;
using MicroserviceTemplate.Features.Tasks.Operations.List;
using MicroserviceTemplate.Features.Tasks.Operations.Read;
using MicroserviceTemplate.Features.Tasks.Operations.Update;

namespace MicroserviceTemplate.Features.Tasks;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder app)
    {
        var tasksApi = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireRateLimiting(RateLimitingSetup.ApiPolicyName);

        tasksApi.MapGet("", async (
            int? pageNumber,
            int? pageSize,
            ListTasksHandler handler,
            CancellationToken cancellationToken) =>
        {
            var tasks = await handler.Handle(new ListTasksRequest(pageNumber, pageSize), cancellationToken);
            return TypedResults.Ok(tasks);
        })
        .WithName("ListTasks")
        .WithSummary("List tasks")
        .Produces<PagedResult<ListTasksResponse>>()
        .ProducesCommonProblems();

        tasksApi.MapGet("/{id}", async (
            string id,
            ReadTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var taskId))
            {
                throw InvalidTaskId();
            }

            var task = await handler.Handle(new ReadTaskRequest(taskId), cancellationToken);
            return task is not null
                ? TypedResults.Ok(task)
                : Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Task with ID {taskId} was not found");
        })
        .WithName("GetTaskById")
        .WithSummary("Get a task by ID")
        .Produces<ReadTaskResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesCommonProblems();

        tasksApi.MapPost("", async (
            CreateTaskRequest request,
            CreateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var task = await handler.Handle(request, cancellationToken);
            return TypedResults.Created($"/api/tasks/{task.Id}", task);
        })
        .WithName("CreateTask")
        .WithSummary("Create a new task")
        .Accepts<CreateTaskRequest>("application/json")
        .Produces<CreateTaskResponse>(StatusCodes.Status201Created)
        .ProducesCommonProblems();

        tasksApi.MapPut("/{id}", async (
            string id,
            UpdateTaskRequest request,
            UpdateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var taskId))
            {
                throw InvalidTaskId();
            }

            var task = await handler.Handle(taskId, request, cancellationToken);
            return task is not null
                ? TypedResults.Ok(task)
                : Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Task with ID {taskId} was not found");
        })
        .WithName("UpdateTask")
        .WithSummary("Update an existing task")
        .Accepts<UpdateTaskRequest>("application/json")
        .Produces<UpdateTaskResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesCommonProblems();

        tasksApi.MapDelete("/{id}", async (
            string id,
            DeleteTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var taskId))
            {
                throw InvalidTaskId();
            }

            var deleted = await handler.Handle(new DeleteTaskRequest(taskId), cancellationToken);
            return deleted
                ? TypedResults.NoContent()
                : Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found",
                    detail: $"Task with ID {taskId} was not found");
        })
        .WithName("DeleteTask")
        .WithSummary("Delete a task")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesCommonProblems();

        return app;
    }

    private static ApplicationProblemException InvalidTaskId() =>
        ApplicationProblemException.BadRequest(
            "Invalid Task ID",
            "The task ID route value must be a valid GUID.",
            "Task.InvalidId");
}
