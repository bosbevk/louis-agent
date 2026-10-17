# 10. Quality requirements

> **Explanation.** How the quality goals of [section 1](01-introduction-and-goals.md#quality-goals) are checked, as
> scenarios with a measure. **Elsewhere:** the runs measured before CL10 in [benchmarks at the `pre-cl10` tag](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md); the checks are being rewritten for the admin portal.

| Quality | Scenario | Measure | Where |
|---|---|---|---|
| Measured cost | A change meant to cut cost is benchmarked before it lands | The demo run's cost and tokens on the ledger, against the M1 baseline; quality must hold (10/10 fixes) | [Benchmarks](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md) |
| Measured cost | A user asks what an answer cost | Every host shows the turn's usage and cost from the same records as the ledger | [0007](../adr/0007-measure-every-request.md) |
| Measured cost | A specialist's prompt keeps growing | It stops calling tools before its next request passes `SUB_SESSION_PROMPT_BUDGET` (80k, under the long-prompt price step) | [FX14](../fixes/FX14-sub-session-prompt-budget.md) |
| Measured cost | Puls delegates a question | The delegation check prices the same questions with and without specialists | [Delegation check](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-repeat/README.md) |
| Correctness | The orchestrator asks for ten fixes | The orchestrator verifies each fix itself: 10/10 in the demo | [Orchestrator demo](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/README.md) |
| Safety | The model asks to read a secret or leave the workspace | The file tools refuse; tests cover the boundary and sensitive files | [Security](08-crosscutting-concepts.md#security) |
| Provider portability | The model is switched to Ollama | The same tools and skills work; the ledger records it | [Models](../MODELS.md) |

**Planned:** a shared evaluation harness with scorers and baselines ([F18](../features/F18-evaluation.md)), and
budgets that warn, ask or stop ([F7](../features/F07-budgets.md)).
