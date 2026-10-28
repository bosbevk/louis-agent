# 0047. Protocol v2: one event contract both sides share

**Status:** Accepted · **Date:** 2026-10-28 (Louis, the 7c design cards, picks A to F; built in 7c steps 1 to 5) ·
**Recorded:** 2026-10-29 · Amends [0022](0022-benchmarks-recorded-from-the-protocol.md)

## Context

[ADR 0022](0022-benchmarks-recorded-from-the-protocol.md) made the eval runner speak and the API record: the runner
prints plain lines and `@{json}` event lines on stdout, and the API reads them into the store. Version 1 of that
protocol was written byte by byte by the runner and read by the API's own parser, field by field, with a frozen
Python-recorded fixture between them. Every field the harness added was spelled three times (the writer, the API's
reader, the portal's reader of the API's reply), PascalCase and snake_case words mixed on the wire, the times were
Unix seconds, and a typo showed only when a test read it back. Once step 7 deleted the Python, the C# runner was the
only writer, so the two sides could share one set of types instead of agreeing by hand.

Considered for the names: OpenTelemetry's GenAI semantic conventions (`gen_ai.*`, the field's shared words for model
calls), or names of our own.

## Decision

- **One event model, in a project of its own** (pick A): `evals/louis-agent.evals.protocol` holds the events and
  their parts as records and enums, references only `louis-agent.choices`, and is referenced by the harness, the
  runner, the API and the web apps' shared library. The harness writes the events, the API reads them, and the
  portal's live view reads the same records; the contract is not the harness. A shared Data Transfer Object
  contract (Fowler, PoEAA), as `louis-agent.api.contracts` is for the API's replies
  ([ADR 0045](0045-typed-answers-and-a-shared-contract.md)).
- **JSON Lines on stdout, `"event"` first** (pick B): one object per line, UTF-8, no `@` prefix. `"event"` is the
  type discriminator (`[JsonPolymorphic]`, one `[JsonDerivedType]` per event), written and read by
  `EvalProtocolJson`. A line that isn't an object with a known `event` member is a log line for people, never a
  failure of the run.
- **kebab-case for every word on the wire** (pick C: `fixed-rule`, `pass`, `api-error`), through each enum's
  `[WireName]` ([ADR 0024](0024-choices-are-enums.md)); field names stay snake_case.
- **Grades and metrics apart** (pick D): a conversation carries its `grades` (each with partial credit in `score`)
  and its `outcome`, and one `metrics` record (calls, hosts, turns, tokens, seconds, cost) that is measured and never
  fails a run. The per-eval score bags of v1 are gone.
- **Every event's time is `t`, ISO 8601 in UTC** ([ADR 0046](0046-dates-are-utc.md)), written with `Z`.
- **Version 1 is discarded, not migrated** (pick E): the API reads only v2, and the store deletes, when it opens,
  every run stored without `protocol_version: 2` and everything under it. It is
  [ADR 0049](0049-nothing-is-compatible-before-a-sale.md)'s first case.
- **The field names are our own** (pick F), not OpenTelemetry's `gen_ai.*`:
  - those conventions describe one model call as a span (its request, its response, its tokens); our events
    describe an eval: a run, its modes, conversations, turns, grades and roll-ups, which they have no words for;
  - the GenAI conventions are still marked experimental and change between releases, so borrowing them would tie
    our wire, our store and our goldens to a moving spec;
  - the model requests themselves are already measured by the usage ledger
    ([ADR 0007](0007-measure-every-request.md)); a later exporter can map a run's requests to `gen_ai.*` spans
    (F44's traces) without the protocol changing.

## Consequences

- A field is written once, as a property of a record; the runner, the API and the portal can't spell it apart, and a
  rename fails the build on both sides.
- The goldens (`stub-run.txt`, the conformance references, the export's round trip) are the contract's own lines;
  a change to the contract regenerates them and their diff is reviewed.
- Stored v1 runs and the v1 parser are gone; trends start again at v2 (the last v1 numbers are in the feature doc).
- Calibration reports still travel as their own `@` line (`CalibrationLine`), outside this contract, read by the
  API's `CalibrationLineReader`.

## Links

[0022](0022-benchmarks-recorded-from-the-protocol.md) · [0024](0024-choices-are-enums.md) ·
[0045](0045-typed-answers-and-a-shared-contract.md) · [0046](0046-dates-are-utc.md) ·
[0049](0049-nothing-is-compatible-before-a-sale.md) · [F18 plan](../plans/F18-evals.md), 7c ·
[OpenTelemetry GenAI semantic conventions](https://opentelemetry.io/docs/specs/semconv/gen-ai/) ·
[JSON Lines](https://jsonlines.org/)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
