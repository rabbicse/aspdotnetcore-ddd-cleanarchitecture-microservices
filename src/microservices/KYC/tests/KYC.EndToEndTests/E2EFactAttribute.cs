namespace KYC.EndToEndTests;

/// <summary>A [Fact] that is skipped (not failed) when no stack is available to test against.</summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public const string BaseUrlVariable = "KYC_E2E_BASE_URL";

    public E2EFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(BaseUrlVariable)))
        {
            Skip = $"Set {BaseUrlVariable} (for example http://localhost:8080) to run end-to-end tests against a running stack.";
        }
    }
}
