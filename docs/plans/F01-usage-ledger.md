# Plan: F1 Usage ledger

> **For:** [F1 Usage ledger](../features/F01-usage-ledger.md) · **Approved:** 2026-10-10 · **Recreated:** 2026-10-11, from
> the plans approved in the session (F1 was planned before plans were kept in the repo)

## Context

Nothing read `ChatResponse.Usage`, so nobody knew what a turn, a fix or a demo run used. F1 records one line per model
request, attributed to host, session, turn, purpose and (for orchestrator fixes) task, run and service. Every later
feature (F2–F11) reads from it. The feature doc's stories (F1-S1 … S4) and its nine steps were the plan; the one step
planned in detail first was the spike, because the record's design depended on what the provider's adapter reports.

## Design

As in the feature doc: `UsageRecord` and `UsageScope`, a `UsageRecordingChatClient` middleware inside the tool loop
(`UseFunctionInvocation`), an `IUsageSink` writing JSON lines to `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`, hosts opening a
scope per turn, and session tags on `POST /sessions` for the orchestrator's fixes and runs.

## Steps (each one commit with its test; stop for review after each)

1. **Spike: what the Anthropic adapter reports.** A throwaway console app (not in the repo) using the agents'
   `LlmClientFactory` client on Haiku 4.5 sends 6 small requests: non-streaming and streaming, with no cache marker, then
   a cache write and a cache read (a prompt above Haiku 4.5's 4,096-token cache minimum). For each it prints every
   `UsageDetails` property and `AdditionalCounts` key, and which streamed update carried the `UsageContent`. Questions:
   are input and output counts present in both modes; is `CachedInputTokenCount` set on a read; which `AdditionalCounts`
   key holds cache **writes**; is `ReasoningTokenCount` set. *Cost:* about $0.05, with approval. *Output:* the answers in
   the optimisation spec (§A open question), and the F1 doc changed if they change the design. No production code.
2. **`UsageRecord` and `UsageScope`.** *Test:* nested scopes restore the outer one; the round counter increments per
   request and resets per turn; scope values flow across `await`.
3. **`UsageRecordingChatClient`, non-streaming.** *Test:* one record with the right counts and scope; `null` counts stay
   `null`; Anthropic-shaped usage (input 10,266, cached 10,227) records `input` 39.
4. **Streaming.** *Test:* a fake stream with `UsageContent` gives one record when it ends; a cancelled stream records
   `stop: "cancelled"`.
5. **Wire into `AgentEngine`** (pipeline, summary client, continuation purpose). *Test:* a 3-round tool loop gives rounds
   1–3; an oversized result adds one `summary` record.
6. **`JsonlUsageSink` + `USAGE_LEDGER`.** *Test:* one JSON line per record; no message text in the file.
7. **Host scopes:** API, ACP, CLI, orchestrator. *Test:* manual curl shows `host`/`session`/`turn`.
8. **Session tags** on `POST /sessions` and in `LouisAgentClient`. *Test:* orchestrator test with a stub API; core test
   that tags reach the records.
9. **Docs and finishing.**

## Verification

- `dotnet test` on the solution after every step.
- Free manual checks against Ollama (the CLI and the API on the Ollama profile).
- One demo run, with approval, whose ledger ties every fix to its task and run (F1's *Done when*).

## Concepts

Measuring before designing (the spike); middleware placement inside a tool loop; ambient context (`AsyncLocal`) and
why it doesn't survive an async iterator's `yield`. Skill map #13 (cost and token economics) and #17 (observability).

*What the build changed (the pluggable `IUsageMapper`, the usage probe, the round-limit fix, LLM profiles, Haiku 5.5)
is recorded in the [feature doc](../features/F01-usage-ledger.md) and in [fixes/](../fixes/README.md).*

---
[Plans](README.md) · Next: [F2](F02-prices-and-cost.md)