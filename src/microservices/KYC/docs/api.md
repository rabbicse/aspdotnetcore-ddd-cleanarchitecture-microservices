# API reference

Base URL when running with Docker: `http://localhost:8080`. The machine-readable contract is served at
`/openapi/v1.json` (built-in .NET OpenAPI; paste it into any OpenAPI viewer).

## Endpoints

| Method | Path | Success | Errors |
|---|---|---|---|
| `POST` | `/api/customers` | `201` + `Location`, body `{ "id": "<guid>" }` | `400`, `415`, `500` |
| `POST` | `/api/customers/async` | `202`, body `{ "jobId": "<id>" }` | `400` |
| `GET` | `/api/customers/{id}` | `200` customer | `404` |
| `GET` | `/api/customers` | `200` array of customers | — |
| `GET` | `/health/live` | `200` | — |
| `GET` | `/health/ready` | `200` | `503` |

## Create a customer

Fields: `firstName` and `lastName` (required, max 100 chars), `dob` (required, between 1900-01-01 and today),
`customerAddress` (optional; `street`, `city`, `state`, `country`, `zipCode`, each max 200 chars).

```bash
curl -i -X POST http://localhost:8080/api/customers \
  -H 'Content-Type: application/json' \
  -d '{
        "firstName": "Ada",
        "lastName": "Lovelace",
        "dob": "1990-05-17",
        "customerAddress": { "street": "12 St James Square", "city": "London", "country": "United Kingdom", "zipCode": "SW1Y 4JH" }
      }'
```

```http
HTTP/1.1 201 Created
Location: http://localhost:8080/api/Customers/3f2c9d0e-6c8a-4a0b-9b1e-2f6f1d7a5c11
Content-Type: application/json

{ "id": "3f2c9d0e-6c8a-4a0b-9b1e-2f6f1d7a5c11" }
```

## Create in the background

Validates immediately, then queues the work in Hangfire. Use it when the caller doesn't need the id right away.

```bash
curl -i -X POST http://localhost:8080/api/customers/async \
  -H 'Content-Type: application/json' \
  -d '{"firstName":"Grace","lastName":"Hopper","dob":"1985-12-09"}'
# HTTP/1.1 202 Accepted   {"jobId":"42"}
```

The job and its retries are visible on the Hangfire dashboard at `/hangfire` (local requests only).

## Read

```bash
curl http://localhost:8080/api/customers/3f2c9d0e-6c8a-4a0b-9b1e-2f6f1d7a5c11
curl http://localhost:8080/api/customers
```

Reads come from MongoDB through Redis, so a customer may take a moment to appear after creation
(see [consistency](architecture.md#consistency)).

## Errors

All errors are `application/problem+json` ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)).

Validation failure (`400`):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FirstName": ["'First Name' must not be empty."],
    "Dob": ["Date of birth cannot be in the future."]
  }
}
```

Not found (`404`): `{ "title": "Not Found", "status": 404, ... }`.
Unexpected failure (`500`): a generic problem document with a `traceId`; details are only in the server log, never in the response.

## Breaking changes versus the .NET 8 version

- `GET /api/customers` now returns a plain JSON array. Previously it returned a `Result` wrapper object.
- Error bodies are problem-details documents, not ad-hoc objects.
- Swagger UI (Swashbuckle) is gone; use `/openapi/v1.json`. Point Scalar, Swagger UI or Postman at it if you want a UI.

Runnable versions of these calls live in [`KYC.API.http`](../src/Presentation/KYC.API/KYC.API.http) (VS Code REST Client / Rider / Visual Studio).
