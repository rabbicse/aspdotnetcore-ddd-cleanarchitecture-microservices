using KYC.Domain.ValueObjects;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>Raised when a customer is deleted.</summary>
public sealed record CustomerDeletedDomainEvent : CustomerBaseDomainEvent
{
    public CustomerDeletedDomainEvent(Guid id, string firstName, string lastName, DateTime dob, Address address)
        : base(nameof(CustomerDeletedDomainEvent), id, firstName, lastName, dob, address)
    {
    }
}
