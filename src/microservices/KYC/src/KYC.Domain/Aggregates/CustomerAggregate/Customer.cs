using KYC.Domain.Aggregates.CustomerAggregate.Events;
using Mehedi.Core.SharedKernel;

namespace KYC.Domain.Aggregates.CustomerAggregate;

/// <summary>
/// Customer Aggregate
/// </summary>
public class Customer(string firstName, string lastName, DateTime dob) : BaseEntity, IAggregateRoot
{

    #region Properties
    /// <summary>
    /// Gets the first name of the customer.
    /// </summary>
    public string FirstName { get; private set; } = firstName;

    /// <summary>
    /// Gets the last name of the customer.
    /// </summary>
    public string LastName { get; private set; } = lastName;
    /// <summary>
    /// Gets the date of birth of the customer.
    /// </summary>
    public DateTime Dob { get; private set; } = dob;
    #endregion

    #region Domain Event(s)
    /// <summary>
    /// Creates the new customer.
    /// </summary>
    public void Create()
    {
        // TODO: other business
        AddDomainEvent(new CustomerCreatedDomainEvent(Id, FirstName, LastName, Dob));
    }
    /// <summary>
    /// Update existing customer
    /// </summary>
    public void Update()
    {
        // TODO: other business
        AddDomainEvent(new CustomerCreatedDomainEvent(Id, FirstName, LastName, Dob));
    }
    /// <summary>
    /// Deletes the customer.
    /// </summary>
    public void Delete()
    {
        // TODO: Other business
        AddDomainEvent(new CustomerDeletedDomainEvent(Id, FirstName, LastName, Dob));
    }
    #endregion
}
