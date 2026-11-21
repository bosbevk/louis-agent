# LouisAgent.Evals.Runner — design

> **Kind:** library (`net10.0`) · **Path:** `src/evals/LouisAgent.Evals.Runner/` · **References:** LouisAgent.Evals,
> YamlDotNet · **Used by:** each demo's runner program, such as
> `demos/louis-agent/hosts/LouisAgent.CodingEvalRunner`, which the API starts as a child process (published to
> `/app/evals` in each demo's API image), and the API's tests' `src/tests/hosts/LouisAgent.Api.TestRunner`
> **Also shows:** `component`, `deployment`

![The eval runner, and what it does: the catalogue, a run or a re-score, and the judge](diagrams/LouisAgent.Evals.Runner.svg)

## Purpose

This is the command that runs an eval. It lists the evals it knows, runs one, re-scores a stored run, writes a label
sheet, and checks a judge.

It's a library, and the program around it is each demo's own. A demo's runner program is a few lines: it calls
`EvalRunnerHost.RunAsync` with the checks and readers its evals can name, its model providers and its telemetry.
The coding demo's, `LouisAgent.CodingEvalRunner`, hands in `RunnerCatalogs.GeneralOnly`, since its evals need only
the general checks. A product with checks of its own, such as Puls in its own repository, hands in a catalogue with
them. So does the API's test runner, which copies the framework's template eval beside it as its one file eval. The
library never names a pack or an identity, because a package can't reference someone's project. So what proves an
identity or a pack is referenced only by that demo's runner program, and no host carries it
([ADR 0073](../adr/0073-packs-reference-no-evals.md), [ADR 0074](../adr/0074-the-folders-say-what-ships.md)).

You run it as `dotnet LouisAgent.CodingEvalRunner.dll run {eval}`, and any demo's in the same way. The API
never references an eval project itself. It starts the runner program and reads what it writes on stdout. That's why
a new eval is just a folder of files, with nothing to change in the API.

The rest of this doc follows the runner's work: finding the evals, reading the command line, the studio's commands,
promptfoo, a run, a re-score, checking the judge, and the stub eval it carries.

*Decided in:* [ADR 0022](../adr/0022-benchmarks-recorded-from-the-protocol.md).

## How it works

![One command: the program's telemetry started, the command line read, every eval folder found and those EVALS= names kept, then the command done](diagrams/LouisAgent.Evals.Runner-flow.svg)

### 1. Finding the evals

Every eval is a folder of files. `Catalog/EvalFolders` finds them: every `eval.json` under `EvalFiles/`, beside the
runner. A folder's path in there is where it sits in the repository.

The runner builds each eval's checks from the catalogues its program handed in, as a `RunnerCatalogs`. The general
checks always come first, then the program's own, such as a pack's checks project or a product's judges. A
check's reader comes from the reader catalogue, held in the same record: the general readers first, then the
program's own. A check type or reader named twice stops the runner as it starts. The judges
build their model clients through the providers the program composed.

`EVALS=` keeps only the evals of the identities it names, or all of them when it's not set. If you name something no
eval proves, it's refused, and the refusal names the ones that do. So a typo runs nothing.

`Catalog/EvalCatalog` lists the chosen evals, then the stub. It refuses a name that shows up twice, and an eval with
no fixed-rule check. `--list` prints one JSON line per eval (whose it is, what it proves, its categories and its
graders), and that's what the API's eval list reads.

*Decided in:* F49.2.

### 2. Reading the command line

`Arguments/CommandLine` parses `run {eval}`, with `--repeat N` (1 to 5), `--split`, `--web`, `--replay {runId}` and
`--rescore {runId}`. It also parses the two judge commands, `--label-sheet` and `--calibrate`. A bad argument is
refused before anything starts (`Refusal/`).

### 3. The studio's commands

When someone composes an eval in the studio, the API asks the runner about it, never the harness.

`--checks` prints the catalogue as JSON lines (`Catalog/ChecksListing`): each check type with its kind and its
settings' JSON schema, then each reader with the kind it reads.

`--check {folder}` (`Studio/CheckCommand`) first seals a manifest that's missing its hash (`ManifestSeal`). Then it
loads the folder as strictly as a run would, and holds it to the conformance gate. It prints one
`{"problem": …}` line per problem and `{"ok": …}` last, and exits 0 whenever it was able to look.

`run {eval} --folder {path}` runs the eval in that folder (`RunTarget`), after checking its name matches the run's.
`--samples a,b` narrows it to those scenarios (`SampleSubset`, a Decorator over `IEval`). The studio's *try* adds
`--replay`.

*Decided in:* F49.5, [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md).

### 4. promptfoo, in and out

The runner can read and write promptfoo files, in `Promptfoo/`, using YamlDotNet.

`--import-promptfoo {file} {folder}` writes an eval folder from a promptfoo file (`PromptfooImport`). The tests
become dev scenarios. The asserts become checks, through the mapping table `PromptfooAssertion`. Each `llm-rubric`
becomes a rubric file a `judge` reads. Anything with no counterpart gets a `{"left_out": …}` line.

`--export-promptfoo {folder}` goes the other way (`PromptfooExport`), using the same table backwards, and prints the
folder as a promptfoo file. A check with no counterpart is named in a comment at the top.

### 5. A run

A run is the harness's `EvalRunner`. It runs the modes and the repeats, and writes protocol v2 on stdout, while the
logs go to stderr. The runner loads no `.env` file and isn't built through `AgentHost`, so it starts the telemetry
its program gave before anything else. The run's id is `BENCHMARK_RUN`, or else `local-` plus its start time.

### 6. A re-score

A re-score reads a stored run's own export on stdin and grades it again. That costs $0, and the new run is marked
`rescore_of` the old one.

### 7. Checking the judge

`Calibration/LabelSheetCommand` writes the files a person labels, from a stored run. `CalibrateCommand` then asks the
model judge about every label, prints how often it agrees, and ends with an `@` report line.

### 8. The stub eval

The stub eval, in `Evals/Stub/`, answers from canned events through a `StubSessionClient`. There's no session and
no model. It's listed after every other eval, whichever program runs it. It proves the runner works end to end, in the tests and in the portal, for free.

![The eval runner: its host, its commands, and the API that starts it](diagrams/LouisAgent.Evals.Runner-diagrams.svg)

![A run from the Evals page: the API starts the runner as a child process, reads its lines on stdout for the live view, and stores the run](diagrams/LouisAgent.Evals.Runner-diagrams-2.svg)

These are the types involved:

![The types: a program names its checks and readers, the host reads one command, finds the eval and runs it](diagrams/LouisAgent.Evals.Runner-interface.svg)

## Structure

| Folder | What's in it |
|---|---|
| `EvalRunnerHost.cs` | The one call a program makes: the catalogues and services in, the command run, the exit code back |
| `Arguments/` | `CommandLine`, the run request, `BenchmarkRun` (the run's id) |
| `Calibration/` | The label sheet and the calibration commands |
| `Catalog/` | `EvalCatalog`, `EvalFolders`, `RunnerCatalogs` (what a program hands in), the listing line, `ChecksListing` (`--checks`) |
| `Evals/Stub/` | The stub eval, its mode, role and toolset |
| `Manuals/` | The stub's manual (`stub.md`) |
| `Messages/`, `Refusal/` | The runner's texts and the refusal report |
| `Promptfoo/` | `PromptfooAssertion` (the mapping table, promptfoo's names as an enum), `PromptfooImport`, `PromptfooExport` |
| `Studio/` | `CheckCommand` (`--check`), `ManifestSeal`, `RunTarget` (`--folder`), `SampleSubset` (`--samples`) |

Its one package of its own is **YamlDotNet** (MIT, 18.1.0), for promptfoo's YAML. No host carries it.

## Configuration

The API starts it inside its container, or you start it by hand from a terminal.

![Where the eval runner runs: the API starts it as a child process, or you start it from a terminal, and it reaches a model and the collector](diagrams/LouisAgent.Evals.Runner-deployment.svg)

| Setting | What it does |
|---|---|
| `EVALS` | The identities whose evals the catalogue lists, by commas, case ignored (`triage` in the coding demo); an unknown one is refused; unset: all |
| `BENCHMARK_RUN` | The run's id, so the API can tie the lines to its run |
| `AGENT_API`, `AGENT_API_KEY`, `EVAL_JUDGE_*` | Read by the harness ([LouisAgent.Evals](LouisAgent.Evals.md)) |
| `EVAL_RUNNER` (the API's) | Where the API finds the demo's runner program: `/app/evals/LouisAgent.CodingEvalRunner.dll` in the coding demo's API image |

## Extending it

A new eval is a folder of files beside its identity or pack. Its project is copied beside the demo's runner
program, which references it, so nothing here changes. A new kind of check or reader is listed in the demo's
catalogues. The steps are in [Add an eval](../guides/add-an-eval.md).

## Tests

The tests are in `src/tests/evals/LouisAgent.Evals.Runner.Tests`. They cover the arguments (repeats are clamped,
an unknown name is refused) and the catalogue (an eval with no grader is refused, each eval shows up once).
`EvalFoldersTests` checks the folders are found and chosen by `EVALS=`. The stub's run is held to its golden file,
every eval folder is checked for conformance, and the data files are checked to be copied beside the assembly.

The studio's commands (`--checks`, `--check`, `--folder`, `--samples`) have tests too, and
`PromptfooRoundTripTests` takes a promptfoo file all the way round. `StubCheckTests`, in the API's
integration tests, runs the real runner through the API.

## Limits and plans

A run against a real model costs money, so it waits for Louis's go. CI only runs the graders, on recorded
transcripts.

## Related docs

[Run the evals](../guides/run-the-evals.md) · [Add an eval](../guides/add-an-eval.md) ·
[LouisAgent.Evals](LouisAgent.Evals.md) · [LouisAgent.Identities.Puls](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.Identities.Puls.md)

---
[Projects](README.md) · Previous: [LouisAgent.Evals.Protocol](LouisAgent.Evals.Protocol.md) · Next: [tests](tests.md)
