# louis-agent.evals.puls — design

> **Kind:** class library (`net10.0`) · **Path:** `evals/louis-agent.evals.puls/` · **References:** louis-agent.evals
> · **Used by:** louis-agent.evals.runner

## Purpose

Puls's evals: what proves that Puls routes a task to the right specialist and that its cook answers a meal well. The
eval set sits beside the identity it proves, in its own project, so the harness names no product and a second
identity gets a project of its own ([ADR 0029](../adr/0029-simulations-in-csharp.md)).

## How it works

- **No eval left, only the cook's checks.** `puls-routing` and `puls-cook` are folders of files beside the identity
  since F49.2 steps 6 and 8b ([louis-agent.identity.puls](louis-agent.identity.puls.md)), and `EVALS=puls` picks them
  by the identity their files name (step 8c deleted the sets). What's left are the cook's checks and the rules they
  read; step 9b moves them beside the identity and deletes this project.
- **The cook's fixed rules** (`Evals/Cook/Grading/`): no failed turn, within the budget, estimates at the shop that
  serves the town, no price column for a shop the cook can't read, an image from the recipe, and serves as asked.
  Each carries a nested `Entry` and is listed in `PulsChecks.All`, which the runner gathers into its check catalogue,
  so the cook's file names them. The model judge `meal-for-people` left in step 9a for its own project beside the
  identity ([louis-agent.identity.puls.judges](louis-agent.identity.puls.judges.md)), since it needs the harness.
- **The rules they read**: the scenario's expectations (`Evals/Cook/Expectations/`), the scoring of prices and
  servings (`Evals/Cook/Scoring/`), and in `Shared/` the turn reading, the shops and links, and the names as enums
  (ADR 0043). With no eval left, `EvalLayoutTests` no longer looks at this project.

## Structure

| Path | What's in it |
|---|---|
| `Evals/Cook/` | The cook's checks (`Grading/`, `PulsChecks.All`), expectations, scoring, and `CookMode` |
| `Shared/` | Turn reading, shops and links, `PulsEvalName`, `PulsEvalRole`, `PulsToolset`, `PulsVoice` |
| `Messages/` | The checks' reasons (`GradeMessage`, `ExpectationMessage`, ADR 0009) |

## Extending it

A new Puls eval is a folder of files beside the identity (`identities/louis-agent.identity.puls/Evals/`), with no C#;
a check only Puls's evals need is a class with an `Entry` here, listed in `PulsChecks.All`, until step 9 moves the
list beside the identity.
The steps are [Add an eval set](../guides/add-an-eval-set.md); running them is
[Run the evals](../guides/run-the-evals.md).

## Tests

`tests/louis-agent.evals.puls.tests`: each grader on hand-cut answers, a turn the infrastructure broke left Unknown
by every check (`InfrastructureFailureTests`), the datasets (`PulsDatasetsTests`: versions,
hashes, every expected key read), the ids as test constants (`PulsTaskIds`, `PulsCookScenarioIds`), and the
conformance of both evals against their references and known-bad transcripts.

## Limits and plans

- The first v2 baseline (2026-10-29): `puls-cook` 23 of 24 in its cook mode, 6 of 24 when Puls answers itself;
  `puls-routing` 72 of 72; the local judge agrees 31 of 36 with a blind labeller, below the 90% bar
  ([louis-agent.evals](louis-agent.evals.md#design-notes)).
- The held-out scenarios still have their older, hand-cut references.

## Related docs

[Run the evals](../guides/run-the-evals.md) ·
[louis-agent.identity.puls](louis-agent.identity.puls.md) · [louis-agent.evals](louis-agent.evals.md)

---
[Projects](README.md) · Previous: [louis-agent.evals.runner](louis-agent.evals.runner.md) · Next: [tests](tests.md)
