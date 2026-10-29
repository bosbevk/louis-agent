# TODO

What's planned, what's next, and the ideas not planned yet — in that order.

**The goal it all works toward:** [docs/GOAL.md](docs/GOAL.md) — become an AI engineer by building louis-agent. This
list is the current plan for getting there and will change as I learn; the goal names the skills it should cover.

**The end product:** three offers on one engine, in [the platform plan](docs/plans/platform.md): **Puls**, the public
South African assistant, a local guide that knows where its users are (SAST, rands, local context) and looks up what
changes instead of relying on training; **a configured agent**, an identity and its packs hosted for a business; and
**the engine itself**, for teams that build their own. The plan's phases set the order of this list.

**How this list works**

- **One item per line.** Every planned feature or fix has exactly one line, in its phase's section; what an item takes
  in from older lines is named on its line, so nothing appears twice.
- **Planned work is a feature.** Each feature has a design doc in [docs/features/](docs/features/README.md) with user
  stories and steps, or its phase in [the platform plan](docs/plans/platform.md) until it gets one. When a feature's
  *Done when* is met, delete its line and add its entry to the [CHANGELOG](CHANGELOG.md); the feature doc says what else
  to update.
- **A feature holds about ten steps.** A bigger one is a parent whose line lists its children (`F49.1`, `F49.2`, …)
  in order, each with its done-when; each child is its own plan, branch and pull request
  ([docs/CLAUDE.md](docs/CLAUDE.md), rule 2). A child leaves the parent's line when it is done.
- **Bigger designs live in [docs/specs/](docs/specs/).** This list keeps only their open items.
- **Bugs are fixes.** Each has a doc in [docs/fixes/](docs/fixes/README.md) (symptom, cause, fix, how it was confirmed);
  an open one has one line here that links to it.
- **When something is finished**, it leaves this list: one line at the top of the [CHANGELOG](CHANGELOG.md) (newest
  first), so everything here is still open.

## Now: after F18, evals as content, the cook, localisation, Puls's specialists and the sales playground (Phases 3b to 4)

Phases 0 to 3 are done (the CHANGELOG has each one). In this order, one at a time:

- [ ] Validate demand: ten conversations and 30 real Malmesbury questions, answered blind by Puls, Gemini, ChatGPT and Google Maps (Louis's own, Phase 0 step 4)
- [ ] **F18's leftovers, before F49.1** (Louis, 2026-10-29): Louis labels the judge's 36 answers himself (the 31 of 36 was a blind agent's labels)
- [ ] The reference transcripts in git: a TODO of its own if they grow (some are over 200 KB today, F18)
- [ ] **F49 Evals as content** (Phase 3b; parent plan in its own thread; Louis, 2026-10-28: "it should be simple to make evals, edit them and from the admin panel, living in identity"), in order: **F49.1** the checks contract out of the harness and core's general catalogue, with its ADR (done when a check is a catalogue entry any project can reference) · **F49.2** the cook's and routing's evals as files beside the Puls identity, run by the runner (`evals/louis-agent.evals.puls` empty, the cook's grades unchanged on replay) · **F49.3** checks in packs, the shops pack's first (a pack lists its checks like its tools) · **F49.4** the studio: samples and known-bads made from conversations and human review (a sample is made from a chat without Rider) · **F49.5** the studio: compose checks, type the rubric, try on the replay cache, publish as a pull request, promptfoo in and out (a new eval is made, tried and published from the admin panel)
- [ ] **F35 Recipe sources** (Phase 3c; `packs/louis-agent.pack.recipes`; free sources only; `research/sa-recipes-api.md` in the Claude project; question 14): **F35.1** the JSON-LD reader, the `Recipe` record, the sources and `RecipeTools` (`FindRecipes` and `ReadRecipe` answer from recorded pages) · **F35.2** the cook reads recipes, the recipes eval, docs (the cook's recipe run measured and written up); once they land, a `local-sources` grader on the cook eval (the old `sources_local` score, dropped in F18 step 7c.2d)
- [ ] **F48 Recipe maths and a teaching cook** (Phase 3c, after F35.1; plan [F48-cook-and-teacher.md](docs/plans/F48-cook-and-teacher.md), decisions settled 2026-10-26; our own C# ingredient-line reader, no Cooklang): **F48.1** `WriteRecipe`: the ingredient-line reader, scaling, the shopping list and the prices, Stats SA's average prices first, our own rows for the gaps (a scaled recipe and its priced list come from code, never the model) · **F48.2** the pantry (a global catalogue, stock per browser id, Mongo in the recipes pack, paste a list now, a photo later) and the teaching, cues and whys in every answer (the cook knows what's in the pantry and teaches, in its eval run); wave 1 of [PULS_COOK.md](docs/specs/PULS_COOK.md) (the food-safety guard, the cheaper pack size, SA dietary-guideline nudges, cook mode, copy the list, "how did it go?") goes in F48 or follows it, Louis to decide
- [ ] **F46 Localisation** (Phase 3d; Louis, 2026-10-25: "all text should come from a localisation file", "keep the enums"; branch `feature/F46-localisation`, plan when asked): **F46.1** core and the API: the text out of `[Description]` into `.resx` beside each message enum, the loader, its ADR (no message text left in an attribute) · **F46.2** the web apps: `ChatText`, Puls's page text, Afrikaans (Puls switches language from the files alone)
- [ ] **F50 Puls's local specialists** (Phase 3e; Louis, 2026-10-26: "the next after F18 is 10 local personalities to configure for puls", then "we can add hiker, weatherman"), eval-first (scenarios and graders as F49's files, then the role until a live run passes), each new source a pack named for what it does, never for Puls (ADR 0028), its robots.txt and licence checked first (ADR 0038). **Every specialist answers for the user's municipality** (Louis: "the specialist must focus on local in which municipality the person is in. Or is currently in"): where the user is now when they share it (with consent, POPIA), else where they live, resolved once to its local municipality (a reverse geocode, OSM's boundary) and handed to the specialist in its task, and the answer says which one it used; this needs the municipality lookup out of *Puls local spec* and *Places* below. In order: **F50.1** the `weather` pack (Open-Meteo; its free tier is non-commercial, so public Puls needs the paid plan or its own instance) · **F50.2** the where-to-buy pack (the branch lookup and the estimates out of the cook, F33's and F48's prices generalised) (each a pack with its guide and checks) · **F50.3** gardener, braaimaster, weerman · **F50.4** farmer, sonkrag (home solar, PVGIS), stapper (hiker: OSM trails, permits) · **F50.5** stargazer, padreis (road trip: route, SANRAL tolls, the DMRE fuel price), hengelaar (angler: Open-Meteo Marine and DFFE's rules; SA tide tables are copyrighted) · **F50.6** voëlkyker (birder, SABAP2), hanyman (home DIY, estimates), and the cook on the new packs (each role child: three role files, each with its eval, run before it is shown) · **F50.7** the front end shows them: the landing page and the chat say what each one does with an example question, and **a user can chat to a specialist directly** (Louis: "I want the users to also chat to the specialists directly"): its card opens a new chat as a level-0 mode of Puls (`puls/{specialist}`, ADR 0019) fixed when the chat opens (ADR 0006), its function shared by the mode and the role without a copy (a layout question for `SkillsLayoutTests`, ADR 0017, decided in the plan); every "kom binnekort" placeholder hidden, not shown disabled (21 buttons in 8 components today; `OnComingSoon`, `ChatPage.ComingSoon`) (no not-implemented marker left on Puls). A hand-off from a Puls chat to a specialist mid-conversation is F39's (Phase 6). The brainstorm is `plans/puls-ten-specialists-brainstorm.md` in the Claude project
- [ ] **F40 The sales playground** (Phase 4, on its own compose, `docker/docker-compose.demo.yml`): **F40.1** the demo identity, the customers pack and the invoice clerk with its simulation (`demo-invoice-batch` passes its bar on a recorded run) · **F40.2** the area guide and its simulation (`demo-area-guide` graded) · **F40.3** the control room in `web.shared`, any simulation's grades, *Try it*, docs (a pitch is given from the room)

## Next: the control layer, then what a sale needs (Phases 5 to 10)

- [ ] [F7](docs/features/F07-budgets.md) Cost you can see and stop (Phase 5; takes in [F6](docs/features/F06-estimates.md), [F11](docs/features/F11-usage-tab-and-reconciliation.md)): **F7.1** a customer on every row and estimates (an estimate with its basis before a run) · **F7.2** budgets that warn, ask or stop, the room's cost view, the bill check, docs (a budget stops a run cleanly and the ledger matches the bill)
- [ ] F39 Orchestration (Phase 6; takes in [F17](docs/features/F17-orchestrator-agent.md), [F8-S4](docs/features/F08-reliability.md)'s structured results between agents, the orchestrator's end-to-end test, F7's deferral, workflows, multi-agent, the recovery contract for bad data; **and hand-offs** (Louis, 2026-10-24: "say I want to do handoffs?", approved with the one-agent-API answer): beside delegation, where the caller keeps the user, a hand-off gives the conversation to another agent: a typed `HandOff(Peer, Role, Summary, Reason)` in `louis-agent.core/Delegation/` and a `HandOffTo` tool checked against the turn's `DelegationPolicy` (peer allowed, role offered there, summary within its cap) that ends the turn with a `hand_off` stream event (CL18's format); the agent only asks and the gateway moves the chat (Phase 9), so a model never sends a person to a server of its choosing (OWASP Agentic ASI07); until Phase 9 the harness plays the gateway; a simulation compares delegation and hand-off on the same place questions (cost per turn, tokens, the grade), GOAL skill 10's *handoff patterns compared*; it goes into F39's plan when that is written). In order: **F39.1** the delegation function: transport, credentials per peer, policy and limiter, `TurnContext`, the town as a turn input (every host delegates through one function) · **F39.2** typed results and the route classifier (a child's answer is a typed, checked result) · **F39.3** the voorman identity, its packs, the fix call and `ConfirmFix` (voorman runs on core with no code of its own in core) · **F39.4** the triage workflow, the exceptions agent, the recovery contract, the seeded service, the measure and docs (`voorman-triage` passes on the seeded service)
- [ ] [FX4 Append-only history](docs/fixes/FX04-append-only-history.md) (first in Phase 7; the prerequisite of F36 and of F43's durable sessions; a written or approved skill arrives as a tool result, not a system prompt reload, [ADR 0033](docs/adr/0033-what-the-agent-writes-has-a-scope.md); removes known limitation 9)
- [ ] F41 Drafts and approvals (Phase 7; takes in drafts, human in the loop, structured outputs, every problem listed at once, `ToolEffect`, the approvals screen with FX20's skill files and a page of the skills)
- [ ] F42 Safety and audit (Phase 7; takes in the safety gaps, abuse handling and moderation, output guardrails, the untrusted-content frame, `IncludeDetailedErrors` reviewed, the egress proxy, repository mounts, branch protection, merges by person; removes known limitation 5): **F42.1** the untrusted-content frame, the audit log and rate limits (every outside text arrives framed and every effect is logged) · **F42.2** moderation and output guardrails, the egress proxy, mounts and branch protection, the injection simulations, docs (the injection suite passes)
- [ ] F36 MCP servers as packs (Phase 8; takes in "MCP servers as a session toolset", a full MCP client, annotations and output schemas on our MCP server, our guards on every path; removes known limitation 1): **F36.1** guarded functions and the MCP export with annotations (our server publishes guarded tools) · **F36.2** `McpPack`, names, the start-up boundary, approval and trust (a server plugs in as a pack with our guards on every call) · **F36.3** `RiderMcpClient` replaced, new chats pick up new packs, the simulation, docs (no hand-written MCP client left)
- [ ] F43 Accounts, tenants and durable sessions (Phase 9; takes in accounts, a sandbox per tenant, POPIA, durable sessions, the MongoDB layer, storage by access pattern and lifecycle states, sync across devices, CLI resume; removes known limitation 2): **F43.1** sign-in and a tenant on every session, row and sandbox (nothing runs without a tenant) · **F43.2** durable sessions and the storage layer by access pattern (a session survives a restart) · **F43.3** sync across devices, CLI resume, the tenant's POPIA report (a chat continues on another device). **From the security read of the eval runner (2026-10-26, with FX25):** the runner as its own job with its own small identity (an Azure Container Apps job, or a Kubernetes Job), not the API's container and user; sign-in (SSO) with roles and a log of who started which run, instead of one shared admin key and a typed name; and how long a run's transcripts (customer data) are kept and where (POPIA, data residency)
- [ ] [F8 Reliability](docs/features/F08-reliability.md) (Phase 9; takes in timeouts and idempotency, the exception strategy)
- [ ] [F15](docs/features/F15-model-limits.md) Model limits and local models (Phase 9; takes in [F13](docs/features/F13-local-models.md), the capability check per model; removes known limitations 3 and 8)
- [ ] F44 The engine as a product (Phase 9; takes in [F12](docs/features/F12-response-speed.md), its S1 first, observability's exporters and dashboards, shipping, notices and the container's licences, split packages, `AddLouisAgent`, OpenTelemetry metrics): **F44.1** speed (`ttft_ms`, tokens per second) and observability's exporters and dashboards (a dashboard shows every host's traces) · **F44.2** split packages, `AddLouisAgent`, notices and licences (a host is one package reference and one call); F44.3 and F44.4 are Phase 10's, below
- [ ] **F51 Hosting for public traffic** (Phase 9, with F44): **F51.1** the Puls gateway in front of the one agent API, hand-offs applied there (Puls's front door is the gateway) · **F51.2** where Puls and the configured agents run, uptime and alerting on the traces, the monthly cost beside the ledger (an outage pages someone and the month's bill is on the admin page)
- [ ] **Phase 10, the engine as a developer package**, after every engine feature (Louis, 2026-10-20: "make the engine its own repo with package"): CL17 widened (*Later*), then **F44.3** Stage A, packaged in place: no internals granted to hosts, a public API baseline, deterministic builds, lock files, a consumer test and a replayed simulation, published behind a release gate (the package builds the same twice and a consumer test uses it) · **F44.4** Stage B, its own repo with its history and the engine guide pack (`louis-agent-engine` is the source of the package); then F47 (*Later*)

## Puls (between the phases, one at a time, in this order)

- [ ] Puls local spec (`docs/specs/puls-local.md`: the place registry, the business registry and its tiers, the municipality theme, POPIA for location)
- [ ] Cook for the week (wave 2 of [PULS_COOK.md](docs/specs/PULS_COOK.md), after F48): a week on a budget, using up leftovers, cheap this month from Stats SA's series, diet and allergy rules in code, swaps that fit the dish, fixing what went wrong with a confidence check
- [ ] CL16 Reusable roles (the cook into `packs/louis-agent.pack.cooking`, under the identity's frame)
- [ ] A domain stop-list for the browser: `RenderPolicy` refuses a listed domain, read from a setting or a file, so a site that asks us to stop ([SITE_OWNERS.md](docs/SITE_OWNERS.md)) is honoured without a release (FX21's review)
- [ ] Sources as data (ADR 0032; `ShopKind` becomes the platform; shops and recipe sites as rows; robots.txt and sitemap indexing)
- [ ] Places (`packs/louis-agent.pack.places`; takes in municipal services; the geocoder behind `IGeocoder`; the area guide moves into Puls)
- [ ] Local businesses (listed, always marked as an ad; providers through their platform, a price list or their MCP server)
- [ ] Vision: `ScreenshotPage(url)` and images as input (question 15)
- [ ] Local lookups: scores, SARS dates, load-shedding, "the game" means the Springboks or Bafana, each a pack when a simulation shows demand
- [ ] F23 Credits (takes in rand pricing and billing)
- [ ] Puls with its own shop tools prices a town with no shop at the default store (`puls-tools` mode, every cook run, main included: `wrong_store` 5 in `14c0907fdff5`, 6 in `b4a6ea22b178`): Puls looks no town up as the cook does; a fix doc when it's taken up
- [ ] The cook's unconfirmed prices: *geskat* or *unknown*, decided, then measured ([F25](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F25-local-cook.md)'s open item)
- [ ] A shorter cook report
- [ ] FX: binary attachments sent as files, not text ([F22](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F22-role-tools.md)'s open item)
- [ ] FX: a continued specialist's ledger rows name the turn that continued it ([F26](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F26-specialist-follow-up.md)'s open item)
- [ ] [F21](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F21-personalities.md): Louis's corrections to the Afrikaans
- [ ] [F28 Team talk](docs/features/F28-team-talk.md), the rest: the mood switch, off by default (question 4: switch), the card as a thread and the Werk chip, the briefing and the mood graded by Puls's simulations
- [ ] Shop prices come back per retailer, with its permission or feed (emails to PnP, Woolworths and Checkers sent 2026-10-25), through *Sources as data* and *More shops*; until then the cook gives estimates and names the shop's branch (F18 step 5.7)
- [ ] More shops: Shoprite, Pick n Pay and Woolworths, and a delivery area per shop, after Sources as data, each only with the retailer's permission or feed

### Later for Puls

- [ ] The cook's wave 3 ([PULS_COOK.md](docs/specs/PULS_COOK.md)): ask about the step you're on and hands-free voice (after cook mode), a photo check (after vision), voice notes in and WhatsApp out (Phase 9's front door), the dishes you've mastered (after accounts)
- [ ] A Puls-owned set of a few hundred South African recipes (its licence decided in an ADR first)
- [ ] WhatsApp as a host
- [ ] The eleven official languages, measured
- [ ] Voice

## Later (and every GOAL Missing item no phase plans yet)

- [ ] F45 Knowledge and retrieval (likely the first after Phase 9, if the conversations confirm it; takes in knowledge on demand, grounding's retrieval, long-term memory and retrieval)
- [ ] Searching PDFs, investigate much later (Louis, 2026-10-26): the engine or a pack takes in PDFs and searches them by meaning and by keyword (contextual chunks, hybrid search fused by rank, a reranker), each result citing its page; part of F45. The standard pipeline and the open questions: [Design: searching PDFs](docs/specs/PDF_SEARCH.md)
- [ ] F47 The CLI and Rider's host on the agent API (Phase 10, after the engine package; **F47.1** Rider's ACP host as a thin translator · **F47.2** the CLI, each speaking only HTTP and SSE to the API; Louis, 2026-10-25: "after the engine package we refactor the acp fully", "the cli must also", "make it one Feature"): both become thin clients of the one agent API (`POST /v1/sessions` and its SSE stream), so the engine, its logs and the ledger live in one host; open: how the API reaches Rider's own tools (Rider's MCP server on the developer's machine) and the repository each chat has open
- [ ] [F9](docs/features/F09-context-management.md) Long chats (takes in [F16](docs/features/F16-summariser.md) and the context-building step)
- [ ] [F10](docs/features/F10-route-settings.md) Settings per role and per session (takes in `max_tool_rounds` per session, thinking measured per level and interleaved thinking, stop conditions beyond a round count, the model per role; removes known limitation 6)
- [ ] CL17 The coding identity (Phase 10, widened so core keeps no tools; takes in CL12, a pull-request tool, the web app chosen by `IDENTITIES`, the default mode named by an identity): **CL17.1** the general packs (files, web, git, dotnet, shell, delegation, skills, clock), the guards staying core services (core holds no tool) · **CL17.2** the coding identity and the Rider host on them (the code-review app runs on the identity)
- [ ] The browser's user agent names the user's town per visit (FX21's later step: the visit receives the user's place as the shop tools receive `area`; never the person)
- [ ] A tool added mid-chat, after FX4 (`tool_addition` behind `LlmClientFactory`)
- [ ] F38 Tool search, when a measured need comes
- [ ] Planning and a self-check before answering, tried on a simulation first
- [ ] An error tracker behind `IErrorFeed`, after F39
- [ ] A webhook trigger beside polling, after F39
- [ ] Computer use, when a simulation needs it
- [ ] An Inspect AI log export for a run (F18 step 6b; Louis, 2026-10-26): Inspect AI's log format is not in F18; when a run must be read by other tools, the runner writes one beside its protocol
- [ ] CL21 Mutation testing with Stryker.NET (`dotnet-stryker` 5.0.0, Apache 2.0, a dotnet local tool in `.config/dotnet-tools.json`; Louis, 2026-10-25): it breaks the code one change at a time and runs the tests, so a line no test guards shows up as a mutant that survived; run by hand on the files a step changed (`dotnet stryker --mutate …`), its report kept out of the repo. It replaces the throwaway mutation probes in reviews; a library fact our code relies on (MEAI reads the content-capture variable once per process) stays a learning test beside that code, so CI runs it. Its own branch off `main`, with its plan
- [ ] Function skills in the Agent Skills format (Louis, 2026-10-26; with Phase 7, its own ADR): `Skills/agent/functions/` become `SKILL.md` folders with `name` and `description` frontmatter; **every role, mode and guide gets the same header** (Louis, 2026-10-28, F18 step 7c's card H): YAML front matter with the spec's `name` and `description`, ours (`level`, `toolsets`) under `metadata`; the built-in prompts stay embedded ("for safety we embed", [ADR 0018](docs/adr/0018-built-in-prompts-embedded.md) holds) ([F48's plan](docs/plans/F48-cook-and-teacher.md))
- [ ] CL23 Explore a Roslyn analyzer for the names rule (Louis, 2026-10-27: "add it as something to explore"; not planned): F18 step 7.5's `RepeatedNameTests` reads the sources as text with its own lexer and a growing list of skip rules (a key, a razor comment, an enum's body); an analyzer runs inside the compiler, knows what each literal is, underlines `"puls-cook"` in Rider as you type and can offer the fix (`PulsRole.Cook.Wire()`). Its own small project and tests; look at it if the ratchet starts getting in the way. [ADR 0043](docs/adr/0043-repeated-names-are-enums.md) (on F18)
- [ ] [F30](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F30-local-web-peer.md)'s minions tested: the cook measured with and without minions, after this plan (Louis, 2026-10-19)
- [ ] Build mode, a side note: an idea to look into after this plan, not planned (question 5, Louis, 2026-10-19)
- [ ] The Azure app behind a key: `puls-app` serves a public ingress with `AGENT_AUTH=off` (CL19 kept it, loudly commented, and locked the ingress to Louis's home IP with `deploy.ps1 lock`), so the Puls web app must send `AGENT_API_KEY` (or a sign-in) before the lock can come off

## Small cleanups (each when its code is next touched)

- [ ] Design docs for the four projects that have none: `louis-agent.choices`, `louis-agent.session-stream`, `louis-agent.api.contracts`, `louis-agent.evals.template` (CL24 found them; [projects](docs/projects/README.md))
- [ ] F16-S1: the oversized-results report, and the worst offenders paged
- [ ] `VerifyQuote`'s prices in the host's currency (`HOST_CULTURE`)
- [ ] Remove CL14's last aliases now, with no deploy to wait for ([ADR 0049](docs/adr/0049-nothing-is-compatible-before-a-sale.md)): `PERSONALITIES_ENABLED` (core's `AgentOptions` and Puls's compose), and `personality` read from what was recorded before CL14 (a chat's delegation card, a stored run, the benchmark protocol and its fixture). CL20 removed the route and the session field
- [ ] Typed peer bodies (FX27, Louis's pick): the sub-session tools send their create and message bodies to the peer as anonymous objects (`SpecialistTools`, `MinionTools`, `SubSessionMessenger`, `SessionReplay.ForSubSession`); ideally one shape both sides read, the API's `CreateSessionRequest` and `SendMessageRequest` in `louis-agent.api.contracts` (ADR 0041, ADR 0045), under its own plan
- [ ] Choices still made by string, the LLM provider first; the web app's `GitChangeAsync("stage" | "unstage" | "discard")` too (CL20 review)
- [ ] The session request bodies (`CreateSessionRequest`, `SendMessageRequest`, `AttachmentRequest`, `ReplayRequest`) into `louis-agent.api.contracts`, their parsing as extension members in the API, when a change touches them or a client (the eval runner's `HttpSessionClient`, the orchestrator) switches to the contract (F18's unattended run, finding 3: the runner and the orchestrator still build these bodies themselves)
- [ ] An OpenAPI document for the agent API (`Microsoft.AspNetCore.OpenApi`, the handlers on `TypedResults` so it knows each answer), for the engine package's `Hosts.Api` (CL20, [ADR 0039](docs/adr/0039-api-routes-under-v1.md))
- [ ] SearXNG on only on purpose
- [ ] SearXNG's results filtered to the site asked for
- [ ] F31's review follow-ups
- [ ] Web tests: the Branches queue, on Phase 4's bUnit project
- [ ] The portal's old benchmark routes and words removed, after Phase 3
- [ ] Remove the `/v1/admin/benchmarks/…` route aliases (F18 step 7b (iii)): every eval route also answers under
  `/v1/admin/evals/…` (`AdminEndpoints.Prefixes`, and the trend's old `/benchmarks/checks/{check}/trend`), so once the
  Puls admin pages call only the new names and a deploy has passed, drop `"/benchmarks"` from `Prefixes`, the old
  trend line and `GET /admin/benchmarks` (with the page that reads it), and regenerate `ApiRoutesSnapshot.json`;
  `EvalRouteAliasTests` goes with them
- [ ] Remove the eval store's old setting names (F18 step 7b (iii)): `AdminOptions` reads `BENCHMARKS_MONGO_URL` and
  `BENCHMARKS_MONGO_DATABASE` after `EVALS_MONGO_URL` and `EVALS_MONGO_DATABASE`. First move what still sets the old
  ones: `config/.env.secrets` on each machine, the CI workflow's `BENCHMARKS_MONGO_URL`, the Mongo contract tests
  (`EvalStoreSubjects`, `HumanReviewStoreMongo`, `CalibrationStoreMongo`, which read only the old name) and the
  Azure secret's name (`benchmarks-mongo-url`); then drop the two fallbacks, the compose's
  `${BENCHMARKS_MONGO_DATABASE-…}` and the alias test in `AdminOptionsTests`
- [ ] Remove `PULS_API`, the eval runner's old name for the API's address (F18 step 7b (iii)): the API sets
  `AGENT_API` now, and `HttpSessionClient.ApiFrom` reads `PULS_API` after it so a shell or a script that still sets
  the old name keeps working; once none does, drop it there, from `RunnerEnvironmentTests`' handled list and from
  `ApiAddressTests`
- [ ] Paymo, DevOps and Rider out of core (core layout review, 2026-10-19): `Tools/Integrations/Paymo` and `Tools/Integrations/DevOps` are packs in waiting (CL17); `Hosting/RiderMcpToolDiscovery` and `Protocols/Mcp/RiderMcpClient` serve only the ACP host and name a product, so they move to `src/louis-agent.acp-server`
- [ ] The rest of the size list (CL15, 2026-10-20): 33 files over 200 lines (400 for tests), held by `FileSizeTests`' `Oversized`; split by responsibility a group at a time, on its own branch
- [ ] A price format per country (CL15 review, 2026-10-20): `Tools/Web/Quotes/QuoteMatch` knows the rand sign, yet core names no country; an `IPriceFormat` seam in core, the South African one from Puls or the shops pack (with CL17)
- [ ] A pack can't add itself to a router (CL15 step 4): the shops' routing line lives in its guide, which loads only in a session that already has the toolset, so the coding agent never learns that shops exist. Costs nothing today; with F36 the routing rows come from each toolset's description
- [ ] Does an unanswered request belong in the usage ledger? A cancelled request is a record of 0 tokens, unpriced, counted as one request today (the stream golden pins it, F18 step 1e); decide before the ledger's rules are written down.

## Known limitations

Facts about today, not tasks; the one list (the development guide and the docs link here). When work removes one,
delete it here (see [Finishing a feature](docs/features/README.md#finishing-a-feature)). Each pointer names the item
above that removes it.

1. **Rider MCP discovery only logs** Rider's tools; they aren't callable yet (needs a full MCP client).
   → **F36** MCP servers as packs (*Next*, Phase 8).
2. **Sessions are in memory** in the API and ACP server; a restart ends them (the web app keeps the transcript).
   → **F43** Accounts, tenants and durable sessions (*Next*, Phase 9;
   [session architecture](docs/specs/SESSION_ARCHITECTURE.md)).
3. **Ollama tool support is a name heuristic** (`LlmOptions.KnownNoToolsPrefixes`).
   → [**F15** Model limits and local models](docs/features/F15-model-limits.md) (the capability check per model).
4. **Paymo task lookup takes the first match** for a name; an ambiguous name can hit the wrong task.
5. **No rate limiting or audit log** beyond the file logs. → **F42** Safety and audit (*Next*, Phase 7).
6. **10 tool rounds per message** (`MaximumIterationsPerRequest`): a long task stops mid-way and needs a follow-up
   message to continue. The orchestrator does this automatically ("Continue the fix…"); most demo fixes take 2-3
   messages. → [**F10** Settings per role and per session](docs/features/F10-route-settings.md) (*Later*).
7. **Running the API with `dotnet run` (unpublished)** only serves the web app in the `Development` environment
   (`ASPNETCORE_ENVIRONMENT=Development`); otherwise `/` returns 404. The Docker images are published, so they're fine.
8. **Local models get a truncated prompt.** The agent sends ~26,000+ tokens (system prompt and tools) and doesn't tell
   Ollama the window, so Ollama uses its own default (2,048 on the version measured here; newer ones pick 4k–256k by
   memory) and drops the rest without an error: the ledger showed `input: 2050`. The `ollama-nvidia` and `ollama-amd`
   containers default to 32k, which covers it; the CPU `ollama` service still uses Ollama's default.
   → [**F15** Model limits and local models](docs/features/F15-model-limits.md) (sends `num_ctx`; takes in F13).
9. **Changing the tools or skills during a chat can break it on Claude Haiku 5.5** (the default). The model rejects a
   changed system prompt or tool list once thinking blocks are in the history (enforced for accounts created on or
   after 2026-08-31). `BeginTurn` sends the engine's live tool list, so a tool the model creates (`CreateTool`) breaks
   the rest of that message; `/approve`, `/reject` and a skill reload break the next one; and in the API every open
   chat with the same toolsets shares one engine and its tool list (F5). Start a new chat afterwards.
   → [**FX4** Append-only history](docs/fixes/FX04-append-only-history.md) (first in Phase 7).
