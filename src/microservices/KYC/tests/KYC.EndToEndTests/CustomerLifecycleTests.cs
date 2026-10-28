using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KYC.EndToEndTests;

/// <summary>
/// The whole system, for real: SQL Server write model, MongoDB read model, Redis cache, Kafka,
/// EventStoreDB and Hangfire all running in the compose stack.
/// </summary>
[Trait("Category", "E2E")]
public sealed class CustomerLifecycleTests : IClassFixture<KycStack>
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);
    private readonly KycStack _stack;

    public CustomerLifecycleTests(KycStack stack) => _stack = stack;

    [E2EFact]
    public async Task The_service_reports_ready()
    {
        var response = await _stack.Client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [E2EFact]
    public async Task A_registered_customer_is_readable_from_the_read_model()
    {
        var lastName = KycStack.UniqueLastName();
        var id = await _stack.RegisterAsync(lastName);

        var customer = await KycStack.EventuallyAsync(
            async () =>
            {
                var response = await _stack.Client.GetAsync($"/api/customers/{id}");
                return response.StatusCode == HttpStatusCode.OK
                    ? await response.Content.ReadFromJsonAsync<KycStack.CustomerDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
                    : null;
            },
            Patience,
            $"customer {id} to appear in the read model");

        Assert.Equal(id, customer.Id);
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal(lastName, customer.LastName);
        Assert.Equal(new DateTime(1990, 5, 17), customer.Dob);
    }

    [E2EFact]
    public async Task A_registered_customer_appears_in_the_list()
    {
        var lastName = KycStack.UniqueLastName();
        var id = await _stack.RegisterAsync(lastName);

        var found = await KycStack.EventuallyAsync(
            async () => (await _stack.ListAsync()).FirstOrDefault(customer => customer.Id == id),
            Patience,
            $"customer {id} to appear in the list");

        Assert.Equal(lastName, found.LastName);
    }

    [E2EFact]
    public async Task Registering_in_the_background_eventually_creates_the_customer()
    {
        var lastName = KycStack.UniqueLastName("E2E-async");

        var response = await _stack.Client.PostAsJsonAsync("/api/customers/async", KycStack.NewCustomer(lastName));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("jobId").GetString()));

        var found = await KycStack.EventuallyAsync(
            async () => (await _stack.ListAsync()).FirstOrDefault(customer => customer.LastName == lastName),
            TimeSpan.FromSeconds(60),
            $"the background job to create a customer named {lastName}");

        Assert.Equal("Ada", found.FirstName);
    }

    [E2EFact]
    public async Task An_invalid_registration_is_rejected_with_problem_details()
    {
        var response = await _stack.Client.PostAsJsonAsync(
            "/api/customers", new { firstName = "", lastName = "Turing", dob = "2999-01-01" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [E2EFact]
    public async Task An_invalid_background_registration_is_rejected_before_it_is_queued()
    {
        var response = await _stack.Client.PostAsJsonAsync(
            "/api/customers/async", new { firstName = "", lastName = "Turing", dob = "1990-05-17" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [E2EFact]
    public async Task An_unknown_customer_is_a_404()
    {
        var response = await _stack.Client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
