# Sample output of the orchestrator demo

What one clean run of `samples/run-demo.ps1` produced (in Docker, model `claude-haiku-4-5-20251001`, 2026-10-06): the
13 production errors became **10 fixed, 2 ignored and 1 escalated**, giving 10 fix branches with one commit each and
nothing pushed. The 10 branches were then merged into `main` in the web app. A run writes these files to `.demo/`
(git-ignored); they are copied here so you can see the result without running it. Times in the files are UTC.

![Merging the ten fix branches in the web app's Branches tab](merge_10_fix_branches.gif)

*Merging the ten fix branches in the web app (http://127.0.0.1:5081 → Branches): opening a branch shows its commit and
diff (the code fix and louis-agent's regression test); **Merge into main** makes a merge commit, and the branch moves
from "To merge" to "Merged".*

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other: per error, each message the orchestrator sent louis-agent (verbatim), each reply (tool calls, text, `FIX-RESULT`), the orchestrator's own checks and a summary. The **run summary** table is at the end. |
| [branches.txt](branches.txt) | The demo repository before merging: `git log --all --graph` (10 `fix/...` branches, each one commit from `main`) and the files each branch changed. |
| [merge_10_fix_branches.gif](merge_10_fix_branches.gif) | A recording of the ten merges in the web app (above). |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log (`.demo/logs/agent-api-*.log`): every tool call it made, with arguments and results. |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the orchestrator's decision and why. |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix: status, commit, branch, tests, its closing words. |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The error handed to a human team (`refund 1005` → payments). |
| [orchestrator-state/acknowledged.jsonl](orchestrator-state/acknowledged.jsonl) | The errors the runbooks list as expected (a duplicate refund, a 404). |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged, so a resumed run skips them. |

## After merging

All ten branches merged without conflicts (ten `--no-ff` merge commits on `main`) and were then deleted from the web
app's Merged list, leaving only `main`. On the merged `main` the test suite passes 34/34 (14 original tests plus
louis-agent's regression tests), and every request that crashed now answers `200`:

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

Your own run will differ in error ids, commit hashes and wording: the model writes each fix and reply anew.
