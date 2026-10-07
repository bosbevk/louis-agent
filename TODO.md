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
- **When something is finished**, tick it, then move it to *Done* as one line (newest first).

## Now: usage, budgets and cheaper responses

From the specs [Usage, estimates and budgets](docs/specs/USAGE_AND_BUDGETS.md) and
[Response optimisation](docs/specs/RESPONSE_OPTIMISATION.md). Work through the milestones in order; each is useful on
its own.

**M1 — See it:** every request's tokens and cost recorded and visible.

- [ ] [F1 Usage ledger](docs/features/F01-usage-ledger.md) — record every model request, attributed to session, task
      and run
- [ ] [F2 Prices and cost](docs/features/F02-prices-and-cost.md) — a price table; cost on every record
- [ ] [F3 Show usage](docs/features/F03-usage-display.md) — cost per answer (web, CLI, Rider), in the API stream, and
      per fix in the comms log

**M2 — Cheaper:** stop paying full price for the same tokens.

- [ ] [F4 Prompt caching](docs/features/F04-prompt-caching.md) — cache the ~26,000-token prefix every request re-sends.
      *Replaces:* "Wire up Anthropic prompt caching on the system prompt"
- [ ] [F5 Toolset profiles](docs/features/F05-toolset-profiles.md) — load only the tools a session needs (the demo:
      coding tools only)

**M3 — In control:** know the cost before, stop cleanly at a limit.

- [ ] [F6 Estimates](docs/features/F06-estimates.md) — estimate a run before it starts; estimate vs actual per fix
- [ ] [F7 Budgets](docs/features/F07-budgets.md) — budgets that warn, ask or stop; the orchestrator defers what it can't
      afford
- [ ] [F8 Reliability](docs/features/F08-reliability.md) — retries with backoff, stop at the first account-limit
      refusal, fallback model, results as tool calls. *Replaces:* "Retry and back off when the model API refuses",
      "Add model fallback", "Wire up structured outputs"

**M4 — Long sessions:** long chats and tasks don't grow without limit.

- [ ] [F9 Bounded history](docs/features/F09-context-management.md) — clear stale tool results, then compact old turns.
      *Replaces:* "Consider context editing and compaction"; first slice of the
      [session architecture](docs/specs/SESSION_ARCHITECTURE.md)'s context pipeline
- [ ] [F10 Route settings](docs/features/F10-route-settings.md) — thinking, tool rounds and summary model per route.
      *Removes the limitation:* 10 tool rounds per message

**M5 — Reporting:** an overview and a check against the bill.

- [ ] [F11 Usage tab and reconciliation](docs/features/F11-usage-tab-and-reconciliation.md) — the web app's Usage tab,
      usage API, comparison with the provider's reports. *With F1–F3, F6 and F7, replaces:* "Track token usage / cost
      per session"

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

### Web app

- [ ] Sync chat history across devices and browsers instead of only `localStorage`
- [ ] A UI for skills (`Skills/*.md`) and for reviewing and approving agent-built tools — today that's typing `/tools`,
      `/approve <Name>` and `/reject <Name>` in a chat

### CLI

- [ ] Resume a previous session after a restart (the web app keeps its chats; the CLI and ACP server don't)

### Tooling and repositories

- [ ] Search and work across more than one mounted repository at once (`REPOSITORIES_PATH` mounts several, but every
      tool is scoped to one workspace)
- [ ] Share agent-built tools between projects or teammates (each repository builds its own today)

### Agent architecture gaps

From the [goal](docs/GOAL.md)'s architecture map: skills it requires that nothing above plans yet. Promote one to a
feature when its turn comes; remove its line here and update the map's *Missing* column.

- [ ] **Evaluation:** an eval suite (task datasets, graded scoring incl. LLM-as-judge) run in CI as a regression gate
- [ ] **Thinking:** measure quality vs tokens per thinking level on the evals; interleaved thinking between tool calls
- [ ] **Models:** choose models per task from measured quality and cost
- [ ] **Prompts:** version prompts and compare changes on the evals
- [ ] **Tools:** tool search / on-demand loading for large toolsets; schema-validated structured outputs
- [ ] **Agent loop:** explicit planning, a self-check before answering, stop conditions beyond a round count
- [ ] **Memory and retrieval:** embeddings, a vector store, retrieval over code and past sessions, measured
- [ ] **Multi-agent:** compare handoff patterns (tool call, session, MCP); parallel sub-agents
- [ ] **Protocols:** a full MCP client (also removes the Rider MCP limitation)
- [ ] **Reliability:** timeouts, idempotency and resumable runs in the core
- [ ] **Shipping:** a deploy pipeline for the Docker images, with the eval suite as a release gate
- [ ] **Safety:** prompt-injection defences and red-team tests, output guardrails, rate limiting, an audit log
- [ ] **Human in the loop:** approval policies per action and risk level
- [ ] **Multimodal and computer use:** images and documents as input; browser or computer-use tools

### Observability

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

## Done

Newest first; details in the git history.

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
