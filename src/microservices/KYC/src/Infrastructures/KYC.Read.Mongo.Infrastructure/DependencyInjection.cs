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

namespace KYC.Read.Mongo.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    /// <summary>
    /// Registers the MongoDB read side: the notification handlers that project domain events into
    /// the read model, the database context and the query repositories.
    /// </summary>
    /// <param name="configuration">Kept for API compatibility; the connection string is read by <see cref="NoSqlDbContext"/>.</param>
    public static IServiceCollection AddReadInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        return services
            // Registers CustomerEventHandler (merged with the application layer's MediatR registration).
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))
            .AddReadDbContext()
            .AddReadOnlyRepositories();
    }

    private static IServiceCollection AddReadDbContext(this IServiceCollection services)
    {
        // One NoSqlDbContext (and therefore one MongoClient connection pool) exposed through both abstractions.
        services
            .AddSingleton<NoSqlDbContext>()
            .AddSingleton<ISynchronizeDb>(provider => provider.GetRequiredService<NoSqlDbContext>())
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
