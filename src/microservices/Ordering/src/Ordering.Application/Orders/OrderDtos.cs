using Ordering.Domain.Orders;

namespace Ordering.Application.Orders;

/// <summary>Returned when an order is placed.</summary>
public sealed record PlacedOrderResponse(Guid OrderId, OrderStatus Status, decimal Total, string Currency);

/// <summary>An order with its lines.</summary>
public sealed record OrderDetails(
    Guid OrderId,
    Guid CustomerId,
    OrderStatus Status,
    string Currency,
    decimal Total,
    DateTime PlacedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    IReadOnlyList<OrderLineDetails> Lines);

/// <summary>One line of an order.</summary>
public sealed record OrderLineDetails(string ProductCode, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

/// <summary>An order in a list.</summary>
public sealed record OrderSummary(Guid OrderId, OrderStatus Status, string Currency, decimal Total, int LineCount, DateTime PlacedAtUtc);

/// <summary>A customer as Ordering knows it.</summary>
public sealed record KnownCustomerDetails(Guid CustomerId, string FullName, DateTime RegisteredAtUtc);
