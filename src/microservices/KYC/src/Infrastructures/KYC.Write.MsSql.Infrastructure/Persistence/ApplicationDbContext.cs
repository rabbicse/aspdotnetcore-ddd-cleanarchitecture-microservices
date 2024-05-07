using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Write.MsSql.Infrastructure.EfConfigurations;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace KYC.Write.MsSql.Infrastructure.Persistence;

/// <summary>
/// Represents the RDBMS database context for the application.
/// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
/// </summary>
/// <param name="options"></param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IWriteDbContext
{
    /// <summary>
    /// Gets or sets the set of customers in the database.
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types exposed in <see cref="DbContext.Set{TEntity}"/> and from
    /// DbSet properties on the derived context. This is a no-op if there are no configurations to apply.
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations from the customer configuration
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());

        // Apply entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Exclude BaseDomainEvent from being included in the DbContext
        modelBuilder.Ignore<BaseDomainEvent>();
    }
}

