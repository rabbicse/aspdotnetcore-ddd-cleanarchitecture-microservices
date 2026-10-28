using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Write.MsSql.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace KYC.Infrastructure.UnitTests;

public class WriteModelTests
{
    // Builds the EF model only; no connection is opened, so no database is needed.
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=unused;")
            .Options);

    [Fact]
    public void The_migrations_are_up_to_date_with_the_model()
    {
        // Fails when someone changes the entity model without adding a migration (or the other way round).
        using var context = CreateContext();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "The EF model differs from the latest migration snapshot. Add a migration: " +
            "dotnet ef migrations add <Name> --project src/Infrastructures/KYC.Write.MsSql.Infrastructure --startup-project src/Presentation/KYC.API");
    }

    [Fact]
    public void Domain_events_are_not_persisted()
    {
        using var context = CreateContext();

        var customer = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Customer));

        Assert.NotNull(customer);
        Assert.Null(customer.FindProperty(nameof(Customer.DomainEvents)));
    }

    [Fact]
    public void The_address_is_stored_inside_the_customer_table()
    {
        using var context = CreateContext();

        var customer = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Customer));

        Assert.NotNull(customer);
        Assert.Equal(nameof(Customer), customer.GetTableName());
        var address = customer.FindNavigation(nameof(Customer.Address));
        Assert.NotNull(address);
        Assert.True(address.TargetEntityType.IsOwned());
    }
}
