using KYC.Application.UseCases.Customers.DTOs;
using Mehedi.Application.SharedKernel.Persistence;

namespace KYC.Application.UseCases.Customers.Repositories;

public interface ICustomerQueryRepository : IQueryRepository<CustomerQueryModel, Guid>
{
    Task<IEnumerable<CustomerQueryModel>> GetAllAsync();
}
