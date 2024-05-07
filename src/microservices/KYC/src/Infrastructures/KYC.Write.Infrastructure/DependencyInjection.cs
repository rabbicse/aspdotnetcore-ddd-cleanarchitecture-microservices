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
/// Provides extension methods for configuring dependency injection services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services required for write operations to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The collection of services to add the infrastructure services to.</param>
    /// <param name="config">The configuration containing connection string information.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddWriteInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        string? connectionString = config.GetConnectionString("SqlConnection");
        // For SQLServer Connection
        return services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    // Any additional configuration for SQL Server options can be applied here if needed               
                });
        })

        // Register the ApplicationDbContext as a scoped service implementing IWriteDbContext
        .AddScoped<IWriteDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>())

        // Register the UnitOfWork as a scoped service implementing IUnitOfWork
        .AddScoped<IUnitOfWork, UnitOfWork>()

        //services.AddScoped(typeof(ICommandRepository<,>), typeof(CommandRepository<,>));

        // Register the CustomerCommandRepository as a scoped service implementing ICustomerCommandRepository
        .AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();
    }
}

