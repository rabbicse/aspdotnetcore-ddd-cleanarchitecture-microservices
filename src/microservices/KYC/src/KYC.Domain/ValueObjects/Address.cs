using Mehedi.Core.SharedKernel;

namespace KYC.Domain.ValueObjects;

/// <summary>
/// Address ValueObject
/// source: https://github.com/dotnet/eShop/blob/main/src/Ordering.Domain/AggregatesModel/OrderAggregate/Address.cs
/// </summary>
/// <param name="street"></param>
/// <param name="city"></param>
/// <param name="state"></param>
/// <param name="country"></param>
/// <param name="zipcode"></param>
public class Address(string street, string city, string state, string country, string zipcode) : ValueObject
{
    public string Street { get; private set; } = street;
    public string City { get; private set; } = city;
    public string State { get; private set; } = state;
    public string Country { get; private set; } = country;
    public string ZipCode { get; private set; } = zipcode;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Using a yield return statement to return each element one at a time
        yield return Street;
        yield return City;
        yield return State;
        yield return Country;
        yield return ZipCode;
    }
}

