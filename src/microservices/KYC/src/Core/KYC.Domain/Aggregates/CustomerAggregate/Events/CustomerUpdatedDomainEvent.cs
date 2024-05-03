using KYC.Domain.ValueObjects;
using Mehedi.Core.SharedKernel;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>
/// CustomerUpdatedDomainEvent
/// </summary>
/// <param name="id"></param>
/// <param name="firstName"></param>
/// <param name="lastName"></param>
/// <param name="dob"></param>
/// <param name="address"></param>
public class CustomerUpdatedDomainEvent(
    Guid id, 
    string firstName, 
    string lastName, 
    DateTime dob,
    Address address) 
    : CustomerBaseDomainEvent(id, firstName, lastName, dob, address)
{
}
