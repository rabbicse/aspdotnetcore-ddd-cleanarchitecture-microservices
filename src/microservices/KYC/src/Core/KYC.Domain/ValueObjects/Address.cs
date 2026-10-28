using Mehedi.Core.SharedKernel;

namespace KYC.Domain.ValueObjects;

/// <summary>
/// Address value object. Two addresses with the same components are equal.
/// Based on https://github.com/dotnet/eShop/blob/main/src/Ordering.Domain/AggregatesModel/OrderAggregate/Address.cs
/// </summary>
/// <remarks>
/// Every component is stored as a non-null string (<see cref="string.Empty"/> when unknown) so the
/// persisted shape stays stable and comparisons never have to deal with nulls.
/// </remarks>
public class Address : ValueObject
{
    /// <summary>An address with no information, used when a customer supplies none.</summary>
    public static Address Empty => new();

    /// <summary>Required by EF Core.</summary>
    public Address() { }

    public Address(string? street, string? city, string? state, string? country, string? zipcode)
    {
        Street = Normalize(street);
        City = Normalize(city);
        State = Normalize(state);
        Country = Normalize(country);
        ZipCode = Normalize(zipcode);
    }

    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;

    /// <summary>True when no component carries any information.</summary>
    public bool IsEmpty =>
        Street.Length == 0 && City.Length == 0 && State.Length == 0 && Country.Length == 0 && ZipCode.Length == 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
