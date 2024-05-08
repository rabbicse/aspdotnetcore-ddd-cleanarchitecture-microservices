using AutoMapper;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Queries;
using KYC.Domain.Aggregates.CustomerAggregate.Events;
using MediatR;
using Mehedi.Application.SharedKernel.Extensions;
using Mehedi.Application.SharedKernel.Services;
using Mehedi.Read.NoSql.Infrastructure.Abstractions;
using Microsoft.Extensions.Logging;

namespace KYC.Read.Mongo.Infrastructure.EventHandlers;

public class CustomerEventHandler(
    IMapper mapper,
    ISynchronizeDb synchronizeDb,
    //ICacheService cacheService,
    ILogger<CustomerEventHandler> logger) : INotificationHandler<CustomerCreatedDomainEvent>
{
    private readonly ILogger<CustomerEventHandler> _logger = logger;
    private readonly ISynchronizeDb _synchronizeDb = synchronizeDb;
    private readonly IMapper _mapper = mapper;
    //private readonly ICacheService _cacheService = cacheService;
    public async Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        LogEvent(notification);

        try
        {
            var customerQueryModel = _mapper.Map<CustomerQueryModel>(notification);
            await _synchronizeDb.UpsertAsync(customerQueryModel, filter => filter.Id == customerQueryModel.Id);
            await ClearCacheAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when add event to read database! Details: {ex}");
        }
    }

    private async Task ClearCacheAsync(CustomerBaseDomainEvent @event)
    {
        var cacheKeys = new[] { nameof(GetAllCustomerQuery), $"{nameof(GetCustomerByIdQuery)}_{@event.Id}" };
        //await _cacheService.RemoveAsync(cacheKeys);
    }

    private void LogEvent<TEvent>(TEvent @event) where TEvent : CustomerBaseDomainEvent =>
        _logger.LogInformation("----- Triggering the event {EventName}, model: {EventModel}", typeof(TEvent).Name, @event.ToJson());
}
