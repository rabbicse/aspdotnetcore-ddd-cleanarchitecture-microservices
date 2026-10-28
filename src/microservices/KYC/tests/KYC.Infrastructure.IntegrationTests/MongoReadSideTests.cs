using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Infrastructure.IntegrationTests.Fixtures;
using KYC.Read.Mongo.Infrastructure;
using Mehedi.Read.NoSql.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.Infrastructure.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class MongoReadSideTests(MongoFixture mongo) : IClassFixture<MongoFixture>
{
    private ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:NoSqlConnection"] = mongo.ConnectionString })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddReadInfrastructureServices(configuration);

        return services.BuildServiceProvider();
    }

    private static CustomerQueryModel NewModel(Guid id, string firstName = "Ada") =>
        new(id, firstName, "Lovelace", new DateTime(1990, 5, 17));

    [Fact]
    public async Task An_upserted_customer_can_be_found_by_id()
    {
        using var provider = BuildProvider();
        var synchronizeDb = provider.GetRequiredService<ISynchronizeDb>();
        var id = Guid.NewGuid();
        var model = NewModel(id);

        await synchronizeDb.UpsertAsync(model, filter => filter.Id == model.Id);

        await using var scope = provider.CreateAsyncScope();
        var found = await scope.ServiceProvider.GetRequiredService<ICustomerQueryRepository>().GetByIdAsync(id);

        Assert.NotNull(found);
        Assert.Equal(id, found.Id);
        Assert.Equal("Ada", found.FirstName);
        Assert.Equal("Lovelace", found.LastName);
        Assert.Equal(new DateTime(1990, 5, 17), found.Dob);
    }

    [Fact]
    public async Task Upserting_the_same_customer_twice_keeps_a_single_document()
    {
        using var provider = BuildProvider();
        var synchronizeDb = provider.GetRequiredService<ISynchronizeDb>();
        var id = Guid.NewGuid();

        await synchronizeDb.UpsertAsync(NewModel(id, "Ada"), filter => filter.Id == id);
        await synchronizeDb.UpsertAsync(NewModel(id, "Augusta"), filter => filter.Id == id);

        await using var scope = provider.CreateAsyncScope();
        var all = await scope.ServiceProvider.GetRequiredService<ICustomerQueryRepository>().GetAllAsync();

        var matching = Assert.Single(all.Where(customer => customer?.Id == id));
        Assert.Equal("Augusta", matching!.FirstName);
    }

    [Fact]
    public async Task Customers_that_share_a_first_name_can_coexist()
    {
        using var provider = BuildProvider();
        var synchronizeDb = provider.GetRequiredService<ISynchronizeDb>();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        await synchronizeDb.UpsertAsync(NewModel(first, "Sam"), filter => filter.Id == first);
        await synchronizeDb.UpsertAsync(NewModel(second, "Sam"), filter => filter.Id == second);

        await using var scope = provider.CreateAsyncScope();
        var all = (await scope.ServiceProvider.GetRequiredService<ICustomerQueryRepository>().GetAllAsync()).ToList();

        Assert.Contains(all, customer => customer?.Id == first);
        Assert.Contains(all, customer => customer?.Id == second);
    }

    [Fact]
    public async Task An_unknown_id_is_not_found()
    {
        using var provider = BuildProvider();

        await using var scope = provider.CreateAsyncScope();
        var found = await scope.ServiceProvider.GetRequiredService<ICustomerQueryRepository>().GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }
}
