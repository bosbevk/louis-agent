# Learning write-ups

One short write-up per skill in the [goal](../GOAL.md)'s architecture map: what was measured before and after, what
changed, the trade-offs, and what didn't work. Name each file after the skill's number and name, e.g.
`13-cost-and-token-economics.md`.

| Skill | Write-up | Covers |
|---|---|---|
| 4 · Tool design | [04-tool-design.md](04-tool-design.md) | CL15: a capability as a pack with a contract (`IAgentPack`), tool names from one catalogue, every pack kept convertible to MCP by a test over shapes (`PackConvertibilityTests`), what the goldens showed. By Claude, read by Louis |
| 7 · Context engineering | [07-context-engineering.md](07-context-engineering.md) | F32–F34: a shop's prices read from its catalogue in code as a short table, not pages read by the model; sourced prices 0 of 32 → 19 of 23 → 31 of 31 rows right; with skill 12: a scorer that counts each way of being wrong, a control with the same tools. By Claude, read by Louis |
| 10 · Multi-agent orchestration | [10-multi-agent-orchestration.md](10-multi-agent-orchestration.md) | Puls: a router, roles as personalities (F21), tools per role (F22), sub-sessions without the router, the Werk panel (F20); prompt ~28.6k → ~2.2k tokens. Draft for Louis |
| 11 · Protocols | [11-protocols.md](11-protocols.md) | CL20: an API is a contract: a version in the path (and why in the code, not a setting), resources as nouns, `201` and `Location`, closed by default, the route table pinned and a refactor proved by an unchanged golden. By Claude, read by Louis |
| 12 · Evaluation | [12-evaluation.md](12-evaluation.md) | F18 steps 6.3 to 6a.5: a judge checked against your 36 labels in four rounds (35 → 32 → 32 → 35 of 36), what each fix taught (show the judge the whole conversation; a model can't misuse what it isn't told), the wobble of a small judge, and why the check is now a button in the portal (a process, data in, data out); a side lesson on least privilege for the runner (FX25). Extended with the rest of F18: the field's words, code graders first, pass@k against pass^k, a held-out split, frozen input and replays, the first v2 judge check on fresh blind labels (31 of 36, below the bar), the limit (a gate on recorded transcripts can't see the agent), and side lessons on traces and keyed log hashes (17), the stream format seam (4) and the prompt hash (3). By Claude, for Louis to read |
| 13 · Cost and token economics | [13-cost-and-token-economics.md](13-cost-and-token-economics.md) | M1 "See it" (F1–F3): the usage ledger, prices, showing cost, the $0.66 baseline; F4 prompt caching, measured: $0.66 → $0.13 a run with 10/10 fixes; F18's replay cache, measured: the first v2 runs $0.37 live, $0 replayed with every request a hit, and the sub-session miss. To be extended at M3 (estimates, budgets) |
| 15 · Safety and security | [15-safety-and-security.md](15-safety-and-security.md) | Phase 1: hosts fail closed without a key (FX18), git arguments as data (FX19), skill files wait for a person's approval (FX20); why a security test must fail for the hole itself. By Claude, read by Louis |
| 18 · Testing and shipping | [18-testing-and-shipping.md](18-testing-and-shipping.md) | CL15: three goldens as a refactor's proof, a composition root tested on its own, the catalogue's start-up checks, a ratchet that empties, a shared test library instead of test projects referencing each other, rules a person can maintain held by ratchets.; CL19: one folder per place, the sidecar pattern, secrets by reference, verifying the environment before the plan. By Claude, read by Louis |
| — · Every skill | [fixes-lessons.md](fixes-lessons.md) | What each fix taught (FX1 to FX27, but the open FX4), kept when the fix docs were folded (CL24): one paragraph each, with its skill. By Claude, for Louis to read |

---
[Docs index](../README.md) · [Goal](../GOAL.md)
