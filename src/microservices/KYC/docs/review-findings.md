# Code review findings

Review of the original .NET 8 code, with what was done about each item. Nothing here was compiled in the authoring
environment; CI is the first real build (see [upgrade guide](upgrading-to-net10.md#verification-status)).

## Fixed {#fixed}

| Area | Finding | Change |
|---|---|---|
| Security | Passwords committed in `appsettings*.json` and compose files | Replaced by placeholders / `.env`. **History still contains them — rotate.** |
| Correctness | No input validation pipeline; invalid input reached the domain | `ValidationBehavior` + FluentValidation; `400` problem-details |
| Correctness | `Customer` accepted empty names and impossible birth dates | Invariants enforced in the aggregate; `Update` validates before mutating |
| Correctness | Handler ignored a failed save and still published an event | Returns an error result; event published only after a successful commit |
| Correctness | Broker outage failed an already-committed write | Logged; request still succeeds |
| Correctness | Read-model cache invalidation was commented out | Re-enabled |
| Correctness | Three separate `NoSqlDbContext` instances | One singleton exposed through both interfaces |
| Correctness | Unique Mongo index on a non-unique field | Index kept, no longer unique |
| Correctness | EventStore repository mixed aggregates in one stream | One stream per aggregate (`customer-{id}`) |
| Privacy | Personal data written to logs | Removed |
| Reliability | `EnableRetryOnFailure` conflicts with user-initiated transactions | Not used; reason documented in code |
| Reliability | `CacheOptions` unvalidated | Validated (> 0) |
| Operability | No health checks | `/health/live`, `/health/ready` |
| Operability | Raw exceptions leaked to clients | `IExceptionHandler` + problem-details |
| Dependencies | AutoMapper, Newtonsoft.Json, Swashbuckle | Removed (manual mapping, System.Text.Json, built-in OpenAPI) |
| Build | Versions scattered across 7 csproj files | Central Package Management |
| Build | Solution referenced a non-existent project | New `KYC.slnx`, every path verified |
| Build | Docker/compose files scattered and inconsistent | Single `Dockerfile` (non-root) and `docker-compose.yml` |
| Quality | Duplicate EF `ApplyConfiguration` | Removed |
| Quality | Time read from `DateTime.Now` | `TimeProvider` injected |
| Quality | No tests | Unit, API, integration, E2E and load tests |

## Open {#open}

These were deliberately **not** changed because they need product or infrastructure decisions.

| # | Finding | Suggested action |
|---|---|---|
| 1 | **No authentication/authorization.** Anyone who can reach the API can create and read customers (PII). | Add JWT bearer auth and policies before any real deployment. |
| 2 | **No outbox.** A failed read-model projection or Kafka publish is only logged. | Transactional outbox + relay with retries ([ADR 0003](adr/0003-integration-event-publishing.md)). |
| 3 | `GET /api/customers` is unpaged. | Add `skip/take` or cursor paging; cap page size. |
| 4 | Hangfire serializes with `TypeNameHandling.All` (inside the `Mehedi.Hangfire.Extensions` package). | Avoid untrusted job payloads; review the package's serializer settings. |
| 5 | Only create and read endpoints exist; the domain supports update and delete. | Expose them when needed (events already exist). |
| 6 | Namespaces inconsistent in the Mongo project (`KYC.Read.Infrastructure.*` vs `KYC.Read.Mongo.Infrastructure.*`). | Rename in a dedicated, mechanical commit. |
| 7 | Mongo collection creation helper is never called (Mongo creates collections lazily). | Remove it or call it at startup. |
| 8 | The Hangfire dashboard is local-only. | Add an authorization filter for real environments. |
| 9 | The `Mehedi.*` packages are pinned at 1.0.0, the API they were written against. | Upgrade deliberately, one package at a time. |

## Corrections to earlier notes

Two statements made during the first pass of this review were wrong and are retracted:

- *"MediatR handlers are registered twice."* Not confirmed in the code; no change was needed.
- *"The authentication middleware is misordered."* There was no authentication middleware at all — the real finding is open item 1.
