namespace MicroserviceTemplate.Features.Tasks;

internal static class TaskCacheKeys
{
    public static string ForTask(Guid id) => $"tasks:{id}";
}
