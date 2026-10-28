# 0049. Nothing is backward compatible until something is sold in production: delete and re-run

**Status:** Accepted (Louis, 2026-10-28, after the review of 3a) · **Date:** 2026-10-28 (Louis, watching 7c step
3a keep an old field name alive: "at this point why do we care about the data, this is not a production env"; "we
should make it a rule until some sort of release"; "we are a startup"; "we do not care until something is sold in
prod") · **Recorded:** 2026-10-28

## Context

Nobody outside this repo runs louis-agent yet: Puls has no users, no configured agent is hosted, and the engine
package does not exist. Still, each step keeps paying for a past it has no customer for: 7b kept a `puls_calls`
alias beside `agent_calls`, 7c's first draft kept a v1 parser, ADR 0048's draft promised that documents written by
the old mapper keep opening, and the turn reader read an unknown failure name as "no failure" instead of refusing it.
Each of these is a second code path, a second test and a review question, for data that a `docker compose down -v`
and a re-run replace in minutes.

The field's rule is the opposite only once something ships: Microsoft's .NET guidance on breaking changes applies to
released APIs; semantic versioning starts counting at 1.0, and before it "anything may change at any time". Until
then the cheap, honest move is to change the shape and throw the old data away.

## Decision

- **Until something is sold and running in production, nothing we store or send between our own parts has to read
  an older shape:** no migration, no alias for an old field name, no lenient reader, no second parser, no "keep
  both until". A changed shape means the old data is deleted and made again (an eval re-run, a replay re-recorded,
  a database dropped).
- **What counts:** the eval store and every MongoDB collection, the replay files, the protocol's lines, the API's
  contract records and the stream, the golden files (regenerated, reviewed in their diff), settings names.
- **What does not:** the usage ledger's past rows, which are measurements and keep their shape and their dated prices
  ([ADR 0007](0007-measure-every-request.md)); a published pull request's history; anything a third party reads
  (an MCP client, a provider's API).
- **Expand-and-contract stays for code that two of our own parts share in one build** (a contract record the API
  and the portal both reference), because that is about the build, not stored data; it ends in the same step list,
  never "later".
- **The first sale ends this ADR:** the phase that first hosts Puls or a configured agent for a paying customer
  (Phase 9, *What a sale needs*) writes the ADR that replaces it, with versioning and migration rules; the engine
  package (Phase 10) gets its own, semantic versioning from 1.0.

## Consequences

- Steps get shorter: a shape change is one commit, and the review has no compatibility branch to read.
- A developer who changes a stored shape runs `docker compose down -v` (or drops the `evals` database) and re-runs;
  the eval runner and the replay recorder make the data again, on Louis's go where a run is paid.
- Until the first sale, a stored run from an older build may simply fail to load, with the enum, value and property
  named ([ADR 0024](0024-choices-are-enums.md)); that is the intended signal, not a bug.
- ADR 0048's consequence about old documents is withdrawn; 7c's "v1 runs are discarded" is this rule's first case.

## Links

[0007](0007-measure-every-request.md) · [0024](0024-choices-are-enums.md) · [0048](0048-mongo-through-records.md) ·
[F18 plan](../plans/F18-evals.md), 7c ·
[Semantic Versioning 2.0.0, item 4](https://semver.org/#spec-item-4) ·
[.NET breaking changes, the rules for shipped APIs](https://learn.microsoft.com/dotnet/core/compatibility/)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
