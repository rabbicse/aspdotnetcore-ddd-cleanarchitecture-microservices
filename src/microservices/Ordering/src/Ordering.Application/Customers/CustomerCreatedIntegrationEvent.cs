using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;
using Ordering.Domain.Customers;

namespace Ordering.Application.Customers;

/// <summary>
/// Ordering's copy of the contract KYC publishes on topic <c>kyc.customers</c>. Only the wire name and the
/// JSON shape are shared; the two services never reference each other's assemblies.
/// </summary>
[IntegrationEventName("kyc.customer-created.v1")]
public sealed record CustomerCreatedIntegrationEvent(Guid CustomerId, string FirstName, string LastName, DateTime RegisteredAtUtc)
    : IntegrationEvent;

/// <summary>
/// Replicates a new KYC customer into <see cref="KnownCustomer"/>. Kafka delivers at least once, so the handler is
/// idempotent: a repeated event updates the same row instead of failing.
/// </summary>
public sealed partial class CustomerCreatedIntegrationEventHandler(
    IKnownCustomerRepository customers,
    IUnitOfWork unitOfWork,
    ILogger<CustomerCreatedIntegrationEventHandler> logger) : IIntegrationEventHandler<CustomerCreatedIntegrationEvent>
{
    public async Task HandleAsync(CustomerCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var fullName = $"{@event.FirstName} {@event.LastName}".Trim();
        var existing = await customers.FindAsync(@event.CustomerId, cancellationToken);

        if (existing is null)
        {
            customers.Add(new KnownCustomer(@event.CustomerId, fullName, @event.RegisteredAtUtc));
        }
        else
        {
            existing.Rename(fullName);
        }

        // Throwing lets the Kafka consumer retry and, eventually, dead-letter the message.
        if (!await unitOfWork.SaveChangesAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Customer {@event.CustomerId} could not be saved.");
        }

        LogReplicated(logger, @event.CustomerId, existing is null);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Customer {CustomerId} replicated from KYC (new: {IsNew})")]
    private static partial void LogReplicated(ILogger logger, Guid customerId, bool isNew);
}
