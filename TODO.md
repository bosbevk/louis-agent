# TODO

What's planned, what's next, and the ideas not planned yet — in that order.

**The goal it all works toward:** [docs/GOAL.md](docs/GOAL.md) — become an AI engineer by building louis-agent. This
list is the current plan for getting there and will change as I learn; the goal names the skills it should cover.

**How this list works**

- **Planned work is a feature.** Each feature has a design doc in [docs/features/](docs/features/README.md) with user
  stories and steps, and exactly **one line under *Now***. When a feature's *Done when* is met, tick that line — it's
  the only place to tick — and the feature doc says what else to update.
- **Each line says what it replaces.** Older backlog items that a feature covers were folded into it, so nothing
  appears twice.
- **Bigger designs live in [docs/specs/](docs/specs/).** This list keeps only their open items.
- **Bugs are fixes.** Each has a doc in [docs/fixes/](docs/fixes/README.md) (symptom, cause, fix, how it was confirmed);
  an open one has one backlog line here that links to it.
- **When something is finished**, tick it, then move it to *Done* as one line (newest first).

## Now: usage, budgets and cheaper responses

From the specs [Usage, estimates and budgets](docs/specs/USAGE_AND_BUDGETS.md) and
[Response optimisation](docs/specs/RESPONSE_OPTIMISATION.md). Work through the milestones in order; each is useful on
its own.

**M1 — See it:** every request's tokens and cost recorded and visible.

- [x] [F1 Usage ledger](docs/features/F01-usage-ledger.md) — record every model request, attributed to session, task
      and run
- [x] [F2 Prices and cost](docs/features/F02-prices-and-cost.md) — a price table; cost on every record
- [x] [F3 Show usage](docs/features/F03-usage-display.md) — cost per answer (web, CLI, Rider), in the API stream, and
      per fix in the comms log

**M2 — Cheaper:** stop paying full price for the same tokens.

- [x] [F4 Prompt caching](docs/features/F04-prompt-caching.md) — cache the ~48,000-token prefix every request re-sends.
      *Replaces:* "Wire up Anthropic prompt caching on the system prompt"
- [ ] [F5 Toolset profiles](docs/features/F05-toolset-profiles.md) — load only the tools a session needs (the demo:
      coding tools only)
- [ ] [F15 Model limits](docs/features/F15-model-limits.md) — each model's context window and cache minimum beside its
      prices, `num_ctx` sent to local models, and F9's compaction point (below the price step: under 100k on Haiku 5.5)
      and F13's budget per model instead of one number for all. *From the local triage runs:* `num_ctx` and `num_gpu`
      per request for the orchestrator (8k fits fully on a 4 GB GPU, 32k doesn't), and `think: false` sent when
      thinking is off ([Local triage](docs/features/README.md#local-triage-2026-10-12))

**M3 — In control:** know the cost before, stop cleanly at a limit.

- [ ] [F6 Estimates](docs/features/F06-estimates.md) — estimate a run before it starts; estimate vs actual per fix
- [ ] [F7 Budgets](docs/features/F07-budgets.md) — budgets that warn, ask or stop; the orchestrator defers what it can't
      afford
- [ ] [F8 Reliability](docs/features/F08-reliability.md) — retries with backoff, stop at the first account-limit
      refusal, fallback model, results as tool calls. *Replaces:* "Retry and back off when the model API refuses",
      "Add model fallback", "Wire up structured outputs"

**M4 — Long sessions:** long chats and tasks don't grow without limit; the parts that summarise and orchestrate are
measured.

- [ ] [F18 Evaluation](docs/features/F18-evaluation.md) — a small shared eval harness: cases as data, exact and
      LLM-judge scorers, cost and a baseline per run; built with F16, its first suite. *Replaces:* the first part of the
      backlog's *Evaluation* gap
- [ ] [F16 Summariser](docs/features/F16-summariser.md) — fewer oversized results, then one measured summariser with its
      own model, prompt and no caching, reused by F9's compaction. *Takes over:* F10's summary model
- [ ] [F9 Bounded history](docs/features/F09-context-management.md) — clear stale tool results, then compact old turns.
      *Replaces:* "Consider context editing and compaction"; first slice of the
      [session architecture](docs/specs/SESSION_ARCHITECTURE.md)'s context pipeline
- [ ] [F10 Route settings](docs/features/F10-route-settings.md) — thinking and tool rounds per route.
      *Removes the limitation:* 10 tool rounds per message
- [ ] [F17 Orchestrator agent](docs/features/F17-orchestrator-agent.md) — classification by a model, the known paths as
      code, the agent for the exceptions; its own settings and cache policy; measured against today's orchestrator.
      *From the local triage runs:* `EscalateToHuman` idempotent per error, and contradicting decisions in one reply
      (acknowledge and fix) rejected

**M5 — Reporting:** an overview, a check against the bill, and how fast each model is.

- [ ] [F11 Usage tab and reconciliation](docs/features/F11-usage-tab-and-reconciliation.md) — the web app's Usage tab,
      usage API, comparison with the provider's reports. *With F1–F3, F6 and F7, replaces:* "Track token usage / cost
      per session"
- [ ] [F12 Response speed](docs/features/F12-response-speed.md) — time to first token and tokens per second on every
      usage record, for every provider (Ollama's own timings too); shown with the usage
- [x] [F14 Display currency](docs/features/F14-display-currency.md) — show costs in the PC's currency (from the Windows
      region, `HOST_CULTURE`) or a chosen one, with the rate's date; rates live from `RATES_URL` (frankfurter) with a
      `config/rates.json` fallback, served at `GET /rates`; the ledger stays in the bill's currency. *Built before
      M2 by choice* ([plan](docs/plans/F14-display-currency.md))

**Local — Local models:** a local model gets everything it is sent.

- [ ] [F13 Local models](docs/features/F13-local-models.md) — fit every request into the model's context window
      (2,048 tokens on Ollama): compact prompt, a few tools, trimmed history, `num_ctx` sent. *Removes the limitation:*
      local models get a truncated prompt

## Next: build-mode POC — build a whole microservice from runbooks

Show that runbooks can *build* a service, not just fix one: each runbook is the spec for a method, louis-agent builds it
on its own branch, the orchestrator checks it against the runbook's examples, and a person merges it in the web app.
**A new, separate sample** — the current fix demo (`samples/order-service`, `run-demo.ps1`, `sample-output/`) keeps
working unchanged. Start after M2 (F4, F5) and F6, so each build is cached, lean and estimated.

Set-up, separate from the fix demo:

- [ ] Own folder `samples/build-service/`: a near-empty starting repository (solution, empty project, test project) and
      the runbooks that specify its methods
- [ ] Own run script (`samples/run-build-demo.ps1`), compose project, port, `.demo` sub-folder, state directory and
      sample output, so both demos can run side by side
- [ ] Decide: a build mode in `louis-agent.orchestrator` (`ORCHESTRATOR_MODE=build`, reusing `LouisAgentClient`,
      `CommsLog`, the checks and the runbook loader) or a separate orchestrator. Reuse is likely cheaper; either way the
      fix mode must not change

The build flow:

- [ ] **Runbook as spec:** add route, inputs, outputs, business rules and worked examples (input → expected output) that
      double as acceptance tests
- [ ] **Scaffold first:** one branch for the skeleton (project, data access, routing where each method registers its own
      route in its own file, test project), merged before any method
- [ ] **One method per branch:** a build request per runbook (implement in `Api/<Method>.cs`, tests for the runbook's
      examples, build, test, commit on `feature/<method>`, report the result), with "continue" follow-ups as for fixes
- [ ] **Check against the spec:** verify the commit, call the method with every runbook example, run the whole suite;
      anything failing is escalated, not merged
- [ ] **Dependencies:** a runbook can name methods it needs (e.g. `vat` uses `order-total`); build in that order, each
      branch from a `main` that already has them
- [ ] **Review and merge** in the web app's Branches tab; the comms log records every request, reply and check
- [ ] Optional: louis-agent **drafts the runbooks** from a written description (or an existing service's code) for a
      person to approve first
- [ ] Docs: a README for the sample, a section in [RUNBOOKS.md](docs/RUNBOOKS.md) on runbooks as specs, links from the
      docs index

Keep in mind: one method per task (louis-agent has 10 tool rounds per message, and a long conversation re-sends a
growing history every round, so smaller tasks are cheaper); make runs resumable after a usage limit, as the fix demo's
`-Resume` is; and tests only prove what the runbook examples cover, so the human review stays essential.

## Backlog: not planned yet

### Quality and CI

- [x] **Project layout, after F4, first of two cleanups** (the rules are in
      [docs/CLAUDE.md](docs/CLAUDE.md#project-layout)): folders by concern (`Agent`, `Llm/{Provider}`, `Tools/{Toolset}`,
      `Skills`, `Guardrails`, `Usage`, `Pricing`, `Protocols/Mcp`, `Hosting`), namespace = folder path in PascalCase,
      `git mv` so history follows, split `AgentEngine` along those concerns, tests mirror the source, and an
      architecture test (namespaces match folders, dependencies point one way). Its own branch and PR, with a plan;
      behaviour unchanged, so every test passes untouched apart from namespaces
- [x] **Split `AgentEngine`, right after the project layout** (its plan comes first): the loop, history and prompt in
      `Agent/`, the tool registry beside it, and the tool-result guards, `ResolvePath`/`IsSensitive` and the SSRF check
      in `Guardrails/`. CL1 moves the class whole; this splits it. Behaviour unchanged
- [x] **Messages and logging, after F4, second of two cleanups** — CL3 ([plan](docs/plans/CL3-messages-and-logging.md); the rule is in [docs/CLAUDE.md](docs/CLAUDE.md#messages-and-logging)): move
      the ~230 inline messages in 35 files (exceptions, log lines, tool results, user-facing text) into message enums
      with `[Description]` templates and a `MessageText` helper, with a test over every enum; switch logging from
      `Console.Error` and `AgentLog`'s files to `ILogger` with Serilog (stderr only, run files and JSON-lines as sinks).
      Its own branch and PR, with a plan
- [ ] CI (`.github/workflows`): `dotnet build` and `dotnet test` on every push and pull request — today regressions are
      only caught locally
- [ ] Automated tests for the API and the web app (bUnit or Playwright); start with the Branches queue (several clicks in
      a row, a conflict partway through)
- [ ] End-to-end tests for the orchestrator against a scripted louis-agent.api (today only its parts are unit-tested;
      the whole loop runs only in the demo, against a real model)

### Orchestrator and agents

Design and open decisions: [docs/specs/ORCHESTRATOR_DESIGN.md](docs/specs/ORCHESTRATOR_DESIGN.md).

- [ ] A pull-request tool (GitHub / Azure DevOps), so a fix ends as a PR instead of a local branch
- [ ] Decide the orchestrator's implementation tier: keep `louis-agent.core` (as the POC) or move to Anthropic's Tool
      Runner
- [ ] Error-tracker tools (e.g. Exceptionless) instead of the demo's JSON-lines log
- [ ] A webhook trigger for error-tracker and uptime alerts, next to polling
- [ ] Branch protection on `main` wherever louis-agent can push (its PR token can usually push to `main` too)
- [ ] Per-service louis-agent.api instances must not mount other repositories (`REPOSITORIES_PATH`): `BashRun` and
      `PythonRun` reach anything mounted (the demo compose file already leaves it out)
- [ ] Record merges made in the web app under the person who clicked, not the server's git identity
- [ ] Regenerate `samples/sample-output/` with the current build (its comms log predates the text-block fix)
- [ ] Self-healing beyond the POC: rollback / kill switch after a bad deploy, synthetic monitoring, a scheduled
      maintenance agent, a dependency / CVE watcher
- [ ] Store operations: a separate orchestrator with catalogue, inventory and pricing tools, and the deploy-gate
      boundary between it and louis-agent

### Sessions and memory

Design: [docs/specs/SESSION_ARCHITECTURE.md](docs/specs/SESSION_ARCHITECTURE.md) (F9 is its first slice).

- [ ] Durable sessions: keep conversations across restarts (removes the *sessions are in memory* limitation)
- [ ] Separate session state from the model's context, with a context-building step (retrieve, rank, budget, compress)
- [ ] Long-term memory and retrieval (past conversations, user facts, external knowledge)
- [ ] Storage by access pattern (hot state, transcripts, vectors, large artifacts) and explicit session lifecycle states
- [ ] **F5 follow-up: roll sub-session usage up into the parent chat.** Sub-session totals (`StartSubSession`) must count in the parent's turn scope (`UsageScope.Add`, summary record only, never written to the ledger) so the per-chat usage line, the chat total and `GET /sessions/{id}` include them. Child cost is converted to USD once, at the parent. Confirm in `Program.cs` that `session.Usage` takes the turn's scope records. Test: parent total = own requests + each child; a failed child's usage is included; no duplicate ledger rows.

### Web app

- [ ] Sync chat history across devices and browsers instead of only `localStorage`
- [ ] A UI for skills (`Skills/*.md`) and for reviewing and approving agent-built tools — today that's typing `/tools`,
      `/approve <Name>` and `/reject <Name>` in a chat
- [ ] Tests for the web app's `UsageFormat`, which repeats core's `UsageText` number and currency rules (F3, F14) with
      no tests of its own: share the rules (a small project both reference) or test both against the same strings

### CLI

- [ ] Resume a previous session after a restart (the web app keeps its chats; the CLI and ACP server don't)

### Tooling and repositories

- [ ] Search and work across more than one mounted repository at once (`REPOSITORIES_PATH` mounts several, but every
      tool is scoped to one workspace)
- [ ] Share agent-built tools between projects or teammates (each repository builds its own today)

### Agent architecture gaps

From the [goal](docs/GOAL.md)'s architecture map: skills it requires that nothing above plans yet. Promote one to a
feature when its turn comes; remove its line here and update the map's *Missing* column.

- [ ] **Evaluation:** run F18's eval suites in CI as a regression gate (the harness itself is F18)
- [ ] **Thinking:** measure quality vs tokens per thinking level on the evals; interleaved thinking between tool calls
- [ ] **Models:** choose models per task from measured quality and cost; check each model's capabilities (real tool
      calls, thinking) by measuring them, not from its name (`LlmOptions.KnownNoToolsPrefixes`; the local triage runs
      showed llama3.1 writing tool calls as text and qwen3's thinking variant timing out)
- [ ] **Prompts:** version prompts and compare changes on the evals
- [ ] **Tools:** tool search / on-demand loading for large toolsets; add a tool
      mid-conversation without breaking thinking or the cache (Anthropic's inline `tool_addition` blocks, behind
      `LlmClientFactory`; an option [FX4](docs/fixes/FX04-append-only-history.md) leaves for later); MCP tool
      annotations (read-only, destructive, idempotent) and output schemas on `louis-agent.mcp-server`'s tools; errors
      that report every problem at once with the valid options, so the model fixes them in one round
- [ ] **Structured outputs:** schema-validated outputs across the toolset (results between agents come first, under
      *Models*)
- [ ] **Drafts:** for an action that changes something outside the workspace, the agent proposes a typed draft that code
      validates (every issue returned with its valid options) and a person approves before it's applied; the model
      retrying on the validation errors is an evaluator-optimizer loop with code as the evaluator. Measure on the evals
      how often a request becomes a valid draft first time (skills 5, 16, 12)
- [ ] **Agent loop:** explicit planning, a self-check before answering, stop conditions beyond a round count
- [ ] **Workflows:** build and compare Anthropic's workflow patterns where they fit: prompt chaining, parallelization,
      evaluator-optimizer (F17 already plans routing; the orchestrator is orchestrator-workers)
- [ ] **Knowledge on demand:** curated, source-cited knowledge topics (front matter: sources, owner, last reviewed)
      looked up through tools and MCP resources when needed, instead of loading every skill file into the system
      prompt; measure the prompt tokens saved and that fixes still hold. Before embeddings
- [ ] **Grounding:** reference data the agent checks values against, versioned with its provenance (source, date,
      hashes, what's synthetic) and returned with every result that used it
- [ ] **Memory and retrieval:** embeddings, a vector store, retrieval over code and past sessions, measured
- [ ] **Multi-agent:** compare handoff patterns (tool call, session, MCP); parallel sub-agents
- [ ] **Protocols:** a full MCP client (also removes the Rider MCP limitation)
- [ ] **Reliability:** timeouts, idempotency and resumable runs in the core
- [ ] **Redis: the fast, short-lived layer.** A response cache: return the stored answer to an *exactly* repeated request
      (key: a hash of model, messages, tools and settings) instead of calling the model, $0 and recorded as a cache hit
      in the ledger, with Redis's own key expiry. Pays off where requests repeat exactly (F16's summaries of the same
      output, F17's triage of a recurring error, re-running F18's evals), hardly in the fix loop, whose context is new
      every round. Also the home for other hot, disposable state (the session architecture's *hot state*: a running
      turn, locks, later rate limits). Not a replacement for F4: Anthropic's prompt cache is on their servers and is what
      bills cached input at 0.1×; ours can only skip whole requests
- [ ] **MongoDB: the durable layer.** Documents that must survive restarts: sessions and their transcripts (removes the
      *sessions are in memory* limitation; the durable-sessions line under *Sessions and memory*), and long-term memory.
      Its vector search can hold a later *semantic* cache (similar, not identical, requests) and retrieval, which need a
      strict similarity threshold and evals, since a near-miss returns a wrong answer. Together with Redis this is the
      session architecture's *storage by access pattern*
- [ ] **Shipping:** a deploy pipeline for the Docker images, with the eval suite as a release gate
- [ ] **Safety:** prompt-injection defences and red-team tests, output guardrails, rate limiting, an audit log
- [ ] **Human in the loop:** approval policies per action and risk level
- [ ] **Multimodal and computer use:** images and documents as input; browser or computer-use tools

### Models

- [ ] [FX4](docs/fixes/FX04-append-only-history.md) Keep conversations append-only for models that bind thinking blocks
      to the history (Haiku 5.5, Sonnet 5.5, Opus 5.5): freeze each conversation's tool list and system prompt, since
      `CreateTool`, `/approve`, `/reject`, a skill reload, or another session change them today. *Removes the
      limitation:* changing the tools or skills during a chat can break it on Haiku 5.5
- [ ] **Structured results between agents, after FX4 and before F5** (F8-S4 pulled forward; its design is in
      [F8](docs/features/F08-reliability.md)): louis-agent ends a fix with a `ReportFixResult` tool call and the
      orchestrator decides with a tool call, validated against their schemas, instead of `FIX-RESULT` / `DECISION` text
      lines a regex has to find (FX8); the text lines stay as a fallback. Its own feature doc and plan

### Observability

- [ ] [FX9](docs/fixes/FX09-cancelled-request-usage.md) Write a request cancelled without streaming (the orchestrator's
      triage, timed out) to the ledger, as a cancelled stream already is
- [ ] Metrics and tracing (e.g. OpenTelemetry) beyond the stderr and JSON-lines logs: latency and error dashboards
      (usage and cost are covered by F1–F3 and F11)

## Known limitations

The same list as [docs/CLAUDE.md](docs/CLAUDE.md#known-limitations); facts about today, not tasks. Where a feature or
backlog item removes one, it says so.

- Rider MCP discovery only logs Rider's tools; they aren't callable yet (needs a full MCP client) → *Agent architecture
  gaps: Protocols*.
- Sessions are in memory in the API and ACP server; a restart ends them → *Sessions and memory: durable sessions*.
- Ollama tool support is a name heuristic (`LlmOptions.KnownNoToolsPrefixes`).
- Paymo task lookup takes the first match for a name; an ambiguous name can hit the wrong task.
- No rate limiting or audit log beyond the file logs → *Agent architecture gaps: Safety*.
- 10 tool rounds per message: long tasks need a follow-up message → **F10**.
- An unpublished API (`dotnet run`) only serves the web app in the `Development` environment.
- Local models get a truncated prompt: the agent sends ~26,000+ tokens (system prompt and tools) without telling Ollama
  the window, so Ollama uses its own default (2,048 on the version measured here) and drops the rest without an error
  (the ledger showed `input: 2050`), except in the `ollama-nvidia` and `ollama-amd` containers, which default to 32k;
  the CPU `ollama` service still uses Ollama's default → **F15** (`num_ctx`) and **F13**.
- On Claude Haiku 5.5 (the default), changing the tools or skills during a chat can make its next request fail with a
  400: the model rejects a changed system prompt or tool list once thinking blocks are in the history (enforced for
  accounts created on or after 2026-08-31). That happens when the model creates a tool (`CreateTool`, mid-message),
  on `/approve`, `/reject` or a skill reload, and in the API for every open chat, since all sessions share one tool
  list. Start a new chat afterwards → [**FX4**](docs/fixes/FX04-append-only-history.md).

## Done

Newest first; details in the git history.

- 2026-10-12 — **Ollama on the GPU and local triage**: `ollama-nvidia` and `ollama-amd` services sized for a 4 GB card
  (32k context, flash attention, 8-bit KV cache); Ollama's client waits 10 minutes, not 100 seconds. The short demo's
  triage on `qwen3:4b-instruct-2507` (profile `.env.ollama-triage`): 4 fixed, 1 escalated, as on Claude, triage $0, the
  run $0.042 against $0.049, 2.4× slower; llama3.1 and qwen3's thinking variant failed
  ([Local triage](docs/features/README.md#local-triage-2026-10-12),
  `samples/sample-output-short-local-triage/`)
- 2026-10-12 — **CL3 Messages and logging**: every exception, log line, tool error and user text is a message enum
  member with a `[Description]` template (486: 313 in core, the rest in each host's own `Messages/`), proved to render
  as before; `ToolMessageSnapshot.json` pins the 190 texts tools return to the model. Logging is `ILogger` with
  Serilog: stderr only, the run file and the JSON-lines records as sinks, holes kept as fields; `AgentLog` is gone.
  The orchestrator logs to stderr too, and `decisions.jsonl` is snake_case like its other records
- 2026-10-12 — **CL2 Split AgentEngine**: 1,078 lines become the engine (the turn loop, 360) and `ToolRegistry`,
  `ToolCallGuards`, `SkillTools`, `AgentBuiltTools` in `Agent/`; pure checks in `Guardrails/` (`WorkspaceBoundary`,
  `SensitiveFiles`, `SsrfGuard`); the console chat in the CLI. A golden file pins what the model is sent; it found that
  the prompt and two descriptions took the checkout's line endings, now always LF
- 2026-10-11 — **CL1 Project layout**: core in folders by concern (`Agent`, `Llm/{Name}Provider`, `Tools/{Toolset}`,
  `Skills`, `Usage`, `Pricing`, `Protocols/Mcp`, `Hosting`, `Config`, `Messages`, `Logging`), the namespace the folder
  path, one type per file; PascalCase namespaces in every project; the tests mirror the source; `ArchitectureTests`
  fails on a namespace off its folder, two types in a file or a dependency against the layers. Behaviour unchanged:
  the same 779 tests pass. Moved with `git mv`, so history follows
- 2026-10-11 — **F4 Prompt caching**: two cache markers on Anthropic (the system prompt with `LLM_CACHE_TTL`, the
  conversation by automatic caching, 5 minutes), as a provider strategy; tools sorted; writes priced at their lifetime
  (`cache_write_1h`); `usage-probe --cache-gaps` recommends the lifetime from the ledger. Full demo $0.13 against M1's
  $0.66 (−80%), louis-agent's input cost −87%, 10/10 fixes; samples in `samples/sample-output-cached/`
- 2026-10-11 — **F14 Display currency**: costs in the PC's currency in every host (rands from `HOST_CULTURE=en-ZA`,
  written by `usage-probe --host-culture`; `R0.034`, `.` decimals), at a dated rate (Frankfurter mapped into our
  `rates.json`, `config/rates.json` as the fallback, served at `GET /rates`), with the rate and the USD charged beside
  converted totals; a currency picker in the web app. The ledger stays in USD. Both demo samples rerun in rands

- 2026-10-11 — [FX8](docs/fixes/FX08-fix-result-quotes.md): a FIX-RESULT whose summary quotes without escaping
  ("Cher") is still read, instead of asking louis-agent to continue three times; found by F14's full demo rerun

- 2026-10-11 — **F3 Show usage** (M1 done): every answer says what it used and cost — a line on the CLI's stderr,
  under each answer in the web app (with the chat's total), in Rider with `USAGE_IN_CHAT=true` — `/usage` in every
  host, `usage` on the API's `message_delta` and `GET /sessions/{id}`, and cost per reply, per error and per run in the
  orchestrator's comms log. The short demo's comms log total equals its ledger's exactly ($0.2526, 57 requests)
- 2026-10-11 — [FX7](docs/fixes/FX07-timeout-ends-run.md): a timed-out request fails that error, not the
  orchestrator's whole run; found with the new `run-demo.ps1 -OrchestratorProfile`

- 2026-10-11 — [FX6](docs/fixes/FX06-usage-on-failed-turns.md): a failed louis-agent turn still reports its usage (on
  the API's `error` event), so the web app's chat total and the orchestrator's comms log match the ledger; built in F3

- 2026-10-11 — [FX5](docs/fixes/FX05-null-conditional-ledger.md): a `?.` in F3's first step skipped the ledger write for
  requests outside a usage scope; F1's tests caught it before the commit

- 2026-10-10 — **F2 Prices and cost**: every ledger record priced from a tracked `config/prices.json` (Haiku 5.5's
  long-prompt tier included), or from the API's `GET /prices` with `PRICES_URL`; `usage-probe -- --ledger` totals a
  ledger; M1 baseline $0.6586 for the full demo ($0.0625 a fix), both sample runs rerun with priced ledgers
- 2026-10-10 — Full demo on Claude Haiku 5.5 with the usage ledger: 10/10 fixes confirmed, 2 ignored, 1 escalated, ≈ $0.66
  (`samples/sample-output/`, now with `usage-2026-10.jsonl`); F2 prices that ledger for the M1 baseline
- 2026-10-10 — [FX1](docs/fixes/FX01-round-limit-tools.md): the engine keeps the tools on the request after the
  10-round limit (`tool_choice: none`), which Haiku 5.5 otherwise rejects; the short demo went from 2/4 to 4/4 fixes
- 2026-10-10 — **F1 Usage ledger**: every model request of every host in `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`, with
  host, session, turn, purpose and the orchestrator's fix and run; `tools/louis-agent.usage-probe`; a short demo
  (`run-demo.ps1 -Short`) saved with its ledger in `samples/sample-output-short/` (4/4 fixes, ≈ $0.27 on Haiku 5.5)
- 2026-10-10 — Default Anthropic model is Claude Haiku 5.5 (about 7.5× cheaper than Haiku 4.5; usage, thinking and
  thinking text checked with `tools/louis-agent.usage-probe`); LLM profiles `config/.env.anthropic` / `.env.ollama`
- 2026-10-07 — Specs and feature plan for usage, budgets and cheaper responses (`docs/specs/`, `docs/features/`); the
  session-architecture and orchestrator designs moved from this list into `docs/specs/`
- 2026-10-07 — Docs: [how the agents and endpoints talk](docs/AGENT_COMMUNICATION.md), [runbooks](docs/RUNBOOKS.md), a
  docs index in reading order
- 2026-10-07 — Web app Branches tab: merges and deletes are queued instead of silently dropped while another runs
- 2026-10-07 — Comms log keeps louis-agent's text blocks apart
- 2026-10-06 — Orchestrator demo in Docker: ten bugs → ten fix branches, Markdown comms log, sample output with a
  recording of the merges
- 2026-10-06 — Web app Branches tab and history endpoints: all commits, branch and commit diffs, merge, delete merged
- 2026-10-06 — Root `.dockerignore`: builds no longer send `config/.env.secrets` or `.git` into the build context
- 2026-10-06 — Orchestrator POC: runbook per API method, fixes delegated to louis-agent.api and verified; `AgentEngine`
  `toolsets:` option
- 2026-10-06 — MCP server: Streamable HTTP transport (`MCP_TRANSPORT=http`) with its own `MCP_API_KEY`
- Earlier — Skill files standardised (titles, tool tables, Python fences); DevOps blockers query fixed (tag, not type);
  sensitive-file checks added to `DeleteDirectory` and `CopyFile`; all 108 tool descriptions rewritten
