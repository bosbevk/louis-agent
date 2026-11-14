# Features: the feature docs still to build

> **Plan.** The feature docs that are still open, and what to do when a feature is finished. **Elsewhere:** what is
> next in the [TODO](../../../TODO.md); every finished feature, one row each, in the [work log](../work-log.md); how a
> feature is being built now in [plans/](../plans/README.md); where it is all going in the [roadmap](../../../ROADMAP.md).

A feature doc says **what** to build and **why**: user stories with acceptance criteria, the design, the steps with
their tests, and a definition of done. The ten docs in this folder come from the two specs of October,
[Usage, estimates and budgets](../specs/USAGE_AND_BUDGETS.md) (`U §n` below) and
[Response optimisation](../specs/RESPONSE_OPTIMISATION.md) (`R §n` below); two more sit with their demo.

**Most features have no doc here, and that is on purpose.** A new feature starts as a plan in
[plans/](../plans/README.md), which says the what, the why and the how while it is built; when it is done the plan is
deleted, what lasts moves to the project's design doc, an ADR and the write-up, and the feature keeps one row in the
[work log](../work-log.md). The docs of the first features were folded the same way on 2026-10-29 and are at the tag
`pre-cl24`.

## The feature docs

Each is open work, or the design of work that another feature took in. *In the TODO as* names the row to look for.

| ID | Feature | Status | Spec | In the TODO as |
|---|---|---|---|---|
| [F6](F06-estimates.md) | Cost estimates before a run | merged into F7 | U §5 | F7 |
| [F7](F07-budgets.md) | Budgets: warn, ask, stop | planned | U §6 | F7 *Cost you can see and stop* |
| [F8](F08-reliability.md) | Reliability: retries, account limits, fallback, structured results | planned (F8-S4 moved into F39) | R §F · U §6.2 | F8 *Reliability* |
| [F9](F09-context-management.md) | Bounded history: clear stale tool results, compact old turns | planned | R §D | F9 *Long chats* |
| [F10](F10-route-settings.md) | Settings per route: thinking and tool rounds | planned | R §E | F10 *Settings per role and per session* |
| [F11](F11-usage-tab-and-reconciliation.md) | Usage tab, usage API, reconciliation with the bill | merged into F7 | U §7.1–7.2, §4.4 | F7 |
| [F12](F12-response-speed.md) | Response speed: time to first token and tokens per second, every provider | merged into F44 | U §4.1 · R §A | F44 *The engine as a product* |
| [F13](F13-local-models.md) | Local models: fit every request into a small context window | merged into F15 | R §C | F15 |
| [F15](F15-model-limits.md) | Model limits: each model's context window and cache minimum, `num_ctx` for local models | planned | R §B–D | F15 *Model limits and local models* |
| [F16](F16-summariser.md) | Summariser: one measured component for oversized tool results and compaction | merged into F9 (F16-S1 a small cleanup) | R §D | F9, and F16 under *Cleanups* |
| [F17](../../../demos/louis-agent/docs/features/F17-orchestrator-agent.md) | Orchestrator agent: a routing workflow with an agent for the exceptions | merged into F39 | Orchestrator design | F39 *Orchestration* |
| [F28](../../../demos/puls/docs/features/F28-team-talk.md) | Team talk: Puls briefs its specialist in a fixed note, mood included | planned (the rest) | — | F28 *Team talk, the rest* |

**The milestones** in these docs' status lines (M1 to M5, *Local*) are the order of October's plan. The
[roadmap](../../../ROADMAP.md)'s phases replaced them on 2026-10-19, and the TODO row says where each doc's work sits
now. The milestone table, and the demo-run benchmark that measured M1 and M2, are in
[this page as it was](https://github.com/bosbevk/louis-agent/blob/pre-roadmap/src/docs/features/README.md) and in
[BENCHMARKS.md at `pre-cl10`](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md). Today a
feature is measured with an eval ([Run the evals](../guides/run-the-evals.md)).

## Conventions

- **Stories** are `Fn-Sm`: "As a *role*, I want *something*, so that *benefit*", each with acceptance criteria written
  as *Given / When / Then*.
- **Roles:** *user* (chats in the web app, Rider or CLI), *operator* (runs the agent, sets budgets and prices),
  *developer* (works on this repo), *orchestrator* (the agent itself, where it is the one acting).
- **Tasks** are small enough for one commit each and name their test, which is written and seen failing first
  ([test first](../guides/write-tests.md#write-the-test-first)).
- **Plans:** before step 1, the plan approved for building the feature now is saved in [plans/](../plans/README.md)
  (`Fnn-name.md`) and committed on the feature's branch. A feature doc stays the *what and why*; the plan is the
  *how, now*.
- **Definition of done** for every feature: its *Done when* is met; its tests are added and the suites pass; its eval
  is rerun where the feature touches cost or quality (on Louis's go); and the steps in *Finishing a feature* below.
- **Status** of a feature with a doc is kept in two places that must agree: the top of its doc and the table above
  (*planned → in progress*, or *merged into Fn* when a plan folds it into another feature). A done feature leaves the
  table.

## Finishing a feature

The list a feature finishes by is in the skill that loads on a feature's last step:
[finish-a-feature](../../../.claude/skills/finish-a-feature/SKILL.md#finishing-a-feature). Its steps keep their
numbers, so *Finishing a feature, step 9* is the write-up. A cleanup and each child of a parent finish by it too.

---
[Docs index](../README.md) · Specs: [Usage, estimates and budgets](../specs/USAGE_AND_BUDGETS.md) ·
[Response optimisation](../specs/RESPONSE_OPTIMISATION.md) · [TODO](../../../TODO.md)
