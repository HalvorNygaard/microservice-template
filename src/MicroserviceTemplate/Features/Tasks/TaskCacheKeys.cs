namespace MicroserviceTemplate.Features.Tasks;

internal static class TaskCacheKeys
{
    public const string AllTasks = "tasks:all";

    public static string ForTask(Guid id) => $"tasks:{id}";
}
