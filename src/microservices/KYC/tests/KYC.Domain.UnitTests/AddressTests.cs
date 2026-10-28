using KYC.Domain.ValueObjects;

namespace KYC.Domain.UnitTests;

public class AddressTests
{
    [Fact]
    public void Addresses_with_the_same_components_are_equal()
    {
        var first = new Address("1 Main St", "Dhaka", "Dhaka Division", "Bangladesh", "1000");
        var second = new Address("1 Main St", "Dhaka", "Dhaka Division", "Bangladesh", "1000");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Addresses_that_differ_in_any_component_are_not_equal()
    {
        var baseline = new Address("1 Main St", "Dhaka", "Dhaka Division", "Bangladesh", "1000");
        var different = new Address("1 Main St", "Dhaka", "Dhaka Division", "Bangladesh", "1001");

        Assert.NotEqual(baseline, different);
    }

    [Fact]
    public void Null_components_become_empty_strings()
    {
        var address = new Address(null, null, null, null, null);

        Assert.Equal(string.Empty, address.Street);
        Assert.Equal(string.Empty, address.City);
        Assert.Equal(string.Empty, address.State);
        Assert.Equal(string.Empty, address.Country);
        Assert.Equal(string.Empty, address.ZipCode);
        Assert.True(address.IsEmpty);
    }

    [Fact]
    public void Components_are_trimmed()
    {
        var address = new Address("  1 Main St ", " Dhaka", "Dhaka Division ", "  Bangladesh  ", " 1000 ");

        Assert.Equal("1 Main St", address.Street);
        Assert.Equal("Dhaka", address.City);
        Assert.Equal("Dhaka Division", address.State);
        Assert.Equal("Bangladesh", address.Country);
        Assert.Equal("1000", address.ZipCode);
    }

    [Fact]
    public void Empty_addresses_are_equal_to_each_other()
    {
        Assert.Equal(Address.Empty, new Address(null, null, null, null, null));
        Assert.True(Address.Empty.IsEmpty);
    }

    [Fact]
    public void An_address_with_any_information_is_not_empty()
    {
        Assert.False(new Address(null, "Dhaka", null, null, null).IsEmpty);
    }
}
