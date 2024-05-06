using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Domain.ValueObjects;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Core.SharedKernel;

namespace KYC.Application.UseCases.Customers.Commands
{
    public class CreateCustomerCommandHandler(
        //IValidator<CreateCustomerCommand> validator,
        ICustomerCommandRepository repository,
        IUnitOfWork unitOfWork
        //IEventProducer eventProducer
        ) : IRequestHandler<CreateCustomerCommand, Result<CreatedCustomerResponse>>
    {
        private readonly ICustomerCommandRepository _repository = repository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        //private readonly IEventProducer _eventProducer = eventProducer;
        //private readonly IValidator<CreateCustomerCommand> _validator = validator;

        public async Task<Result<CreatedCustomerResponse>> Handle(
            CreateCustomerCommand request,
            CancellationToken cancellationToken)
        {
            // Validating the request.
            //var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            //if (!validationResult.IsValid)
            //{
            //    // Return the result with validation errors.
            //    //return Result<CreatedCustomerResponse>.Invalid(validationResult.AsErrors());
            //    return Result<CreatedCustomerResponse>.Invalid(new Mehedi.Application.SharedKernel.Exceptions.ValidationError());
            //}

            // Instantiating the Email value object.
            //var email = Email.Create(request.Email).Value;

            // Checking if a customer with the email address already exists.
            //if (await _repository.ExistsByEmailAsync(email))
            //    return Result<CreatedCustomerResponse>.Error("The provided email address is already in use.");

            // Creating an instance of the customer entity.
            // When instantiated, the "CustomerCreatedEvent" will be created.
            var address = new Address(
                street: "New Airport Road",
                city: "Dhaka",
                state: "Dhaka",
                country: "Bangladesh",
                zipcode: "1205");
            var customer = new Customer(request.FirstName,
                request.LastName,
                request.Dob,
                address);
            customer.Create();

            // Adding the entity to the repository.
            _ = await _repository.AddAsync(customer);

            // Saving changes to the database and triggering domain events.
            var success = await _unitOfWork.SaveChangesAsync(cancellationToken);

            //// If persistence success and domain events processed successfully then publish events
            //if (success) 
            //{
            //    var @event = new CustomerCreatedEvent(Guid.NewGuid(), customer.Id);
            //    await _eventProducer.PublishAsync(@event);
            //}

            // Returning the ID and success message.
            return Result<CreatedCustomerResponse>.Success(
                new CreatedCustomerResponse(customer.Id), "Successfully registered!");
        }
    }
}
