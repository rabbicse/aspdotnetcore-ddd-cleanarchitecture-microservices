using KYC.Domain.Aggregates.CustomerAggregate.Events;
using KYC.Domain.ValueObjects;
using Mehedi.Core.SharedKernel;

namespace KYC.Domain.Aggregates.CustomerAggregate;

/// <summary>
/// Customer aggregate root. All invariants are enforced here, so an invalid
/// <see cref="Customer"/> can never be constructed or persisted.
/// </summary>
public class Customer : BaseEntity<Guid>, IAggregateRoot
{
    /// <summary>Maximum length of <see cref="FirstName"/> and <see cref="LastName"/>.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Earliest date of birth the domain accepts.</summary>
    public static readonly DateTime MinimumDateOfBirth = new(1900, 1, 1);

    /// <summary>Required by EF Core. Not usable from application code.</summary>
    protected Customer()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Address = Address.Empty;
    }

    /// <summary>
    /// Creates a customer.
    /// </summary>
    /// <param name="timeProvider">Clock used to validate the date of birth. Defaults to the system clock.</param>
    /// <exception cref="ArgumentException">A name is empty or longer than <see cref="MaxNameLength"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The date of birth is in the future or before <see cref="MinimumDateOfBirth"/>.</exception>
    public Customer(
        string firstName,
        string lastName,
        DateTime dob,
        Address? address = null,
        TimeProvider? timeProvider = null)
    {
        var today = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime.Date;

        FirstName = ValidateName(firstName, nameof(firstName));
        LastName = ValidateName(lastName, nameof(lastName));
        Dob = ValidateDateOfBirth(dob, today);
        Address = address ?? Address.Empty;
    }

    /// <summary>New customers get a random (version 4) id when they are constructed.</summary>
    protected override Guid GenerateNewId() => Guid.NewGuid();

    #region Properties
    /// <summary>Gets the first name of the customer.</summary>
    public string FirstName { get; private set; }

    /// <summary>Gets the last name of the customer.</summary>
    public string LastName { get; private set; }

    /// <summary>Gets the date of birth of the customer.</summary>
    public DateTime Dob { get; private set; }

    /// <summary>Gets the address (value object) of the customer.</summary>
    public Address Address { get; private set; }
    #endregion

    #region Behaviour
    /// <summary>Registers the customer and raises <see cref="CustomerCreatedDomainEvent"/>.</summary>
    public void Create() =>
        AddDomainEvent(new CustomerCreatedDomainEvent(Id, FirstName, LastName, Dob, Address));

    /// <summary>Changes the customer's details and raises <see cref="CustomerUpdatedDomainEvent"/>.</summary>
    public void Update(
        string firstName,
        string lastName,
        DateTime dob,
        Address? address = null,
        TimeProvider? timeProvider = null)
    {
        var today = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime.Date;

        // Validate everything before changing anything, so a rejected update leaves the customer untouched.
        var validFirstName = ValidateName(firstName, nameof(firstName));
        var validLastName = ValidateName(lastName, nameof(lastName));
        var validDob = ValidateDateOfBirth(dob, today);

        FirstName = validFirstName;
        LastName = validLastName;
        Dob = validDob;
        Address = address ?? Address.Empty;

        AddDomainEvent(new CustomerUpdatedDomainEvent(Id, FirstName, LastName, Dob, Address));
    }

    /// <summary>Marks the customer as deleted and raises <see cref="CustomerDeletedDomainEvent"/>.</summary>
    public void Delete() =>
        AddDomainEvent(new CustomerDeletedDomainEvent(Id, FirstName, LastName, Dob, Address));
    #endregion

    #region Invariants
    private static string ValidateName(string? value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new ArgumentException($"Must not be longer than {MaxNameLength} characters.", parameterName);
        }

        return trimmed;
    }

    private static DateTime ValidateDateOfBirth(DateTime dob, DateTime today)
    {
        if (dob.Date > today)
        {
            throw new ArgumentOutOfRangeException(nameof(dob), dob, "Date of birth cannot be in the future.");
        }

        if (dob.Date < MinimumDateOfBirth)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dob), dob, $"Date of birth cannot be before {MinimumDateOfBirth:yyyy-MM-dd}.");
        }

        return dob;
    }
    #endregion
}
