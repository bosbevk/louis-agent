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
- [ ] [F18](docs/features/F18-evaluation.md) Simulations in C# (Phase 3; takes in CL11, the Python checks, "evaluation in CI", "prompts compared on the evals", notes on a run (F31 step 11c), the response cache as a replay cache, traces per turn and tool call; added 2026-10-20, in the platform plan's source until Phase 3's plan is copied: the word *evals* for benchmarks, datasets as files with input and expected behaviour, graders by kind (fixed rule, model, human review) with a fixed rule required, the models a run used, results overall and by category, every eval visible in the admin, the admin pages rebuilt under CL15's rules (W2b.6), and a test for the grader scoring one behaviour two ways)
- [ ] CL18 A pluggable stream format (Phase 3 step 1e, with the session-stream reader: `ITurnStreamFormat`, `ClaudeStreamFormat`, `"stream"` on `POST /sessions`)
- [ ] Personal data out of the logs: the place and the user's text hashed in the logs (Phase 3 step 4f, with F18; Part 3 row 14)
- [ ] F40 The sales playground (Phase 4: the demo identity, the customers pack, the invoice and area-guide simulations, the control room, Try it, on its own compose, `docker/docker-compose.demo.yml`)

## Next: the control layer, then what a sale needs (Phases 5 to 9)

- [ ] [F7](docs/features/F07-budgets.md) Cost you can see and stop (Phase 5; takes in [F6](docs/features/F06-estimates.md), [F11](docs/features/F11-usage-tab-and-reconciliation.md))
- [ ] F39 Orchestration (Phase 6; takes in [F17](docs/features/F17-orchestrator-agent.md), [F8-S4](docs/features/F08-reliability.md)'s structured results between agents, the orchestrator's end-to-end test, F7's deferral, workflows, multi-agent, the recovery contract for bad data)
- [ ] [FX4 Append-only history](docs/fixes/FX04-append-only-history.md) (first in Phase 7; the prerequisite of F36 and of F43's durable sessions; a written or approved skill arrives as a tool result, not a system prompt reload, [ADR 0033](docs/adr/0033-what-the-agent-writes-has-a-scope.md); removes known limitation 9)
- [ ] F41 Drafts and approvals (Phase 7; takes in drafts, human in the loop, structured outputs, every problem listed at once, `ToolEffect`, the approvals screen with FX20's skill files and a page of the skills)
- [ ] F42 Safety and audit (Phase 7; takes in the safety gaps, abuse handling and moderation, output guardrails, the untrusted-content frame, `IncludeDetailedErrors` reviewed, the egress proxy, repository mounts, branch protection, merges by person; removes known limitation 5)
- [ ] F36 MCP servers as packs (Phase 8; takes in "MCP servers as a session toolset", a full MCP client, annotations and output schemas on our MCP server, our guards on every path; removes known limitation 1)
- [ ] F43 Accounts, tenants and durable sessions (Phase 9; takes in accounts, a sandbox per tenant, POPIA, durable sessions, the MongoDB layer, storage by access pattern and lifecycle states, sync across devices, CLI resume; removes known limitation 2)
- [ ] [F8 Reliability](docs/features/F08-reliability.md) (Phase 9; takes in timeouts and idempotency, the exception strategy)
- [ ] [F15](docs/features/F15-model-limits.md) Model limits and local models (Phase 9; takes in [F13](docs/features/F13-local-models.md), the capability check per model; removes known limitations 3 and 8)
- [ ] F44 The engine as a product (Phase 9; takes in [F12](docs/features/F12-response-speed.md), its S1 first, observability's exporters and dashboards, shipping, notices and the container's licences, split packages, `AddLouisAgent`, OpenTelemetry metrics)
- [ ] Hosting for public traffic (Phase 9, with F44: where Puls and the configured agents run, uptime and alerting on the traces, the running cost per month beside the ledger)

## Puls (between the phases, one at a time, in this order)

- [ ] Puls local spec (`docs/specs/puls-local.md`: the place registry, the business registry and its tiers, the municipality theme, POPIA for location)
- [ ] F35 Recipe sources (`packs/louis-agent.pack.recipes`; free sources only; the JSON-LD reader first; `research/sa-recipes-api.md` in the Claude project; question 14)
- [ ] CL16 Reusable roles (the cook into `packs/louis-agent.pack.cooking`, under the identity's frame)
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
- [ ] More shops: Shoprite, Pick n Pay and Woolworths, and a delivery area per shop, after Sources as data, each only with the retailer's permission or feed

### Later for Puls

- [ ] A Puls-owned set of a few hundred South African recipes (its licence decided in an ADR first)
- [ ] WhatsApp as a host
- [ ] The eleven official languages, measured
- [ ] Voice

## Later (and every GOAL Missing item no phase plans yet)

- [ ] F45 Knowledge and retrieval (likely the first after Phase 9, if the conversations confirm it; takes in knowledge on demand, grounding's retrieval, long-term memory and retrieval)
- [ ] [F9](docs/features/F09-context-management.md) Long chats (takes in [F16](docs/features/F16-summariser.md) and the context-building step)
- [ ] [F10](docs/features/F10-route-settings.md) Settings per role and per session (takes in `max_tool_rounds` per session, thinking measured per level and interleaved thinking, stop conditions beyond a round count, the model per role; removes known limitation 6)
- [ ] CL17 The coding identity (takes in CL12, a pull-request tool, the web app chosen by `IDENTITIES`, the default mode named by an identity)
- [ ] A tool added mid-chat, after FX4 (`tool_addition` behind `LlmClientFactory`)
- [ ] F38 Tool search, when a measured need comes
- [ ] Planning and a self-check before answering, tried on a simulation first
- [ ] An error tracker behind `IErrorFeed`, after F39
- [ ] A webhook trigger beside polling, after F39
- [ ] Computer use, when a simulation needs it
- [ ] [F30](docs/features/F30-local-web-peer.md)'s minions tested: the cook measured with and without minions, after this plan (Louis, 2026-10-19)
- [ ] Build mode, a side note: an idea to look into after this plan, not planned (question 5, Louis, 2026-10-19)
- [ ] The Azure app behind a key: `puls-app` serves a public ingress with `AGENT_AUTH=off` (CL19 kept it, loudly commented, and locked the ingress to Louis's home IP with `deploy.ps1 lock`), so the Puls web app must send `AGENT_API_KEY` (or a sign-in) before the lock can come off

## Small cleanups (each when its code is next touched)

- [ ] F16-S1: the oversized-results report, and the worst offenders paged
- [ ] `VerifyQuote`'s prices in the host's currency (`HOST_CULTURE`)
- [ ] Remove CL14's aliases, after a deploy
- [ ] Choices still made by string, the LLM provider first
- [ ] SearXNG on only on purpose
- [ ] SearXNG's results filtered to the site asked for
- [ ] F31's review follow-ups
- [ ] Web tests: the Branches queue, on Phase 4's bUnit project
- [ ] The portal's old benchmark routes and words removed, after Phase 3
- [ ] Paymo, DevOps and Rider out of core (core layout review, 2026-10-19): `Tools/Integrations/Paymo` and `Tools/Integrations/DevOps` are packs in waiting (CL17); `Hosting/RiderMcpToolDiscovery` and `Protocols/Mcp/RiderMcpClient` serve only the ACP host and name a product, so they move to `src/louis-agent.acp-server`
- [ ] The rest of the size list (CL15, 2026-10-20): 33 files over 200 lines (400 for tests), held by `FileSizeTests`' `Oversized`; split by responsibility a group at a time, on its own branch
- [ ] A price format per country (CL15 review, 2026-10-20): `Tools/Web/Quotes/QuoteMatch` knows the rand sign, yet core names no country; an `IPriceFormat` seam in core, the South African one from Puls or the shops pack (with CL17)
- [ ] `ToolMessage` split per toolset (core layout review, 2026-10-19): one enum of every tool's texts hides which toolset each belongs to; one enum per toolset beside its tools, as the shops pack has (`ShopToolMessage`), with the golden split the same way
- [ ] A pack can't add itself to a router (CL15 step 4): the shops' routing line lives in its guide, which loads only in a session that already has the toolset, so the coding agent never learns that shops exist. Costs nothing today; with F36 the routing rows come from each toolset's description

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
