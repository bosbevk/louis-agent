# 10. Quality requirements

> **Explanation.** How the quality goals of [section 1](01-introduction-and-goals.md#quality-goals) are checked, as
> scenarios with a measure. **Elsewhere:** how a run is made, graded and priced in [the evals](evals.md).

![The quality requirements: measured cost, correctness, safety and provider portability, each as scenarios with a measure](diagrams/10-quality-requirements.svg)

A quality goal only counts when something checks it. So each goal below comes as a scenario, something that happens,
with the measure that says whether it held and where you can see it.

The main measure is the evals. An eval runs an identity on its scenarios through real sessions on the API, grades
each answer with its checks, and joins each run's cost from the usage ledger. The portal shows the runs and their
trend, so a change to a prompt or a tool is seen in the scores and in the money together.

| Quality | Scenario | Measure | Where |
|---|---|---|---|
| Measured cost | A change meant to cut cost is tried before it lands | The eval run's cost and tokens from the ledger, beside its checks, and the trend across runs | [The evals](evals.md) |
| Measured cost | A run is looked at again | A replay gives the recorded answers again for nothing, and is left out of the estimate and the trend | [The evals](evals.md#how-a-run-talks) |
| Measured cost | A user asks what an answer cost | Every host shows the turn's usage and cost from the same records as the ledger | [0007](../adr/0007-measure-every-request.md) |
| Measured cost | A subagent's prompt keeps growing | It stops calling tools before its next request passes `SUB_SESSION_PROMPT_BUDGET` (80k, under the long-prompt price step) | [FX14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX14-sub-session-prompt-budget.md) |
| Correctness | An identity answers its scenarios | The eval's checks pass, and a person's verdict in the run explorer can be compared with a model's check | [The evals](evals.md) |
| Correctness | The orchestrator fixes a service's error | It verifies each fix itself: it checks the commit, replays the request and runs the tests | [Runbooks](../../../demos/louis-agent/docs/projects/orchestrator-runbooks.md) |
| Safety | The model asks to read a secret or leave the workspace | The file tools refuse; tests cover the boundary and sensitive files | [Security](08-crosscutting-concepts.md#security) |
| Safety | A host on the network starts without its key | It refuses to start, unless `AGENT_AUTH=off` says so on purpose | [Security](08-crosscutting-concepts.md#security) |
| Provider portability | The model is switched to Ollama | The same tools and skills work; the ledger records it | [Models](../MODELS.md) |

Budgets that warn, ask or stop are planned, not built ([F7](../features/F07-budgets.md)).
