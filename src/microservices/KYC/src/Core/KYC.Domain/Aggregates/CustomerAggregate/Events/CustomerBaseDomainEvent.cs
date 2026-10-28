using KYC.Domain.ValueObjects;
using Mehedi.Core.SharedKernel;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>
/// The state every customer domain event carries. <see cref="BaseDomainEvent.AggregateId"/> is the customer id and
/// <see cref="BaseDomainEvent.MessageType"/> the concrete event name (used as the EventStoreDB event type).
/// </summary>
public abstract record CustomerBaseDomainEvent : BaseDomainEvent
{
    protected CustomerBaseDomainEvent(
        string messageType,
        Guid id,
        string firstName,
        string lastName,
        DateTime dob,
        Address address)
        : base(messageType, id)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Dob = dob;
        Address = address;
    }

    /// <summary>The customer id (same as <see cref="BaseDomainEvent.AggregateId"/>).</summary>
    public Guid Id { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public DateTime Dob { get; }

    public Address Address { get; }
}
