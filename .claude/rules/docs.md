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
- **True to the code, nothing invented** (Louis, 2026-11-13: "always make sure docs are 100% correct and nothing
  invented which is not there", "if something the ai is typing makes him unsure he must check the code first").
  Every statement in a doc is something the code, a test or a recorded decision shows. How:
  - **Unsure of a sentence: open the code and read it before writing it.** Memory and a good guess are not a
    source. That holds most for a detail: a number, a limit, an order, a name, what happens on a failure.
  - **A reason is copied, never worked out.** The "so that" of a design comes from the code's own summary, an
    ADR or Louis's words. A reason made up because the sentence reads better is an invention.
  - **What can't be checked is left out**, or said plainly as not known. Never smoothed over.
  - **A rewrite keeps every fact** of the text it replaces, and checks each one it restates. Simpler words must
    not change what is said: "the chat portal" is not "the handoff from an admin screen to the chat".
  - **Say what is there today.** What is planned or being built belongs in the plan, not the design doc.
- **Explanatory and detailed** (Louis, 2026-11-13: "it must be explanatory and detailed"). Teaching is not
  thinning. Each part says what happens, how, and why it is built that way, with the particulars a reader needs
  to act: the numbers, the limits, the order things happen in, what a failure does. Shorter sentences, not fewer
  facts.
- **A doc teaches, in the order a reader needs it** (Louis, 2026-11-13: "our documents must read in order for
  people to understand, like you are a teacher teaching people not just a robot index", "it should not be
  difficult to follow"). Someone new reads it once, top to bottom, and can then say what the thing does and how a
  request moves through it. How:
  - **Open with what it is for** and who uses it, in a few plain sentences, before any name of code. Then say how
    the doc is laid out, so the reader knows where they are going.
  - **Walk the main path as a story**, one step at a time, each step building on the one before: what happens,
    then why it is built that way. A picture sits where its part of the story is told.
  - **One idea to a paragraph**, under a heading that says the idea. An example comes before the rule it shows.
  - **A name of code comes after its idea**, only where the reader must find it, never several in a row. The full
    list of types and folders is a table at the end, marked as the place to look things up.
  - **Ids and ADR links close a section** (*Decided in:* F127.6, ADR 0120). They are not wedged into a sentence.
  - **A robot index fails**: a bold label followed by type names in brackets with ids between them. A doc that
    reads that way is rewritten when it is next touched, the explaining part first.
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
- **Write-ups** (`src/docs/learning/`): every feature and cleanup ends with its section in the write-up of the GOAL.md
  skill it teaches, and every fix with its lesson under its own heading in `fixes-lessons.md` (Louis, 2026-11-13);
  Claude writes them for Louis to read afterwards (Louis, 2026-10-19). `WriteUpsTests` holds it, with the ones
  owed on that day listed so the list can only shrink.
- **A project's docs move with its code** (Louis, 2026-11-13): a step that changes a project checks that project's
  design doc and how-to (both show under `docs/` in the project in Rider) and changes what the step made untrue.
  A picture that no longer shows the code is changed in its JSON and redrawn with the diagrams tool, never by hand
  ([Draw a diagram](../../src/docs/guides/draw-a-diagram.md)).
- **ADRs** (`src/docs/adr/`, [its README](../../src/docs/adr/README.md) has the template): a lasting decision gets one in the
  same PR, numbered next free, with *Context*, *Decision* as a rule, *Consequences*, *Links*; the quotes and dates
  that explain a rule live there, not in `src/docs/CLAUDE.md`.
- **Sources of truth:** `TODO.md` (what's next), `src/docs/work-log.md` (what was finished: one row per feature,
  fix or cleanup, when and what, the counts, and the open list; it replaced the changelog on 2026-11-07), the
  design docs, the guides; update them rather than duplicating them. A link to a doc that CL24 deleted points at
  the tag `pre-cl24`.
- **Links:** relative within the repo; every doc ends with its index line (`[Docs index](…) · Related: …`); a
  renamed heading takes its anchors with it (`grep -rn "CLAUDE.md#"`).
