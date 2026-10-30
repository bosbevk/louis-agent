# louis-agent.identity.puls.judges — design

> **Kind:** class library (`net10.0`) · **Path:** `identities/louis-agent.identity.puls.judges/` · **References:**
> louis-agent.evals (the harness), louis-agent.evals.puls (until F49.2 step 9b, then the identity) · **Used by:**
> louis-agent.evals.runner only

## Purpose

Puls's model judge: the cook's `meal-for-people`, which asks a model whether an answer is a meal for the people asked
for. It sits beside the identity it judges, but in its own project, because a judge is built on the harness's judge
(`EvaluatorCheck`, `JudgeClients`) and the identity must never reference the harness
([ADR 0027](../adr/0027-engine-identities-and-packs.md)). Only the runner references it; the composition never does,
so no host (the API, the CLI, the ACP and MCP servers) carries the harness.

## How it works

- **The judge** (`MealForPeopleCheck`) reads its rubric from the cook's eval folder
  (`identities/louis-agent.identity.puls/Evals/Cook/Rubrics/meal-for-people.md`), asks with the people and the town
  the scenario expects (`CookAsk`), and reads a yes-or-no verdict with a reason (`MealVerdict`). It is shown and counted
  but not required until its calibration passes. A person's review of it (`human`) asks the rubric's title.
- **The list** (`PulsJudges.All`) is what an eval file can name; the runner gathers it with the harness's general
  checks and Puls's fixed rules (`Catalog/RunnerChecks`).

## Extending it

A new judge of Puls's is a class here with a nested `Entry`, listed in `PulsJudges.All`. A judge every identity could
use is the harness's general `judge` entry instead, which needs no C#.

## Tests

`tests/louis-agent.identity.puls.judges.tests`: the judge on a scripted client, the prompt it sends against its golden
(`meal-for-people.sent.txt`), the rubric's hash (`161e5cebcc15`, the one its calibration measured), its entry built
from an eval folder, and the list. Its small helpers (`JudgeTrials`, `CookRubric`) are its own, cut from the Puls
evals' tests' helpers, since no test project references another.

## Related docs

[louis-agent.identity.puls](louis-agent.identity.puls.md) · [louis-agent.evals](louis-agent.evals.md) ·
[louis-agent.evals.runner](louis-agent.evals.runner.md) · [Run the evals](../guides/run-the-evals.md)

---
[Projects](README.md) · Previous: [louis-agent.identity.puls](louis-agent.identity.puls.md) · Next: [louis-agent.pack.shops](louis-agent.pack.shops.md)
