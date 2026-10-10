# Sample output: the short demo on Claude Haiku 5.5, with the usage ledger

What one clean run of `samples/run-demo.ps1 -Short` produced (in Docker, model `claude-haiku-5-5`, 2026-10-10, run id
`run-20261010-165434`): five production errors, four of them auto-fixable per the runbooks. It is the first saved run
with the **usage ledger** (F1), so every model request of both agents is in [usage-2026-10.jsonl](usage-2026-10.jsonl).
The full thirteen-error run on Haiku 4.5 is in [sample-output/](../sample-output/README.md). Times are UTC.

| # | Request | Decision | What happened |
|---|---|---|---|
| 1 | `order-total 1003` | **fixed** | louis-agent fixed it in 10 model requests; verified, replayed, tests pass |
| 2 | `shipping-cost 1006` | escalated | louis-agent committed a fix (`23d96fb`), then its turn **failed with a 400** before it could report |
| 3 | `packing-slip 1001` | **fixed** | fixed in 9 requests; verified, replayed, tests pass |
| 4 | `vat 1009` | escalated | the turn **failed with a 400** before louis-agent committed; the orchestrator stashed its work |
| 5 | `refund 1005` | escalated | correct: refunds move money and are never auto-fixed (handed to payments) |

## Why two fixes failed

Both failed fixes used all **10 tool rounds** of a message and still wanted to go on (the ledger shows 10 requests
ending in `tool_calls`). `FunctionInvokingChatClient` then sends one last request **without the tools** to get a final
answer. On Haiku 4.5 that returns a reply without a `FIX-RESULT`, and the orchestrator asks louis-agent to continue.
Haiku 5.5 binds its thinking blocks to the conversation, tool list included, and rejects that request:

```text
400 invalid_request_error: messages.1.content.0: Invalid `signature` in `thinking` block. The block is bound to a
different conversation. ... The `tools` list differs from the one this block was created with.
```

So the 10-round limit (a known limitation) turns from "needs a follow-up message" into a failed fix on Haiku 5.5. See
`louis-agent-api-log.txt` (search for `Turn failed`) and the TODO's *Models* items.

## What the ledger shows

Totals from [usage-2026-10.jsonl](usage-2026-10.jsonl), priced by hand at Haiku 5.5's rates ($0.10 input, $0.50 output
per million tokens; F2 will price records itself). Fix records carry `task: fix:<error id>`; triage records carry the
`triage_<error id>` session; every record carries the run id and `service: order-service`.

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix (louis-agent, host `api`) | 9–10 | 474,000–544,000 | 2,400–3,900 | $0.049–0.056 |
| A triage (orchestrator, `purpose: triage`) | 2–3 | 15,000–25,000 | 600–1,000 | $0.002–0.003 |
| **The run** (5 errors) | **53** | **2,193,193** | **16,880** | **≈ $0.23** |

Nothing is cached yet ([F4](../../docs/features/F04-prompt-caching.md)): every request re-sends the ~48,000-token
system prompt and tools, which is nearly all of the input. The largest request was 57,879 tokens, under Haiku 5.5's
100,000-token price step. `reasoning` is null throughout: Claude's thinking is counted inside `output`.

## Files

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other, per error, and the run summary at the end |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one line per model request of both agents (counts and ids only, no prompt text) |
| [branches.txt](branches.txt) | The demo repository after the run: `git log --all --graph` and each fix branch's changed files |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log: every tool call, and the two failed turns |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the decision and why |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix attempt |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The three errors handed to a human team |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged |

Your own run will differ in error ids, hashes, wording and token counts: the model writes each fix and reply anew.

---
[Orchestrator demo](../README.md) · [Full run on Haiku 4.5](../sample-output/README.md)