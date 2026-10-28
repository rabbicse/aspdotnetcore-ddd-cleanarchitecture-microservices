using KYC.Write.MsSql.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace KYC.Infrastructure.IntegrationTests.Fixtures;

/// <summary>One SQL Server container per test class, migrated with the real EF migrations.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options);
}
