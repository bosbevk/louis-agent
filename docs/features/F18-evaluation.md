# F18 · Evals in C#: one harness for checks, regressions and demos

> **Status:** planned (built on `feature/F18-evals`) · **Phase:** 3 of [the platform plan](../plans/platform.md) ·
> **Plan:** [F18-evals.md](../plans/F18-evals.md) · **Depends on:** F1, F2 and Phase 2 (CL15)
>
> **In the TODO:** **F18 Evals in C#** under *Now*, which also takes in the CI gate · **Decided in:**
> [ADR 0029](../adr/0029-simulations-in-csharp.md) (amended 2026-10-24: the word is *eval*) · **Replaces:** the earlier
> *F18 Evaluation* design, a `tools/louis-agent.evals` console app over suites in `evals/<suite>/` (`cases.jsonl`, the
> scorers `exact`, `contains` and `judge`), built on F16 and F17, which is not built; its name and folder are the new
> harness's now. It also replaces the backlog's *Evaluation* gap.

Today the only quality check is the orchestrator demo: ten fixes, each verified, at about $0.66 a run. It says whether
the whole system still works, but not whether one part (a summary, a triage decision, a cook's price list) is right, and
it's too slow and costly to run for every prompt change. Puls's checks are Python scripts (`benchmarks/`), the session
stream is read four times, and the demos we sell with have no harness at all. F18 replaces all three with one C# harness
(`evals/`): every check, regression and demo is an **eval** of a real task, and each eval sits beside the identity or
pack it proves.

## The words, from the field

Anthropic's *Demystifying evals for AI agents* (9 Jan 2026) gives the words we use. Checked 2026-10-18.

- **Task:** a scenario, its made-up data and its **success criteria**. An eval runs a task and scores it; it simulates a
  real task.
- **Trial:** one run of a task. Each trial leaves a **transcript** (the session's events, tool calls included, each
  turn's time, and any infrastructure failure) and ends in an **outcome**.
- **Grader:** scores an outcome. Code graders first (fixed rules, free and repeatable); a model grader only where code
  can't judge, calibrated against Louis's own labels first.
- **pass@k** (passed at least once in k trials) and **pass^k** (passed every time in k). A sold agent needs pass^k.
- **Mode:** our own addition. A task runs in one or more modes (Puls alone, or Puls with its cook).

## User stories

**F18-S1 — An eval is a task.** As a *developer*, I want each check and demo written as a task with success criteria,
so that a new one is a class, not a script.
- *Given* an eval in `evals/louis-agent.evals.{identity-or-pack}/` *then* the runner lists it by name and runs its
  trials; a name listed twice is refused.

**F18-S2 — One runner, one stream reader.** As a *developer*, I want one runner and one reader for the session stream, so
that a new event is learnt once.
- *Given* `dotnet /app/evals/louis-agent.evals.runner.dll run {name} --repeat N` *then* each trial runs and writes the
  existing version-1 protocol lines, which the API stores as it does today.
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

The full design, steps, namespace map and diagrams are in [the plan](../plans/F18-evals.md). In short:

- **The harness** is `evals/louis-agent.evals/`, sold with the engine, and names no identity.
- **Evals** live beside what they prove: `evals/louis-agent.evals.puls/` (the cook and routing) and later the demo and
  voorman ones. Puls's scoring rules stay out of the harness.
- **The protocol stays.** F31's version-1 line protocol is the seam; the C# runner writes the same lines, and what it adds
  is additive, so every stored run still reads. The API starts the C# runner instead of `python3`.
- **Engine changes** (steps 4a to 4f of the plan): the prompt's hash on every ledger row, traces per turn and tool call,
  a replay cache for recorded evals only, the web input recorded and replayed, and personal data out of the logs
  (`LOG_CONTENT`, hashed by default).
- **The word:** the public word becomes *eval* in the way CL14 changed *role*: the new word is written, the old routes
  and names still answer until a TODO line removes them.
- **Packages:** Microsoft.Extensions.AI.Evaluation (MIT), pinned to 10.10.0 to match Microsoft.Extensions.AI. Its grader
  uses `IEvaluator.EvaluateAsync` directly, so no result store is needed.

## Implementation steps

The steps are the plan's, in order; each one's tests, and what it changes from this doc, are in the plan's table and the *What the build changed* notes below:

- **0** The plan and the word
- **1a** The one session-stream reader
- **1b** `TurnStream` and `AgentApi` read through it
- **1c** `SubSessionStream` reads a peer's stream through it
- **1d** `LouisAgentClient` reads through it
- **1e** The stream's golden, before the seam
- **1f** The writer's seam, a pluggable stream format
- **2** The harness's contracts and the protocol in C#
- **2a** What a run ran, recorded
- **3** Fixed-rule graders and the scores document
- **3a.1** The dataset loader and its hash (harness: `Datasets/`; the Puls datasets are 3a.2)
- **3a.2** Puls's datasets: `puls-cook` and `puls-routing`, the Python checks' scenarios as files
- **3a** Datasets as files, one folder per eval
- **4** The runner and the stub eval; the eval sets pluggable (`IEvalSet`, `EVALS=`) and the common graders; the developer's how-to `docs/guides/add-an-eval-set.md` (approved during the build)
- **4.1** The eval sets (`IEvalSet`), the runner project with its catalogue, arguments and the stub eval, and the stub's parity with the Python stub's protocol lines
- **4.2a** The common graders in `Grading/Common/`: no error, answered within a time, cost under a limit, routed to a role; one reader of the engine's events
- **4.2b** `EvalConformanceTests` and the reference transcripts: each scenario has one transcript that passes its required graders and one that fails them
- **4.3a** The API lists the C# evals and starts the runner: `BenchmarkCheck` names an eval (`BenchmarkCheckKind`), `BenchmarkCatalog` from the runner's `--list` (`EvalListing`), `BenchmarkRunner` starts `run`, `EVAL_RUNNER`, the stub's parity
- **4.3b** Both images carry the runner at `/app/evals` (compose sets no `EVAL_RUNNER`), and a by-hand Docker check
- **4.4** The developer's how-to, `docs/guides/add-an-eval-set.md`: its example checked against the code, and what writing it found (the build notes)
- **4a** Web input recorded and replayed: the browser
- **4b** The same for search, fetch and the clock
- **4c** The prompt's hash on every ledger row
- **4d** Traces per turn and tool call
- **4e** A replay cache for recorded evals
- **4f** Personal data out of the logs
- **5** Puls's cook and routing evals
- **5a** pass@k and pass^k
- **5b** Re-scoring for $0
- **5c** Results rolled up: overall and by category
- **6** A model grader where code can't judge
- **6a** Human review as a grader
- **7** Python leaves
- **7a** Why a run behaved as it did
- **7b** Every eval visible in the admin portal, and the API names no product; the store split by record (approved during the build)
- **7c** Protocol v2: one event model for the runner and the API; v1 runs discarded (approved during the build)
- **8** Docs and the write-ups

## Done when

- The plan's steps pass their tests; the golden files are unchanged, or reviewed.
- Louis has read the write-ups (step 8): skill 12's, the new evaluation one, and skill 13's, with the replay cache, traces and the prompt hash (skills 17 and 3).
- The paid runs are made only on Louis's go: one cook and one routing run in step 5, recorded beside the last Python runs; a free replay of each costs $0.
- Every eval is listed in the admin portal, including one never run, and `ApiNamesNoProductTests` passes (step 7b).
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): move **F18** to the CHANGELOG, set *Status* to done here and in the features table, and update skill 12 in the goal map.

## What the build changed

- **Step 1a** (`louis-agent.session-stream/`, its tests in `tests/louis-agent.session-stream.tests/`): the reader frames
  with the BCL's `SseParser` and maps each item to a record. The plan's `UsageReported` is not in it: the recorded
  stream has no usage event (usage is in `message_delta`), so it waits for the step that needs it. The recorded
  fixture's `StartSubSession` still names its role under `personality`, so the test reads both keys, as the Python does.
  The hosts are worked out in the test from the URLs, since the reader doesn't score them (step 3 does).
- **Review of step 1a:** `MessageStarted` carries its `Toolsets` and `Role` from `message` (the plan's signature; 1b's
  `TurnStream` reads them). A data line that isn't JSON is kept as an `UnknownEvent` whose `Data` is its text, so a cut-off
  connection doesn't lose the events after it.
- **Step 1b** (`src/louis-agent.web.shared`): `AgentApi.StreamMessageAsync` yields the reader's `SessionEvent`s, and
  `AgentStreamEvent` is gone. `TurnStream.Apply` matches the records: `MessageStarted` gives the toolsets and the role,
  `SubSessionStreamed` its inner event (no more second read of the JSON). `SessionEvent.From(type, data)` builds an
  event from a stored line, for the admin portal's saved runs and the tests. Where the plan said the tests stay
  unchanged: their assertions do, but the helpers that built an `AgentStreamEvent` now call `SessionEvent.From`, and
  the shared fixture reads through the reader. One behaviour change: a `message_start` with an empty `toolsets` array
  leaves the turn's toolsets as they were, where it used to set an empty list; nothing reads the difference today.
- **Step 1c** (`louis-agent.core`): core references the reader, and `SubSessionStream` reads a peer's stream through
  it, keeping its signature and its contract tests. The reader now delivers an event the stream ends before its blank
  line: SSE discards such an event, but the line-by-line loop this replaces read it, and seven contract tests (an error
  as the last event, a stop as the last) depend on it. The reader gets the blank line from a wrapper stream
  (`EndedByBlankLine`), with its own test written first and seen failing. A cut-off connection's last event is read
  the same way, and one cut inside a line is kept as an `UnknownEvent`.
  One behaviour change comes with the SSE framing: an event with no `event:` line gets the type `message`, and the name
  resets for each event, where the old loop kept the last name. Nothing changes today, because our API always sends
  `event:`.
- **Step 1e** (`tests/louis-agent.api.tests/Sessions/StreamGolden.sse`): the writer's bytes, pinned before anything moves.
  A scripted turn of each kind goes through `TurnStream.StreamTurnAsync` on a real engine and a `FakeChatClient`: a
  thinking, text and tool-call turn; a sub-session's event; a command reply (`/tools`); a cancel (the model's request is
  cut off, as a cancelled HTTP call is, and the stop is `cancelled`); and an error. The golden passes against today's code
  (nothing fails: the step pins the bytes), and reading it back through the reader gives the same types and data in
  order. The API tests reference `louis-agent.testing` for the fake model. Usage is in the bytes: the turns that call the model carry it (unpriced, so no price table is read), and a turn that fails on its second request, with its text block open, closes the block and reports its usage in the error. The golden is written and compared as the endpoint writes it, with `SseFormatter` and byte for byte (`StreamGolden.sse` is `-text` in `.gitattributes`). Regenerate with `LOUIS_UPDATE_GOLDEN=1`.

- **Step 1f** (`louis-agent.api`, `Sessions/Streaming/`): the writer is behind `ITurnStreamFormat` (`Start`, `Content`,
  `Text`, `SubSession`, `Stop`, `Fail`, and `Usage`), and the turn and the session view call only that. `ClaudeStyleStream`
  is `Streaming/Claude/ClaudeStreamFormat`, the only format today. `"stream"` on `POST /sessions` picks the format (the
  enum `StreamFormat`, parsed with `SettingChoice`, Claude when none is sent) and the session keeps it. A name none of
  the format's names fits is a 400 before any engine is built; the request's parse is tested, the endpoint's branch is
  not (the API has no endpoint tests yet), so it was checked by reading. **Not tested yet:** the endpoint's 400 for an unknown `"stream"`; the first endpoint test takes that case. The turn's usage is a `TurnUsage`, computed once
  from the ledger and only rendered by a format. The golden and the recorded fixture are unchanged; a second format waits
  for a client that needs one.

- **Step 2** (`evals/louis-agent.evals/`, the tests in `tests/louis-agent.evals.tests/`): the harness's contracts and the
  protocol. `IEval`, `EvalMode`, `EvalScenario`, `EvalInput`, `EvalScores`, `EvalRun`, `Trial`, `TurnTranscript`,
  `TrialFailure`, `IGrader`, `Grade`, `GradeOutcome`, `GraderKind`, `ISessionClient`, `SessionRequest` and
  `CannedSessionClient` are the plan's shapes. `ProtocolWriter` writes F31's version-1 lines, ASCII only, with `t` from the
  clock; `EvalRunner` runs an eval's modes, repeats and scenarios through an `ISessionClient`, writing the lines in order.
  Tests were red first: the writer and the runner threw `NotImplementedException`, and `HostMessageEnumTests` did not find
  `EvalMessage` and `EvalLogMessage`. The writer's lines are checked against the Python's own output, generated by
  `protocol.emit` with the clock fixed (`Fixtures/python-protocol.txt`): each one parses through `BenchmarkProtocol.Parse`
  to the same record. The shapes not yet in: `SessionReplay` (step 4a),
  the graders' calls in `EvalRunner` (step 3, so each trial's scores are the eval's with no grades), `HttpSessionClient`
  (step 4), and `RescoreAsync` (5b). The protocol's extra fields (`puls_calls`, `hosts`, `errors`) come with the cook and
  routing evals (step 5), so a turn here carries the answer, the role and the seconds only.
  Review of step 2: the session is closed in a `finally`, so a turn that throws (a timeout, later) doesn't leave it
  open, and a test checks it (the close, removed, fails it). A content block delta without its `delta` no longer throws.
  The runner logs through core's `Log(LogLevel, Enum, values)`; `NoMessages` is a log line, so `EvalMessage` (empty
  until a person reads a text) is gone and `EvalLogMessage` has it. A close that fails in the `finally` is logged as
  `CloseFailed` (a warning), not thrown, so the turn's own error is the one the run reports (the Python swallows it too,
  `routing/run_check.py:96-100`); a test has both the send and the close throw.

- **Step 2a** (`evals/louis-agent.evals`, `src/louis-agent.api/Benchmarks`): the run records what it ran. The protocol's first line carries the eval, its version, the commit (`EvalBuild.Commit`, stamped at build with `-p:EvalCommit=…`, `unknown` when absent), its graders and the model each mode ran on, which is `unknown` until step 4 reads the session's settings. The conversation line carries its `grades` and `outcome` (none until step 3: `grades` is `[]` and `outcome` is absent). `BenchmarkProtocol` reads both, the recorder keeps them on the run and the conversation, and both stores write them. Tests red first: the parser and the writer's `Begin` and grades tests failed on the old code, and the store contract test passes in memory; the Mongo mapping is written but not run here (`BENCHMARKS_MONGO_URL` is not set). The three leftovers are done too: the trend lines are per mode and model (a run on another model is another line, named with its model; a run that named none keeps the mode's name); a ledger row on a model the run didn't declare is named in the run's log (`RunModels.Unlisted`, ignoring `unknown`); and the admin portal's runs list has a model picker (`RunModelFilter`, in `louis-agent.puls/Admin/`, the page only calls it; the page's `@code` is on the pending list until step 7b).

  Review of step 2a: the models are a list of `{mode, level, model}`, one entry per level a mode runs at. Only the
  mode's own level (`agent`) is written so far, with `unknown` as the model; the specialist, minion and grader levels
  and their models come with step 4, which reads the session's settings. The outcomes and the grader kinds are enums in
  the API (`BenchmarkOutcome`, `BenchmarkGraderKind`), parsed once where the protocol line enters with `SettingChoice`;
  the writer takes `TrialOutcome` and `GradeOutcome`. The three checks listed above (the model filter, the trend split,
  the ledger flag) were built in 9942318; step 4 re-checks them once the models are real. `Started()` is kept only for
  the parity test against the Python's first line; a run writes `Begin`. Nothing stamps `-p:EvalCommit` yet, so every
  run says `unknown` until the runner's image is built in step 4, which stamps the commit.

  Second review of step 2a: the level is an enum (`ModelLevel` in the harness, `BenchmarkModelLevel` in the API: Agent,
  Specialist, Minion, Grader), and a specialist's role is its own field, so two specialists are both Specialist. An
  unknown model is null, not a string, as an unpriced cost is null: the readers skip null. Step 4 must declare every
  level of a run at once, the agent, each specialist and minion role it calls and the grader; declared one at a time,
  the flag fires on every specialist and grader row as soon as the agent's model is real. Nothing is unknown but the
  models themselves until step 4 reads them from the session.

- **Step 3.1** (`evals/louis-agent.evals.puls/Scoring`, the tests in `tests/louis-agent.evals.puls.tests`): the shared
  scoring rules of `scoring.py`, split by concern: the shops and their facts (`Shops`), the links and whether a price is
  sourced (`Links`), the shopping list's tables (`Lists`), and the price rules (`Prices`, with `PriceCount`). The 27
  tests of `test_scoring.py` are ported with the Python's expected values. This is a port of rules that already pass in
  Python, so 3.1 has no red run of its own. To see that the tests hold, the own-shop check in `Links.Sourced` was broken
  on purpose: two tests failed, the two about another shop's link (`AProductLinkedToAnotherShopOrARecipe_IsNoSource` and
  `AnotherShopsLink_IsNoSourceForThisShopsPrice`), and all 27 pass again once it was put back. The one new behaviour,
  the no-shop rule, is step 3.3's, and its red test comes there. Where the C# checks a guess, it rebuilds the row from
  its cells, which the rules read the same way (only pipes and spacing differ). Uri is stricter than Python's `urlparse`
  on odd URLs; nothing in the recordings hits that, and step 5's scores-equal check would catch it.
- **Step 3.2** (`evals/louis-agent.evals.puls`: `Transcripts`, `Cook`, and the tests in
  `tests/louis-agent.evals.puls.tests`): the turn's reader (`TurnView.From` over the session's typed events, the
  Python's `read_turn`), the hosts a turn read (`ToolHosts`, the Python's `hosts`), the cook's score function
  (`CookScorer.Score`, with the score record `CookScores`, whose properties are the Python's keys) and the setup the
  tests read (`CookSetup`: the two modes and the places of two conversations). The 18 tests of `test_cook.py` are ported
  with the Python's expected values; two of them read the setup, which holds only what they need: the full list of
  conversations comes with the cook eval in step 5. The turn reader has two tests of its own, since it's new code that
  no Python test covers. The cook's behaviour is the Python's as it is today: the no-shop rule changes in step 3.3.
  Checked by a mutation: the cook is made never expected, and two tests fail (the handed-to-the-cook one and the
  answered-itself one); with the rule restored, all 47 pass. No red run for the port itself, as in 3.1.
- **Review of step 3.2:** a specialist's error is the specialist's, not the turn's, so the reader reads an error only
  from the turn's own events, as the Python does (a test, which fails when the guard is removed). A tool result whose
  content is not a string, and an error that is a string rather than an object, no longer throw: they're read as their
  JSON. An empty role, handle or error is none, as the Python's truthiness reads it, and an empty role falls through to
  `personality`. The cook's fixed-rule graders are in: `CookRoutedRight` (required: the route) and `CookNoError`
  (required: no turn failed), both `FixedRule`, which step 4's eval sets need. They were written first and failed on the
  red run. The priced and no-shop grades are not graders yet: their thresholds are for the cook eval (step 5) to decide,
  not invented here.
  The cook is on when a mode has the specialists toolset, as the Python decides it (`CookScorer.CookOn`), whatever the mode is named, and both graders score a trial through one helper (`CookScorer.ScoreTrial`). A test with a mode named `puls-cook-sonnet` checks it.
- **Step 3.3** (`evals/louis-agent.evals.puls`: `Routing`, `Grading/FixedRules/RoutingRoutedRight`, and the tests in
  `tests/louis-agent.evals.puls.tests`): the routing's score (`RoutingScorer`, with `RoutingScores`), its setup
  (`RoutingSetup`: the kinds and their roles, the two modes' toolsets, the twelve questions' tasks; their texts come
  with the routing eval in step 5) and its grader (`RoutingRoutedRight`, required). The 10 tests of `test_routing.py`
  are ported with the Python's expected values. The one-key rule is the one behaviour change in this step, and it
  changes the cook's score, not the routing's: in a town with no shop a priced row is `wrong_store` with or without its
  link, and an unlinked price is `wrong_store` too, so `unsourced` is 0 there. The red test was written first and failed
  on today's numbers (1 and 1, where the rule gives 2 and 0); after the change all 70 Puls tests pass. This is the one
  case where the C# scores differ from the Python's, as the plan asks: the Python keeps the old rule until its checks
  are retired. Checked by a mutation: the routing's expected role is made null, and three tests fail (the two about a
  meal, and the grader); with the rule restored, all 70 pass.
- **Review of step 3.3:** a mode is delegating when it has the specialists toolset, as `routing/run_check.py` decides
  it, so a minions-only mode is not (a test, red first). The score records' key sets are held by tests (the cook's 27,
  the routing's 9), which fail on a renamed property; the shared snake_case options are now `EvalScores.Json`, public,
  for the runner and the tests. The cook's manual (`benchmarks/cook/HANDLEIDING.md`, the `wrong_store` and `unsourced`
  rows) describes the one-key rule. A grader's message reads "handled by {role}" or "answered by Puls itself". Step 5's
  `CookEval` starts at a new version, so the trend never joins runs made under the old rule with runs made under the new
  one.
- **Review fixes before step 4:** the graders' reasons are a message enum (`GradeMessage`, ADR 0009); `IEval`, `EvalMode`
  and `EvalScores` sit in the harness root (`LouisAgent.Evals`), `EvalScenario` and `EvalInput` in `Datasets/`, and the runner
  in `Running/`; `evals` is on the file-size ratchet; the grader tests sit in `Grading/FixedRules/`. No behaviour changed.
  Core (1290), API (174), Puls (76) and harness (9) tests pass.
- **Step 1d** (`louis-agent.orchestrator`): `LouisAgentClient.ReadStreamAsync` takes the response's stream and reads its
  events through the reader, folding them by their record types; the framing loop is gone. Its tests feed the same SSE
  text as bytes instead of a `StringReader`, with no assertion changed.
  The orchestrator references the reader directly, as it reads the stream itself (the core reference alone is transitive). The file was already on the size ratchet
  (`FileSizeTests`) and is now shorter (216 to 206 lines).
- **Commit 1, the harness's namespaces** (no behaviour change): `IEval`, `EvalMode` and `EvalScores` sit in the root
  (`LouisAgent.Evals`); `EvalScenario` and `EvalInput` in `Datasets/` (`LouisAgent.Evals.Datasets`), ahead of the loader; the
  empty `Evals/` is gone. The tests mirror it: `EvalRunnerTests` in `Running/`, `ProtocolWriterTests` in `Protocol/`.
- **Step 3a.1** (`evals/louis-agent.evals/Datasets/`, its tests in `tests/louis-agent.evals.tests/Datasets/`): the loader reads
  `dataset.json` and `scenarios/{dev,held-out}/*.json` strictly, with System.Text.Json and `UnmappedMemberHandling.Disallow`,
  so no schema library is needed (ADR 0030): an unknown field, a `split` written into a scenario, or a missing required field
  is refused. The split is the folder's name, read once with `SettingChoice` (ADR 0024). The loader refuses a duplicate id,
  an `expected` key no grader reads (`IGrader.ExpectedKeys`, a required property: each grader names what it reads, and the
  three Puls graders and the fake read none, so the Puls scenarios' keys come with step 5), a folder whose hash differs from
  `content_hash` (the message names the hash to paste), and a folder that is neither dev nor held-out. `DatasetHash` is
  SHA-256 over `scenarios/` only, files in ordinal path order, each path and its content followed by a NUL, with CRLF read
  as LF so a Windows checkout hashes like Linux. Its pinned value comes from an independent Python run of the same rule.
  Red first: 11 tests failed on the stubs, all with `NotImplementedException`. Mutations checked: removing the CRLF fold
  fails the pinned-hash test and the CRLF test; removing the expected-key check fails the refusal test.
  The `Datasets/` folder holds seven files: the plan's six and `DatasetHash`, which the plan allowed as its own class. Under
  the folders rule's "about six", it is flagged for Louis's review rather than split into subfolders.
- **Review of step 3a.1** (commit `bf297a5`'s review, 2026-10-24): each finding is a red test first, then the fix.
  The strict read now refuses a missing constructor parameter and a null where the type isn't nullable
  (`RespectRequiredConstructorParameters`, `RespectNullableAnnotations`), so a missing `time_zone` or `"categories": null` fails
  at the file, not in the run. The loader reads the same files the hash covers, so a file under `scenarios/` is loaded or
  refused: outside `dev/` or `held-out/`, nested, not `.json`, or not lowercase (checked ordinally, never by the file system).
  A missing `dataset.json` is `NoManifest`, not a bare `FileNotFoundException`. `changes` must hold one dated line per
  version, from 1. The limit: that checks the count, not the history. Whether a bumped version came with its line, and
  whether the line describes the change, needs the base branch's `dataset.json` (later, with the release check). The
  `HashChanged` text says what was found: the scenarios hash to one value, not the recorded `content_hash`. Split names
  stay case-insensitive, as ADR 0024's parser is; a folder `Dev` and a folder `dev` on Linux are not yet refused. The JSON
  options are `EvalJson.SnakeCase`, shared with the scores.
  A later review (before 3a.2) found the hash compared before the files were read, so a stray file such as `desktop.ini`
  read as a change to bump. The files are now read first; `AStrayFileIsRefusedBeforeAHashChange` is red before the fix.
- **Step 3a.2** (`evals/louis-agent.evals.puls/Datasets/`: `puls-cook`, 6 scenarios, and `puls-routing`, 12; their tests in
  `tests/louis-agent.evals.puls.tests/Datasets/`): the scenarios the Python checks ran, as files. Each `input` is the Python's:
  the test compares every one with `Fixtures/python-scenarios.json`, which an `ast` read of `benchmarks/cook/run_check.py` and
  `benchmarks/routing/run_check.py` wrote (nothing imported or run). The loader accepts each dataset with Puls's own graders,
  and the hash in `dataset.json` is computed by an independent Python implementation of the rule. The datasets are copied
  beside the Puls assembly and beside the tests. Red: 7 tests failed while the folders were absent. Green: 83 of 83.
  Decisions (approved by Louis on the review, recorded under the plan's *Approved during the build*; held-out is new
  scenarios in step 5):
  - Every scenario is `dev`, and every Python run used all 18, so the prompts were tuned on every one of them: moving some
    into `held-out/` would not make them unseen. The held-out set is new scenarios, written in step 5; `held-out/` stays
    absent until then, since an empty folder can't be committed.
  - `categories` are decided by hand from the Python's own terms: cook `language:af|en`, `budget`, `follow-up`, `shop:none`,
    `control`. Routing's are `kind:{kind}`, which the plan doesn't name.
  - `expected` is `{}` everywhere. The graders read no keys yet, and the loader refuses a key no grader reads; the keys
    come with the graders that read them in step 5.
  - The loader orders scenarios by file name, not the Python's order, so the tests compare sets.
  - Carried to later steps (review of 3a.2): step 4's how-to explains re-hashing with C# (`DatasetHash`), not with the
    Python script; step 5 deletes `CookSetup.Places` and `RoutingSetup.Tasks`, which now repeat what the datasets hold; step 7
    retires the Python parity tests.
- **Step 4.1** (`evals/louis-agent.evals.runner/`, its tests in `tests/louis-agent.evals.runner.tests/`; `IEvalSet` in the
  harness root, `PulsEvalSet` in `evals/louis-agent.evals.puls/`): the runner's catalogue (`EvalCatalog`) lists the chosen sets
  and the stub, each name once, and refuses an eval with no graders, with only model or human graders, or with no required
  fixed rule. `EVALS=` is an `EvalSetKind` enum parsed with `SettingChoice`, as `IDENTITIES` is. `run {name} --repeat N` is
  clamped to 1..5 with the API's own constant (the two must match). `--list` writes one JSON object per line, and stdout is
  the protocol and nothing else: refusals and the run's progress go to stderr. The stub is the Python stub in C#, two modes,
  two conversations, with its events and session ids the Python's. Red first: the runner's 27 tests failed on the stubs, the
  enum test failed with `Missing items: RunnerMessage, StubMessage` (its folder moved aside to show it), and the protocol
  writer's three key-order tests failed before the writer wrote the fields.
  Parity: the Python stub's `@` lines are saved as `Fixtures/python-stub.txt` (free, no model). The C# run matches them line
  for line, with two stated masks: `t` and `seconds` are clock readings, and the header is left out, since the Python's first
  line is the version alone and the runner's carries the eval, its graders and its models, which the store needs. Mutation:
  one canned fact (`specialist_calls` 3 to 2) fails the parity test at the turn line.
  Decisions approved by Louis on the review of `6d26a1a` (2026-10-24):
  - The protocol gains what the API reads: the mode's `conversations`, the turn's `continued`, `puls_calls`,
    `specialist_calls`, `hosts` and `error`, the conversation's `errors`. The eval says the turn's facts (`IEval.TurnFactsOf`,
    a new member); the runner takes the turn's role from them and no longer falls back to the mode's. Step 5's Puls evals derive
    them from `TurnView`. The stub's are canned (its hosts and continuation are constants, as in the Python).
  - `grades` is left out of the conversation line (it was `[]`): the graders don't run in the runner until 4.2 and step 3.
  - The stub brings its own session client (`StubSessionClient`), not the harness's `CannedSessionClient`: the Python's session
    ids and events depend on the mode, the repeat and the task, which the canned client, one id and its turns in order, can't
    give. `SessionRequest` gains the `Repeat` for it.
  - `--list` escapes an apostrophe as `\u0027`: valid JSON, read back the same by the API. Its refusals go to `Console.Error`,
    since stdout carries `--list`'s JSON lines and the protocol. The Python stub's turn `seconds` is the constant 0.1, not a measure,
    which is why the parity test masks the seconds.
  - A mode whose eval has no session client is refused until step 5 brings the HTTP client; only the stub runs now.
  Review fixes (the same review, 2026-10-24): the run's name is `BENCHMARK_RUN` (default `local`, as the Python reads it), so
  every session's tag, and the ledger's rows, carry the run (`BenchmarkRunTests`, `EverySessionIsTaggedWithTheRunsName`); the
  catalogue refuses an unknown name (`EvalCatalog.Get`, tested), and the host only calls it; the stub's cook reply is the
  `StubMessage` member it was always meant to be (the inline copy is gone); `TurnFacts.PulsCalls` is `AgentCalls`, with the
  protocol's key `puls_calls` unchanged, since the API reads that name. The stub's host (`www.checkers.co.za`) and its
  time zone are data, not text, and stay literals.
- **Step 4.2a** (`evals/louis-agent.evals/Grading/Common/`, `Trials/EngineEvents.cs`, the tests in
  `tests/louis-agent.evals.tests/Grading/Common/`): the four common graders and the one reader of the engine's events they share.
  `NoErrorGrader` passes when no turn streamed an error event or ended in a failure. `WithinTimeGrader` passes when every
  turn's duration, by the harness's clock, is within the limit. `CostUnderGrader` sums the turns' costs and passes at or under
  the limit; an unknown cost is Unknown, never a pass. `RoutedToGrader` passes when the first turn's first `StartSubSession`
  call went to the role, and fails when it went elsewhere or the agent answered itself. Each takes a `required` flag (default
  true). A grade's score is 1 for a pass and 0 for a fail or an unknown, so the score agrees with the outcome.
  Red first: the 21 tests that call a stub failed with `NotImplementedException`, and the enum test failed with
  `Missing items: CommonGradeMessage`. Mutation: an unknown cost made a pass fails two cost tests.
  Decisions approved by Louis on the review (2026-10-25):
  - The cost is the sum of the turns' `usage.cost` from the stream, which is the ledger's total per turn; no price is worked out
    in the harness. A turn with an unpriced request has no known cost (the stream's `unpriced` count is above zero).
  - The route is the first turn's first `StartSubSession` role, the engine's own tool, and not the `session_role` the stream
    reports on message_start: "went to role X" is a call to X, as Anthropic's tool-call grader checks.
  - `EngineEvents` is the one reader; a grader does not parse the stream itself.
  Review fixes (2026-10-25): `RoutedToGrader` takes the expected role per trial (`Func<Trial, string?>`; null means the agent
  answers itself, as Puls-only's cook mode expects), and a turn that streamed an error fails the route, as the Python check does.
  An infrastructure failure (a timeout, a dropped connection: `TrialFailure`) is reported apart: `Verdict.InfrastructureFailure`
  makes it Unknown in all four graders, never a Fail. A specialist's error, its own hand-over and its usage are not the turn's:
  the reader reads the top-level events only, and each of those is pinned by a test (one mutation of the error rule fails two).
  `EngineEventsTests` moved to `Trials/`, with `TrialBuilder`, to mirror the source.
- **Review of step 4.1, leftovers** (2026-10-25): a scenario with no messages is refused, not skipped: the catalogue refuses
  it when it is built (`RunnerMessage.NoMessages`, tested), and the dataset loader refuses it when the file is read
  (`EvalErrorMessage.NoMessages`, tested). The runner's own skip stays as a guard, now unreachable from a catalogue. The
  runner's summary no longer says the graders join in step 3: it writes no grades yet, and the conformance test runs the
  graders (4.2b).
- **Step 4.2b** (`evals/louis-agent.evals/Conformance/`, `Protocol/ProtocolTranscripts.cs`, the tests in
  `tests/louis-agent.evals.tests/Conformance/` and `Protocol/`, and `tests/louis-agent.evals.runner.tests/Conformance/`): the
  conformance check (`EvalConformance.CheckAsync`), and the reference transcripts it reads. The shape: a name safe as an MCP tool
  name (`^[A-Za-z0-9_-]{1,64}$`), a version of at least 1, a required fixed rule, every expected key read by a grader. Every
  grade: a reason, a score from 0 to 1, an outcome that agrees with the score. Every scenario: a reference transcript that passes
  its required graders, and a known-bad one that fails them. A grader that throws is reported, not rethrown. The runner's
  `EveryEvalOfEverySetConforms` runs it on the stub, on the empty Puls set and on a fake set (Puls's evals join in step 5).
  Red first: the three protocol-reader tests, the 12 checker tests and the runner's two conformance tests failed on their stubs,
  and the enum test failed with `Missing items: ConformanceMessage`. Mutations: a no-error grader that passes everything fails
  the runner's conformance test with `the known-bad transcript passes every required grader`, and the stub's grader that passed
  everything is gone.
  Decisions to review:
  - The reference transcripts are the Python stub's own protocol lines, read back by `ProtocolTranscripts`. The known-bad ones
    are the same conversation wrong in one way each (see the review note below). A scenario with no reference fails the check
    (`NoCase`), so a new eval brings its references.
  - The stub's graders are now the common no-error grader and the within-time grader at one minute, both required. The stub's
    own grader, which passed every trial, could not fail a known-bad transcript, so the check caught it; it is removed.
  - The turn line's `error` field is not read by `ProtocolTranscripts`: the error is the sse line's error event, and reading the
    turn's field too would count it twice.
- **Review of step 4.2a, nits** (2026-10-25): the no-error reason carries the stream's error message (`ErrorInTurnWithMessage`), so a
  person sees why. `EngineEvents` never throws on an odd event: a content block with no name, a block that isn't an object, a usage
  count that isn't a number, all read as absent (no route, or an unknown cost). Each has a red test first.
- **Review of step 4.2b** (2026-10-25): the conformance cases are files beside the eval, `Transcripts/{eval}/` (the stub's in the
  runner, the fake set's in the runner's tests, Puls's in step 5): `{scenario}.reference.txt` and `{scenario}.bad.{label}.txt`, read by
  `ConformanceCases.Load`. The known-bad transcripts are no longer one error-only transcript: each required grader must FAIL at least one
  of them (an Unknown is not a fail), and no known-bad may pass every required grader. The stub's known-bads are the error (no-error
  fails) and a first turn of 61 s (within-time fails). Shape checks added: grader names unique, grader versions at least 1. The
  runner's conformance test is one test per eval (`TestCaseSource`), so each eval shows on its own line. Red first: 22 harness
  tests and 3 runner tests failed on their stubs. Mutation: a within-time grader that never flags a slow turn fails the runner's
  conformance test with `no known-bad transcript fails the required grader 'within-time'`.
  Decisions to review:
  - A known-bad that's a file beside the eval, not code in the test: the references are data, reviewed as data.
  - The check is a function in the harness that any eval set can call (a package user's check), and the runner only runs it.
- **Review of step 4.2, commit 1** (`4d0aa6b`, 2026-10-25): the known-bad rule is per eval (Louis, 2026-10-25): each required grader
  must fail at least one known-bad of any of the eval's scenarios, and only a `Fail` counts; a scenario may have none when the others
  cover every grader. Every scenario still needs its reference, and no known-bad may pass every required grader. `ConformanceCases.Load`
  refuses a stray file (a misspelling, a deleted scenario's files, a known-bad with no reference; `EvalErrorMessage.StrayTranscript`).
  Grader names are checked as MCP tool names, as the eval names are. A blank `StartSubSession` role is no route. The cost reads three
  reasons apart, each Unknown with its own text: no usage, a request with no price, a currency other than USD; the cost grader's limit is
  `limitUsd`. The run's name is `local-{yyyyMMdd-HHmmss}` from the clock's local time, as the Python checks name it
  (`BenchmarkRun.Of(value, clock)`). The runner's comment names the step that reads the model: none yet. Red first: 17 tests.
- **Step 4.3a** (`src/louis-agent.api/Benchmarks/Runs/`, `Messages/EvalListingMessage.cs`, the tests in
  `tests/louis-agent.api.tests/Benchmarks/Runs/`): the API lists the evals with `dotnet {EVAL_RUNNER} --list`, once, on the first read of
  the catalogue's `Checks`, as an argument list with a 30 s timeout. Each line is read as the API's own `EvalLine` (the runner writes
  snake_case: `name`, `identity`, `version`, `proves`). A listing that can't be read (the file missing, a timeout, a non-zero exit, a line
  that isn't JSON) is a warning (`ApiLogMessage.BenchmarkListingUnavailable`) and the Python checks stay. A listed eval hides the Python
  check with its id; the rest are the Python checks until steps 5 and 7. An eval check has no script: its manual is
  `benchmarks/{id}/HANDLEIDING.md` until step 7 moves the manuals beside the evals, and its settings record `eval` and `eval_runner` in
  place of `script` and `arguments`. The runner starts with `dotnet {runner} run {name} --repeat N`, under the same environment as a script
  (`PULS_API`, `BENCHMARK_RUN`). The parity test runs the Python stub and the C# stub through the recorder and compares what each stores,
  key for key, with the times stripped; the store writes PascalCase keys, so the strip matches names whatever their case. `EVAL_RUNNER`
  defaults to `/app/evals/louis-agent.evals.runner.dll`, and `launchSettings.json` and `config/.env.example` carry it. The API references
  no evals project (ADR 0027): the test project references the runner with `ReferenceOutputAssembly="false"`, so the tests list and run
  the real dll. Red first: 15 tests, each failing on the missing behaviour. Open: the admin endpoint still shows each check's `script`,
  blank for an eval; left as it is until step 7b makes every eval visible in the admin portal.
- **Review of step 4.3a** (2026-10-25): `EVAL_RUNNER` is made absolute where it enters (`Path.GetFullPath` in `AdminOptions`),
  because a run's working directory is the workspace root, where a relative path from a Rider debug run didn't resolve; the settings
  record the absolute path. The listing's lines are read by `EvalListing.Parse`, which is pure, with System.Text.Json's required and
  nullable rules on: `{}` and a null name are refused, not listed with no id (`EvalListingMessage.NotAnEval`). The API reads the listing
  at start-up (`BenchmarkCatalog.Warm`, off the start-up path), so a warning lands in the start-up log; a failed listing lasts until a
  restart. An eval check's settings take the runner path as a required parameter of `BenchmarkSettings.Of`, so the compiler refuses a missing
  one rather than a guessed default (ADR 0007).
  `Benchmarks/Runs/` has 12 files: ADR 0010's split by concern is left for step 7b, which moves the Python checks and the evals'
  folders, and is not decided here. Red first: 9 tests for these fixes.
- **Step 4.3b** (`docker/images/Dockerfile.api` and `Dockerfile.puls`, `config/.env.example`, the docs): both images publish the
  runner in the build stage to `/app/evals-publish` and copy it to `/app/evals`, its own folder, because it ships its own copies of
  core and the session-stream reader. The publish carries the Puls datasets (`puls-cook`, `puls-routing`). Images, before and
  after: `louis-agent-puls` 1.47 GB to 1.53 GB, `louis-agent-api` 1.47 GB to 1.53 GB (the runner's own dlls and the datasets). By
  hand, against the rebuilt Puls container (`docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build`):
  `dotnet /app/evals/louis-agent.evals.runner.dll --list` printed the stub's line; the start-up log had no
  `BenchmarkListingUnavailable` warning; the admin endpoint listed the stub with its `proves` as the title; the stub run finished
  with exit 0 and 109 lines; the store returned the run with `eval: stub` and `eval_runner: /app/evals/louis-agent.evals.runner.dll`.
  The API image was built with `docker compose -f docker/docker-compose.yml --env-file config/.env build api` and not started, so the
  code-review app kept its container. On Git Bash, `docker exec` needs `MSYS_NO_PATHCONV=1`, or `/app` is rewritten to a Windows path.
- **Step 4.4 prep** (`evals/louis-agent.evals.puls/Turns/`, `tests/louis-agent.evals.puls.tests/Turns/`): Puls's turn readers
  (`TurnView`, `ToolHosts`) moved out of `Transcripts/`, so that folder holds only reference transcripts, in every evals project (ADR
  0010). The namespace is `LouisAgent.Evals.Puls.Turns`. The conformance test builds every `EvalSetKind` and the fake set, so a new
  set is checked with no edit to the test. A refactor: the tests were green before and after, with nothing to see red. The step 3.2
  note above names the old folder as it was then.
- **Step 4.4** (`docs/guides/add-an-eval-set.md`, the two index rows, the link in `change-the-api.md`): the guide's example is `acme`,
  not Puls, because Puls's set is empty until step 5. Each step names the file that does the same thing today. Checked by building,
  outside the repo: the guide's grader, eval, set and `Scenarios` line compile against the harness; a dataset with a placeholder hash is
  refused with the hash it needs, loads with that hash, and refuses an unknown field; the example's reference passes the three required
  graders, and its `wrong-area` known-bad fails the area grader. What writing it found, against the plan and the code:
  - `IGrader.GradeAsync` returns `Grade`, not a `Verdict`. `Verdict` is internal to the harness, so a grader builds its `Grade` directly,
    as the Puls graders do.
  - The `Scenarios` initialiser can't read an instance `Graders` property. The example keeps its graders in a static list; the first
    draft of the guide had this wrong, and building it found it.
  - No eval loads its dataset from its folder yet. `StubEval` keeps its scenarios in code, the Puls tests load theirs with
    `DatasetLoader`, and step 5's evals will load theirs the same way; the guide documents that path.
  - The runner doesn't select held-out scenarios yet; the split is recorded only.
  - A dataset that fails to load surfaces at the catalogue's build as an unhandled exception, since the runner catches only its own
    refusals. The portal's listing warning then shows only the exit code, because the API reads the runner's stdout, not its stderr.
    Open: the runner could refuse with its own message, or the API could read its stderr.
  - The spec's "every project links its docs" is not true of the evals projects: the identity and pack projects do, and the evals
    projects don't. The guide asks for the link; the evals projects are unchanged.
  - `TurnView`, the answer reader, lives in Puls's set, so an identity's grader reads the answer itself. Two sets sharing one reader
    should move it into the harness.
  - The first draft's example had two messages, and its reference didn't match its expected answer. The example now has one
    message, and the check above holds for it.
  - `NoErrorGrader` judges the stream's errors only (an infrastructure failure is `Unknown`), and `RoutedToGrader` judges the first
    turn only. The first draft said otherwise; the guide now says what the code does.
- **Notes for step 5** (from the review of 4.2): `PulsEvalSet`'s `TurnView` builds on `EngineEvents`, so the route, the errors and the
  cost come from one reader. If an expected route goes in a scenario's `expected`, `RoutedToGrader` must declare that key in its
  `ExpectedKeys`. When the HTTP client sets a turn's `Failure`, the turn line must carry it, so `ProtocolTranscripts` reads it back. A
  reference may need its mode in its file name (`{scenario}.{mode}.reference.txt`) when an eval's modes expect different routes.

---
[Features](README.md) · Previous: [F17 Orchestrator agent](F17-orchestrator-agent.md)
