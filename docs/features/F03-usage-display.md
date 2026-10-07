# F3 · Show usage: chat, CLI, Rider, API, comms log

> **Status:** planned · **Milestone:** M1 · **Depends on:** F1, F2
>
> **Spec:** [Usage §7.2–7.4, §7.1 (stream)](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F3 Show usage** under *Now* ([TODO](../../TODO.md))

Recording is only useful if people see it where they already work. F3 adds a usage line to every answer and cost per fix
to the orchestrator's comms log. (The Usage tab and reporting endpoints are F11.)

## User stories

**F3-S1 — Cost per answer in the web app.** As a *user*, I want to see what each answer used and cost, so that I learn
what's expensive.
- *Given* an answer finishes *then* under it: `1.9k in · 26k cached · 412 out · $0.004` (input includes cache writes;
  "cached" are cache reads).
- *Given* a chat *then* its header shows the chat's total so far; totals survive a page reload (stored with the chat).
- *Given* an unpriced model *then* the line shows tokens and "price unknown".

**F3-S2 — Usage in the API stream.** As a *developer* calling the API, I want usage in the event stream, so that my
client can show or record it.
- *Then* `message_delta` carries `usage`: `input_tokens`, `cache_creation_input_tokens`, `cache_read_input_tokens`,
  `output_tokens` (the turn's totals, Claude's field names) plus `cost` and `currency` when priced.
- *And* `GET /sessions/{id}` adds the session's totals.

**F3-S3 — CLI.** As a *CLI user*, I want a one-line summary after each answer and a `/usage` command, so that I see cost
without leaving the terminal.
- *Then* after each answer: `turn: 3 requests · 31k tokens (26k cached) · $0.006 · session $0.04` (grey, stderr-safe).
- *Given* `/usage` *then* the session's totals by purpose (turn, continue, summary), answered locally like `/tools`.

**F3-S4 — Rider (optional).** As a *Rider user*, I want an optional usage line at the end of an answer, since Rider has
no usage UI.
- *Given* `USAGE_IN_CHAT=true` for the ACP server *then* the answer ends with the same line as the CLI; off by default.

**F3-S5 — Cost per fix and per run.** As an *operator* of the orchestrator, I want tokens and cost per fix and per run in
the comms log, so that I know what the demo (and any real run) cost.
- *Then* each louis-agent reply section shows its tokens and cost; each error's summary shows louis-agent's cost, the
  orchestrator's triage cost, and the total; the run summary table gains *Tokens* and *Cost* columns and a run total.
- *And* `fixes.jsonl` and `decisions.jsonl` entries carry tokens and cost.

## Design

- **Turn totals.** `UsageScope` (F1) accumulates the current turn's records; hosts read `scope.Totals` when the turn ends.
  Session totals: `AgentSession` (API), the ACP session object and the CLI loop keep a running `UsageTotals`.
- **API** (`src/louis-agent.api/Program.cs`): `ClaudeStyleStream.Stop(stopReason, usage)` writes `usage` into the
  `message_delta` event; `GET /sessions/{id}` adds `usage`.
- **Web** (`louis-agent.web`): `Turn.Usage` (new model type, saved by `ChatStore`); `ChatPage.Apply` reads it from
  `message_delta`; `TurnView` renders the line; the chat header sums the turns. Formatting helper shared with F11.
- **CLI** (`AgentEngine.RunAsync`): print the line after each answer; `/usage` handled in `HandleUserCommand`.
- **ACP** (`AcpServer`): when `USAGE_IN_CHAT` is on, send one final `agent_message_chunk` with the line.
- **Orchestrator:** `LouisAgentClient.ReadStreamAsync` reads `usage` from `message_delta` into `AgentReply.Usage`;
  triage usage comes from the orchestrator's own `UsageRecorded` events within the error's scope. `CommsLog.Received`
  and `EndEvent`/`EndRun` render it; `ServiceTools` adds it to `fixes.jsonl`.

## Implementation steps

1. **`UsageTotals` + turn accumulation in `UsageScope`.** *Test:* totals sum records of the turn only; session totals
   sum turns.
2. **API: `message_delta.usage` and session totals.** *Test:* a `ClaudeStyleStream` unit test (or manual curl) shows the
   fields; existing web client ignores unknown fields.
3. **Web: model, store, render.** *Test (manual):* answer line, header total, reload keeps it; "price unknown" case.
4. **CLI line and `/usage`.** *Test:* `HandleUserCommand("/usage")` returns the totals text and never reaches the model.
5. **ACP optional line.** *Test (manual in Rider)* with `USAGE_IN_CHAT=true`.
6. **Orchestrator: parse usage, comms log columns, jsonl fields.** *Test:* `LouisAgentClientTests` parses `usage` from a
   stream; `CommsLog` test asserts the new run-summary columns and totals.
7. **Docs:** README web app section, quick start, AGENT_COMMUNICATION event table (`usage` on `message_delta`).

## Done when

- Stories' criteria pass; tests added; suites pass.
- A demo run's comms log shows cost per fix and a run total that matches the ledger's sum for that run.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F3** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F2 Prices and cost](F02-prices-and-cost.md) · Next: [F4 Prompt caching](F04-prompt-caching.md)
