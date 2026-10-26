# Run the evals

> **How-to.** Run an eval, from the admin portal or the command line; record its web once and replay it; pick the dev or
> the held-out scenarios; and read what a run cost. **Elsewhere:** writing an eval and its dataset in
> [Add an eval set](add-an-eval-set.md); the portal's own pages in [Change the Puls app](change-the-puls-app.md#change-the-admin-portal);
> the runner's start from the API in [Change the API](change-the-api.md#add-an-eval-to-the-portal); why evals are C# in
> [ADR 0029](../adr/0029-simulations-in-csharp.md).

An eval runs against the real agent, so **a run spends money** (a cook run is minutes of model calls). Ask before each one, and
read the cost in the ledger afterwards, never from a grader. Listing the evals is free.

## From the command line

```bash
EVALS=puls dotnet run --project evals/louis-agent.evals.runner -- --list     # free: the evals the runner knows
BENCHMARK_RUN=cook-1 PULS_API=http://127.0.0.1:5070/ \
  dotnet run --project evals/louis-agent.evals.runner -- run puls-cook --repeat 1 --web record
```

| Flag | What it does |
|---|---|
| `--repeat N` | Each mode N times (1 to 5). |
| `--split dev\|held-out\|all` | The scenarios a run asks for. `dev` is the default, and all the portal runs; the held-out ones are for a release check, never tuned against. The run's first line says which. |
| `--web off\|record\|replay` | What the run does with the web: nothing special (the instance's `WEB_REPLAY`), saves every page, search, fetch and the time it reads, or answers from a recording. `replay` needs `--of {runId}`. |
| `--rescore {runId}` | Grades a stored run's transcripts again with today's graders, for $0, and records the result as a new run (`rescore_of`). It reads the stored run's protocol lines from stdin and takes no other flag. |
| `--label-sheet {eval} {grader} {runId} {folder} [{count}]` | Writes the files a person labels, from a stored run on stdin; free. See *Checking the judge*. |
| `--calibrate {eval} {grader}` | Asks a model grader about every labelled answer and prints how often it agrees with the person; never in CI. See *Checking the judge*. |
| `--replay {runId}` | A replay of that run **with the model's answers too**: every model request is a cache hit, the run costs $0, and its scores equal the recorded run's. It tests everything around the model (the graders, the harness, the engine's code), not a prompt: a changed prompt is a different request, so it misses. |

The run's id is `BENCHMARK_RUN`, else `local-` and the time it started: set it yourself, so you know what to replay. The runner
writes the protocol on stdout and its logs on stderr, and needs `AGENT_API_KEY` when the API wants one.

## From the portal

A run records the commit the runner was built from. The container build takes it as `EVAL_COMMIT`:
`EVAL_COMMIT=$(git rev-parse --short HEAD) docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build`;
without it the run says `unknown`.

The admin portal lists the evals the runner lists and starts them with `POST /v1/admin/benchmarks/{check}/runs`:

```json
{ "repeat": 1, "web": "record" }
{ "repeat": 1, "replay": "{runId}" }
```

`web` is `record` or `off`; `replay` is a recorded run's id, with the model's answers (the runner's `--replay`). Send one of them,
not both, and only for an eval: a Python check answers 400. A portal run is always the `dev` scenarios. The run is stored as it
runs, with how its web was handled (`web`, `replay_of` in its settings), and a replay never joins a check's trend: it is the recorded
run again, at no cost.

## The three ways to run one

Pick by what you changed:

| You changed | Run | The web | The model | Cost |
|---|---|---|---|---|
| nothing: the baseline | `--web record` | live, saved | live, saved | the run's own |
| a prompt | `--web replay --of {runId}` | the recording's | live | the run's own |
| the graders, the harness, the engine's code | `--replay {runId}` | the recording's | the recording's | $0 |

### Re-scoring a run

A grader change shouldn't cost a paid run. `run {name} --rescore {runId} < run.txt` (the file is `GET /v1/admin/benchmarks/runs/{id}/protocol`) grades the
stored run's conversations again with the graders as they are today and records a new run that names the old one. No session opens and no model is
called, so it costs nothing, and the old run stays as it was. A scenario the dataset no longer has is graded `Unknown`, never `Fail`.

In the portal, a finished run's row has a *Re-score* button (`POST /v1/admin/benchmarks/runs/{id}/rescore`). The new run says "re-scored from" the original, with a link, and costs $0.

A replay that misses says where the request changed (the model, the system prompt, a tool, the settings, or message *n*); what
the recording never read comes back to the model as a short text it treats as unknown.

## Reading the results

A run's conversations carry their grades and an outcome (a trial passes only when every required grader passes, and an
`Unknown` never counts as a pass); a turn that failed for the infrastructure (a timeout, a refused connection, a broken stream, a
non-2xx answer) is recorded with its failure and left out of the pass rates, and counted apart. A run page shows each mode's pass@k (the share of scenarios that passed at least once in their k trials) and pass^k (the share that passed every time), labelled with k, and the trend draws both; a rate no trial made is a dash, never 0. A grader that does not apply to a scenario (no budget
set, no shop named) passes: read it as "not asked". The cost is the usage ledger's (`{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`), per
request, priced from a dated table.

Records of a run are private: the recordings are in the API's `REPLAY_DIRECTORY` (git-ignored), and nothing serves them.

### Reading the results by category

A run's page has a *Results by category* table for each mode (a mode is one configuration of the agent, such as `puls-cook`, so its trials are never mixed with another's). The first line is **overall**; under it, one line per **category** its scenarios carry (`language:af`, `shop:any`, `shops:two`, `budget`, `follow-up`). A category is a label on a scenario, set in its file; a scenario can have several, and then it counts in each of their lines and once in the overall one. One overall number hides where an agent fails; the lines by category show it.

Each line says how many trials it holds (`n`), how many passed of the ones that counted (a trial that failed for the infrastructure is counted apart, in *Infra*, and left out of the rates), how many were Unknown (a check that couldn't tell is never a pass), the pass rate, **pass@k** (the scenarios that passed at least once) and **pass^k** (the ones that passed every time), and the cost per trial and per pass, from the ledger. Two things to read carefully: `n` counts **trials**, while pass@k and pass^k are shares of **scenarios** (a scenario run three times is three trials and one scenario), and a cost is "unknown" when a price was unknown, "$0" for a re-score.

**Why "too few for a rate" appears.** Fewer than five counted trials in a slice give no percentage, only the counts (`2 of 3 passed · too few for a rate`): one trial moves a share of three by a third, so a rate would look precise and not be. A run with one repeat has about one trial per scenario, so most categories are too few; that is why the end-of-F18 runs use `--repeat 3` or more. A run from before categories existed says so, with a *Re-score* button that gives it its lines for $0. A line's tasks link to their conversations in the explorer below.

## Checking the judge

Some things code can't judge: *is this recipe a meal for four?* An eval then asks a model, a **judge**, and its answer is a measurement like any other. A measurement is only worth having if it is checked, so a judge is checked against a person before its grade counts for anything: you read some answers and say yes or no, and the judge is asked about the same answers. If it agrees with you on **9 of 10** or more, **in each class of your labels**, it sees what you see: a judge that says "pass" to everything agrees with every label that is a pass, so the check also asks it to say "fail" where you did. If it doesn't, its grades are noise, and the fix is a better prompt or a better model, not trusting it anyway. Until a judge passes, its grade is shown and counted but **never required**: the outcome of a trial doesn't wait on it.

The check costs nothing on a local model (Ollama), and it **never runs in CI**: it asks a model. Set the judge first (`EVAL_JUDGE_PROVIDER`, `EVAL_JUDGE_MODEL`, and `EVAL_JUDGE_ENDPOINT` for a local server; see `config/.env.example`).

**1. Make a sheet.** From a stored run (the export is the same text a re-score reads), write ten files to label:

```bash
# run.txt is GET /v1/admin/benchmarks/runs/8f7802b9a72e/protocol (the same file a re-score reads)
EVALS=puls dotnet run --project evals/louis-agent.evals.runner -- --label-sheet puls-cook meal-for-people 8f7802b9a72e   evals/louis-agent.evals.puls/Evals/Cook/Dataset/labels 10 < run.txt
```

The sheet holds only scenarios that ask for a number of people (a meal for four can't be labelled where no four is asked), one conversation of each scenario before a second of any, so ten labels are not ten repeats of one dish. Asking twice gives the same sheet.

**2. Label it.** Each file holds the grader it is a label for, the scenario, the mode and repeat, the run, the turn and the answer, and `"label": null`. Read the answer as the user would and set the label. **Answer the one question the judge is asked, and nothing else**: not the price, not the shop, not whether you like it, which other graders already check; a label that answers two questions measures two things at once. Give it before you read the judge's reason, so it is yours alone:

```json
"label": { "pass": true, "note": "a main dish, feeds four" }
```

`pass` is what the grader should say: for `meal-for-people`, `true` for a main dish that feeds the number of people asked, `false` for a snack, a side, a refusal, or portions that don't add up. Write the note for yourself: when the judge disagrees, it is what you will read. **A file with `"label": null` stops the eval from loading**, naming the file; label them all, or delete the ones you don't want. The labels are part of the dataset, but the dataset's version and hash cover the scenarios only, so labelling changes neither.

**3. Calibrate.** `dotnet run` builds first, which copies the labels beside the runner. Then:

```bash
EVALS=puls EVAL_JUDGE_MODEL=qwen3:8b EVAL_JUDGE_ENDPOINT=http://localhost:11434   dotnet run --project evals/louis-agent.evals.runner -- --calibrate puls-cook meal-for-people
```

It prints the headline (`Agreement: 17 of 18`), then the agreement in each class of your labels, the result, and each answer where the judge differed:

```
Labelled pass: judge pass 13, fail 1, can't tell 0 (13 of 14, 93%)
Labelled fail: judge pass 0, fail 4, can't tell 0 (4 of 4, 100%), few labels
Result: pass, at least 90% in each class
```

The bar is **90% of each class** (a class of four needs all four); a class with no labels is untested and does not pass; a class of fewer than ten is marked *few labels*. After that, for each answer where the judge differed: the scenario, the run, which way you labelled it, which way the judge went (*can't tell* counts as a disagreement) and its reason. Read those first: a disagreement is either the judge's mistake or yours, and the judge's reason says which.

If a class is under the bar, stop and decide: a clearer prompt (`Prompts/judges/meal-for-people.md`, which changes the prompt's hash, so the old grades are of another measure), a better model, or a paid judge, which is a decision to make before it is a bill.

---
[Guides](README.md) · [Docs index](../README.md)
