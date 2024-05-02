using KYC.RedisCache.Infrastructure.Services;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.RedisCache.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCacheInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("CacheConnection");
        services.AddOptions<CacheOptions>()
            .BindConfiguration(CacheOptions.Name, binderOptions => binderOptions.BindNonPublicProperties = true);
        return services.AddScoped<ICacheService, DistributedCacheService>()
            .AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.InstanceName = "KYC";
                redisOptions.Configuration = connectionString;
            });
    }
}
