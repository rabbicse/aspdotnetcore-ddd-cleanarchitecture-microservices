namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>
/// CustomerDeletedDomainEvent
/// </summary>
/// <param name="id"></param>
/// <param name="firstName"></param>
/// <param name="lastName"></param>
/// <param name="dob"></param>
public class CustomerDeletedDomainEvent(Guid id, string firstName, string lastName, DateTime dob) : CustomerBaseDomainEvent(id, firstName, lastName, dob)
{
}
