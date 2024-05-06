using KYC.Application.UseCases.Customers.Repositories;
using KYC.Write.Infrastructure.Persistence;
using KYC.Write.Infrastructure.Repositories;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Mehedi.EventBus.Kafka;
using Mehedi.Write.Infrastructure.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace KYC.Write.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWriteInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        string? connectionString = config.GetConnectionString("SqlConnection");
        // For SQLServer Connection
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                connectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                });
        });

        // Add Event Bus
        var producerConfig = new KafkaProducerConfig(config.GetConnectionString("Kafka"), config["eventsTopicName"]);
        services.AddSingleton(producerConfig);


        services.AddScoped<IWriteDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        //services.AddScoped(typeof(ICommandRepository<,>), typeof(CommandRepository<,>));
        services.AddScoped<ICustomerCommandRepository, CustomerCommandRepository>();

        // Add eventbus producers and consumers
        services.AddSingleton<IEventProducer, EventProducer>();

        return services;
    }
}

