using FluentValidation;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Application.SharedKernel.Services;

namespace KYC.Application.UseCases.Customers.Queries;

//public class GetCustomerByIdQueryHandler(
//    //IValidator<GetCustomerByIdQuery> validator,
//    ICustomerQueryRepository repository
//    //ICacheService cacheService
//    ) : IRequestHandler<GetCustomerByIdQuery, Result<CustomerQueryModel>>
//{
//    //private readonly ICacheService _cacheService = cacheService;
//    private readonly ICustomerQueryRepository _repository = repository;
//    //private readonly IValidator<GetCustomerByIdQuery> _validator = validator;

//    public async Task<Result<CustomerQueryModel>> Handle(
//        GetCustomerByIdQuery request,
//        CancellationToken cancellationToken)
//    {
//        // Validating the request.
//        //var validationResult = await _validator.ValidateAsync(request, cancellationToken);
//        //if (!validationResult.IsValid)
//        //{
//        //    // Returns the result with validation errors.
//        //    //return Result<CustomerQueryModel>.Invalid(validationResult.AsErrors());
//        //    return Result<CustomerQueryModel>.Invalid(new Mehedi.Application.SharedKernel.Exceptions.ValidationError());
//        //}

//        // Creating a cache key using the query name and the customer ID.
//        var cacheKey = $"{nameof(GetCustomerByIdQuery)}_{request.Id}";

//        // Getting the customer from the cache service. If not found, fetches it from the repository.
//        // The customer will be stored in the cache service for future queries.
//        //var customer = await _cacheService.GetOrCreateAsync(cacheKey, () => _repository.GetByIdAsync(request.Id));

//        // If the customer is null, returns a result indicating that no customer was found.
//        // Otherwise, returns a successful result with the customer.
//        //return customer == null
//        //    ? Result<CustomerQueryModel>.NotFound($"No customer found by Id: {request.Id}")
//        //    : Result<CustomerQueryModel>.Success(customer);
//        return Result<CustomerQueryModel>.NotFound($"No customer found by Id: {request.Id}");
//    }
//}
