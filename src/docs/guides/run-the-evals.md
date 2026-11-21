# Run the evals

> **How-to.** Run an eval, from the admin portal or the command line; record its web once and replay it; pick the dev or
> the held-out scenarios; read what a run cost; and check a judge against people. **Elsewhere:** writing an eval and its
> dataset in [Add an eval](add-an-eval.md); the portal's own pages in [Change the eval pages](change-the-eval-pages.md);
> the runner's start from the API in [Change the API](change-the-api.md#add-an-eval-to-the-portal); why evals are C# in
> [ADR 0029](../adr/0029-simulations-in-csharp.md).

![Running the evals at a glance: the runner plays an eval's scenarios against the real agent, records or replays the web, and its grades, metrics and judge checks land on the run's page](diagrams/run-the-evals.svg)

An eval runs against the real agent, so **a run spends money**: a cook run is minutes of model calls. Ask before each
one, and read the cost in the ledger afterwards, never from a grader. Listing the evals is free.

## From the command line

```bash
EVALS=puls dotnet run --project demos/puls/hosts/LouisAgent.PulsEvalRunner -- --list     # free: Puls's evals (unset: every eval)
BENCHMARK_RUN=cook-1 AGENT_API=http://127.0.0.1:5070/ \
  dotnet run --project demos/puls/hosts/LouisAgent.PulsEvalRunner -- run puls-cook --repeat 1 --web record
```

| Flag | What it does |
|---|---|
| `--repeat N` | Runs each mode N times (1 to 5). |
| `--split dev\|held-out\|all` | The scenarios a run asks for. `dev` is the default, and it's all the portal runs. The held-out ones are for a release check, and are never tuned against. The run's first line says which. |
| `--web off\|record\|replay` | What the run does with the web. `off` does nothing special (the instance's `WEB_REPLAY`), `record` saves every page, search and fetch and the time it reads, and `replay` answers from a recording, which needs `--of {runId}`. |
| `--rescore {runId}` | Grades a stored run's transcripts again with today's graders, for $0, and records the result as a new run (`rescore_of`). It reads the stored run's protocol lines from stdin and takes no other flag. |
| `--label-sheet {eval} {grader} {runId} {folder} [{count}]` | Writes the files a person labels, from a stored run on stdin. Free. See *Checking the judge*. |
| `--calibrate {eval} {grader} [--labels -]` | Asks a model grader about every labelled answer and prints how often it agrees with the person, then the report as one `@` line of data. Never in CI. With `--labels -` the labels are read from stdin, one per line, which is what the portal's *Check the judge now* does. See *Checking the judge*. |
| `--replay {runId}` | Replays that run **with the model's answers too**. Every model request is a cache hit, the run costs $0, and its grades equal the recorded run's. It tests everything around the model (the graders, the harness, the engine's code), not a prompt: a changed prompt is a different request, so it misses. |

The run's id is `BENCHMARK_RUN`, or else `local-` and the time it started. Set it yourself, so you know what to replay.
The runner needs `AGENT_API_KEY` when the API wants one.

The runner writes the protocol on stdout and its logs on stderr. The protocol is version 2: one JSON object per line
with its `event` first, the records in `src/evals/LouisAgent.Evals.Protocol` that the API reads too
([ADR 0047](../adr/0047-protocol-v2-one-event-contract.md)). Any other line is a line for people.

## An eval's manual

Each eval has a manual beside it, a `manual.md` in its folder (Puls's are in `Evals/Cook/`, `Evals/Routing/`,
`Evals/Recipes/` and `Evals/RecipesLong/`, and the stub's is `stub.md` in the runner's `Manuals/`). It says what the
eval measures, its modes, every grader it declares, what it costs and how to run it by hand. The portal opens it from
the eval's details, and the runner's `--list` gives its path (`manual`). A test holds each one to its eval: it opens
with a title and names every grader in backticks.

## From the portal

A run records the commit the runner was built from, and both images' builds take it as `EVAL_COMMIT`. For Puls:

```bash
EVAL_COMMIT=$(git rev-parse --short HEAD) docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build
```

For the code-review API, it's the same with `demos/louis-agent/docker/docker-compose.yml … up -d --build api`. In
PowerShell, set `$env:EVAL_COMMIT = git rev-parse --short HEAD` first. Without it, or set to nothing, the run says
`unknown`. The cloud image gets it from `src/site/deploy/azure/deploy.ps1 build`, which passes the checkout's commit.

The admin portal lists the evals the runner lists, and starts one with `POST /v1/admin/evals/{check}/runs`:

```json
{ "repeat": 1, "web": "record" }
{ "repeat": 1, "replay": "{runId}" }
```

`web` is `record` or `off`, and `replay` is a recorded run's id, with the model's answers (the runner's `--replay`).
Send one of them, not both. A portal run is always the `dev` scenarios. Every eval route also still answers under its
old `/v1/admin/benchmarks/…` name, until CL48 removes it.

**The run's process sees only what it reads** (FX25). That's `EVALS`, `CI`, the `EVAL_JUDGE_*` settings,
`AGENT_API_KEY`, `LOG_DIRECTORY`, `USAGE_LEDGER`, `PRICES_FILE` and `PRICES_URL`, plus what it needs to start (`PATH`,
a home, a temp folder, `DOTNET_*`) and what the API sets (`AGENT_API`, `BENCHMARK_RUN`). An Anthropic judge with no
`EVAL_JUDGE_API_KEY` gets the Anthropic key under that name. The Mongo login, the admin key and the other providers'
keys never reach it. So set `ADMIN_API_KEY` on its own: without it, the portal's key is `AGENT_API_KEY`, which the
runner does get. A setting the runner starts reading must be added to `RunnerEnvironment.Names` in the eval studio's
API, or it never arrives.

The run is stored as it runs, with how its web was handled (`web`, `replay_of` in its settings). A replay never joins
an eval's trend: it is the recorded run again, at no cost.

While a run goes, the Evals page follows it. A row per turn appears as the runner's events come in, with its role, its
calls (the agent's own `agent_calls` and the specialists' `specialist_calls`), its seconds and, once the run has ended,
its cost. A click opens the turn as it streams. A conversation's grades show on the run's page once it is stored.

**When the API starts, it deletes what an older build stored.** That's every run without `protocol_version: 2`, with
its conversations, turns, requests, notes, people's grades and judge checks, and it logs one line with the counts.
Stored eval data is disposable until a sale, so a changed shape means delete and run again
([ADR 0049](../adr/0049-nothing-is-compatible-before-a-sale.md)).

## The three ways to run one

![Picking a run: nothing changed means a recorded baseline, a changed prompt means a web replay with a live model, and a change around the model means a full replay at no cost](diagrams/run-the-evals-flow.svg)

Pick by what you changed:

| You changed | Run | The web | The model | Cost |
|---|---|---|---|---|
| nothing: the baseline | `--web record` | live, saved | live, saved | the run's own |
| a prompt | `--web replay --of {runId}` | the recording's | live | the run's own |
| the graders, the harness, the engine's code | `--replay {runId}` | the recording's | the recording's | $0 |

The studio's *Try* ([Add an eval](add-an-eval.md#9-or-compose-it-in-the-studio)) is the last row, on an eval that isn't
in the catalogue yet. It replays the run you name, from the eval's folder (`--folder`), and only the samples you list
(`--samples`), so it costs nothing.

A replay that misses says where the request changed: the model, the system prompt, a tool, the settings, or message
*n*. What the recording never read comes back to the model as a short text it treats as unknown.

### Re-scoring a run

A grader change shouldn't cost a paid run. `run {name} --rescore {runId} < run.txt` grades the stored run's
conversations again with the graders as they are today, and records a new run that names the old one. The file is
`GET /v1/admin/evals/runs/{id}/protocol`. No session opens and no model is called, so it costs nothing, and the old run
stays as it was. A scenario the dataset no longer has is graded `Unknown`, never `Fail`.

In the portal, a finished run's row has a *Re-score* button (`POST /v1/admin/evals/runs/{id}/rescore`). The new run says
"re-scored from" the original, with a link, and costs $0.

## Reading the results

A run's conversations carry their grades and an outcome. A trial passes only when every required grader passes, and an
`Unknown` never counts as a pass. A grader that doesn't apply to a scenario (no budget set, no shop named) passes, so
read that as "not asked".

A turn that failed for the infrastructure (a timeout, a refused connection, a broken stream, a non-2xx answer) is
recorded with its failure, counted apart, and left out of the pass rates.

A run's page shows each mode's pass@k, the share of scenarios that passed at least once in their k trials, and pass^k,
the share that passed every time, each labelled with its k. The trend draws both. A rate no trial made is a dash,
never 0. The cost is the usage ledger's (`{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`), per request, priced from a dated
table.

Beside its grades, each conversation carries what it **measured** (`metrics`): the agent's and the specialists' calls,
the hosts it read, its turns, its tokens in and out, its seconds and its cost. The tokens and the cost come from the
ledger, once the run has ended. A metric is never a verdict: it never fails a run, and an unknown one is empty, never
0. The trend draws the pass rate ("passed"), pass@k and pass^k, the cost, tokens, time and the sites read.

A run's records are private. The recordings are in the API's `REPLAY_DIRECTORY` (git-ignored), and nothing serves them.

### Reading the results by category

One overall number hides where an agent fails, so a run's page has a *Results by category* table for each mode. A mode
is one configuration of the agent, such as `puls-cook`, so its trials are never mixed with another's. The first line
is **overall**. Under it is one line per **category** its scenarios carry (`language:af`, `shop:any`, `shops:two`,
`budget`, `follow-up`). A category is a label on a scenario, set in its file. A scenario can have several, and then it
counts in each of their lines and once in the overall one.

Each line says how many trials it holds (`n`) and how many of the ones that counted passed. A trial that failed for the
infrastructure is counted apart, in *Infra*, and left out of the rates. The line also says how many were Unknown, since
a check that couldn't tell is never a pass. Then come the pass rate, **pass@k** (the scenarios that passed at least
once), **pass^k** (the ones that passed every time), and the cost per trial and per pass, from the ledger.

Read two things carefully. `n` counts **trials**, while pass@k and pass^k are shares of **scenarios**: a scenario run
three times is three trials and one scenario. And a cost is "unknown" when a price was unknown, and "$0" for a
re-score.

**Why "too few for a rate" appears.** Fewer than five counted trials in a slice give no percentage, only the counts
(`2 of 3 passed · too few for a rate`). One trial moves a share of three by a third, so a rate would look precise and
not be. A run with one repeat has about one trial per scenario, so most categories are too few, which is why a
measuring run uses `--repeat 3` or more. A run from before categories existed says so, with a *Re-score* button that
gives it its lines for $0. A line's tasks link to their conversations in the explorer below.

## Checking the judge

Some things code can't judge, such as *is this recipe a meal for four?* An eval then asks a model, a **judge**, and its
answer is a measurement like any other. A measurement is only worth having if it is checked, so a judge is checked
against a person before its grade counts for anything. You read some answers and say yes or no, and the judge is asked
about the same answers.

If it agrees with you on **9 of 10** or more, **in each class of your labels**, it sees what you see. The classes
matter: a judge that says "pass" to everything agrees with every label that is a pass, so the check also asks it to
say "fail" where you did. If it doesn't agree, its grades are noise, and the fix is a better prompt or a better model,
not trusting it anyway. Until a judge passes, its grade is shown and counted but **never required**: the outcome of a
trial doesn't wait on it.

The check costs nothing on a local model (Ollama), and it **never runs in CI**, because it asks a model. Set the judge
first: `EVAL_JUDGE_PROVIDER`, `EVAL_JUDGE_MODEL`, and `EVAL_JUDGE_ENDPOINT` for a local server (see
`demos/puls/config/.env.example`). **Without a judge model set, every answer is "can't tell"**, and the report says so:
a check that agrees on nothing is the sign.

**The short way is the portal.** Grade answers in a run's explorer, then press *Check the judge now* in the *Judge
against people* card, as *Reviewing answers yourself* below says. The steps here are the long way, for labels you keep
in the dataset.

**1. Make a sheet.** From a stored run, write ten files to label. The export is the same text a re-score reads:

```bash
# run.txt is GET /v1/admin/evals/runs/8f7802b9a72e/protocol (the same file a re-score reads)
EVALS=puls dotnet run --project demos/puls/hosts/LouisAgent.PulsEvalRunner -- --label-sheet puls-cook meal-for-people 8f7802b9a72e   demos/puls/evals/LouisAgent.Identities.Puls.Evals/Evals/Cook/Dataset/labels 10 < run.txt
```

The sheet holds only scenarios that ask for a number of people, since a meal for four can't be labelled where no four
is asked. It takes one conversation of each scenario before a second of any, so ten labels are not ten repeats of one
dish. Asking twice gives the same sheet.

**2. Label it.** Each file holds the grader it is a label for, the scenario, the mode and repeat, the run, the turn and
the answer, and `"label": null`. Read the answer as the user would and set the label. **Answer the one question the
judge is asked, and nothing else**: not the price, not the shop, not whether you like it, which other graders already
check. A label that answers two questions measures two things at once. Give it before you read the judge's reason, so
it is yours alone:

```json
"label": { "pass": true, "note": "a main dish, feeds four" }
```

`pass` is what the grader should say. For `meal-for-people`, that's `true` for a main dish that feeds the number of
people asked, and `false` for a snack, a side, a refusal, or portions that don't add up. Write the note for yourself:
when the judge disagrees, it is what you will read.

**A file with `"label": null` stops the eval from loading**, naming the file, so label them all or delete the ones you
don't want. The labels are part of the dataset, but the dataset's version and hash cover the scenarios only, so
labelling changes neither.

**3. Calibrate.** `dotnet run` builds first, which copies the labels beside the runner. Then:

```bash
EVALS=puls EVAL_JUDGE_MODEL=qwen3:8b EVAL_JUDGE_ENDPOINT=http://localhost:11434   dotnet run --project demos/puls/hosts/LouisAgent.PulsEvalRunner -- --calibrate puls-cook meal-for-people
```

It prints the headline (`Agreement: 17 of 18`), then the agreement in each class of your labels, the result, and each
answer where the judge differed:

```
Labelled pass: judge pass 13, fail 1, can't tell 0 (13 of 14, 93%)
Labelled fail: judge pass 0, fail 4, can't tell 0 (4 of 4, 100%), few labels
Result: pass, at least 90% in each class
@{"event":"calibration","eval":"puls-cook","grader":"meal-for-people","judge_model":"qwen3:8b","agreed":17,"total":18,…}
```

The last line is the same report as data, the classes in the portal's shape and each disagreement with its reason, for
whatever started the runner: the portal keeps it as the check's report. Read the lines above it.

The bar is **90% of each class**, so a class of four needs all four. A class with no labels is untested and does not
pass, and a class of fewer than ten is marked *few labels*. After that come the answers where the judge differed, each
with the scenario, the run, which way you labelled it, which way the judge went (*can't tell* counts as a disagreement)
and its reason. Read those first: a disagreement is either the judge's mistake or yours, and the judge's reason says
which.

If a class is under the bar, stop and decide. The way out is a clearer prompt (the eval's rubric,
`Evals/Cook/Rubrics/meal-for-people.md`, which changes the prompt's hash, so the old grades are of another measure), a
better model, or a paid judge, which is a decision to make before it is a bill.

## Reviewing answers yourself

The judge is only worth trusting if people agree with it, so the portal lets you grade real answers and then shows how
often the judge said the same.

**Do it.** Open a run and go into one conversation, the explorer's second level. The *review* panel is under the
turns. It shows the question the judge was asked, what was asked of the agent, and the answer. Type your name the
first time (it stays in this browser, and the buttons wait for it), then press **Pass**, **Fail** or **Unsure**, with a
note if you like. A grade can be changed, and your latest one counts.

**Why you can't see the judge first.** If you see the judge's verdict before giving yours, you tend to agree with it,
and then the judge is being measured against itself. So the judge's verdict, its reason and its chip are hidden
everywhere in the explorer until you have graded that conversation. After that they show, with a line saying whether
you agreed.

**What Unsure is for.** It's for the answers you can't call. It is kept and shown, but never counted as a label,
because a label has to be a yes or a no you stand by. An *Unsure* after a *Pass* takes the pass back.

**How often the judge agreed.** The run's page has a *Judge against people* table for each reviewed judge: of the
answers you passed, how many the judge passed too, and of the ones you failed, how many it failed. Under ten labels in
a class it says *few labels*, because a share of four is just four answers.

**A judge that reads tool results can't be checked this way yet.** A label holds the questions and the answers, not
what the tools gave back, so a judge with `sources` is asked with no page and fails every answer. Use a re-score for
those, as the [recipes manual](../../../demos/puls/evals/LouisAgent.Identities.Puls.Evals/Evals/Recipes/manual.md) says.

**Checking the judge against your grades, from the portal.** In the same card, press **Check the judge now**. The API
writes your decided grades (pass or fail, each with its whole conversation) to the eval runner's stdin and runs
`--calibrate … --labels -` with the judge the API's instance is set to (`EVAL_JUDGE_*`). So a prompt change needs the
image rebuilt first. Nothing else can run meanwhile, because the judge is shared and it's one run at a time. It takes a
minute or two on a local judge, and the card says it is asking.

When it ends, the card shows the report: when and with which judge, how often it agreed with you, and whether it clears
the bar (90% in each class) or which class fell short. Under that are your passes and your fails with what the judge
said to each, *Where the judge differed* with the judge's reason for each, and *Earlier checks* folded underneath, so
you can see a prompt change move the number. A check that ended without a report shows the runner's last line, which
is the reason. The reports are kept in the store (`calibrations`), beside your grades, and go when the run is deleted.
The routes are `POST /v1/admin/evals/runs/{id}/calibrations/{grader}` and `GET /v1/admin/evals/runs/{id}/calibrations`.

**Using your grades as labels in the dataset.** Press *Download the labels (zip)*, unzip the files into
`demos/puls/evals/LouisAgent.Identities.Puls.Evals/Evals/Cook/Dataset/labels/`, and run the check from *Checking the
judge* (`--calibrate`). The dataset's labels are the ones a developer runs against without a store, while the portal's
check reads the store.

**What the name means.** The portal has one shared key, so nobody logs in: the name records who *said* they graded,
not who proved it. The panel says who it is grading as, and *Not you?* lets the name go so you can type your own. The
store keeps the name with each grade. The downloaded label files don't carry it, so a label is a label whoever gave it.

---
[Guides](README.md) · [Docs index](../README.md)
