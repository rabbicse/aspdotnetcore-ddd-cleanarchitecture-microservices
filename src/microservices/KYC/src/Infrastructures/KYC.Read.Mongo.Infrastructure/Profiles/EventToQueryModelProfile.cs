using AutoMapper;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Domain.Aggregates.CustomerAggregate.Events;

namespace KYC.Read.Infrastructure.Profiles;

public class EventToQueryModelProfile : Profile
{
    public EventToQueryModelProfile()
    {
        CreateMap<CustomerCreatedDomainEvent, CustomerQueryModel>(MemberList.Destination)
            .ConstructUsing(@event => CreateCustomerQueryModel(@event));

        //CreateMap<CustomerUpdatedEvent, CustomerQueryModel>(MemberList.Destination)
        //    .ConstructUsing(@event => CreateCustomerQueryModel(@event));

        //CreateMap<CustomerDeletedEvent, CustomerQueryModel>(MemberList.Destination)
        //    .ConstructUsing(@event => CreateCustomerQueryModel(@event));
    }

    public override string ProfileName => nameof(EventToQueryModelProfile);

    private static CustomerQueryModel CreateCustomerQueryModel<TEvent>(TEvent @event) where TEvent : CustomerBaseDomainEvent =>
        new(@event.Id, @event.FirstName, @event.LastName, @event.Dob);
}
