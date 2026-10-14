# Plan: CL4 One type per doc (cleanup)

> **For:** the TODO's *One type per doc* (cleanup) · **Branch:** `docs/CL4-doc-types`, off `docs/refresh` (it builds
> on that branch's guide changes; rebased onto `main` once `docs/refresh` merges) · **Approved:** 2026-10-14
>
> **Decided with Louis (2026-10-14):**
>
> - The TODO's *Done* moves whole to a root `CHANGELOG.md`, not trimmed in place.
> - Finished items leave the TODO. Today's nine ticked lines (F1–F4, F14, F20, CL1–CL3) are deleted, not moved: each
>   already has a *Done* entry, and the features README keeps each feature's status and milestone. From now on,
>   finishing means deleting the line and adding a CHANGELOG entry, so the TODO lists open work only.

## Context

An AI reads the docs the way a person skims them: it opens the doc whose title fits and reads it in full. When one doc
mixes a tutorial, a settings reference, an explanation and a changelog, answering one question loads all four. Shorter,
single-purpose context gives better answers. That much is established; a particular doc layout has not been proven
better than others. **Diátaxis** is a proven structure for human readers (Django, Canonical), and it produces that
kind of context: every doc is one of four types.

| Type | Answers | Already like this |
|---|---|---|
| Tutorial | Get me started | `QUICK_START_AGENT.md` |
| How-to | How do I do X? | `docs/CLAUDE.md` |
| Reference | What exactly is X? | `SETUP.md`, `projects/*.md` |
| Explanation | Why is it like this? | `specs/`, `fixes/`, `ARCHITECTURE.md` |

`docs/README.md` is already the index, in the style of [llms.txt](https://llmstxt.org) (each doc has a "Read it to…"
line). Six docs mix types. That is what this cleanup fixes.

**This is a move, not a rewrite.** No fact is dropped: before a section is cut, every fact in it is found in its new
home, or moved there. Nothing the agent's model sees changes (`Skills/` is untouched). Nothing paid runs.

## Design

**The rule** (added to the docs index and to the guide's *Rules that always apply*): every doc is one Diátaxis type,
and it opens with two lines saying what it answers and where the rest is.

**Where each mixed part goes:**

| Doc | Keeps | Moves out |
|---|---|---|
| `README.md` (230 lines) | Pitch, Puls, ways to use it, a three-command quick start, the docs index link | Configuration → `SETUP.md`; HTTP API → `AGENT_COMMUNICATION.md`; Web app → `projects/louis-agent.web.md`; Skills and tools, Safety → `ARCHITECTURE.md`; Logs → the guide's *Running and debugging* |
| `AGENT_INTERACTION.md` | How a turn works (explanation) | *Extending* (an out-of-date how-to: "register it in the `AgentEngine` constructor") → deleted, with links to the guide's how-tos; its `## Skill:` example → the guide's *Adding a skill* |
| `ARCHITECTURE.md` | The components and why | *Configuration reference* → links to `SETUP.md` and `MODELS.md` (anything they lack is moved first) |
| `features/README.md` | The features table, milestones, conventions, *how* to benchmark | The four benchmark runs (~115 lines) → new `docs/BENCHMARKS.md` (reference) |
| `TODO.md` (429 lines) | Now, Next, Backlog, Known limitations: open work only | *Done* (81 lines) → new `CHANGELOG.md` at the root, newest first, and the TODO links to it; the nine ticked lines are deleted (each is already in *Done*) |
| `docs/README.md` | The index | Gains a *Type* column, and rows for `BENCHMARKS.md` and `CHANGELOG.md` |

**Links:** anchors that move are updated everywhere. For example, the TODO's Done links to
`features/README.md#local-triage-2026-10-12`, which becomes `BENCHMARKS.md#…`. Historical docs (plans, fix docs) keep
the paths they were written with, as CL1 did, unless the link would break.

**Not in this plan:**
- Nested `CLAUDE.md` files and project skills for Claude Code. That's the next idea, with its own plan.
- Moving the backlog's design notes into `specs/`.
- The `samples/` `.ignore` file.

## Steps (each one commit; a review stop after each)

0. **The plan in the repo:** `docs/plans/CL4-doc-types.md`, its row in the plans table, and the TODO line.
1. **The rule and the index:** the rule in `docs/README.md` (a *Writing docs* paragraph) and in the guide; a *Type*
   column in the index. Add the two-line opening to the docs no later step touches: `QUICK_START_AGENT`, `SETUP`,
   `MODELS`, `PAYMO-PROMPTS`, `RUNBOOKS`, `AGENT_COMMUNICATION`, `GOAL`.
2. **`AGENT_INTERACTION.md`:** *Extending* removed. Its skill example goes to the guide.
3. **`ARCHITECTURE.md`:** *Configuration reference* replaced by links. Anything SETUP or MODELS lacked is moved there
   first.
4. **`docs/BENCHMARKS.md`:** the four runs moved out of `features/README.md`. Update the links (TODO, learning
   write-up 13, GOAL), and *Finishing a feature* step 5 now points at BENCHMARKS.
5. **`CHANGELOG.md`:** the TODO's *Done* moved there, and the nine ticked lines deleted after checking each has its
   entry. *Finishing a feature* and *Finishing a fix* now say "delete the TODO line and add a CHANGELOG entry". So does
   the features README's "tick it there when the feature is done".
6. **`README.md`:** cut down to its own type, one section per commit if the diff is too large to review in ten
   minutes. Every section that goes is checked against its new home first.
7. **Finish:** the CL4 line deleted from the TODO and its CHANGELOG entry; the guide's *Steps, branches and commits* mentions the
   rule.

## Verification

- **A link check after every step:** a throwaway script in the scratchpad, not committed and no new package. It
  resolves every relative `.md` link and `#anchor` across `README.md`, `TODO.md`, `docs/`, `samples/*/README.md` and
  `Skills/`. Zero broken links, compared with a baseline run before step 1.
- **No fact lost:** for each section moved or cut, its key terms (setting names, endpoints, type names) are grepped in
  the destination before the cut. The step's message lists any that had to be moved.
- **Line counts** before and after: README, TODO, ARCHITECTURE, features/README and AGENT_INTERACTION.
- **Nothing the model sees changes:** `git diff --stat main -- Skills/` is empty. `dotnet test tests/louis-agent.core.tests
  --filter "ModelRequestSnapshotTests|SkillFilesTests"` passes, as a safety check.
- No paid run.

## Concepts

- **Context engineering for docs (#7):** single-purpose documents, an index that says which doc answers what, and
  detail one link deeper. It's the same progressive disclosure as F5's toolset guides, applied to the repo's own docs.
- **One source of truth (#18):** a fact in two docs drifts. The out-of-date *Extending* section is the example.
- **Checking docs like code (#18):** a link check that runs in seconds turns "the docs link correctly" from a hope into
  a result.
