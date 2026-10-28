using KYC.RedisCache.Infrastructure.Services;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.RedisCache.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "CacheConnection";

    /// <summary>
    /// Registers the Redis-backed <see cref="ICacheService"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <c>ConnectionStrings:CacheConnection</c> setting is missing.</exception>
    public static IServiceCollection AddCacheInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");

        services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.Name, binderOptions => binderOptions.BindNonPublicProperties = true)
            .Validate(
                options => options.AbsoluteExpirationInHours > 0 && options.SlidingExpirationInSeconds > 0,
                $"{CacheOptions.Name}:AbsoluteExpirationInHours and {CacheOptions.Name}:SlidingExpirationInSeconds must both be greater than zero.");

        return services
            .AddScoped<ICacheService, DistributedCacheService>()
            .AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.InstanceName = "KYC";
                redisOptions.Configuration = connectionString;
            });
    }
}
