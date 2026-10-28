# Configuration

ASP.NET Core configuration layers, later wins: `appsettings.json` → `appsettings.{Environment}.json` →
user-secrets (Development) → **environment variables** → command line.
In environment variables, `:` becomes `__` (`ConnectionStrings__SqlConnection`).

## Settings

| Key | Purpose | Example |
|---|---|---|
| `ConnectionStrings:SqlConnection` | SQL Server write model (**required**) | `Server=localhost,1433;Database=Kyc;User Id=sa;Password=...;TrustServerCertificate=True;` |
| `ConnectionStrings:NoSqlConnection` | MongoDB read model | `mongodb://localhost:27017` |
| `ConnectionStrings:CacheConnection` | Redis | `localhost:6379` |
| `ConnectionStrings:EventStoreDbConnection` | EventStoreDB | `esdb://localhost:2113?tls=false` |
| `ConnectionStrings:Kafka` | Kafka bootstrap servers | `localhost:29092` |
| `ConnectionStrings:HangfireConnection` | PostgreSQL for Hangfire (not needed in `Testing`) | `Host=localhost;Port=5432;Database=hangfire;Username=postgres;Password=...;` |
| `eventsTopicName` | Kafka topic for integration events | `events` |
| `CacheOptions:AbsoluteExpirationInHours` | Hard cache lifetime (must be > 0) | `2` |
| `CacheOptions:SlidingExpirationInSeconds` | Sliding cache lifetime (must be > 0) | `60` |
| `Database:MigrateOnStartup` | Apply EF migrations at startup | `true` in Development/Docker, `false` otherwise |

A missing required setting fails fast at startup with a message that names the key.

## Secrets

- Nothing sensitive is committed. `appsettings.Development.json` contains `CHANGE_ME` placeholders on purpose.
- Docker: passwords come from `.env` (copy `.env.example`; `.env` is git-ignored).
- Local runs: `dotnet user-secrets` (see [getting started](getting-started.md#option-b--run-the-api-from-your-ide-dependencies-in-docker)).
- Real deployments: your platform's secret store, mapped to environment variables.

## Environments

| Environment | Used by | Notes |
|---|---|---|
| `Development` | IDE runs | Migrates on startup; OpenAPI enabled |
| `Docker` | `docker compose` | Container hostnames, migrates on startup, no HTTPS redirect |
| `Testing` | `KYC.API.Tests` | Dummy connection strings; Hangfire disabled; infrastructure replaced by fakes |
| anything else | production | Nothing migrated automatically |
