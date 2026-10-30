# The evals: architecture and communication

> **Explanation.** How the evals work as built at the end of F18 (2026-10-29): which parts there are, how they talk,
> what one run looks like in memory, and the lines the runner writes. **Elsewhere:** running them in
> [Run the evals](../guides/run-the-evals.md); adding one in [Add an eval set](../guides/add-an-eval-set.md); each
> project's files and settings in [projects/](../projects/README.md); why it's built this way in
> [ADR 0029](../adr/0029-simulations-in-csharp.md) and [ADR 0047](../adr/0047-protocol-v2-one-event-contract.md).

## In one paragraph

An eval is a C# class that asks the agent real questions and grades the answers. The **runner** is a small console
app: it opens real chat sessions on the **API**, exactly as a person's chat would, reads what streams back, grades each
conversation with the eval's **graders**, and writes everything it saw as JSON lines on stdout (the **protocol**). The
API starts the runner as a child process when someone presses *Run* in the **portal**, reads those lines as they come,
keeps them in the **store**, joins the cost from the **usage ledger** when the run ends, and the portal shows the
result. The runner never knows the cost and the API never knows how to grade: each side does one job, and the protocol
is the only thing they share.

## The parts

```mermaid
flowchart LR
  subgraph Portal["Portal (louis-agent.puls, Components/Admin)"]
    Bench["Benchmarks page<br/>checks, runs, live run"]
    Explorer["Run explorer<br/>conversations, turns, reviews"]
  end
  subgraph API["louis-agent.api"]
    Admin["Admin endpoints<br/>/v1/admin/evals/..."]
    Proc["EvalProcess<br/>starts the runner"]
    Rec["EvalRecorder<br/>RunRecording"]
    Sessions["Session endpoints<br/>POST /v1/sessions"]
    Replay["ReplayResolution"]
  end
  subgraph Runner["louis-agent.evals.runner (child process)"]
    Cat["EvalCatalog"]
    Harness["EvalRunner<br/>(louis-agent.evals)"]
    Set["PulsEvalSet<br/>(louis-agent.evals.puls)"]
  end
  Agent["The agent under test<br/>(engine, Puls identity, packs)"]
  Store[("Mongo: runs, conversations,<br/>turns, requests, reviews,<br/>calibrations, notes")]
  Ledger[("Usage ledger<br/>usage-YYYY-MM.jsonl")]
  RStore[("Replay store<br/>REPLAY_DIRECTORY")]
  Judge["Judge model<br/>(EVAL_JUDGE_*)"]

  Bench -->|"start a run"| Admin --> Proc -->|"dotnet runner.dll run puls-cook"| Cat
  Cat --> Set --> Harness
  Harness -->|"HttpSessionClient"| Sessions --> Agent
  Sessions --> Replay --> RStore
  Agent -->|"every model request"| Ledger
  Agent -.->|"stream (SSE)"| Harness
  Harness -->|"model graders"| Judge
  Harness -->|"protocol v2 on stdout"| Proc --> Rec --> Store
  Rec -->|"when the run ends"| Ledger
  Explorer -->|"read"| Admin --> Store
```

| Part | Project | Its one job |
|---|---|---|
| The harness | `evals/louis-agent.evals` | How an eval runs: `IEval`, the datasets, the trials, the graders, the protocol writer, re-scoring, conformance, labels and calibration. Names no product. |
| An eval set | `evals/louis-agent.evals.puls` | What proves Puls: `puls-cook` and `puls-routing`, their datasets, their own graders, their reference and known-bad transcripts. |
| The runner | `evals/louis-agent.evals.runner` | The command: lists the evals (`--list`), runs one, re-scores a stored run, writes a label sheet, calibrates a judge. |
| The protocol | `evals/louis-agent.evals.protocol` | The eight line records both sides share, and their reader and writer. Records only. |
| The API | `src/louis-agent.api` (`Evals/`, `Endpoints/Admin/`) | Starts the runner, records its lines, prices them from the ledger, serves the portal; also runs the sessions the runner opens. |
| The portal | `src/louis-agent.puls` (`Components/Admin/`) | Start a run, watch it live, read runs and trends, review answers, calibrate a judge, write notes. |
| The replay store | `louis-agent.core` (`Sessions/Replay/`, `Llm/Replay/`, `Tools/Web/Replay/`) | Records a run's web answers, clock and model answers, so the same run can be replayed for free. |

## The types

```mermaid
classDiagram
  class IEvalSet {
    <<interface>>
    string Identity
    IReadOnlyList~IEval~ Evals
  }
  class IEval {
    <<interface>>
    string Name
    int Version
    string Proves
    ModesAsync(sessions) EvalMode list
    IReadOnlyList~EvalScenario~ Scenarios
    DatasetManifest Dataset
    IReadOnlyList~IGrader~ Graders
    IReadOnlyList~LabeledAnswer~ Labels
    TurnFactsOf(turn, mode) TurnFacts
  }
  class IGrader {
    <<interface>>
    string Name
    int Version
    GraderKind Kind
    IReadOnlySet~string~ ExpectedKeys
    bool Required
    GradeAsync(Trial) Grade
  }
  class Grade {
    <<record>>
    string Grader
    int Version
    double Score
    GradeOutcome Outcome
    string Reason
    GradeJudge Judge
  }
  class Trial {
    <<record>>
    string RunId
    EvalMode Mode
    int Repeat
    EvalScenario Scenario
    Transcript Transcript
  }
  class ISessionClient {
    <<interface>>
  }
  class EvalRunner {
    RunAsync(eval, run)
    RescoreAsync(eval, runId, rescoreOf, lines)
  }
  class EvalEvent {
    <<record>>
    DateTimeOffset T
  }
  IEvalSet o-- IEval
  IEval o-- IGrader
  IGrader ..> Trial : grades
  IGrader ..> Grade : returns
  IGrader <|.. NoErrorGrader
  IGrader <|.. RoutedToGrader
  IGrader <|.. CostUnderGrader
  IGrader <|.. WithinTimeGrader
  IGrader <|.. EvaluatorGrader
  IGrader <|.. HumanReviewGrader
  IGrader <|.. PricedAtRetailer
  ISessionClient <|.. HttpSessionClient
  ISessionClient <|.. CannedSessionClient
  EvalRunner ..> IEval
  EvalRunner ..> ISessionClient
  EvalRunner ..> ProtocolWriter
  ProtocolWriter ..> EvalEvent
  EvalEvent <|-- ProtocolEvent
  EvalEvent <|-- ConversationEvent
```

The three kinds of grader (`GraderKind`):

| Kind | What it is | Examples |
|---|---|---|
| `fixed-rule` | Plain C# that reads the transcript. Every eval needs at least one required fixed-rule grader. | `NoErrorGrader`, `RoutedToGrader`, `CostUnderGrader`, `WithinTimeGrader` (the harness's); `PricedAtRetailer`, `UntooledShops`, `EstimatesAtShop`, `WithinBudget`, `ServesAsAsked`, `RecipeImage` (the cook's) |
| `model` | A judge model reads the answer against a rubric. `EvaluatorGrader` wraps Microsoft.Extensions.AI.Evaluation's `IEvaluator`; the grade carries the model and the prompt's hash. | `meal-for-people` (`MealForPeopleGrader`) |
| `human` | A person's verdict in the run explorer, paired with a model grader so the two can be compared. | `meal-for-people.human` (`HumanReviewGrader.For`) |

A grade's score is 0 to 1 with an outcome (pass, fail, unknown) and a reason. A conversation's metrics (calls, hosts,
turns, tokens, seconds, cost) are measured beside the grades and never decide a pass.

## One cook run, in memory

```mermaid
flowchart TB
  set["PulsEvalSet<br/>Identity = puls"]
  eval["CookEval : IEval<br/>Name = puls-cook, Version = 10"]
  modes["EvalMode puls-cook · EvalMode puls-tools"]
  data["Dataset puls-cook v7<br/>dev and held-out scenarios, labels"]
  graders["9 graders: CookNoError, routed-to, WithinBudget,<br/>EstimatesAtShop, UntooledShops, RecipeImage, ServesAsAsked,<br/>meal-for-people, meal-for-people.human"]
  trial["Trial<br/>run 5d1fd231c9b1 · puls-cook · repeat 1 · cook-malmesbury"]
  transcript["Transcript<br/>TurnTranscript per question"]
  grades["Grade × 9<br/>e.g. untooled-shops 1.0 pass"]
  conv["ConversationEvent<br/>grades, TrialOutcome, Metrics"]
  set --> eval
  eval --> modes
  eval --> data
  eval --> graders
  modes --> trial
  data --> trial
  trial --> transcript
  graders --> grades
  transcript --> grades
  grades --> conv
```

## How a run talks

```mermaid
sequenceDiagram
  actor Admin
  participant Portal
  participant API
  participant Runner as Runner (child process)
  participant Agent as Agent sessions (same API)
  participant Judge
  participant Store as Mongo store
  participant Ledger as Usage ledger

  Admin->>Portal: Run puls-cook, repeat 3
  Portal->>API: POST /v1/admin/evals/puls-cook/runs
  API->>Runner: start: run puls-cook --repeat 3 (BENCHMARK_RUN = run id)
  Runner-->>API: protocol line (eval, version, graders, dataset)
  loop each mode, repeat and scenario
    Runner-->>API: mode, question
    Runner->>Agent: POST /v1/sessions (role, toolsets, replay), then the message
    Agent->>Ledger: one row per model request
    Agent-->>Runner: the turn's stream (SSE)
    Runner-->>API: sse lines, then turn
    Runner->>Judge: the answer and the rubric (model graders only)
    Judge-->>Runner: label and reason
    Runner-->>API: conversation (grades, outcome, metrics)
    API->>Store: run, conversation, turn as they arrive
  end
  Runner-->>API: mode-done (pass@k, pass^k), summary
  API->>Ledger: read this run's rows
  API->>Store: each conversation's tokens and cost, the run's end
  Portal->>API: GET /v1/admin/evals/runs/current/events (live) and the run's pages
```

The other three conversations use the same pieces:

- **A re-score** (`--rescore {runId}`): the API exports the stored run as protocol lines on the runner's stdin;
  `Rescorer` reads the transcripts back (`ProtocolTranscripts`) and grades them again, with no session and no model
  cost unless a judge runs. The new run says `rescore_of`.
- **A replay** (`--replay {runId}`): the runner asks for `replay` on each session; `ReplayResolution` in the API turns
  that into recorded web answers, a recorded clock and recorded model answers (`ModelReplayChatClient`), so the agent
  gives the same answers for nothing. An ask the recording doesn't have is a replay miss: the engine throws
  `ModelReplayMissException`, the API streams it as an error of kind `replay_miss_error` (`StreamErrorKind` in
  `louis-agent.session-stream`), and the runner marks the turn `TurnFailure.ReplayMiss`, so it counts as
  infrastructure, never as the agent's failed answer (FX26). A replay only matches when every request reads the same
  as when it was recorded, so nothing that changes between runs may enter a tool result: a specialist's or minion's
  footer names the child, never what it cost (FX27). The cook recording `dc272b95eaeb` replays as `c0362ff73151` with
  no miss at any of its 20 turns. The portal marks a replay on both pages, leaves it out of the estimate and the
  trend, and starts one from a recorded run's row (`StoredRunRow.ReplayOf` and `Recorded`, read from the run's
  settings by `EvalSettings`; CL26, FX28).
- **A calibration**: `--label-sheet` writes the answers a person labels; `--calibrate` asks the judge about each label
  and reports how often it agrees (the bar is 90% of each class, ADR 0029). From the portal, `POST
  .../runs/{id}/calibrations/{grader}` runs the same command and stores the report.

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

## Where the files are

```
evals/
├── louis-agent.evals/              # the harness
│   ├── Api/                        # ISessionClient, HttpSessionClient, CannedSessionClient, the session request
│   ├── Conformance/                # references must pass, known-bads must fail
│   ├── Datasets/                   # dataset.json + scenarios/{dev,held-out}/ (Inspect's Sample shape), the loader
│   ├── Grading/                    # Common/ (the graders), Models/ (the judge), Human/, Outcomes/
│   ├── Labels/                     # the label sheet and the calibration
│   ├── Protocol/                   # ProtocolWriter, ProtocolTranscripts
│   ├── Reports/                    # RunSummary
│   ├── Running/                    # EvalRunner, ScenarioAttempt, TrialGrading, Rescoring/
│   └── Trials/                     # ConversationMetrics
├── louis-agent.evals.checks/       # the check contract: EvalMode; Datasets/Scenarios/; Trials/; Grading/ (IGrader, Grade, Verdict); Messages/
├── louis-agent.evals.protocol/     # Events/, Parts/, Kinds/, EvalProtocolJson
├── louis-agent.evals.puls/         # PulsEvalSet; Evals/Cook/, Evals/Routing/ (each with Dataset/ and transcripts); Shared/
├── louis-agent.evals.runner/       # Program, Arguments/, Catalog/, Calibration/, Evals/Stub/
└── louis-agent.evals.template/     # what a new eval set starts from
src/louis-agent.api/Evals/          # List/, Runs/, Store/, Reviews/, Calibrations/, Reports/, Export/
src/louis-agent.puls/Components/Admin/  # the portal's pages
```

## What to keep in mind

- **The API decides, the runner asks.** The runner only says it wants a replay; the API's settings decide what a
  session may do (ADR 0029).
- **Cost comes from the ledger, never from a grader.** The runner can't see prices; the API joins them after the run
  (ADR 0007).
- **CI grades recorded transcripts only.** Every eval's references and known-bads run in the tests for free; a live
  run waits for Louis's go.
- **What's next changes this picture.** F49 Evals as content (Phase 3b of the platform plan) moves an eval from C#
  to files beside its identity, a check from a class in an eval set to an entry in a catalogue, and adds a studio to
  the portal. Each F49 child updates this page.

---
[Architecture](README.md) · Related: [Building block view](05-building-block-view.md) ·
[Runtime view](06-runtime-view.md) · [Run the evals](../guides/run-the-evals.md)
