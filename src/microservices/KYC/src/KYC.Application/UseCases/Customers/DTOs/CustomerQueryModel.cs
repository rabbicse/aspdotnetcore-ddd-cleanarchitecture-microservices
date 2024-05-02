using Mehedi.Core.SharedKernel;

namespace KYC.Application.UseCases.Customers.DTOs;

public class CustomerQueryModel : IQueryModel<Guid>
{
    public CustomerQueryModel(
        Guid id,
        string firstName,
        string lastName,
        DateTime dob)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Dob = dob;
    }

    private CustomerQueryModel()
    {
    }

    public Guid Id { get; private init; }
    public string FirstName { get; private init; }
    public string LastName { get; private init; }
    public DateTime Dob { get; private init; }

    public string FullName => (FirstName + " " + LastName).Trim();
}

