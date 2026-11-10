# Plans: how each feature or fix is going to be built

A feature doc ([features/](../features/README.md)) or a fix doc ([fixes/](../fixes/README.md)) says *what* and *why*.
A plan says **how we'll build it now**: written and approved before the first step, with what it changes from the doc
(the code has usually moved on since the doc was written), the steps, and how the result is checked. A plan stays as
it was approved while its work is built; what the build then changed is recorded in the feature or fix doc (*What the
build changed*), not here. When the work is done the plan is deleted (CL24): this folder holds only what is being
built or comes next, and every plan as it was is at the tag
[`pre-cl24`](https://github.com/bosbevk/louis-agent/tree/pre-cl24/docs/plans) or in git.

## Plans

| Plan | For | Approved | Notes |
|---|---|---|---|
| [F127.1-demo-showroom.md](F127.1-demo-showroom.md) | *F127.1 The demo showroom* (F127's first child) | 2026-11-10 | Louis's picks: the rail and the mocked website combined, purple as the fifth accent, a slim bar of its own, a mock of a header, a hero line and the agent, colourful and fancy; [mockups](F127.1-mockups/showroom.html); unattended |
| [F40-sales-playground.md](F40-sales-playground.md) | Draft: *F40 The sales playground* (Phase 4) | not yet | Moved whole out of the old platform plan on 2026-11-07 (CL56); rewritten into the plan Louis approves when it starts |
| [F07-budgets.md](F07-budgets.md) | Draft: *F7 Cost you can see and stop* (Phase 5) | not yet | The same |
| [F39-orchestration.md](F39-orchestration.md) | Draft: *F39 Orchestration* (Phase 6) | not yet | The same |
| [phase-7-checked-and-guarded.md](phase-7-checked-and-guarded.md) | Draft: *FX4, F41, F42* (Phase 7) | not yet | The same; one plan per id when each starts |
| [F36-mcp-packs.md](F36-mcp-packs.md) | Draft: *F36 MCP servers as packs* (Phase 8) | not yet | The same |
| [phase-9-what-a-sale-needs.md](phase-9-what-a-sale-needs.md) | Draft: *F43, F8, F15, F44, F51* (Phase 9) | not yet | The same; F61 took the accounts and the hosting |
| [puls-track.md](puls-track.md) | Draft: *Puls's track, between the phases* | not yet | The same; the items are the TODO's *Puls* rows |
| [phase-10-engine-package.md](phase-10-engine-package.md) | Draft: *F74 Packaging the engine* (the framework's track) | not yet | The same; since 2026-11-07 Stage A belongs to the framework's track, Stage B still waits for the engine features |
| [F106-framework-track.md](F106-framework-track.md) | Draft: *F106 The framework's track, before the agents* | not yet | Louis, 2026-11-07: the framework first, then the configured agents built on it; positioning, five proposed children, four open choices asked one at a time |
| [F106.3-triage-and-fix-a-service.md](F106.3-triage-and-fix-a-service.md) | *F106.3 Triage and fix a service, and the demo a visitor watches* (the framework's track, third child; a parent with five children) | 2026-11-08 | Drafted 2026-11-08 from Louis's shape of the workflow (2026-11-07) and of the site's *Try it* (2026-11-08): one identity with three roles and the coder as a peer, a department as configuration, the triage pack, the workflow in code over F39 Part A and F43, the `/try` page with a popup per step and the guide reading the run; the one choice that spends money (a live run per visitor within a cap) put to him with three options |
| [F106.3.1-the-service-and-the-pack.md](F106.3.1-the-service-and-the-pack.md) | *F106.3.1 The service and the triage pack* (F106.3's first child) | 2026-11-08 | The old order-service restored from `pre-cl10` as the agent's workspace (a sample skips the size rules), the triage pack: the feed moved from the orchestrator, a run's recorder, the decision tools with records checked in code, `RaiseToPerson`, `ReadTriageRun`, departments as YAML and the boundary test; eight steps |
| [F106.1-agent-builder.md](F106.1-agent-builder.md) | *F106.1 A builder for the common path* (the framework's track, first child) | for Louis's approval | Louis's picks 2026-11-07: the builder first; a sub-builder on the .NET host (`services.AddLouisAgent(agent => agent.AddAnthropic().AddPack<T>().AddIdentity<T>())`), each provider's `Add…` in its own project (ADR 0092); eight steps, `AgentHost.Build` stays as the escape hatch |
| [F125-an-agentic-site.md](F125-an-agentic-site.md) | *F125 An agentic site* (a parent with eight children) | 2026-11-09 | Louis's picks from the research of 2026-11-09: `docs_search` + `docs_fetch` (BM25 over sections, whole-doc fetch, `doc://` resources), the docs MCP public with a server card and the official registry, `Offer` and `ShowCard`, a rate limit, `ReportGap`, three live demos under a cap, tools over MCP behind `TOOL_TRANSPORT` and discovery an admin approves (F36 folded into F125.8) |
| [F126-evals-redesigned.md](F126-evals-redesigned.md) | *F126 Evals, redesigned* (a parent with four children) | 2026-11-09 | Louis's picks from the research of 2026-11-09 (C1 to C7): one `.eval.json` of conversations with turns, six facts in code, criteria a judge marks per criterion or per conversation, flips instead of labels, re-grade reusing marks, its own eval host, the guide's page fetched at run, six screens drawn in [F126-mockups/](F126-mockups/); today's evals deleted, not ported; replaces CL60, dropped |
| [F48-cook-and-teacher.md](../../../demos/puls/docs/plans/F48-cook-and-teacher.md) | TODO: *F48 Recipe maths and a teaching cook* (Puls, after F35's steps 1 to 4) | 2026-10-26 | Decisions settled, steps proposed: no Cooklang, our own C# ingredient-line reader, scaler and shopping list on F35's `Recipe`; a `WriteRecipe` tool prints the recipe and list; estimates from Stats SA's average prices plus our own rows (`null`, never 0); cues and whys in every answer; a pantry (global catalogue, stock per browser id, Mongo in the recipes pack). The four diagrams, the namespace map and folder trees are added when it is copied onto its branch |
| [F28-team-talk.md](../../../demos/puls/docs/plans/F28-team-talk.md) | [F28 Team talk](../../../demos/puls/docs/features/F28-team-talk.md) | 2026-10-15 | The note in the tools' `message` (no tool change), five labelled lines in the task's language; `read_mood` per turn with a marker line, as the user's place travels (F22); the shared card as a thread; both checks score `briefed` |

## Conventions

- **One file per plan**, named like what it plans: `Fnn-name.md` for a feature, `FXnn-name.md` for a fix, `CLn-name.md` for a
  cleanup (a TODO line with no feature or fix doc). A fix gets a plan only when it takes more than one step; a one-step
  fix has its fix doc alone (FX18, FX19 and FX20).
- **The roadmap** is the one other kind: [ROADMAP.md](../../../ROADMAP.md) says what we build and in which order, and each plan
  says how. It is the one name allowed outside `Fnn`/`FXnn`/`CLn`. It holds only current and future work (Louis,
  2026-10-28): a built phase leaves one done-line, and the detail stays in the ADRs, the write-ups and git
  (Louis, 2026-10-29, replacing the frozen copy of 2026-10-19).
- **Sub-features** (Louis, 2026-10-28): a plan holds at most about ten steps; bigger work is a parent with dotted
  children (`F49.1-name.md`, `F49.2-name.md`), each its own plan, branch, pull request to `main`, done-when and
  write-up. The parent's TODO line lists its children and their done-whens; a parent plan file is written when the
  parent starts, and a child's plan when the child starts (Louis, 2026-10-29).
- **A done plan is deleted** (Louis, 2026-10-28) in the PR that finishes its work, after its lasting parts are in an
  ADR, a project doc or a write-up; git keeps it, and the plans deleted on 2026-10-29 are at the tag `pre-cl24`.
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