using KYC.Application.UnitTests.Support;
using KYC.Application.UseCases.Customers.Commands;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Events;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Domain.Aggregates.CustomerAggregate.Events;
using Mehedi.Application.SharedKernel.Enums;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace KYC.Application.UnitTests;

public class CreateCustomerCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly ICustomerCommandRepository _repository = Substitute.For<ICustomerCommandRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventProducer _producer = Substitute.For<IEventProducer>();
    private readonly CreateCustomerCommandHandler _handler;

    /// <summary>The customer the handler handed to the repository, if any.</summary>
    private Customer? _addedCustomer;

    public CreateCustomerCommandHandlerTests()
    {
        _repository.AddAsync(Arg.Any<Customer>()).Returns(call =>
        {
            _addedCustomer = call.Arg<Customer>();
            return Task.FromResult(_addedCustomer);
        });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);
        _producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>()).Returns(true);

        _handler = new CreateCustomerCommandHandler(
            _repository,
            _unitOfWork,
            _producer,
            NullLogger<CreateCustomerCommandHandler>.Instance,
            new FixedTimeProvider(Now));
    }

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

    [Fact]
    public async Task A_valid_command_registers_the_customer_and_returns_its_id()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(_addedCustomer);
        Assert.Equal(_addedCustomer.Id, result.Value.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task The_customer_is_built_from_the_command()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(_addedCustomer);
        Assert.Equal("Ada", _addedCustomer.FirstName);
        Assert.Equal("Lovelace", _addedCustomer.LastName);
        Assert.Equal(new DateTime(1990, 5, 17), _addedCustomer.Dob);
        Assert.Equal("London", _addedCustomer.Address.City);
        Assert.Equal("SW1Y 4JH", _addedCustomer.Address.ZipCode);
    }

    [Fact]
    public async Task A_command_without_an_address_registers_a_customer_with_an_empty_address()
    {
        var command = ValidCommand();
        command.CustomerAddress = null;

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(_addedCustomer);
        Assert.True(_addedCustomer.Address.IsEmpty);
    }

    [Fact]
    public async Task The_customer_carries_a_created_domain_event_for_the_unit_of_work_to_dispatch()
    {
        await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.NotNull(_addedCustomer);
        var domainEvent = Assert.IsType<CustomerCreatedDomainEvent>(Assert.Single(_addedCustomer.DomainEvents));
        Assert.Equal(_addedCustomer.Id, domainEvent.Id);
    }

    [Fact]
    public async Task The_integration_event_is_published_after_the_changes_are_saved()
    {
        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Received.InOrder(() =>
        {
            _repository.AddAsync(Arg.Any<Customer>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>());
        });

        await _producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEvent>(@event => @event is CustomerCreatedEvent && ((CustomerCreatedEvent)@event).CustomerId == result.Value.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_saving_fails_the_result_is_an_error_and_nothing_is_published()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.NotEmpty(result.Errors);
        await _producer.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }

    [Fact]
    public async Task When_the_broker_throws_the_registration_still_succeeds()
    {
        _producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("broker is down"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task When_the_broker_does_not_persist_the_event_the_registration_still_succeeds()
    {
        _producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Cancellation_while_publishing_is_not_swallowed()
    {
        _producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(ValidCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task A_command_that_breaks_a_domain_rule_throws_and_persists_nothing()
    {
        // Normally the validation pipeline rejects this first; the domain is the last line of defence.
        var command = ValidCommand();
        command.Dob = Now.UtcDateTime.Date.AddDays(1);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _handler.Handle(command, CancellationToken.None));

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await _producer.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
    }
}
