---
paths:
  - "**/*.md"
---

# Docs: the how of every rule

Loaded when a `.md` file is read or edited. The index of every doc is [src/docs/README.md](../../src/docs/README.md).

The rules come in this order: what every doc is, where a doc goes, the docs that have a lifecycle, ADRs, the
records, links, and how big a file that Claude loads may be.

## What every doc is

- **True to the code, nothing invented** (Louis, 2026-11-13: "always make sure docs are 100% correct and nothing
  invented which is not there", "if something the ai is typing makes him unsure he must check the code first").
  Every statement in a doc is something the code, a test or a recorded decision shows. How:
  - **Unsure of a sentence: open the code and read it before writing it.** Memory and a good guess are not a
    source. That holds most for a detail: a number, a limit, an order, a name, what happens on a failure.
  - **The why of a design is looked up, never guessed.** A doc may say "so that…" only when that reason is
    written in the code's own summary, in an ADR, or in Louis's words. If it is in none of them, leave the
    reason out. A reason that only sounds right is an invention.
  - **What you could not confirm is left out, or said plainly as not known.** That is anything you did not
    find in the code, a test or a recorded decision: something not built yet, or something you could not run
    or see for yourself. Never write a confident sentence over the gap.
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
  - **A project doc keeps its shared sections**, the ones *Each doc covers* lists in the projects' index:
    *Purpose*, *How it works*, *Diagrams*, *Structure* and the rest, by those names, so every project doc is
    found the same way. The story is told inside *How it works*, in numbered parts. *Structure* is the lookup.
  - **A robot index fails**: a bold label followed by type names in brackets with ids between them. A doc that
    reads that way is rewritten when it is next touched, the explaining part first.
- **In plain words** (Louis, 2026-10-26): the way a friend would explain it, casual but straight to the point, in
  short everyday sentences. Lead with what it means, then the detail. Say what a technical term means the first
  time. Name code, a class or a file, only where the reader needs to find it.
- **In English** (Louis, 2026-10-24): every developer doc, manual and write-up. A product's own texts, such as
  Puls's Afrikaans, are content, not docs.
- **No build history in an official doc** (Louis, 2026-11-13, of a design doc's *What the build decided (CL60.4,
  2026-11-12, unattended)*: "this must be taken out of official docs never ever be there"). A design doc, a guide
  or an architecture page says what the thing is, how it works and why. It never says how the work went: who
  picked what, "Claude's picks", "unattended", what a build found or settled on a date. That is in git, in the
  commit and its pull request. A decision that later work must follow is an ADR, stated as the rule and its
  reason, not as the story of the build.

## Where a doc goes

- **One type per doc.** Every doc is a tutorial, a how-to, a reference, an explanation or a plan. It opens with a
  line that says which, and what it answers. New text goes in the doc of its type, and is never duplicated across
  two ([Writing docs](../../src/docs/README.md#writing-docs-one-type-per-doc)). A how-to lives in
  `src/docs/guides/`, a why in `src/docs/adr/`, a project's design in `src/docs/projects/`, and a lesson in
  `src/docs/learning/`.
- **A doc lives with what it is about.** The framework's docs are in `src/docs/`, one demo's in
  `demos/{demo}/docs/`, and the packs' in `packs/docs/`, in the same folders (`adr/`, `guides/`, `projects/`). ADR
  numbers are shared across all of them, and `src/docs/adr/README.md` lists every one.
- **A project's docs move with its code** (Louis, 2026-11-13): a step that changes a project checks that project's
  design doc and how-to (both show under `docs/` in the project in Rider) and changes what the step made untrue.
  A picture that no longer shows the code is changed in its JSON and redrawn with the diagrams tool, never by hand
  ([Draw a diagram](../../src/docs/guides/draw-a-diagram.md)).

*Decided in:* [ADR 0013](../../src/docs/adr/0013-documentation-structure.md),
[ADR 0074](../../src/docs/adr/0074-the-folders-say-what-ships.md).

## The docs that have a lifecycle

- **A plan** (`src/docs/plans/`) is approved before step 1 and committed on its branch. It is never rewritten: a
  change Louis approves during the build goes under *Approved during the build*. It holds at most about ten
  steps (Louis, 2026-10-28). Bigger work is a parent with dotted children (`F49.1`), each with its own plan,
  branch, PR, done-when and write-up, so `main` moves every few days. A done plan is deleted in the PR that
  finishes its work, after its lasting parts are in an ADR, a project doc or a write-up. Its sections and its four
  diagrams are in `/plan-a-feature` and in [the plans' README](../../src/docs/plans/README.md).
- **A feature doc and a fix doc** (`src/docs/features/`, `src/docs/fixes/`) say what and why. A fix doc has
  *Symptom* (the exact error), *Cause* (proved), *Fix*, *Tests*, *Confirmed* and *Lesson*. When the work is done,
  the doc's lasting parts move to the project's design doc and its write-up, the fixes table's row keeps the
  symptom and the cause, and the doc is deleted. The lists are *Finishing a feature* in `/finish-a-feature` and
  *Finishing a fix* in `/fix-a-bug`.
- **A write-up** (`src/docs/learning/`) closes every piece of work (Louis, 2026-11-13). A feature and a cleanup
  end with their section in the write-up of the GOAL.md skill they teach. A fix ends with its lesson under its
  own heading in `fixes-lessons.md`. Claude writes them, for Louis to read afterwards (Louis, 2026-10-19).
  `WriteUpsTests` holds it. The ones that were owed on that day are listed in the test, so the list can only
  shrink.

The docs folded on 2026-10-29 are at the tag `pre-cl24`.

## ADRs

- **No new ADR without Louis's approval** (Louis, 2026-11-13: "the agent may not add adrs without my approval").
  When work turns up a decision that later work must follow, say so, propose the ADR in a few lines (the rule
  and its reason) and wait for his yes before writing the file. The same holds for a new rule in an existing ADR.
- **An ADR is one decision** (Louis, 2026-11-13: "0122 is not an ADR"): the context that forced a choice, the
  choice, and what it costs. It says who it binds: the framework, or one site or pack. A list of what a feature
  built, its routes, its files and its styles, is the project's design doc. A rule found on the way amends the
  ADR that already owns that part, with his approval as for any new rule.
- **An ADR's shape** (`src/docs/adr/`): it is numbered next free, and has *Context*, *Decision* as a rule,
  *Consequences* and *Links*. [Its README](../../src/docs/adr/README.md) has the template. A lasting decision gets
  its ADR in the same PR as the work.
- **The quotes and dates that explain a design rule live in its ADR**, not in `src/docs/CLAUDE.md`. A way of
  working with Louis keeps his words and their date where it is written.

## The records

- **A record is a table** (Louis, 2026-11-07, of the work log: "we just need something simple as this"): the
  work log and the TODO are one row per item, with its id and what it is in a few words. Never a prose column,
  and the changelog does not come back. The reasoning goes in the plan or an ADR, written when the work starts.
  A new item takes the next id of its kind (`F`, `FX`, `CL`). It is a fix, never a bug.
- **Sources of truth.** `TODO.md` says what is next. `src/docs/work-log.md` says what was finished: one row per
  feature, fix or cleanup, with when and what, the counts, and the open list. It replaced the changelog on
  2026-11-07. The design docs and the guides are the rest. Update them, rather than duplicating them.

## Links

- **Relative within the repo.**
- **Every doc ends with its index line**: `[Docs index](…) · Related: …`.
- **A renamed heading takes its anchors with it.** `grep -rn "CLAUDE.md#"` finds the links to one.
- **A link to a doc that CL24 deleted points at the tag `pre-cl24`.**

## What Claude loads stays small

Every `CLAUDE.md`, every file in `.claude/rules/` and every skill holds at most 200 lines (Louis, 2026-10-29,
CL25). `ClaudeMdSizeTests` holds it.

What doesn't fit goes to an ADR (the why), a rules file (the how), a skill (a procedure) or a guide (the how-to),
and the file it left keeps one line with the link. When each file is loaded is in
[.claude/README.md](../README.md).
