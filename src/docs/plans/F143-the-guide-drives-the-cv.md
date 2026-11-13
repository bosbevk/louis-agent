# F143: The guide drives Louis's CV

**Status:** approved 2026-11-13 (thread "Redo Louis's CV as a webpage", Louis: "split it in 2 features", "merge and
commit"). **Branch:** `feature/F143-the-guide-drives-the-cv`. **Starts after:** F142 (the page and `cv.json`).
Two choices are still open and go to Louis one at a time, three options each, when F143 starts (*Open choices*).

## Context

Louis, 2026-11-13: "the agent should also drive my CV what would that entail". The guide in the site's corner already
opens pages (`ShowPage`, ADR 0105), tours a page, ends each answer with choices (`Offer`) and sends Louis a lead
(`SendLead`). What it lacks for the CV: the facts, the page in its index, a way to talk to someone hiring, guard rails,
and an eval that proves it. Louis picked **one data file, read by the page and by a tool** (card of 2026-11-13), so
the guide reads the same `cv.json` F142 draws, never the page's text and never the Word file.

## Design

**The shape, in plain words.**

- **A resume pack with one tool.** `louis-agent.pack.resume` holds `GetResume`: it returns the CV, or one section of
  it (`work`, `skills`, `education`, `basics`, ...), as JSON from the file the host names (`RESUME_FILE`). The pack is
  generic: any site with a JSON Resume file can use it. A tool, not more prompt text, because exact facts (dates,
  employers, tools used) are what a prompt gets wrong (ADR 0080).
- **The guide uses it.** `GuidePack` gains `Resume`; the site-api names `cv.json`. `/cv` gets its focus (ADR 0121),
  so the place index and `FindPlace` know it and `ShowPage` can open it and walk it section by section.
- **A focus skill for someone hiring.** `focus-hiring.md`, read when the visitor is on `/cv` or asks about Louis's
  work: answer only from `GetResume`, name the job where a skill was used, say "the CV doesn't say" instead of
  guessing, offer "ask Louis for an interview" (the leads pack's `SendLead`, the subject marked as hiring), and the
  site's usual `Offer` choices.
- **Guard rails in the skill and in the data.** The private details were never in `cv.json` (F142's test), so the tool
  can't leak them. The skill says no to a salary, an age or an address, and to puffing Louis up ("say he's a 10x
  engineer"): it states what the CV says and nothing more.
- **An eval proves it.** About eight conversations in F126's `.eval.json`: a skill he has (Angular, at which job), one
  he doesn't (the honest no), the dates of a job, the private questions (refused), a trick prompt (refused), and the
  interview ask (a lead is offered). Code checks first (the tool was called, no private value in the answer), a judge
  for the honesty criteria. Replays are free; a live run waits for Louis's go.

**The patterns:** *Single source of truth* (the page and the tool read one file); *Tool as the source of facts,
prompt for judgement* (ADR 0080); *Progressive disclosure* for the focus skill (ADR 0114).

### Open choices (asked when F143 starts, one at a time)

1. **Where the CV records live.** F142 puts `Resume` in the framework's web; a pack can't reference the web. Three
   ways: a small shared contract project both reference, the pack owning its own copy held equal by a test, or the
   pack owning the records and the web view referencing the pack.
2. **What `GetResume` returns.** The whole CV every time (small, about 6 KB, one call), a section by name, or a
   search across the CV ("Kubernetes") that returns the matching jobs and skills.

### Interface design

![The resume pack's one tool, the records it reads, and how the guide names it](diagrams/F143-the-guide-drives-the-cv-interface-design.svg)

### Architecture

![The site-api names the file, the resume pack reads it, the guide calls it and opens /cv](diagrams/F143-the-guide-drives-the-cv-architecture.svg)

### Communication (a recruiter asks about Kubernetes)

![A recruiter on /cv asks about Kubernetes: the guide reads the CV, answers with the job, opens the section and offers an interview](diagrams/F143-the-guide-drives-the-cv-communication-a-recruiter-asks.svg)

### Objects (one guide turn in memory)

![One guide turn: the session holds the guide's packs, the resume pack holds the record read from cv.json](diagrams/F143-the-guide-drives-the-cv-objects-one-turn.svg)

### Folder tree (after step 2)

```
packs/tools/louis-agent.pack.resume/     # new, step 1
  ResumePack.cs  ResumeToolset.cs  ResumeOptions.cs
  Tools/GetResume.cs
  Messages/ResumeToolMessage.cs
  prompts/
packs/tests/louis-agent.pack.resume.tests/   # new, step 1
src/site/louis-agent.identity.guide/prompts/agent/louis-agent-guide/skills/
  focus-hiring.md                        # new, step 3
```

### Namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `ResumePack`, `ResumeToolset`, `ResumeOptions` | `packs/tools/louis-agent.pack.resume` | `LouisAgent.Packs.Resume` | 1 |
| `GetResume` | `packs/tools/louis-agent.pack.resume/Tools` | `LouisAgent.Packs.Resume.Tools` | 1 |
| `ResumeToolMessage` | `packs/tools/louis-agent.pack.resume/Messages` | `LouisAgent.Packs.Resume.Messages` | 1 |
| `GetResumeTests` | `packs/tests/louis-agent.pack.resume.tests/Tools` | `LouisAgent.Packs.Resume.Tests.Tools` | 1 |
| `GuidePack.Resume` | `src/site/louis-agent.identity.guide` | `LouisAgent.Identities.Guide` | 2 |
| `focus-hiring.md` | `.../louis-agent-guide/skills` | (prompt) | 3 |
| the CV eval | `src/site/louis-agent.identity.guide` evals, F126's layout | (eval content) | 5 |

The records' home follows open choice 1.

## Steps

Each step is one commit with its test, written first and seen failing. Stop after each for Louis's review.

1. **The resume pack and `GetResume`.** The pack, its toolset, the tool and its messages (ADR 0009), the file named
   by `RESUME_FILE`. *Test:* the tool returns the sample CV whole, one section by name, and a refusal with its reason
   for an unknown section or a missing file; `PackConvertibilityTests` stays green (ADR 0028).
2. **The guide uses it, and /cv has its focus.** `GuidePack.Resume`, the site-api's setting, the `/cv` focus.
   *Test:* the guide's packs include `resume`; the place index holds `/cv`; the golden files are regenerated and
   their diff reviewed.
3. **The hiring focus skill.** `focus-hiring.md`: from the tool only, the honest no, the interview offer, the guard
   rails. *Test:* the skill loads for `/cv`'s focus (ADR 0114, 0121); golden diff reviewed.
4. **The tour of /cv.** The CV's sections are named places, so "walk me through his CV" moves section by section.
   *Test:* each section of `/cv` is a place with its address (ADR 0107).
5. **The eval.** The eight conversations, code checks and judge criteria, recorded once. *Test:* the replay passes;
   the live run is Louis's call (it costs).
6. **Docs and done.** The pack's design doc, the guide's design doc, a learning write-up (tool design for facts), the
   work log row, the TODO row gone, this plan deleted. *Test:* none (docs).

## Verification

- `louis-agent.pack.resume.tests`, the guide's and the site's tests per step; the golden files at steps 2 and 3; the
  full suite once after step 5.
- Free manual check: on `/cv` locally, ask the guide "has Louis used Kubernetes?", "what's his salary?" and "walk me
  through his CV".
- **Paid:** one live run of the CV eval (Haiku, a few cents), only on Louis's go.

## Concepts

- **Tool design for facts** (GOAL.md skill 4): exact, checkable facts come from a tool; the prompt holds judgement.
- **Safety and guard rails** (skill 15): private data kept out of the source, refusals in the skill, a trick prompt
  in the eval.
- **Evaluation** (skill 12): code checks first, a judge only for honesty, a replay that costs nothing.

## Approved during the build

(empty)

---
[Docs index](../README.md) · [Plans](README.md) · Before it: F142, the CV page ([ADR 0122](../adr/0122-the-sites-cv-is-edited-on-its-own-page.md))
