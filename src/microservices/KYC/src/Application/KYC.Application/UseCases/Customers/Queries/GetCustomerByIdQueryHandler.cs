using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Application.SharedKernel.Services;

namespace KYC.Application.UseCases.Customers.Queries;

public class GetCustomerByIdQueryHandler(
    ICustomerQueryRepository repository,
    ICacheService cacheService)
    : IRequestHandler<GetCustomerByIdQuery, Result<CustomerQueryModel>>
{
    /// <summary>Cache key for a single customer. Shared with the read-model projection that invalidates it.</summary>
    public static string CacheKeyFor(Guid id) => $"{nameof(GetCustomerByIdQuery)}_{id}";

    public async Task<Result<CustomerQueryModel>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Cache-aside: return the cached customer, or load it from the read database and cache it.
        var customer = await cacheService.GetOrCreateAsync(
            CacheKeyFor(request.Id),
            () => repository.GetByIdAsync(request.Id));

        return customer is null
            ? Result<CustomerQueryModel>.NotFound($"No customer found by Id: {request.Id}")
            : Result<CustomerQueryModel>.Success(customer);
    }
}
