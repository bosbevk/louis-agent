# F18 · Simulations in C#: one harness for checks, regressions and demos

> **Status:** planned (built on `feature/F18-simulations`) · **Phase:** 3 of [the platform plan](../plans/platform.md) ·
> **Plan:** [F18-simulations.md](../plans/F18-simulations.md) · **Depends on:** F1, F2 and Phase 2 (CL15)
>
> **In the TODO:** **F18 Simulations in C#** under *Now*, which also takes in the CI gate · **Decided in:**
> [ADR 0029](../adr/0029-simulations-in-csharp.md) · **Replaces:** the earlier *F18 Evaluation* design (an
> `evals/` folder of cases with `exact`, `contains` and `judge` scorers, run from a CLI, built on F16 and F17) and the
> backlog's *Evaluation* gap

Today the only quality check is the orchestrator demo: ten fixes, each verified, at about $0.66 a run. It says whether
the whole system still works, but not whether one part (a summary, a triage decision, a cook's price list) is right, and
it's too slow and costly to run for every prompt change. Puls's checks are Python scripts (`benchmarks/`), the session
stream is read four times, and the demos we sell with have no harness at all. F18 replaces all three with one C# harness
(`simulations/`): every check, regression and demo is a **simulation** of a real task, and each simulation sits beside
the identity or pack it proves.

## The words, from the field

Anthropic's *Demystifying evals for AI agents* (9 Jan 2026) gives the words we use. Checked 2026-10-18.

- **Task:** a scenario, its made-up data and its **success criteria**. A simulation is a task that runs.
- **Trial:** one run of a task. Each trial leaves a **transcript** (the session's events, tool calls included, each
  turn's time, and any infrastructure failure) and ends in an **outcome**.
- **Grader:** scores an outcome. Code graders first (fixed rules, free and repeatable); a model grader only where code
  can't judge, calibrated against Louis's own labels first.
- **pass@k** (passed at least once in k trials) and **pass^k** (passed every time in k). A sold agent needs pass^k.
- **Mode:** our own addition. A task runs in one or more modes (Puls alone, or Puls with its cook).

## User stories

**F18-S1 — A simulation is a task.** As a *developer*, I want each check and demo written as a task with success
criteria, so that a new one is a class, not a script.
- *Given* a simulation in `simulations/louis-agent.simulations.{identity-or-pack}/` *then* the runner lists it by name
  and runs its trials; a name listed twice is refused.

**F18-S2 — One runner, one stream reader.** As a *developer*, I want one runner and one reader for the session stream, so
that a new event is learnt once.
- *Given* `dotnet /app/simulations/louis-agent.simulation.runner.dll simulate {name} --repeat N` *then* each trial runs
  and writes the existing version-1 protocol lines, which the API stores as it does today.
- *And* `--repeat` gives pass@k and pass^k per mode and task; a trial that failed for the infrastructure (a timeout, a
  dead container, a replay miss) is reported apart and counts in neither.

**F18-S3 — Graders that can be checked.** As a *developer*, I want graders that score the same outcome the same way every
time, so that a change in the score is a change in the agent.
- *Given* a code grader *then* it scores with no model call, and its scores match the Python checks' key for key.
- *Given* a model grader *then* it runs only after it agrees with Louis's labels on at least 9 of 10 stored answers, and
  its cost is in the ledger.
- *And* a grader change re-scores stored runs for $0 (`--rescore {runId}`), and the old scores stay beside the new.

**F18-S4 — Cost from the ledger, and frozen input.** As an *operator*, I want each run's cost from the usage ledger and
the same web input on every re-run, so that a difference comes from our change and not from the websites.
- *Then* every model call is a ledger row with the run's tag, and the report's cost equals the ledger's.
- *Given* `--web record` *then* the pages, searches and fetches a run reads are saved once per hash, and
  `--replay {runId}` answers from them for $0 through the replay cache. Saved pages are never served and never committed.

## Design

The full design, steps, namespace map and diagrams are in [the plan](../plans/F18-simulations.md). In short:

- **The harness** is `simulations/louis-agent.simulation/`, sold with the engine, and names no identity.
- **Simulations** live beside what they prove: `simulations/louis-agent.simulations.puls/` (the cook and routing) and
  later the demo and voorman ones. Puls's scoring rules stay out of the harness.
- **The protocol stays.** F31's version-1 line protocol is the seam; the C# runner writes the same lines, and what it adds
  is additive, so every stored run still reads. The API starts the C# runner instead of `python3`.
- **Engine changes** (steps 4a to 4f of the plan): the prompt's hash on every ledger row, traces per turn and tool call,
  a replay cache for recorded simulations only, the web input recorded and replayed, and personal data out of the logs
  (`LOG_CONTENT`, hashed by default).
- **The word:** the public word becomes *simulation* in the way CL14 changed *role*: the new word is written, the old
  routes and names still answer until a TODO line removes them.
- **Packages:** Microsoft.Extensions.AI.Evaluation (MIT), pinned to 10.10.0 to match Microsoft.Extensions.AI. Its grader
  uses `IEvaluator.EvaluateAsync` directly, so no result store is needed.

## Implementation steps

The steps are the plan's: step 0 (this doc and the package pin, docs only), 1a to 1d (one session-stream reader), 2 to 2a
(the harness and the protocol in C#), 3 (code graders), 4 to 4f (runner, web replay, prompt hash, traces, replay cache,
logs), 5 (the cook and routing simulations), 5a to 5b (pass@k and re-scoring), 6 (a calibrated model grader), 7 (Python
leaves), 7a (notes on a run) and 8 (docs and the two learning write-ups).

## Done when

- The plan's steps pass their tests; the golden files are unchanged, or reviewed.
- Puls's cook and routing simulations score as the Python checks did, on recorded transcripts; one paid run of each, on
  Louis's go, is recorded beside the last Python runs, and a free replay of each costs $0.
- Louis has read the two learning write-ups (skill 12 and skill 13, with traces and the prompt hash).
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): move **F18** to the CHANGELOG, set
  *Status* to done here and in the features table, and update skill 12 in the goal map.

---
[Features](README.md) · Previous: [F17 Orchestrator agent](F17-orchestrator-agent.md)
