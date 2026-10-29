# louis-agent.evals.protocol — design

> **Kind:** class library (`net10.0`) · **Path:** `evals/louis-agent.evals.protocol/` · **References:**
> louis-agent.choices only · **Used by:** louis-agent.evals (the writer), louis-agent.api (the reader and the
> export), louis-agent.web.shared and louis-agent.puls (the live view)

## Purpose

The eval protocol's contract, version 2: the records the runner writes on stdout and the API and the portal read
([ADR 0047](../adr/0047-protocol-v2-one-event-contract.md)). One set of types on both sides, so a field one side
renames is a compile error on the other, not a silent empty column. Records and enums only, no behaviour; it
references only `louis-agent.choices`, so each side takes it without the others.

## How it works

- **One event per line.** JSON Lines on stdout, UTF-8. `EvalEvent` is the base record with `t` (an ISO 8601 UTC time,
  written with `Z` by `UtcZTimeConverter`, [ADR 0046](../adr/0046-dates-are-utc.md)); `"event"` comes first and is
  System.Text.Json's type discriminator.
- **The run's events** (`Events/Run/`): `ProtocolEvent` (the first line: the eval, its version and commit, the graders,
  the models, the split, the dataset), `ModeEvent`, `ModeDoneEvent` (with `RatesInfo`: pass@k, pass^k, k, the
  infrastructure failures and retries) and `SummaryEvent` (the roll-up's lines).
- **The conversation's events** (`Events/Conversations/`): `QuestionEvent`, `SseEvent` (a piece of a turn's stream),
  `TurnEvent` (its figures: `agent_calls`, `specialist_calls`, hosts, seconds, error, failure) and
  `ConversationEvent` (its `GradeInfo`s, its `TrialOutcome` and its `Metrics`). `TurnKey` and `ConversationKey` name
  them.
- **Words are kebab-case, fields snake_case.** Every choice is an enum in `Kinds/` (`GraderKind`, `GradeOutcome`,
  `TrialOutcome`, `TurnFailure`, `ModelLevel`, `RunSplit`), written with its `[WireName]`
  ([ADR 0024](../adr/0024-choices-are-enums.md)). The field names are our own, not OpenTelemetry's `gen_ai.*`.
- **A line that is no event is a line for people.** `EvalProtocolJson.Read` returns null for a line that isn't a JSON
  object with a known `event`; the live view shows it as a log line. `Line` writes an event; both use one options
  object.

## Structure

| Path | What's in it |
|---|---|
| `EvalEvent.cs`, `EvalProtocolJson.cs`, `UtcZTimeConverter.cs` | The base record, the reader and writer, the time |
| `Events/Run/`, `Events/Conversations/` | The eight events |
| `Parts/Run/`, `Parts/Conversations/` | The parts they carry (grader, model, dataset, rates, counts; keys, grade, metrics) |
| `Kinds/` | The six enums |

## Extending it

A new field is added to its record on both sides at once (the build says where). A changed shape bumps
`ProtocolEvent.CurrentVersion`, and the stored runs of the old version are deleted when the store opens
([ADR 0049](../adr/0049-nothing-is-compatible-before-a-sale.md)): no second parser.

## Tests

`tests/louis-agent.evals.protocol.tests`: every event round-trips through `EvalProtocolJson`, `event` comes first,
the time is written with `Z`, a person's line and an unknown event read as none. The harness's writer is pinned to a
golden in `louis-agent.evals.tests`, and the API's reader in its `RecordedV2RunTests`.

## Limits and plans

- The calibration report still travels as an `@` line outside the contract (`CalibrationLine`).

## Related docs

[ADR 0047](../adr/0047-protocol-v2-one-event-contract.md) ·
[ADR 0022](../adr/0022-benchmarks-recorded-from-the-protocol.md) ·
[louis-agent.evals](louis-agent.evals.md) · [louis-agent.api](louis-agent.api.md)

---
[Projects](README.md) · Previous: [louis-agent.evals](louis-agent.evals.md) · Next: [louis-agent.evals.runner](louis-agent.evals.runner.md)
