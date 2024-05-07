using KYC.Domain.Aggregates.CustomerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KYC.Write.MsSql.Infrastructure.EfConfigurations;

/// <summary>
/// Source: https://github.com/dotnet/eShop/blob/main/src/Ordering.Infrastructure/OrderingContext.cs
/// https://github.com/dotnet/eShop/blob/main/src/Ordering.Infrastructure/EntityConfigurations/OrderEntityTypeConfiguration.cs
/// </summary>
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(nameof(Customer));

        builder.Ignore(b => b.DomainEvents);

        builder.OwnsOne(o => o.Address);
    }
}
