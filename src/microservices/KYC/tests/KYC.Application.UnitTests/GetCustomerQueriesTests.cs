using KYC.Application.UnitTests.Support;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Queries;
using KYC.Application.UseCases.Customers.Repositories;
using Mehedi.Application.SharedKernel.Enums;
using NSubstitute;

namespace KYC.Application.UnitTests;

public class GetCustomerByIdQueryHandlerTests
{
    private readonly ICustomerQueryRepository _repository = Substitute.For<ICustomerQueryRepository>();
    private readonly PassThroughCacheService _cache = new();
    private readonly GetCustomerByIdQueryHandler _handler;

    public GetCustomerByIdQueryHandlerTests() => _handler = new GetCustomerByIdQueryHandler(_repository, _cache);

    [Fact]
    public async Task An_existing_customer_is_returned()
    {
        var id = Guid.NewGuid();
        var model = new CustomerQueryModel(id, "Ada", "Lovelace", new DateTime(1990, 5, 17));
        _repository.GetByIdAsync(id).Returns(model);

        var result = await _handler.Handle(new GetCustomerByIdQuery(id), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Same(model, result.Value);
        Assert.Equal("Ada Lovelace", result.Value.FullName);
    }

    [Fact]
    public async Task An_unknown_customer_is_reported_as_not_found()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id).Returns(Task.FromResult<CustomerQueryModel>(null!));

        var result = await _handler.Handle(new GetCustomerByIdQuery(id), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Contains(result.Errors, error => error.Contains(id.ToString()));
    }

    [Fact]
    public async Task The_customer_is_cached_under_a_key_derived_from_its_id()
    {
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id).Returns(new CustomerQueryModel(id, "Ada", "Lovelace", new DateTime(1990, 5, 17)));

        await _handler.Handle(new GetCustomerByIdQuery(id), CancellationToken.None);

        Assert.Equal(new[] { GetCustomerByIdQueryHandler.CacheKeyFor(id) }, _cache.RequestedKeys);
    }

    [Fact]
    public void Different_customers_use_different_cache_keys()
    {
        Assert.NotEqual(
            GetCustomerByIdQueryHandler.CacheKeyFor(Guid.NewGuid()),
            GetCustomerByIdQueryHandler.CacheKeyFor(Guid.NewGuid()));
    }
}

public class GetAllCustomerQueryHandlerTests
{
    private readonly ICustomerQueryRepository _repository = Substitute.For<ICustomerQueryRepository>();
    private readonly PassThroughCacheService _cache = new();
    private readonly GetAllCustomerQueryHandler _handler;

    public GetAllCustomerQueryHandlerTests() => _handler = new GetAllCustomerQueryHandler(_repository, _cache);

    [Fact]
    public async Task All_customers_are_returned()
    {
        var customers = new[]
        {
            new CustomerQueryModel(Guid.NewGuid(), "Ada", "Lovelace", new DateTime(1990, 5, 17)),
            new CustomerQueryModel(Guid.NewGuid(), "Grace", "Hopper", new DateTime(1985, 12, 9))
        };
        _repository.GetAllAsync().Returns(Task.FromResult<IEnumerable<CustomerQueryModel>>(customers));

        var result = await _handler.Handle(new GetAllCustomerQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(customers, result.Value);
    }

    [Fact]
    public async Task No_customers_is_an_empty_successful_result()
    {
        _repository.GetAllAsync().Returns(Task.FromResult<IEnumerable<CustomerQueryModel>>([]));

        var result = await _handler.Handle(new GetAllCustomerQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task The_list_is_cached_under_the_shared_list_key()
    {
        _repository.GetAllAsync().Returns(Task.FromResult<IEnumerable<CustomerQueryModel>>([]));

        await _handler.Handle(new GetAllCustomerQuery(), CancellationToken.None);

        Assert.Equal(new[] { GetAllCustomerQueryHandler.CacheKey }, _cache.RequestedKeys);
    }
}
