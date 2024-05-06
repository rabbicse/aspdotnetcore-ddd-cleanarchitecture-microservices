using FluentValidation;

namespace KYC.Application.UseCases.Customers.Commands;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(command => command.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.LastName)
            .NotEmpty()
            .MaximumLength(100);

        //RuleFor(command => command.Email)
        //    .NotEmpty()
        //    .MaximumLength(254)
        //    .EmailAddress();
    }
}
