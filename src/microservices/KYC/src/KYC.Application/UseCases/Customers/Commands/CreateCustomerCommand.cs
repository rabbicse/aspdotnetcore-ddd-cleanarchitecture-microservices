using KYC.Application.UseCases.Customers.DTOs;
using KYC.Domain.ValueObjects;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using System.ComponentModel.DataAnnotations;

namespace KYC.Application.UseCases.Customers.Commands;

public class CreateCustomerCommand : IRequest<Result<CreatedCustomerResponse>>
{
    [Required]
    [MaxLength(100)]
    [DataType(DataType.Text)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    [DataType(DataType.Text)]
    public string LastName { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Dob { get; set; }
    public Address? CustomerAddress { get; set; }
}
