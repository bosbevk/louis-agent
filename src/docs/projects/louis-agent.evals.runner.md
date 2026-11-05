# louis-agent.evals.runner — design

> **Kind:** console app (`net10.0`) · **Path:** `src/evals/louis-agent.evals.runner/` · **References:**
> louis-agent.evals, louis-agent.composition (the toolsets an eval's modes name, ADR 0027), and what proves each identity
> and pack, which only the runner references so no host carries it (ADR 0073): louis-agent.identity.puls.evals (its eval
> folders and readers), louis-agent.identity.puls.judges (its judge), louis-agent.pack.where-to-buy.checks and
> louis-agent.pack.recipes.checks · **Used by:** louis-agent.api (starts it as a child process; published to
> `/app/evals` in the API's images)

## Purpose

The command that runs an eval: `dotnet louis-agent.evals.runner.dll run {eval}`. It lists the evals it knows, runs
one, re-scores a stored run, writes a label sheet and checks a judge. The API never references an eval project: it
starts this runner and reads its stdout, so a new eval is a folder of files and nothing in the API
([ADR 0022](../adr/0022-benchmarks-recorded-from-the-protocol.md)).

## How it works

- **The catalogue.** Every eval is a folder of files (F49.2). `Catalog/EvalFolders` finds them: every `eval.json`
  under `EvalFiles/` beside the runner, whose path below it is where the folder sits in the repository. The runner
  builds their checks from the check catalogue it gathers (`Catalog/RunnerCatalogs`, from the program that started it: the general checks, then each pack's
  checks project, then Puls's judges `PulsJudges.All`), and a check's reader from the reader catalogue
  (the same record: the general readers, then the program's, such as `PulsReaders.All`). `EVALS=` then keeps the evals of the identities it names (all when unset); a name no eval proves
  is refused, naming the ones that do, so a typo runs nothing. `Catalog/EvalCatalog` lists the chosen evals, then the
  stub, and refuses a name twice and an eval with no fixed-rule check. `--list` prints one JSON line per eval (whose
  it is, what it proves, its categories and graders), which the API's eval list reads.
- **The arguments.** `Arguments/CommandLine` parses `run {eval}` with `--repeat N` (1 to 5), `--split`, `--web`,
  `--replay {runId}` and `--rescore {runId}`, and the two judge commands, `--label-sheet` and `--calibrate`. A bad
  argument is refused before anything starts (`Refusal/`).
- **The studio's commands** (F49.5, [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)): the API asks the
  runner, never the harness, about an eval being composed. `--checks` prints the catalogue as JSON lines
  (`Catalog/ChecksListing`: each check type with its kind and its settings' JSON schema, then each reader with the
  kind it reads). `--check {folder}` (`Studio/CheckCommand`) seals a manifest left without its hash
  (`ManifestSeal`), loads the folder as strictly as a run would and holds it to the conformance gate, then prints one
  `{"problem": …}` line each and `{"ok": …}` last, exiting 0 whenever it could look. `run {eval} --folder {path}`
  runs the eval in that folder (`RunTarget`, its name checked against the run's), and `--samples a,b` narrows it to
  those scenarios (`SampleSubset`, a Decorator over `IEval`); the studio's try adds `--replay`.
- **promptfoo in and out** (`Promptfoo/`, YamlDotNet): `--import-promptfoo {file} {folder}` writes an eval folder
  from a promptfoo file (`PromptfooImport`: the tests as dev scenarios, the asserts as checks by the mapping table
  `PromptfooAssertion`, each `llm-rubric` a rubric file a `judge` reads) and prints a `{"left_out": …}` line for
  each part with no counterpart; `--export-promptfoo {folder}` prints the folder as a promptfoo file
  (`PromptfooExport`, the table backwards, a check with no counterpart named in a comment at the top).
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
| `Catalog/` | `EvalCatalog`, `EvalFolders`, `RunnerCatalogs`, the listing line, `ChecksListing` (`--checks`) |
| `Evals/Stub/` | The stub eval, its mode, role and toolset |
| `Manuals/` | The stub's manual (`stub.md`) |
| `Messages/`, `Refusal/` | The runner's texts and the refusal report |
| `Promptfoo/` | `PromptfooAssertion` (the mapping table, promptfoo's names as an enum), `PromptfooImport`, `PromptfooExport` |
| `Studio/` | `CheckCommand` (`--check`), `ManifestSeal`, `RunTarget` (`--folder`), `SampleSubset` (`--samples`) |

The one package of its own is **YamlDotNet** (MIT, 18.1.0), for promptfoo's YAML; no host carries it.

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

`src/tests/evals/louis-agent.evals.runner.tests`: the arguments (clamped repeats, an unknown name refused), the catalogue (no
grader refused, each eval once), the folders found and chosen by `EVALS=` (`EvalFoldersTests`), the stub's run
against its golden, the conformance of every eval folder, the data files copied beside the assembly, the studio's
commands (`--checks`, `--check`, `--folder`, `--samples`) and a promptfoo file's round trip (`PromptfooRoundTripTests`). `StubCheckTests` (api integration) runs the real runner through the API.

## Limits and plans

- A paid run waits for Louis's go; CI runs only the graders on recorded transcripts.

## Related docs

[Run the evals](../guides/run-the-evals.md) · [Add an eval](../guides/add-an-eval.md) ·
[louis-agent.evals](louis-agent.evals.md) · [louis-agent.identity.puls](../../../demos/puls/docs/projects/louis-agent.identity.puls.md)

---
[Projects](README.md) · Previous: [louis-agent.evals.protocol](louis-agent.evals.protocol.md) · Next: [tests](tests.md)
