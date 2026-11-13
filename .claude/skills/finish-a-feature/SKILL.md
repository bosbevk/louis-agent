---
name: finish-a-feature
description: Close a feature, a cleanup or a child of a parent on its last step. Use it when a plan's last step is reached or its done-when is met, before the pull request that finishes the work. It holds the list a feature finishes by, with the TODO and work-log rows, the docs of every project it touched, the write-up, the plan folded and deleted, and what waits for Louis's word.
---

# Finish a feature

The feature's *Done when* is met. This closes it, so the TODO, the docs and the code agree again. Four stops come
first.

1. **All of the list goes in the same commit**, so the TODO, the specs and the docs never drift apart. Nothing is
   left for a later tidy-up.
2. **A live eval run waits for Louis's go.** It spends money.
3. **A new ADR waits for his approval.** Propose it in a few lines, the rule and its reason, before the file is
   written.
4. **`main` is merged in and the full suite of unit tests passes before the pull request.** That is in `/ship-it`.

## Finishing a feature

When a feature's *Done when* is met, do all of this in the same commit, so the TODO, the specs and these docs never
drift apart. A cleanup (`CL`) finishes by the same list, and so does each child of a parent (`F49.1`):

1. **TODO and the work log:** delete the feature's row in the TODO, and add its row (id, date, what in a few
   words) under *Features* (or *Cleanups* for a `CL`) in the [work log](../../../src/docs/work-log.md), newest first, with the
   count above (`WorkLogTests` holds the counts and that nothing open is listed as done).
2. **The features folder** (`src/docs/features/`): if the feature has a doc there, it is folded and its row leaves that folder's table (step 10).
3. **Spec:** in the spec's *Implementation* map, mark the feature done; change anything the build proved different
   (for example F4's two cache markers instead of three).
4. **Limitations:** if the feature removes a known limitation (F10: tool rounds), delete it from the
   TODO's [Known limitations](../../../TODO.md#known-limitations) (the one list).
5. **Measure:** for a feature that changes cost or quality, run its eval ([Run the evals](../../../src/docs/guides/run-the-evals.md);
   a live run spends money, so on Louis's go). The run is kept in the eval store, so the write-up names the run's id
   and its numbers; the runs before CL10 stay in
   [BENCHMARKS.md at `pre-cl10`](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md).
6. **The docs of every project it touched** (Louis, 2026-11-13): list the projects the feature's commits changed
   (`git diff --name-only main...HEAD`, by folder). For each one, read its design doc and its how-to (both show under
   `docs/` in the project in Rider) against the code as it is now: the parts, the structure table, the settings, how
   to extend it. Its pictures too: one that no longer shows the code is changed in its JSON and redrawn with the tool
   ([Draw a diagram](../../../src/docs/guides/draw-a-diagram.md)), never by hand. Then what users see (settings in
   [SETUP](../../../src/docs/SETUP.md)), and the lists no single feature owns, when the feature adds a project, a pack, a host or a
   setting: the README's *Where it stands*, the [ROADMAP](../../../ROADMAP.md)'s *What exists*, the indexes
   ([docs](../../../src/docs/README.md), [projects](../../../src/docs/projects/README.md), [guides](../../../src/docs/guides/README.md)) and the project
   structure in the [development guide](../../rules/repo-map.md).
7. **Decisions:** if the feature made a lasting decision, its [ADR](../../../src/docs/adr/README.md) is in the same PR; a decision it
   changed is superseded, not edited.
8. **Goal map:** in [docs/GOAL.md](../../../src/docs/GOAL.md#what-im-learning-the-whole-agent-architecture), move what the feature
   delivered into *Have today* and drop it from *Planned* or *Missing*; if it closes a line in the TODO's *Later*, remove
   that line.
9. **Write-up** (Louis, 2026-11-13): every feature and cleanup adds its section to the write-up of the skill it teaches
   in [docs/learning/](../../../src/docs/learning) (a new file when the skill has none yet), and its words to that write-up's row
   in the [list](../../../src/docs/learning/README.md): what was built, what was measured, the trade-offs, what didn't work. Claude
   writes it, for Louis to read afterwards. `WriteUpsTests` fails for a done row no write-up names; the ones owed
   on 2026-11-13 are listed in it, and that list only shrinks.
10. **Fold and delete** (Louis, 2026-10-28): move what lasts in the feature's plan, and in its doc if it has one (how
    it works, its settings, its limits, its open items), into its project's design doc in
    [docs/projects/](../../../src/docs/projects/README.md), its numbers and lessons into the write-up, and any open item into the
    TODO; then delete the plan and the doc, and take the doc's row out of that folder's table. The work log's row (step 1)
    is what stays. The docs folded on 2026-10-29 are at the tag `pre-cl24`.

## Then

`/ship-it`, for the pull request that finishes the work.
