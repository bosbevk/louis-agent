# Sample output of the orchestrator demo

What one clean run of `samples/run-demo.ps1` produced (in Docker, model `claude-haiku-5-5`, 2026-10-10, run id
`run-20261010-174312`): the 13 production errors became **10 fixed, 2 ignored and 1 escalated**, giving 10 fix branches
with one commit each and nothing pushed. It took about 8½ minutes. Every model request of both agents is in the usage
ledger, [usage-2026-10.jsonl](usage-2026-10.jsonl). A run writes these files to `.demo/` (git-ignored); they are copied
here so you can see the result without running it. Times in the files are UTC.

![Merging the ten fix branches in the web app's Branches tab](merge_10_fix_branches.gif)

*Merging the ten fix branches in the web app (http://127.0.0.1:5081 → Branches), recorded on an earlier run (2026-10-06,
Claude Haiku 4.5) with the same ten bugs: opening a branch shows its commit and diff (the code fix and louis-agent's
regression test); **Merge into main** makes a merge commit, and the branch moves from "To merge" to "Merged".*

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other: per error, each message the orchestrator sent louis-agent (verbatim), each reply (tool calls, text, `FIX-RESULT`), the orchestrator's own checks and a summary. The **run summary** table is at the end. |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one JSON line per model request of both agents, with token counts, model, duration, and who it was for (host, session, turn, purpose, `fix:<error id>`, run). Counts and ids only, no prompt text. |
| [branches.txt](branches.txt) | The demo repository before merging: `git log --all --graph` (10 `fix/...` branches, each one commit from `main`) and the files each branch changed. |
| [merge_10_fix_branches.gif](merge_10_fix_branches.gif) | A recording of the ten merges in the web app (above; from the earlier run). |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log (`.demo/logs/agent-api-*.log`): every tool call it made, with arguments and results. |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the orchestrator's decision and why. |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix: status, commit, branch, tests, its closing words. |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The error handed to a human team (`refund 1005` → payments). |
| [orchestrator-state/acknowledged.jsonl](orchestrator-state/acknowledged.jsonl) | The errors the runbooks list as expected (a duplicate refund, a 404). |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged, so a resumed run skips them. |

## What it cost: the usage ledger

Totals from [usage-2026-10.jsonl](usage-2026-10.jsonl), priced by hand at Claude Haiku 5.5's rates ($0.10 input, $0.50
output per million tokens; [F2](../../docs/features/F02-prices-and-cost.md) will price records itself). Each fix's
records carry `task: fix:<error id>`, each triage's the `triage_<error id>` session, and every record the run id.

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix (louis-agent) | 9–14 | 0.47M–0.77M | 2,300–4,300 | $0.049–0.079 (about $0.062 on average) |
| All 10 fixes | 113 | 6,068,107 | 32,187 | $0.62 |
| All 13 triages (orchestrator) | 36 | 293,462 | 9,374 | $0.03 |
| **The run** | **149** | **6,361,569** | **41,561** | **≈ $0.66** |

- **Seven fixes took one message, three took two** (`shipping-cost`, `vat`, `delivery-estimate`): they used all 10 tool
  rounds of the first message, and the orchestrator asked louis-agent to continue. The engine keeps the tools on the
  request after the limit (`tool_choice: none`), which Haiku 5.5 requires; without it those three fixes fail with a 400.
- **Nearly all input is the same prompt sent again.** Nothing is cached yet
  ([F4](../../docs/features/F04-prompt-caching.md)), so every request re-sends the ~48,000-token system prompt and
  tools. The largest request was 58,315 tokens, under Haiku 5.5's 100,000-token price step.
- `reasoning` is null throughout: Claude's thinking is counted inside `output`.

## After merging

All ten branches merge without conflicts (checked in a copy of the demo repository, merging each with `--no-ff` as the
web app does). On the merged `main` the test suite passes 27/27 (14 original tests plus louis-agent's regression
tests), and every request that crashed now answers `200`:

| Request | Before | After |
|---|---|---|
| `get-order 1006` | `ArgumentOutOfRangeException` | `Order 1006 for Barbara Liskov: 0 item(s)` |
| `order-total 1003` | `KeyNotFoundException` | `35.00` (retired code, no discount) |
| `shipping-cost 1006` | `DivideByZeroException` | `0.00` |
| `invoice-number 1007` | `FormatException` | `INV-202610-1007` |
| `packing-slip 1001` | `NullReferenceException` | `2 x BOOK-1; 1 x PEN-3` |
| `loyalty-points 1008` | `OverflowException` | `2500000` |
| `vat 1009` | `KeyNotFoundException` | `2.78` |
| `delivery-estimate 1010` | `IndexOutOfRangeException` | `2026-10-07` |
| `discount-label 1011` | `FormatException` | `FREESHIP` |
| `customer-initials 1012` | `IndexOutOfRangeException` | `C` |

Your own run will differ in error ids, commit hashes, wording and token counts: the model writes each fix and reply anew.
A quicker five-error run is in [sample-output-short/](../sample-output-short/README.md).

---
[Docs index](../../docs/README.md) · Previous: [Runbooks](../../docs/RUNBOOKS.md)