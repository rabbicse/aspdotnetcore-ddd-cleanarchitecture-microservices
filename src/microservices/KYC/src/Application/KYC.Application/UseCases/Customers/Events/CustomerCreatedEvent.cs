using MediatR;
using Mehedi.EventBus;

namespace KYC.Application.UseCases.Customers.Events;

public record CustomerCreatedEvent : IntegrationEvent, INotification
{
    public CustomerCreatedEvent(Guid id, Guid customerId)
    {
        this.Id = id;
        this.CustomerId = customerId;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
}
