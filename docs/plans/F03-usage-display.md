# Plan: F3 Show usage

> **For:** [F3 Show usage](../features/F03-usage-display.md) · **Approved:** 2026-10-11

## Context

F1 records every model request and F2 prices it, but nobody sees it without running `usage-probe -- --ledger`. F3 shows
usage where people already work: a line under each answer (web app, CLI, optionally Rider), usage on the API's
`message_delta` event, a `/usage` command, and cost per fix and per run in the orchestrator's comms log (which the
demo prints at the end, so `run-demo.ps1` shows its cost too). It completes milestone M1 ("every request's tokens and
cost recorded and **visible**"). Stories: F3-S1 … S5 in the feature doc; spec: usage §7.1–7.4.

## Design (what changes from F3's doc, from what F1 and F2 built)

**Turn totals from the scope.** The doc says "`UsageScope` accumulates the turn's records". The cost is added after the
recorder (`PricingUsageSink`), so the scope must collect the *priced* record:
- `IUsageSink.Record` returns the record it stored (`PricingUsageSink` returns the priced one, `JsonlUsageSink` its
  input). The recorder (`UsageRecordingChatClient`) then calls `scope?.Add(stored)` on the scope the request started in.
- `UsageScope.Add` keeps the record and passes it to its outer scope, so a summary's nested scope still counts toward its
  turn. `UsageScope.Records` (thread-safe) and `Totals` reuse F2's `UsageReport.Total`: one totalling rule everywhere.
- Hosts keep a session list of records (the turn's added at the end), so session totals and `/usage` reuse
  `UsageReport.Total` / `Totals(…, r => r.Purpose)`.

**One text format, in core** (`usage/UsageText.cs`): `Line(turn, session)` →
`turn: 3 requests · 31k tokens (26k cached) · $0.006 · session $0.04`; unpriced → `price unknown`; `Summary(records)`
for `/usage` (totals by purpose: turn, continue, summary). The web app (Blazor WASM, no core reference) formats its own
shorter line from the stream's numbers: `1.9k in · 26k cached · 412 out · $0.004`.

**`/usage` in every host, not just the CLI:** `AgentEngine.HandleUserCommand(input, sessionUsage)` answers `/usage` from
the session records the host passes, like `/tools` (it never reaches the model). The CLI, the API (so the web app gets
it) and ACP each pass their own.

| Host | Shows | Where |
|---|---|---|
| API | `message_delta.usage`: `input_tokens`, `cache_creation_input_tokens`, `cache_read_input_tokens`, `output_tokens` (Claude's names; our four kinds match them), `cost`, `currency` (null when unpriced); `GET /sessions/{id}` adds the session's `usage` | `ClaudeStyleStream.Stop(stopReason, usage)`; `AgentSession` keeps records |
| Web | the line under each answer; the chat's total in the header; kept with the chat (`localStorage`) so a reload keeps it | `Models.cs` (`Turn.Usage`), `ChatPage.Apply` (`message_delta`), `TurnView`, header |
| CLI | the line after each answer, on **stderr** in grey (stdout stays the answer alone, for scripts using `RunSinglePromptAsync`) | `AgentEngine.RunAsync` / `RunSinglePromptAsync` |
| Rider | an optional final `agent_message_chunk` with the line when `USAGE_IN_CHAT=true` (off by default) | `AcpServer`, `AgentOptions.UsageInChat` |
| Orchestrator | per louis-agent reply: tokens and cost; per error: louis-agent, triage and total; run summary gains *Tokens* and *Cost* columns and a run total; `fixes.jsonl` and `decisions.jsonl` carry tokens and cost | `LouisAgentClient` parses `usage` into `AgentReply.Usage`; triage totals from the error's `UsageScope`; `CommsLog`, `ServiceTools`, `Program.cs` |

## Steps (each one commit with its test; stop for review after each)

Branch `feature/f3-usage-display` off `main`.

0. **Plans in the repo** (asked for while planning): `docs/plans/` with this plan and F1's and F2's, and the docs that
   point to it (`docs/CLAUDE.md`, the docs index, the features and fixes READMEs). *Check:* links resolve.
1. **Turn totals.** `IUsageSink.Record` returns the stored record; `UsageScope.Add` / `Records` / `Totals` (propagates to
   the outer scope); the recorder adds the priced record. *Tests:* a turn's totals include its rounds and its summary,
   not other turns; streaming and cancelled requests count; the priced cost reaches the scope.
2. **`UsageText` + `/usage`.** The line, "price unknown", k/M formatting, the per-purpose summary;
   `HandleUserCommand("/usage", records)`. *Tests:* exact strings; `/usage` never reaches the model; no records → a
   "nothing yet" reply.
3. **CLI.** Session records in `RunAsync`; the line after each answer on stderr; `/usage`. *Test:* the session list sums
   turns; a manual run on Ollama (free).
4. **API.** `message_delta.usage`, `GET /sessions/{id}` usage, `/usage` with the session's records. *Check by hand*
   (the API has no tests): curl on the Ollama profile shows the fields; an unpriced model gives `cost: null`.
5. **Web app.** `Turn.Usage`, `Apply`, the `TurnView` line, the header total, survives a reload. *Check by hand* in the
   browser.
6. **Rider.** `USAGE_IN_CHAT`. *Test:* the setting parses; manual in Rider.
7. **Orchestrator.** `AgentReply.Usage`; the comms log per reply, per error and in the run summary; the jsonl fields.
   *Tests:* `LouisAgentClientTests` parses `usage` from a stream; a `CommsLog` test checks the new columns and run total.
8. **Docs, the demo check, finish F3.** README, quick start, AGENT_COMMUNICATION (`usage` on `message_delta`), SETUP
   (`USAGE_IN_CHAT`), the orchestrator and web design docs. Then, **with approval** (≈ $0.30), a `-Short` demo whose comms
   log run total equals `usage-probe -- --ledger` for that run (F3's *Done when*), saved as the short sample. Finish per
   the checklist: TODO, status, spec map, GOAL map (skills 13 and 17: usage visible; M1 complete).

## Approved during the build (2026-10-11)

- **F14 Display currency** written as a feature doc for after F3 (not built in F3).
- **[FX6](../fixes/FX06-usage-on-failed-turns.md) built as part of F3**, before step 8: a failed turn's usage on the
  API's `error` event, read by the web app and the orchestrator, so the demo's "comms log total equals the ledger"
  check can't fail just because a reply failed.

## Verification

- `dotnet test louis-agent-solution.sln` (core, orchestrator, order-service) after every step.
- Free manual checks on the Ollama profile: the CLI line and `/usage`; the API's `message_delta.usage` via curl; the web
  app's line, header total and reload.
- Paid, only with approval: one `-Short` demo (≈ $0.30), whose comms log shows cost per fix and a run total equal to the
  ledger's total for that run.

## Concepts

Making cost visible at the point of decision; one number, one totalling rule (`UsageReport.Total`) across every view so
they can't disagree; Claude-compatible event fields so any Claude client can read them. Skill map #13 (cost and token
economics) and #17 (observability).

---
[Plans](README.md) · Previous: [F2](F02-prices-and-cost.md)