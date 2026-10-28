using KYC.EventStore.EventStoreDB.Infrastructure;
using KYC.RedisCache.Infrastructure;
using KYC.RedisCache.Infrastructure.Services;
using KYC.Write.MsSql.Infrastructure;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KYC.Infrastructure.UnitTests;

/// <summary>Misconfiguration must fail fast with a message naming the missing setting.</summary>
public class DependencyInjectionTests
{
    private static IConfiguration Config(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(setting => setting.Key, setting => (string?)setting.Value))
            .Build();

    [Fact]
    public void The_write_side_requires_a_sql_connection_string()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddWriteInfrastructureServices(Config()));

        Assert.Contains("SqlConnection", error.Message);
    }

    [Fact]
    public void The_cache_requires_a_cache_connection_string()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddCacheInfrastructureServices(Config()));

        Assert.Contains("CacheConnection", error.Message);
    }

    [Fact]
    public void The_event_store_requires_a_connection_string()
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddEventStoreInfrastructureServices(Config()));

        Assert.Contains("EventStoreDbConnection", error.Message);
    }

    [Fact]
    public void The_cache_service_is_registered_when_configured()
    {
        var services = new ServiceCollection();

        services.AddCacheInfrastructureServices(Config(("ConnectionStrings:CacheConnection", "localhost:6379")));

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ICacheService));
    }

    [Fact]
    public void Cache_options_are_bound_from_configuration()
    {
        var services = new ServiceCollection();
        services.AddCacheInfrastructureServices(Config(
            ("ConnectionStrings:CacheConnection", "localhost:6379"),
            ("CacheOptions:AbsoluteExpirationInHours", "3"),
            ("CacheOptions:SlidingExpirationInSeconds", "45")));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CacheOptions>>().Value;

        Assert.Equal(3, options.AbsoluteExpirationInHours);
        Assert.Equal(45, options.SlidingExpirationInSeconds);
    }

    [Fact]
    public void Missing_cache_options_are_rejected_instead_of_silently_defaulting_to_zero()
    {
        var services = new ServiceCollection();
        services.AddCacheInfrastructureServices(Config(("ConnectionStrings:CacheConnection", "localhost:6379")));

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<CacheOptions>>().Value);
    }
}
