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
| `--replay {runId}` | A replay of that run **with the model's answers too**: every model request is a cache hit, the run costs $0, and its scores equal the recorded run's. It tests everything around the model (the graders, the harness, the engine's code), not a prompt: a changed prompt is a different request, so it misses. |

The run's id is `BENCHMARK_RUN`, else `local-` and the time it started: set it yourself, so you know what to replay. The runner
writes the protocol on stdout and its logs on stderr, and needs `AGENT_API_KEY` when the API wants one.

## From the portal

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

A replay that misses says where the request changed (the model, the system prompt, a tool, the settings, or message *n*); what
the recording never read comes back to the model as a short text it treats as unknown.

## Reading the results

A run's conversations carry their grades and an outcome (a trial passes only when every required grader passes, and an
`Unknown` never counts as a pass); a turn that failed for the infrastructure (a timeout, a refused connection, a broken stream, a
non-2xx answer) is recorded with its failure and left out of the pass rates, and counted apart. A run page shows each mode's pass@k (the share of scenarios that passed at least once in their k trials) and pass^k (the share that passed every time), labelled with k, and the trend draws both; a rate no trial made is a dash, never 0. A grader that does not apply to a scenario (no budget
set, no shop named) passes: read it as "not asked". The cost is the usage ledger's (`{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`), per
request, priced from a dated table.

Records of a run are private: the recordings are in the API's `REPLAY_DIRECTORY` (git-ignored), and nothing serves them.

---
[Guides](README.md) · [Docs index](../README.md)
