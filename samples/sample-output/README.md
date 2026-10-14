# Sample output of the orchestrator demo

What one run of `samples/run-demo.ps1` produced (in Docker, model `claude-haiku-5-5` for both agents, 2026-10-11, run
id `run-20261011-083646`): the 13 production errors became **10 fixed, 2 ignored and 1 escalated**, giving 10 fix
branches with one commit each and nothing pushed. It took about 9 minutes and cost **$0.7157, shown as R11.83** in the
comms log (F14: costs in the PC's currency, at 16.5331 ZAR/USD on 2026-10-09). Every model request of both agents is
in the usage ledger, [usage-2026-10.jsonl](usage-2026-10.jsonl), with its cost in USD, as charged. A run writes these
files to `.demo/` (git-ignored); they are copied here so you can see the result without running it. Times in the files
are UTC.

**This run found [FX8](../../docs/fixes/FX08-fix-result-quotes.md)**, and is kept as it was. Fix 10
(`customer-initials 1012`, the customer "Cher") was made and verified, but louis-agent's `FIX-RESULT` quoted the name
without escaping it, so the orchestrator couldn't read the line: the comms log shows that fix as `no result (4 msg)`,
lists 9 branches ready to merge instead of 10, and the fix cost $0.0999 (R1.70 with its triage) after three needless
"continue" messages. The branch, `fix/customer-initials-9c8637c0128d`, is in [branches.txt](branches.txt). FX8 now
reads such a line; the [short sample](../sample-output-short/README.md) was run after it.

![Merging the ten fix branches in the web app's Branches tab](merge_10_fix_branches.gif)

*Merging the ten fix branches in the web app (http://127.0.0.1:5081 → Branches), recorded on an earlier run (2026-10-06,
Claude Haiku 4.5) with the same ten bugs: opening a branch shows its commit and diff (the code fix and louis-agent's
regression test); **Merge into main** makes a merge commit, and the branch moves from "To merge" to "Merged".*

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other: per error, each message the orchestrator sent louis-agent (verbatim), each reply (tool calls, text, `FIX-RESULT`, usage), the orchestrator's own checks and a summary with the error's cost. The **run summary** table, in rands with the rate and the dollars charged, is at the end. |
| [usage-2026-10.jsonl](usage-2026-10.jsonl) | The usage ledger: one JSON line per model request of both agents, with token counts, cost in USD, model, duration, and who it was for (host, session, turn, purpose, `fix:<error id>`, run). Counts and ids only, no prompt text. |
| [branches.txt](branches.txt) | The demo repository before merging: `git log --all --graph` (10 `fix/...` branches, each one commit from `main`) and the files each branch changed. |
| [merge_10_fix_branches.gif](merge_10_fix_branches.gif) | A recording of the ten merges in the web app (above; from the earlier run). |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log (`.demo/logs/agent-api-*.log`): every tool call it made, with arguments and results. |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the orchestrator's decision, why, and its usage in USD (total, triage, louis-agent). |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix: status, commit, branch, tests, its closing words, usage. |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The error handed to a human team (`refund 1005` → payments). |
| [orchestrator-state/acknowledged.jsonl](orchestrator-state/acknowledged.jsonl) | The errors the runbooks list as expected (a duplicate refund, a 404). |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged, so a resumed run skips them. |

## What it cost

The end of [agent-comms.md](agent-comms.md):

```
**Run total:** 160 requests · 7M tokens · R11.83 (triage R0.56, louis-agent R11.27).

Shown in ZAR at 16.5331 ZAR/USD on 2026-10-09; charged $0.72 (the ledger, fixes.jsonl and decisions.jsonl keep USD).
```

The orchestrator's own figures (`decisions.jsonl`) and the ledger agree exactly for the run: **$0.715704**, 160
requests. Totals from the ledger, with
`dotnet run --project tools/louis-agent.usage-probe -- --ledger samples/sample-output/usage-2026-10.jsonl`:

| Work | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|
| A fix (louis-agent): fewest / average / most | 9 / 12.4 / 18 | 469,655 / 664,146 / 972,577 | 2,547 / 3,520 / 5,277 | $0.0486 / $0.0682 / $0.0999 |
| All 10 fixes | 124 | 6,641,460 | 35,204 | $0.6818 |
| All 13 triages (orchestrator) | 36 | 294,103 | 9,087 | $0.0340 |
| **The run** | **160** | **6,935,563** | **44,291** | **$0.7157** |

- **Against the M1 baseline** ($0.6586, 150 requests, `run-20261010-193730`): 8.7% more, 10 more requests. Most of it is
  fix 10's three needless follow-ups (FX8); the rest is the usual variation in how many tool rounds a fix takes. The
  quality held: 10/10 fixes confirmed, the other three handled as their runbooks say.
- **Five fixes took one message, four took two**, and fix 10 took four (FX8). A fix that uses its 10 tool rounds
  continues in a second message, which costs about 1.5× a one-message fix.
- **Nearly all input is the same prompt sent again.** Nothing is cached yet
  ([F4](../../docs/features/F04-prompt-caching.md)), so every request re-sends the ~48,000-token system prompt and
  tools. The largest request was 58,556 tokens, under Haiku 5.5's 100,000-token price step.
- `reasoning` is null throughout: Claude's thinking is counted inside `output`.

## After merging

Checked on the previous run of the same ten bugs (2026-10-10), whose branches made the same fixes: all ten merge
without conflicts (merging each with `--no-ff`, as the web app does), the test suite passes 31/31 on the merged `main`
(14 original tests plus louis-agent's regression tests), and every request that crashed answers `200`:

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

In this run each branch was checked by the orchestrator itself (the commit, the replayed request answering `200`, the
whole test suite); the merges weren't repeated.

Your own run will differ in error ids, commit hashes, wording and token counts: the model writes each fix and reply anew.
A quicker five-error run is in [sample-output-short/](../sample-output-short/README.md).

---
[Docs index](../../docs/README.md) · Previous: [Runbooks](../../docs/projects/orchestrator-runbooks.md)