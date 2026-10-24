# Plans: how each feature or fix is going to be built

A feature doc ([features/](../features/README.md)) or a fix doc ([fixes/](../fixes/README.md)) says *what* and *why*.
A plan says **how we'll build it now**: written and approved before the first step, with what it changes from the doc
(the code has usually moved on since the doc was written), the steps, and how the result is checked. Plans are kept as
they were approved, so the reasoning at the time stays readable; what the build then changed is recorded in the feature
or fix doc (*What the build changed*), not here.

## Plans

| Plan | For | Approved | Notes |
|---|---|---|---|
| [platform.md](platform.md) | The roadmap: CL15, F18, F40, F7, F39, FX4, F41, F42, F36, F43, F8, F15, F44, Puls's track | 2026-10-19 | One engine, its identities and packs (CL15), C# simulations (F18), the sales playground (F40), then the control layer phase by phase; replaces the CL15 and F39 drafts; the 15 questions answered in it (orchestrator `voorman`, a place specialist with sites as data) |
| [F18-evals.md](F18-evals.md) | TODO: *F18* (Phase 3 of [platform.md](platform.md)); [F18](../features/F18-evaluation.md) | 2026-10-24 | The platform plan's Phase 3, copied with the 2026-10-20 and 2026-10-24 decisions: the word *eval* everywhere (`IEval`, `evals/`, `/admin/evals`); one stream reader and a pluggable writer (CL11, CL18); the Python checks rewritten in C# with datasets as files and graders by kind; recorded web input, a replay cache, the prompt's hash, traces and hashed logs; pass@k, pass^k and categories; every eval in the admin, the API naming no product (ADR 0037) |
| [CL19-config-by-place.md](CL19-config-by-place.md) | TODO: *CL19 Config and Docker by place* (cleanup) | 2026-10-24 | A folder is one kind of thing for one place: `config/` what the app reads (unchanged, so no code moves), `docker/` this PC (`images/`, the stacks, a home override that publishes the browser and Mongo on the Tailscale IP), `deploy/azure/` the cloud (the app yamls with `secretRef`s, one `ts-forwarder` sidecar for both ports, `deploy.ps1`, the how-to); `puls-agent` deleted; `services/` and `config/azure/` go |
| [CL15-engine-identities-packs.md](CL15-engine-identities-packs.md) | TODO: *CL15 Engine, identities and packs* (Phase 2 of [platform.md](platform.md)) | 2026-10-19 | The platform plan's Phase 2, copied as approved: a Puls golden first (step 0); `IAgentIdentity`, `IAgentPack` and one `AgentCatalog`; Puls an identity and shops a pack; `HOST_TIME_ZONE`; every pack convertible to MCP. Done: PR #71 (2026-10-20), its paid cook run on the finished branch |
| [platform-summary.md](platform-summary.md) | The platform plan, for someone outside the project | 2026-10-19 | Two pages to give an investor or partner: what Puls is, what's built and measured, the ten phases, how it makes money, competition and risks; its ask is for Louis to fill in |
| [FX17-cook-labels-and-counts.md](FX17-cook-labels-and-counts.md) | [FX17](../fixes/FX17-cook-labels-and-counts.md) | 2026-10-18 | The cook's two fixed labels in both languages (*Dien*/*Serves*, *Geprys*/*Priced*), and a count of packs after the name, so the photo stays first (the cook's prompt); approved during the build, a follow-up's card naming the cook (`session_role`) |
| [F34-shop-deals.md](F34-shop-deals.md) | [F34 Shop deals](../features/F34-shop-deals.md) | 2026-10-17 | The user's store (Checkers' store contexts for their town, via the site's own address lookup); each product's deal from the same visit; `SearchSpecials`, `SearchPromotions`; the cook prices only shops it has a tool for |
| [CL14-public-word-role.md](CL14-public-word-role.md) | TODO: *CL14 The public word: role* | 2026-10-17 | "personality" becomes "role" where callers see it: the API (`role`, `GET /roles`), `StartSubSession(role)`, `ROLES_ENABLED`, the web apps and benchmarks; write the new word, read both, the input aliases removed after a deploy |
| [CL13-skills-by-role.md](CL13-skills-by-role.md) | TODO: *CL13 Skills by role* (cleanup) | 2026-10-17 | `Skills/` in the seams' pattern: the contract at the top, `Providers/`, `Roles/`; "personality" becomes "role" in the code (`RoleCatalog`, `RoleLevel`, `RoleException`); the dead `PersonalitySkillsProvider` deleted; the public names in CL14 |
| [F33-shop-catalogues.md](F33-shop-catalogues.md) | [F33 Shop catalogues](../features/F33-shop-catalogues.md) | 2026-10-17 | The guarded browser moved into `Tools/Web/Browsing`; `Tools/Shops` with `IShopCatalogue` and `Providers/CheckersCatalogue`, which reads Checkers' catalogue JSON through the page; a `shops` toolset off unless `SHOP_CATALOGUES` names the shop; the first plan with a namespace map |
| [F32-self-hosted-web.md](F32-self-hosted-web.md) | [F32 Self-hosted web](../features/F32-self-hosted-web.md) | 2026-10-17 | SearXNG behind `IWebSearchProvider`, a headless Chrome behind `IPageRenderer` (PuppeteerSharp over CDP, every request held to `SsrfGuard`), both in the Puls stack on their own network; Tavily still selectable; MCP for the model its own feature |
| [F31-benchmarks-portal.md](F31-benchmarks-portal.md) | [F31 Benchmarks in the admin portal](../features/F31-benchmarks-portal.md) | 2026-10-17 | The harness's stdout protocol recorded by the API into MongoDB (`puls-mongo`, data on a host folder; `MongoDB.Driver`); a store behind a contract test; the cook and routing checks rewritten; the screen laid out first, on Puls's theme; built after F30's build steps, before its measured run; closes F27 |
| [CL10-clear-the-decks.md](CL10-clear-the-decks.md) | TODO: *Clear the decks* (cleanup) | 2026-10-17 | `samples/` and `BENCHMARKS.md` go; the admin portal stops being file-based (results will live in SQL Server `puls_benchmarks`, scripts in `benchmarks/`); roles named, not derived; history docs link at tag `pre-cl10`; before F30 step 4 |
| [CL9-modes-per-product.md](CL9-modes-per-product.md) | TODO: *Modes per product* (cleanup) | 2026-10-16 | A level-0 session is a mode, one file of voice and routing in a folder per product (`agent/puls/voice-af.md`, `agent/louis-agent/voice.md` …; approved during the build); Puls's language menu picks the mode; `AGENT_FUNCTION` goes; before F30 step 4; ADR 0019 |
| [CL8-skills-by-level.md](CL8-skills-by-level.md) | TODO: *Skills by level* (cleanup) | 2026-10-16 | `Skills/` in folders by level (`agent/`, `specialists/`, `minions/`) beside `toolsets/`; the loader refuses a role at the wrong level; built before F30 step 4; ADR 0017 |
| [F30-local-web-peer.md](F30-local-web-peer.md) | [F30 Local web peer](../features/F30-local-web-peer.md) | 2026-10-15 | The model per session (an LLM profile in the engine's key), a price role on Ollama, a quote check in code, measured against Haiku alone |
| [F29-validated-specialists.md](F29-validated-specialists.md) | [F29 Validated specialists](../features/F29-validated-specialists.md) | 2026-10-15 | A setting the API enforces (a role off the list is refused as unknown), prompts that match it, the chat opening with `agents` and `web`, the checks expecting no delegation but the cook's |
| [F28-team-talk.md](F28-team-talk.md) | [F28 Team talk](../features/F28-team-talk.md) | 2026-10-15 | The note in the tools' `message` (no tool change), five labelled lines in the task's language; `read_mood` per turn with a marker line, as the user's place travels (F22); the shared card as a thread; both checks score `briefed` |
| [F27-admin-portal.md](F27-admin-portal.md) | [F27 Admin portal](../features/F27-admin-portal.md) | 2026-10-15 | Our own sidebar-and-cards layout on Puls's variables (AdminLTE, Tabler and MudBlazor as reference only); the checks' Python scripts run by the API as child processes, progress over SSE; `ADMIN_API_KEY` gate |
| [F26-specialist-follow-up.md](F26-specialist-follow-up.md) | [F26 Specialist follow-up](../features/F26-specialist-follow-up.md) | 2026-10-15 | A `SubSessionRegistry` owned by the chat's session; `ContinueSubSession(handle, message)`; caps in code; measured by F25's follow-up question |
| [F25-local-cook.md](F25-local-cook.md) | [F25 Local cook](../features/F25-local-cook.md) | 2026-10-15 | Two routing rows; a cook check beside the delegation check that reads the specialist's calls from the stream; a paid run on Louis's go |
| [F22-role-tools.md](F22-role-tools.md) | [F22 Role tools](../features/F22-role-tools.md) | 2026-10-13 | Reuse before building: PdfPig, OpenXml, ExcelDataReader, git CLI, F14's rates |
| [F21-personalities.md](F21-personalities.md) | [F21 Personalities](../features/F21-personalities.md) | 2026-10-13 | Built before F20 continues; `puls.md` and `puls-specialist.md` |
| [F20-puls-look.md](F20-puls-look.md) | [F20 Puls look](../features/F20-puls-look.md) | 2026-10-13 | Sensai's look and Preline's placeholders, in Puls's colours, dark; ten steps |
| [F01-usage-ledger.md](F01-usage-ledger.md) | [F1 Usage ledger](../features/F01-usage-ledger.md) | 2026-10-10 | Recreated on 2026-10-11 from the plans approved in the session |
| [F02-prices-and-cost.md](F02-prices-and-cost.md) | [F2 Prices and cost](../features/F02-prices-and-cost.md) | 2026-10-10 | Recreated on 2026-10-11; includes the `PRICES_URL` addition approved during the build |
| [F03-usage-display.md](F03-usage-display.md) | [F3 Show usage](../features/F03-usage-display.md) | 2026-10-11 | |
| [F14-display-currency.md](F14-display-currency.md) | [F14 Display currency](../features/F14-display-currency.md) | 2026-10-11 | Built before M2 by choice |
| [F05-toolset-profiles.md](F05-toolset-profiles.md) | [F5 Toolset profiles](../features/F05-toolset-profiles.md) | 2026-10-13 | No toolsets at start-up; an engine per toolset list; sub-sessions on a peer (`StartSubSession`) |
| [F05-usage-rollup.md](F05-usage-rollup.md) | TODO: *F5 follow-up: roll sub-session usage up* | 2026-10-13 | One roll-up record per sub-session reply in the parent scope, and (approved during the build) a row in the parent's ledger; three steps, built 2026-10-14 |
| [F24-landing-motion.md](F24-landing-motion.md) | [F24 Landing motion](../features/F24-landing-motion.md) | 2026-10-15 | The reviewed artifact into `LandingPage.razor`, `landing.js`, `Water.razor` and `LandingDemo.razor`; the app's empty chat, Werk panel, real loading percentage and an *Animasies* switch; eight steps |
| [CL7-docs-tidy.md](CL7-docs-tidy.md) | TODO: *Docs tidy* (cleanup) | 2026-10-14 | The session picture to `specs/images/` (SVG only); How a turn works into the runtime view; Runbooks beside the orchestrator's doc |
| [CL6-arc42.md](CL6-arc42.md) | TODO: *Architecture docs in arc42* (cleanup) | 2026-10-14 | arc42's twelve sections replace ARCHITECTURE.md; 13 ADRs (Nygard) written from where each decision is recorded; C4 diagrams in mermaid flowcharts |
| [CL5-guides-by-folder.md](CL5-guides-by-folder.md) | TODO: *Guides by folder* (cleanup) | 2026-10-14 | The guide's folder-only how-tos move to `CLAUDE.md` files beside the code, loaded only there; none in `Skills/` (embedded, read as personalities) |
| [CL4-doc-types.md](CL4-doc-types.md) | TODO: *One type per doc* (cleanup) | 2026-10-14 | Diátaxis: each doc one type; benchmarks to `BENCHMARKS.md`, Done to `CHANGELOG.md`; a link check per step |
| [CL3-messages-and-logging.md](CL3-messages-and-logging.md) | TODO: *Messages and logging* (cleanup) | 2026-10-12 | Serilog behind `ILogger`, not NLog; tool texts pinned by a golden file; the comms log unchanged |
| [CL2-split-agent-engine.md](CL2-split-agent-engine.md) | TODO: *Split `AgentEngine`* (cleanup) | 2026-10-11 | Classes with the engine as façade; console UI to the CLI; pure checks in `Guardrails/` |
| [CL1-project-layout.md](CL1-project-layout.md) | TODO: *Project layout* (cleanup) | 2026-10-11 | PascalCase in every project, one type per file; `AgentEngine` moved whole, its split next |
| [F04-prompt-caching.md](F04-prompt-caching.md) | [F4 Prompt caching](../features/F04-prompt-caching.md) | 2026-10-11 | Conversation marker by automatic caching (option B); orchestrator and summariser left to their own features |

## Conventions

- **One file per plan**, named like what it plans: `Fnn-name.md` for a feature, `FXnn-name.md` for a fix, `CLn-name.md` for a
  cleanup (a TODO line with no feature or fix doc). A fix gets a plan only when it takes more than one step; a one-step
  fix has its fix doc alone (FX18, FX19 and FX20).
- **The roadmap** is the one other kind: [platform.md](platform.md) says what we build and in which order, and each plan
  says how. It is the one name allowed outside `Fnn`/`FXnn`/`CLn`, and after its approval it is never edited except to
  link each phase's plan.
- **A phase's plan is copied from the roadmap**, as approved, into its own file on the phase's own branch before its
  first step (`CL15-engine-identities-packs.md`, `F18-evals.md`, `F40-sales-playground.md`,
  `F39-orchestration.md`), with its diagrams, namespace map and folder trees; *Approved during the build* goes only in
  that copy. A phase the roadmap names without a section of its own (5, 7, 8 and 9) gets a new plan, approved by Louis
  before its first step.
- **Sections:** *Context* (why now, what the doc says), *Design* (what changes from the doc, and why), *Steps* (one
  commit each, with its test, written and seen failing first; the review stop after each), *Verification* (tests, free manual checks, and any paid run,
  which needs approval first), and the *Concepts* each step teaches, with their skill in the [goal](../GOAL.md).
- **Four diagrams, always** (Louis, 2026-10-16; the third 2026-10-17, F32; the fourth 2026-10-18), in the *Design*, as mermaid: an
  **interface design** (a `classDiagram` with the C# signatures beside it: the interfaces and records the change uses or
  adds, every implementation behind them, old and new, marked with the step that builds it, and who creates, reads and
  calls whom), an **architecture diagram** (a `flowchart`: the containers, hosts and services, and which step builds or
  changes each part) and a **communication diagram** (a `sequenceDiagram`: who calls whom, in order, for the feature's
  main case, with what each call carries), and an **object diagram** (a `flowchart` of the instances the main case
  holds in memory, and which holds which). A plan approved without them gets them under *Approved during the build*.
- **Folder trees** (Louis, 2026-10-18), in the *Design*, whenever a plan adds or splits projects or folders: the tree
  today and the tree after (after each step when the steps move things).
- **Reviewed as one page** (Louis, 2026-10-18): a plan is reviewed as one page with its diagrams drawn in place beside
  its full text, not as markdown with the diagrams as code.
- **A namespace map, always** (Louis, 2026-10-17, F33: "include in the plan the namespaces, this must be a rule"), in the
  *Design*: a table of every type the plan adds, moves or changes, with its folder, its namespace and the step that
  does it (tests included), so the layout is decided before the code and `ArchitectureTests` holds no surprise.
- **A lasting decision gets an ADR:** when a plan decides something later work must follow, its record goes in
  [docs/adr/](../adr/README.md) in the same PR; the plan keeps the steps, the ADR the why.
- **Saved and committed before step 1**, on the feature's branch, so the plan is in the same PR as the work.
- **Changes during the build:** a change that is asked for and approved during the build (F2's `PRICES_URL`) is added to
  the plan under *Approved during the build*, with the date; everything else the build learns goes in the feature or fix
  doc.

---
[Docs index](../README.md) · Related: [Features](../features/README.md) · [Fixes](../fixes/README.md)