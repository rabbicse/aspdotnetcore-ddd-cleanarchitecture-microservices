using KYC.Application.UnitTests.Support;
using KYC.Application.UseCases.Customers.Commands;
using KYC.Application.UseCases.Customers.DTOs;

namespace KYC.Application.UnitTests;

public class CreateCustomerCommandValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly CreateCustomerCommandValidator _validator = new(new FixedTimeProvider(Now));

    private static CreateCustomerCommand ValidCommand() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Dob = new DateTime(1990, 5, 17),
        CustomerAddress = new AddressRequest
        {
            Street = "12 St James's Square",
            City = "London",
            State = "England",
            Country = "United Kingdom",
            ZipCode = "SW1Y 4JH"
        }
    };

    private string[] FailingProperties(CreateCustomerCommand command) =>
        _validator.Validate(command).Errors.Select(error => error.PropertyName).Distinct().ToArray();

    [Fact]
    public void A_valid_command_passes()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    [Fact]
    public void The_address_is_optional()
    {
        var command = ValidCommand();
        command.CustomerAddress = null;

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_first_name_fails(string firstName)
    {
        var command = ValidCommand();
        command.FirstName = firstName;

        Assert.Equal(new[] { nameof(CreateCustomerCommand.FirstName) }, FailingProperties(command));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_last_name_fails(string lastName)
    {
        var command = ValidCommand();
        command.LastName = lastName;

        Assert.Equal(new[] { nameof(CreateCustomerCommand.LastName) }, FailingProperties(command));
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void The_name_length_limit_is_100_characters(int length, bool expectedValid)
    {
        var command = ValidCommand();
        command.FirstName = new string('a', length);

        Assert.Equal(expectedValid, _validator.Validate(command).IsValid);
    }

    [Fact]
    public void A_date_of_birth_in_the_future_fails()
    {
        var command = ValidCommand();
        command.Dob = Now.UtcDateTime.Date.AddDays(1);

        Assert.Equal(new[] { nameof(CreateCustomerCommand.Dob) }, FailingProperties(command));
    }

    [Fact]
    public void A_date_of_birth_of_today_passes()
    {
        var command = ValidCommand();
        command.Dob = Now.UtcDateTime.Date;

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void A_date_of_birth_before_1900_fails()
    {
        var command = ValidCommand();
        command.Dob = new DateTime(1899, 12, 31);

        Assert.Equal(new[] { nameof(CreateCustomerCommand.Dob) }, FailingProperties(command));
    }

    [Fact]
    public void An_unset_date_of_birth_fails()
    {
        var command = ValidCommand();
        command.Dob = default;

        Assert.Equal(new[] { nameof(CreateCustomerCommand.Dob) }, FailingProperties(command));
    }

    [Fact]
    public void An_address_field_that_is_too_long_fails_with_a_nested_property_name()
    {
        var command = ValidCommand();
        command.CustomerAddress!.Street = new string('x', AddressRequestValidator.MaxLength + 1);

        Assert.Equal(new[] { "CustomerAddress.Street" }, FailingProperties(command));
    }

    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        var command = ValidCommand();
        command.FirstName = "";
        command.LastName = "";
        command.Dob = Now.UtcDateTime.Date.AddDays(1);

        var failing = FailingProperties(command);

        Assert.Contains(nameof(CreateCustomerCommand.FirstName), failing);
        Assert.Contains(nameof(CreateCustomerCommand.LastName), failing);
        Assert.Contains(nameof(CreateCustomerCommand.Dob), failing);
    }
}
