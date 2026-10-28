using MediatR;
using Mehedi.Application.SharedKernel.BackgroundJobs;
using Mehedi.Application.SharedKernel.Responses;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Microsoft.Extensions.Logging;
using Ordering.Application.Abstractions;
using Ordering.Application.IntegrationEvents;
using Ordering.Domain.Orders;
using Ordering.Domain.Orders.Events;

namespace Ordering.Application.Orders;

/// <summary>
/// Runs after an order is committed (domain events are dispatched after the transaction) and queues its payment
/// as a background job, so placing an order returns at once.
/// </summary>
public sealed class OrderPlacedDomainEventHandler(IBackgroundJobQueue jobs) : INotificationHandler<OrderPlacedDomainEvent>
{
    public async Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken) =>
        await jobs.EnqueueAsync(new ProcessPaymentCommand(notification.OrderId), cancellationToken);
}

/// <summary>Charges a pending order and confirms or cancels it. Runs as a background job.</summary>
public sealed record ProcessPaymentCommand(Guid OrderId) : IRequest<Result>;

/// <summary>
/// Charges the order through <see cref="IPaymentGateway"/>, then confirms it or cancels it with the decline reason,
/// recording the matching integration event in the outbox. Safe to run twice: a non-pending order is left alone.
/// </summary>
public sealed partial class ProcessPaymentCommandHandler(
    IOrderRepository orders,
    IPaymentGateway payments,
    IIntegrationEventOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ProcessPaymentCommandHandler> logger) : IRequestHandler<ProcessPaymentCommand, Result>
{
    public async Task<Result> Handle(ProcessPaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.NotFound();
        }

        if (order.Status != OrderStatus.Pending)
        {
            // Already handled (a retried job, or the customer cancelled first).
            return Result.Success();
        }

        var payment = await payments.ChargeAsync(order.Id, order.Total.Amount, order.Currency, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (payment.Approved)
        {
            order.Confirm(timeProvider);
            outbox.Add(new OrderConfirmedIntegrationEvent(order.Id, order.CustomerId, now));
            LogConfirmed(logger, order.Id);
        }
        else
        {
            var reason = $"Payment declined: {payment.DeclineReason ?? "no reason given"}";
            order.Cancel(reason, timeProvider);
            outbox.Add(new OrderCancelledIntegrationEvent(order.Id, order.CustomerId, order.CancellationReason!, now));
            LogDeclined(logger, order.Id);
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken)
            ? Result.Success()
            : Result.Error("The payment outcome could not be saved.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} confirmed")]
    private static partial void LogConfirmed(ILogger logger, Guid orderId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} cancelled: payment declined")]
    private static partial void LogDeclined(ILogger logger, Guid orderId);
}
