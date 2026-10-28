using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KYC.EndToEndTests;

/// <summary>HTTP access to the stack under test, plus helpers for its eventually consistent parts.</summary>
public sealed class KycStack : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public KycStack()
    {
        var baseUrl = Environment.GetEnvironmentVariable(E2EFactAttribute.BaseUrlVariable);
        Client = new HttpClient
        {
            BaseAddress = new Uri(string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:8080" : baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public HttpClient Client { get; }

    public void Dispose() => Client.Dispose();

    public sealed record CustomerDto(Guid Id, string FirstName, string LastName, DateTime Dob, string FullName);

    private sealed record CreatedDto(Guid Id);

    public static object NewCustomer(string lastName, string firstName = "Ada") => new
    {
        firstName,
        lastName,
        dob = "1990-05-17",
        customerAddress = new { street = "12 St James's Square", city = "London", country = "United Kingdom", zipCode = "SW1Y 4JH" }
    };

    /// <summary>A last name that no other test run has used, so lists can be searched for it.</summary>
    public static string UniqueLastName(string prefix = "E2E") => $"{prefix}-{Guid.NewGuid():N}";

    public async Task<Guid> RegisterAsync(string lastName)
    {
        var response = await Client.PostAsJsonAsync("/api/customers", NewCustomer(lastName));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreatedDto>(Json);
        Assert.NotNull(created);
        return created.Id;
    }

    public async Task<IReadOnlyList<CustomerDto>> ListAsync()
    {
        var customers = await Client.GetFromJsonAsync<List<CustomerDto>>("/api/customers", Json);
        return customers ?? [];
    }

    /// <summary>Retries <paramref name="probe"/> until it returns a value or the timeout elapses.</summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T?>> probe, TimeSpan timeout, string description)
        where T : class
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            var value = await probe();
            if (value is not null)
            {
                return value;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out after {timeout.TotalSeconds:0}s waiting for: {description}");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }
}
