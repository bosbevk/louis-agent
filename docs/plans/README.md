# Plans: how each feature or fix is going to be built

A feature doc ([features/](../features/README.md)) or a fix doc ([fixes/](../fixes/README.md)) says *what* and *why*.
A plan says **how we'll build it now**: written and approved before the first step, with what it changes from the doc
(the code has usually moved on since the doc was written), the steps, and how the result is checked. Plans are kept as
they were approved, so the reasoning at the time stays readable; what the build then changed is recorded in the feature
or fix doc (*What the build changed*), not here.

## Plans

| Plan | For | Approved | Notes |
|---|---|---|---|
| [F01-usage-ledger.md](F01-usage-ledger.md) | [F1 Usage ledger](../features/F01-usage-ledger.md) | 2026-10-10 | Recreated on 2026-10-11 from the plans approved in the session |
| [F02-prices-and-cost.md](F02-prices-and-cost.md) | [F2 Prices and cost](../features/F02-prices-and-cost.md) | 2026-10-10 | Recreated on 2026-10-11; includes the `PRICES_URL` addition approved during the build |
| [F03-usage-display.md](F03-usage-display.md) | [F3 Show usage](../features/F03-usage-display.md) | 2026-10-11 | |
| [F14-display-currency.md](F14-display-currency.md) | [F14 Display currency](../features/F14-display-currency.md) | 2026-10-11 | Built before M2 by choice |
| [CL3-messages-and-logging.md](CL3-messages-and-logging.md) | TODO: *Messages and logging* (cleanup) | 2026-10-12 | Serilog behind `ILogger`, not NLog; tool texts pinned by a golden file; the comms log unchanged |
| [CL2-split-agent-engine.md](CL2-split-agent-engine.md) | TODO: *Split `AgentEngine`* (cleanup) | 2026-10-11 | Classes with the engine as façade; console UI to the CLI; pure checks in `Guardrails/` |
| [CL1-project-layout.md](CL1-project-layout.md) | TODO: *Project layout* (cleanup) | 2026-10-11 | PascalCase in every project, one type per file; `AgentEngine` moved whole, its split next |
| [F04-prompt-caching.md](F04-prompt-caching.md) | [F4 Prompt caching](../features/F04-prompt-caching.md) | 2026-10-11 | Conversation marker by automatic caching (option B); orchestrator and summariser left to their own features |

## Conventions

- **One file per plan**, named like what it plans: `Fnn-name.md` for a feature, `FXnn-name.md` for a fix, `CLn-name.md` for a
  cleanup (a TODO line with no feature or fix doc).
- **Sections:** *Context* (why now, what the doc says), *Design* (what changes from the doc, and why), *Steps* (one
  commit each, with its test; the review stop after each), *Verification* (tests, free manual checks, and any paid run,
  which needs approval first), and the *Concepts* each step teaches, with their skill in the [goal](../GOAL.md).
- **Saved and committed before step 1**, on the feature's branch, so the plan is in the same PR as the work.
- **Changes during the build:** a change that is asked for and approved during the build (F2's `PRICES_URL`) is added to
  the plan under *Approved during the build*, with the date; everything else the build learns goes in the feature or fix
  doc.

---
[Docs index](../README.md) · Related: [Features](../features/README.md) · [Fixes](../fixes/README.md)