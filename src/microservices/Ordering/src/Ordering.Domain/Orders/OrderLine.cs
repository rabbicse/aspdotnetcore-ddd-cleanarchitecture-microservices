using Mehedi.Core.SharedKernel;
using Ordering.Domain.Common;

namespace Ordering.Domain.Orders;

/// <summary>One product on an order. Created only through <see cref="Order.Place"/>.</summary>
public sealed class OrderLine : BaseEntity<Guid>
{
    /// <summary>Maximum length of <see cref="ProductCode"/>.</summary>
    public const int MaxProductCodeLength = 50;

    /// <summary>Maximum length of <see cref="ProductName"/>.</summary>
    public const int MaxProductNameLength = 200;

    /// <summary>Required by EF Core.</summary>
    private OrderLine()
    {
        ProductCode = string.Empty;
        ProductName = string.Empty;
        UnitPrice = null!;
    }

    internal OrderLine(string productCode, string productName, Money unitPrice, int quantity)
    {
        ProductCode = productCode;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    /// <summary>The product's code (SKU).</summary>
    public string ProductCode { get; private set; }

    /// <summary>The product's name at the time of ordering.</summary>
    public string ProductName { get; private set; }

    /// <summary>The price of one unit at the time of ordering.</summary>
    public Money UnitPrice { get; private set; }

    /// <summary>How many units.</summary>
    public int Quantity { get; private set; }

    /// <summary><see cref="UnitPrice"/> × <see cref="Quantity"/>.</summary>
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    protected override Guid GenerateNewId() => Guid.NewGuid();
}

/// <summary>What the caller asks for when placing an order.</summary>
/// <param name="ProductCode">The product's code (SKU).</param>
/// <param name="ProductName">The product's name.</param>
/// <param name="UnitPrice">The price of one unit.</param>
/// <param name="Quantity">How many units; at least 1.</param>
public sealed record NewOrderLine(string ProductCode, string ProductName, Money UnitPrice, int Quantity);
