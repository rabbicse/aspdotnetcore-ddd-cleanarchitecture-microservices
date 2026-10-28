using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KYC.Application.UseCases.Customers.DTOs;
using Mehedi.EventBus;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace KYC.API.Tests;

public sealed class CustomersApiTests(KycApiFactory factory) : IClassFixture<KycApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object ValidCustomer(string firstName = "Ada", string lastName = "Lovelace") => new
    {
        firstName,
        lastName,
        dob = "1990-05-17",
        customerAddress = new
        {
            street = "12 St James's Square",
            city = "London",
            state = "England",
            country = "United Kingdom",
            zipCode = "SW1Y 4JH"
        }
    };

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private async Task<Guid> RegisterAsync(string firstName = "Ada", string lastName = "Lovelace")
    {
        var response = await _client.PostAsJsonAsync("/api/customers", ValidCustomer(firstName, lastName));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreatedCustomerResponse>();
        Assert.NotNull(created);
        return created.Id;
    }

    #region POST /api/customers
    [Fact]
    public async Task Registering_a_customer_returns_201_with_its_id_and_a_location()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", ValidCustomer());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedCustomerResponse>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.EndsWith($"/{created.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task A_registered_customer_can_be_fetched_by_id()
    {
        var id = await RegisterAsync("Grace", "Hopper");

        var response = await _client.GetAsync($"/api/customers/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customer = await ReadJsonAsync(response);
        Assert.Equal(id, customer.GetProperty("id").GetGuid());
        Assert.Equal("Grace", customer.GetProperty("firstName").GetString());
        Assert.Equal("Hopper", customer.GetProperty("lastName").GetString());
        Assert.Equal("Grace Hopper", customer.GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task The_registration_is_announced_to_other_services()
    {
        using var isolated = new KycApiFactory();
        using var client = isolated.CreateClient();

        var response = await client.PostAsJsonAsync("/api/customers", ValidCustomer());
        var created = await response.Content.ReadFromJsonAsync<CreatedCustomerResponse>();

        Assert.NotNull(created);
        await isolated.Producer.Received(1).PublishAsync(
            Arg.Is<IntegrationEvent>(@event => ((KYC.Application.UseCases.Customers.Events.CustomerCreatedEvent)@event).CustomerId == created.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_broker_outage_does_not_fail_the_registration()
    {
        using var isolated = new KycApiFactory();
        isolated.Producer.PublishAsync(Arg.Any<IntegrationEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("broker is down"));
        using var client = isolated.CreateClient();

        var response = await client.PostAsJsonAsync("/api/customers", ValidCustomer());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    #endregion

    #region Validation
    [Fact]
    public async Task A_blank_first_name_returns_400_problem_details_naming_the_field()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", ValidCustomer(firstName: ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.GetProperty("errors").EnumerateObject().Any(error =>
            error.Name.Equals("FirstName", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task A_date_of_birth_in_the_future_returns_400_problem_details_naming_the_field()
    {
        var request = new { firstName = "Ada", lastName = "Lovelace", dob = "2999-01-01" };

        var response = await _client.PostAsJsonAsync("/api/customers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.True(problem.GetProperty("errors").EnumerateObject().Any(error =>
            error.Name.Equals("Dob", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task A_name_that_is_too_long_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", ValidCustomer(lastName: new string('x', 101)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_json_returns_400()
    {
        using var content = new StringContent("{ this is not json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/customers", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_non_json_content_type_returns_415()
    {
        using var content = new StringContent("firstName=Ada", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _client.PostAsync("/api/customers", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_request_registers_nothing()
    {
        using var isolated = new KycApiFactory();
        using var client = isolated.CreateClient();

        await client.PostAsJsonAsync("/api/customers", ValidCustomer(firstName: ""));

        Assert.Empty(isolated.Customers);
    }
    #endregion

    #region GET
    [Fact]
    public async Task An_unknown_id_returns_404_problem_details()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await ReadJsonAsync(response);
        Assert.Equal(404, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task An_id_that_is_not_a_guid_does_not_match_a_route()
    {
        var response = await _client.GetAsync("/api/customers/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_list_contains_every_registered_customer()
    {
        var first = await RegisterAsync("Ada", "Lovelace");
        var second = await RegisterAsync("Grace", "Hopper");

        var response = await _client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customers = await ReadJsonAsync(response);
        var ids = customers.EnumerateArray().Select(customer => customer.GetProperty("id").GetGuid()).ToList();
        Assert.Contains(first, ids);
        Assert.Contains(second, ids);
    }

    [Fact]
    public async Task The_list_is_a_plain_array_not_a_result_wrapper()
    {
        var response = await _client.GetAsync("/api/customers");

        var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }
    #endregion

    #region Platform
    [Fact]
    public async Task The_liveness_probe_reports_healthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_openapi_document_describes_the_customer_endpoints()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await ReadJsonAsync(response);
        var paths = document.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();
        Assert.Contains(paths, path => path.Equals("/api/Customers", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, path => path.Equals("/api/Customers/{id}", StringComparison.OrdinalIgnoreCase));
    }
    #endregion
}
