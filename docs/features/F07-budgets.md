# F7 · Budgets: warn, ask, stop

> **Status:** planned · **Milestone:** M3 · **Depends on:** F1, F2, F6
>
> **Spec:** [Usage §6](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F7 Budgets** under *Now* ([TODO](../../TODO.md))

The only limits today are the Anthropic account's credit and monthly spend limit; hitting them fails every remaining
request. F7 adds louis-agent's own budgets — per session, task, run, day and month — that warn early and stop or pause
cleanly, between requests, never mid-answer.

## User stories

**F7-S1 — Define budgets.** As an *operator*, I want to set budgets in a config file, so that spending is capped at the
levels I care about.
- *Given* `config/budgets.json` with session, task, run, day and month budgets (optionally per host, model or service;
  in `usd` or `tokens`) *then* they load at start-up and are listed in the startup log.
- *Given* several budgets apply to one request *then* the tightest decides.

**F7-S2 — Warnings before the limit.** As an *operator*, I want warnings as a budget fills, so that a stop is never a
surprise.
- *Given* `warn_at: [0.8]` *when* a scope passes 80% *then* a warning is logged once, shown in the chat (amber budget
  bar, F3/F11) and in the comms log.

**F7-S3 — Stop cleanly.** As a *user*, I want a reply that reaches a `stop` budget to end cleanly with a clear message,
so that I know why and can continue later.
- *Given* the next request's estimated cost would exceed a `stop` budget *then* it isn't sent: the turn ends with
  stop reason `budget`; the API sends an `error` event of type `budget_error` with which budget, its limit and use;
  ACP and CLI print the same message.
- *And* the conversation so far is kept, so the user can continue once the budget allows (new period or raised limit).

**F7-S4 — Ask to continue.** As a *user*, I want an `ask` budget to offer to continue, so that I decide case by case.
- *Given* a session budget with `action: "ask"` is reached *then* the web app shows "Budget reached ($0.50 for this chat).
  Continue with another $0.50?" with a button; continuing raises that session's limit and resumes; the CLI asks y/n.

**F7-S5 — The orchestrator defers, it doesn't fail.** As an *operator*, I want the orchestrator to skip fixes it can't
afford and report them, so that a run within budget does as much as it can and the rest is resumable.
- *Given* a fix's estimate (F6) exceeds what's left in the run, day or month budget *then* the fix isn't started; the
  error is recorded as **deferred: budget**, left unprocessed, and listed in the run summary; `-Resume` with more budget
  completes it.

## Design

New, in `louis-agent.core/usage/`:

| Type | Responsibility |
|---|---|
| `BudgetConfig` | Loads `BUDGETS_FILE` (default `config/budgets.json`); validates scopes, filters, limits, thresholds, actions |
| `BudgetLedger` | Keeps used amounts per budget and period: loaded from the current month's ledger at start-up, updated by every `UsageRecorded` event (F1) |
| `BudgetGuard` | `Check(scope, estimatedNextCost)` → `Ok` / `Warn(budget, fraction)` / `Ask(budget)` / `Stop(budget)` |
| `BudgetExceededException` | Thrown by the enforcement middleware; carries the decision |

Enforcement:

- **Before every model request:** a `BudgetChatClient` in the same inner pipeline position as F1's recorder estimates
  the request (prefix + history at input price, typical output) and calls `BudgetGuard`. On `Stop`/`Ask` it throws
  before sending.
- **`AgentEngine.StreamPromptAsync`** catches it, appends what was produced so far to the history (so the turn is
  resumable), and yields a final update with a `budget` stop reason. Hosts map it: API → `budget_error` event; ACP/CLI →
  message; web → the ask UI.
- **Ask → continue:** `POST /sessions/{id}/budget` `{ "add_usd": 0.50 }` raises that session's limit (in memory);
  the web button calls it, then sends "continue".
- **Orchestrator:** `ServiceTools.CallLouisAgentFix` checks `BudgetGuard` with the F6 fix estimate before creating the
  session; on refusal it returns "deferred: budget" to the model, records it, and `Program` does not mark the error
  processed. A `run` scope is opened at start.
- **Account limits** (credit / usage limit refusals from the provider) are handled in F8; budgets act before them.

## Implementation steps

1. **`BudgetConfig` + example file** (`config/budgets.example.json`, real file git-ignored). *Test:* validation errors
   name the field; filters parse.
2. **`BudgetLedger`** periods (session, task, run, day, month in UTC). *Test:* rebuilt totals from a fixture ledger;
   live updates; month rollover.
3. **`BudgetGuard`.** *Test:* tightest-budget wins; thresholds fire once; tokens vs usd budgets; unpriced records count
   toward token budgets only.
4. **`BudgetChatClient` + engine handling.** *Test:* scripted loop stops before the request that would exceed; history
   contains the partial turn; stop reason `budget`.
5. **Host mapping:** API `budget_error`; ACP/CLI message. *Test:* API stream test / manual.
6. **Ask flow:** endpoint + web dialog + CLI prompt. *Test (manual)* in the web app.
7. **Orchestrator deferral.** *Test:* with a tiny run budget, a fake API is never called for the deferred fix; the error
   stays unprocessed; run summary lists it.
8. **Demo:** `run-demo.ps1` passes a run budget option (`-BudgetUsd`); acceptance run below and resume above.
9. **Docs:** SETUP, samples README, spec status.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Acceptance run: a run budget below the full run's cost stops the run cleanly with deferred fixes; `-Resume` with a
  larger budget completes them (spec §9).
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F7** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F6 Estimates](F06-estimates.md) · Next: [F8 Reliability](F08-reliability.md)
