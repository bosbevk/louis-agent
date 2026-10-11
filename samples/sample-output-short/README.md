# Sample output: the short demo on Claude Haiku 5.5, with its cost shown

What one clean run of `samples/run-demo.ps1 -Short` produced (in Docker, model `claude-haiku-5-5` for both agents,
2026-10-11, run id `run-20261011-005339`): five production errors became **4 fixed and 1 escalated**, four fix branches
with one commit each, nothing pushed. Every model request of both agents is in the **usage ledger**,
[usage-2026-10.jsonl](usage-2026-10.jsonl), each with its cost (F2), and since F3 the comms log shows that cost per
reply, per error and for the run. The full thirteen-error run is in [sample-output/](../sample-output/README.md). Times
are UTC.

| # | Request | Decision | louis-agent | Cost (triage + fix) |
|---|---|---|---|---|
| 1 | `order-total 1003` | **fixed** | 1 message, 10 model requests | $0.058 |
| 2 | `shipping-cost 1006` | **fixed** | 1 message, 10 requests | $0.058 |
| 3 | `packing-slip 1001` | **fixed** | 1 message, 9 requests | $0.053 |
| 4 | `vat 1009` | **fixed** | 2 messages, 14 requests | $0.082 |
| 5 | `refund 1005` | escalated | correct: refunds move money and are never auto-fixed (handed to payments) | $0.0018 |

Every fix was confirmed by the orchestrator: the commit is on its branch and touches the right files, the crashing
request now answers `200`, and the service's tests pass. Fix 4 used all 10 tool rounds of its first message and finished
in a second one.

## What it cost, and the check that the views agree

The end of [agent-comms.md](agent-comms.md) (F3):

```
| # | Request | … | Tokens | Cost |
| 1 | `order-total 1003` | … | 563k | $0.058 |
…
**Run total:** 57 requests · 2.5M tokens · $0.25 (triage $0.013, louis-agent $0.24).
```

The same run from the ledger, with
`dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/sample-output-short/usage-2026-10.jsonl`:

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix in one message (louis-agent, host `api`) | 9–10 | 481,333–537,553 | 3,041–3,202 | $0.0497–0.0553 |
| The fix that needed a second message | 14 | 776,357 | 3,343 | $0.0793 |
| A triage (orchestrator, `purpose: triage`) | 2–3 | 14,820–25,525 | 598–841 | $0.0018–0.0029 |
| **The run** (5 errors) | **57** | **2,444,261** | **16,356** | **$0.2526** |

F3's check: the orchestrator's own figures (`orchestrator-state/decisions.jsonl`, the same numbers as the comms log)
add up to **exactly** the ledger's for the run: $0.252605, 57 requests, 2,444,261 input and 16,356 output tokens in
both. Against the previous short run ($0.2920, 65 requests), fewer fixes needed a second message; the cost per request
is unchanged.

Nothing is cached yet ([F4](../../docs/features/F04-prompt-caching.md)): every request re-sends the ~48,000-token
system prompt and tools, which is nearly all of the input. `reasoning` is null throughout: Claude's thinking is counted
inside `output`.

## Files

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other, per error, with each reply's and each error's usage, and the run summary with its cost at the end |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one line per model request of both agents, with tokens and cost (counts and ids only, no prompt text) |
| [branches.txt](branches.txt) | The demo repository after the run: `git log --all --graph` and each fix branch's changed files |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log: every tool call it made, with arguments and results |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the decision, why, and its usage (total, triage, louis-agent) |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix, with its usage |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The refund error handed to payments |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged |

Your own run will differ in error ids, hashes, wording and token counts: the model writes each fix and reply anew.

---
[Orchestrator demo](../README.md) · [Full run](../sample-output/README.md)