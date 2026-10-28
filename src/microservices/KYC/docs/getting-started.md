# Getting started

## Prerequisites

| To do this | You need |
|---|---|
| Run the whole stack | Docker with Compose v2 |
| Build and test from the command line | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see `global.json`) |
| Run the integration tests | Docker running (Testcontainers starts the databases) |
| Run the load tests | Docker, or a local [k6](https://k6.io/docs/get-started/installation/) |

## Option A — everything in Docker (recommended first run)

```bash
cd src/microservices/KYC
cp .env.example .env     # set MSSQL_SA_PASSWORD and POSTGRES_PASSWORD
docker compose up --build
```

The first start takes a few minutes (images, restore, build). When the API container reports healthy:

| What | Where |
|---|---|
| API | <http://localhost:8080> |
| OpenAPI document | <http://localhost:8080/openapi/v1.json> |
| Liveness / readiness | <http://localhost:8080/health/live> · <http://localhost:8080/health/ready> |
| EventStoreDB UI | <http://localhost:2113> |
| SQL Server / MongoDB / Redis / PostgreSQL | `localhost:1433` / `27017` / `6379` / `5432` |
| Kafka (from the host) | `localhost:29092` |

The API applies EF Core migrations on startup (`Database:MigrateOnStartup=true` in the `Docker` environment), so
the SQL database is created for you.

Stop and keep the data: `docker compose down`. Stop and **delete** the data: `docker compose down --volumes`.

## Option B — run the API from your IDE, dependencies in Docker

Useful for debugging.

```bash
cd src/microservices/KYC
cp .env.example .env
docker compose up -d mssql mongo redis postgres kafka eventstore      # everything except the API
```

Give the API the two passwords it cannot find in `appsettings.Development.json` (they are `CHANGE_ME` there on purpose).
Use the same values as in `.env`:

```bash
cd src/Presentation/KYC.API
dotnet user-secrets set "ConnectionStrings:SqlConnection" \
  "Server=localhost,1433;Database=Kyc;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:HangfireConnection" \
  "Host=localhost;Port=5432;Database=hangfire;Username=postgres;Password=<POSTGRES_PASSWORD>;"
dotnet run --launch-profile http          # http://localhost:5085
```

Environment variables work too (`ConnectionStrings__SqlConnection=...`); they override every JSON file.

## Database migrations

The write model's schema is managed by EF Core migrations in `KYC.Write.MsSql.Infrastructure`.

```bash
dotnet tool restore            # or: dotnet tool install --global dotnet-ef
dotnet ef migrations add AddCustomerEmail \
  --project src/Infrastructures/KYC.Write.MsSql.Infrastructure \
  --startup-project src/Presentation/KYC.API
```

Run it with `ASPNETCORE_ENVIRONMENT=Development` so the startup project finds its connection strings. A unit test
(`WriteModelTests.The_migrations_are_up_to_date_with_the_model`) fails the build if you change the entity model
and forget to add a migration.

Migrations run automatically when `Database:MigrateOnStartup` is `true`. Leave it `false` in production and
apply migrations as a deployment step (`dotnet ef database update` or a migration bundle).

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `docker compose up` stops with *Set MSSQL_SA_PASSWORD in .env* | You skipped `cp .env.example .env`. |
| SQL Server container exits immediately | The password failed SQL Server's complexity rules (8+ characters with upper, lower, digit and symbol). |
| API container restarts, logs show a connection error | A dependency was not ready yet. `docker compose ps` shows health; the API waits for SQL Server, MongoDB, Redis and PostgreSQL. |
| `/health/ready` returns 503 | SQL Server or Redis is unreachable; the JSON body names which check failed. |
| `POST /api/customers` works but `GET` returns 404 right after | The read model (MongoDB) failed to update. Look for *Failed to project CustomerCreatedDomainEvent* in the API log; see [architecture](architecture.md#consistency). |
| Hangfire dashboard (`/hangfire`) is 403 from a container | By design it only serves requests from the local machine. Add a Hangfire authorization filter if you need remote access. |
| Integration tests fail with *Docker is either not running or misconfigured* | Start Docker Desktop / the Docker daemon. |
| Kafka connection errors on the host | Use `localhost:29092` from the host and `kafka:9092` from containers. |
