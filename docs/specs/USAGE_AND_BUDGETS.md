# Spec: Usage, Estimates and Budgets

**Status:** in progress (F1, F2 done) · **Date:** 2026-10-07 · **Builds on:** [Response optimisation](RESPONSE_OPTIMISATION.md) §A (measuring usage)

## Implementation

Built as features in [docs/features/](../features/README.md); each is one line under *Now* in the
[TODO](../../TODO.md). When a feature is done, mark it here too.

| Section | Feature | Status |
|---|---|---|
| §4.1, §4.3 Recording | [F1 Usage ledger](../features/F01-usage-ledger.md) | **done** (2026-10-10): `input` is uncached input (the adapter's count minus cache reads and writes); `reasoning` is null on Claude; triage records are tied to their run, not to a task |
| §4.2 Prices | [F2 Prices and cost](../features/F02-prices-and-cost.md) | **done** (2026-10-10): `config/prices.json` is tracked; a `long_prompt` tier (Haiku 5.5); also served at the API's `GET /prices` and read with `PRICES_URL` |
| §7.2–7.4 Showing it (and the stream in §7.1) | [F3 Show usage](../features/F03-usage-display.md) | planned |
| §5 Estimates | [F6 Estimates](../features/F06-estimates.md) | planned |
| §6 Budgets | [F7 Budgets](../features/F07-budgets.md) (account-limit refusals: [F8](../features/F08-reliability.md)) | planned |
| §7.1–7.2 Usage tab and API, §4.4 Reconciliation | [F11 Usage tab and reconciliation](../features/F11-usage-tab-and-reconciliation.md) | planned |

## 1. Goal

Make spending on the model **visible, predictable and capped**:

1. **Record** what every request, turn, session, fix and run actually used and cost, in one durable ledger.
2. **Estimate** what something will cost *before* it runs: a chat turn, an orchestrator run, a build-mode service.
3. **Budget**: set limits per session, per run and per day/month, warn as they are approached, and stop or pause
   cleanly when one is reached — instead of failing halfway through, as the demo did when the account's credit and
   then its monthly usage limit ran out.
4. **Show** all of it where people already look: the web app, the CLI, the API, the comms log, the demo script.

Out of scope: billing users, and anything that changes *how much* a request costs (that is the optimisation spec).

## 2. What exists today

- **Recorded (F1, F2):** every model request is one line in the usage ledger (§4.1), priced from `config/prices.json`
  (§4.2), and tagged with its task and run (§4.3). The M1 baseline is in the features'
  [Benchmark](../features/README.md#the-benchmark).
- **Not yet shown:** no host displays usage or cost (F3); totals come from `usage-probe -- --ledger`.
- The only limit is the Anthropic account's own: a credit balance and a monthly spend limit, set in the Anthropic
  Console. When either is hit, every request fails with a 400 ("credit balance is too low" / "reached your specified
  API usage limits") and the orchestrator marks each remaining error as failed. `-Resume` recovers afterwards, but
  nothing warned beforehand.
- No estimates and no louis-agent budgets yet (F6, F7).

## 3. Concepts

| Term | Meaning |
|---|---|
| **Usage record** | One model request: tokens in four kinds (input, cache write, cache read, output) plus reasoning, with who and what it was for |
| **Price table** | Per-model prices per million tokens for those four kinds; configuration, never code |
| **Cost** | Usage × price, computed when recorded and stored with the record (prices change; the record keeps what it cost then) |
| **Scope** | What usage is grouped by: request → turn → session → task (a fix or build) → run → day / month; plus host, model, service |
| **Estimate** | A predicted range (low–high) of tokens and cost for something not yet run, with how it was worked out |
| **Budget** | A limit on a scope and period (tokens or money), with warning thresholds and an action when reached |

## 4. Recording: the usage ledger

### 4.1 The record

`UsageRecordingChatClient`, a middleware inside the engine's tool loop, writes one record per model request (including
the separate summarisation calls), streaming or not. `PricingUsageSink` adds the cost, and `JsonlUsageSink` appends it to
`{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`: one JSON line, no prompt text, no tool output. Without `LOG_DIRECTORY` the ledger
is off, with a warning.

```json
{
  "at": "2026-10-07T09:14:03.512Z",
  "host": "api",                      // cli | acp | api | orchestrator
  "session": "sess_2b84e43a…",
  "turn": 1,                          // user message number in the session
  "round": 7,                         // model request number within the turn
  "purpose": "turn",                  // turn | continue | summary | compaction | triage
  "task": "fix:e8b0b572223d",         // set by the caller via the API (see 4.3); null for chats
  "run": "demo-2026-10-07T08:51",     // orchestrator run id; null otherwise
  "service": "order-service",
  "model": "claude-haiku-4-5-20251001",
  "tokens": { "input": 1840, "cache_write": 26110, "cache_read": 0, "output": 412, "reasoning": 210 },
  "cost": { "currency": "USD", "amount": 0.0365, "price_table": "2026-10-01" },  // 1,840×$1 + 26,110×$1.25 + 412×$5 per M
  "duration_ms": 3120,
  "stop": "tool_use"
}
```

- Token counts come from `UsageDetails`: `cache_read` = `CachedInputTokenCount`, `cache_write` =
  `AdditionalCounts["CacheCreationInputTokens"]`, `output` = `OutputTokenCount`, `reasoning` = `ReasoningTokenCount`.
  The four kinds don't overlap: the Anthropic adapter's `InputTokenCount` is the whole prompt, so `input` =
  `InputTokenCount − cache_read − cache_write` (measured; see the optimisation spec's *What the Anthropic adapter
  reports*). Reasoning (thinking) tokens are part of the output count and billed as output; they are recorded
  separately only to show how much of the output was thinking. The Anthropic adapter doesn't report them, so
  `reasoning` is `null` there. Providers that report nothing (some Ollama models) record counts as `null`, never as 0.
- The file is append-only and rotates monthly (`usage-2026-10.jsonl`, by UTC month); it is the source for every total
  below. Several processes can append to it at once (the API and the orchestrator share one in the demo).
- Sessions are in memory, but the ledger is not: totals survive restarts.

### 4.2 Prices

A price table in `config/prices.json`, **tracked** (prices aren't secret, so every clone and container prices the same
way; `PRICES_FILE` points elsewhere). A host can also fetch it over HTTP with `PRICES_URL`, e.g. from louis-agent.api's
public `GET /prices`, so several hosts price with one table; if the URL can't be reached at start-up it falls back to the
file. Comments are allowed in the file:

```json
{
  "as_of": "2026-10-10",
  "currency": "USD",
  "models": {
    "claude-haiku-5-5": {
      "input": 0.10, "output": 0.50, "cache_read": 0.01, "cache_write_5m": 0.125, "cache_write_1h": 0.20,
      "long_prompt": { "above_tokens": 100000, "input": 0.50, "output": 2.50, "cache_read": 0.05, "cache_write_5m": 0.625, "cache_write_1h": 1.00 }
    },
    "claude-haiku-4-5": { "input": 1.00, "output": 5.00, "cache_read": 0.10, "cache_write_5m": 1.25, "cache_write_1h": 2.00 }
  }
}
```

Prices are per million tokens and are matched by the longest model prefix (`claude-haiku-4-5` also matches
`claude-haiku-4-5-20251001`). **Copy them from Anthropic's pricing page** and update `as_of` when they change. A
`long_prompt` tier prices the whole request once its prompt (input + cache reads + cache writes) is over `above_tokens`
(Claude Haiku 5.5: 5× above 100,000). Every price must be given: a missing or negative one stops the host rather than
read as free. Local models (Ollama) are priced at 0 on purpose; a model without a price records tokens with
`cost: null`, and every view says "price unknown" instead of showing $0.

### 4.3 Attributing usage to tasks

A session can be tagged so its usage rolls up to a task and a run:

- `POST /sessions` accepts optional `{ "task": "fix:<error id>", "run": "<run id>", "service": "<name>" }`; the
  orchestrator sets them for every fix (and build, in the build-mode POC).
- The orchestrator's own triage requests are recorded with `host: "orchestrator"`, `purpose: "triage"`, session
  `triage_{error id}` and the same run id (no task), so a run's total includes both agents.

### 4.4 Reconciling with the real bill (optional)

The ledger is computed from the API's reported tokens and the configured prices, so it should match the bill closely
but is not the bill. Where an **Admin API key** exists (organisation accounts only — not available to individual
accounts), a daily job reads Anthropic's Usage and Cost reports (`GET /v1/organizations/usage_report/messages`,
`GET /v1/organizations/cost_report`; report reads, no tokens used) and records the difference per day and model. A
difference above 5% is flagged: usually a missing price, a wrong model prefix, or other software sharing the API key.
Recommend a separate API key or workspace for louis-agent so the reports are attributable.

## 5. Estimates

Every estimate is a **range with its basis**, e.g. *"≈ $0.40–0.90 (10 fixes × $0.04–0.09, median of the last 20
fixes)"* — never a single figure presented as certain.

### 5.1 How

| Basis | Used when | How |
|---|---|---|
| **History** (preferred) | The ledger has ≥ 5 similar tasks (same task kind, model, toolset) | Low–high = 25th–75th percentile of their cost; the median is the headline |
| **Model** (fallback) | No or too little history | `rounds × (prefix + avg history) × price`, with the prefix measured from the last request (or counted with the provider's token-counting endpoint) and cache effects applied: first round at write price, later rounds at read price |
| **Calibration** | After every run | Compare each task's estimate with its actual; show the error in the run summary; the history basis improves itself as the ledger grows |

Defaults for the model-based estimate come from the M1 baseline on Claude Haiku 5.5 (8–16 requests per fix, 11.4 on
average; a ~48,000-token prefix; see the features' [Benchmark](../features/README.md#the-benchmark)) until history
replaces them.

### 5.2 Where estimates appear

| Where | What it shows |
|---|---|
| `run-demo.ps1` (and the build-mode script) | Before starting: "13 errors · ~10 likely fixes · estimated $X–$Y · budget remaining $Z" and, if the estimate's high end exceeds what's left in a budget, asks to continue |
| Orchestrator, before each fix | Its estimate for that fix against the run budget left; skips (and reports) a fix it can't afford instead of starting it |
| Build mode | Per method and total, before building; per method in the plan the human approves |
| Web app / CLI, while a reply streams | Running cost of the current turn; no up-front estimate for chat (too variable) |

## 6. Budgets

### 6.1 Definition

Budgets live in `config/budgets.json` (not secret; an example ships):

```json
{
  "budgets": [
    { "scope": "session",                  "limit": { "usd": 0.50 },  "warn_at": [0.8],      "action": "ask"   },
    { "scope": "run",                      "limit": { "usd": 3.00 },  "warn_at": [0.5, 0.8], "action": "stop"  },
    { "scope": "task",  "kind": "fix",     "limit": { "usd": 0.30 },                         "action": "stop"  },
    { "scope": "day",                      "limit": { "usd": 10.00 }, "warn_at": [0.8],      "action": "stop"  },
    { "scope": "month", "service": "order-service", "limit": { "usd": 50.00 }, "warn_at": [0.5, 0.8, 0.95], "action": "stop" }
  ]
}
```

- **Scopes:** session, task (by kind: fix, build, triage), run, day, month — optionally narrowed by host, model or
  service. Limits in money (`usd`) or tokens (`tokens`, for unpriced or local models).
- **Several budgets can apply at once**; the tightest one decides.
- **Actions when reached:**
  - `warn` — note it and carry on;
  - `ask` — finish the current request, then pause and ask the person (web app / CLI / Rider) whether to continue,
    and by how much;
  - `stop` — finish the current request, then end the turn or task cleanly with a clear message.
- A request already sent is never cut off mid-answer: budgets are checked **before** each model request, using the
  next request's estimated cost (its prefix plus history at input price, plus the output cap's typical use).

### 6.2 Enforcement points

| Where | Check | When over budget |
|---|---|---|
| `AgentEngine`, before every model request | session, task, day, month | Ends the turn with a `budget_exceeded` outcome: the API sends an `error` event of type `budget_error` (Claude's error shape), ACP and CLI print the message; the turn's history stays intact so it can continue once allowed |
| Orchestrator, before each fix (and each build) | run, task estimate, day, month | Doesn't start the task; records the error as **deferred: budget**, leaves it unprocessed (so `-Resume` picks it up later), and lists it in the run summary |
| Orchestrator, on an account-limit refusal from the provider (credit / usage limit) | — | Stops the whole run at the first such error instead of failing each remaining one; tells the person which limit, and that `-Resume` continues |

### 6.3 Relationship to the provider's own limits

The Anthropic Console's credit balance and spend limits remain the **hard backstop**; louis-agent's budgets are the
**earlier, finer-grained** limits that stop work before the account does. Keep the Console's monthly limit above the
sum of louis-agent's monthly budgets.

Some newer Claude models also accept an advisory per-task token budget in the request itself (so the model paces
itself). Claude Haiku 5.5, the default, supports it as a beta (`task-budgets-2026-03-13` header, `task_budget` in
`output_config`, at least 20,000 tokens); Claude Haiku 4.5 doesn't. Where the configured model supports it, the task
budget can be passed through as a hint — it never replaces the checks above.

## 7. Showing it

### 7.1 API

| Endpoint | Returns |
|---|---|
| `GET /usage?from=&to=&group_by=day,model,host,service,session,task,run` | Totals per group: tokens by kind, cost, request count, cache-hit rate |
| `GET /usage/sessions/{id}` | The session's turns with tokens and cost per turn |
| `GET /budgets` | Each budget: limit, used, remaining, period end, status (`ok` / `warning` / `reached`) |
| `POST /estimates` | `{ "kind": "fix" \| "build" \| "run", "count": n, "service": "…" }` → low / median / high, with the basis |
| SSE `message_delta` | Adds `usage` (tokens and cost for the turn so far) — Claude's own event shape carries it |

Same `AGENT_API_KEY` rules as the other endpoints.

### 7.2 Web app

- **Chat:** a small line under each answer — `1.9k in · 26k cached · 412 out · $0.004` — and the chat's total in the
  header; a budget bar appears when a session budget exists, and turns amber at a warning threshold.
- **Usage tab** (new, beside Branches): today / this month, by model, host and service; the most expensive sessions
  and tasks; cache-hit rate; budgets with used / remaining and their status; links from a task to its comms log.
- **Ask action:** when a budget's action is `ask`, the reply ends with "Budget reached ($0.50 for this chat). Continue
  with another $0.50?" and buttons, instead of an error.

### 7.3 CLI and Rider

- CLI: one line after each answer (`turn: 3 requests · 31k tokens (26k cached) · $0.006 · chat total $0.04`), and
  `/usage` and `/budget` commands that answer locally, like `/tools`.
- Rider (ACP): the same one-line summary as a final `agent_message_chunk` when enabled (`USAGE_IN_CHAT=true`), since
  Rider has no dedicated usage UI.

### 7.4 Orchestrator and demo

- **Comms log:** tokens and cost per message and per fix (louis-agent and the orchestrator's own triage shown
  separately), the estimate next to the actual, and run totals plus budget status in the run summary.
- **`fixes.jsonl` / `decisions.jsonl`:** tokens, cost and estimate per entry.
- **Demo script:** the up-front estimate (5.2), and at the end: total cost, cost per fix, estimate accuracy, and the
  budgets' remaining amounts.

## 8. Configuration

| Setting | Default | Meaning |
|---|---|---|
| `USAGE_LEDGER` | `on` | Write `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl` (needs `LOG_DIRECTORY`; off with a warning without it) |
| `PRICES_FILE` | `config/prices.json` | Price table (missing file → tokens only, with a warning; a malformed one stops the host) |
| `PRICES_URL` | unset | Fetch the price table over HTTP, e.g. louis-agent.api's `GET /prices`; unreachable → `PRICES_FILE` |
| `BUDGETS_FILE` | `config/budgets.json` | Budgets (missing file → no louis-agent budgets; the account's limits still apply) |
| `USAGE_IN_CHAT` | `true` web/CLI, `false` Rider | Show the per-answer usage line |
| `ADMIN_API_KEY` *(secret)* | unset | Enables daily reconciliation with Anthropic's Usage and Cost reports (4.4) |

## 9. Rollout

1. **Ledger + prices** (with the optimisation spec's workstream A): records, cost, the monthly ledger, the per-answer
   line, comms-log totals. Baseline: one demo run. *Ledger, prices and baseline done (F1, F2); the per-answer line and
   comms-log totals are F3.*
2. **Estimates**: model-based first, history-based once the ledger has enough tasks; shown in the demo script and run
   summary with estimate-vs-actual.
3. **Budgets**: `warn` and `stop` first (session, run, day, month), then `ask` with the web app's continue button.
4. **Usage tab and endpoints** in the web app.
5. **Reconciliation** where an Admin API key exists.

**Accept when:**

- A demo run's ledger total is within 5% of the Console's figure for the same period (same key, nothing else using it).
- The demo script shows an estimate before starting, and its run summary shows estimate vs actual per fix.
- With a run budget below the full run's cost, the orchestrator stops cleanly, defers the remaining errors, and
  `-Resume` with a larger budget completes them.
- When the provider refuses with a credit or usage limit, the run stops at the first refusal with a clear message
  (no cascade of failed errors).

## 10. Risks and open questions

| Risk / question | Note |
|---|---|
| Prices go stale | `as_of` is shown wherever cost is shown; reconciliation (4.4) catches drift where it is available |
| Cache-write tokens not exposed by the adapter | Resolved: the Anthropic adapter reports them as `AdditionalCounts["CacheCreationInputTokens"]` (measured in F1 step 1) |
| Adapter changes how it counts input | The adapter's `InputTokenCount` includes cached tokens today; an upgrade that changes this would double-count or undercount. A unit test pins the subtraction, and reconciliation (4.4) catches drift |
| Estimates mislead on new task kinds | Always a range with its basis; history-based only after ≥ 5 similar tasks |
| The ledger as a privacy concern | Counts and ids only — no prompts, file contents or tool output |
| Shared API key blends usage | Recommend a dedicated key or workspace for louis-agent |
| Budgets per user? | Not until the API has user identities; today budgets are per instance, service and host |

---
[Docs index](../README.md) · Related: [Response optimisation](RESPONSE_OPTIMISATION.md) · [TODO](../../TODO.md)
