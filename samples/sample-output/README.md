# Sample output of the orchestrator demo

What one clean run of `samples/run-demo.ps1` produced (in Docker, model `claude-haiku-4-5-20251001`, 2026-10-06): the
13 production errors became **10 fixed, 2 ignored and 1 escalated**, giving 10 fix branches with one commit each and
nothing pushed. A run writes these files to `.demo/` (git-ignored); they are copied here so you can see the result
without running it. Times in the files are UTC.

| File | What it is |
|---|---|
| [agent-comms.md](agent-comms.md) | Everything the agents said to each other: per error, each message the orchestrator sent louis-agent (verbatim), each reply (tool calls, text, `FIX-RESULT`), the orchestrator's own checks and a summary. The **run summary** table is at the end. |
| [branches.txt](branches.txt) | The demo repository afterwards: `git log --all --graph` (10 `fix/...` branches, each one commit from `main`) and the files each branch changed. |
| [louis-agent-api-log.txt](louis-agent-api-log.txt) | louis-agent.api's log (`.demo/logs/agent-api-*.log`): every tool call it made, with arguments and results. |
| [orchestrator-state/decisions.jsonl](orchestrator-state/decisions.jsonl) | One line per error: the orchestrator's decision and why. |
| [orchestrator-state/fixes.jsonl](orchestrator-state/fixes.jsonl) | louis-agent's result for each fix: status, commit, branch, tests, its closing words. |
| [orchestrator-state/escalations.jsonl](orchestrator-state/escalations.jsonl) | The error handed to a human team (`refund 1005` → payments). |
| [orchestrator-state/acknowledged.jsonl](orchestrator-state/acknowledged.jsonl) | The errors the runbooks list as expected (a duplicate refund, a 404). |
| [orchestrator-state/processed.txt](orchestrator-state/processed.txt) | Error ids already triaged, so a resumed run skips them. |

All ten branches were checked to merge together without conflicts; the merged test suite passed 34/34 (14 original
tests plus louis-agent's regression tests).

Your own run will differ in error ids, commit hashes and wording: the model writes each fix and reply anew.
