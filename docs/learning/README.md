# Learning write-ups

One short write-up per skill in the [goal](../GOAL.md)'s architecture map: what was measured before and after, what
changed, the trade-offs, and what didn't work. Name each file after the skill's number and name, e.g.
`13-cost-and-token-economics.md`.

| Skill | Write-up | Covers |
|---|---|---|
| 7 · Context engineering | [07-context-engineering.md](07-context-engineering.md) | F32–F34: a shop's prices read from its catalogue in code as a short table, not pages read by the model; sourced prices 0 of 32 → 19 of 23 → 31 of 31 rows right; with skill 12: a scorer that counts each way of being wrong, a control with the same tools. By Claude, read by Louis |
| 10 · Multi-agent orchestration | [10-multi-agent-orchestration.md](10-multi-agent-orchestration.md) | Puls: a router, roles as personalities (F21), tools per role (F22), sub-sessions without the router, the Werk panel (F20); prompt ~28.6k → ~2.2k tokens. Draft for Louis |
| 13 · Cost and token economics | [13-cost-and-token-economics.md](13-cost-and-token-economics.md) | M1 "See it" (F1–F3): the usage ledger, prices, showing cost, the $0.66 baseline; F4 prompt caching, measured: $0.66 → $0.13 a run with 10/10 fixes. To be extended at the rest of M2 and M3 (estimates, budgets) |
| 15 · Safety and security | [15-safety-and-security.md](15-safety-and-security.md) | Phase 1: hosts fail closed without a key (FX18), git arguments as data (FX19), skill files wait for a person's approval (FX20); why a security test must fail for the hole itself. By Claude, read by Louis |

---
[Docs index](../README.md) · [Goal](../GOAL.md)
