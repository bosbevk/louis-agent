# Sample output: the short demo on Claude Haiku 5.5, with its cost shown in rands

What one clean run of `samples/run-demo.ps1 -Short` produced (in Docker, model `claude-haiku-5-5` for both agents,
2026-10-11, run id `run-20261011-085011`, with [FX8](../../docs/fixes/FX08-fix-result-quotes.md) in place): five
production errors became **4 fixed and 1 escalated**, four fix branches with one commit each, nothing pushed. It cost
**$0.2884, shown as R4.77**: since F14 the comms log shows costs in the PC's currency (`HOST_CULTURE=en-ZA` in
`config/.env`, at 16.5331 ZAR/USD on 2026-10-09), while the usage ledger, [usage-2026-10.jsonl](usage-2026-10.jsonl), and
the jsonl files keep each cost in USD, as charged. The full thirteen-error run is in
[sample-output/](../sample-output/README.md). Times are UTC.

| # | Request | Decision | louis-agent | Cost (triage + fix) |
|---|---|---|---|---|
| 1 | `order-total 1003` | **fixed** | 2 messages, 16 model requests | R1.52 |
| 2 | `shipping-cost 1006` | **fixed** | 1 message, 11 requests | R1.04 |
| 3 | `packing-slip 1001` | **fixed** | 2 messages, 13 requests | R1.22 |
| 4 | `vat 1009` | **fixed** | 1 message, 10 requests | R0.96 |
| 5 | `refund 1005` | escalated | correct: refunds move money and are never auto-fixed (handed to payments) | R0.029 |

Every fix was confirmed by the orchestrator: the commit is on its branch and touches the right files, the crashing
request now answers `200`, and the service's tests pass. Fixes 1 and 3 used all 10 tool rounds of their first message
and finished in a second one.

## What it cost, and the check that the views agree

The end of [agent-comms.md](agent-comms.md):

```
**Run total:** 64 requests · 2.8M tokens · R4.77 (triage R0.22, louis-agent R4.55).

Shown in ZAR at 16.5331 ZAR/USD on 2026-10-09; charged $0.29 (the ledger, fixes.jsonl and decisions.jsonl keep USD).
```

The same run from the ledger, with
`dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/sample-output-short/usage-2026-10.jsonl`:

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix in one message (louis-agent, host `api`) | 10–11 | 532,538–585,657 | 3,010–3,613 | $0.0551–0.0601 |
| A fix that needed a second message | 13–16 | 694,766–870,105 | 2,922–3,689 | $0.0709–0.0889 |
| A triage (orchestrator, `purpose: triage`) | 2–3 | 14,806–25,503 | 550–873 | $0.0018–0.0030 |
| **The run** (5 errors) | **64** | **2,799,213** | **16,921** | **$0.2884** |

F3's check, in USD: the orchestrator's own figures (`orchestrator-state/decisions.jsonl`) add up to **exactly** the
ledger's for the run, $0.288381 and 64 requests. The rand figures are those USD amounts at one rate (demo-api's
`/rates`, read by both agents), so they add up too. Against the previous short run ($0.2526, 57 requests), two fixes
needed a second message instead of one; the cost per request is unchanged.

Nothing is cached yet ([F4](../../docs/features/F04-prompt-caching.md)): every request re-sends the ~48,000-token
system prompt and tools, which is nearly all of the input. `reasoning` is null throughout: Claude's thinking is counted
inside `output`.

## Files

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other, per error, with each reply's and each error's usage in rands, and the run summary with the rate and the USD charged at the end |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one line per model request of both agents, with tokens and cost in USD (counts and ids only, no prompt text) |
| [branches.txt](branches.txt) | The demo repository after the run: `git log --all --graph` and each fix branch's changed files |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log: every tool call it made, with arguments and results |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the decision, why, and its usage in USD (total, triage, louis-agent) |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix, with its usage |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The refund error handed to payments |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged |

Your own run will differ in error ids, hashes, wording and token counts: the model writes each fix and reply anew.

---
[Orchestrator demo](../README.md) · [Full run](../sample-output/README.md)