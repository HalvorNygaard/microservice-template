using MicroserviceTemplate.Configurations.Options;
using MicroserviceTemplate.Features.Tasks.Services.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace MicroserviceTemplate.Features.Tasks.Services;

public sealed class TaskCacheService(
    IDistributedCache cache,
    IOptions<CacheOptions> options) : ITaskCacheService
{
    private readonly CacheOptions options = options.Value;

    public Task<string?> GetListAsync(CancellationToken cancellationToken)
        => cache.GetStringAsync(TaskCacheKeys.AllTasks, cancellationToken);

    public Task SetListAsync(string value, CancellationToken cancellationToken)
        => cache.SetStringAsync(TaskCacheKeys.AllTasks, value, CreateEntryOptions(), cancellationToken);

    public Task<string?> GetTaskAsync(Guid id, CancellationToken cancellationToken)
        => cache.GetStringAsync(TaskCacheKeys.ForTask(id), cancellationToken);

    public Task SetTaskAsync(Guid id, string value, CancellationToken cancellationToken)
        => cache.SetStringAsync(TaskCacheKeys.ForTask(id), value, CreateEntryOptions(), cancellationToken);

    public Task InvalidateListAsync(CancellationToken cancellationToken)
        => cache.RemoveAsync(TaskCacheKeys.AllTasks, cancellationToken);

    public async Task InvalidateTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(TaskCacheKeys.ForTask(id), cancellationToken);
        await InvalidateListAsync(cancellationToken);
    }

    private DistributedCacheEntryOptions CreateEntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(options.AbsoluteExpirationSeconds),
        SlidingExpiration = TimeSpan.FromSeconds(options.SlidingExpirationSeconds)
    };
}
