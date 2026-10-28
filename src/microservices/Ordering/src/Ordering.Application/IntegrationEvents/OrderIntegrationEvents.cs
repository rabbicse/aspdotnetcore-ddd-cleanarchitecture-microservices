using Mehedi.EventBus;

namespace Ordering.Application.IntegrationEvents;

// Events this service publishes (topic "ordering.orders"). They are the public contract of the service:
// add fields freely, but rename or remove one only under a new versioned name (.v2).
// All of an order's events share its id as partition key, so consumers see them in order.

/// <summary>An order was placed and is waiting for payment.</summary>
[IntegrationEventName("ordering.order-placed.v1")]
public sealed record OrderPlacedIntegrationEvent(Guid OrderId, Guid CustomerId, decimal Total, string Currency, DateTime PlacedAtUtc)
    : IntegrationEvent
{
    public override Guid GetPartitionKey() => OrderId;
}

/// <summary>The payment of an order was accepted.</summary>
[IntegrationEventName("ordering.order-confirmed.v1")]
public sealed record OrderConfirmedIntegrationEvent(Guid OrderId, Guid CustomerId, DateTime ConfirmedAtUtc) : IntegrationEvent
{
    public override Guid GetPartitionKey() => OrderId;
}

/// <summary>An order was cancelled.</summary>
[IntegrationEventName("ordering.order-cancelled.v1")]
public sealed record OrderCancelledIntegrationEvent(Guid OrderId, Guid CustomerId, string Reason, DateTime CancelledAtUtc)
    : IntegrationEvent
{
    public override Guid GetPartitionKey() => OrderId;
}
