using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using FluentValidation;
using KYC.API.Http;
using KYC.Application.UseCases.Customers.Commands;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Queries;
using MediatR;
using Mehedi.Hangfire.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace KYC.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(IMediator mediator) : ControllerBase
{
    /// <summary>Registers a new customer and returns its id (HTTP 201).</summary>
    /// <response code="201">The customer was registered. The Location header points to the new resource.</response>
    /// <response code="400">The request is invalid. The body lists the failing fields.</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<CreatedCustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody, Required] CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);

        return result.ToActionResult(this, created =>
            CreatedAtAction(nameof(GetById), new { id = created.Id }, created));
    }

    /// <summary>
    /// Queues the registration as a background (Hangfire) job and returns immediately (HTTP 202).
    /// The customer appears in <c>GET /api/customers</c> once the job has run.
    /// </summary>
    /// <response code="202">The job was queued.</response>
    /// <response code="400">The request is invalid. Nothing was queued.</response>
    [HttpPost("async")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAsync(
        [FromBody, Required] CreateCustomerCommand command,
        [FromServices] IValidator<CreateCustomerCommand> validator,
        CancellationToken cancellationToken)
    {
        // Validate up front: a queued job cannot report a validation failure back to the caller.
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        mediator.Enqueue(nameof(CreateCustomerCommand), command);

        return Accepted();
    }

    /// <summary>Gets a customer by id.</summary>
    /// <response code="200">The customer.</response>
    /// <response code="404">No customer has this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerQueryModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Required] Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCustomerByIdQuery(id), cancellationToken);

        return result.ToActionResult(this);
    }

    /// <summary>Lists all customers.</summary>
    /// <response code="200">The customers (possibly an empty list).</response>
    [HttpGet]
    [ProducesResponseType<IEnumerable<CustomerQueryModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllCustomerQuery(), cancellationToken);

        return result.ToActionResult(this);
    }
}
