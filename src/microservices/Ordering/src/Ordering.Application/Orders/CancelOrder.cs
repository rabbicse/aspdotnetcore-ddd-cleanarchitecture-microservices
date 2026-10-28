using FluentValidation;
using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Ordering.Application.Abstractions;
using Ordering.Application.IntegrationEvents;
using Ordering.Domain.Orders;

namespace Ordering.Application.Orders;

/// <summary>Cancels an order that is not already cancelled.</summary>
public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<Result>;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(Order.MaxReasonLength);
    }
}

/// <summary>Cancels the order and records <see cref="OrderCancelledIntegrationEvent"/> in the outbox.</summary>
public sealed class CancelOrderCommandHandler(
    IOrderRepository orders,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.NotFound();
        }

        // Throws DomainException (HTTP 409) when the order is already cancelled.
        order.Cancel(command.Reason, timeProvider);
        outbox.Add(new OrderCancelledIntegrationEvent(
            order.Id, order.CustomerId, order.CancellationReason!, timeProvider.GetUtcNow().UtcDateTime));

        return await unitOfWork.SaveChangesAsync(cancellationToken)
            ? Result.Success()
            : Result.Error("The cancellation could not be saved.");
    }
}
