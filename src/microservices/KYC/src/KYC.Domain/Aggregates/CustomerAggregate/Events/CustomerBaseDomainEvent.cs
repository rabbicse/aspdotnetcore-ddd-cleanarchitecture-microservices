using Mehedi.Core.SharedKernel;

namespace KYC.Domain.Aggregates.CustomerAggregate.Events;

/// <summary>
/// CustomerBaseDomainEvent
/// </summary>
/// <param name="id"></param>
/// <param name="firstName"></param>
/// <param name="lastName"></param>
/// <param name="dob"></param>
public class CustomerBaseDomainEvent(Guid id,
                                string firstName,
                                string lastName,
                                DateTime dob) : BaseDomainEvent
{
    public Guid Id { get; private init; } = id;
    public string FirstName { get; private init; } = firstName;
    public string LastName { get; private init; } = lastName;
    public DateTime Dob { get; private init; } = dob;
}
