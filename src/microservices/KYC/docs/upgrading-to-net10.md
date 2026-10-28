# Upgrading from .NET 8 to .NET 10

## What changed

| Topic | Before | After |
|---|---|---|
| Target framework | `net8.0` per project | `net10.0` once, in `Directory.Build.props` |
| SDK | whatever is installed | pinned in `global.json` (10.0.100, roll forward to newer feature bands) |
| Package versions | in each csproj | `Directory.Packages.props` (Central Package Management) |
| Solution | `KYC.sln` | `KYC.slnx` (needs the .NET 10 SDK / current IDEs) |
| API docs | Swashbuckle | built-in `AddOpenApi()` / `MapOpenApi()` |
| Errors | ad hoc | `ProblemDetails` + `IExceptionHandler` |
| Time | `DateTime.Now/UtcNow` | `TimeProvider` |
| Mapping/serialization | AutoMapper, Newtonsoft.Json | explicit mapping, System.Text.Json |
| EF Core | 8.x | 10.0 |
| Container images | `dotnet/*:8.0` | `dotnet/*:10.0`, non-root user |

## Breaking changes

1. `GET /api/customers` returns an array instead of a `Result` wrapper ([API notes](api.md#breaking-changes-versus-the-net-8-version)).
2. `KYC.sln` and the `docker-composes/` folder are removed; use `KYC.slnx` and `docker-compose.yml`.
3. The stack needs a `.env` file (`cp .env.example .env`).
4. The old `.dockerignore` was replaced by a stricter one.
5. Read model: the Mongo index is no longer unique. Drop the old index if one exists in your database:
   `db.customers.dropIndexes()` and let the app recreate it.
6. EventStoreDB: events are now written to `customer-{aggregateId}` streams, with the event type name as the type
   and the serialized event as payload. Existing history in the old layout isn't migrated.
7. Swagger UI is gone; use `/openapi/v1.json`.

## Migration checklist for your environments

- [ ] Install the .NET 10 SDK (or rely on the containers).
- [ ] Rotate any password that ever appeared in this repository's history.
- [ ] Provide secrets via environment variables / `.env` / user-secrets.
- [ ] Apply the (unchanged) EF migrations; keep `Database:MigrateOnStartup=false` outside dev.
- [ ] Update any client that consumed the list endpoint.
- [ ] Update dashboards/probes to `/health/live` and `/health/ready`.

## Verification status {#verification-status}

The upgrade was authored in an environment with no .NET SDK and no NuGet access, so it was written conservatively
against the API of the pinned package versions but **not compiled or run there**. The first GitHub Actions run is
the real build. Expect to fix a small number of compile or version issues; the usual suspects are:

- members of the `Mehedi.*` 1.0.0 packages (event-store types, unit of work, NoSQL abstractions),
- EF Core 10 against the model snapshot (the `up-to-date` unit test will say so),
- package versions that may not exist on NuGet.org (Testcontainers, Polly, test SDKs),
- the MongoDB driver (2.30.0) with the Mehedi packages built against an older one.
