using AutoMapper;
using FluentValidation;
using KYC.Application.UseCases.Customers.Repositories;
using KYC.Read.Infrastructure.Repositories;
using KYC.Read.Mongo.Infrastructure.Persistence;
using Mehedi.Application.SharedKernel.Persistence;
using Mehedi.Read.NoSql.Infrastructure.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace KYC.Read.Mongo.Infrastructure;


[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>
    /// Adds query handlers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection AddReadInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return services
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))
            .AddSingleton<IMapper>(new Mapper(new MapperConfiguration(cfg => cfg.AddMaps(assembly))))
            .AddValidatorsFromAssembly(assembly)
            
            // Add custom extension to add mongodb contexts
            .AddReadDbContext()

            // Add custom extension to add repositories
            .AddReadOnlyRepositories();
    }

    /// <summary>
    /// Adds the read database context to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static IServiceCollection AddReadDbContext(this IServiceCollection services)
    {
        services
            // Write operations will be handled by ISynchronizeDb
            .AddSingleton<ISynchronizeDb, NoSqlDbContext>()
            // Read operations will be handled by IReadDbContext
            .AddSingleton<IReadDbContext, NoSqlDbContext>()
            // Construct NoSqlDbContext for mongodb
            .AddSingleton<NoSqlDbContext>();

        ConfigureMongoDb();

        return services;
    }

    /// <summary>
    /// Adds read-only repositories to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static IServiceCollection AddReadOnlyRepositories(this IServiceCollection services) =>
        services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();

    #region Configuration(s)
    /// <summary>
    /// Configures the MongoDB settings and mappings.
    /// </summary>
    private static void ConfigureMongoDb()
    {
        try
        {
            // Step 1: Configure the serializer for Guid type.
            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.CSharpLegacy));

            // Step 2: Configure the conventions to be applied to all mappings.
            // REF: https://mongodb.github.io/mongo-csharp-driver/2.0/reference/bson/mapping/conventions/
            ConventionRegistry.Register("Conventions",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(), // Convert element names to camel case
                    new EnumRepresentationConvention(BsonType.String), // Serialize enums as strings
                    new IgnoreExtraElementsConvention(true), // Ignore extra elements when deserializing
                    new IgnoreIfNullConvention(true) // Ignore null values when serializing
                }, _ => true);

            // Step 3: Register the mappings configurations.
            // It is recommended to register all mappings before initializing the connection with MongoDb
            // REF: https://mongodb.github.io/mongo-csharp-driver/2.0/reference/bson/mapping/
            //new CustomerMap().Configure(); // Configuration for Customer class
        }
        catch
        {
            // Ignore
        }
    }
    #endregion
}
