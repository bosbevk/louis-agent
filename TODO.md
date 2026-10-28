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
- **Bigger designs live in [docs/specs/](docs/specs/).** This list keeps only their open items.
- **Bugs are fixes.** Each has a doc in [docs/fixes/](docs/fixes/README.md) (symptom, cause, fix, how it was confirmed);
  an open one has one line here that links to it.
- **When something is finished**, it leaves this list: one line at the top of the [CHANGELOG](CHANGELOG.md) (newest
  first), so everything here is still open.

## Now: three doors, the cut, simulations and the sales playground (Phases 0 to 4)

- [ ] Validate demand: ten conversations and 30 real Malmesbury questions, answered blind by Puls, Gemini, ChatGPT and Google Maps (Louis's own, Phase 0 step 4)
- [ ] [F18](docs/features/F18-evaluation.md) Evals in C# (Phase 3; takes in CL11, the Python checks, "evaluation in CI", "prompts compared on the evals", notes on a run (F31 step 11c), the response cache as a replay cache, traces per turn and tool call; added 2026-10-20, its plan now [F18-evals.md](docs/plans/F18-evals.md): the word *evals* for benchmarks, datasets as files with input and expected behaviour, graders by kind (fixed rule, model, human review) with a fixed rule required, the models a run used, results overall and by category, every eval visible in the admin, the admin pages rebuilt under CL15's rules (W2b.6), and a test for the grader scoring one behaviour two ways). Progress: steps 0 to 6a are on the branch, through 6a.5 (2026-10-27: the judge check from the portal, *Check the judge now*) and the review's steps 6a.3b to 6a.3d; step 6b is dropped (the cook's scenarios move to F48's step 7); step 7, Python leaves, is through 7.4 (`benchmarks/` is deleted, `NoPythonTests` keeps it so, and the API no longer runs a script); 7.4 (the API's script path) is done; next is 7.5, repeated names become enums, then Part E (the step list is in the feature doc)
- [ ] FX23 A script that never reads its input is reported as failed: `ProcessRunner` (`louis-agent.core/Tools/ProcessRunner.cs:50-51`) writes the arguments to its stdin, a script that exits first makes that write throw `IOException: Broken pipe`, and the catch-all turns a run that succeeded into "Error running bash: Broken pipe" (exit −1) and drops its output. It makes `ScriptToolTests.CreatedTool_SupportsPowerShellAndBash("bash", …)` flaky (CI run 337, F18 step 4c.2) and is certain with a 1 MB input (`bash -c "echo done"` gives −1, not 0 and "done"). The fix: catch `IOException` around the write and the close; the 1 MB case is the red test; its own branch off `main`, with its fix doc
- [ ] CL18 A pluggable stream format (Phase 3 step 1e, with the session-stream reader: `ITurnStreamFormat`, `ClaudeStreamFormat`, `"stream"` on `POST /v1/sessions`)
- [ ] Personal data out of the logs: the place and the user's text hashed in the logs (Phase 3 step 4f, with F18; Part 3 row 14)
- [ ] F40 The sales playground (Phase 4: the demo identity, the customers pack, the invoice and area-guide simulations, the control room, Try it, on its own compose, `docker/docker-compose.demo.yml`)

## Next: the control layer, then what a sale needs (Phases 5 to 9)

- [ ] [F7](docs/features/F07-budgets.md) Cost you can see and stop (Phase 5; takes in [F6](docs/features/F06-estimates.md), [F11](docs/features/F11-usage-tab-and-reconciliation.md))
- [ ] F39 Orchestration (Phase 6; takes in [F17](docs/features/F17-orchestrator-agent.md), [F8-S4](docs/features/F08-reliability.md)'s structured results between agents, the orchestrator's end-to-end test, F7's deferral, workflows, multi-agent, the recovery contract for bad data; **and hand-offs** (Louis, 2026-10-24: "say I want to do handoffs?", approved with the one-agent-API answer): beside delegation, where the caller keeps the user, a hand-off gives the conversation to another agent: a typed `HandOff(Peer, Role, Summary, Reason)` in `louis-agent.core/Delegation/` and a `HandOffTo` tool checked against the turn's `DelegationPolicy` (peer allowed, role offered there, summary within its cap) that ends the turn with a `hand_off` stream event (CL18's format); the agent only asks and the gateway moves the chat (Phase 9), so a model never sends a person to a server of its choosing (OWASP Agentic ASI07); until Phase 9 the harness plays the gateway; a simulation compares delegation and hand-off on the same place questions (cost per turn, tokens, the grade), GOAL skill 10's *handoff patterns compared*. Approved after `docs/plans/platform.md`, which is never edited, so it goes into F39's own plan when that is copied)
- [ ] [FX4 Append-only history](docs/fixes/FX04-append-only-history.md) (first in Phase 7; the prerequisite of F36 and of F43's durable sessions; a written or approved skill arrives as a tool result, not a system prompt reload, [ADR 0033](docs/adr/0033-what-the-agent-writes-has-a-scope.md); removes known limitation 9)
- [ ] F41 Drafts and approvals (Phase 7; takes in drafts, human in the loop, structured outputs, every problem listed at once, `ToolEffect`, the approvals screen with FX20's skill files and a page of the skills)
- [ ] F42 Safety and audit (Phase 7; takes in the safety gaps, abuse handling and moderation, output guardrails, the untrusted-content frame, `IncludeDetailedErrors` reviewed, the egress proxy, repository mounts, branch protection, merges by person; removes known limitation 5)
- [ ] F36 MCP servers as packs (Phase 8; takes in "MCP servers as a session toolset", a full MCP client, annotations and output schemas on our MCP server, our guards on every path; removes known limitation 1)
- [ ] F43 Accounts, tenants and durable sessions (Phase 9; takes in accounts, a sandbox per tenant, POPIA, durable sessions, the MongoDB layer, storage by access pattern and lifecycle states, sync across devices, CLI resume; removes known limitation 2) **From the security read of the eval runner (2026-10-26, with FX25):** the runner as its own job with its own small identity (an Azure Container Apps job, or a Kubernetes Job), not the API's container and user; sign-in (SSO) with roles and a log of who started which run, instead of one shared admin key and a typed name; and how long a run's transcripts (customer data) are kept and where (POPIA, data residency)
- [ ] [F8 Reliability](docs/features/F08-reliability.md) (Phase 9; takes in timeouts and idempotency, the exception strategy)
- [ ] [F15](docs/features/F15-model-limits.md) Model limits and local models (Phase 9; takes in [F13](docs/features/F13-local-models.md), the capability check per model; removes known limitations 3 and 8)
- [ ] F44 The engine as a product (Phase 9; takes in [F12](docs/features/F12-response-speed.md), its S1 first, observability's exporters and dashboards, shipping, notices and the container's licences, split packages, `AddLouisAgent`, OpenTelemetry metrics)
- [ ] Hosting for public traffic (Phase 9, with F44: where Puls and the configured agents run, uptime and alerting on the traces, the running cost per month beside the ledger)

## Puls (between the phases, one at a time, in this order)

- [ ] **Next after F18: twelve local specialists** (Louis, 2026-10-26: "the next after F18 is 10 local personalities to configure for puls", then "we can add hiker, weatherman"; "let's start with those"), one feature each, eval-first (scenarios and graders, then the role until a live run passes): the cook (done), gardener, farmer, stargazer, braaimaster, sonkrag (home solar, PVGIS), padreis (road trip: route, SANRAL tolls, the DMRE fuel price), hengelaar (angler: Open-Meteo Marine and DFFE's rules; SA tide tables are copyrighted), voëlkyker (birder, SABAP2), hanyman (home DIY, estimates), weerman (weatherman) and stapper (hiker: OSM trails, permits). **Every specialist answers for the user's municipality** (Louis, 2026-10-26: "the specialist must focus on local in which municipality the person is in. Or is currently in"): where the user is now when they share it (the chat's location, with consent, POPIA), else where they live (their profile); the place is resolved to its local municipality once (a reverse geocode, OSM's local-municipality boundary) and handed to the specialist in its task, so every answer (the weather, water restrictions, the branch, the trails, the permits) is that municipality's, and the answer says which one it used. This needs the *Puls local spec* and *Places* lines below first, or at least the municipality lookup out of them. Three shared pieces come first, because most of them need them: the municipality lookup, a `weather` pack (Open-Meteo; its free tier is non-commercial, so public Puls needs the paid plan or a self-hosted instance) and a "where to buy" toolset taken out of the cook (the branch lookup and the estimates). A new source is a pack named for what it does (`weather`, `solar`, `fuel`), never for Puls (ADR 0028), and its robots.txt and licence are checked before it is used (ADR 0038). The brainstorm (criteria, sources, alternates) is `plans/puls-ten-specialists-brainstorm.md` in the Claude project; each specialist gets its plan when it is taken up
- [ ] **Then the Puls front end shows the specialists** (Louis, 2026-10-26): the landing page and the chat say what each one does, with an example question; **a user can also chat to a specialist directly** (Louis: "I want the users to also chat to the specialists directly"): a specialist's card opens a new chat with it, as a level-0 mode of Puls (`puls/{specialist}`, ADR 0019) fixed when the chat opens (ADR 0006), in the user's municipality, while a chat with Puls keeps routing to the same specialists. A specialist's file says it never talks to the user, so its function must be shared by the mode and the role without a copy, a layout question for `SkillsLayoutTests` (ADR 0017) decided in the plan; a hand-off from a Puls chat to a specialist mid-conversation is F39's hand-off (Phase 6), not this line; and every placeholder that only says "kom binnekort" is hidden, not shown disabled (21 buttons in 8 components today: retry, feedback, share, edit, the screenshot and image items, projects, and items in the profile, credits, sidebar and chat menus; `OnComingSoon`, `ChatPage.ComingSoon`)
- [ ] Puls local spec (`docs/specs/puls-local.md`: the place registry, the business registry and its tiers, the municipality theme, POPIA for location)
- [ ] F35 Recipe sources (`packs/louis-agent.pack.recipes`; free sources only; the JSON-LD reader first; `research/sa-recipes-api.md` in the Claude project; question 14)
- [ ] F48 Recipe maths and a teaching cook (after F35's steps 1 to 4; plan [F48-cook-and-teacher.md](docs/plans/F48-cook-and-teacher.md), decisions settled 2026-10-26): our own C# ingredient-line reader, scaler and shopping list on F35's `Recipe`, a `WriteRecipe` tool, estimates from Stats SA's average prices plus our own rows, cues and whys in every answer, graders on data, the pantry (global catalogue, stock per browser id, Mongo in the recipes pack, paste a list now, a photo later); wave 1 of [PULS_COOK.md](docs/specs/PULS_COOK.md) (the food-safety guard, the cheaper pack size, SA dietary-guideline nudges, cook mode, copy the list, "how did it go?") goes in F48 or follows it, Louis to decide
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
- [ ] The cook's unconfirmed prices: *geskat* or *unknown*, decided, then measured ([F25](docs/features/F25-local-cook.md)'s open item)
- [ ] A shorter cook report
- [ ] FX: binary attachments sent as files, not text ([F22](docs/features/F22-role-tools.md)'s open item)
- [ ] FX: a continued specialist's ledger rows name the turn that continued it ([F26](docs/features/F26-specialist-follow-up.md)'s open item)
- [ ] [F21](docs/features/F21-personalities.md): Louis's corrections to the Afrikaans
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
- [ ] F47 The CLI and Rider's host on the agent API (after the engine package; Louis, 2026-10-25: "after the engine package we refactor the acp fully", "the cli must also", "make it one Feature"): both become thin clients of the one agent API (`POST /v1/sessions` and its SSE stream), so the engine, its logs and the ledger live in one host; open: how the API reaches Rider's own tools (Rider's MCP server on the developer's machine) and the repository each chat has open
- [ ] [F9](docs/features/F09-context-management.md) Long chats (takes in [F16](docs/features/F16-summariser.md) and the context-building step)
- [ ] [F10](docs/features/F10-route-settings.md) Settings per role and per session (takes in `max_tool_rounds` per session, thinking measured per level and interleaved thinking, stop conditions beyond a round count, the model per role; removes known limitation 6)
- [ ] CL17 The coding identity (takes in CL12, a pull-request tool, the web app chosen by `IDENTITIES`, the default mode named by an identity)
- [ ] The browser's user agent names the user's town per visit (FX21's later step: the visit receives the user's place as the shop tools receive `area`; never the person)
- [ ] A tool added mid-chat, after FX4 (`tool_addition` behind `LlmClientFactory`)
- [ ] F38 Tool search, when a measured need comes
- [ ] Planning and a self-check before answering, tried on a simulation first
- [ ] An error tracker behind `IErrorFeed`, after F39
- [ ] A webhook trigger beside polling, after F39
- [ ] Computer use, when a simulation needs it
- [ ] An Inspect AI log export for a run (F18 step 6b; Louis, 2026-10-26): Inspect AI's log format is not in F18; when a run must be read by other tools, the runner writes one beside its protocol
- [ ] CL21 Mutation testing with Stryker.NET (`dotnet-stryker` 5.0.0, Apache 2.0, a dotnet local tool in `.config/dotnet-tools.json`; Louis, 2026-10-25): it breaks the code one change at a time and runs the tests, so a line no test guards shows up as a mutant that survived; run by hand on the files a step changed (`dotnet stryker --mutate …`), its report kept out of the repo. It replaces the throwaway mutation probes in reviews; a library fact our code relies on (MEAI reads the content-capture variable once per process) stays a learning test beside that code, so CI runs it. Its own branch off `main`, with its plan
- [ ] Function skills in the Agent Skills format (Louis, 2026-10-26; with Phase 7, its own ADR): `Skills/agent/functions/` become `SKILL.md` folders with `name` and `description` frontmatter; roles, modes and guides stay as they are ([F48's plan](docs/plans/F48-cook-and-teacher.md))
- [ ] CL22 One shared contract for the eval protocol (after F18; Louis, 2026-10-25): today the runner writes the `@{json}` lines by hand (`Protocol/ProtocolWriter`) and the API reads them by hand (`Evals/Runs/Protocol/EvalProtocol`, `SummaryLineReader`) and writes them back by hand in its export (`ProtocolLines`, `SummaryExport`, which repeats the runner's `summary` fields), held together only by the recorded-stream test. A small `louis-agent.evals.protocol` project of one record per event, referenced by both, makes a mismatch a compile error; the transport stays stdout (streaming, one-way dependencies, [ADR 0022](docs/adr/0022-benchmarks-recorded-from-the-protocol.md)). The hand-written writer kept the Python harness's field order, which no longer matters, so ADR 0041's (on F18) "the bytes are the contract" case is reweighed in its plan. Protocol version 2; stored and exported version 1 runs still read. Its own branch off `main`, with its plan
- [ ] CL23 Explore a Roslyn analyzer for the names rule (Louis, 2026-10-27: "add it as something to explore"; not planned): F18 step 7.5's `RepeatedNameTests` reads the sources as text with its own lexer and a growing list of skip rules (a key, a razor comment, an enum's body); an analyzer runs inside the compiler, knows what each literal is, underlines `"puls-cook"` in Rider as you type and can offer the fix (`PulsRole.Cook.Wire()`). Its own small project and tests; look at it if the ratchet starts getting in the way. [ADR 0043](docs/adr/0043-repeated-names-are-enums.md) (on F18)
- [ ] [F30](docs/features/F30-local-web-peer.md)'s minions tested: the cook measured with and without minions, after this plan (Louis, 2026-10-19)
- [ ] Build mode, a side note: an idea to look into after this plan, not planned (question 5, Louis, 2026-10-19)
- [ ] The Azure app behind a key: `puls-app` serves a public ingress with `AGENT_AUTH=off` (CL19 kept it, loudly commented, and locked the ingress to Louis's home IP with `deploy.ps1 lock`), so the Puls web app must send `AGENT_API_KEY` (or a sign-in) before the lock can come off

## Small cleanups (each when its code is next touched)

- [ ] F16-S1: the oversized-results report, and the worst offenders paged
- [ ] `VerifyQuote`'s prices in the host's currency (`HOST_CULTURE`)
- [ ] Remove CL14's last aliases, after a deploy: `PERSONALITIES_ENABLED` (core's `AgentOptions` and Puls's compose), and `personality` read from what was recorded before CL14 (a chat's delegation card, a stored run, the benchmark protocol and its fixture). CL20 removed the route and the session field
- [ ] Choices still made by string, the LLM provider first; the web app's `GitChangeAsync("stage" | "unstage" | "discard")` too (CL20 review)
- [ ] The session request bodies (`CreateSessionRequest`, `SendMessageRequest`, `AttachmentRequest`, `ReplayRequest`) into `louis-agent.api.contracts`, their parsing as extension members in the API, when a change touches them or a client (the eval runner's `HttpSessionClient`, the orchestrator) switches to the contract ([the F18 run's finding 3](docs/plans/F18-unattended-decisions.md))
- [ ] An OpenAPI document for the agent API (`Microsoft.AspNetCore.OpenApi`, the handlers on `TypedResults` so it knows each answer), for the engine package's `Hosts.Api` (CL20, [ADR 0039](docs/adr/0039-api-routes-under-v1.md))
- [x] The rest of `Endpoints/` by concern (CL20): `InfoEndpoints`, `SessionEndpoints` and `AdminEndpoints` split by resource as `Endpoints/Git/` is, and a long handler a named method
- [ ] SearXNG on only on purpose
- [ ] SearXNG's results filtered to the site asked for
- [ ] F31's review follow-ups
- [ ] Web tests: the Branches queue, on Phase 4's bUnit project
- [ ] The portal's old benchmark routes and words removed, after Phase 3
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
