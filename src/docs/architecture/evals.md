# The evals: architecture and communication

> **Explanation.** How the evals work: which parts there are, how they talk, what one run looks like in memory, the
> lines the runner writes, how the studio makes an eval, and why an eval is files. **Elsewhere:** running them in
> [Run the evals](../guides/run-the-evals.md); adding one in [Add an eval](../guides/add-an-eval.md); each
> project's files and settings in [projects/](../projects/README.md); why it's built this way in
> [ADR 0029](../adr/0029-simulations-in-csharp.md), [ADR 0047](../adr/0047-protocol-v2-one-event-contract.md),
> [ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md) and
> [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md).

![The evals: an eval is files, a demo's runner grades real sessions on the API, and the portal shows the runs](diagrams/evals.svg)

## In one paragraph

An eval is a folder of files in the evals project of the identity it proves: an `eval.json` that names its modes and its checks, with
its dataset, manual and transcripts beside it. It asks the agent real questions and grades the answers. The
**runner** is a small console
app, one per demo, built on the framework's runner library: it opens real chat sessions on the **API**, exactly as a person's chat would, reads what streams back, grades each
conversation with the eval's **checks**, and writes everything it saw as JSON lines on stdout (the **protocol**). The
API starts the runner as a child process when someone presses *Run* in the **portal**, reads those lines as they come,
keeps them in the **store**, joins the cost from the **usage ledger** when the run ends, and the portal shows the
result. The runner never knows the cost and the API never knows how to grade: each side does one job, and the protocol
is the only thing they share.

## The parts

![The evals: the portal, the API and the runner](diagrams/evals-the-parts.svg)

| Part | Project | Its one job |
|---|---|---|
| The check contract | `src/evals/LouisAgent.Evals.Checks` | What a check reads and returns (`Trial`, `ICheck`, `Grade`), and the catalogue a file names a check from (`ICheckType`, `CheckCatalog`). What a pack can reference to ship a check ([ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md)). |
| The harness | `src/evals/LouisAgent.Evals` | How an eval runs: `IEval`, `FileEval` (an eval read from its folder), the datasets, the general checks and their entries, the protocol writer, re-scoring, conformance, labels and calibration. Names no product. |
| An identity's evals | `demos/louis-agent/evals/LouisAgent.Identities.Triage.Evals` (`Evals/`) | What proves an identity, as folders of files. The triage team's are one eval for each of its roles: the sorter, the investigator and the reviewer. Puls's, in its own repository, are `puls-cook`, `puls-routing` and the recipes evals, with its own readers. A project under a demo's `evals/` that only its runner program references ([ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md), [ADR 0073](../adr/0073-packs-reference-no-evals.md)). |
| A pack's checks | `packs/evals/LouisAgent.Packs.{Name}.Checks` | The rules no general check can say, apart from the pack; only the runner references them ([ADR 0073](../adr/0073-packs-reference-no-evals.md)). |
| An identity's judges | a project under a demo's `evals/` | A model judge of the identity's own, apart from the identity because it needs the harness; only the runner references it. Puls's `meal-for-people` is one, in its own repository. |
| The runner | `src/evals/LouisAgent.Evals.Runner`, run by a demo's program, such as `LouisAgent.CodingEvalRunner` | The command (`EvalRunnerHost.RunAsync`), handed the program's own checks and readers (`RunnerCatalogs`), so the library names no pack or identity: finds the eval folders and gathers the check catalogue, lists the evals (`--list`), runs one, re-scores a stored run, writes a label sheet, calibrates a judge; for the studio, lists the catalogue (`--checks`), judges a folder (`--check`), runs a folder's samples (`--folder`, `--samples`) and converts promptfoo files. |
| The protocol | `src/evals/LouisAgent.Evals.Protocol` | The eight line records both sides share, and their reader and writer. Records only. |
| The API | `src/hosts/LouisAgent.Api.Evals`, on `src/hosts/LouisAgent.Api` | Starts the runner `EVAL_RUNNER` names, records its lines, prices them from the ledger, serves the portal; the API itself runs the sessions the runner opens. |
| The portal | `src/web/LouisAgent.Web.Evals`, in a site that adds it | Start a run, watch it live, read runs and trends, grade the answers that wait (one at a time on the Review page, F53.3), calibrate a judge, write notes; read every built-in prompt (`/admin/prompts`); make samples and transcripts from runs, and compose, check, try and publish an eval in the Studio. |
| The drafts | `src/hosts/LouisAgent.Api.Evals` (`Drafts/`, Mongo's `eval_drafts`) | What the studio made and hasn't published yet: samples, transcripts and evals being composed ([ADR 0061](../adr/0061-evals-grow-from-use.md), [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)). |
| The replay store | `LouisAgent.Core` (`Sessions/Replay/`, `Llm/Replay/`) and `LouisAgent.Core.Web` (`Replay/`) | Records a run's web answers, clock and model answers, so the same run can be replayed for free. |

## The types

![The evals' types: an eval is an IEval, each check an ICheck built from its type, and every event a record the writer puts on a line](diagrams/evals-the-types.svg)

The three kinds of check (`CheckKind`; on the wire a check is still a `grader`, as stored runs say):

| Kind | What it is | Examples |
|---|---|---|
| `fixed-rule` | Plain C# that reads the transcript. Every eval needs at least one required fixed-rule check. | `NoErrorCheck`, `RoutedToCheck`, `ContainsCheck`, `AmountUnderCheck`, `ImageShownCheck`, `NearCheck` and the rest of the harness's 23; `UntooledShops`, `EstimatesAtShop` (the shops pack's rules, F49.3) |
| `model` | A judge model reads the answer against a rubric. `EvaluatorCheck` wraps Microsoft.Extensions.AI.Evaluation's `IEvaluator`; the grade carries the model and the prompt's hash. | `judge` (the general one over a rubric file); `meal-for-people` (`MealForPeopleCheck`, Puls's own) |
| `human` | A person's verdict in the run explorer, paired with a model check so the two can be compared. | `meal-for-people.human` (`HumanReviewCheck`, the `human` entry) |

An eval's file names its checks by type (`within-time`, `{"seconds": 120}`): each check's entry, an `ICheckType`,
builds it from a typed settings record, and `CheckCatalog` finds the entry. The catalogue always starts with the
harness's `GeneralChecks.All`: 25 types by family, from `no-error` to `judge` and `human`, and the table is in
[Add an eval](../guides/add-an-eval.md#4-choose-the-checks). A demo's runner program adds its own through
`RunnerCatalogs`, and a type named twice stops the runner as it starts. The coding demo's program adds nothing, since
its triage evals name only general checks. A product with checks of its own adds them there: Puls's program, in its
own repository, adds its packs' checks (the where-to-buy rules and the recipes rule `recipe-from-source`) and its
judges.

*Decided in:* F49.3, F50.2, F35.2, [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md),
[ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md).

**Extract, then compare** (F49.2b): a check that compares a value in the answer with the scenario's (a total with a
budget, a number of people, an image with the pages read) is a general compare plus a *reader* the file names beside
the key (`"reader": "rand-total"`). Readers are a second registry, `ReaderCatalog`, built the same way: the harness's
`GeneralReaders.All` (`currency-amount`, `digits`, `markdown-images`), then what a demo's program hands in
(Puls's, in its own repository: `rand-total`, `rand-prices`, `servings`, `recipe-photos`, `recipe-link`). So an identity ships readers, not
checks, and its eval file keeps its stored names with `name` (`{"type": "amount-under", "name": "within-budget", …}`).

**A pack or an identity references no evals** ([ADR 0073](../adr/0073-packs-reference-no-evals.md), which supersedes
[ADR 0060](../adr/0060-a-pack-ships-its-checks.md)): what proves one is a project under `evals/` that only a runner program references
(`LouisAgent.Identities.Triage.Evals` here, and Puls's packs' checks projects in its own repository), and a new check
is a general one in the harness when it can be. What an identity knows comes in through a reader the file names:
Puls's where-to-buy rule `estimates-at-shop` reads whether a price is given as fact through Puls's `rand-prices`. Plain markdown reading (tables, links, a site's host)
is the contract's (`Markdown/`), so an identity, a pack and the harness read them the same way.

A grade's score is 0 to 1 with an outcome (pass, fail, unknown) and a reason. A conversation's metrics (calls, hosts,
turns, tokens, seconds, cost) are measured beside the grades and never decide a pass.

## One cook run, in memory

The worked example is one run of Puls's cook, the richest eval built on this harness. Puls lives in its own repository
now, and its runner program is this framework's runner on the packages, so the run's objects are the same as any
demo's.

![The evals: one cook run, in memory](diagrams/evals-one-cook-run-in-memory.svg)

## How a run talks

![How a run talks: the API starts the runner, the runner asks agent sessions and the judge, and every line is stored as it arrives, the cost read from the ledger at the end](diagrams/evals-how-a-run-talks.svg)

The other three conversations use the same pieces.

A re-score (`--rescore {runId}`) grades a stored run again. The API exports the run as protocol lines on the runner's
stdin, and `Rescorer` reads the transcripts back (`ProtocolTranscripts`) and grades them once more. No session opens,
and nothing costs money unless a judge runs. The new run says what it re-scored, in `rescore_of`.

A replay (`--replay {runId}`) runs a recorded run again for free. The runner asks for `replay` on each session, and
`ReplayResolution` in the API turns that into the recorded web answers, the recorded clock and the recorded model
answers (`ModelReplayChatClient`), so the agent gives the same answers without paying for them.

When the agent asks for something the recording doesn't have, that's a replay miss. The engine throws
`ModelReplayMissException`, the API streams it as an error of kind `replay_miss_error` (`StreamErrorKind` in
`LouisAgent.SessionStream`), and the runner marks the turn `TurnFailure.ReplayMiss`. So it counts as a failure of
the infrastructure, never as the agent getting an answer wrong.

A replay only matches when every request reads exactly as it did when it was recorded. That's why nothing that changes
between runs may get into a tool result: a specialist's or a minion's footer names who answered, never what it cost.
The cook recording `dc272b95eaeb` replays as `c0362ff73151` with no miss at any of its 20 turns. The portal marks a
replay on both pages and leaves it out of the estimate and the trend, and you start one from a recorded run's row
(`StoredRunRow.ReplayOf` and `Recorded`, read from the run's settings by `EvalSettings`).

*Decided in:* FX26, FX27, FX28, CL26.

A calibration checks the judge against a person. `--label-sheet` writes out the answers a person labels, and
`--calibrate` asks the judge about each label and reports how often it agrees. The bar is 90% of each class. From the
portal, `POST .../runs/{id}/calibrations/{grader}` runs the same command and stores the report.

*Decided in:* [ADR 0029](../adr/0029-simulations-in-csharp.md).

## The lines the runner writes

One JSON object per line, UTF-8, `"event"` first, every time UTC with `Z`. A line that isn't an event is a log line
for people.

| `event` | Record | When | Carries |
|---|---|---|---|
| `protocol` | `ProtocolEvent` | First line | The eval, its version, the commit, the graders, the models, the split, the dataset |
| `mode` | `ModeEvent` | A mode's repeat starts | The mode, the repeat, its toolsets, role and model, how many scenarios |
| `question` | `QuestionEvent` | A question is sent | Its conversation and turn key, the text |
| `sse` | `SseEvent` | A piece of the turn's stream | The stream event as the session sent it |
| `turn` | `TurnEvent` | A turn ends | Agent and specialist calls, hosts, seconds, error, failure kind |
| `conversation` | `ConversationEvent` | A conversation is graded | Its grades (`GradeInfo`), its `TrialOutcome`, its `Metrics` |
| `mode-done` | `ModeDoneEvent` | A mode's repeat ends | Pass rates so far (`RatesInfo`: pass@k, pass^k), infrastructure failures, retries |
| `summary` | `SummaryEvent` | After a mode's last repeat | The roll-up: overall, per category, per grader, per grader kind |

## The studio

The Studio page (`/admin/studio`, F49.5) makes a new eval without Rider. It never reads an eval file itself: the runner
is the one reader and judge, started as a process for each question, and git is where a finished eval ends up
([ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)). An eval being composed is a draft in `eval_drafts`;
`DraftFolder` writes it out as one folder, with the samples and transcripts kept for it from runs (F49.4), and the
check, the try, the publish and the promptfoo export all read that folder.

![The studio: a draft is kept, checked by the runner, tried on a replay, and published as a commit on its own branch](diagrams/evals-the-studio.svg)

| Method | Route | Does |
|---|---|---|
| `GET` | `/v1/admin/evals/checks` | The runner's catalogue: types, kinds, settings' schemas, readers |
| `POST`, `GET` | `/v1/admin/evals/drafts` | Make an eval draft; list them |
| `GET`, `PUT`, `DELETE` | `/v1/admin/evals/drafts/{id}` | One eval draft |
| `POST` | `/v1/admin/evals/drafts/{id}/verdict` | The runner's check of its folder |
| `POST` | `/v1/admin/evals/drafts/{id}/try` | A replay of a stored run on the samples named |
| `POST` | `/v1/admin/evals/drafts/{id}/publish` | A commit on its branch, or a zip where there is no git workspace |
| `POST` | `/v1/admin/evals/drafts/promptfoo` | A promptfoo file to a new eval draft, with what was left out |
| `GET` | `/v1/admin/evals/drafts/{id}/promptfoo` | The draft as a promptfoo file |

## Why an eval is files

Louis put the aim simply: *"it should be simple to make evals, edit them and from the admin panel, living in
identity."* Every eval tool we studied keeps three things apart, and ours does the same.

The eval itself is data: `eval.json`, its dataset, its manual and its transcripts, in a folder beside the identity or
pack it proves. An identity is who, a pack is what it can do, and an eval is how we prove it. The checks are a
catalogue of compiled C# classes, each picked by a kebab-case type, from the harness and from the projects that prove
the packs. And the judge's rubric is the one bit of free text a person writes, kept as a markdown file.

There's no script engine. .NET has no sandbox for running untrusted code inside the hosted service, so the way past
the catalogue is a new, reviewed C# class, never a script in a file. When a promptfoo file is imported, its
JavaScript asserts are left out, by name.

A few things we looked at shaped the format. Microsoft.Extensions.AI.Evaluation keeps its scenarios in C# only, so
there was no file format to reuse, but its judge evaluators can still be reached through the catalogue. Anthropic's
*Develop test cases* says to prefer volume over polish, to grow cases from real use, and to grade by code first, a
model second and a person last; a rubric asks for a constrained output and lets the grader think first.

promptfoo's assertion has `type`, `value`, `threshold`, `weight` and `metric`, and our check entry keeps the same
fields, so importing and exporting is renaming types from a table, not translating one shape into another. Inspect's
`Sample` (`input`, `target`, `id`, `metadata`) is the shape our scenarios keep. And `JsonSchemaExporter` (.NET 9 and
up) makes a JSON schema from a settings record, which is how the runner's `--checks` hands the studio a form for every
check.

*Decided in:* F49.1 to F49.5, [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md),
[ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md), [ADR 0059](../adr/0059-an-identity-ships-readers-not-checks.md),
[ADR 0061](../adr/0061-evals-grow-from-use.md), [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md),
[ADR 0073](../adr/0073-packs-reference-no-evals.md).

## Where the files are

```
src/evals/
├── LouisAgent.Evals/              # the harness
│   ├── Api/                        # ISessionClient, HttpSessionClient, CannedSessionClient, the session request
│   ├── Conformance/                # references must pass, known-bads must fail
│   ├── Datasets/                   # dataset.json + scenarios/{dev,held-out}/ (Inspect's Sample shape), the loader
│   ├── Files/                      # FileEval: an eval read from its folder
│   ├── Grading/                    # Common/ (the general checks, GeneralChecks; Answers/, Tools/), Models/ (the judge), Human/, Outcomes/
│   ├── Labels/                     # the label sheet and the calibration
│   ├── Protocol/                   # ProtocolWriter, ProtocolTranscripts
│   ├── Reports/                    # RunSummary
│   ├── Running/                    # EvalRunner, ScenarioAttempt, TrialChecking, Rescoring/
│   └── Trials/                     # ConversationMetrics
├── LouisAgent.Evals.Checks/       # the check contract: EvalMode, EvalJson; Catalog/; Datasets/; Trials/; Grading/ (ICheck, Grade, Verdict); Markdown/; Readers/; Messages/
├── LouisAgent.Evals.Protocol/     # Events/, Parts/, Kinds/, EvalProtocolJson
├── LouisAgent.Evals.Runner/       # EvalRunnerHost, Arguments/, Catalog/ (RunnerCatalogs, EvalCatalog), Calibration/, Evals/, Manuals/, Refusal/,
│                                   # Studio/, Promptfoo/ (the table, import, export)
└── template/Example/               # the folder a new eval starts as a copy of (never listed)
demos/louis-agent/evals/LouisAgent.Identities.Triage.Evals/Evals/  # Sorter/, Investigator/, Reviewer/
demos/louis-agent/hosts/LouisAgent.CodingEvalRunner/  # the coding demo's runner program, the general checks only
src/hosts/LouisAgent.Api.Evals/    # List/, Runs/, Store/, Reviews/, Calibrations/, Reports/, Export/, Replies/,
│                                   # Drafts/ (Samples/, Transcripts/, Composed/, Store/), Studio/ (DraftFolder, the runner's parts, Publish/),
│                                   # Endpoints/ (Evals/, Runs/, Reviews/, Drafts/, Studio/ with Promptfoo/)
src/web/LouisAgent.Web.Evals/      # Pages/ (Building/ with EvalStudio and Prompts, Running/, Reviewing/), Components/ (the portal's pages; Studio/),
                                    # Admin/ (the plain classes; Studio/ with Compose/ and Schema/)
```

## What to keep in mind

The API decides and the runner asks. The runner only says it wants a replay, and the API's settings decide what a
session may do. Cost comes from the ledger, never from a check: the runner can't see prices, and the API joins them on
after the run.

CI grades recorded transcripts only. Every eval's references and known-bads run in the tests, for free, and a live run
waits for Louis's go.

An eval is content, and a check is code. A new eval that uses the checks we have is a folder of files with no C#, and
a new check is a reviewed class with its entry. The runner reads the eval files, and git keeps them. The API and the
portal never read an eval's folder themselves, and the studio's publish is a commit that a person pushes.

*Decided in:* [ADR 0007](../adr/0007-measure-every-request.md), [ADR 0029](../adr/0029-simulations-in-csharp.md),
[ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md), [ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md),
[ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md).

---
[Architecture](README.md) · Related: [Building block view](05-building-block-view.md) ·
[Runtime view](06-runtime-view.md) · [Run the evals](../guides/run-the-evals.md)
