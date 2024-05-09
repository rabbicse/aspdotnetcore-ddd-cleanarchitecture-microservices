using Mehedi.EventBus.Kafka;
using Mehedi.EventBus;

namespace KYC.API;

public static class DependencyInjection
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration config)
    {
        // Add Event Bus
        var producerConfig = new KafkaProducerConfig(config.GetConnectionString("Kafka"), config["eventsTopicName"]);
        return services.AddSingleton(producerConfig)
                       // Add eventbus producers and consumers
                       .AddSingleton<IEventProducer, EventProducer>(); ;
    }
}
