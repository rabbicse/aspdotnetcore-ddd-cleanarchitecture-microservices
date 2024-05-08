using EventStore.Client;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KYC.EventStore.EventStoreDB.Infrastructure.Repositories;

public sealed class EventStoreRepository(EventStoreClient eventStoreClient, ILogger<EventStoreRepository> logger) : IEventStoreRepository
{
    private readonly EventStoreClient _eventStoreClient = eventStoreClient;
    private readonly ILogger<EventStoreRepository> _logger = logger;
    /// <summary>
    /// Append events asynchronously
    /// </summary>
    /// <param name="eventStores"></param>
    /// <returns></returns>
    public async Task StoreAsync(IEnumerable<EventStoreEvent> eventStores)
    {
        if (eventStores == null)
        {
            return;
        }

        // TODO:
        var result = await _eventStoreClient.AppendToStreamAsync(
            eventStores.First().Id.ToString(),
            StreamState.Any,
            eventStores.Select(ev => new EventData(
                Uuid.NewUuid(), 
                "Test Event", 
                JsonSerializer.SerializeToUtf8Bytes(ev))));

        _logger.LogInformation($"Event Store result: {result.NextExpectedStreamRevision}");
    }

    #region IDisposable

    // To detect redundant calls.
    private bool _disposed;

    // Public implementation of Dispose pattern callable by consumers.
    ~EventStoreRepository() => Dispose(false);

    // Public implementation of Dispose pattern callable by consumers.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        // Dispose managed state (managed objects).        

        _disposed = true;
    }

    #endregion
}
