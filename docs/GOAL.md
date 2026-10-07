# Goal

**Move from Software Engineer to AI Engineer by turning louis-agent into a production-grade, cost-aware agent platform,
and being able to explain and measure every design decision behind it.**

This is the outcome the work in the [TODO](../TODO.md) adds up to. The TODO says *what's next*,
[features](features/README.md) say *how*, and [specs](specs/) say *why*; this page says *what it's all for* and when
it's done. (The same text, as plain text, is used as the coding coordinator's goal.)

## Done when all of this is true

### 1. The TODO's *Now* plan is shipped

Features F1–F11 in milestone order, each meeting its *Done when* and closed with the
[Finishing a feature](features/README.md#finishing-a-feature) checklist.

| Milestone | Outcome |
|---|---|
| **M1 — See it** (F1–F3) | Every model request's tokens and cost recorded; cost shown per answer and per orchestrator fix; a baseline benchmark recorded |
| **M2 — Cheaper** (F4–F5) | Prompt caching and toolset profiles cut the demo's input cost by **at least 60%** against the baseline, with **10/10 fixes** still confirmed |
| **M3 — In control** (F6–F8) | Runs estimated before they start; budgets warn, ask or stop cleanly; account limits stop a run at once; transient errors retry; results between agents arrive as tool calls |
| **M4 — Long sessions** (F9–F10) | A 30-turn chat stays under the compaction threshold and still recalls its first turns; task sessions need fewer "continue" messages |
| **M5 — Reporting** (F11) | A Usage tab; the ledger within **5%** of the provider's report where an admin key exists |

### 2. Build mode works

The [build-mode POC](../TODO.md#next-build-mode-poc--build-a-whole-microservice-from-runbooks) runs as a new, separate
sample: a microservice built method by method from runbooks, one verified branch per method, merged by a person. The
existing fix demo still works unchanged.

### 3. Quality is automatic

CI runs build and tests on every push; the API, the web app and the orchestrator loop have automated tests (TODO
backlog: *Quality and CI*).

### 4. The learning is visible

For each milestone, a short write-up in the repository: what was measured before and after, what changed, the
trade-offs, and what didn't work. Together they form a portfolio of AI-engineering practice:

| Milestone | Skill it demonstrates |
|---|---|
| M1 | Token economics and observability |
| M2 | Prompt and cache design |
| M3 | Resilient LLM systems, estimates and budgets |
| M4 | Context engineering |
| M5 | Cost reporting and reconciliation |
| Build mode | Multi-agent orchestration, specs as runbooks |

## How to work

- **Source of truth:** [TODO.md](../TODO.md) (what's next), [docs/features](features/README.md) (how),
  [docs/specs](specs/) (why). Don't duplicate them; update them.
- **One feature at a time**, in milestone order; one implementation step per commit, each with its test. Keep the
  existing suites green.
- **Measure, don't assume:** rerun the benchmark (the orchestrator demo plus the scripted long chat) for every change
  that affects cost. Keep a change only if quality holds (10/10 fixes, recall passes) and record the numbers.
- **Keep everything in step** when a feature finishes: the TODO tick and *Done* entry, the feature's status, the spec's
  implementation map, known limitations (TODO and [docs/CLAUDE.md](CLAUDE.md#known-limitations)), and the wiki's
  *Roadmap* page.
- **Ask first** before anything that spends real money (benchmark or demo runs against the API), before pushing, and on
  the open decisions: the orchestrator's tier (`louis-agent.core` vs Tool Runner), and reusing the orchestrator for
  build mode vs a new one.
- **Provider-neutral core:** keep provider-specific code in `LlmClientFactory`.
- **Stop and report** rather than guess when a result can't be verified.

---
[Docs index](README.md) · [TODO](../TODO.md) · [Features](features/README.md)
