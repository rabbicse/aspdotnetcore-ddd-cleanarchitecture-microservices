using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Application.SharedKernel.Services;

namespace KYC.Application.UseCases.Customers.Queries;

public class GetAllCustomerQueryHandler(
    ICustomerQueryRepository repository,
    ICacheService cacheService)
    : IRequestHandler<GetAllCustomerQuery, Result<IEnumerable<CustomerQueryModel>>>
{
    /// <summary>Cache key for the full customer list. Shared with the read-model projection that invalidates it.</summary>
    public const string CacheKey = nameof(GetAllCustomerQuery);

    public async Task<Result<IEnumerable<CustomerQueryModel>>> Handle(
        GetAllCustomerQuery request,
        CancellationToken cancellationToken)
    {
        // Cache-aside: return the cached list, or load it from the read database and cache it.
        var customers = await cacheService.GetOrCreateAsync(CacheKey, repository.GetAllAsync);

        return Result<IEnumerable<CustomerQueryModel>>.Success(customers);
    }
}
