# ADR 0003 — Publish integration events after commit; tolerate broker outage

**Status:** accepted for now; superseded by an outbox when one is built

**Context.** Publishing before the SQL commit can announce data that never existed; failing the request after the
commit tells the client a stored customer was not created.

**Decision.** Publish after a successful commit. If the broker is down or returns failure, log and return success.

**Consequences.** No phantom events. A customer may exist without its event ever being published (at-most-once).
The proper fix is a transactional outbox: persist the event with the entity, deliver from a relay with retries.
This is tracked as open item 2 in the [review findings](../review-findings.md#open).
