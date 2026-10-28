using KYC.Domain.ValueObjects;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>Raised when a customer is created.</summary>
public sealed record CustomerCreatedDomainEvent : CustomerBaseDomainEvent
{
    public CustomerCreatedDomainEvent(Guid id, string firstName, string lastName, DateTime dob, Address address)
        : base(nameof(CustomerCreatedDomainEvent), id, firstName, lastName, dob, address)
    {
    }
}
