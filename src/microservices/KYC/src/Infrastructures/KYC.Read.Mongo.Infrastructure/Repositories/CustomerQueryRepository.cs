using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Read.Infrastructure.SharedKernel.Repositories;

namespace KYC.Read.Mongo.Infrastructure.Repositories;

/// <summary>Reads customers from the MongoDB read model.</summary>
public class CustomerQueryRepository(IReadDbContext readDbContext)
    : QueryRepository<CustomerQueryModel, Guid>(readDbContext), ICustomerQueryRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<CustomerQueryModel>> GetAllAsync()
    {
        var (_, customers) = await GetAllCollectionAsync();
        return customers;
    }
}
