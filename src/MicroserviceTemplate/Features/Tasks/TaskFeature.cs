using MicroserviceTemplate.Configurations.Setup;
using MicroserviceTemplate.Features.Tasks.Operations.Create;
using MicroserviceTemplate.Features.Tasks.Operations.Delete;
using MicroserviceTemplate.Features.Tasks.Operations.List;
using MicroserviceTemplate.Features.Tasks.Operations.Read;
using MicroserviceTemplate.Features.Tasks.Operations.Update;
using MicroserviceTemplate.Features.Tasks.Services;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;

namespace MicroserviceTemplate.Features.Tasks;

public static class TaskFeature
{
    public static IServiceCollection AddTasks(this IServiceCollection services)
    {
        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<ReadTaskHandler>();
        services.AddScoped<ListTasksHandler>();
        services.AddScoped<UpdateTaskHandler>();
        services.AddScoped<DeleteTaskHandler>();

        services.AddScoped<ITaskCacheService, TaskCacheService>();

        return services;
    }

    public static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder app)
    {
        var tasksApi = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .RequireRateLimiting(RateLimitingSetup.ApiPolicyName);

        tasksApi.MapGet("", async (
            ListTasksHandler handler,
            CancellationToken cancellationToken) =>
        {
            var tasks = await handler.Handle(new ListTasksRequest(), cancellationToken);
            return TypedResults.Ok(tasks);
        })
        .WithName("GetAllTasks")
        .WithSummary("Get all tasks");

        tasksApi.MapGet("/{id}", async (
            string id,
            ReadTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var taskId))
            {
                return InvalidTaskId();
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
        .WithSummary("Get a task by ID");

        tasksApi.MapPost("", async (
            CreateTaskRequest request,
            CreateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var task = await handler.Handle(request, cancellationToken);
            return TypedResults.Created($"/api/tasks/{task.Id}", task);
        })
        .WithName("CreateTask")
        .WithSummary("Create a new task");

        tasksApi.MapPut("/{id}", async (
            string id,
            UpdateTaskRequest request,
            UpdateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var taskId))
            {
                return InvalidTaskId();
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
        .WithSummary("Update an existing task");

        tasksApi.MapDelete("/{id}", async (
            string id,
            DeleteTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var taskId))
            {
                return InvalidTaskId();
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
        .WithSummary("Delete a task");

        return app;
    }

    private static IResult InvalidTaskId() => Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid Task ID",
        detail: "The task ID route value must be a valid GUID.");
}
