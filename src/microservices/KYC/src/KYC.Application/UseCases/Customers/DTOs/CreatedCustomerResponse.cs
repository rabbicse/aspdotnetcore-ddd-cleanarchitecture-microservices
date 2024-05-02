using Mehedi.Application.SharedKernel.Interfaces;

namespace KYC.Application.UseCases.Customers.DTOs;


public class CreatedCustomerResponse(Guid id) : IResponse
{
    public Guid Id { get; } = id;
}

