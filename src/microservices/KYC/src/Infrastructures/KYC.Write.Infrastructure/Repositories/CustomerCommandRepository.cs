using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Write.Infrastructure.SharedKernel.Repositories;

namespace KYC.Write.Infrastructure.Repositories;

public class CustomerCommandRepository(IWriteDbContext writeDbContext) : CommandRepository<Customer, Guid>(writeDbContext), ICustomerCommandRepository
{
}
