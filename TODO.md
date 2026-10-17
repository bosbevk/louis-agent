# TODO

What's planned, what's next, and the ideas not planned yet — in that order.

**The goal it all works toward:** [docs/GOAL.md](docs/GOAL.md) — become an AI engineer by building louis-agent. This
list is the current plan for getting there and will change as I learn; the goal names the skills it should cover.

**The end product:** a general AI agent with a South African flavour, released to the public (aimed at 2027). It knows
where its users are (SAST, rands, local context) and looks up what changes (scores, SARS, load-shedding) instead of
relying on training. What that needs beyond the skills is under *Backlog: South African general AI*.

**How this list works**

- **Planned work is a feature.** Each feature has a design doc in [docs/features/](docs/features/README.md) with user
  stories and steps, and exactly **one line under *Now***. When a feature's *Done when* is met, delete that line and
  add its entry to the [CHANGELOG](CHANGELOG.md); the feature doc says what else to update.
- **Each line says what it replaces.** Older backlog items that a feature covers were folded into it, so nothing
  appears twice.
- **Bigger designs live in [docs/specs/](docs/specs/).** This list keeps only their open items.
- **Bugs are fixes.** Each has a doc in [docs/fixes/](docs/fixes/README.md) (symptom, cause, fix, how it was confirmed);
  an open one has one backlog line here that links to it.
- **When something is finished**, it leaves this list: one line at the top of the [CHANGELOG](CHANGELOG.md) (newest
  first), so everything here is still open.

## Now: usage, budgets and cheaper responses

From the specs [Usage, estimates and budgets](docs/specs/USAGE_AND_BUDGETS.md) and
[Response optimisation](docs/specs/RESPONSE_OPTIMISATION.md). Work through the milestones in order; each is useful on
its own.

**M1 — See it** is done: every request's tokens and cost recorded and visible (F1–F3, in the
[CHANGELOG](CHANGELOG.md)).

**M2 — Cheaper:** stop paying full price for the same tokens.

- [ ] [F15 Model limits](docs/features/F15-model-limits.md) — each model's context window and cache minimum beside its
      prices, `num_ctx` sent to local models, and F9's compaction point (below the price step: under 100k on Haiku 5.5)
      and F13's budget per model instead of one number for all. *From the local triage runs:* `num_ctx` and `num_gpu`
      per request for the orchestrator (8k fits fully on a 4 GB GPU, 32k doesn't), and `think: false` sent when
      thinking is off ([Local triage](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#local-triage-2026-10-12))

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
      *Removes the limitation:* 10 tool rounds per message. *Takes over* Louis's "can we make it something we can pass
      per session?" (2026-10-15): a session's `max_tool_rounds` when it opens (`POST /sessions`, `StartSubSession`), a
      default per role in `roles.json`, enforced by a guard beside FX14's `KeepWithinPromptBudget` (`ToolMode.None` at
      the limit, so the history stays append-only) with `MaximumIterationsPerRequest` as a ceiling. Not urgent for
      the cook: it never reached 10 rounds in 42 measured turns (*Next up: a faster, cheaper cook*)
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
- [ ] Docs: a README for the sample, a section in [the runbooks](docs/projects/orchestrator-runbooks.md) on runbooks as specs, links from the
      docs index

Keep in mind: one method per task (louis-agent has 10 tool rounds per message, and a long conversation re-sends a
growing history every round, so smaller tasks are cheaper); make runs resumable after a usage limit, as the fix demo's
`-Resume` is; and tests only prove what the runbook examples cover, so the human review stays essential.

## Backlog: not planned yet

### South African general AI (the end product)

A public, culture-specific general agent for the South African market. Not planned yet: each line becomes a feature
when its turn comes. Check demand first (what people would pay for, and which local part matters most) before building
the product parts.

- [ ] **Validate demand:** ask 10-20 likely users what they'd pay and which local part matters most (language, price,
      local tools, WhatsApp); let that pick the first launch feature
- [ ] **SA context in the prompt:** a `Skills/` file that sets the user's place (South Africa, SAST, rands, "the game"
      means the Springboks or Bafana) so the agent asks less and assumes right; measured on the evals (F18)
- [ ] **Local tools:** live lookups for what training can't know: sports results, SARS dates, load-shedding schedules,
      municipal services, rand prices (a general toolset profile, F5, instead of today's coding tools)
- [ ] **All 11 official languages, measured:** eval cases per language; route to a local model where it beats the
      general one (Lelapa AI's Vulavula, UCT's MzansiLM as a base to fine-tune)
- [ ] **Voice:** speech-to-text and text-to-speech, including local languages (add to the goal's skill map, skill 19)
- [ ] **WhatsApp as a host**, next to the CLI, web app and Rider
- [ ] **Accounts and multi-tenancy:** sign-in, a user per session, durable sessions and per-user memory (*Sessions and
      memory*)
- [ ] **A sandbox per user** for tools that run code or touch files, so one user can't reach another's data
- [ ] **Rand pricing and billing:** plans below ChatGPT's R149/month, kept safe by the ledger, caching and per-user
      budgets (F7)
- [ ] **POPIA:** consent, retention and deletion of user data, and data kept in South Africa
- [ ] **Abuse handling and moderation**, plus the *Safety* gaps below (rate limiting, prompt-injection defences)
- [ ] **Hosting:** uptime, monitoring and running cost for public traffic (with *Shipping* below)

### Quality and CI

- [ ] Automated tests for the API and the web app (bUnit or Playwright); start with the Branches queue (several clicks in
      a row, a conflict partway through)
- [ ] End-to-end tests for the orchestrator against a scripted louis-agent.api (today only its parts are unit-tested;
      the whole loop runs only in the demo, against a real model)

### Orchestrator and agents

Design and open decisions: [docs/specs/ORCHESTRATOR_DESIGN.md](docs/specs/ORCHESTRATOR_DESIGN.md).

- [ ] **Relook at how often Puls delegates (Louis, 2026-10-14):** maybe too often. In every delegation check
      ([roles](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-roles/README.md), [after FX10–FX12](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-fixes/README.md),
      [three runs after FX10–FX14](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-repeat/README.md): +31% on average) delegating cost more than one agent answering, except plain chat and some web research: each
      specialist starts cold and the answer is written twice. Options to measure: Puls keeps cheap read-only
      toolsets itself (`files`, `git`, `web`) and delegates only long or risky work; a rule for when a task is worth a
      specialist; F5's *Done when* ("delegate only where the ledger shows it is cheaper"). The next check runs on a
      changed prompt: the docs refresh (PR #30) changed `default.md`'s `StartSubSession` line to the call without
      `agent` (FX10), so compare it with the repeat run with that in mind
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
- [ ] [F22 Role tools](docs/features/F22-role-tools.md) — the tools Puls's specialists need: today's date, attachments, documents, git history, tables, currency ([plan](docs/plans/F22-role-tools.md)). Built; open: a specialist reading an attached PDF in a real chat.
      - [ ] Binary attachments: an attached PDF, Word file or spreadsheet is read as text in Puls's web app and
            carried as a string to the specialist, so it arrives mangled; send it as base64 with its media type and
            read it with `DocumentText` (found in the docs review, 2026-10-14)
- [ ] [F21 Personalities](docs/features/F21-personalities.md) — each session (and sub-session) names its personality; Puls speaks everyday South African Afrikaans ([plan](docs/plans/F21-personalities.md)). Built; open: Louis's corrections to the Afrikaans (the delegation check's `answers.md`).
- [ ] **F31 follow-ups from the review of PR #55** (small, 2026-10-17): the orphan cleanup's time filter; a deadline
      for a run, and a way to cancel one; the leaks the review found in the Benchmarks page; the dozen labels still in
      Afrikaans in the admin portal (the checks' manuals, `HANDLEIDING.md`, among them); and the stale doc lines it
      listed (the build review, `F31-benchmarks-portal-build-review.md` in the Claude project, at its bottom).
- [ ] **Why a benchmark run behaved as it did** (to plan; F31 step 11c, split off when F31 finished, 2026-10-17):
      a note by a person on a run and on a conversation (*Tavily out of credits: no web search in this run*), kept
      in the benchmark store with when it was written, shown on the run's row and as a marker on its trend point;
      and the search provider and page renderer in a run's settings (`BenchmarkSettings`), once they can be chosen
      (F32). Each turn's tool counts are already kept when it's recorded (review of PR #55).
- [ ] [F30 Local web peer](docs/features/F30-local-web-peer.md) — built, not in use: a session names its LLM profile, and a specialist can hand one task and one of its own tools to a minion on a cheap model (`MINION_LLMS`, depth 2, 16 per turn); Puls's prompts don't use minions until F31 measures the cook with and without them ([plan](docs/plans/F30-local-web-peer.md))
- [ ] **Next up: a faster, cheaper cook** (to plan; Louis, 2026-10-15: "can we have a subsession allow him to spawn
      web sessions for each task"). The cook does every search and fetch itself, one at a time, and every page it reads
      stays in its context and is paid for again on each later request (400–800k tokens and up to 183 s a meal turn).
      - [x] **Where the time goes, measured** (the ledger, 42 first cook turns, 2026-10-15): 5 rounds on average, 9 at
            most, every turn ended by the cook itself (`stop`), never at the 10-round limit; the largest request 31k
            tokens on average (66k at most, under FX14's 80k). Model time 49 s a turn on average (100 s at most), 9.7 s
            a request; the cook writes ~9,100 output tokens a turn, and the final report alone (~4,850) takes ~24 s.
            The rest of a turn's wall time is tool time and Puls's own requests
      - [ ] **First: less output.** The report is half the model time: a shorter report shape, a thinking budget for
            the cook, or fewer columns; measured with `run_check.py --personalities` (seconds, output tokens, priced
            ingredients must not drop)
      - [ ] Then, for the tool time, one of two:
      - [ ] **A fan-out tool, no extra model:** `PriceAtShops(ingredients, shops)` runs the shops' searches in parallel
            (`Task.WhenAll`, Tavily with `site:`) and returns one compact table (price, link, source per cell); testable
            with `StubHandler`, no architecture change
      - [ ] **Workers at depth 2:** the cook opens a web-only sub-session per task (a shop's prices, the branches), in
            parallel, each returning a small table. Needs: `specialists` at depth 1 for the cook only (in code), a peer for
            `puls_agent`, `AllowConcurrentInvocation` with thread-safe guards, a per-depth limit instead of
            `MaxPerTurn = 3`, the ledger roll-up through two levels, the Werk card nested
- [ ] [F25 Local cook](docs/features/F25-local-cook.md) — Puls's first local specialist: a South African meal, the recipe and a shopping list priced in rand at the shops near the user; the routing and the sources measured ([plan](docs/plans/F25-local-cook.md), [the run](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-cook/README.md)). The first of the *Local tools* above. Built; open:
      - [x] **Tavily, measured** ([one run from the portal](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-cook-20261015-130521/README.md), 2026-10-15): no search refused, prices read from product pages, unconfirmed marks on the meals down from 20 to 2 and 0
      - [ ] **The cook's unconfirmed prices:** it writes *geskat* (an estimate) where its role says *unknown*; decide which, then measure again
- [ ] [F29 Validated specialists](docs/features/F29-validated-specialists.md) — `PERSONALITIES_ENABLED` gates the roles an instance may use; Puls boots with `specialists` (then called `agents`) and `web`, answers the web itself and hands only cooking to the cook, until each other role has a report shape, a relay rule and a check like the cook's ([plan](docs/plans/F29-validated-specialists.md))
- [ ] [F26 Specialist follow-up](docs/features/F26-specialist-follow-up.md) — a sub-session stays open for its chat and Puls continues it by handle (`ContinueSubSession`), so a follow-up on the cook's meal goes to the same cook with its context, not to a new one cold ([plan](docs/plans/F26-specialist-follow-up.md)). Built; open:
      - [x] **The firmer follow-up rule, measured** ([one run from the portal](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-cook-20261015-130521/README.md), 2026-10-15): every follow-up continued the same cook, 4 of 4, with `web` on Puls too (was 3 of 6 and 0 of 6)
      - [x] **The Werk panel names the role** and shows a continue as its own item (2026-10-15)
      - [ ] **A continued sub-session's own ledger rows name the turn that opened it:** the peer session's `parent` tag is fixed at creation (`{session}#{turn}`), so the specialist's requests during a follow-up turn are attributed to turn 1 in the ledger (the portal's explorer shows turn 2's specialist lane empty); only the `sub_session` roll-up row lands in the right turn. Found in the explorer, 2026-10-15. Fix: send the parent turn with each message (the API's message endpoint takes no tags today), or tag the child's requests from the roll-up
- [ ] **F23 Credits** (to plan): buy credits with money, spend them per question at cost plus a margin, from the usage
      ledger and prices (F1, F2) — Puls's *Koop krediete*. Louis: "'n layer wat geld na ons interne krediete verander. Wil so
      bietjie geld maak as ek kan."

### Web app

- [ ] [F28 Team talk](docs/features/F28-team-talk.md) — Puls briefs its specialist in a fixed note (what the user wants, how they sound from their typing, where they are, what they said), with a per-chat *Lees my stemming* switch sent per turn; the delegation card shows the exchange as a thread; both checks score it ([plan](docs/plans/F28-team-talk.md))
- [ ] Sync chat history across devices and browsers instead of only `localStorage`
- [ ] A UI for skills (the agent's own in `Skills/`, the built-in ones in `louis-agent.core/Prompts/`) and for reviewing and approving agent-built tools — today that's typing `/tools`,
      `/approve <Name>` and `/reject <Name>` in a chat
- [ ] Tests for the web app's `UsageFormat`, which repeats core's `UsageText` number and currency rules (F3, F14) with
      no tests of its own: share the rules (a small project both reference) or test both against the same strings

### CLI

- [ ] Resume a previous session after a restart (the web app keeps its chats; the CLI and ACP server don't)

### Tooling and repositories

- [ ] **F32 follow-up: SearXNG's results** (from F32's run): filter them to the site asked for (Bing, often the one
      engine answering, ignores `site:`), and retry once without the time range when a freshness filter leaves no
      engine.
- [ ] **F33 follow-up: more shop catalogues** (to plan): Shoprite (Checkers' sister store, likely the same Sixty60
      catalogue), Pick n Pay and Woolworths as providers under `Tools/Shops/Providers`; and a delivery area per shop
      (Louis's Malmesbury idea: Checkers keeps it in cookies) for local stock.
- [ ] **CL14 The public word: role** (to plan; CL13 renamed the code, `RoleNamingTests` lists what's left): `role` on `POST /sessions` and in `StartSubSession` (the
      golden files move), `ROLES_ENABLED`, `GET /roles`, the web apps, the benchmarks' Python and the error texts; the
      old names still accepted for a while.
- [ ] **F32 follow-up: one egress proxy for the browser** (to plan; review of PR #56): `puls-browser` reaches the
      internet only through a proxy that resolves each name once, refuses internal addresses and connects to the address
      it checked, so DNS rebinding (Chrome resolving again after `RenderPolicy` checked) and a Web Worker's WebSocket
      can't reach `puls`, `puls-agent` or the host. Until then, set `AGENT_API_KEY` wherever the stack is reachable.
- [ ] **F32 follow-up: SearXNG on only where it's meant to be** (re-review of PR #56; Louis's call): the Puls compose
      file sets `SEARXNG_URL` by default, so a sold deployment without a Tavily key would search through SearXNG, against
      ADR 0023 (its engines don't license their results). Option: leave `SEARXNG_URL` unset in compose and set it in a
      development machine's `config/.env`, so SearXNG is switched on on purpose.
- [ ] **CL12 Integrations by role** (cleanup, to plan after F32; Louis: "this should also be interfaced", then "it would
      not follow the same pattern as Web"): the integrations laid out as the web seams are, a contract per role at the top
      and its vendors in `Providers/`: `Tools/Integrations/TimeTracking/` (`ITimeTracker`; Paymo) and `WorkItems/`
      (`IWorkItemTracker`; Azure DevOps), the shared HTTP, Basic auth, error and JSON plumbing in one place
      (`IntegrationClient`, or a `DelegatingHandler`: the plan weighs both), the tool classes using only the role
      contracts, the choice of vendor an enum (ADR 0024). The toolset names (`paymo`, `devops`) stay unless the plan
      decides otherwise.
- [ ] **Choices still made by string** (cleanup; ADR 0024, F32 step 1b): make each an enum parsed once with
      `SettingChoice` when it's next touched: the LLM provider (`LlmOptions.Provider`, `"anthropic"`), the rates format
      (`RatesMappers.For`), a skill's script language (`SkillTools`, `AgentBuiltTools`, `MarkdownSkillLoader`), the MCP
      server's `MCP_TRANSPORT`, and `ListPackages`' kind (`DotNetTools`).
- [ ] **MCP servers as a session toolset** (to plan after F32; Louis, 2026-10-17: "An MCP toolset for the model"): a
      session names MCP servers as it names toolsets (F5, so its prefix stays cacheable), their tools wrapped with our
      guards (untrusted-data framing, oversized results, failures with reasons) and their versions pinned, so a
      vendor's server can be used without its tool list changing under a chat.
- [ ] Search and work across more than one mounted repository at once (`REPOSITORIES_PATH` mounts several, but every
      tool is scoped to one workspace)
- [ ] Share agent-built tools between projects or teammates (each repository builds its own today)

### Agent architecture gaps

From the [goal](docs/GOAL.md)'s architecture map: skills it requires that nothing above plans yet. Promote one to a
feature when its turn comes; remove its line here and update the map's *Missing* column.

- [ ] **One session-stream reader** (cleanup, CL11; to plan after F31): the session's SSE stream (the API's
      `content_block_*`, `message_delta`, `sub_session`, `usage` events) is read by four separate switches, in
      `web.shared/TurnStream.cs`, the orchestrator's `LouisAgentClient`, core's `SpecialistTools` and F31's Python
      harness, so a new event (F30's minions) has to be learnt four times or a reader quietly miscounts. One C# reader,
      lines in and typed events out, in a small project the browser app can reference too, under the three C# readers;
      F31 step 7's recorded stream (`benchmarks/fixtures/session-stream.sse`) as the contract the Python reader is held
      to as well (Louis, 2026-10-17)
- [ ] **Exception strategy (a cleanup with its own ADR, after F28):** four kinds, each with its rule: a failure the model
      can act on (a `ToolMessage` with the reason, as the web and peer tools do), a setting the operator must fix (fail
      fast or a 400 naming it), a transient fault (one retry in the provider strategy, then reported), a bug (propagates,
      logged with a correlation id, never into the model's history as an outcome). One base exception carrying its kind
      and message member, so hosts map by kind; a tool guard helper that catches only the expected kinds (Paymo's 17
      `catch (Exception)` blocks swallow bugs and cancellation alike); the API's turn error as an enum text with the kind,
      not the exception's type name; an architecture test that fails a filterless `catch (Exception)` outside the hosts'
      top-level handlers. First measurable win: a cancelled turn no longer scored as a tool failure (noted 2026-10-15)
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

- [ ] Metrics and tracing (e.g. OpenTelemetry) beyond the stderr and JSON-lines logs: latency and error dashboards
      (usage and cost are covered by F1–F3 and F11)

## Known limitations

Facts about today, not tasks; the one list (the development guide and the docs link here). When work removes one,
delete it here (see [Finishing a feature](docs/features/README.md#finishing-a-feature)). Where a feature or backlog
item removes one, it says so.

1. **Rider MCP discovery only logs** Rider's tools; they aren't callable yet (needs a full MCP client).
   → *Agent architecture gaps: Protocols* (backlog).
2. **Sessions are in memory** in the API and ACP server; a restart ends them (the web app keeps the transcript).
   → *Sessions and memory: durable sessions* ([session architecture](docs/specs/SESSION_ARCHITECTURE.md), backlog).
3. **Ollama tool support is a name heuristic** (`LlmOptions.KnownNoToolsPrefixes`).
4. **Paymo task lookup takes the first match** for a name; an ambiguous name can hit the wrong task.
5. **No rate limiting or audit log** beyond the file logs. → *Agent architecture gaps: Safety* (backlog).
6. **10 tool rounds per message** (`MaximumIterationsPerRequest`): a long task stops mid-way and needs a follow-up
   message to continue. The orchestrator does this automatically ("Continue the fix…"); most demo fixes take 2-3
   messages. → [**F10** Route settings](docs/features/F10-route-settings.md).
7. **Running the API with `dotnet run` (unpublished)** only serves the web app in the `Development` environment
   (`ASPNETCORE_ENVIRONMENT=Development`); otherwise `/` returns 404. The Docker images are published, so they're fine.
8. **Local models get a truncated prompt.** The agent sends ~26,000+ tokens (system prompt and tools) and doesn't tell
   Ollama the window, so Ollama uses its own default (2,048 on the version measured here; newer ones pick 4k–256k by
   memory) and drops the rest without an error: the ledger showed `input: 2050`. The `ollama-nvidia` and `ollama-amd`
   containers default to 32k, which covers it; the CPU `ollama` service still uses Ollama's default.
   → [**F15** Model limits](docs/features/F15-model-limits.md) (sends `num_ctx`) and
   [**F13** Local models](docs/features/F13-local-models.md).
9. **Changing the tools or skills during a chat can break it on Claude Haiku 5.5** (the default). The model rejects a
   changed system prompt or tool list once thinking blocks are in the history (enforced for accounts created on or
   after 2026-08-31). `BeginTurn` sends the engine's live tool list, so a tool the model creates (`CreateTool`) breaks
   the rest of that message; `/approve`, `/reject` and a skill reload break the next one; and in the API every open
   chat with the same toolsets shares one engine and its tool list (F5). Start a new chat afterwards.
   → [**FX4** Append-only history](docs/fixes/FX04-append-only-history.md).
