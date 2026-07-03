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
}
