using FluentValidation;
using KYC.Application.Behaviors;
using MediatR;

namespace KYC.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record Ping(string Name) : IRequest<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(ping => ping.Name).NotEmpty().WithMessage("Name is required.");
    }

    private sealed class SecondPingValidator : AbstractValidator<Ping>
    {
        public SecondPingValidator() => RuleFor(ping => ping.Name).MaximumLength(3).WithMessage("Name is too long.");
    }

    private static (RequestHandlerDelegate<string> Next, Func<int> Calls) CountingNext()
    {
        var calls = 0;
        return (() => { calls++; return Task.FromResult("handled"); }, () => calls);
    }

    [Fact]
    public async Task Without_validators_the_request_goes_straight_to_the_handler()
    {
        var behavior = new ValidationBehavior<Ping, string>([]);
        var (next, calls) = CountingNext();

        var response = await behavior.Handle(new Ping(""), next, CancellationToken.None);

        Assert.Equal("handled", response);
        Assert.Equal(1, calls());
    }

    [Fact]
    public async Task A_valid_request_reaches_the_handler()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);
        var (next, calls) = CountingNext();

        var response = await behavior.Handle(new Ping("Ada"), next, CancellationToken.None);

        Assert.Equal("handled", response);
        Assert.Equal(1, calls());
    }

    [Fact]
    public async Task An_invalid_request_throws_and_never_reaches_the_handler()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator()]);
        var (next, calls) = CountingNext();

        var error = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new Ping(""), next, CancellationToken.None));

        Assert.Equal(0, calls());
        var failure = Assert.Single(error.Errors);
        Assert.Equal("Name", failure.PropertyName);
        Assert.Equal("Name is required.", failure.ErrorMessage);
    }

    [Fact]
    public async Task Failures_from_every_validator_are_collected()
    {
        var behavior = new ValidationBehavior<Ping, string>([new PingValidator(), new SecondPingValidator()]);
        var (next, _) = CountingNext();

        // "" fails the first validator only; "Alexander" fails the second only. Use both in turn.
        var empty = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new Ping(""), next, CancellationToken.None));
        var tooLong = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new Ping("Alexander"), next, CancellationToken.None));

        Assert.Contains(empty.Errors, failure => failure.ErrorMessage == "Name is required.");
        Assert.Contains(tooLong.Errors, failure => failure.ErrorMessage == "Name is too long.");
    }
}
