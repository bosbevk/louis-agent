---
paths:
  - "**/*.md"
---

# Docs: the how of every rule

Loaded when a `.md` file is read or edited. The why: [ADR 0013](../../src/docs/adr/0013-documentation-structure.md) (one
type per doc) and the dated decisions below; the index of every doc is [src/docs/README.md](../../src/docs/README.md).

- **One type per doc:** every doc is a tutorial, how-to, reference, explanation or plan, and opens with a line that
  says which and what it answers; new text goes in the doc of its type, never duplicated across two
  ([Writing docs](../../src/docs/README.md#writing-docs-one-type-per-doc)). A how-to lives in `src/docs/guides/`, a why in
  `src/docs/adr/`, a project's design in `src/docs/projects/`, a lesson in `src/docs/learning/`.
- **A doc lives with what it is about** ([ADR 0074](../../src/docs/adr/0074-the-folders-say-what-ships.md)): the framework's in
  `src/docs/`, one demo's in `demos/{demo}/docs/`, the packs' in `packs/docs/`, in the same folders (`adr/`, `guides/`,
  `projects/`). ADR numbers are shared across all of them, and `src/docs/adr/README.md` lists every one.
- **In English** (Louis, 2026-10-24): every developer doc, manual and write-up; the product's own texts (Puls's
  Afrikaans) are content, not docs.
- **In plain words** (Louis, 2026-10-26): the way a friend would explain it, casual but straight to the point, in
  short everyday sentences; lead with what it means, then the detail; say what a technical term means the first time;
  name code (a class, a file) only where the reader needs to find it.
- **What Claude loads stays small** (Louis, 2026-10-29, CL25): every `CLAUDE.md` and every file in `.claude/rules/`
  holds at most 200 lines; `ClaudeMdSizeTests` holds it. The root `CLAUDE.md` loads on every session; a subfolder's
  and a rules file with `paths:` load when a matching file is touched. What doesn't fit goes to an ADR (the why), a
  rules file (the how) or a guide (the how-to), and the guide keeps one line with the link.
- **Plans** (`src/docs/plans/`, [its README](../../src/docs/plans/README.md) has the sections and the four diagrams): approved
  before step 1 and committed on the branch; never rewritten, a change Louis approves during the build goes under
  *Approved during the build*; **a plan holds at most about ten steps** (Louis, 2026-10-28): bigger work is a parent
  with dotted children (`F49.1`), each its own plan, branch, PR, done-when and write-up, so `main` moves every few
  days; **a done plan is deleted** in the PR that finishes its work, after its lasting parts are in an ADR, a project
  doc or a write-up (the docs folded on 2026-10-29 are at the tag `pre-cl24`).
- **Feature and fix docs** (`src/docs/features/`, `src/docs/fixes/`): say what and why; a fix doc has *Symptom* (the exact
  error), *Cause* (proved), *Fix*, *Tests*, *Confirmed*, *Lesson*; when the work is done the doc's lasting parts move
  to the project's design doc and its write-up, the fixes table row keeps the symptom and cause, and the doc is
  deleted (*Finishing a feature*, *Finishing a fix* in their READMEs).
- **Write-ups** (`src/docs/learning/`): when a skill or feature finishes, Claude writes its learning write-up for Louis to
  read afterwards (Louis, 2026-10-19), naming the GOAL.md skill it teaches; a fix's lesson goes in
  `fixes-lessons.md`.
- **ADRs** (`src/docs/adr/`, [its README](../../src/docs/adr/README.md) has the template): a lasting decision gets one in the
  same PR, numbered next free, with *Context*, *Decision* as a rule, *Consequences*, *Links*; the quotes and dates
  that explain a rule live there, not in `src/docs/CLAUDE.md`.
- **Sources of truth:** `TODO.md` (what's next), `src/docs/work-log.md` (what was finished: one row per feature,
  fix or cleanup, when and what, the counts, and the open list; it replaced the changelog on 2026-11-07), the
  design docs, the guides; update them rather than duplicating them. A link to a doc that CL24 deleted points at
  the tag `pre-cl24`.
- **Links:** relative within the repo; every doc ends with its index line (`[Docs index](…) · Related: …`); a
  renamed heading takes its anchors with it (`grep -rn "CLAUDE.md#"`).
