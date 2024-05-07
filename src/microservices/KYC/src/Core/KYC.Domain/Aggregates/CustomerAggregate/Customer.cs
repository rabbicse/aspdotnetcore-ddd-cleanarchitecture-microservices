﻿using KYC.Domain.Aggregates.CustomerAggregate.Events;
using KYC.Domain.ValueObjects;
using Mehedi.Core.SharedKernel;

namespace KYC.Domain.Aggregates.CustomerAggregate;

/// <summary>
/// Customer Aggregate
/// </summary>
public class Customer 
    : BaseEntity, IAggregateRoot
{
    public Customer() { }

    public Customer(string firstName, string lastName, DateTime dob, Address address) 
    {
        FirstName = firstName;
        LastName = lastName;
        Dob = dob;
        Address = address;
    }

    #region Properties
    /// <summary>
    /// Gets the first name of the customer.
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// Gets the last name of the customer.
    /// </summary>
    public string LastName { get; private set; }
    /// <summary>
    /// Gets the date of birth of the customer.
    /// </summary>
    public DateTime Dob { get; private set; }
    /// <summary>
    /// Get the address (ValueObject) of a customer
    /// </summary>
    public Address Address { get; private set; }
    #endregion

    #region Domain Event(s)
    /// <summary>
    /// Creates the new customer.
    /// </summary>
    public void Create()
    {
        // TODO: other business
        AddDomainEvent(new CustomerCreatedDomainEvent(Id, FirstName, LastName, Dob, Address));
    }
    /// <summary>
    /// Update existing customer
    /// </summary>
    public void Update()
    {
        // TODO: other business
        AddDomainEvent(new CustomerUpdatedDomainEvent(Id, FirstName, LastName, Dob, Address));
    }
    /// <summary>
    /// Deletes the customer.
    /// </summary>
    public void Delete()
    {
        // TODO: Other business
        AddDomainEvent(new CustomerDeletedDomainEvent(Id, FirstName, LastName, Dob, Address));
    }
    #endregion
}
