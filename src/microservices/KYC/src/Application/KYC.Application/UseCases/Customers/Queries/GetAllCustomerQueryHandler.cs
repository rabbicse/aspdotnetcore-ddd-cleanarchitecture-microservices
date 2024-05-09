using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Application.SharedKernel.Services;

namespace KYC.Application.UseCases.Customers.Queries;

public class GetAllCustomerQueryHandler(
    ICustomerQueryRepository repository,
    ICacheService cacheService
    )
    : IRequestHandler<GetAllCustomerQuery, Result<IEnumerable<CustomerQueryModel>>>
{
    private const string CacheKey = nameof(GetAllCustomerQuery);
    private readonly ICacheService _cacheService = cacheService;
    private readonly ICustomerQueryRepository _readOnlyRepository = repository;

    public async Task<Result<IEnumerable<CustomerQueryModel>>> Handle(
          GetAllCustomerQuery request,
          CancellationToken cancellationToken)
    {
        // This method will either return the cached data associated with the CacheKey
        // or create it by calling the GetAllAsync method.
        return Result<IEnumerable<CustomerQueryModel>>.Success(
            await _cacheService.GetOrCreateAsync(CacheKey, _readOnlyRepository.GetAllAsync));
    }
}
