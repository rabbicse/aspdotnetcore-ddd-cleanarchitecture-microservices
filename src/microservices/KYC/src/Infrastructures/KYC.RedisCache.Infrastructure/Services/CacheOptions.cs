namespace KYC.RedisCache.Infrastructure.Services;

public sealed class CacheOptions
{
    public const string Name = "CacheOptions";
    public int AbsoluteExpirationInHours { get; private init; }
    public int SlidingExpirationInSeconds { get; private init; }
}
