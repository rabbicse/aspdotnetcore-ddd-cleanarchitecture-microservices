using Mehedi.Application.SharedKernel.Services;

namespace KYC.API.Tests;

/// <summary>A cache that never caches, so every request reaches the (fake) repository.</summary>
internal sealed class PassThroughCacheService : ICacheService
{
    public Task<TItem> GetOrCreateAsync<TItem>(string cacheKey, Func<Task<TItem>> factory) => factory();

    public Task<IReadOnlyList<TItem>> GetOrCreateAsync<TItem>(string cacheKey, Func<Task<IReadOnlyList<TItem>>> factory) => factory();

    public Task RemoveAsync(params string[] cacheKeys) => Task.CompletedTask;
}
