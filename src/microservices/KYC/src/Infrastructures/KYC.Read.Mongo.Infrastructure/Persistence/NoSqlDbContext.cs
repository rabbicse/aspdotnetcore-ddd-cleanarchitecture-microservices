using System.Linq.Expressions;
using System.Text.RegularExpressions;
using KYC.Application.UseCases.Customers.DTOs;
using Mehedi.Application.SharedKernel.Extensions;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Core.SharedKernel;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Polly;
using Polly.Retry;

namespace KYC.Read.Mongo.Infrastructure.Persistence;

/// <summary>
/// MongoDB implementation of <see cref="IReadDbContext"/>: one collection per query model type, named after the type.
/// Writes are retried with exponential back-off and jitter.
/// </summary>
public sealed class NoSqlDbContext : IReadDbContext
{
    private const string DatabaseName = "KYC";
    private const int RetryCount = 2;
    private static readonly ReplaceOptions UpsertOptions = new() { IsUpsert = true };

    private readonly IMongoDatabase _database;
    private readonly ILogger<NoSqlDbContext> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    /// <summary>Connects to <paramref name="connectionString"/> (the client pools connections; register as a singleton).</summary>
    public NoSqlDbContext(string connectionString, ILogger<NoSqlDbContext> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _database = new MongoClient(connectionString).GetDatabase(DatabaseName);
        _logger = logger;
        _retryPolicy = CreateRetryPolicy(logger);
    }

    /// <summary>The collection that stores <typeparamref name="TQueryModel"/>.</summary>
    public IMongoCollection<TQueryModel> GetCollection<TQueryModel>() where TQueryModel : IQueryModel =>
        _database.GetCollection<TQueryModel>(typeof(TQueryModel).Name);

    #region Writes

    /// <inheritdoc />
    public Task UpsertAsync<TQueryModel>(TQueryModel queryModel, Expression<Func<TQueryModel, bool>> upsertFilter)
        where TQueryModel : IQueryModel =>
        _retryPolicy.ExecuteAsync(() => GetCollection<TQueryModel>().ReplaceOneAsync(upsertFilter, queryModel, UpsertOptions));

    /// <inheritdoc />
    /// <remarks>Each model is matched on its <c>_id</c>; models that are not <see cref="IQueryModel{TKey}"/> are inserted.</remarks>
    public async Task UpsertAsync<TQueryModel>(IEnumerable<TQueryModel> queryModels) where TQueryModel : IQueryModel
    {
        ArgumentNullException.ThrowIfNull(queryModels);

        var writes = queryModels
            .Select(model => (WriteModel<TQueryModel>)new ReplaceOneModel<TQueryModel>(IdFilter<TQueryModel>(model), model) { IsUpsert = true })
            .ToList();

        if (writes.Count > 0)
        {
            await _retryPolicy.ExecuteAsync(() => GetCollection<TQueryModel>().BulkWriteAsync(writes));
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync<TQueryModel>(Expression<Func<TQueryModel, bool>> deleteFilter) where TQueryModel : IQueryModel =>
        _retryPolicy.ExecuteAsync(() => GetCollection<TQueryModel>().DeleteManyAsync(deleteFilter));

    #endregion

    #region Reads

    /// <inheritdoc />
    public async Task<(long, IEnumerable<TQueryModel>)> GetAllCollectionAsync<TQueryModel>() where TQueryModel : IQueryModel
    {
        var items = await GetCollection<TQueryModel>().Find(FilterDefinition<TQueryModel>.Empty).ToListAsync();
        return (items.Count, items);
    }

    /// <inheritdoc />
    public Task<(long, IEnumerable<TQueryModel>)> GetCollectionAsync<TQueryModel>(int pageNumber = 1, int pageSize = 100)
        where TQueryModel : IQueryModel =>
        PageAsync(FilterDefinition<TQueryModel>.Empty, pageNumber, pageSize);

    /// <inheritdoc />
    public async Task<TQueryModel?> GetByIdAsync<TQueryModel, TKey>(TKey id) where TQueryModel : IQueryModel =>
        await GetCollection<TQueryModel>().Find(Builders<TQueryModel>.Filter.Eq("_id", id)).FirstOrDefaultAsync();

    /// <inheritdoc />
    /// <remarks>Every field must contain its value (case-insensitive); field names are the stored (camelCase) names.</remarks>
    public Task<(long, IEnumerable<TQueryModel>)> WildSearchAsync<TQueryModel>(
        Dictionary<string, string> queries, int pageNumber = 1, int pageSize = 100) where TQueryModel : IQueryModel =>
        PageAsync(SearchFilter<TQueryModel>(queries, Regex.Escape), pageNumber, pageSize);

    /// <inheritdoc />
    /// <remarks>
    /// Matches values whose characters appear in order with anything in between (case-insensitive),
    /// so <c>"lvlc"</c> finds <c>"Lovelace"</c>.
    /// </remarks>
    public Task<(long, IEnumerable<TQueryModel>)> FuzzySearchAsync<TQueryModel>(
        Dictionary<string, string> queries, int pageNumber = 1, int pageSize = 100) where TQueryModel : IQueryModel =>
        PageAsync(
            SearchFilter<TQueryModel>(queries, value => string.Join(".*", value.Select(c => Regex.Escape(c.ToString())))),
            pageNumber,
            pageSize);

    /// <inheritdoc />
    public object GetDatabaseContext() => _database;

    private async Task<(long, IEnumerable<TQueryModel>)> PageAsync<TQueryModel>(
        FilterDefinition<TQueryModel> filter, int pageNumber, int pageSize) where TQueryModel : IQueryModel
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var collection = GetCollection<TQueryModel>();
        var total = await collection.CountDocumentsAsync(filter);
        var items = await collection.Find(filter).Skip((pageNumber - 1) * pageSize).Limit(pageSize).ToListAsync();
        return (total, items);
    }

    private static FilterDefinition<TQueryModel> SearchFilter<TQueryModel>(
        Dictionary<string, string> queries, Func<string, string> toPattern)
    {
        ArgumentNullException.ThrowIfNull(queries);

        var filters = queries
            .Where(query => !string.IsNullOrWhiteSpace(query.Value))
            .Select(query => Builders<TQueryModel>.Filter.Regex(query.Key, new BsonRegularExpression(toPattern(query.Value.Trim()), "i")))
            .ToList();

        return filters.Count == 0 ? FilterDefinition<TQueryModel>.Empty : Builders<TQueryModel>.Filter.And(filters);
    }

    private static FilterDefinition<TQueryModel> IdFilter<TQueryModel>(TQueryModel model)
    {
        var id = model!.GetType().GetProperty("Id")?.GetValue(model)
            ?? throw new InvalidOperationException($"{typeof(TQueryModel).Name} has no Id to match on.");
        return Builders<TQueryModel>.Filter.Eq("_id", id);
    }

    #endregion

    #region Setup

    /// <summary>Creates the collections of every query model in the application assembly, and their indexes.</summary>
    public async Task CreateCollectionsAsync(CancellationToken cancellationToken = default)
    {
        using var cursor = await _database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var existing = await cursor.ToListAsync(cancellationToken);

        foreach (var name in GetCollectionNamesFromAssembly())
        {
            if (!existing.Exists(collection => collection.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("MongoDB: creating collection {Name}", name);
                await _database.CreateCollectionAsync(name, cancellationToken: cancellationToken);
            }
        }

        // Not unique: many customers share a first name.
        var index = new CreateIndexModel<CustomerQueryModel>(
            Builders<CustomerQueryModel>.IndexKeys.Ascending(model => model.FirstName),
            new CreateIndexOptions { Unique = false });
        await GetCollection<CustomerQueryModel>().Indexes.CreateOneAsync(index, cancellationToken: cancellationToken);
    }

    private static List<string> GetCollectionNamesFromAssembly() =>
        typeof(CustomerQueryModel).Assembly
            .GetAllTypesOf<IQueryModel>()
            .Select(type => type.Name)
            .Distinct()
            .ToList();

    private static AsyncRetryPolicy CreateRetryPolicy(ILogger logger) =>
        Policy
            .Handle<MongoException>()
            .WaitAndRetryAsync(
                RetryCount,
                // Exponential back-off with jitter: ~2 s, ~4 s.
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000)),
                (exception, delay, attempt, _) =>
                    logger.LogWarning(exception, "MongoDB write failed; retry {Attempt} in {Delay}", attempt, delay));

    #endregion
}
