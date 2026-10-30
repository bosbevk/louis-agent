# louis-agent.evals.runner — design

> **Kind:** console app (`net10.0`) · **Path:** `evals/louis-agent.evals.runner/` · **References:**
> louis-agent.evals, louis-agent.composition (the identities' and packs' eval folders and their checks' lists,
> `EvalChecks`, reach the runner through it, ADR 0027), louis-agent.identity.puls.judges (Puls's judge, which needs the harness, so no host
> may reach it through the composition) · **Used by:** louis-agent.api (starts it as a child process; published to
> `/app/evals` in the API's images)

## Purpose

The command that runs an eval: `dotnet louis-agent.evals.runner.dll run {eval}`. It lists the evals it knows, runs
one, re-scores a stored run, writes a label sheet and checks a judge. The API never references an eval project: it
starts this runner and reads its stdout, so a new eval is a folder of files and nothing in the API
([ADR 0022](../adr/0022-benchmarks-recorded-from-the-protocol.md)).

## How it works

- **The catalogue.** Every eval is a folder of files (F49.2). `Catalog/EvalFolders` finds them: every `eval.json`
  under `EvalFiles/` beside the runner, whose path below it is where the folder sits in the repository. The runner
  builds their checks from the check catalogue it gathers (`Catalog/RunnerChecks`: the general checks, then Puls's
  fixed rules `PulsChecks.All`, then its judges `PulsJudges.All`). `EVALS=` then keeps the evals of the identities it names (all when unset); a name no eval proves
  is refused, naming the ones that do, so a typo runs nothing. `Catalog/EvalCatalog` lists the chosen evals, then the
  stub, and refuses a name twice and an eval with no fixed-rule check. `--list` prints one JSON line per eval (whose
  it is, what it proves, its categories and graders), which the API's eval list reads.
- **The arguments.** `Arguments/CommandLine` parses `run {eval}` with `--repeat N` (1 to 5), `--split`, `--web`,
  `--replay {runId}` and `--rescore {runId}`, and the two judge commands, `--label-sheet` and `--calibrate`. A bad
  argument is refused before anything starts (`Refusal/`).
- **A run.** The harness's `EvalRunner` runs the modes and repeats and writes protocol v2 on stdout; logs go to
  stderr. The run's id is `BENCHMARK_RUN`, else `local-` and its start time.
- **A re-score** reads the stored run's own export on stdin and grades it again, for $0 (`rescore_of`).
- **The judge.** `Calibration/LabelSheetCommand` writes the files a person labels from a stored run;
  `CalibrateCommand` asks the model judge about every label and prints the agreement, then an `@` report line.
- **The stub eval** (`Evals/Stub/`) answers from canned events through a `CannedSessionClient`: no session, no model.
  It proves the runner end to end in the tests and in the portal for free.

## Structure

| Folder | What's in it |
|---|---|
| `Arguments/` | `CommandLine`, the run request |
| `Calibration/` | The label sheet and the calibration commands |
| `Catalog/` | `EvalCatalog`, `EvalFolders`, `RunnerChecks`, the listing line |
| `Evals/Stub/` | The stub eval, its mode, role and toolset |
| `Manuals/` | The stub's manual (`stub.md`) |
| `Messages/`, `Refusal/` | The runner's texts and the refusal report |

## Configuration

| Setting | What it does |
|---|---|
| `EVALS` | The identities whose evals the catalogue lists, by commas, case ignored (`puls` today); an unknown one is refused; unset: all |
| `BENCHMARK_RUN` | The run's id, so the API can tie the lines to its run |
| `AGENT_API`, `AGENT_API_KEY`, `EVAL_JUDGE_*` | Read by the harness ([louis-agent.evals](louis-agent.evals.md)) |
| `EVAL_RUNNER` (the API's) | Where the API finds this runner; `/app/evals/louis-agent.evals.runner.dll` in the images |

## Extending it

A new eval is a folder of files beside its identity or pack, which reaches the runner through
`louis-agent.composition`; nothing here changes. The steps are [Add an eval](../guides/add-an-eval.md).

## Tests

`tests/louis-agent.evals.runner.tests`: the arguments (clamped repeats, an unknown name refused), the catalogue (no
grader refused, each eval once), the folders found and chosen by `EVALS=` (`EvalFoldersTests`), the stub's run
against its golden, the conformance of every eval folder, the data files copied beside the assembly. `StubCheckTests` (api integration) runs the real runner through the API.

## Limits and plans

- A paid run waits for Louis's go; CI runs only the graders on recorded transcripts.

## Related docs

[Run the evals](../guides/run-the-evals.md) · [Add an eval](../guides/add-an-eval.md) ·
[louis-agent.evals](louis-agent.evals.md) · [louis-agent.identity.puls](louis-agent.identity.puls.md)

---
[Projects](README.md) · Previous: [louis-agent.evals.protocol](louis-agent.evals.protocol.md) · Next: [tests](tests.md)
