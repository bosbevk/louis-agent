# louis-agent.evals.runner — design

> **Kind:** console app (`net10.0`) · **Path:** `evals/louis-agent.evals.runner/` · **References:**
> louis-agent.evals, louis-agent.evals.puls, louis-agent.composition (the identities' and packs' eval folders reach
> the runner through it, ADR 0027) · **Used by:** louis-agent.api (starts it as a child process; published to
> `/app/evals` in the API's images)

## Purpose

The command that runs an eval: `dotnet louis-agent.evals.runner.dll run {eval}`. It lists the evals it knows, runs
one, re-scores a stored run, writes a label sheet and checks a judge. The API never references an eval project: it
starts this runner and reads its stdout, so a new eval set is a reference here and nothing in the API
([ADR 0022](../adr/0022-benchmarks-recorded-from-the-protocol.md)).

## How it works

- **The catalogue.** `Catalog/EvalCatalog` lists every eval of every set named in `EvalSetKind`, then every eval that is
  a folder of files, then the stub; `EVALS=` picks the sets by name and the folders by the identity their file names
  (all when unset). It refuses a name twice and an eval with no fixed-rule check. `Catalog/EvalFolders` finds the
  folders: every `eval.json` under `EvalFiles/` beside the runner, whose path below it is where the folder sits in the
  repository (F49.2); the runner builds their checks from the check catalogue it gathers (`GeneralChecks.All` today). `--list` prints one JSON line per
  eval (what it proves, its modes, categories and graders), which the API's eval list reads.
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
| `Arguments/` | `CommandLine`, the run request, the eval-set selection |
| `Calibration/` | The label sheet and the calibration commands |
| `Catalog/` | `EvalCatalog`, `EvalSetKind`, `EvalSets`, the listing line |
| `Evals/Stub/` | The stub eval, its mode, role and toolset |
| `Manuals/` | The stub's manual (`stub.md`) |
| `Messages/`, `Refusal/` | The runner's texts and the refusal report |

## Configuration

| Setting | What it does |
|---|---|
| `EVALS` | The eval sets the catalogue lists, and the identities whose eval folders it lists (`puls` today; the template project is never registered); unset: all |
| `BENCHMARK_RUN` | The run's id, so the API can tie the lines to its run |
| `AGENT_API`, `AGENT_API_KEY`, `EVAL_JUDGE_*` | Read by the harness ([louis-agent.evals](louis-agent.evals.md)) |
| `EVAL_RUNNER` (the API's) | Where the API finds this runner; `/app/evals/louis-agent.evals.runner.dll` in the images |

## Extending it

A new eval set is a project reference and one member of `EvalSetKind` with its arm in `EvalSets`; the steps are
[Add an eval set](../guides/add-an-eval-set.md).

## Tests

`tests/louis-agent.evals.runner.tests`: the arguments (clamped repeats, an unknown name refused), the catalogue (no
grader refused, each eval once), the stub's run against its golden, the conformance of every eval of every set, the
data files copied beside the assembly. `StubCheckTests` (api integration) runs the real runner through the API.

## Limits and plans

- A paid run waits for Louis's go; CI runs only the graders on recorded transcripts.

## Related docs

[Run the evals](../guides/run-the-evals.md) · [Add an eval set](../guides/add-an-eval-set.md) ·
[louis-agent.evals](louis-agent.evals.md) · [louis-agent.evals.puls](louis-agent.evals.puls.md)

---
[Projects](README.md) · Previous: [louis-agent.evals.protocol](louis-agent.evals.protocol.md) · Next: [louis-agent.evals.puls](louis-agent.evals.puls.md)
