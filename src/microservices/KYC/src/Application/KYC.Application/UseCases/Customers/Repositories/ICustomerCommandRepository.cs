using KYC.Domain.Aggregates.CustomerAggregate;
using Mehedi.Core.SharedKernel;

namespace KYC.Application.UseCases.Customers.Repositories;

/// <summary>Write-side repository of the <see cref="Customer"/> aggregate.</summary>
public interface ICustomerCommandRepository : ICommandRepository<Customer, Guid>;
