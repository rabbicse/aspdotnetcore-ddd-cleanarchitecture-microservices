using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Read.Infrastructure.SharedKernel.Repositories;
using MongoDB.Driver;
using System.Linq;

namespace KYC.Read.Infrastructure.Repositories;

public class CustomerQueryRepository(IReadDbContext readDbContext)
    : QueryRepository<CustomerQueryModel, Guid>(readDbContext), ICustomerQueryRepository
{
    public async Task<IEnumerable<CustomerQueryModel?>> GetAllAsync()
    {
        var collections = await Collection;
        //return collections
        //    .Where(Builders<CustomerQueryModel>.Filter.Empty)
        //    .SortBy(customer => customer.FirstName)
        //    .ThenBy(customer => customer.DateOfBirth)
        //    .ToListAsync();

        return collections.ToList();
    }
}
