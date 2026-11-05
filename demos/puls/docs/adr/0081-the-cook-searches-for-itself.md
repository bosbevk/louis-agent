# ADR 0081: The cook searches for itself, and a minion has to earn its place

> **Explanation.** Why Puls's cook uses no minion for its recipe search, and what a minion must show to come back.

## Context

A minion is a small agent on a cheap or free model that does one job for a specialist. We gave the cook one to search the
recipe sites, on the idea that a free local model would cut the bill. The test was set before the run: **the same accuracy
and the same time for less cost**, measured against no minion.

Both runs used the recipes eval, 3 repeats, on the same commit (`fda192dd`):

| | No minion (`79ea99b3ec08`) | Local minion (`713d617be18b`) |
|---|---|---|
| Judge | 27 of 30 | 27 of 30 |
| Recipe from its source | 30 of 30 | 30 of 30 |
| Time per answer | 52 s | 60 s |
| Cost per answer | $0.00587 | $0.00626 |

The local model is free, and the cook made slightly fewer paid requests, but it wrote more, so the answer cost 7% more. The
local model also took about 6.4 s per answer. A Haiku minion had already cost 7% more and taken 10 s longer than no minion.
The search was only about 7% of an answer's cost, so even a perfect free minion had little to save.

## Decision

**Puls's cook calls `FindRecipes` itself.** Its role file no longer asks for the `minions` toolset, and Puls's compose and
Azure app set no `MINION_LLMS`.

**A minion stays only if it beats no minion on the test above.** Run both on one commit and compare them. A cheaper model
doesn't count as a saving until the whole answer costs less.

The minions feature stays in the engine ([ADR 0021](../../../../src/docs/adr/0021-minions-one-task-one-tool.md)), and a minion
still always runs a local model. The `puls-minion-find-recipe` role file stays too, doing nothing unless an instance sets
`MINION_LLMS` and a role asks for it, as `puls-minion-web-price-lookup` already does.

## Consequences

- Answers are 8 s quicker and 6% cheaper than with the minion, at the same accuracy.
- Azure no longer needs a local model before the next deploy, which removes that blocker.
- We lose a small variety gain: 14 sites against 13, and 4 repeated pages against 5. The biggest site went the other way, 17%
  with the minion and 13% without. All of these are too small to count on.
- A 7% cost gap from one run could be partly chance. Two minions failing the same way is why we didn't repeat it.

## Links

- [ADR 0021: minions, one task, one tool](../../../../src/docs/adr/0021-minions-one-task-one-tool.md)
- [ADR 0078: a recipe search asks several sites](../../../../packs/docs/adr/0078-a-recipe-search-asks-several-sites.md)
- [The F35 plan](../plans/F35-recipe-sources.md), *Approved during the build*

---
[Docs index](../../../../src/docs/README.md) · Related: [ADR index](../../../../src/docs/adr/README.md)
