using System.Text;
using EventStore.Client;
using Mehedi.Application.SharedKernel.Services;
using Microsoft.Extensions.Logging;

namespace KYC.EventStore.EventStoreDB.Infrastructure.Repositories;

/// <summary>
/// Appends domain events to EventStoreDB. Events are stored in one stream per aggregate
/// (<c>customer-{aggregateId}</c>) under their own event type name.
/// </summary>
public sealed class EventStoreRepository(EventStoreClient eventStoreClient, ILogger<EventStoreRepository> logger)
    : IEventStoreRepository
{
    /// <summary>Prefix of every customer stream, e.g. <c>customer-3f2c...</c>.</summary>
    public const string StreamPrefix = "customer-";

    public async Task StoreAsync(IEnumerable<EventStoreEvent> eventStores)
    {
        ArgumentNullException.ThrowIfNull(eventStores);

        foreach (var aggregateEvents in eventStores.GroupBy(@event => @event.AggregateId.ToString()))
        {
            var streamName = StreamPrefix + aggregateEvents.Key;

            var eventData = aggregateEvents.Select(@event => new EventData(
                Uuid.FromGuid(@event.Id),
                @event.MessageType ?? "UnknownEvent",
                Encoding.UTF8.GetBytes(@event.Data)));

            var result = await eventStoreClient.AppendToStreamAsync(streamName, StreamState.Any, eventData);

            logger.LogInformation(
                "Appended events to stream {StreamName}, next expected revision {NextExpectedRevision}",
                streamName,
                result.NextExpectedStreamRevision);
        }
    }

    /// <summary>
    /// Nothing to release: the <see cref="EventStoreClient"/> is a shared singleton owned by the DI container.
    /// </summary>
    public void Dispose()
    {
    }
}
