using System.Collections.Concurrent;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Domain.Aggregates.CustomerAggregate.Events;
using KYC.Domain.ValueObjects;
using KYC.Infrastructure.IntegrationTests.Fixtures;
using KYC.Write.MsSql.Infrastructure;
using MediatR;
using Mehedi.Application.SharedKernel.Services;
using Mehedi.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.Infrastructure.IntegrationTests;

/// <summary>Collects the domain events the unit of work dispatches.</summary>
public sealed class DispatchedEvents
{
    public ConcurrentBag<CustomerCreatedDomainEvent> Created { get; } = [];
}

public sealed class RecordingCreatedHandler(DispatchedEvents log) : INotificationHandler<CustomerCreatedDomainEvent>
{
    public Task Handle(CustomerCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        log.Created.Add(notification);
        return Task.CompletedTask;
    }
}

/// <summary>Stands in for EventStoreDB: remembers what the unit of work asked to append.</summary>
public sealed class RecordingEventStore : IEventStoreRepository
{
    public ConcurrentBag<EventStoreEvent> Stored { get; } = [];

    public Task StoreAsync(IEnumerable<EventStoreEvent> eventStores)
    {
        foreach (var stored in eventStores)
        {
            Stored.Add(stored);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}

[Trait("Category", "Integration")]
public sealed class SqlServerWriteSideTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:SqlConnection"] = sql.ConnectionString })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DispatchedEvents>();
        services.AddSingleton<RecordingEventStore>();
        services.AddSingleton<IEventStoreRepository>(provider => provider.GetRequiredService<RecordingEventStore>());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RecordingCreatedHandler>());
        services.AddWriteInfrastructureServices(configuration);

        return services.BuildServiceProvider();
    }

    private static Customer NewCustomer(string lastName = "Lovelace") =>
        new("Ada", lastName, new DateTime(1990, 5, 17),
            new Address("12 St James's Square", "London", "England", "United Kingdom", "SW1Y 4JH"));

    [Fact]
    public async Task A_saved_customer_can_be_read_back_with_its_address()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICustomerCommandRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var customer = NewCustomer();
        customer.Create();
        await repository.AddAsync(customer);
        var saved = await unitOfWork.SaveChangesAsync();

        Assert.True(saved);

        // Read through a brand-new context so nothing comes from the change tracker.
        await using var verify = sql.CreateContext();
        var stored = await verify.Customers.AsNoTracking().SingleAsync(c => c.Id == customer.Id);
        Assert.Equal("Ada", stored.FirstName);
        Assert.Equal("Lovelace", stored.LastName);
        Assert.Equal(new DateTime(1990, 5, 17), stored.Dob);
        Assert.Equal(customer.Address, stored.Address);
    }

    [Fact]
    public async Task Saving_dispatches_the_domain_events_and_clears_them_from_the_aggregate()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICustomerCommandRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var dispatched = provider.GetRequiredService<DispatchedEvents>();
        var eventStore = provider.GetRequiredService<RecordingEventStore>();

        var customer = NewCustomer("Hopper");
        customer.Create();
        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        var domainEvent = Assert.Single(dispatched.Created);
        Assert.Equal(customer.Id, domainEvent.Id);
        Assert.Equal("Hopper", domainEvent.LastName);
        Assert.Empty(customer.DomainEvents);

        // The same event is also appended to the event store, keyed by the aggregate id.
        var stored = Assert.Single(eventStore.Stored);
        Assert.Equal(customer.Id.ToString(), stored.AggregateId.ToString());
        Assert.False(string.IsNullOrWhiteSpace(stored.Data));
    }

    [Fact]
    public async Task A_customer_saved_without_an_address_is_stored_with_empty_address_fields()
    {
        using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICustomerCommandRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var customer = new Customer("Alan", "Turing", new DateTime(1985, 6, 23));
        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        await using var verify = sql.CreateContext();
        var stored = await verify.Customers.AsNoTracking().SingleAsync(c => c.Id == customer.Id);
        Assert.True(stored.Address.IsEmpty);
    }

    [Fact]
    public async Task The_same_customer_can_be_loaded_through_the_repository()
    {
        using var provider = BuildProvider();

        Guid id;
        await using (var writeScope = provider.CreateAsyncScope())
        {
            var customer = NewCustomer("Johnson");
            await writeScope.ServiceProvider.GetRequiredService<ICustomerCommandRepository>().AddAsync(customer);
            await writeScope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
            id = customer.Id;
        }

        await using var readScope = provider.CreateAsyncScope();
        var loaded = await readScope.ServiceProvider.GetRequiredService<ICustomerCommandRepository>().GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Johnson", loaded.LastName);
    }
}
