using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Queries;
using KYC.Domain.Aggregates.CustomerAggregate.Events;
using MediatR;
using Mehedi.Application.SharedKernel.Services;
using Mehedi.Application.SharedKernel.Persistence;
using Microsoft.Extensions.Logging;

namespace KYC.Read.Mongo.Infrastructure.EventHandlers;

/// <summary>
/// Projects <see cref="CustomerCreatedDomainEvent"/> into the MongoDB read model and invalidates the
/// cached query results that the new customer makes stale.
/// </summary>
/// <remarks>
/// A projection failure is logged and swallowed: the write has already been committed, so failing the
/// request would be misleading. The read model can therefore lag behind the write model; see
/// docs/architecture.md ("Consistency") for the trade-off and the outbox option.
/// Personal data (names, date of birth) is deliberately NOT written to the logs.
/// </remarks>
public sealed class CustomerEventHandler(
    IReadDbContext readDb,
    ICacheService cacheService,
    ILogger<CustomerEventHandler> logger) : INotificationHandler<CustomerCreatedDomainEvent>
{
    public async Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Projecting {EventName} for customer {CustomerId}", nameof(CustomerCreatedDomainEvent), notification.Id);

        try
        {
            var queryModel = new CustomerQueryModel(
                notification.Id,
                notification.FirstName,
                notification.LastName,
                notification.Dob);

            await readDb.UpsertAsync(queryModel, filter => filter.Id == queryModel.Id);

            await cacheService.RemoveAsync(
                GetAllCustomerQueryHandler.CacheKey,
                GetCustomerByIdQueryHandler.CacheKeyFor(notification.Id));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex, "Failed to project {EventName} for customer {CustomerId} into the read database",
                nameof(CustomerCreatedDomainEvent), notification.Id);
        }
    }
}
