# CL63: Claude's instructions, by when they are needed

> **Plan.** How the files Claude Code reads are rewritten: what stays loaded on every turn, what loads with a kind
> of file, and what becomes a skill that loads for a job. Nothing Claude does today is lost.

**Status:** approved 2026-11-13 (Louis: "you go"). **Branch:** `refactor/CL63-instructions-as-skills`.
**Built after F134.1 is merged**, since that branch changes the docs rules this one rewrites.

## Context

Louis, 2026-11-13: "can we look at our claude.md and rewrite it without losing any functionality, and separate
functions by skill files", "if there are skill files, can we also rewrite them", "whole redesign, show me first".
He said go to the redesign as shown.

Today one file, `src/docs/CLAUDE.md`, is loaded on every turn and holds four different things: how to work with
Louis, the procedures of a step, about 27 design rules, and a map of the repo. It is 200 lines, the most it may
be, so every new rule has to squeeze into an old line. And more than twenty standing instructions from Louis are not in
the repo at all. They live only in Claude's private notes, so another session, or another person's Claude, never
sees them.

There are no skill files in the project today. This cleanup creates them.

## Design

### How Claude Code loads each kind of file

This is what the split rests on. It is from Claude Code's own docs. Step 1 proves it with a trial before anything
else is moved.

| Kind of file | When its text is loaded |
|---|---|
| `CLAUDE.md`, and a file it imports | Every session, whole. The docs advise under 200 lines. |
| A file in `.claude/rules/` with no `paths` | Every session, like `CLAUDE.md` |
| A file in `.claude/rules/` with `paths` | When a matching file is read or edited |
| A skill, `.claude/skills/{name}/SKILL.md` | Its one-line description is always there. Its body loads when Claude judges the job matches, or when Louis types `/name`. After a long session is compacted, only its start is kept. |

Two rules for the design follow from that table.

1. **What must never be missed stays in what loads every time.** A skill loads on Claude's judgement, so a hard
   stop, such as "never push without Louis's word", can't live in one.
2. **A skill puts its must-dos first**, since only its start survives a compacted session.

### The three layers

![Three layers: what is always loaded, what loads with a kind of file, and what loads for a job](diagrams/CL63-instructions-as-skills-architecture.svg)

**Layer 1, always loaded: how to work with Louis, and what never changes.**

| File | What it holds | About |
|---|---|---|
| `src/docs/CLAUDE.md` | Who Louis is and the goal. The eleven working rules. One list of hard stops. The loop of a step in a few lines, naming the skill for each part. | 90 lines |
| `.claude/rules/design-rules.md` | The design rules every change keeps, one line each with its ADR, as today | 80 lines |
| `.claude/rules/repo-map.md` | Where things are in the repo | 25 lines |

The hard stops, in one list for the first time: never without Louis's word a push, a merge, a paid run, a new
package, a new ADR, or a design choice the plan doesn't settle.

**Layer 2, loaded with a kind of file: the how.** The four rules files that exist (`csharp.md`, `razor.md`,
`css.md`, `docs.md`) and the three folder files (`src/CLAUDE.md`, `src/tests/CLAUDE.md`, core's `CLAUDE.md`). They
keep their job and are rewritten to the docs rules: true to the code, in a reader's order, detailed.

**Layer 3, skills: one per job.**

| Skill | Loads when | What it holds | Comes from |
|---|---|---|---|
| `plan-a-feature` | A plan is asked for | The sections, the four pictures, the namespace map, ten steps at most, parent and children, approval, the lifecycle | `docs.md`, the plans' README |
| `do-a-step` | A step starts | Test first and see it fail, test what you touch and what a shared file reaches, look at the real thing | `CLAUDE.md`, the tests' guide |
| `finish-a-step` | A step is built | Goldens, the docs and pictures, the formatting order with Rider's tools, what to show Louis, commit, stop | `CLAUDE.md`, the *Finish a step* guide |
| `put-a-finding` | A choice the plan doesn't settle | Look up the pattern, three options with their cost, recommend, wait, record the pick | `CLAUDE.md` rules 10 and 11 |
| `fix-a-bug` | A fix starts | Its own branch, a test that reproduces it, the fixes row, the work log, the lesson | The fixes' README, `docs.md` |
| `check-a-page` | Something a visitor sees changed | Run the site locally, look at 1920×1080 and 390 px, a real print for a PDF, the live check after a deploy | Claude's notes |
| `ship-it` | A commit, a push or a pull request | Branch names, the commit message, the pull request's body, the checks, merge only on Louis's word, one at a time | `CLAUDE.md`, Claude's notes |
| `finish-a-feature` | The last step | The full suite, the write-up, the docs, the plan deleted, the TODO and work-log rows | The plans' README, `docs.md` |
| `draw-a-diagram` | A diagram's JSON is touched | The four steps, pointing at the guide | The *Draw a diagram* guide |

**A skill holds its procedure** (Louis's go to the recommendation). The *Finish a step* guide is followed only by
Claude, so it folds into its skill and the guide goes. A guide a person reads stays where it is, and its skill
points at it.

### The standing instructions that move into the repo

These are in Claude's private notes today, each from Louis's own words. Each goes to the file named, in his words
with their date. Louis strikes any he doesn't want moved.

| Instruction | Goes to |
|---|---|
| Say what you are busy with before a batch of work | `CLAUDE.md` |
| Do the work yourself: no handing build work to another agent unless asked | `CLAUDE.md` |
| Test credit may be spent on the guide's calls, with the cost reported after | `CLAUDE.md`, beside the hard stops |
| Ask before any package, with three options | `put-a-finding` |
| Offer editing on the page itself before a form beside it | `put-a-finding` |
| Show the step's diff before committing | `finish-a-step` |
| A Rider reformat can write back a stale file: the tests are the gate | `finish-a-step` |
| Rebuild the affected container after a change, never during a benchmark run | `finish-a-step` |
| Test what a shared file reaches, not only the folder it is in | `do-a-step` |
| A fix gets its own branch, and small fixes share one branch with a commit each | `fix-a-bug` |
| Check the site locally first, at desktop and phone width | `check-a-page` |
| Check 1920×1080 by paging through every page and section | `check-a-page` |
| Commit as Louis's GitHub identity | `ship-it` |
| Fetch before judging main, and never rebase a pushed branch | `ship-it` |
| One pull request at a time | `ship-it` |
| A plan has its pictures and a namespace map | `plan-a-feature` |
| No "(Louis, date)" inside a prompt or a tool's text | A new rules file for prompt files |
| No semicolons in words a visitor reads | `razor.md` |
| No rainbow gradients, and reuse a Sass variable or mixin before writing one | `css.md` |
| On paper every link is a QR code | `css.md` |
| Records are tables, and an ADR is one decision | `docs.md` |
| One site's feature stays in that site | `design-rules.md` |

What stays in Claude's notes: what is true of a moment and not a rule, such as which branch is open.

### A README, and all of it in the solution

Louis, 2026-11-13: "it should also have its small readme with some overview and graphs", "it should be attached to
the solution".

**A small README**, `.claude/README.md`, says how the instructions are laid out, for a person and for Claude. It
has the three layers and when each loads, a table of every skill with the job it is for, and two pictures: the
three layers, and one step with what is loaded when. By Claude Code's docs it is not one of the files that are
loaded, so it costs no context.

**Every instruction file is in the solution**, in a solution folder of its own, so Rider's tree shows them as it
shows the docs: the README, the rules files and each skill's `SKILL.md`. Today only `src/docs/CLAUDE.md` is there.
A test holds it, as one holds that every ADR is a solution item.

Step 2 has one thing to confirm. The docs' picture test wants every picture to be shown by a doc. If it does not
count a picture shown from this README, the two pictures are also shown on the docs' index.

### How nothing is lost

- **An inventory**, built up over the steps and checked in the last one: every sentence of today's files, and the
  file it lives in afterwards. It is in the pull request, not in the docs.
- **Three tests.** `ClaudeMdSizeTests` also sizes each skill. A new test holds that every skill `CLAUDE.md` names
  exists and has a description. And one holds that every instruction file is in the solution.

### Interface design

![What each file holds: CLAUDE.md names the skills, the rules files load by path, and each skill has a description and a body](diagrams/CL63-instructions-as-skills-interface-design.svg)

### Communication (one step, and what is loaded when)

![One step of a feature: the always-loaded files are there from the start, a rules file arrives with the first file touched, and a skill with each job](diagrams/CL63-instructions-as-skills-communication-one-step-and-what-is-loaded-when.svg)

### Objects (what Claude holds in the middle of a step)

![In the middle of a step: the three always-loaded files, the rules for C#, and the body of one skill](diagrams/CL63-instructions-as-skills-objects-what-claude-holds-in-the-middle-of-a-step.svg)

### File map

No C# type is added but the one test, so this is a map of files.

| File | Change | Step |
|---|---|---|
| `.claude/skills/draw-a-diagram/SKILL.md` | New, the trial | 1 |
| `ClaudeMdSizeTests`, and a new `SkillFilesTests` beside it | Changed, new | 1 |
| `louis-agent-solution.sln`: a solution folder for the instruction files, and `SolutionLayoutTests` | Changed | 1 |
| `.claude/README.md`, with its two pictures | New in step 2, finished in step 9 | 2, 9 |
| `src/docs/CLAUDE.md` | Rewritten, about 90 lines | 2 |
| `.claude/rules/design-rules.md`, `.claude/rules/repo-map.md` | New, from `CLAUDE.md` | 2 |
| `.claude/skills/plan-a-feature/`, `put-a-finding/` | New | 3 |
| `.claude/skills/do-a-step/`, `finish-a-step/`; `src/docs/guides/finish-a-step.md` | New; the guide deleted | 4 |
| `.claude/skills/fix-a-bug/`, `ship-it/`, `finish-a-feature/` | New | 5 |
| `.claude/skills/check-a-page/` | New | 6 |
| `.claude/rules/csharp.md`, `razor.md`, `css.md`, `docs.md`, and a new `prompts.md` | Rewritten, new | 7 |
| `src/CLAUDE.md`, `src/tests/CLAUDE.md`, `src/engine/louis-agent.core/CLAUDE.md` | Rewritten | 8 |

## Steps

Each step is one commit. A step that only moves or rewrites instructions has no test first. Stop after each for
Louis's review.

1. **A trial skill, and the tests.** `draw-a-diagram`, with `paths` for a diagram's JSON. Louis starts a new
   session, touches such a file, and we see whether its body loaded and whether `/draw-a-diagram` works. The size
   test takes in skills, and `SkillFilesTests` is written first and seen failing. The solution gets its folder for
   the instruction files, with the test that every one is in it, seen failing first too. If skills don't load as
   the docs say, the plan stops here and is put to Louis again.
2. **The always-loaded layer.** `CLAUDE.md` rewritten, with the hard stops in one list, and the design rules and
   the map moved to their two rules files. The README starts, with its two pictures. The inventory starts.
3. **Planning and findings.** `plan-a-feature` and `put-a-finding`.
4. **Doing and finishing a step.** `do-a-step` and `finish-a-step`. The *Finish a step* guide folds in and goes.
5. **Fixing and shipping.** `fix-a-bug`, `ship-it` and `finish-a-feature`.
6. **Checking a page.** `check-a-page`.
7. **The rules by file type**, rewritten, with the standing instructions that belong in them, and `prompts.md`.
8. **The folder files**, rewritten.
9. **The inventory checked, and done.** Every sentence of the old files is found in the new ones, line by line.
   The README is checked against the files it describes, and its two pictures move out of this plan to stay with
   it. Louis runs `/doctor prompt-audit`. The docs index, the work log, the write-up, and this plan deleted.

## Verification

- `louis-agent.core.tests` at every step, since it holds the size and the layout of these files.
- Step 1's trial, and at the end a whole step of real work done in a fresh session with the new files, to see that
  each skill loads when its job comes up.
- No paid run beyond that session.

## Concepts

Skill 7 (context engineering): what is in the model's context on every turn costs every turn, so a procedure is
loaded for its job and a rule that must never be missed is loaded always. Skill 3 (prompt design): an instruction
is written where and when it will be read.

## Approved during the build

- **2026-11-13: built on the F134.1 branch, and before its step 6.** This plan said its own branch, after F134.1
  was merged. Louis: "merge this into this branch", "we need CL on this branch so we have to do it first". So
  the plan's branch was merged into `feature/F134.1-the-start-is-a-stage`, and the steps are built there, where
  the docs rules it rewrites already are.
- **2026-11-13, step 1: `.claude` is a group at the top of the solution.** The solution's rule leaves dot folders
  out of its top level. Louis's pick of three (let `.claude` in by name, hang the files under the docs group, or
  attach only a README): let it in by name. So the rule's test names `.claude` as the one dot folder in the
  solution, and Rider's tree shows `rules` and `skills` under it, as on disk.
- **2026-11-13, step 1: the two tests of the instruction files have a folder of their own**,
  `Docs/Instructions/` in core's tests, since the `Docs` folder would have held seven files where six is the most.
- **2026-11-13: which tests, and when.** Louis, while step 4 was built: "we must also add that before
  comitting only touched unit tests, before doing a PR main must be merged and full suite unit tests passes".
  It is in `CLAUDE.md`, so it holds with no skill loaded, and in `do-a-step` and `finish-a-step`. `ship-it`
  takes it too, in step 5.
- **2026-11-13: step 7 is four commits.** As one step it was 345 lines of rules to rewrite and check, too much
  to review in ten minutes. Louis's pick of three (four commits with a stop each, only the additions now and
  the rewrites as their own cleanup, or as planned in one commit): four commits. 7a is `prompts.md` and the
  standing instructions moved in, with nothing rewritten. 7b rewrites `razor.md` and `css.md`, 7c `docs.md`, 7d
  `csharp.md`.

---
[Docs index](../README.md) · Related: [Plans](README.md)
