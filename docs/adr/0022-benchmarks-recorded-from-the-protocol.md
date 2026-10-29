# 0022. Benchmark results in MongoDB, recorded by the API from the harness's stdout protocol

**Status:** Amended by [0029](0029-simulations-in-csharp.md) and [0047](0047-protocol-v2-one-event-contract.md)
(protocol v2: the events are one shared contract project, not the harness) · **Date:** 2026-10-17 (decided in the F31 plan, built in its steps 1–6)

## Context

Puls's checks (the cook check, the routing check) measure what a design change does to cost and quality. Before CL10
they were Python scripts run from a terminal, each writing its run into a folder of JSON files under `samples/`, and
computing its own costs from the usage ledger; the admin portal (F27) read those folders. Two copies of the cost
logic disagreed, a half-done run was lost, and nothing compared runs over time. Louis asked for the checks to run
only from the portal, their results kept in a store, with every level of a run readable.

Considered: the script writing the database itself (a driver and secrets in the script, a second schema in Python);
the script POSTing its results to the API (a second channel that can disagree with what the page streamed); SQL
Server (Louis chose MongoDB: a run is a tree of documents, conversation to turn to request); a per-event collection
(a turn's replay would be thousands of reads).

## Decision

- **The script speaks, the API records.** A check prints plain lines and `@{json}` lines on stdout, a versioned
  protocol (`@{"event":"protocol","version":1}`, then `mode`, `question`, `sse`, `turn`, `conversation`,
  `mode_done`), parsed by `BenchmarkProtocol`; a malformed line is kept as a log line, never a failure. The runner
  streams the lines to the page; `BenchmarkRecorder` reads the same lines and writes documents as they arrive, so the
  page and the store agree by construction and a half-done run is already data.
- **Costs come from the ledger, once.** At the end the recorder joins the usage ledger's rows for the run's tag to
  their turns (`BenchmarkLedger`), skipping sub-session roll-ups so delegations aren't counted twice. No check
  computes a cost.
- **The store is behind a contract.** `IBenchmarkStore` with an in-memory store and `MongoBenchmarkStore`
  (`MongoDB.Driver`, database `puls_benchmarks` on `puls-mongo`, its data on the host); one abstract contract test
  runs against both, the Mongo one skipping when `BENCHMARKS_MONGO_URL` isn't set (CI sets it with a throwaway
  server). Without a store the API starts no run: a paid run whose results vanish is the worst outcome.
- **Documents per level:** `runs` (settings, modes, total, log), `conversations` (scores), `turns` (question,
  answer, role, the turn's stream events for replay) and `requests` (one per ledger row). Trends are computed in C#
  over a check's runs, so they're the same on both stores.

## Consequences

- A new check is a script on the shared harness (`benchmarks/puls_check/`) and a catalogue entry; the portal, the
  store and the explorer need nothing new.
- The protocol is a contract between Python and C#: both readers are held to one recorded stream
  (`benchmarks/fixtures/session-stream.sse`); a change bumps its version.
- Scoring is deterministic code over the transcript (calls made, prices with a source), so a score can be recomputed
  for a stored run when the rule changes, as 11b's was.
- MongoDB is a container to run and back up (`data/mongo`); losing it loses the runs, not the ledger.

## Links

[F31 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F31-benchmarks-portal.md) · [F31 feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F31-benchmarks-portal.md) ·
[F27 feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F27-admin-portal.md) · [ADR 0007](0007-measure-every-request.md) (the ledger)
