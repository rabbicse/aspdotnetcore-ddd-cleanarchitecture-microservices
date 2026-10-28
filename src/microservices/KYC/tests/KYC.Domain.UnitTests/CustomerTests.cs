using KYC.Domain.Aggregates.CustomerAggregate;
using KYC.Domain.Aggregates.CustomerAggregate.Events;
using KYC.Domain.ValueObjects;

namespace KYC.Domain.UnitTests;

public class CustomerTests
{
    // "Today" for every test: 28 October 2026.
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTime ValidDob = new(1990, 5, 17);

    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static Address SampleAddress() => new("12 St James's Square", "London", "England", "United Kingdom", "SW1Y 4JH");

    private Customer NewCustomer() => new("Ada", "Lovelace", ValidDob, SampleAddress(), _clock);

    #region Construction
    [Fact]
    public void A_new_customer_has_the_given_details_and_a_generated_id()
    {
        var address = SampleAddress();

        var customer = new Customer("Ada", "Lovelace", ValidDob, address, _clock);

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal(ValidDob, customer.Dob);
        Assert.Equal(address, customer.Address);
    }

    [Fact]
    public void Every_customer_gets_its_own_id()
    {
        Assert.NotEqual(NewCustomer().Id, NewCustomer().Id);
    }

    [Fact]
    public void Names_are_trimmed()
    {
        var customer = new Customer("  Ada ", " Lovelace  ", ValidDob, SampleAddress(), _clock);

        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
    }

    [Fact]
    public void A_customer_without_an_address_gets_the_empty_address()
    {
        var customer = new Customer("Ada", "Lovelace", ValidDob, address: null, timeProvider: _clock);

        Assert.True(customer.Address.IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_first_name_is_rejected(string? firstName)
    {
        var error = Assert.ThrowsAny<ArgumentException>(
            () => new Customer(firstName!, "Lovelace", ValidDob, SampleAddress(), _clock));

        Assert.Equal("firstName", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_last_name_is_rejected(string? lastName)
    {
        var error = Assert.ThrowsAny<ArgumentException>(
            () => new Customer("Ada", lastName!, ValidDob, SampleAddress(), _clock));

        Assert.Equal("lastName", error.ParamName);
    }

    [Fact]
    public void A_name_of_exactly_the_maximum_length_is_accepted()
    {
        var name = new string('a', Customer.MaxNameLength);

        var customer = new Customer(name, name, ValidDob, SampleAddress(), _clock);

        Assert.Equal(name, customer.FirstName);
        Assert.Equal(name, customer.LastName);
    }

    [Fact]
    public void A_name_longer_than_the_maximum_is_rejected()
    {
        var tooLong = new string('a', Customer.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => new Customer(tooLong, "Lovelace", ValidDob, SampleAddress(), _clock));
        Assert.Throws<ArgumentException>(() => new Customer("Ada", tooLong, ValidDob, SampleAddress(), _clock));
    }

    [Fact]
    public void A_date_of_birth_in_the_future_is_rejected()
    {
        var tomorrow = Now.UtcDateTime.Date.AddDays(1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Customer("Ada", "Lovelace", tomorrow, SampleAddress(), _clock));
    }

    [Fact]
    public void A_date_of_birth_of_today_is_accepted()
    {
        var customer = new Customer("Ada", "Lovelace", Now.UtcDateTime.Date, SampleAddress(), _clock);

        Assert.Equal(Now.UtcDateTime.Date, customer.Dob);
    }

    [Fact]
    public void A_date_of_birth_before_the_minimum_is_rejected()
    {
        var tooOld = Customer.MinimumDateOfBirth.AddDays(-1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Customer("Ada", "Lovelace", tooOld, SampleAddress(), _clock));
    }

    [Fact]
    public void The_minimum_date_of_birth_itself_is_accepted()
    {
        var customer = new Customer("Ada", "Lovelace", Customer.MinimumDateOfBirth, SampleAddress(), _clock);

        Assert.Equal(Customer.MinimumDateOfBirth, customer.Dob);
    }
    #endregion

    #region Domain events
    [Fact]
    public void A_new_customer_has_no_domain_events_until_it_is_created()
    {
        Assert.Empty(NewCustomer().DomainEvents);
    }

    [Fact]
    public void Create_raises_a_customer_created_event_carrying_the_customer_data()
    {
        var customer = NewCustomer();

        customer.Create();

        var domainEvent = Assert.IsType<CustomerCreatedDomainEvent>(Assert.Single(customer.DomainEvents));
        Assert.Equal(customer.Id, domainEvent.Id);
        Assert.Equal("Ada", domainEvent.FirstName);
        Assert.Equal("Lovelace", domainEvent.LastName);
        Assert.Equal(ValidDob, domainEvent.Dob);
        Assert.Equal(customer.Address, domainEvent.Address);
    }

    [Fact]
    public void Delete_raises_a_customer_deleted_event()
    {
        var customer = NewCustomer();

        customer.Delete();

        var domainEvent = Assert.IsType<CustomerDeletedDomainEvent>(Assert.Single(customer.DomainEvents));
        Assert.Equal(customer.Id, domainEvent.Id);
    }
    #endregion

    #region Update
    [Fact]
    public void Update_changes_the_details_and_raises_a_customer_updated_event()
    {
        var customer = NewCustomer();
        var newAddress = new Address("1 Main St", "Dhaka", "Dhaka Division", "Bangladesh", "1000");
        var newDob = new DateTime(1985, 12, 9);

        customer.Update("Grace", "Hopper", newDob, newAddress, _clock);

        Assert.Equal("Grace", customer.FirstName);
        Assert.Equal("Hopper", customer.LastName);
        Assert.Equal(newDob, customer.Dob);
        Assert.Equal(newAddress, customer.Address);

        var domainEvent = Assert.IsType<CustomerUpdatedDomainEvent>(Assert.Single(customer.DomainEvents));
        Assert.Equal(customer.Id, domainEvent.Id);
        Assert.Equal("Grace", domainEvent.FirstName);
        Assert.Equal("Hopper", domainEvent.LastName);
    }

    [Fact]
    public void Update_keeps_the_id()
    {
        var customer = NewCustomer();
        var id = customer.Id;

        customer.Update("Grace", "Hopper", ValidDob, SampleAddress(), _clock);

        Assert.Equal(id, customer.Id);
    }

    [Fact]
    public void A_rejected_update_leaves_the_customer_unchanged_and_raises_no_event()
    {
        var customer = NewCustomer();
        var future = Now.UtcDateTime.Date.AddDays(1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => customer.Update("Grace", "Hopper", future, SampleAddress(), _clock));

        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal(ValidDob, customer.Dob);
        Assert.Empty(customer.DomainEvents);
    }
    #endregion
}
