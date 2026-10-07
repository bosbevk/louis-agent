# F10 · Settings per route: thinking, tool rounds, summary model

> **Status:** planned · **Milestone:** M4 · **Depends on:** F1 ·
> **Spec:** [Optimisation §E](../specs/RESPONSE_OPTIMISATION.md) · **TODO:** known limitation *10 tool rounds per
> message* (make `MaximumIterationsPerRequest` configurable)

One set of settings serves very different work today: a quick chat, a 20-round fix, the orchestrator's triage, and
summaries. F10 lets each route have its own thinking level, tool-round limit and model — fixed per session, so caching
isn't affected.

## User stories

**F10-S1 — More tool rounds for tasks.** As an *operator*, I want API task sessions to allow more tool rounds per message,
so that a fix needs fewer "continue" round-trips.
- *Given* a session created with `{ "profile": "task" }` (the orchestrator does this) *then* it uses
  `AGENT_TASK_MAX_TOOL_ROUNDS` (default 30); chats keep `AGENT_MAX_TOOL_ROUNDS` (default 10).
- *Given* the benchmark *then* "continue" messages per fix drop (from 2–3 today) and the fix rate holds.

**F10-S2 — Thinking per route.** As an *operator*, I want thinking set per route, so that classification-style work
doesn't pay for deep reasoning.
- *Given* the orchestrator *then* its triage uses `ORCHESTRATOR_THINKING` (default `low`); louis-agent's chats and tasks
  keep `LLM_THINKING`.
- *And* a session's thinking level never changes mid-session.

**F10-S3 — A cheaper model for summaries.** As an *operator*, I want oversized-result summaries and compaction (F9) to
use a cheaper model, so that housekeeping doesn't cost main-model prices.
- *Given* `LLM_SUMMARY_MODEL` *then* `SummariseToolResultAsync` and compaction use it (same provider), recorded in the
  ledger with that model; unset → the main model, as today.

**F10-S4 — Less custom code.** As a *developer*, I want to use the SDK's own thinking mode if it covers our needs, so that
`LlmClientFactory` keeps less provider-specific code.
- *Given* the SDK's `AsIChatClient(..., AnthropicThinkingMode)` *then* check whether `Extended` replaces the repo's
  fixed thinking-budget wrapper for pre-4.6 models; replace it if behaviour (budgets per level) matches, otherwise
  record why not.

## Design

- **Profiles:** `RouteProfile { MaxToolRounds, Thinking }` with two built-ins, `chat` and `task`. `POST /sessions` accepts
  `profile`; ACP and CLI are `chat`; `LouisAgentClient` sends `task`.
- **Per-profile pipelines:** `FunctionInvokingChatClient.MaximumIterationsPerRequest` is set per client instance, so
  `AgentEngine` builds one pipeline per profile (lazily, cached by profile) instead of one; `StreamPromptAsync` /
  `ProcessPromptAsync` take the profile. Thinking per profile is applied in `BeginTurn`.
- **Summary client:** `LlmClientFactory.CreateSummaryClient(options)` when `LLM_SUMMARY_MODEL` is set; `AgentEngine`
  takes it as an optional constructor argument and uses it for summaries and compaction.
- **Orchestrator:** its own engine's `Thinking` from `ORCHESTRATOR_THINKING`.

## Implementation steps

1. **Settings:** `AGENT_MAX_TOOL_ROUNDS`, `AGENT_TASK_MAX_TOOL_ROUNDS`, `ORCHESTRATOR_THINKING`, `LLM_SUMMARY_MODEL`.
   *Test:* parsing and defaults.
2. **Per-profile pipelines.** *Test:* a scripted loop of 15 rounds stops at 10 under `chat` and completes under `task`.
3. **`POST /sessions` profile + `LouisAgentClient` sends `task`.** *Test:* tags/profile reach the session.
4. **Summary client.** *Test:* summaries go to the summary client; usage records its model.
5. **Orchestrator thinking.** *Test:* triage requests carry the low reasoning effort (recording fake client).
6. **Thinking-mode spike** (S4): compare request bodies for each `LLM_THINKING` level with the wrapper vs the SDK mode;
   replace or document.
7. **Benchmark:** continues per fix, cost per fix, fix rate; record against M3.
8. **Docs:** SETUP, MODELS (thinking per route), AGENT_COMMUNICATION (`profile` on `POST /sessions`), CLAUDE.md known
   limitation 6 updated.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Benchmark: fewer continues per fix, fix rate unchanged, triage cheaper; numbers recorded.

---
[Features](README.md) · Previous: [F9 Bounded history](F09-context-management.md) · Next: [F11 Usage tab and reconciliation](F11-usage-tab-and-reconciliation.md)
