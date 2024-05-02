namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>
/// CustomerCreatedDomainEvent
/// </summary>
/// <param name="id"></param>
/// <param name="firstName"></param>
/// <param name="lastName"></param>
/// <param name="dob"></param>
public class CustomerCreatedDomainEvent(Guid id, string firstName, string lastName, DateTime dob): CustomerBaseDomainEvent(id, firstName, lastName, dob)
{
}
