# ADR 0002 — Validate in the MediatR pipeline; report errors as problem-details

**Status:** accepted

**Context.** Validators existed but were never executed, so invalid input reached the domain, and failures surfaced as
unstructured 500s.

**Decision.** Run all FluentValidation validators in a `ValidationBehavior` before any handler. Convert exceptions to
RFC 9457 `application/problem+json` in one `IExceptionHandler`. Use `Result<T>` for expected outcomes (not found,
save failed). The aggregate still enforces its own invariants as a second line of defense.

**Consequences.** Handlers can assume valid input; every use case gets validation for free (including background
jobs, except that `/async` also validates up front so the caller sees errors). Clients get one consistent error shape.
