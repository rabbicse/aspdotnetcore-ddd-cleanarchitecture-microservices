using KYC.Application.UseCases.Customers.DTOs;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Core.SharedKernel;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Polly;
using Polly.Retry;
using System.Linq.Expressions;
using System.Reflection;
using Mehedi.Application.SharedKernel.Extensions;
using Microsoft.Extensions.Configuration;
using Mehedi.Read.NoSql.Infrastructure.Abstractions;

namespace KYC.Read.Mongo.Infrastructure.Persistence;

public sealed class NoSqlDbContext : IReadDbContext, ISynchronizeDb
{
    #region Constructor

    private const string DatabaseName = "KYC";
    private const int RetryCount = 2;
    private static readonly Random Rnd = new();

    private static readonly ReplaceOptions DefaultReplaceOptions = new()
    {
        IsUpsert = true
    };

    private static readonly CreateIndexOptions DefaultCreateIndexOptions = new()
    {
        Unique = true,
        Sparse = true
    };

    private readonly IMongoDatabase _database;
    private readonly ILogger<NoSqlDbContext> _logger;
    private readonly AsyncRetryPolicy _mongoRetryPolicy;

    public NoSqlDbContext(IConfiguration config, ILogger<NoSqlDbContext> logger)
    {
        var connectionString = config.GetConnectionString("NoSqlConnection");

        var mongoClient = new MongoClient(connectionString);
        _database = mongoClient.GetDatabase(DatabaseName);
        _logger = logger;
        _mongoRetryPolicy = CreateRetryPolicy(logger);
    }

    #endregion

    #region IReadDbContext

    public async Task<IEnumerable<TQueryModel>> GetCollectionAsync<TQueryModel>() where TQueryModel : IQueryModel =>
        await GetCollection<TQueryModel>().Find(x => true).ToListAsync();

    public IMongoCollection<TQueryModel> GetCollection<TQueryModel>() where TQueryModel : IQueryModel =>
    _database.GetCollection<TQueryModel>(typeof(TQueryModel).Name);
    public async Task CreateCollectionsAsync()
    {
        using var asyncCursor = await _database.ListCollectionNamesAsync();
        var collections = await asyncCursor.ToListAsync();

        foreach (var collectionName in GetCollectionNamesFromAssembly())
        {
            // Check if the collection does not exist in the database
            if (!collections.Exists(db => db.Equals(collectionName, StringComparison.InvariantCultureIgnoreCase)))
            {
                _logger.LogInformation("----- MongoDB: creating the Collection {Name}", collectionName);
                await _database.CreateCollectionAsync(collectionName);
            }
            else
            {
                _logger.LogInformation("----- MongoDB: the {Name} collection already exists", collectionName);
            }
        }

        await CreateIndexAsync();
    }

    private async Task CreateIndexAsync()
    {
        // Define the index key as ascending order of the Email field in the CustomerQueryModel class
        var indexDefinition = Builders<CustomerQueryModel>.IndexKeys.Ascending(model => model.FirstName);

        // Create an index model with the defined index key and default index options
        var indexModel = new CreateIndexModel<CustomerQueryModel>(indexDefinition, DefaultCreateIndexOptions);

        var collection = GetCollection<CustomerQueryModel>();
        await collection.Indexes.CreateOneAsync(indexModel);
    }

    private static List<string> GetCollectionNamesFromAssembly() =>
        Assembly
            .GetExecutingAssembly()
            .GetAllTypesOf<IQueryModel>()
            .Select(impl => impl.Name)
            .Distinct()
            .ToList();

    #endregion

    #region ISynchronizeDb

    public async Task UpsertAsync<TQueryModel>(TQueryModel queryModel, Expression<Func<TQueryModel, bool>> upsertFilter)
        where TQueryModel : IQueryModel
    {
        var collection = GetCollection<TQueryModel>();

        await _mongoRetryPolicy.ExecuteAsync(async () =>
            await collection.ReplaceOneAsync(upsertFilter, queryModel, DefaultReplaceOptions));
    }

    public async Task DeleteAsync<TQueryModel>(Expression<Func<TQueryModel, bool>> deleteFilter)
        where TQueryModel : IQueryModel
    {
        var collection = GetCollection<TQueryModel>();
        await _mongoRetryPolicy.ExecuteAsync(async () => await collection.DeleteOneAsync(deleteFilter));
    }

    private static AsyncRetryPolicy CreateRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<MongoException>()
            .WaitAndRetryAsync(RetryCount, SleepDurationProvider, OnRetry);

        void OnRetry(Exception ex, TimeSpan _) =>
            logger.LogError(ex, "An unexpected exception occurred while saving to MongoDB: {Message}", ex.Message);

        TimeSpan SleepDurationProvider(int retryAttempt)
        {
            // Retry with jitter
            // A well-known retry strategy is exponential backoff, allowing retries to be made initially quickly,
            // but then at progressively longer intervals: for example, after 2, 4, 8, 15, then 30 seconds.
            // REF: https://github.com/App-vNext/Polly/wiki/Retry-with-jitter#simple-jitter
            var sleepDuration =
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(Rnd.Next(0, 1000));

            logger.LogWarning("----- MongoDB: Retry #{Count} with delay {Delay}", retryAttempt, sleepDuration);
            return sleepDuration;
        }
    }

    #endregion
}
