namespace MicroserviceTemplate.Features.Tasks.Services.Abstractions;

public interface ITaskCacheService
{
    Task<string?> GetListAsync(CancellationToken cancellationToken);

    Task SetListAsync(string value, CancellationToken cancellationToken);

    Task<string?> GetTaskAsync(Guid id, CancellationToken cancellationToken);

    Task SetTaskAsync(Guid id, string value, CancellationToken cancellationToken);

    Task InvalidateListAsync(CancellationToken cancellationToken);

    Task InvalidateTaskAsync(Guid id, CancellationToken cancellationToken);
}
