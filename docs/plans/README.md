# Plans: how each feature or fix is going to be built

A feature doc ([features/](../features/README.md)) or a fix doc ([fixes/](../fixes/README.md)) says *what* and *why*.
A plan says **how we'll build it now**: written and approved before the first step, with what it changes from the doc
(the code has usually moved on since the doc was written), the steps, and how the result is checked. Plans are kept as
they were approved, so the reasoning at the time stays readable; what the build then changed is recorded in the feature
or fix doc (*What the build changed*), not here.

## Plans

| Plan | For | Approved | Notes |
|---|---|---|---|
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
  cleanup (a TODO line with no feature or fix doc).
- **Sections:** *Context* (why now, what the doc says), *Design* (what changes from the doc, and why), *Steps* (one
  commit each, with its test, written and seen failing first; the review stop after each), *Verification* (tests, free manual checks, and any paid run,
  which needs approval first), and the *Concepts* each step teaches, with their skill in the [goal](../GOAL.md).
- **A lasting decision gets an ADR:** when a plan decides something later work must follow, its record goes in
  [docs/adr/](../adr/README.md) in the same PR; the plan keeps the steps, the ADR the why.
- **Saved and committed before step 1**, on the feature's branch, so the plan is in the same PR as the work.
- **Changes during the build:** a change that is asked for and approved during the build (F2's `PRICES_URL`) is added to
  the plan under *Approved during the build*, with the date; everything else the build learns goes in the feature or fix
  doc.

---
[Docs index](../README.md) · Related: [Features](../features/README.md) · [Fixes](../fixes/README.md)