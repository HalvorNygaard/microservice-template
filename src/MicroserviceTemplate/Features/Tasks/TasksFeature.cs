using MicroserviceTemplate.Features.Tasks.Complete;
using MicroserviceTemplate.Features.Tasks.Create;
using MicroserviceTemplate.Features.Tasks.Delete;
using MicroserviceTemplate.Features.Tasks.Get;
using MicroserviceTemplate.Features.Tasks.List;
using MicroserviceTemplate.Features.Tasks.Update;

namespace MicroserviceTemplate.Features.Tasks;

public static class TasksFeature
{
    public const string RoutePrefix = "/api/v1/tasks";

    public static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup(RoutePrefix).WithTags("Tasks");

        CreateTask.Map(group);
        GetTask.Map(group);
        ListTasks.Map(group);
        UpdateTask.Map(group);
        CompleteTask.Map(group);
        DeleteTask.Map(group);

        return endpoints;
    }
}
