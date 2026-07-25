using ModernMicroservice.Features.Tasks.Complete;
using ModernMicroservice.Features.Tasks.Create;
using ModernMicroservice.Features.Tasks.Delete;
using ModernMicroservice.Features.Tasks.Get;

namespace ModernMicroservice.Features.Tasks;

internal static class TasksFeature
{
    internal const string RoutePrefix = "/api/v1/tasks";

    internal static IEndpointRouteBuilder MapTasks(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup(RoutePrefix).WithTags("Tasks");

        CreateTask.Map(group);
        GetTask.Map(group);
        CompleteTask.Map(group);
        DeleteTask.Map(group);

        return endpoints;
    }
}
