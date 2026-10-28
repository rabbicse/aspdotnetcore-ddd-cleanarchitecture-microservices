using Mehedi.Core.SharedKernel;

namespace Ordering.Domain.Common;

/// <summary>An amount in a currency. Two values are equal when both parts are equal.</summary>
public sealed class Money : ValueObject
{
    /// <summary>Required by EF Core.</summary>
    private Money()
    {
        Currency = string.Empty;
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>The amount, rounded to 2 decimals.</summary>
    public decimal Amount { get; private set; }

    /// <summary>ISO 4217 code, upper case, for example <c>EUR</c>.</summary>
    public string Currency { get; private set; }

    /// <summary>Creates a non-negative amount.</summary>
    /// <exception cref="DomainException">The amount is negative or the currency is not a 3-letter code.</exception>
    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("An amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3 || !currency.Trim().All(char.IsAsciiLetter))
        {
            throw new DomainException("A currency is a 3-letter ISO 4217 code, for example EUR.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), currency.Trim().ToUpperInvariant());
    }

    /// <summary>Zero in <paramref name="currency"/>.</summary>
    public static Money Zero(string currency) => Of(0, currency);

    /// <summary>Adds two amounts of the same currency.</summary>
    /// <exception cref="DomainException">The currencies differ.</exception>
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new DomainException($"Cannot add {other.Currency} to {Currency}.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>Multiplies the amount, for example by a quantity.</summary>
    public Money Multiply(int factor) => Of(Amount * factor, Currency);

    public override string ToString() => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
