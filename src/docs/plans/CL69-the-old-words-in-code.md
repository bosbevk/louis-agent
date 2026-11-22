# CL69 The old words in code

A plan, for Louis's approval: the words "specialist" and "minion" leave the code outside the evals. CL68.2 made the
specialists subagents (ADR 0133) and CL68.3 made the minions lookup agents (ADR 0021), and both renamed what the
model and the pages read; what is left is 517 lines in 133 files of names, comments, test names, test fixtures and
three texts a person or the model reads. Nine steps, one commit each, on `refactor/CL69-the-old-words-in-code`.

![CL69 in nine steps: a test that holds the words, the three texts, the fixtures, core, its tests, the API and the stream, the web, the rest, then the docs](diagrams/CL69-the-old-words-in-code-overview.svg)

## Context

CL68.2 and CL68.3 renamed the levels where the model and the visitor see them, and left the rest for a cleanup of
its own: "the old words left in code: \"specialist\" in the framework's web library (`SpecialistAnswer`…) and
\"minion\" in about 30 files' comments and 25 test methods' names". Louis put CL69 next, after CL70. On 2026-11-22
the scope was put to him on one card, since a search found far more than the row said, and he picked **A:
everything outside the evals, the test fixtures included**, over the same without the fixtures and over everything
with the evals too. Of the evals he had said: "we are going to scrap all about evals and redo it total rework".

What was read for this plan, on `main` at `20025d6ba`. Outside the docs the two words are on 790 lines in 233 files.
273 lines in 100 files are the eval code (the protocol's `ModelLevel` with its wire names `"specialist"` and
`"minion"`, `SpecialistCalls` in turn events and run rows, the eval studio), and they stay. The other 517 lines in
133 files are these. Code names: the web library's `SpecialistAnswer` (with `AnswerActions` and `DelegationCard`),
`WorkPanel`'s `SpecialistUsage`, `SkillErrorMessage.MinionNeedsRole`, a stale `<param name="Minion">` on the web's
`Delegation`, the test helpers `AskMinion`, `StartMinion`, `ContinueMinion`, `MinionsFolder`, and about 40 test
methods (`ASpecialist_LoadsTheToolsetsItsRoleNames`, `AnUnknownMinion_IsRefused_NamingTheIdentitysMinions`).
Fixtures: the test subagent `testprod.specialist` (its folder `agents/testprod/subagents/specialist/`), the API's
`TestAgent.Specialist` and its `-minion` base, and addresses such as `minion-lookup`, on 54 lines in 20 files. Three
texts: `QuoteTools`' parameter description, "as the minion (or you) copied them", which the model reads (a golden
file holds it); `MinionNeedsRole`, "A minion opens with a role"; and `ProbeUserText`'s "the specialists' own
requests". The rest are comments and summaries.

Three things keep the old words on purpose, and Louis is asked to confirm them with this plan. Louis's CV
(`src/site/LouisAgent.Site/Content/cv.json`) says "the Puls specialists", his own words about Puls, which has its own
repo now. The coding agent's persona is called "Louis's Minion" in its voice, a name that is his to change. And the
eval code, by his pick.

## Design

### What changes

| Today | After |
|---|---|
| `SpecialistAnswer`, `SpecialistUsage`, `MinionNeedsRole` | `SubagentAnswer`, `SubagentUsage`, `LookupNeedsRole` |
| `testprod.specialist`, `TestAgent.Specialist`, `testagent-minion` | `testprod.researcher`, `TestAgent.Researcher`, `testagent-lookup` |
| About 40 test methods named for a specialist or a minion | Named for a subagent or a lookup agent |
| Three texts that say minion or specialists | They say lookup agent or subagents |
| Nothing holds it | `OldWordsTests`: no text says the words, and a list of the files that still do, which only shrinks to none |

The fixture's new name says what its voice does: it "always loads the web", and says "what you found and where", so
it is a researcher.

### The shape

Every symbol is renamed with Rider's rename, which follows each reference (Fowler's *Rename Method* and *Rename
Field*; ADR 0052); a record's primary-constructor parameter, which Rider couldn't rename in CL68.3, is changed by
hand and said so. Comments and summaries are edited where they stand, and each file in a step's commit is fixed
whole. The guard comes first: `OldWordsTests`, in core's tests beside the other rules that read the repo, holds two
things. No `[Description]` outside the evals says "minion" or "specialist", so a text the model or a person reads
can't slip back. And the files outside the evals, the docs and the three kept places that still say them are a
list in the test, which each step shortens, a ratchet like `WriteUpsTests`' owed list; at step 8 it is empty and
the test holds the words gone.

![The interface design: the types renamed, the fixtures renamed, and OldWordsTests reading the repo](diagrams/CL69-the-old-words-in-code-interface-design.svg)

![The architecture: the projects each step reaches, the evals left as they are](diagrams/CL69-the-old-words-in-code-architecture.svg)

![A step: shorten the list, rename with Rider, run the touched tests and OldWordsTests](diagrams/CL69-the-old-words-in-code-communication-a-step.svg)

![Objects in memory: OldWordsTests holding the kept places, the list that only shrinks, and the files it reads](diagrams/CL69-the-old-words-in-code-objects-in-memory.svg)

### Namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `OldWordsTests` | `src/tests/engine/LouisAgent.Core.Tests/Docs/` | `LouisAgent.Core.Tests.Docs` | 1 |
| `QuoteTools`' text, `SkillErrorMessage.LookupNeedsRole`, `ProbeUserText`'s text, the golden files | `packs/tools/LouisAgent.Packs.Web/Quotes/`, `src/engine/LouisAgent.Core/Skills/Messages/`, `src/tools/LouisAgent.UsageProbe/Messages/` | as today | 2 |
| `testprod.researcher`'s voice, `TestAgent.Researcher` | `src/tests/engine/LouisAgent.Core.Tests/agents/testprod/subagents/researcher/`, `src/tests/hosts/LouisAgent.Api.Testing/` | as today | 3 |
| Core's names and comments | `src/engine/LouisAgent.Core/` | as today | 4 |
| Core's tests | `src/tests/engine/LouisAgent.Core.Tests/` | as today | 5 |
| SessionStream, Composition, Core.Web, the API, its contracts' summaries, their tests | `src/engine/`, `src/hosts/`, `src/tests/` | as today | 6 |
| `SubagentAnswer`, `SubagentUsage`, `Delegation`, their tests | `src/web/LouisAgent.Web/`, `src/tests/web/` | as today | 7 |
| The tools, packs, demos and shared tests; the list empty | `src/tools/`, `packs/`, `demos/louis-agent/tests/`, `src/tests/shared/` | as today | 8 |

### What is reused

Rider's rename for every symbol, the repo-reading pattern of the rule tests in core's tests (`WriteUpsTests`'
owed list, the file and folder size tests), and every existing test as the safety net: a rename changes no
behaviour, so each step's touched tests pass unchanged in what they assert. No package is added.

## Steps

One commit each, with a review stop after each.

1. **The guard.** `OldWordsTests` with its list of today's files. Test first: its text check fails on the three
   texts, and its list check passes, since the list is today's.
2. **The three texts.** `QuoteTools`' description, `MinionNeedsRole` renamed `LookupNeedsRole` with its text, and
   `ProbeUserText`'s line; the golden files regenerated and read word by word. The text check passes.
3. **The fixtures.** `testprod.specialist` to `testprod.researcher`, with its folder; `TestAgent.Specialist` to
   `Researcher` and its `-minion` base to `-lookup`; the `minion-lookup` addresses. Held by the tests that use them.
4. **Core.** Its code names and comments (24 files).
5. **Core's tests.** Test method names through Rider, the helpers, comments (32 files).
6. **The API and the stream.** SessionStream, Composition, Core.Web, the API and the summaries of its contracts,
   with their tests (about 25 files). `SpecialistCalls` in the run rows stays: it is the eval protocol's.
7. **The web library.** `SubagentAnswer`, `SubagentUsage`, the `Delegation` summaries, the chat's components, and the
   web's tests (about 35 files).
8. **The rest, and the list empty.** The usage probe, the packs' and demos' tests, the shared test helpers; the
   list in `OldWordsTests` is empty, so the test now holds the words gone.
9. **The docs, and closed.** The design docs that name a renamed type, the write-up's section in
   `10-multi-agent-orchestration.md` (skill 10), and this plan folded (`/finish-a-feature`).

Each of steps 3 to 8 is a pure rename and adds no test: it is held by the tests already there, and by
`OldWordsTests`' list shrinking (do-a-step, *Test first*).

## Verification

- **The tests** of each step, `OldWordsTests` among them, and before the pull request, with `main` merged in, the
  full unit suite.
- **Free checks by hand:** the code-review chat on :5080 shows a subagent's card and a lookup agent's card as before.
- **No paid run:** the one text the model reads changes a word in a parameter's description.
- **Done when:** outside the evals, the docs, the CV and the persona, no file says "minion" or "specialist", and
  `OldWordsTests` holds it.

## Concepts

- **A ratchet** (step 1): a list in a test that may only shrink, so a big change can land in steps and never go
  back. GOAL.md skill 18, *Testing and shipping*.
- **One word for one thing** (steps 2 to 8): the levels' names match in the prompts, the code and the docs, so a
  reader, or the model, never meets two names for one idea. Skill 10, *Multi-agent orchestration*.

## Approved during the build

Nothing yet.

## The records

In the commit that saves this plan: its row in the plans' index, and CL69's row in the TODO pointing at it.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [ADR 0133](../adr/0133-subagents-live-inside-their-agent.md) · [ADR 0021](../adr/0021-minions-one-task-one-tool.md)
