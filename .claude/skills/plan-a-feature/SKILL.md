---
name: plan-a-feature
description: Writes, changes or finishes a plan. Use before the first step of a feature, a cleanup or a fix of more than one step, when Louis asks for a plan, when a plan grows past ten steps, or when he approves a change during the build. Holds the research first, where the plan goes, its sections and pictures, and its records.
---

# Plan a feature

Nothing is built before its plan is approved. Five stops come first.

1. **Research before you write it.** A plan written from memory is a guess. The part below says what to look
   up. Nothing in a plan is assumed.
2. **No step before Louis approves the plan.** Show it to him as one page, with its pictures drawn in place, and
   wait for his word.
3. **An approved plan is not rewritten.** A change he approves during the build is added under *Approved during the
   build*, with the date and his words. Everything else the build learns goes in the commit.
4. **At most about ten steps.** Bigger work is a parent with dotted children (`F49.1`, `F49.2`). Each child has its
   own plan, branch, pull request to `main`, done-when and write-up. A parent's plan is written when the parent
   starts, and a child's when the child starts.
5. **Read the code before you draw it.** The interface picture and the namespace map are written from the real
   signatures, never from memory.

## Research before you write it

Do this first, and write the plan from what you found.

- **What is there already.** Read the code the work will touch, and how the repo solved the nearest problem. The
  repo may hold the thing the plan was about to build, and then the plan uses it.
- **Which rules reach it.** A change to a shared file is tested by everything that reads it. Find the tests
  and the rules that will run against the work, so no step is stopped by one it didn't expect.
- **The patterns that fit**, read on the web that day: Refactoring.Guru, the Gang of Four, Fowler, Microsoft's
  .NET guidance. Where there is a choice among them, it goes to Louis with `/put-a-finding` before the plan is
  written.
- **How a tool really behaves**, when the plan rests on one: a package, a platform, an API, a price, a limit.
  Read its own documentation on the web that day, and link the page. Then plan a first step that proves it, and
  say the plan stops there if it doesn't hold.
- **How others solved it**, for anything new to this repo. Look for two or three who built the same thing, and
  say what each did and what it cost them.
- **What a mockup or a brief leaves open.** Each gap is a finding for Louis, not a detail for the build.

The plan's *Context* says what was looked up. What could not be confirmed is said plainly, with the step that
will confirm it.

## Where the plan goes

One file in `src/docs/plans/`, named for what it plans: `Fnn-name.md` for a feature, `FXnn-name.md` for a fix,
`CLn-name.md` for a cleanup. A one-step fix has no plan.

Its id is the next free number of its kind. Take it from `TODO.md` and `src/docs/work-log.md` on `origin/main`,
after `git fetch origin main`, since another branch may have taken one since this clone was made. A plan drafted
in the project's files takes its id the same way, so two plans never share one.

Its branch is off `main` and carries the plan's id. The names are in `/ship-it`.

The plan is saved and committed on that branch before step 1, so it is in the same pull request as the work.

## What it holds

In this order.

1. **Context.** Why now, and what Louis asked for, in his own words.
2. **Design.** What changes and why. It always has these:
   - **An overview and four pictures**, each a JSON in `diagrams/` beside the plan, drawn by our tool. Use
     `/draw-a-diagram`. No doc holds Mermaid. A picture too big to read is split in two.
     - *Overview*, a `cards` picture under the plan's opening line, as every doc opens
       (`.claude/rules/docs.md`): what the plan builds and its main parts.
     - *Interface design*, a `classes` picture: the interfaces and records the change uses or adds, every
       implementation behind them, old and new, each new one marked with the step that builds it, and who creates,
       reads and calls whom.
     - *Architecture*, a `flow` picture: the containers, hosts and services, and which step builds or changes each.
     - *Communication*, a `sequence` picture: who calls whom, in order, for the main case, with what each call
       carries.
     - *Objects*, a `flow` picture: the instances the main case holds in memory, and which holds which.
   - **A namespace map**: a table of every type the plan adds, moves or changes, with its folder, its namespace
     and the step that does it. Tests are in it too.
   - **Folder trees**, when the plan adds or splits projects or folders: the tree today and the tree after.
   - **What is reused.** For each piece, the existing component or the package it uses. A new package is put to
     Louis first, with `/put-a-finding`.
3. **Steps.** One commit each, with its test, written first and seen failing. A review stop after each.
4. **Verification.** The tests, the free checks by hand, any paid run, which needs his approval first, and the
   **Done when**: what must be true before `/finish-a-feature` closes the work.
5. **Concepts.** What each step teaches, and its skill in `src/docs/GOAL.md`.
6. **Approved during the build.** Empty at first.

A design choice the plan would have to make is put to Louis before the plan is written, with `/put-a-finding`.

## The records that go with it

- Its row in the index of `src/docs/plans/README.md`.
- Its row in `TODO.md`. A parent's row lists its children and their done-whens.
- Its row in the work log's open table, `src/docs/work-log.md`, and the count above that table.

## When the work is done

The plan is deleted in the pull request that finishes its work. Before that, its lasting parts move: a decision
that later work must follow into an ADR, which needs Louis's approval before it is written, the rest into the
project's design doc or the write-up.

## An example

CL64, our prompts in the skill shape. Its plan opened with its context in Louis's words, drew its pictures
from JSON in `diagrams/`, had a namespace map and numbered steps, and kept a change he made during the build under
*Approved during the build*: on 2026-11-18 he narrowed it to the guide alone, since the cook and Puls move to their
own repos. When the work was done, what lasted went into the guide's design doc and the context-engineering
write-up, and the plan and its pictures were deleted.

## References

- [The plans' README](../../../src/docs/plans/README.md), the sections and the index
- `/draw-a-diagram`, for the pictures, and `/ship-it`, for the branch's name
- The patterns: [Refactoring.Guru](https://refactoring.guru/design-patterns), Fowler's
  [catalog of patterns](https://martinfowler.com/eaaCatalog/), and Microsoft's
  [.NET design guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
