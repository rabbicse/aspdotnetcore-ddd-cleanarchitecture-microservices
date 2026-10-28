using Mehedi.Core.SharedKernel;
using Ordering.Domain.Common;
using Ordering.Domain.Orders.Events;

namespace Ordering.Domain.Orders;

/// <summary>
/// The order aggregate root. An order belongs to one customer, has 1 to <see cref="MaxLines"/> lines in a single
/// currency, and moves Pending → Confirmed, or Pending/Confirmed → Cancelled. Every rule lives here.
/// </summary>
public sealed class Order : BaseEntity<Guid>, IAggregateRoot
{
    /// <summary>The most lines one order may have.</summary>
    public const int MaxLines = 50;

    /// <summary>The most units of one product per line.</summary>
    public const int MaxQuantityPerLine = 1000;

    /// <summary>Maximum length of <see cref="CancellationReason"/>.</summary>
    public const int MaxReasonLength = 500;

    private readonly List<OrderLine> _lines = [];

    /// <summary>Required by EF Core.</summary>
    private Order()
    {
        Currency = string.Empty;
    }

    private Order(Guid customerId, string currency, DateTime placedAtUtc)
    {
        CustomerId = customerId;
        Currency = currency;
        PlacedAtUtc = placedAtUtc;
        Status = OrderStatus.Pending;
    }

    /// <summary>The customer (its id in the KYC service).</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Where the order is in its life cycle.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>The currency of every line and of <see cref="Total"/>.</summary>
    public string Currency { get; private set; }

    /// <summary>When the order was placed (UTC).</summary>
    public DateTime PlacedAtUtc { get; private set; }

    /// <summary>When payment was accepted (UTC).</summary>
    public DateTime? ConfirmedAtUtc { get; private set; }

    /// <summary>When the order was cancelled (UTC).</summary>
    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>Why the order was cancelled.</summary>
    public string? CancellationReason { get; private set; }

    /// <summary>The order lines.</summary>
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>The sum of the line totals.</summary>
    public Money Total => _lines.Aggregate(Money.Zero(Currency), (sum, line) => sum.Add(line.LineTotal));

    /// <summary>Places a new order and raises <see cref="OrderPlacedDomainEvent"/>.</summary>
    /// <exception cref="DomainException">A rule is violated (no lines, too many lines, mixed currencies, bad quantity...).</exception>
    public static Order Place(Guid customerId, IReadOnlyCollection<NewOrderLine> lines, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (customerId == Guid.Empty)
        {
            throw new DomainException("An order needs a customer.");
        }

        if (lines.Count == 0)
        {
            throw new DomainException("An order needs at least one line.");
        }

        if (lines.Count > MaxLines)
        {
            throw new DomainException($"An order can have at most {MaxLines} lines.");
        }

        var currency = lines.First().UnitPrice.Currency;
        if (lines.Any(line => line.UnitPrice.Currency != currency))
        {
            throw new DomainException("All lines of an order must use the same currency.");
        }

        var order = new Order(customerId, currency, timeProvider.GetUtcNow().UtcDateTime);
        foreach (var line in lines)
        {
            order._lines.Add(CreateLine(line));
        }

        order.AddDomainEvent(new OrderPlacedDomainEvent(order.Id, customerId));
        return order;
    }

    /// <summary>Marks the payment as accepted and raises <see cref="OrderConfirmedDomainEvent"/>.</summary>
    /// <exception cref="DomainException">The order is not pending.</exception>
    public void Confirm(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Only a pending order can be confirmed; this one is {Status}.");
        }

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        AddDomainEvent(new OrderConfirmedDomainEvent(Id));
    }

    /// <summary>Cancels the order and raises <see cref="OrderCancelledDomainEvent"/>.</summary>
    /// <exception cref="DomainException">The order is already cancelled, or no reason is given.</exception>
    public void Cancel(string reason, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("Say why the order is cancelled.");
        }

        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("The order is already cancelled.");
        }

        var trimmed = reason.Trim();
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        CancellationReason = trimmed.Length <= MaxReasonLength ? trimmed : trimmed[..MaxReasonLength];
        AddDomainEvent(new OrderCancelledDomainEvent(Id, CancellationReason));
    }

    protected override Guid GenerateNewId() => Guid.NewGuid();

    private static OrderLine CreateLine(NewOrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (string.IsNullOrWhiteSpace(line.ProductCode) || line.ProductCode.Trim().Length > OrderLine.MaxProductCodeLength)
        {
            throw new DomainException($"A product code is required and at most {OrderLine.MaxProductCodeLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(line.ProductName) || line.ProductName.Trim().Length > OrderLine.MaxProductNameLength)
        {
            throw new DomainException($"A product name is required and at most {OrderLine.MaxProductNameLength} characters.");
        }

        if (line.Quantity is < 1 or > MaxQuantityPerLine)
        {
            throw new DomainException($"A quantity is between 1 and {MaxQuantityPerLine}.");
        }

        return new OrderLine(line.ProductCode.Trim(), line.ProductName.Trim(), line.UnitPrice, line.Quantity);
    }
}
