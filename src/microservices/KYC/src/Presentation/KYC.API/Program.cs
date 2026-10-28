using System.IO.Compression;
using Hangfire;
using Hangfire.PostgreSql;
using KYC.API;
using KYC.API.Health;
using KYC.API.Http;
using KYC.Application;
using KYC.EventStore.EventStoreDB.Infrastructure;
using KYC.Read.Mongo.Infrastructure;
using KYC.RedisCache.Infrastructure;
using KYC.Write.MsSql.Infrastructure;
using KYC.Write.MsSql.Infrastructure.Persistence;
using Mehedi.Hangfire.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The "Testing" environment is used by the in-process API tests; it has no Hangfire server or Postgres.
var isTesting = builder.Environment.IsEnvironment("Testing");

// ---- HTTP ------------------------------------------------------------------------------------
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

// ---- Application and infrastructure ----------------------------------------------------------
builder.Services.AddApplications();
builder.Services.AddEventBus(builder.Configuration);
builder.Services.AddWriteInfrastructureServices(builder.Configuration);
builder.Services.AddReadInfrastructureServices(builder.Configuration);
builder.Services.AddEventStoreInfrastructureServices(builder.Configuration);
builder.Services.AddCacheInfrastructureServices(builder.Configuration);

// ---- Health checks ---------------------------------------------------------------------------
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("sql", tags: ["ready"])
    .AddCheck<CacheHealthCheck>("redis", tags: ["ready"]);

// ---- Background jobs (Hangfire on PostgreSQL) -------------------------------------------------
if (!isTesting)
{
    var hangfireConnection = builder.Configuration.GetConnectionString("HangfireConnection")
        ?? throw new InvalidOperationException("Connection string 'ConnectionStrings:HangfireConnection' is not configured.");

    builder.Services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(hangfireConnection));
        config.UseMediatR(); // Custom extension: lets Hangfire serialize MediatR requests.
    });
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

// ---- Database migrations (opt-in) ------------------------------------------------------------
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// ---- Pipeline --------------------------------------------------------------------------------
app.UseExceptionHandler();
app.UseResponseCompression();

if (!app.Environment.IsProduction())
{
    // OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
}

if (!isTesting)
{
    // Only reachable from the machine running the API unless an authorization filter is configured.
    app.UseHangfireDashboard();
}

// Containers and the test host terminate TLS elsewhere.
if (!app.Environment.IsEnvironment("Docker") && !isTesting)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Liveness: the process is up. Readiness: its dependencies answer.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

/// <summary>Exposed so <c>WebApplicationFactory&lt;Program&gt;</c> can host the API in tests.</summary>
public partial class Program;
