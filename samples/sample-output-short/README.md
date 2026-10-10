# Sample output: the short demo on Claude Haiku 5.5, with the usage ledger

What one clean run of `samples/run-demo.ps1 -Short` produced (in Docker, model `claude-haiku-5-5`, 2026-10-10, run id
`run-20261010-171318`): five production errors became **4 fixed and 1 escalated**, four fix branches with one commit
each, nothing pushed. Every model request of both agents is in the **usage ledger** (F1),
[usage-2026-10.jsonl](usage-2026-10.jsonl). The full thirteen-error run, also on Haiku 5.5 with its ledger, is in
[sample-output/](../sample-output/README.md). Times are UTC.

| # | Request | Decision | louis-agent |
|---|---|---|---|
| 1 | `order-total 1003` | **fixed** | 1 message, 10 model requests |
| 2 | `shipping-cost 1006` | **fixed** | 1 message, 10 requests |
| 3 | `packing-slip 1001` | **fixed** | 1 message, 11 requests: it used all 10 tool rounds and reported on the final request |
| 4 | `vat 1009` | **fixed** | 2 messages, 16 requests: it hit the round limit, and the orchestrator asked it to continue |
| 5 | `refund 1005` | escalated | correct: refunds move money and are never auto-fixed (handed to payments) |

Every fix was confirmed by the orchestrator: the commit is on its branch and touches the right files, the crashing
request now answers `200`, and the service's tests pass.

## The round limit on Haiku 5.5

Fixes 3 and 4 used all 10 tool rounds of a message. After the limit, `FunctionInvokingChatClient` sends one last
request without tools; Haiku 5.5 rejects a changed tool list once its thinking blocks are in the history, so the first
run of this demo (commit `57833e5`) lost both of those fixes to a 400. The engine now puts the tools back on that request
with `tool_choice: none` (commit `d9c7c45`), and the ledger shows the result: fix 3's eleventh request and fix 4's
follow-up message both went through.

## What the ledger shows

Totals from [usage-2026-10.jsonl](usage-2026-10.jsonl), priced by hand at Haiku 5.5's rates ($0.10 input, $0.50 output
per million tokens; F2 will price records itself). Fix records carry `task: fix:<error id>`; triage records carry the
`triage_<error id>` session; every record carries the run id and `service: order-service`.

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix in one message (louis-agent, host `api`) | 10–11 | 525,000–584,000 | 2,800–3,600 | $0.054–0.060 |
| The fix that needed a follow-up (`vat`) | 16 | 900,494 | 4,521 | $0.092 |
| A triage (orchestrator, `purpose: triage`) | 2–3 | 15,000–25,000 | 550–920 | $0.002–0.003 |
| **The run** (5 errors) | **61** | **2,656,923** | **17,515** | **≈ $0.27** |

Nothing is cached yet ([F4](../../docs/features/F04-prompt-caching.md)): every request re-sends the ~48,000-token
system prompt and tools, which is nearly all of the input. The largest request was 60,064 tokens, under Haiku 5.5's
100,000-token price step. `reasoning` is null throughout: Claude's thinking is counted inside `output`.

## Files

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other, per error, and the run summary at the end |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one line per model request of both agents (counts and ids only, no prompt text) |
| [branches.txt](branches.txt) | The demo repository after the run: `git log --all --graph` and each fix branch's changed files |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log: every tool call it made, with arguments and results |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the decision and why |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The refund error handed to payments |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged |

Your own run will differ in error ids, hashes, wording and token counts: the model writes each fix and reply anew.

---
[Orchestrator demo](../README.md) · [Full run](../sample-output/README.md)
