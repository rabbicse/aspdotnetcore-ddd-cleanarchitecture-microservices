using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Events;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Domain.ValueObjects;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Microsoft.Extensions.Logging;

namespace KYC.Application.UseCases.Customers.Commands;

/// <summary>
/// Creates a customer: persists the aggregate (which triggers the read-model projection through
/// its domain events) and then announces the new customer to other services.
/// </summary>
/// <remarks>
/// Input is validated by <see cref="Behaviors.ValidationBehavior{TRequest, TResponse}"/> before this
/// handler runs. Publishing the integration event happens AFTER the transaction committed, so a
/// broker outage is logged but does not fail the request (see docs/adr/0003-integration-event-publishing.md).
/// </remarks>
public sealed class CreateCustomerCommandHandler(
    ICustomerCommandRepository repository,
    IUnitOfWork unitOfWork,
    IEventProducer eventProducer,
    ILogger<CreateCustomerCommandHandler> logger,
    TimeProvider timeProvider)
    : IRequestHandler<CreateCustomerCommand, Result<CreatedCustomerResponse>>
{
    public async Task<Result<CreatedCustomerResponse>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var address = new Address(
            street: request.CustomerAddress?.Street,
            city: request.CustomerAddress?.City,
            state: request.CustomerAddress?.State,
            country: request.CustomerAddress?.Country,
            zipcode: request.CustomerAddress?.ZipCode);

        var customer = new Customer(request.FirstName, request.LastName, request.Dob, address, timeProvider);

        // Raises CustomerCreatedDomainEvent; it is dispatched by the unit of work after the commit.
        customer.Create();

        _ = await repository.AddAsync(customer);

        var saved = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saved)
        {
            logger.LogError("Customer {CustomerId} could not be saved.", customer.Id);
            return Result<CreatedCustomerResponse>.Error("The customer could not be saved.");
        }

        await PublishIntegrationEventAsync(customer.Id, cancellationToken);

        return Result<CreatedCustomerResponse>.Success(
            new CreatedCustomerResponse(customer.Id), "Successfully registered!");
    }

    private async Task PublishIntegrationEventAsync(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var published = await eventProducer.PublishAsync(
                new CustomerCreatedEvent(Guid.NewGuid(), customerId), cancellationToken);

            if (!published)
            {
                logger.LogWarning(
                    "The CustomerCreated integration event for customer {CustomerId} was not persisted by the broker.",
                    customerId);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The customer is already committed; failing the request now would invite duplicate registrations on retry.
            logger.LogError(
                ex, "Publishing the CustomerCreated integration event for customer {CustomerId} failed.", customerId);
        }
    }
}
