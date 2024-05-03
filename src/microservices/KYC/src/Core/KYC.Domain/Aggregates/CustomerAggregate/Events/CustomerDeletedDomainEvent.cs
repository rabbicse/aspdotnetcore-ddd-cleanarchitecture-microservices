using KYC.Domain.ValueObjects;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>
/// CustomerDeletedDomainEvent
/// </summary>
/// <param name="id"></param>
/// <param name="firstName"></param>
/// <param name="lastName"></param>
/// <param name="dob"></param>
/// <param name="address"></param>
public class CustomerDeletedDomainEvent(
    Guid id, 
    string firstName, 
    string lastName, 
    DateTime dob,
    Address address) 
    : CustomerBaseDomainEvent(id, firstName, lastName, dob, address)
{
}
