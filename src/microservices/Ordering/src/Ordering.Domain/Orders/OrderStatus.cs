namespace Ordering.Domain.Orders;

/// <summary>Where an order is in its life cycle: Pending → Confirmed, or Pending/Confirmed → Cancelled.</summary>
public enum OrderStatus
{
    /// <summary>Placed; payment not yet processed.</summary>
    Pending,

    /// <summary>Payment accepted.</summary>
    Confirmed,

    /// <summary>Cancelled by the customer or because payment was declined.</summary>
    Cancelled
}
