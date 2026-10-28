using System.Collections.Concurrent;
using KYC.Application.UseCases.Customers.DTOs;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Domain.Aggregates.CustomerAggregate;
using Mehedi.Application.SharedKernel.Services;
using Mehedi.Core.SharedKernel;
using Mehedi.EventBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace KYC.API.Tests;

/// <summary>
/// Hosts the real API pipeline (routing, model binding, MediatR, validation, exception handling, JSON)
/// in memory. Only the infrastructure edges are replaced:
/// the write repository and unit of work store customers in a dictionary that doubles as the read model,
/// the cache never caches, and the event producer is a substitute.
/// </summary>
public sealed class KycApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Stands in for both SQL Server (write) and MongoDB (read).</summary>
    public ConcurrentDictionary<Guid, CustomerQueryModel> Customers { get; } = new();

    public IEventProducer Producer { get; } = Substitute.For<IEventProducer>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Loads appsettings.Testing.json (placeholder connection strings, no Hangfire server).
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var commandRepository = Substitute.For<ICustomerCommandRepository>();
            commandRepository.AddAsync(Arg.Any<Customer>()).Returns(call =>
            {
                var customer = call.Arg<Customer>();
                Customers[customer.Id] = new CustomerQueryModel(customer.Id, customer.FirstName, customer.LastName, customer.Dob);
                return Task.FromResult(customer);
            });

            var queryRepository = Substitute.For<ICustomerQueryRepository>();
            queryRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(call =>
                Task.FromResult(Customers.GetValueOrDefault(call.Arg<Guid>())!));
            queryRepository.GetAllAsync().Returns(_ =>
                Task.FromResult<IEnumerable<CustomerQueryModel>>(Customers.Values.ToList()));

            var unitOfWork = Substitute.For<IUnitOfWork>();
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(true);

            Producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>()).Returns(true);

            services.RemoveAll<ICustomerCommandRepository>();
            services.RemoveAll<ICustomerQueryRepository>();
            services.RemoveAll<IUnitOfWork>();
            services.RemoveAll<ICacheService>();
            services.RemoveAll<IEventProducer>();

            services.AddSingleton(commandRepository);
            services.AddSingleton(queryRepository);
            services.AddSingleton(unitOfWork);
            services.AddSingleton<ICacheService, PassThroughCacheService>();
            services.AddSingleton(Producer);
        });
    }
}
