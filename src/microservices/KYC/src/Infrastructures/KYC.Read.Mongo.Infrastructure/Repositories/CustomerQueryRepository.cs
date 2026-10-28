using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using Mehedi.Application.SharedKernel.Persistence;

namespace KYC.Read.Mongo.Infrastructure.Repositories;

/// <summary>Reads customers from the MongoDB read model.</summary>
public class CustomerQueryRepository(IReadDbContext readDbContext) : ICustomerQueryRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<CustomerQueryModel>> GetAllAsync()
    {
        var (_, customers) = await readDbContext.GetAllCollectionAsync<CustomerQueryModel>();
        return customers;
    }

    /// <inheritdoc />
    public Task<(long, IEnumerable<CustomerQueryModel>)> GetAllCollectionAsync() =>
        readDbContext.GetAllCollectionAsync<CustomerQueryModel>();

    /// <inheritdoc />
    public Task<(long, IEnumerable<CustomerQueryModel>)> GetCollectionAsync(int pageNumber = 1, int pageSize = 100) =>
        readDbContext.GetCollectionAsync<CustomerQueryModel>(pageNumber, pageSize);

    /// <inheritdoc />
    public Task<CustomerQueryModel?> GetByIdAsync(Guid id) =>
        readDbContext.GetByIdAsync<CustomerQueryModel, Guid>(id);
}
