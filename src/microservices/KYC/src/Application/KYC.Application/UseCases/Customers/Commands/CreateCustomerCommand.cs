using System.ComponentModel.DataAnnotations;
using KYC.Application.UseCases.Customers.DTOs;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;

namespace KYC.Application.UseCases.Customers.Commands;

/// <summary>Registers a new customer.</summary>
public class CreateCustomerCommand : IRequest<Result<CreatedCustomerResponse>>
{
    /// <summary>Given name. Required, at most 100 characters.</summary>
    [Required]
    [MaxLength(100)]
    [DataType(DataType.Text)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Family name. Required, at most 100 characters.</summary>
    [Required]
    [MaxLength(100)]
    [DataType(DataType.Text)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Date of birth. Must not be in the future.</summary>
    [Required]
    [DataType(DataType.Date)]
    public DateTime Dob { get; set; }

    /// <summary>Optional postal address.</summary>
    public AddressRequest? CustomerAddress { get; set; }
}
