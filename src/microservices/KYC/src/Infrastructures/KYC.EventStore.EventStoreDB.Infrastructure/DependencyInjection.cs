using EventStore.Client;
using KYC.EventStore.EventStoreDB.Infrastructure.Repositories;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KYC.EventStore.EventStoreDB.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "EventStoreDbConnection";

    /// <summary>
    /// Registers the EventStoreDB client (singleton, disposed with the container) and the event store repository.
    /// </summary>
    /// <exception cref="InvalidOperationException">The <c>ConnectionStrings:EventStoreDbConnection</c> setting is missing.</exception>
    public static IServiceCollection AddEventStoreInfrastructureServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");

        var settings = EventStoreClientSettings.Create(connectionString);

        // Registered through a factory so the container owns (and disposes) the client.
        return services
            .AddSingleton(_ => new EventStoreClient(settings))
            .AddScoped<IEventStoreRepository, EventStoreRepository>();
    }
}
