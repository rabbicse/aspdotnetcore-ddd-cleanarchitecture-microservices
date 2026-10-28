using System.Reflection;
using KYC.Domain.Aggregates.CustomerAggregate;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Core.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace KYC.Write.MsSql.Infrastructure.Persistence;

/// <summary>
/// The write model's EF Core context (SQL Server). Also the <see cref="IWriteDbContext"/> used by the
/// shared <c>CommandRepository</c> and <c>UnitOfWork</c>.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IWriteDbContext
{
    /// <summary>The customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <inheritdoc />
    public IQueryable<TEntity> Entities<TEntity>() where TEntity : class => Set<TEntity>();

    /// <inheritdoc />
    /// <remarks>Use <c>{0}</c>, <c>{1}</c>... placeholders for <paramref name="parameters"/>; they are sent as SQL parameters.</remarks>
    public async Task<IEnumerable<TEntity>> FromSqlRawAsync<TEntity>(string sql, params object[] parameters)
        where TEntity : class =>
        await Set<TEntity>().FromSqlRaw(sql, parameters).AsNoTracking().ToListAsync();

    /// <inheritdoc />
    /// <remarks><typeparamref name="TResponse"/> does not have to be an entity: its properties are matched to the result columns.</remarks>
    public async Task<IEnumerable<TResponse>> FromSqlRawAsync<TEntity, TResponse>(string sql, params object[] parameters)
        where TEntity : class
        where TResponse : class =>
        await Database.SqlQueryRaw<TResponse>(sql, parameters).ToListAsync();

    /// <inheritdoc />
    /// <remarks>
    /// Runs <c>SELECT * FROM functionName(@p0, @p1, ...)</c> with the public properties of <paramref name="parameters"/>
    /// as SQL parameters, in declaration order. <paramref name="functionName"/> is inserted as-is, so never pass user input.
    /// </remarks>
    public async Task<IEnumerable<T>> FromSqlFunctionAsync<T>(string functionName, object parameters) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        ArgumentNullException.ThrowIfNull(parameters);

        var values = parameters.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.GetValue(parameters))
            .ToArray();
        var placeholders = string.Join(", ", values.Select((_, index) => $"{{{index}}}"));

        return await Database.SqlQueryRaw<T>($"SELECT * FROM {functionName}({placeholders})", values!).ToListAsync();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations (EfConfigurations folder).
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Domain events live on the aggregates in memory only.
        modelBuilder.Ignore<BaseDomainEvent>();
    }
}
