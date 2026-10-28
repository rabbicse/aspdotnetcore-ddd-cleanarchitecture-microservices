using Mehedi.Application.SharedKernel.Services;

namespace KYC.Application.UnitTests.Support;

/// <summary>
/// A cache that never caches: it always runs the factory, and records which keys were requested or removed
/// so tests can assert on cache usage without a Redis server.
/// </summary>
internal sealed class PassThroughCacheService : ICacheService
{
    public List<string> RequestedKeys { get; } = [];
    public List<string> RemovedKeys { get; } = [];

    public Task<TItem> GetOrCreateAsync<TItem>(string cacheKey, Func<Task<TItem>> factory)
    {
        RequestedKeys.Add(cacheKey);
        return factory();
    }

    public Task<IReadOnlyList<TItem>> GetOrCreateAsync<TItem>(string cacheKey, Func<Task<IReadOnlyList<TItem>>> factory)
    {
        RequestedKeys.Add(cacheKey);
        return factory();
    }

    public Task RemoveAsync(params string[] cacheKeys)
    {
        RemovedKeys.AddRange(cacheKeys);
        return Task.CompletedTask;
    }
}
