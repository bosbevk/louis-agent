# Goal

**Become an AI engineer by building louis-agent: learn each AI-engineering skill hands-on, measure what it changes,
and be able to explain every design decision behind it.**

This is the one fixed aim. The plan to get there changes as I learn: the [TODO](../TODO.md) says *what's next*,
[features](features/README.md) say *how* and [specs](specs/) say *why*, and their targets live there, not here. This page
changes only when what I want to learn changes. (The same text, as plain text, is used as the coding coordinator's goal.)

## What I'm learning

Each skill is learned by shipping something in louis-agent that needs it. The *Proven by* column names the work that
currently covers it; when the TODO changes, update that column, not the skill.

| Skill | Proven by (today) |
|---|---|
| Token economics and observability | M1: usage ledger, prices, cost per answer and per fix |
| Prompt and cache design | M2: prompt caching, toolset profiles |
| Resilient LLM systems, estimates and budgets | M3: estimates, budgets, retries and fallback |
| Context engineering | M4: bounded history, settings per route |
| Cost reporting and reconciliation | M5: Usage tab, comparison with the provider's bill |
| Multi-agent orchestration, specs as runbooks | The orchestrator fix demo and the build-mode POC |
| Testing and shipping AI systems | CI and automated tests for the API, web app and orchestrator loop |

A skill can be added, dropped or reworded at any time; that is a change to this page.

## A skill counts as learned when

1. **It's built.** The work that covers it is shipped and meets its *Done when* in the feature doc or TODO.
2. **It's measured.** Before and after numbers on the benchmark (see [features](features/README.md#the-benchmark)),
   with quality held: the targets themselves are set in the feature docs.
3. **It's written up.** A short write-up in [docs/learning/](learning/): what was measured before and after, what
   changed, the trade-offs, and what didn't work. Together the write-ups form a portfolio of AI-engineering practice.
4. **I can explain it.** The write-up answers *why this design and not the alternatives*, without needing the code open.

## How to work

- **Source of truth:** [TODO.md](../TODO.md) (what's next), [docs/features](features/README.md) (how),
  [docs/specs](specs/) (why). Don't duplicate them here; update them.
- **Learning over finishing:** when a choice is between done faster and understood better, prefer understood better,
  and say what was learned in the write-up.
- **One feature at a time**, in the TODO's order; one implementation step per commit, each with its test. Keep the
  existing suites green.
- **Measure, don't assume:** rerun the benchmark for every change that affects cost or quality, keep a change only if
  quality holds, and record the numbers.
- **Keep everything in step** when a feature finishes: follow
  [Finishing a feature](features/README.md#finishing-a-feature).
- **Ask first** before anything that spends real money (benchmark or demo runs against the API), before pushing, and
  on open design decisions listed in the TODO.
- **Provider-neutral core:** keep provider-specific code in `LlmClientFactory`.
- **Stop and report** rather than guess when a result can't be verified.

---
[Docs index](README.md) · [TODO](../TODO.md) · [Features](features/README.md)
