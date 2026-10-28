using KYC.Application.UseCases.Customers.Repositories;
using KYC.Read.Mongo.Infrastructure.Repositories;
using KYC.Read.Mongo.Infrastructure.Persistence;
using Mehedi.Application.SharedKernel.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using System.Diagnostics.CodeAnalysis;

namespace KYC.Read.Mongo.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>
    /// Registers the MongoDB read side: the notification handlers that project domain events into
    /// the read model, the database context and the query repositories.
    /// </summary>
    public const string ConnectionStringName = "NoSqlConnection";

    /// <exception cref="InvalidOperationException">The <c>ConnectionStrings:NoSqlConnection</c> setting is missing.</exception>
    public static IServiceCollection AddReadInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");

        var assembly = typeof(DependencyInjection).Assembly;

        return services
            // Registers CustomerEventHandler (merged with the application layer's MediatR registration).
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))
            .AddReadDbContext(connectionString)
            .AddReadOnlyRepositories();
    }

    private static IServiceCollection AddReadDbContext(this IServiceCollection services, string connectionString)
    {
        // One NoSqlDbContext, and therefore one MongoClient connection pool, for the whole process.
        services
            .AddSingleton(provider => new NoSqlDbContext(connectionString, provider.GetRequiredService<ILogger<NoSqlDbContext>>()))
            .AddSingleton<IReadDbContext>(provider => provider.GetRequiredService<NoSqlDbContext>());

        ConfigureMongoDb();

        return services;
    }

    private static IServiceCollection AddReadOnlyRepositories(this IServiceCollection services) =>
        services.AddScoped<ICustomerQueryRepository, CustomerQueryRepository>();

    /// <summary>Configures the MongoDB serializers and conventions (idempotent).</summary>
    private static void ConfigureMongoDb()
    {
        try
        {
            // Guids are stored with the legacy C# representation, as in the existing data.
            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.CSharpLegacy));

            // REF: https://mongodb.github.io/mongo-csharp-driver/2.0/reference/bson/mapping/conventions/
            ConventionRegistry.Register("Conventions",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new EnumRepresentationConvention(BsonType.String),
                    new IgnoreExtraElementsConvention(true),
                    new IgnoreIfNullConvention(true)
                }, _ => true);
        }
        catch (Exception)
        {
            // Registration can only fail when another component already registered the same
            // serializer or conventions; the existing registration is then reused.
        }
    }
}
