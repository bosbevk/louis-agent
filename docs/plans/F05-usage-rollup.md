# F5 follow-up · Roll a sub-session's usage up into the parent's totals: plan

> **For:** the TODO's *F5 follow-up: roll sub-session usage up into the parent chat* · **Branch:**
> `feature/F5-toolset-profiles` · **Approved:** 2026-10-13 · **Effort:** about half a day, three small steps

## Context

A chat that delegates (Puls, or any session with the `agents` toolset) shows a usage line per answer, a chat total, and
answers `/usage` and `GET /sessions/{id}`. All of them count only the chat's **own** model requests: the specialist's
requests run in another process (the peer API) and are recorded in the ledger there, linked by `parent`, but never added
to the parent's totals. So a delegated answer looks almost free in the chat.

**Goal:** every total a host shows for a turn or a session includes its sub-sessions, while the ledger stays exactly as it
is (the specialist's requests are already in it; nothing may be written twice).

## What the code already does (read these first)

- `louis-agent.core/Usage/UsageScope.cs`: `Add(UsageRecord)` keeps a record **in memory** and passes it to the outer
  scope. It does **not** write to the ledger.
- `louis-agent.core/Llm/UsageRecordingChatClient.cs` (around line 99): the ledger write happens here, *before*
  `scope?.Add(stored)`. That is the only place records reach the ledger.
- Every view adds up `UsageScope.Records` (or a session's records, built from them):
  - `UsageReport.Total(key, records)` in `louis-agent.core/Usage/UsageReport.cs` (requests = record count; tokens and cost
    summed; `unpriced` = records without a cost);
  - `UsageText.Line(records, …)` (the CLI and Rider line);
  - `UsageSession.EndTurn(records)` (`/usage`, `GET /sessions/{id}`);
  - `ClaudeStyleStream.Usage(records, display)` in `src/louis-agent.api/Program.cs` (the `message_delta` usage the web
    apps show).
- `louis-agent.core/Tools/Agents/AgentPeerTools.cs`: `StartSubSession` already reads the child's usage from its
  `message_delta` (and from its `error` event) in `ReadStreamAsync`, as a JSON object with `requests`, `input_tokens`,
  `cache_creation_input_tokens`, `cache_read_input_tokens`, `output_tokens`, `cost`, `currency`, `unpriced`.

## Design

**Add the sub-session's total to the parent turn's scope as one summary record.** Because every view totals the scope's
records, they all include it with no change; because `UsageScope.Add` never writes the ledger, nothing is duplicated.

- New purpose `UsagePurpose.SubSession = "sub_session"`.
- `UsageRecord` gets an optional `int? Requests = null` (how many model requests a summary record stands for; null means
  1). It must **not** appear in ledger lines: mark it `[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`
  so real records are written exactly as today.
- `UsageReport.Total` counts requests as `Sum(r.Requests ?? 1)` and unpriced as the same weight over records without a
  cost. Nothing else in it changes.
- In `AgentPeerTools.StartSubSession`, after the child's stream is read (success **or** failure, whenever a usage object
  arrived), call `UsageScope.Current?.Add(record)` with:
  - `Purpose: UsagePurpose.SubSession`, `Round: 0`, `Host`, `Session`, `Turn` and `Tags` from the current scope,
  - `Model: $"sub-session:{agent}"`, `Stop: "sub_session"`, `DurationMs`: the call's wall time,
  - `Tokens: new UsageTokens(input, cacheWrite, cacheRead, output, null)` from the child's usage,
  - `Cost: cost is null ? null : new UsageCost(currency, cost, null)`, `Requests: requests`.

## Traps (check each one)

1. **Never write the summary to the ledger.** Use `UsageScope.Add`, never a sink. Test it (step 3).
2. **The charged cost, not the display cost.** The child's usage object has both `cost`/`currency` (what was charged,
   the ledger's currency) and `display_cost`/`display_currency` (converted for showing). Use `cost` and `currency` only;
   the parent's views convert for display themselves.
3. **Add to the scope that is current when the tool runs.** It is the parent turn's scope (it flows into the tool call
   through `AsyncLocal`, as the existing `parent` tag does). Don't open a new scope for it.
4. **A failed or timed-out child.** If its `error` event carried usage, add it (those requests were charged). If no usage
   arrived (a timeout before any reply), add nothing: never a zero record.
5. **Request counts.** The summary is one record but stands for `requests` requests; without `Requests`, a turn would say
   "2 requests" when it made 1 + 3.

## Steps (each one commit with its test; run `dotnet test louis-agent-solution.sln` after each)

1. **Weighted totals.** `UsagePurpose.SubSession`; `UsageRecord.Requests` (ignored when null in JSON); `UsageReport.Total`
   weighs requests and unpriced by it. *Tests* (`tests/louis-agent.core.tests/Usage/UsageReportTests.cs`): a summary
   with `Requests = 3` plus one normal record totals 4 requests and adds tokens and cost; an unpriced summary counts its
   requests as unpriced; a normal record serialised with `JsonlUsageSink.Json` has **no** `requests` field (the ledger
   format is unchanged).
2. **Roll up in the tool.** `AgentPeerTools` builds the summary record from the child's usage and adds it to
   `UsageScope.Current`. *Tests* (`tests/louis-agent.core.tests/Tools/Agents/AgentPeerToolsTests.cs`, with the existing
   stub peer): inside `UsageScope.Begin(session: "sess_parent", turn: 2)`, after one sub-session the scope's
   `Total()` has the child's 3 requests and `$0.0123`; a child whose message fails with usage in its `error` event is
   still added; a timeout adds nothing; outside a scope nothing throws.
3. **The views agree, and the ledger doesn't double-count.** *Tests*: `UsageSession.EndTurn` and `UsageText.Line` on a
   turn's records that include a summary show own + child; a turn run through an engine with a `ListUsageSink`
   (`tests/louis-agent.core.tests/TestHelpers.cs`) and a stubbed sub-session writes **only the parent's own** records to
   the sink, while the scope's total includes the child.

## Done when

- A delegated answer's usage line, the chat total, `/usage` and `GET /sessions/{id}` include the specialist's requests,
  tokens and cost.
- The ledger has the same rows as before (the specialist's in its own records, linked by `parent`; no summary rows).
- All suites pass; the TODO's follow-up line is ticked, with a line under F5's *Approved during the build*.

## By hand (costs a few cents)

Puls on 5070 (`docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build`): delegate one
task; the answer's usage line should now be about the sum of the chat's line and the card's line, and
`logs/puls/usage-*.jsonl` should have no `sub_session` rows.

## Approved during the build (2026-10-14)

- **Branch:** `feature/F5-usage-rollup` off `main`; the plan's branch was merged before the build started.
- **The roll-up is written to the parent's ledger too.** Louis: "we should still keep a separate specialist record,
  I'm just thinking we might need it for future" (a per-session cost that includes delegation: credits, F23;
  reconciliation, F11). So the record goes through the engine's sink and then into the scope, as
  `UsageRecordingChatClient.Store` does, instead of the scope only; `requests` is written on that row (the
  `JsonIgnore` keeps it off every other row). The tool gets the sink through `ToolsetContext.UsageSink`. *Trap 1* and
  the *By hand* check change accordingly: the ledger has **one `sub_session` row per sub-session reply**, next to the
  specialist's own rows when peers share a ledger (Puls and its specialist both mount `logs/puls`).
- **Ledger reports list roll-up rows without totalling them.** The requests a roll-up stands for are real rows in a
  ledger (the same file for Puls), so the usage-probe's `--ledger` totals skip `sub_session` and print the roll-ups
  on their own line, and `CacheGaps` ignores them. `UsageReport.Total` stays purpose-blind: a turn's line counts its
  roll-up, a filtered ledger report doesn't, with one adder.
- **The sub-session prompt budget** (`ToolCallGuards.KeepWithinPromptBudget`, FX14) skips `sub_session` records as it
  skips summaries. It only runs inside a sub-session, which can't delegate, so this states the rule rather than
  fixing a live bug.
