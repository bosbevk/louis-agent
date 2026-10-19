# F16 · Summariser: one measured component for condensing text

> **Status:** merged into [F9](F09-context-management.md) (2026-10-19, [the platform plan](../plans/platform.md)):
> today's guard already summarises every result over 50,000 characters, so F9 is its second user, not its only one;
> F16-S1 (the oversized-results report, and the worst offenders paged) is a small cleanup of its own · **Milestone:**
> M4 (with [F18](F18-evaluation.md), before F9) · **Depends on:** F1, F4 (the caching strategy), F18 (its eval)
>
> **Spec:** [Optimisation §D (bounded history), §E (summary model)](../specs/RESPONSE_OPTIMISATION.md)
>
> **In the TODO:** merged, so its line is gone; its work is on **F9 Long chats** under *Later*, and **F16-S1** under
> *Small cleanups* ([TODO](../../TODO.md)) · **Takes over:** F10-S3 (a cheaper model for summaries)

When a tool result passes 50,000 characters, `ToolCallGuards.SummariseToolResultAsync` (`Agent/`) sends up to 200,000 characters of
it to the main model with an inline prompt and a 16,000-token output cap, the same as a whole turn. Nobody knows how
often it fires, which tools cause it, or whether a summary keeps what the next step needed. F9 will need the same skill
again to compact old turns. F16 makes the summariser one component with its own settings, prompt and policy, and
measures it, following Anthropic's guidance:

- **Simplest thing first:** cap tool output and tell the agent how to narrow its call before adding a model call
  ([writing tools for agents](https://www.anthropic.com/engineering/writing-tools-for-agents)).
- **Condensed returns:** a sub-agent's summary is typically 1,000–2,000 tokens.
- **Tune for recall first, then precision**, on real traces
  ([context engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)).

## User stories

**F16-S1 — Fewer summaries needed.** As an *operator*, I want the tools that produce oversized results found and fixed,
so that the summariser runs only when it must.
- *Given* `logs/oversized-tool-results.jsonl` (already written today) *then* a report lists each tool's count, sizes
  and whether it was summarised or truncated.
- *And* the top offenders page or filter their output and say how to narrow the call (each fix its own step, with its
  test); the report after the demo shows fewer oversized results.

**F16-S2 — Its own component and settings.** As a *developer*, I want the summariser to be one class with its own
model, prompt and output cap, so that it can be tuned and reused without touching the engine.
- *Given* `SUMMARY_MODEL` (same provider; from F10-S3) *then* summaries use it, recorded in the ledger with that model
  and purpose `summary`; unset → the main model, as today.
- *And* `SUMMARY_MAX_OUTPUT_TOKENS` defaults to 2,000; the prompt lives in `Skills/summariser.md`, so a change to it is
  a reviewed, versioned change.

**F16-S3 — No caching.** As an *operator*, I want summary requests never cached, so that a one-off prompt isn't
written at 1.25× and never read.
- *Then* the summariser's client uses `NoPromptCaching` ([provider strategies](../architecture/05-building-block-view.md#provider-strategies));
  F4's interim `summary`-scope skip is removed; the ledger shows no cache writes on `summary` records.

**F16-S4 — Measured quality.** As a *developer*, I want the summariser scored on real cases, so that a prompt or model
change is kept only when quality holds.
- *Given* F18's `summariser` suite (about 20 real oversized outputs from demo runs: test logs, diffs, large files, each
  with the questions the next step needs answered) *then* the current prompt on the main model is the baseline.
- *And* the new prompt (tuned for recall, then precision) and a cheaper model are each compared with it; a change is
  kept only at an equal or better pass rate, and the numbers are recorded here.

**F16-S5 — Ready for compaction.** As a *developer*, I want F9's compaction to use the same component with its own
instructions, so that there is one summariser, one place to tune it, and one eval.
- *Then* the component takes the text and a job (`tool-result`, later `compaction`), each with its own prompt section.

## Design

- **`Summariser`** (`louis-agent.core/context/`): `Task<string?> SummariseAsync(SummaryJob job, string text, string?
  request, CancellationToken)`, returning null on failure so the engine keeps today's fallback (truncation).
  `AgentEngine` takes it as a constructor argument and calls it where `ToolCallGuards` calls `SummariseToolResultAsync` today.
- **Its client** comes from `LlmClientFactory.CreateSummaryClient(options)`: `SUMMARY_MODEL` or the main model, with
  `NoPromptCaching` and its own usage recorder (as summaries are recorded today, outside the tool loop).
- **Output cap** 2,000 tokens by default, in line with Anthropic's 1,000–2,000-token guidance for condensed returns.
- **The eval target** for F18 calls `SummariseAsync`, then asks the case's questions of the summary alone (an `exact`
  or `judge` scorer per question), so it measures what the next step can still do, not how the summary reads.

## Implementation steps

1. **Oversized-results report.** *Test:* totals per tool from a sample `oversized-tool-results.jsonl`.
2. **Fix the top offending tools**, one step each (page, filter, or a narrower default, with a hint in the result).
   *Test:* per tool.
3. **`Summariser` component**, behaviour unchanged (main model, current prompt moved to `Skills/summariser.md`).
   *Test:* the engine's existing summary tests pass through the component; a failure still truncates.
4. **Settings and client:** `SUMMARY_MODEL`, `SUMMARY_MAX_OUTPUT_TOKENS`, `CreateSummaryClient` with `NoPromptCaching`.
   *Test:* parsing; the ledger records the summary model; no cache markers on summary requests.
5. **Eval suite** (with F18): cases from demo logs, the target, the baseline run (**needs Louis's go-ahead**, cents).
6. **Tune:** prompt for recall then precision, then a cheaper model; each a measured comparison (**go-ahead**).
7. **Jobs:** `SummaryJob` with `tool-result`; `compaction` added by F9. *Test:* each job sends its own prompt section.
8. **Docs:** SETUP settings, the core project doc, the architecture (runtime view, guards), the skill-7 map.

## Done when

- Stories' criteria pass; tests added; suites pass.
- The suite's baseline and the kept changes are recorded with pass rate and cost per summary; quality held.
- The demo still confirms 10/10 fixes.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): move **F16** to the CHANGELOG, set
  *Status* to done here and in the features table, and update skills 7 and 12 in the goal map.

---
[Features](README.md) · Previous: [F15 Model limits](F15-model-limits.md) · Next: [F17 Orchestrator agent](F17-orchestrator-agent.md)
