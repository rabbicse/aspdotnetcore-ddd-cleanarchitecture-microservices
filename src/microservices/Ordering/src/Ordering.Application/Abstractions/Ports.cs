using Mehedi.Core.SharedKernel;
using Ordering.Application.Orders;
using Ordering.Domain.Customers;
using Ordering.Domain.Orders;

namespace Ordering.Application.Abstractions;

/// <summary>Write-side repository of the <see cref="Order"/> aggregate.</summary>
public interface IOrderRepository : ICommandRepository<Order, Guid>
{
    /// <summary>Loads an order with its lines, tracked for changes; <c>null</c> when it does not exist.</summary>
    Task<Order?> FindAsync(Guid orderId, CancellationToken cancellationToken);
}

/// <summary>The local copies of KYC customers.</summary>
public interface IKnownCustomerRepository
{
    /// <summary>Loads a customer, tracked for changes; <c>null</c> when it is not known.</summary>
    Task<KnownCustomer?> FindAsync(Guid customerId, CancellationToken cancellationToken);

    /// <summary>Adds a customer; it is stored by the next save.</summary>
    void Add(KnownCustomer customer);
}

/// <summary>
/// Read-side queries. Ordering reads from the same database it writes to, through untracked projections
/// (a lighter form of CQRS than KYC's separate MongoDB read model).
/// </summary>
public interface IOrderQueries
{
    /// <summary>One order with its lines, or <c>null</c>.</summary>
    Task<OrderDetails?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>A customer's orders, newest first.</summary>
    Task<IReadOnlyList<OrderSummary>> GetCustomerOrdersAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>A replicated customer, or <c>null</c>.</summary>
    Task<KnownCustomerDetails?> GetKnownCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}

/// <summary>Takes payment for an order. The sample uses a simulated gateway; replace it with a real one.</summary>
public interface IPaymentGateway
{
    /// <summary>Charges <paramref name="amount"/> in <paramref name="currency"/> for an order.</summary>
    Task<PaymentResult> ChargeAsync(Guid orderId, decimal amount, string currency, CancellationToken cancellationToken);
}

/// <summary>The outcome of a charge.</summary>
/// <param name="Approved">Whether the payment went through.</param>
/// <param name="DeclineReason">Why it was declined.</param>
public sealed record PaymentResult(bool Approved, string? DeclineReason = null)
{
    public static PaymentResult Approve() => new(true);

    public static PaymentResult Decline(string reason) => new(false, reason);
}
