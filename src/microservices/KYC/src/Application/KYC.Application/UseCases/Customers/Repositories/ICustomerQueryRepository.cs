using KYC.Application.UseCases.Customers.DTOs;
using Mehedi.Core.SharedKernel;

namespace KYC.Application.UseCases.Customers.Repositories;

/// <summary>Read-side repository of the customer read model.</summary>
public interface ICustomerQueryRepository : IQueryRepository<CustomerQueryModel, Guid>
{
    /// <summary>Returns every customer in the read model.</summary>
    Task<IEnumerable<CustomerQueryModel>> GetAllAsync();
}
