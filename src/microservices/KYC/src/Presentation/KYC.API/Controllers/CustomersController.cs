﻿using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using KYC.Application.UseCases.Customers.Commands;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Queries;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Hangfire.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace KYC.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class CustomersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    ////////////////////////
    // POST: /api/customers
    ////////////////////////

    /// <summary>
    /// Register a new customer.
    /// </summary>
    /// <response code="200">Returns the Id of the new client.</response>
    /// <response code="400">Returns list of errors if the request is invalid.</response>
    /// <response code="500">When an unexpected internal error occurs on the server.</response>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    //[ProducesResponseType(typeof(ApiResponse<CreatedCustomerResponse>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult Create([FromBody][Required] CreateCustomerCommand command)
    {
        //await _mediator.Send(command);
        _mediator.Enqueue(nameof(CreateCustomerCommand), command);
        return Ok("Customer Command Request Accepted");
    }

    ///////////////////////
    // PUT: /api/customers
    //////////////////////

    ///// <summary>
    ///// Updates an existing client.
    ///// </summary>
    ///// <response code="200">Returns the response with the success message.</response>
    ///// <response code="400">Returns list of errors if the request is invalid.</response>
    ///// <response code="404">When no client is found by the given Id.</response>
    ///// <response code="500">When an unexpected internal error occurs on the server.</response>
    //[HttpPut]
    //[Consumes(MediaTypeNames.Application.Json)]
    //[Produces(MediaTypeNames.Application.Json)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> Update([FromBody][Required] UpdateCustomerCommand command) =>
    //    (await _mediator.Send(command)).ToActionResult();

    ////////////////////////////////
    //// DELETE: /api/customers/{id}
    ////////////////////////////////

    ///// <summary>
    ///// Deletes the client by Id.
    ///// </summary>
    ///// <response code="200">Returns the response with the success message.</response>
    ///// <response code="400">Returns list of errors if the request is invalid.</response>
    ///// <response code="404">When no client is found by the given Id.</response>
    ///// <response code="500">When an unexpected internal error occurs on the server.</response>
    //[HttpDelete("{id:guid}")]
    //[Consumes(MediaTypeNames.Application.Json)]
    //[Produces(MediaTypeNames.Application.Json)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> Delete([Required] Guid id) =>
    //    (await _mediator.Send(new DeleteCustomerCommand(id))).ToActionResult();

    /////////////////////////////
    //// GET: /api/customers/{id}
    /////////////////////////////

    ///// <summary>
    ///// Gets the client by Id.
    ///// </summary>
    ///// <response code="200">Returns the client.</response>
    ///// <response code="400">Returns list of errors if the request is invalid.</response>
    ///// <response code="404">When no client is found by the given Id.</response>
    ///// <response code="500">When an unexpected internal error occurs on the server.</response>
    //[HttpGet("{id:guid}")]
    //[Consumes(MediaTypeNames.Application.Json)]
    //[Produces(MediaTypeNames.Application.Json)]
    //[ProducesResponseType(typeof(ApiResponse<CustomerQueryModel>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    //public async Task<IActionResult> GetById([Required] Guid id) =>
    //    (await _mediator.Send(new GetCustomerByIdQuery(id))).ToActionResult();

    ////////////////////////
    //// GET: /api/customers
    ////////////////////////

    ///// <summary>
    ///// Gets a list of all customers.
    ///// </summary>
    ///// <response code="200">Returns the list of clients.</response>
    ///// <response code="500">When an unexpected internal error occurs on the server.</response>
    [HttpGet]
    //[Consumes(MediaTypeNames.Application.Json)]
    //[Produces(MediaTypeNames.Application.Json)]
    //[ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerQueryModel>>), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllCustomerQuery());

        return Ok(result);
    }
}
