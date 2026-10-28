# Testing

```text
            ▲  few, slow     k6 load tests · End-to-end (compose stack)
           ╱ ╲
          ╱   ╲              Integration (real SQL Server, MongoDB, Redis via Testcontainers)
         ╱     ╲
        ╱       ╲            API tests (whole HTTP pipeline in memory, fakes for I/O)
       ╱         ╲
      ▼ many, fast           Unit: Domain · Application · Infrastructure logic
```

| Project | Needs Docker | Runs in CI | What it proves |
|---|---|---|---|
| `KYC.Domain.UnitTests` | no | yes | Aggregate invariants, address equality, domain events |
| `KYC.Application.UnitTests` | no | yes | Validators, handler behavior (incl. save failure, broker outage), cache-aside queries, validation pipeline |
| `KYC.Infrastructure.UnitTests` | no | yes | Read-model projection, DI wiring is valid, EF model matches migrations |
| `KYC.API.Tests` | no | yes | Status codes, problem-details, content negotiation, OpenAPI document, liveness |
| `KYC.Infrastructure.IntegrationTests` | **yes** | yes | EF mapping against real SQL Server, Mongo queries, Redis cache behavior |
| `KYC.EndToEndTests` | **yes** (stack) | yes | Create → read through the real running containers |
| `tests/load/k6` | stack | smoke only | Latency and error-rate thresholds |

## Running

```bash
dotnet test KYC.slnx --filter "Category!=Integration&Category!=E2E"    # seconds; no Docker
dotnet test tests/KYC.Infrastructure.IntegrationTests                   # Docker required
dotnet test KYC.slnx --collect:"XPlat Code Coverage"                    # coverage (coverlet)
```

### End-to-end

E2E tests skip themselves unless `KYC_E2E_BASE_URL` is set, so a plain `dotnet test` never fails for lack of a stack.

```bash
docker compose up --build -d
KYC_E2E_BASE_URL=http://localhost:8080 dotnet test tests/KYC.EndToEndTests
docker compose down --volumes
```

### Load tests (k6)

```bash
docker compose up --build -d
docker compose --profile loadtest run --rm k6                              # smoke
docker compose --profile loadtest run --rm -e SCENARIO=load k6             # steady load
```

Or with a local k6: `k6 run -e BASE_URL=http://localhost:8080 -e SCENARIO=stress tests/load/k6/customers.js`.

| Scenario | Shape | Use it to |
|---|---|---|
| `smoke` | 1 VU, 30 s | Check the script and the system work |
| `load` | ramp to 20 VUs, hold | Measure normal-traffic latency |
| `stress` | ramp to 100 VUs | Find where it degrades |
| `spike` | sudden 150 VUs | Check recovery from bursts |

Thresholds (failure rate and p95 latency) are defined in the script; k6 exits non-zero when one is broken, so it can gate a pipeline.
Run load tests against an environment that resembles production — numbers from a laptop are only relative.

## Writing tests

- Domain/Application: plain xUnit + NSubstitute. Use `FixedTimeProvider` instead of the system clock.
- New endpoint: add a case to `CustomersApiTests`; the factory (`KycApiFactory`) already replaces databases and the broker.
- New persistence behavior: add an integration test with the existing fixtures; tag the class
  `[Trait("Category", "Integration")]` so the fast filter excludes it.

## CI

`.github/workflows/kyc-ci.yml` runs on changes under `src/microservices/KYC`:
restore → build (warnings visible) → fast tests → integration tests → coverage artifact, then a second job that starts the
compose stack, runs the E2E tests and a k6 smoke test.
