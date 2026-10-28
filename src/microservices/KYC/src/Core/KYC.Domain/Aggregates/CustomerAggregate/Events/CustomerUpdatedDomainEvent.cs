using KYC.Domain.ValueObjects;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>Raised when a customer is updated.</summary>
public sealed record CustomerUpdatedDomainEvent : CustomerBaseDomainEvent
{
    public CustomerUpdatedDomainEvent(Guid id, string firstName, string lastName, DateTime dob, Address address)
        : base(nameof(CustomerUpdatedDomainEvent), id, firstName, lastName, dob, address)
    {
    }
}
