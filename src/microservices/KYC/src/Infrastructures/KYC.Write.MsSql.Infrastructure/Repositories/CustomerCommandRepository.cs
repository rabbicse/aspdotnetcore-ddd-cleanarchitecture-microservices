using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Write.RDBMS.Infrastructure.Abstractions.Repositories;

namespace KYC.Write.MsSql.Infrastructure.Repositories;

/// <summary>
/// Represents a repository for performing command operations on customer entities.
/// Initializes a new instance of the <see cref="CustomerCommandRepository"/> class with the specified <see cref="IWriteDbContext"/>.
/// </summary>
public class CustomerCommandRepository(IWriteDbContext writeDbContext) 
    : CommandRepository<Customer, Guid>(writeDbContext), ICustomerCommandRepository
{
}
