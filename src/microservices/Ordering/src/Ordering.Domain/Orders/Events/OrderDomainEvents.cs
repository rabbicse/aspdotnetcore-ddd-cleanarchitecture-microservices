using Mehedi.Core.SharedKernel;

namespace Ordering.Domain.Orders.Events;

/// <summary>An order was placed and is waiting for payment.</summary>
public sealed record OrderPlacedDomainEvent : BaseDomainEvent
{
    public OrderPlacedDomainEvent(Guid orderId, Guid customerId) : base(nameof(OrderPlacedDomainEvent), orderId)
    {
        OrderId = orderId;
        CustomerId = customerId;
    }

    public Guid OrderId { get; }

    public Guid CustomerId { get; }
}

/// <summary>The payment of an order was accepted.</summary>
public sealed record OrderConfirmedDomainEvent : BaseDomainEvent
{
    public OrderConfirmedDomainEvent(Guid orderId) : base(nameof(OrderConfirmedDomainEvent), orderId) => OrderId = orderId;

    public Guid OrderId { get; }
}

/// <summary>An order was cancelled.</summary>
public sealed record OrderCancelledDomainEvent : BaseDomainEvent
{
    public OrderCancelledDomainEvent(Guid orderId, string reason) : base(nameof(OrderCancelledDomainEvent), orderId)
    {
        OrderId = orderId;
        Reason = reason;
    }

    public Guid OrderId { get; }

    public string Reason { get; }
}
