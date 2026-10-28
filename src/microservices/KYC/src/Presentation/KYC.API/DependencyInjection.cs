using Mehedi.EventBus;
using Mehedi.EventBus.Kafka;

namespace KYC.API;

public static class DependencyInjection
{
    /// <summary>Registers the Kafka integration-event producer.</summary>
    /// <exception cref="InvalidOperationException">
    /// <c>ConnectionStrings:Kafka</c> or <c>eventsTopicName</c> is missing.
    /// </exception>
    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration config)
    {
        var bootstrapServers = config.GetConnectionString("Kafka")
            ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:Kafka' is not configured.");
        var topic = config["eventsTopicName"]
            ?? throw new InvalidOperationException("Setting 'eventsTopicName' is not configured.");

        // The instance id is written to every message's "sender" header.
        return services
            .AddSingleton(new KafkaProducerConfig(bootstrapServers, topic, instanceId: "kyc-api"))
            .AddSingleton<IEventProducer, KafkaEventProducer>();
    }
}
