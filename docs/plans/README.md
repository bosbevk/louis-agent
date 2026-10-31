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
| [platform.md](platform.md) | The living roadmap: Phases 0 to 3 done; next 3b evals as content, 3c the cook (F35, F48), 3d localisation (F46), 3e Puls's specialists (F50), then F40 and the control layer, Phase 10 the engine as a package | 2026-10-19, kept current | One engine, its identities and packs, evals in C#, the sales playground, then the control layer phase by phase; only current and future work, a built phase becomes a done-line |
| [F49-evals-as-content.md](F49-evals-as-content.md) | TODO: *F49 Evals as content* (Phase 3b) | 2026-10-30 | Parent: no steps of its own, five children F49.1 to F49.5 |
| [F49.3-checks-in-packs.md](F49.3-checks-in-packs.md) | TODO: *F49 Evals as content*, child F49.3 | 2026-10-31 | Five steps: the shops pack ships the shop rules, the composition gathers them, Puls keeps no shop check |
| [F48-cook-and-teacher.md](F48-cook-and-teacher.md) | TODO: *F48 Recipe maths and a teaching cook* (Puls, after F35's steps 1 to 4) | 2026-10-26 | Decisions settled, steps proposed: no Cooklang, our own C# ingredient-line reader, scaler and shopping list on F35's `Recipe`; a `WriteRecipe` tool prints the recipe and list; estimates from Stats SA's average prices plus our own rows (`null`, never 0); cues and whys in every answer; a pantry (global catalogue, stock per browser id, Mongo in the recipes pack). The four diagrams, the namespace map and folder trees are added when it is copied onto its branch |
| [platform-summary.md](platform-summary.md) | The platform plan, for someone outside the project | 2026-10-19 | Two pages to give an investor or partner: what Puls is, what's built and measured, the ten phases, how it makes money, competition and risks; its ask is for Louis to fill in |
| [F28-team-talk.md](F28-team-talk.md) | [F28 Team talk](../features/F28-team-talk.md) | 2026-10-15 | The note in the tools' `message` (no tool change), five labelled lines in the task's language; `read_mood` per turn with a marker line, as the user's place travels (F22); the shared card as a thread; both checks score `briefed` |

## Conventions

- **One file per plan**, named like what it plans: `Fnn-name.md` for a feature, `FXnn-name.md` for a fix, `CLn-name.md` for a
  cleanup (a TODO line with no feature or fix doc). A fix gets a plan only when it takes more than one step; a one-step
  fix has its fix doc alone (FX18, FX19 and FX20).
- **The roadmap** is the one other kind: [platform.md](platform.md) says what we build and in which order, and each plan
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