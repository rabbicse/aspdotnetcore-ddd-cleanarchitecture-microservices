using System.Linq.Expressions;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Queries;
using KYC.Domain.Aggregates.CustomerAggregate.Events;
using KYC.Domain.ValueObjects;
using KYC.Read.Mongo.Infrastructure.EventHandlers;
using Mehedi.Application.SharedKernel.Services;
using Mehedi.Application.SharedKernel.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace KYC.Infrastructure.UnitTests;

public class CustomerEventHandlerTests
{
    private readonly IReadDbContext _synchronizeDb = Substitute.For<IReadDbContext>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly CustomerEventHandler _handler;

    public CustomerEventHandlerTests() =>
        _handler = new CustomerEventHandler(_synchronizeDb, _cache, NullLogger<CustomerEventHandler>.Instance);

    private static CustomerCreatedDomainEvent CreatedEvent(Guid id) =>
        new(id, "Ada", "Lovelace", new DateTime(1990, 5, 17), Address.Empty);

    [Fact]
    public async Task The_customer_is_projected_into_the_read_database()
    {
        var id = Guid.NewGuid();

        await _handler.Handle(CreatedEvent(id), CancellationToken.None);

        await _synchronizeDb.Received(1).UpsertAsync(
            Arg.Is<CustomerQueryModel>(model =>
                model.Id == id &&
                model.FirstName == "Ada" &&
                model.LastName == "Lovelace" &&
                model.Dob == new DateTime(1990, 5, 17)),
            Arg.Any<Expression<Func<CustomerQueryModel, bool>>>());
    }

    [Fact]
    public async Task The_upsert_filter_matches_on_the_customer_id()
    {
        var id = Guid.NewGuid();
        Expression<Func<CustomerQueryModel, bool>>? capturedFilter = null;
        _ = _synchronizeDb.UpsertAsync(
            Arg.Any<CustomerQueryModel>(),
            Arg.Do<Expression<Func<CustomerQueryModel, bool>>>(filter => capturedFilter = filter));

        await _handler.Handle(CreatedEvent(id), CancellationToken.None);

        Assert.NotNull(capturedFilter);
        var filter = capturedFilter.Compile();
        Assert.True(filter(new CustomerQueryModel(id, "Ada", "Lovelace", new DateTime(1990, 5, 17))));
        Assert.False(filter(new CustomerQueryModel(Guid.NewGuid(), "Ada", "Lovelace", new DateTime(1990, 5, 17))));
    }

    [Fact]
    public async Task The_cached_list_and_the_cached_customer_are_invalidated()
    {
        var id = Guid.NewGuid();

        await _handler.Handle(CreatedEvent(id), CancellationToken.None);

        await _cache.Received(1).RemoveAsync(
            GetAllCustomerQueryHandler.CacheKey,
            GetCustomerByIdQueryHandler.CacheKeyFor(id));
    }

    [Fact]
    public async Task A_projection_failure_is_swallowed_and_the_cache_is_left_alone()
    {
        _synchronizeDb.UpsertAsync(
                Arg.Any<CustomerQueryModel>(),
                Arg.Any<Expression<Func<CustomerQueryModel, bool>>>())
            .ThrowsAsync(new TimeoutException("mongo is down"));

        var exception = await Record.ExceptionAsync(
            () => _handler.Handle(CreatedEvent(Guid.NewGuid()), CancellationToken.None));

        Assert.Null(exception);
        await _cache.DidNotReceiveWithAnyArgs().RemoveAsync();
    }
}
