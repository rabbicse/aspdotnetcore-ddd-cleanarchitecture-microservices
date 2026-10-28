# ADR 0004 — CQRS with an eventually consistent read model

**Status:** accepted

**Context.** Reads and writes have different shapes and load. SQL Server gives transactional writes; MongoDB plus
Redis gives cheap, denormalized reads.

**Decision.** Writes go to SQL Server. Domain events update MongoDB (and invalidate Redis) right after the save.
Queries read only MongoDB, through a cache-aside layer. Projection errors are logged and swallowed so they cannot undo
a committed write.

**Consequences.** Reads can lag or, on a projection failure, miss a customer until it is repaired. Document this for
API consumers (done in [api.md](../api.md#read)). Repair needs a replay tool or the outbox from ADR 0003.
