# Sample output: the short demo on Claude Haiku 5.5, with the priced usage ledger

What one clean run of `samples/run-demo.ps1 -Short` produced (in Docker, model `claude-haiku-5-5`, 2026-10-10, run id
`run-20261010-193216`): five production errors became **4 fixed and 1 escalated**, four fix branches with one commit
each, nothing pushed. Every model request of both agents is in the **usage ledger**,
[usage-2026-10.jsonl](usage-2026-10.jsonl), **each with its cost** (F2). The full thirteen-error run is in
[sample-output/](../sample-output/README.md). Times are UTC.

| # | Request | Decision | louis-agent |
|---|---|---|---|
| 1 | `order-total 1003` | **fixed** | 1 message, 10 model requests |
| 2 | `shipping-cost 1006` | **fixed** | 2 messages, 13 requests |
| 3 | `packing-slip 1001` | **fixed** | 2 messages, 13 requests |
| 4 | `vat 1009` | **fixed** | 2 messages, 15 requests |
| 5 | `refund 1005` | escalated | correct: refunds move money and are never auto-fixed (handed to payments) |

Every fix was confirmed by the orchestrator: the commit is on its branch and touches the right files, the crashing
request now answers `200`, and the service's tests pass. Fixes 2–4 used all 10 tool rounds of their first message and
finished in a second one; the engine keeps the tools on the request after the round limit (`tool_choice: none`), which
Haiku 5.5 requires.

## What it cost: the priced ledger

Each line of [usage-2026-10.jsonl](usage-2026-10.jsonl) carries its cost, priced as it was written:
`"cost":{"currency":"USD","amount":…,"price_table":"2026-10-10"}`. louis-agent.api priced its records from the mounted
`config/prices.json`; the orchestrator fetched the same table from the API (`PRICES_URL=http://demo-api:8080/prices`).
Totals with `dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/sample-output-short/usage-2026-10.jsonl`:

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix in one message (louis-agent, host `api`) | 10 | 521,379 | 2,852 | $0.0536 |
| A fix that needed a second message | 13–15 | 690,723–806,133 | 2,885–3,450 | $0.0705–0.0823 |
| A triage (orchestrator, `purpose: triage`) | 2–3 | 15,029–25,305 | 710–871 | $0.0019–0.0030 |
| **The run** (5 errors) | **65** | **2,838,055** | **16,445** | **$0.2920** |

Nothing is cached yet ([F4](../../docs/features/F04-prompt-caching.md)): every request re-sends the ~48,000-token
system prompt and tools, which is nearly all of the input. `reasoning` is null throughout: Claude's thinking is counted
inside `output`.

## Files

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other, per error, and the run summary at the end |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one line per model request of both agents, with tokens and cost (counts and ids only, no prompt text) |
| [branches.txt](branches.txt) | The demo repository after the run: `git log --all --graph` and each fix branch's changed files |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log: every tool call it made, with arguments and results |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the decision and why |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The refund error handed to payments |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged |

Your own run will differ in error ids, hashes, wording and token counts: the model writes each fix and reply anew.

---
[Orchestrator demo](../README.md) · [Full run](../sample-output/README.md)