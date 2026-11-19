# louis-agent.evals.protocol — design

> **Kind:** contracts library (`net10.0`) · **Path:** `src/evals/louis-agent.evals.protocol/` · **References:**
> louis-agent.choices only · **Used by:** louis-agent.evals (the writer), louis-agent.evals.checks (the kinds a grade
> carries), louis-agent.api.evals (the reader, the store and the export), louis-agent.web.evals (the live view), and
> louis-agent.api
> **Also shows:** `component`, `sequence`

![The eval protocol: one event per line, the run's and the conversation's events, and its choices as enums](diagrams/louis-agent.evals.protocol.svg)

## Purpose

When an eval runs, the runner writes what happens as it goes, and the API and the portal read it. This project is
the contract between them, version 2: the records the runner writes on stdout and the other side reads.

Both sides use the very same types. So if one side renames a field, the other side fails to compile, instead of
quietly showing an empty column. It's records and enums only, with no behaviour. It references nothing but
`louis-agent.choices`, so each side can take it without dragging in the others.

The rest of this doc goes through the contract in the order a run writes it: the line format, the run's own events,
each conversation's events, how words and fields are spelled, and what happens to a line that isn't an event.

*Decided in:* [ADR 0047](../adr/0047-protocol-v2-one-event-contract.md).

## How it works

### 1. One event per line

The runner writes JSON Lines on stdout, in UTF-8: one event per line. Every event is built on `EvalEvent`, the base
record. Its `t` is the time, in ISO 8601 UTC, written with a `Z` by `UtcZTimeConverter`. The `"event"` field comes
first, because that's what System.Text.Json reads to know which type it's looking at.

*Decided in:* [ADR 0046](../adr/0046-dates-are-utc.md).

### 2. The run's events

A run has four events of its own, in `Events/Run/`. `ProtocolEvent` is always the first line. It says which eval
this is, its version and commit, the graders, the models, the split and the dataset. `ModeEvent` and
`ModeDoneEvent` open and close each mode. `ModeDoneEvent` carries `RatesInfo`: pass@k, pass^k, k, and the
infrastructure failures and retries. `SummaryEvent` closes the run with the roll-up's lines.

### 3. The conversation's events

Inside a run, each conversation has four events too, in `Events/Conversations/`. `QuestionEvent` is the question
asked. `SseEvent` is one piece of a turn's stream. `TurnEvent` gives a turn's figures: `agent_calls`,
`specialist_calls`, the hosts, the seconds, and any error or failure. `ConversationEvent` closes it, with its
`GradeInfo`s, its `TrialOutcome` and its `Metrics`. `TurnKey` and `ConversationKey` name which turn and
conversation an event belongs to.

### 4. How words and fields are written

Words are kebab-case and fields are snake_case. Every choice is an enum, in `Kinds/`: `CheckKind`, `GradeOutcome`,
`TrialOutcome`, `TurnFailure`, `ModelLevel` and `RunSplit`. Each is written with its `[WireName]`. The field names
are our own, not OpenTelemetry's `gen_ai.*`.

*Decided in:* [ADR 0024](../adr/0024-choices-are-enums.md).

### 5. A line that isn't an event is for people

Not every line on stdout is an event. `EvalProtocolJson.Read` returns null for any line that isn't a JSON object
with a known `event`, and the live view just shows it as a log line. It's strict about the contract, so a missing
field, or a null where the record has none, isn't an event either. `Line` writes an event. Both use one options
object, so reading and writing always agree. Enums are read by their names only, never as numbers.

![The eval protocol: written as JSON lines, read by the API and the web](diagrams/louis-agent.evals.protocol-diagrams.svg)

![The order of a run's lines: the protocol event first, then each mode's conversations, and the summary last](diagrams/louis-agent.evals.protocol-diagrams-2.svg)

These are the types involved:

![The types: every line is one EvalEvent record, written and read through one options object](diagrams/louis-agent.evals.protocol-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `EvalEvent.cs`, `EvalProtocolJson.cs`, `UtcZTimeConverter.cs` | The base record, the reader and writer, the time |
| `Events/Run/`, `Events/Conversations/` | The eight events |
| `Parts/Run/`, `Parts/Conversations/` | The parts they carry (grader, model, dataset, rates, counts; keys, grade, metrics) |
| `Kinds/` | The six enums |

## Extending it

A new field is added to its record, and both sides get it at once; the build tells you where it's used. When an
event's shape changes, bump `ProtocolEvent.CurrentVersion`. The stored runs of the old version are then deleted when
the store opens. There's no second parser for old runs.

*Decided in:* [ADR 0049](../adr/0049-nothing-is-compatible-before-a-sale.md).

## Tests

The tests are in `src/tests/evals/louis-agent.evals.protocol.tests`. They check that every event round-trips
through `EvalProtocolJson`, that `event` comes first, that the time is written with a `Z`, and that a person's line
and an unknown event both read as none.

Two more tests pin the sides down. The harness's writer is tested in `louis-agent.evals.tests`
(`ProtocolWriterTests`), which reads each line back with the contract's own reader, so the tests hold the records and
never the JSON text. The API's recording of a run is tested in `RecordedV2RunTests`, in the Puls demo's
`louis-agent.api.evals.tests`.

## Limits and plans

The calibration report still travels as an `@` line, outside the contract. The harness writes it
(`CalibrationLine`, in `louis-agent.evals`), and the API reads it with a reader of its own
(`CalibrationLineReader`).

## Related docs

[ADR 0047](../adr/0047-protocol-v2-one-event-contract.md) ·
[ADR 0022](../adr/0022-benchmarks-recorded-from-the-protocol.md) ·
[louis-agent.evals](louis-agent.evals.md) · [louis-agent.api](louis-agent.api.md)

---
[Projects](README.md) · Previous: [louis-agent.evals.checks](louis-agent.evals.checks.md) · Next: [louis-agent.evals.runner](louis-agent.evals.runner.md)
