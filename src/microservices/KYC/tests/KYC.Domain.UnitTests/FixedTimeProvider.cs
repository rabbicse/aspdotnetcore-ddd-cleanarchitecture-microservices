namespace KYC.Domain.UnitTests;

/// <summary>A clock that always reports the same instant, so date rules are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
