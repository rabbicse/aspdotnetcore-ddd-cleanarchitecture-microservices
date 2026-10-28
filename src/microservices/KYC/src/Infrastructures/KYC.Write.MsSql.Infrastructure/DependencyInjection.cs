using KYC.Application.UseCases.Customers.Repositories;
using KYC.Write.MsSql.Infrastructure.Persistence;
using KYC.Write.MsSql.Infrastructure.Repositories;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Core.SharedKernel;
using Mehedi.Write.RDBMS.Infrastructure.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.Write.MsSql.Infrastructure;

/// <summary>
/// Dependency injection for the write side (SQL Server, EF Core).
/// </summary>
public static class DependencyInjection
{
    public const string ConnectionStringName = "SqlConnection";

    /// <summary>
    /// Registers <see cref="ApplicationDbContext"/>, the unit of work and the command repositories.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <c>ConnectionStrings:SqlConnection</c> setting is missing.</exception>
    public static IServiceCollection AddWriteInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");

        // NOTE: Do not enable EnableRetryOnFailure() here. The unit of work dispatches domain events from inside
        // the execution strategy, and a retry would run that block again with the events already cleared.
        return services
            .AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString))

            // The same scoped DbContext instance is exposed as the write-side abstraction.
            .AddScoped<IWriteDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>())

            // The unit of work commits the transaction, dispatches domain events and appends to the event store.
            .AddScoped<IUnitOfWork, UnitOfWork>()

            .AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();
    }
}
