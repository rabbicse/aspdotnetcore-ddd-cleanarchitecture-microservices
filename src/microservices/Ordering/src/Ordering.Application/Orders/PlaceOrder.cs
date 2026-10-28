using FluentValidation;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;
using Ordering.Application.IntegrationEvents;
using Ordering.Domain.Common;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders;

/// <summary>Places an order for a customer known from KYC.</summary>
public sealed record PlaceOrderCommand(Guid CustomerId, string Currency, IReadOnlyList<PlaceOrderLine> Lines)
    : IRequest<Result<PlacedOrderResponse>>;

/// <summary>One requested line.</summary>
public sealed record PlaceOrderLine(string ProductCode, string ProductName, decimal UnitPrice, int Quantity);

/// <summary>Input rules; the <see cref="Order"/> aggregate checks the business rules again.</summary>
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEmpty();
        RuleFor(command => command.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO 4217 code, for example EUR.");
        RuleFor(command => command.Lines).NotEmpty()
            .Must(lines => lines.Count <= Order.MaxLines).WithMessage($"An order can have at most {Order.MaxLines} lines.");
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductCode).NotEmpty().MaximumLength(OrderLine.MaxProductCodeLength);
            line.RuleFor(l => l.ProductName).NotEmpty().MaximumLength(OrderLine.MaxProductNameLength);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
            line.RuleFor(l => l.Quantity).InclusiveBetween(1, Order.MaxQuantityPerLine);
        });
    }
}

/// <summary>
/// Places the order and records <see cref="OrderPlacedIntegrationEvent"/> in the outbox, in one transaction.
/// Payment then happens in the background (see <see cref="OrderPlacedDomainEventHandler"/>).
/// </summary>
public sealed partial class PlaceOrderCommandHandler(
    IKnownCustomerRepository customers,
    IOrderRepository orders,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<PlaceOrderCommandHandler> logger) : IRequestHandler<PlaceOrderCommand, Result<PlacedOrderResponse>>
{
    public async Task<Result<PlacedOrderResponse>> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        // Ordering learns about customers from KYC's events; one that has not arrived yet is unknown here.
        if (await customers.FindAsync(command.CustomerId, cancellationToken) is null)
        {
            LogUnknownCustomer(logger, command.CustomerId);
            return Result<PlacedOrderResponse>.NotFound(
                $"Customer {command.CustomerId} is not known to the ordering service. " +
                "Register the customer in KYC first; it can take a moment to arrive.");
        }

        var lines = command.Lines
            .Select(line => new NewOrderLine(line.ProductCode, line.ProductName, Money.Of(line.UnitPrice, command.Currency), line.Quantity))
            .ToList();
        var order = Order.Place(command.CustomerId, lines, timeProvider);

        await orders.AddAsync(order);
        outbox.Add(new OrderPlacedIntegrationEvent(order.Id, order.CustomerId, order.Total.Amount, order.Currency, order.PlacedAtUtc));

        if (!await unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return Result<PlacedOrderResponse>.Error("The order could not be saved.");
        }

        LogPlaced(logger, order.Id, order.CustomerId);
        return Result<PlacedOrderResponse>.Success(new PlacedOrderResponse(order.Id, order.Status, order.Total.Amount, order.Currency));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} placed for customer {CustomerId}")]
    private static partial void LogPlaced(ILogger logger, Guid orderId, Guid customerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Order rejected: customer {CustomerId} is not known")]
    private static partial void LogUnknownCustomer(ILogger logger, Guid customerId);
}
