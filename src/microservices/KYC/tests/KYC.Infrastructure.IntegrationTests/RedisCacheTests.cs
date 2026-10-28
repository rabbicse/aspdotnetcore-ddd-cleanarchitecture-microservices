using KYC.Application.UseCases.Customers.DTOs;
using KYC.Infrastructure.IntegrationTests.Fixtures;
using KYC.RedisCache.Infrastructure;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class RedisCacheTests(RedisFixture redis) : IClassFixture<RedisFixture>
{
    private ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CacheConnection"] = redis.ConnectionString,
                ["CacheOptions:AbsoluteExpirationInHours"] = "1",
                ["CacheOptions:SlidingExpirationInSeconds"] = "30"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCacheInfrastructureServices(configuration);

        return services.BuildServiceProvider();
    }

    private static string UniqueKey() => $"test:{Guid.NewGuid():N}";

    [Fact]
    public async Task The_factory_runs_once_and_later_calls_are_served_from_the_cache()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey();
        var factoryCalls = 0;

        Task<string> Factory()
        {
            factoryCalls++;
            return Task.FromResult("value");
        }

        var first = await cache.GetOrCreateAsync(key, Factory);
        var second = await cache.GetOrCreateAsync(key, Factory);

        Assert.Equal("value", first);
        Assert.Equal("value", second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task Removing_a_key_makes_the_next_call_run_the_factory_again()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey();
        var factoryCalls = 0;

        Task<string> Factory()
        {
            factoryCalls++;
            return Task.FromResult($"value {factoryCalls}");
        }

        await cache.GetOrCreateAsync(key, Factory);
        await cache.RemoveAsync(key);
        var afterRemoval = await cache.GetOrCreateAsync(key, Factory);

        Assert.Equal(2, factoryCalls);
        Assert.Equal("value 2", afterRemoval);
    }

    [Fact]
    public async Task A_null_result_is_not_cached()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey();
        var factoryCalls = 0;

        Task<string?> Factory()
        {
            factoryCalls++;
            return Task.FromResult<string?>(null);
        }

        await cache.GetOrCreateAsync(key, Factory);
        await cache.GetOrCreateAsync(key, Factory);

        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task A_customer_list_survives_the_round_trip_through_redis()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var key = UniqueKey();
        var id = Guid.NewGuid();
        IReadOnlyList<CustomerQueryModel> original = [new CustomerQueryModel(id, "Ada", "Lovelace", new DateTime(1990, 5, 17))];

        await cache.GetOrCreateAsync<CustomerQueryModel>(key, () => Task.FromResult(original));
        Func<Task<IReadOnlyList<CustomerQueryModel>>> mustNotRun =
            () => throw new InvalidOperationException("The value should have come from the cache.");
        var cached = await cache.GetOrCreateAsync<CustomerQueryModel>(key, mustNotRun);

        var customer = Assert.Single(cached);
        Assert.Equal(id, customer.Id);
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal(new DateTime(1990, 5, 17), customer.Dob);
    }
}
