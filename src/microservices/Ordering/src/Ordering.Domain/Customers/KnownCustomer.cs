using Mehedi.Core.SharedKernel;

namespace Ordering.Domain.Customers;

/// <summary>
/// A customer registered in the KYC service, replicated here from its <c>kyc.customer-created.v1</c> events.
/// Ordering keeps only what it needs, and only accepts orders from known customers.
/// </summary>
/// <remarks>The id is the KYC customer id, so replicating the same event twice updates one row.</remarks>
public sealed class KnownCustomer : BaseEntity<Guid>
{
    /// <summary>Maximum length of <see cref="FullName"/>.</summary>
    public const int MaxNameLength = 201;

    /// <summary>Required by EF Core.</summary>
    private KnownCustomer()
    {
        FullName = string.Empty;
    }

    /// <summary>Creates the local copy of customer <paramref name="customerId"/>.</summary>
    public KnownCustomer(Guid customerId, string fullName, DateTime registeredAtUtc) : base(customerId)
    {
        FullName = Normalize(fullName);
        RegisteredAtUtc = registeredAtUtc;
    }

    /// <summary>First and last name.</summary>
    public string FullName { get; private set; }

    /// <summary>When the customer registered in KYC (UTC).</summary>
    public DateTime RegisteredAtUtc { get; private set; }

    /// <summary>Applies a newer name from KYC.</summary>
    public void Rename(string fullName) => FullName = Normalize(fullName);

    protected override Guid GenerateNewId() => Guid.NewGuid();

    private static string Normalize(string fullName)
    {
        var trimmed = fullName?.Trim() ?? string.Empty;
        return trimmed.Length <= MaxNameLength ? trimmed : trimmed[..MaxNameLength];
    }
}
