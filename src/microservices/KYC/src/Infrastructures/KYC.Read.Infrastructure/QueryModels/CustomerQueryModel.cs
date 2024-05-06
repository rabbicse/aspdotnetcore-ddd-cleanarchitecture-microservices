﻿using Mehedi.Core.SharedKernel;

namespace KYC.Read.Infrastructure.QueryModels;

public class CustomerQueryModel : IQueryModel<Guid>
{
    public CustomerQueryModel(
        Guid id,
        string firstName,
        string lastName,
        string gender,
        string email,
        DateTime dateOfBirth)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Gender = gender;
        Email = email;
        DateOfBirth = dateOfBirth;
    }

    public Guid Id { get; private init; }
    public string FirstName { get; private init; }
    public string LastName { get; private init; }
    public string Gender { get; private init; }
    public string Email { get; private init; }
    public DateTime DateOfBirth { get; private init; }

    public string FullName => (FirstName + " " + LastName).Trim();
}
