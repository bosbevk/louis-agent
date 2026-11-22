# 10. Quality requirements

> **Explanation.** How the quality goals of [section 1](01-introduction-and-goals.md#quality-goals) are checked, as
> scenarios with a measure. **Elsewhere:** what a request costs in [the ledger's decision](../adr/0007-measure-every-request.md).

![The quality requirements: measured cost, correctness, safety and provider portability, each as scenarios with a measure](diagrams/10-quality-requirements.svg)

A quality goal only counts when something checks it. So each goal below comes as a scenario, something that happens,
with the measure that says whether it held and where you can see it.

The evals are being rebuilt from scratch, so no eval runs today. When they come back, an eval is one file of
conversations beside the identity it proves, run through real sessions on the API ([ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md)).
Until then, cost is measured by the usage ledger, and correctness by the tests and by the orchestrator's own checks.

| Quality | Scenario | Measure | Where |
|---|---|---|---|
| Measured cost | A user asks what an answer cost | Every host shows the turn's usage and cost from the same records as the ledger | [0007](../adr/0007-measure-every-request.md) |
| Measured cost | A subagent's prompt keeps growing | It stops calling tools before its next request passes `SUB_SESSION_PROMPT_BUDGET` (80k, under the long-prompt price step) | [FX14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX14-sub-session-prompt-budget.md) |
| Correctness | The orchestrator fixes a service's error | It verifies each fix itself: it checks the commit, replays the request and runs the tests | [Runbooks](../../../demos/louis-agent/docs/projects/orchestrator-runbooks.md) |
| Safety | The model asks to read a secret or leave the workspace | The file tools refuse; tests cover the boundary and sensitive files | [Security](08-crosscutting-concepts.md#security) |
| Safety | A host on the network starts without its key | It refuses to start, unless `AGENT_AUTH=off` says so on purpose | [Security](08-crosscutting-concepts.md#security) |
| Provider portability | The model is switched to Ollama | The same tools and skills work; the ledger records it | [Models](../MODELS.md) |

Budgets that warn, ask or stop are planned, not built ([F7](../features/F07-budgets.md)).
