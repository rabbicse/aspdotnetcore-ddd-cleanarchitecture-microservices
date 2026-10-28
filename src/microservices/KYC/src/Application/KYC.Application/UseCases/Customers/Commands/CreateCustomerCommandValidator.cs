using FluentValidation;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Domain.Aggregates.CustomerAggregate;

namespace KYC.Application.UseCases.Customers.Commands;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(Customer.MaxNameLength);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(Customer.MaxNameLength);

        RuleFor(command => command.Dob)
            .Must(dob => dob.Date <= timeProvider.GetUtcNow().UtcDateTime.Date)
            .WithMessage("Date of birth cannot be in the future.")
            .Must(dob => dob.Date >= Customer.MinimumDateOfBirth)
            .WithMessage($"Date of birth cannot be before {Customer.MinimumDateOfBirth:yyyy-MM-dd}.");

        When(command => command.CustomerAddress is not null, () =>
            RuleFor(command => command.CustomerAddress!).SetValidator(new AddressRequestValidator()));
    }
}

public class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public const int MaxLength = 200;

    public AddressRequestValidator()
    {
        RuleFor(address => address.Street).MaximumLength(MaxLength);
        RuleFor(address => address.City).MaximumLength(MaxLength);
        RuleFor(address => address.State).MaximumLength(MaxLength);
        RuleFor(address => address.Country).MaximumLength(MaxLength);
        RuleFor(address => address.ZipCode).MaximumLength(MaxLength);
    }
}
