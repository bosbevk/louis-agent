# louis-agent.evals.puls — design

> **Kind:** class library (`net10.0`) · **Path:** `evals/louis-agent.evals.puls/` · **References:** louis-agent.evals
> · **Used by:** louis-agent.evals.runner

## Purpose

Puls's evals: what proves that Puls routes a task to the right specialist and that its cook answers a meal well. The
eval set sits beside the identity it proves, in its own project, so the harness names no product and a second
identity gets a project of its own ([ADR 0029](../adr/0029-simulations-in-csharp.md)).

## How it works

- **One set, two evals.** `PulsEvalSet` lists `puls-cook` and `puls-routing`; the set keeps its own copy of the
  identity's name (a set never references an identity), held to the original by a test.
- **`puls-cook`** (`Evals/Cook/`) asks the dataset's meal questions in two modes: `puls-cook` (Puls hands the meal to
  its cook) and `puls-tools` (Puls answers itself with the web tools). Its graders: no failed turn, routed to the
  cook, within the budget, estimates at the shop that serves the town, an image from the recipe, serves as asked,
  and the model judge `meal-for-people` with its human review `meal-for-people.human`.
- **`puls-routing`** (`Evals/Routing/`) asks one question per kind (chat, cook, files, git, numbers, web) in
  `baseline` and `delegated` modes, and grades where it went (`routed-to`).
- **One folder per eval.** Each holds the eval, its setup, its dataset (`Dataset/`, Inspect's Sample shape, step 7d),
  its graders and scoring rules, and its transcripts (`Transcripts/`: a reference per scenario, cut from a real run
  that passed every grader, and known-bad ones). What both evals use is in `Shared/` (expectations, turn reading,
  the names as enums, ADR 0043).

## Structure

| Path | What's in it |
|---|---|
| `PulsEvalSet.cs` | The set the runner lists |
| `Evals/Cook/`, `Evals/Routing/` | Each eval, its dataset, graders, scoring and transcripts |
| `Shared/` | Expectations, grading and scoring helpers, turn reading, `PulsEvalName`, `PulsEvalRole`, `PulsModes`, `PulsToolset`, `PulsVoice` |
| `Manuals/` | Each eval's manual, shown in the portal (`cook.md`, `routing.md`) |
| `Messages/` | The graders' reasons and the set's texts (`GradeMessage`, `ExpectationMessage`, `PulsEvalMessage`, ADR 0009) |
| `Prompts/judges/` | The model judges' prompts |

## Extending it

A new Puls eval is a folder under `Evals/` with its dataset and transcripts, and a line in `PulsEvalSet`; a new
scenario is a file under the dataset's split folder, then a new dataset version with its `changes` line and hash.
The steps are [Add an eval set](../guides/add-an-eval-set.md); running them is
[Run the evals](../guides/run-the-evals.md).

## Tests

`tests/louis-agent.evals.puls.tests`: each grader on hand-cut answers, the datasets (`PulsDatasetsTests`: versions,
hashes, every expected key read), the ids as test constants (`PulsTaskIds`, `PulsCookScenarioIds`), and the
conformance of both evals against their references and known-bad transcripts.

## Limits and plans

- The first v2 baseline (2026-10-29): `puls-cook` 23 of 24 in its cook mode, 6 of 24 when Puls answers itself;
  `puls-routing` 72 of 72; the local judge agrees 31 of 36 with a blind labeller, below the 90% bar
  ([F18 step 5.5](../features/F18-evaluation.md)).
- The held-out scenarios still have their older, hand-cut references.

## Related docs

[F18](../features/F18-evaluation.md) · [Run the evals](../guides/run-the-evals.md) ·
[louis-agent.identity.puls](louis-agent.identity.puls.md) · [louis-agent.evals](louis-agent.evals.md)

---
[Projects](README.md) · Previous: [louis-agent.evals.runner](louis-agent.evals.runner.md) · Next: [tests](tests.md)
