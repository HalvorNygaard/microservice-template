namespace MicroserviceTemplate.Features.Tasks.Services.Abstractions;

public interface ITaskCacheService
{
    Task<string?> GetTaskAsync(Guid id, CancellationToken cancellationToken);

    Task SetTaskAsync(Guid id, string value, CancellationToken cancellationToken);

    Task InvalidateTaskAsync(Guid id, CancellationToken cancellationToken);
}
