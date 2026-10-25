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
- **4.4** The developer's how-to, `docs/guides/add-an-eval-set.md`: its example checked against the code, and what writing it found (the build notes), and the review's three gaps fixed
- **4.5a** The runner grades each trial: every grader of the eval (`TrialGrading`), the outcome (`PassRates.Of`), both on the conversation line (approved during the build)
- **4.5b** The admin rows carry the standard records: the run's eval, version, commit and graders; a conversation's grades and outcome; null from before 2a
- **4.5c** Every eval's grades in the portal, from one template: `GradeChips` and `GraderList`, and the `PortalReadsNoScoreKeyTests` ratchet
- **4a** Web input recorded and replayed: the browser
  - **4a.1** The replay store and the recorded browser (core only; nothing uses them yet)
  - **4a.2** Sessions record and replay the browser: the settings, one browser per process, `replay` on `POST /sessions`, sub-sessions
  - **4a.3** The runner's `--web record|replay` and `--of`
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
- **Review of step 4.4** (2026-10-25): three gaps fixed, and the guide uses the fixes. `Verdict` is public: a grader in an identity's own
  project builds its grades with it (ADR 0030), so the 4.4 note's Verdict bullet above is superseded. `EngineEvents.AnswerOf` reads the
  turn's own answer as `TurnView.Answer` does; a parity test keeps the two readers on one rule until step 5 makes `TurnView` build on
  `EngineEvents`. The runner refuses with the harness's refusals as it does its own (`RefusalReport`: an `InvalidDataException` or a
  `RunnerException`): the reason goes to stderr with exit code 1, so a dataset that won't load shows its reason, not an unhandled
  exception. The API's listing reads both pipes, and a refusal's last stderr line is the warning's reason (`ExitCodeWithReason`); a
  non-zero exit with nothing on stderr keeps `ExitCode`. The three evals projects and their test projects link the guide. `HashChanged`
  says what to do both when a scenario changed and when the hash is a placeholder. Red first: five harness tests (four for `AnswerOf`,
  and the `HashChanged` pin), four runner tests, two API tests red and a third green (it pins the existing no-stderr message), and the Puls
  tests: the visibility test didn't compile until `Verdict` was public (CS0122), and the parity test failed on the stub when run alone.
- **Step 4.5a** (`evals/louis-agent.evals/Running/TrialGrading.cs`, `Grading/PassRates.cs`, `EvalRunner`): the runner grades every
  trial with the eval's graders, in the eval's order, and writes the grades and the outcome on the conversation line; `ScoresOf` gets
  the grades it was always declared to take. A grader that throws is one Unknown grade with its reason (`CommonGradeMessage.GraderThrew`)
  and one warning line (`EvalLogMessage.GraderThrew`: the eval, the grader, the scenario, the reason), and the run goes on; a cancel still
  stops it. The conformance check keeps its own loop: there a throw is a conformance failure with no grade, here an Unknown grade.
  `PassRates.Of` is the outcome rule: only required graders count; Fail beats Unknown, Unknown beats Pass, a required grader with no
  grade or no required grader at all is Unknown. The turn's answer now comes from `EngineEvents.AnswerOf`: the runner's own loop called
  `GetString()` on each delta's `text`, which throws on a number, so one odd delta stopped a run; the reuse fixes it. Two parity tests
  changed: `StubCheckTests.TheTwoStubs_StoreTheSameModesConversationsAndTurns` and `StubEvalTests.ARunIsThePythonStubsLineForLine`
  now leave the conversations' `grades` and `outcome` out, because the Python stub never wrote them. Red first: six `PassRatesTests`,
  three `TrialGradingTests` and three `EvalRunnerTests` (`NotImplementedException` from the stubs, the `InvalidOperationException` of
  the number text, a missing grade in `ScoresOf`), and the stub check's new assertion (`Grades` was null).
- **Step 4.5b** (`src/louis-agent.api/Admin/AdminRows.cs`, `tests/louis-agent.api.tests/Admin/AdminRowsTests.cs`): the row builders
  left `AdminEndpoints.cs` (232 lines, on `FileSizeTests`' list) for `AdminRows`, an internal static class the endpoints call through a
  `using static`; the move sends the same bytes, and the existing tests stayed green. The run row gains `eval`, `eval_version`, `commit`
  and `graders` (`{ name, version, kind }` in the run's order); the conversation row gains `grades` (`{ grader, version, score, outcome,
  reason }` in the stored order) and `outcome`. Enums are written with `ToString()`, as `models[].level` already was, so `FixedRule` and
  `Pass` run from the runner's line through the store to the portal unchanged; a run from before 2a sends null for each, never `[]`
  or 0. `AdminEndpoints.cs` is 195 lines and off the list (the 4.5b commit message says 176; 195 is right, counted by `FileSizeTests`' rule). Red first: the four `AdminRowsTests` failed with `KeyNotFoundException`
  (no such key on the row). The tests serialize with `JsonSerializerDefaults.Web`, which is what `Results.Json` uses with no options.
- **Step 4.5c** (`src/louis-agent.puls/Admin/Grades/`, `Components/Admin/Grades/`, `tests/louis-agent.web.shared.tests/PortalReadsNoScoreKeyTests.cs`):
  every eval's grades in the portal from one template. The readers (`RunGradersReader`, `ConversationGradesReader`) read only the
  standard records the admin rows send; `GradeOutcomes.Parse` is the one place an outcome enters (case ignored, an unknown word is
  `Unknown`, never a number: the web app has no `SettingChoice`, so it is a three-arm switch); `GradeChipList.Of` orders the chips by
  the run's graders, an unlisted grader last, and gives one "no grades" chip with no kind where there are none. `GradeChips` sits
  beside the Puls chips on each conversation's row (one tag, a `Grades` field on `ConversationNode` and a `_graders` field, both
  filled in `Build()`); `GraderList` sits above the explorer (one tag). Neither folder names anything of Puls, held by the ratchet's
  second test. `Admin/Grades/` had eleven flat files, over the "about six" of ADR 0010; the review below sorts them. Red first: the ratchet with two of
  its three files listed (`BenchmarkTrends.cs` as a new offender), 15 NUnit tests on `NotImplementedException`, and the four bUnit
  goldens with no file; the goldens were then generated and read (the outcome chip first, then the grades in order with their
  reasons as titles; a `<p class="muted">` for a run from before 2a).
- **Review of step 4.5c** (2026-10-25): `Admin/Grades/` sorted by concern (Louis: eleven flat files mixing records, readers and chip logic
  break *Separate by concern*). `Grades/` keeps the records both components use and `GradesText`; `Reading/` holds the two readers and
  `GradeOutcomes.Parse`; `Chips/` holds `GradeChip`, `GradeChipList` and `ChipKind.Of`, moved out of `GradeOutcomes` because choosing a
  chip's class is the view's job. The tests mirror the folders. Two fixes: a grade's missing score or version, and a grader's missing
  version, read as null now (`double?`, `int?`), never 0 (ADR 0007), so the chip and the grader line show nothing for an unknown
  number; and the ratchet fails on a scanned folder that isn't there (step 7b renames `Benchmarks/`), instead of scanning it as empty.
  Red first: the no-score test read `Version = 0, Score = 0`; the ratchet with `Evals/Reports` in its list failed with "No folder".
  **By hand, the first stored run through the template** (the Puls image rebuilt with `docker compose -f docker/docker-compose.puls.yml
  --env-file config/.env up -d --build`, the C# stub started with `POST /admin/benchmarks/stub/runs`): done with exit 0, 109 lines,
  stored; `GET /admin/benchmarks/runs/{id}` answered `eval: stub`, `eval_version: 1`, the two graders `no-error v1 FixedRule` and
  `within-time v1 FixedRule` in that order, and all four conversations (`puls-only` and `puls-cook` × `bobotie` and `cottage-pie`)
  with the outcome `Pass` and both grades `Pass` at score 1. The run page, with the admin key in: `GraderList` above the explorer
  reads `eval stub · version 1`, `commit unknown`, and the two graders as chips (`no-error v1 · FixedRule`, `within-time v1 ·
  FixedRule`); on the Conversations level each of the four rows shows, under its Puls chip (`Puls itself ✓` or `puls-cook ✓`), a
  green `Pass` outcome chip and then `no-error` and `within-time`, both green, in the grader order. `commit` is `unknown` in the image: the Docker build has no git, so
  `EvalBuild.Commit` falls back (step 2a's "unknown when absent"); a build argument would carry it, left for step 7b.
- **Step 4a.1** (`louis-agent.core/Sessions/Replay/`, `Tools/Web/Replay/`): `WebReplayMode { Off, Record, Replay }` and `SessionReplay(Web, Of,
  Model)`, held in an `AsyncLocal` as `UserPlace` is, with `DefaultMode` parsing `WEB_REPLAY` once with `SettingChoice`; `WebReplayStore`
  (a page once per SHA-256 of its JSON bytes in `pages/`, one index line per visit in `trials/{safe name}.jsonl`, appended under one lock);
  `RecordedWebBrowser` around `IWebBrowser`. The key of an ask is its `AbsoluteUri`, script and ready selector, ordinal; `capture` and
  the cookies a visit starts with are not saved. A replay answers from the *first* recording and keeps no counters; a failed visit is
  recorded as its text with no hash and replays as the same `WebBrowserException`; a cancel is never recorded. A trial key reaches the
  disk only through `TrialFileName` (unsafe characters to `_`, cut to 100, then `-` and 12 hex digits of the key's SHA-256), and
  `TrialFileName.Under` refuses any path outside the folder. Two findings against the brief: `Tools/Web/Replay/` is at its six files
  (`ReplayedVisit` was folded into a tuple from `FindAsync`), so 4b's decorators need a subfolder or a split; and `ErrorMessage.cs` had
  room for one member, not three, so `UnknownWebReplay` is there and the two developer-facing exceptions are in a new `ReplayMessage`
  (`MessageEnumTests` pins core's enums by name, and its expected list gained that one name; the other way out was adding `ErrorMessage.cs`
  to `FileSizeTests`' oversized list, which only shrinks). The one golden change is the new `ToolMessage.ReplayMiss`; `ModelRequestSnapshot.json`
  is unchanged. Red first: 33 tests on `NotImplementedException` from the stubs. (The tuple did not last: see 4a.2's fold-in.)
- **Step 4a.2** (`louis-agent.core/Tools/Web/{Browsing,Replay}/`, `Config/AgentOptions.cs`, `Hosting/AgentEngineFactory.cs`,
  `src/louis-agent.api/Sessions/`): *Fold-in first* (Louis's review of 4a.1: the size rule splits folders, it does not weaken types).
  The store and its records moved to `Replay/Store/`, and `ReplayedVisit` is back as a record with two cases, `Answered(page)` and
  `Failed(error)`, and a `ToVisit()` that returns the visit or throws the recorded `WebBrowserException`; `FindAsync` returns it, and
  the browser no longer reads a pair with `Page!`. Then the settings (`WEB_REPLAY`, `REPLAY_DIRECTORY`, and `AgentOptions.KeepsRecordings`,
  the one thing the API may ask about the folder), `WebBrowserFactory` (the process's one browser, recorded when a folder is set),
  `ToolsetContext.Browser` falling back to it and `Renderer` reading through `Browser` (`PageRendererFactory.Create` takes it), and
  `AgentEngineFactory` handing the one browser to the one context, so the shops and `ReadRenderedPage` share it and its store. On the
  API: `ReplayRequest`, `CreateSessionRequest.Replay`, and `ReplayResolution.TryResolve`, which holds the seven rules and returns the
  reason of a 400 (`ApiUserText`, five new members) before any engine is built; `WEB_REPLAY` is parsed once in `MapSessions`, so a bad
  value stops start-up; `AgentSession.Replay`, and `TurnStream` sets it per turn beside the place. `SpecialistTools` and `MinionTools`
  send `replay` (`SessionReplay.ForSubSession()`: the web mode in lower case, the parent's `Of`, the model flag), so a child records
  into and replays from its parent's trial. Three small findings: `model: true` is refused in every mode, `off` included (never
  pretended); a default `replay` with no `of` is refused like a sent one; and the privacy test (`ReplayFolderIsPrivateTests`) caught
  my own 400 text and two comments naming `REPLAY_DIRECTORY`, so the message now points to `config/.env.example`. Red first: the
  settings, the factory, the renderer and the shared-browser tests failed on `NotImplementedException` and on the old renderer building
  its own Chrome (an 8 s timeout against a browser that isn't there); the sub-session bodies had no `replay` key; 28 of the 30 API tests
  failed on the stub (the `AgentSessions` one passed at once, since its plumbing is only a parameter passed on).
  **By hand, against the rebuilt Puls container** (`docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build`, which
  sets `REPLAY_DIRECTORY: /data/replay`), `POST /sessions` with no model spend: `replay` with no run tag, an unknown `web`
  (`sometimes`), `model: true`, and `web: replay` with no `of` each gave a 400 with its own text (the unknown `web` lists
  `off, record, replay`); a run-tagged `web: record`, a run tag alone (the default, `WEB_REPLAY` unset) and a plain live chat each
  gave a 201. The 201s were deleted again.
- **Review of step 4a.2** (2026-10-25), one rule changed (the mistake was in step 4a's prompt, not in the build): a run tag does not make
  a session an eval. The orchestrator sends `run` on every fix session, so `WEB_REPLAY=record` would have recorded its real sessions and
  `WEB_REPLAY=replay` would have made each of them fail with a 400; and an explicit `web: off` was not passed on to a sub-session, which
  fell back to the instance default and recorded behind its parent. Now an eval marks itself by sending `replay`, even `{}`, and
  `ReplayResolution` returns none for a session that sends none, whatever the default and the run tag; the default only fills in a missing
  `web`. One rule fixes both: a parent resolved to off has no `SessionReplay.Current`, so its child is sent nothing and is not an eval
  either. `ReplayResolutionTests` now say it (`replay: {}` is the eval's mark in each case that wanted the default); red first: three
  tests (a run tag alone under a `record` default and under a `replay` default, and a child sent nothing under `record`); the other 29
  passed already. The plan's *Approved during the build* has the entry, and 4a.3's runner sends `replay` on every session it opens.
  A slip of mine in the 4a.2 note, found here: it left a second, empty "Notes for step 5" line above the real one; it is gone.
  By hand, against the rebuilt container (`WEB_REPLAY` unset, so the default is off and the container can't show a session *not*
  being recorded; the unit tests hold that): a run tag alone, `replay: {}` with a run tag, `replay: {"web":"record"}` and
  `replay: {"web":"off"}` each gave a 201; `replay: {}` with no run tag gave a 400 (an eval needs its run), and `web: "replay"` with
  no `of` a 400.
- **Step 4a.3** (`evals/louis-agent.evals.runner/Arguments/CommandLine.cs`, `evals/louis-agent.evals/Running/`, `Api/WebReplayRequest.cs`):
  the runner takes `run {name} [--repeat N] [--web off|record|replay] [--of <runId>]`, the flags in any order, a flag given twice keeping
  its last value. `--web` is parsed with `SettingChoice` (ADR 0024); each refusal is a `RunnerMessage` member: `UnknownWeb`, `ReplayNeedsOf`,
  `OfNeedsReplay`, `BadOf`. Every session the runner opens sends `replay` (the eval's mark), with `web` left out when no `--web` was named,
  so the instance's `WEB_REPLAY` decides. A replay's `of` is `{ofRunId}:{mode}:{repeat}/{scenario}`, the trial the recording wrote; a
  recording's is null, so the API writes `{run tag}/{task}`. The runner doesn't check `REPLAY_DIRECTORY` or work out a key the API would
  refuse (ADR 0029). `EvalRun` and `SessionRequest` carry the flags; the runner's `CannedSessionClient` gains `Opened`, the sessions it was
  asked to open, so the tests can see what was sent. A cross-check test runs a recording and a replay of its run and compares the replay's
  `of` with the recorded session's run tag and task, so a later change to the run tag can't break replays quietly.
  Red first: 17 of the 32 argument tests failed on their assertions (the flags were ignored, so a refusal never came), and 2 of the 12
  runner tests failed on the stub's raw run id where the trial's key belongs (`run_A` for `run_A:cook:1/pannekoek`). Two passed at once
  (a run with no flags, and a recording's `of` null), since the stub already sent the mark and no trial. Green: runner tests 62, harness
  131, all 14 suites of the solution.
  By hand, in the rebuilt Puls container (`docker compose -f docker/docker-compose.puls.yml exec -T puls dotnet /app/evals/louis-agent.evals.runner.dll ...`,
  the stub only, so nothing is spent or recorded): `--web sometimes`, `--of` without a replay, `--of ../x` and `--web replay` with no `--of`
  each exited 1 with their own texts; `run stub --web replay --of run_A` and `run stub --web record` each exited 0 with 108 protocol lines.
- **Close of step 4a.** What is recorded: each visit the browser makes in a recording session (its address, script, ready selector, time,
  the page's HTML, captured answers, script result and cookies), once per hash under `pages/` and one index line per visit under
  `trials/`, in the API's `REPLAY_DIRECTORY` (`data/puls/replay` on the host, under the Puls compose). A failed visit is recorded as its
  text. A replay answers each ask from the trial's first recording of it and never opens Chrome; an ask the recording didn't read comes
  back to the model as: "{url} was not read in the recorded run, so this replay can't open it: replays read only recorded pages. Treat
  what it holds as unknown." The runner never touches the folder. Search, `FetchUrl` and the clock were still live after 4a; step 4b records them. Model answers replay in 4e; `replay.model` is refused until then. Recordings stay on the machine
  that made them, and the guide says how to record and replay (*Add an eval set*, section 11).
- **Refactor: the message enums split by area, commit 1 of 3 (`ToolMessage`)** (approved 2026-10-25, before 4b; see the plan's
  *Approved during the build*). The 260 members of `ToolMessage` split: 250 moved to 14 area enums in each area's `Messages/`
  (`Agent` 41, `Guardrails` 7, `Tools/Agents` 28, `Tools/Clock` 1, `Tools/Currency` 3, `Tools/Documents` 9, `Tools/DotNet` 12, `Tools/Git` 8,
  `Tools/Integrations/DevOps` 14, `Tools/Integrations/Paymo` 19, `Tools/Scripts` 15, `Tools/Shell` 16, `Tools/Web` 45, `Tools/Workspace` 32);
  10 stay in core's `Messages/ToolMessage` because more than one area returns them (`ExitCode`, `ErrorWithReason` among them, the shops
  pack's). The file is 792 lines, over `FileSizeTests`' 200; the next member would have failed it. Member texts, names and order are
  unchanged, and `ToolMessageSnapshot.json` is byte-equal. The move ran from a table checked against the code first (260 in both, no
  duplicates, every member with one `[Description]`), so no member went anywhere the table didn't say. Red first: `TheMessageEnumsExist`
  (the list had no area enums yet) and `FileSizeTests` (`ToolMessage.cs` over, once it left `Oversized`). Two findings while moving:
  the architecture test refused `ReplayMessage`'s doc cross-reference, because a `using` for `WebToolMessage` made layer 0 use a tool
  area (fixed: the reference is named in full, no `using`); and `GitRunner.Run`'s `whenEmpty` parameter was typed as the shared enum, so it
  became `Enum?` (no text changed). Corrected in the next commit: it is `GitToolMessage?` (the git toolset's own texts only), and a null means `ToolMessage.SucceededNoOutput`. Checks: no `tests/*.json`
  changed against `HEAD`; the `[Description(` lines in core and the API are 820 before and 820 after, and their sorted diff is empty.
  The TODO line *`ToolMessage` split per toolset* is closed.
- **Refactor: the message enums split by area, commit 2 of 3 (`LogMessage`)**. The 71 members of `LogMessage` moved to nine area enums,
  each in its area's `Messages/`: `AgentLogMessage` 16, `ConfigLogMessage` 4, `HostingLogMessage` 18, `LoggingLogMessage` 4,
  `PricingLogMessage` 10, `ProtocolLogMessage` 9, `SkillLogMessage` 1, `WebLogMessage` 3, `UsageLogMessage` 1. `LogMessage` had no member
  shared by two areas, so the file is deleted. Five members were dead and are deleted with it: `SkillsLoaded`, `PaymoKeyMissing`,
  `DevOpsKeyMissing`, `DevOpsSettingsMissing` (nothing logged them) and `SkillFileLoadFailed` (only a test sample used it). The move
  ran from a table checked against the code first (71 in both, no duplicates, every member with one `[Description]`), with the five
  as the table's deletions. Red first: `TheMessageEnumsExist` (the list still had `LogMessage`) and `FileSizeTests` (`LogMessage.cs`
  over 200 lines once it left `Oversized`). Two places needed more than the move. `AgentLoggingTests.TheBrackets_…` logged
  `SkillFileLoadFailed` as its sample; the brief said it logs `LedgerWriteFailed` and asserts "Could not write", but the test asserted
  "Failed to load" (today's text of the deleted member), so the sample became `UsageLogMessage.LedgerWriteFailed` and its assertion
  takes that member's text: the same holes and the same check for the bracket. And `ArchitectureTests`' Skills rule refused
  `SkillFolder.cs`'s use of `Skills.Messages`; the rule now allows the `Messages/` folder from every folder of `Skills/`, as core's
  `Messages/` is allowed everywhere, and its comment says so. `DisplayCurrency.Warn`'s parameter is `PricingLogMessage`. Checks: no
  `tests/*.json` changed against `HEAD`; the `[Description(` lines are 820 before and 815 after, and the diff is exactly the five deleted
  members' lines, nothing added or changed.
- **Refactor: the message enums split by area, commit 3 of 3 (`ErrorMessage`, and `ReplayMessage` folded in)**. The split closes here:
  three commits, from `ToolMessage` and `LogMessage` to `ErrorMessage`. The 62 members of `ErrorMessage` went to ten area enums in each
  area's `Messages/`: `AgentErrorMessage` 8, `ConfigErrorMessage` 6, `GuardrailErrorMessage` 2, `HostingErrorMessage` 6,
  `LlmErrorMessage` 4 (a new `Llm/Messages/`), `PricingErrorMessage` 15, `SessionErrorMessage` 1 (a new `Sessions/Messages/`),
  `SkillErrorMessage` 6, `UsageErrorMessage` 1, and `WebErrorMessage` (the two `ReplayMessage` members, renamed from `ReplayMessage`,
  which is deleted). The API's ten texts left core for `src/louis-agent.api/Messages/ApiErrorMessage`, since only the API's sessions use
  them. Three texts stay in core's `ErrorMessage` (`MessageNeedsValue` and `MessageHasNoTemplate`, the message system's own, and
  `ApiKeyRequired`, which the API and the MCP server share). Core's shared `Messages/` now keeps `ErrorMessage`, `ToolMessage`,
  `UserText` and `MessageText`. Red first: `TheMessageEnumsExist` (the list still had `ReplayMessage` and no area error enums) and
  `TheHostsHaveMessageEnums` (no `ApiErrorMessage`); the samples that used `ErrorMessage.UnknownCacheTtl` moved to
  `ConfigErrorMessage` with the green step, since the test project could not compile with the enum missing (the same reason as in commit
  2). Two things the move needed: `WebErrorMessage`'s two files that use it are in `Tools/Web/Replay/`, a child namespace, so they need a
  `using` for `Tools.Web.Messages` (a child namespace is not visible to its parent's code); and a `MessageText` doc example had pulled in
  a `using` for `Config.Messages`, which the doc example doesn't need, so it is removed. Checks: no `tests/*.json` changed against `HEAD`;
  the `[Description(` lines are 815 before and 815 after, and their diff is empty (the API's texts are read as well). Core tests: 1342
  passing.
- **Step 4b.1** (`Tools/Web/Replay/{Store,Answers}/`, `Hosting/AgentEngineFactory.cs`): one store keeps every kind of answer. An ask is a
  `ReplayKind` and a key (the seam's ask as compact JSON, built by `ReplayAsk.Of` from an anonymous object, compared ordinally); an answer
  is any JSON value, once per hash in `answers/` (renamed from `pages/`). Enums in the files are names (`JsonStringEnumConverter`,
  snake_case, `allowIntegerValues: false`, ADR 0024). `ReplayAnswer<T>` (`Answered` / `Failed`, `OrThrow`) replaced `ReplayedVisit`;
  `ReplayTrial.Current` is the one place the seams read the session's mode and trial; `WebReplayStore.From` is the only place outside
  `AgentOptions` that names `REPLAY_DIRECTORY` (`ReplayFolderIsPrivateTests` now allows the store, not the browser's factory). Red first: the
  test project would not compile (the `Answers` namespace, `ReplayKind`, `ReplayAnswer<T>` and the generic store calls did not exist).
  The review found one fault: `ReadAnswerAsync<T>` returned `T?`, which for a struct is plain `T`, so a deleted answer file replayed as
  `default` (a clock as 1 January 0001). It now returns `ReplayAnswer<T>.Answered?`, never a struct (`7db44c3`; red: `Answered { Value =
  0001/01/01 }`). The only recording-format change for the browser is that an index line holds `kind` and `key` instead of `url`, `script`
  and `ready_selector`; no real recording existed.
- **Step 4b.2** (`RecordedSearchProvider`): the key is the provider's name, text, `max_results`, site and recency (as a name); the answer is
  the results. A `WebSearchException` is recorded as its text and rethrown; any other exception and a cancel are not recorded. The provider's
  request is never saved, so a search key never reaches the folder. A host's own provider is wrapped too, so a fake records like a real one.
  One new text: `WebToolMessage.ReplaySearchMiss`. Red first: 11 tests on the stub's `NotImplementedException`.
- **Step 4b.3** (`RecordedFetchHandler`, `ReplayResponse`, `PageFetcher`): the recorder is a `DelegatingHandler` outside the SSRF guard's, so
  a recording's request still meets the connect-time check. A response is saved as an allow list (status, reason, content type, `Location`,
  body), so no `Set-Cookie`, no other response header and no request header reaches the folder. Only an answer is recorded: a network error,
  a timeout, a body over 5 MB and a connect-time refusal propagate, and in a replay that request is a miss with 4a's `ReplayMiss` text. A
  replay skips the per-hop DNS pre-check, and only where the fetcher has a store and the flow is a replay; `EnsurePublicAsync` (the
  renderer's) is unchanged. Red first: 8 of 12 tests failed on the stubs; the 4 that passed are the guard's behaviours that must not
  change. The review found one fault: `FetchUrl` called a response too large when the error text held "buffer", so a replay miss on
  `/wiki/Buffer_overflow` said the page was over 5 MB. It now switches on `HttpRequestError.ConfigurationLimitExceeded` (`58210fe`; ADR
  0024's lesson), and a guard test covers the live path with no store (`4c69878`).
- **Before 4b.4: the engine factory's cache and key moved to `Hosting/Engines/`** (`EngineCache`, `ProfileKey`; `202bf5a`), and the key
  states "empty means none" once (`5d0b653`). See the plan's *Approved during the build*.
- **Step 4b.4** (`ReplayClock`, `AgentEngine`, `OwnTools`, `AgentEngineFactory`): within a trial the clock stands still. The first read is
  saved while recording and read back while replaying, and every later read in the trial answers it, so a replay shows the model the same
  time as the recording (step 4e's cache of model answers would otherwise miss). One `Lazy` per trial and mode makes a parent and its
  sub-session save one line; the store call blocks once per trial (`GetAwaiter().GetResult()`, safe because ASP.NET Core has no
  synchronization context). A recording that never read the clock leaves a replay with a miss, `WebToolMessage.ReplayClockMiss`, and a
  `Lazy` keeps its exception, so a miss stays a miss for the trial. `AgentEngine` takes `clock` on its existing parameter line (198 lines,
  unchanged); the factory builds one `ReplayClock` per process. Red first: 7 tests on the stub's `NotImplementedException`. Golden files: only
  `ToolMessageSnapshot.json` gained `ReplaySearchMiss` and `ReplayClockMiss`; `ModelRequestSnapshot.json` is unchanged. Core tests: 1401
  passing.
- **Close of step 4b.** What is recorded now: each visit the browser makes, each search (its results), each fetch (`FetchUrl` and
  `VerifyQuote`, a hop at a time) and the time, in a recording session; a replay answers all four from the trial's first recording of the ask
  and uses no browser, search engine, network or DNS. What isn't: model answers (4e); a failed fetch (it replays as a miss); a search
  failure other than a `WebSearchException`. Known gaps: the Tavily renderer (`PAGE_RENDERER=tavily`) is live, because the page goes to a
  third party even in a replay; `ReadRenderedPage`'s pre-check still resolves DNS in a replay; a failed fetch replays as a miss; and model
  answers replay only from 4e. The guide says how to record and replay (*Add an eval set*, section 11).
- **Step 4c.1** (`PromptHash`, `UsageRecordingChatClient`, `UsageRecord`): every ledger row of a model request carries `prompt_hash`, a
  SHA-256 over the leading system messages, `Instructions` and the tools in the order sent, taken when the request starts (so before any
  provider strategy, and the same on every round of a turn). The encoding is compact UTF-8 JSON with the relaxed escaper and is pinned by a
  test. The field is last on the record and left out when null, so a roll-up's line is byte-equal to before. Red first: 15 tests failed, 14
  on the stub's `NotImplementedException` and one on a null `PromptHash`; the sink and report tests passed at once, since they pin what the
  new field must not change. Golden files unchanged. Core tests: 1419 passing.
- **Step 4c.2** (`BenchmarkLedger.PromptHashesOf`, `BenchmarkSettings.WithPromptHashes`, `BenchmarkRecorder`): when a run ends, the recorder
  adds the distinct hashes of its ledger rows (without the roll-ups, sorted ordinally, joined with commas) to the run's settings as
  `prompt_hashes`; a run with none gets no key. Both stores already keep settings, so neither changed, nor did the request documents.
  `BenchmarkRecorder` gained 2 lines net (a field, one line in `JoinLedgerAsync`, `Document()` reading the field; 230 lines, still on
  `FileSizeTests`' list): splitting it is not this step. Red first: 5 tests failed, 4 on the stubs and the end-to-end on the missing key;
  the no-hash end-to-end passed at once, as it pins today's behaviour. API tests: 273 passing; the whole solution is green.
- **Close of step 4c.** What a run now says about its prompt: each ledger row of a model request carries `prompt_hash`, and the run's
  settings carry `prompt_hashes`, the sorted set of the hashes its rows used.
  - **`prompt_hashes` is the set of prompts a run used, not the set it has.** A run that never called the cook, or never summarised, lacks
    those agents' hashes. Equal lists show the same prompts; different lists prove nothing by themselves, since the difference may be a
    changed prompt or only another path through the same eval.
  - **To compare two runs:** the same eval, and rows from the same agent that differ only in `prompt_hash`. **Step 7a must compare per
    agent**: group a run's rows by the agent that sent them (the purpose, or the sub-session task tag) and compare each agent's hashes with
    the same agent's in the other run; an agent in only one run is "not used", not "changed". The setting can't say which agent a hash
    belongs to, so 7a reads the ledger's rows, not the setting.
  - **What is left:** the trend marker (7a), the cache key (4e, which reuses `PromptHash`) and `ttft_ms` (F44's first step).
- **Step 4d: traces per turn and tool call.** Every model request and tool call is now a span, from Microsoft.Extensions.AI's own
  `UseOpenTelemetry` (source `Experimental.Microsoft.Extensions.AI`, named by the OpenTelemetry GenAI conventions); no decorator of ours.
  - **The tree a turn makes:** `orchestrate_tools` (the tool loop) holds a `chat` span for each request and an `execute_tool {name}` span
    for each call. A summary is a `chat` span under the `execute_tool` of the call it summarises, so the traces count the same requests as
    the ledger (`ChatPipeline.BuildSummaryClient`; approved during the build). The tracing layer is the innermost one, so a span is the
    request the provider got and its duration is the provider's alone. A sub-session's spans nest under its parent's `StartSubSession`
    tool span through `Activity.Current`, with no code.
  - **No content, whatever the environment says.** `WithoutContent` sets `EnableSensitiveData = false` on both clients, so
    `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true` has no effect on our spans. MEAI reads that variable once per process (a static
    readonly), so a test that sets it midway proves nothing: `TelemetryCapture`, a `[SetUpFixture]`, turns it on before the first client is
    built, and the test checks that precondition first. A span keeps the tool names, ids, descriptions and schemas, never the arguments,
    results or messages.
  - **Red and green:** with no tracing, `ChatPipelineTests` failed on the missing spans (4 of 4); with it, 4 pass. Mutation: with
    `WithoutContent` removed from the pipeline, the no-content test fails and lists `gen_ai.input.messages` and `gen_ai.output.messages`
    among the tags. Core: 1423 passing. No golden file changed.
  - **What F44 adds:** an exporter and a dashboard, `traceparent` across processes, metrics views and `ttft_ms`; and, if it needs content
    in a trace, a setting that is off by default.
- **Step 4e.1: the replay store serves every kind.** A move and a rename, no behaviour: the model's answers (4e.2) go in the 4b store, which
  is no longer web-only. `Tools/Web/Replay/Store/` is now `Sessions/Replay/Store/`, `WebReplayStore` is `ReplayStore`, and `ReplayTrial`
  moved to `Sessions/Replay/` beside `SessionReplay`, which it reads. Its two texts, `NeedsTrial` and `OutsideFolder`, moved verbatim to
  `SessionErrorMessage`, which left `WebErrorMessage` empty, so that enum is gone (and `MessageEnumTests`' list with it). The files on disk
  are unchanged: the store's tests pin `answers/` and `trials/`. No new test (a move); the tests moved to mirror
  (`Sessions/Replay/Store/ReplayStoreTests`). Whole solution green (core 1423). No golden file changed.
- **Step 4e.2: the model's answers recorded and replayed, a hit a known 0.** Core only, tested with fakes; nothing is wired into the
  engine until 4e.3. `Llm/Replay/` holds `ModelReplayChatClient`, `ModelAsk` and `ReplayMiss` (decisions 3 to 7 of the step's prompt).
  - **What it does:** per request, from the flow (`ReplayTrial.Current` and `SessionReplay.Current`): web `Record` calls the model and
    saves the answer, and never reads; web `Replay` with `Model` answers only from the recording, and a miss throws without calling the
    model; everything else passes through. The answer is the `ChatResponse` as MEAI writes it (`AIJsonUtilities.DefaultOptions`), saved
    as a `JsonElement` under `ReplayKind.Model`. A hit has `replay_hit` on the response and no `Usage`; `UsageRecordingChatClient` sees
    the tag (on the response, or on any update of a stream) and writes zero tokens and `cache_hit: true`; `CostCalculator` prices a hit
    as a known 0 before it looks the model up.
  - **The key leaves out `CreatedAt` and `MessageId`** (the probe's finding 2, the test `MessagesThatDifferOnlyInCreatedAtAndMessageId…`).
    The mutation check: with `MessageId` added to a message's hash, four tests fail (the key test, the hit test through the thinking
    round trip, streamed and not).
  - **A miss says what changed:** the model, the system prompt, the tool `'{Name}'` (changed, added, gone, or the tools reordered), the
    settings, or message *n* (from 1, after the system prompt, with its role), against the recording that matches longest; a trial with
    no model answers says it was recorded before step 4e or is a different run. `PromptHash` gained `SystemOf` and `ToolOf` and a
    shared `Digest`; `Of` is byte-equal and its pinned test did not change. The store gained `AsksAsync(trial, kind)`.
  - **Not in the key:** a setting that reaches the provider only through `RawRepresentationFactory` (a function; the options' own JSON
    leaves it out). A provider's thinking budget set that way would not make a miss. Said in the close of step 4e.
  - **Red and green:** the stubs threw `NotImplementedException`; 71 tests in the new and touched classes ran, **33 failed** (the key, the
    miss texts, the client's six cases streamed and not, the recorder's tagged case, the unpriced hit). The sink's two tests and the
    priced-model cost case passed already, because `UsageRecord.CacheHit` had to exist for the tests to compile. With the code, 82 pass.
    Mutations: without `response.Usage = null` the hit test fails in both modes (it needed a streaming fake that reports a
    `UsageContent`; the first version could not see it); with `MessageId` in the key, four fail. Core: 1462 passing (1423 + 39). No
    golden file changed. `ModelAsk`'s two small records are nested in it (the layout test allows one top-level type per file).
- **Step 4e.3: the engine and the API replay the model.** `ChatPipeline.Build` and `BuildSummaryClient` take the process's `ReplayStore?` and
  put `ModelReplayChatClient` innermost (`… MarkTruncated → OpenTelemetry → ModelReplay → provider`; the summaries' client the same, so a free
  replay doesn't pay for its summaries). `AgentEngine` takes `replay` and passes it on (196 lines, as before); `AgentEngineFactory` keeps its
  store in `_replay` beside `_clock`. The API accepts `model: true` only with web `replay` (a 400, `ApiUserText.ReplayModelNeedsReplay`, which
  replaces `ReplayModelNotYet`): with `record`, `off` or the instance's default being off it is refused, and a replay with `model` still needs
  `of` and an instance that keeps recordings. A sub-session inherits `model` through `SessionReplay.ForSubSession`, as before.
  - **The test that matters:** `ChatPipelineReplayTests` records two streamed turns with a tool call through a real `AgentEngine`
    (`files` toolset, `PricingUsageSink` over a table that prices no model), then replays them on a client that throws. The answers are
    equal, the model is never asked, every ledger row of the replay has `cache_hit` and a known $0 cost, while the recording's rows stay
    `cache_hit` null and cost null; the history holds no `replay_hit`. It runs with a small result and with an oversized one, whose summary
    replays too. Mutation: with `MessageId` and `CreatedAt` in the key, both cases fail with the miss text "message 3 (tool) changed", which is
    exactly the probe's finding 2 (turn 2's first request carries the stamps the tool loop put on the tool message).
  - **Red and green:** with `AgentEngine` taking `replay` but ignoring it, the two replay cases and the factory case failed ("the model was
    called"), and six API cases failed (the old refusal). The two "no replay in the flow" cases passed on the red run, as they should: they
    pin that nothing is written or read. Core: 1467 passing, API: 277. No golden file changed.
- **Step 4e.4: the runner's `--replay`.** `run {name} --replay {runId}` is `--web replay --of {runId}` with the model's answers too: it
  parses to `RunRequest(name, repeat, Replay, runId, Model: true)`, and `RunRequest`, `EvalRun` and `WebReplayRequest` carry `Model` (false by
  default, so every other run sends exactly what it sent). `ReplayFor` passes `run.Model`. Given with `--web` or `--of` (either order) it is
  refused (`RunnerMessage.ReplayExcludesWebAndOf`); its value is held to `--of`'s rule for a run id, through the same `ParseOf`, so the two
  refuse a bad id with the same text. A flag, not a new string choice (ADR 0024). The guide's section 11 now has the three ways, in a table.
  - **Red and green:** with `Model` on the records but the parser ignorant of `--replay`, the runner's 10 new cases failed (the flag, its two
    refusals, a bad id, the usage text) and the evals' 1 (`ReplayFor` sent `Model` false); with the code, runner 73 and evals 132 pass. Whole
    solution green: core 1467, API 277.
- **Close of step 4e.** What the cache is, and what it isn't:
  - **A replay proves** that everything around the model still works on the same inputs: the graders, the harness, the engine's code
    (the pipeline, the guards, the recorder, the tools), all against the model's recorded answers. It costs $0 and takes seconds, and gives the
    same answers. It is a regression check for those, and it can't pass if the run's own requests changed.
  - **A replay can't test a prompt change.** A changed prompt is a different request, so it misses, and the miss names the part that changed.
    A prompt change is tested by `--web replay --of`, which keeps the pages and asks the live model.
  - **Known limits, not solved.** (1) One identical request twice in one trial is answered by its first recording: the store keeps no counters
    (as for the web seams in 4b). (2) A sub-session that hit its prompt budget (FX14) in the recording won't in a replay, because a hit counts 0
    tokens; its later requests then differ and miss, and the miss says where. (3) A setting that reaches the provider only through
    `RawRepresentationFactory` (a function, which the options' JSON leaves out) is not in the key, so changing it alone does not make a miss.
  - **Measured check: still to run (a paid run, on Louis's go).** Record the cook eval live (`run cook --web record`), then replay it
    (`run cook --replay {runId}`). Expected: a hit rate of 100% and a cost of $0.00 on the replay, against the recording's cost. Until the
    HTTP session client (step 5) exists only the stub runs, so this waits for step 5 as well; the plan's line 197 stays unmeasured until then.
- **Step 4f.1: tool calls logged as keyed hashes and lengths; `LOG_CONTENT=full` keeps the text.** Core only; the shops pack and Rider's host
  are 4f.2.
  - **The setting:** `LOG_CONTENT`, `hashed` (the default) or `full`, an enum (`LogContentMode`, ADR 0024) parsed once by `LogContent.ModeOf`
    with `SettingChoice`; an unknown value throws `UnknownLogContent` (a `LoggingErrorMessage` member) and stops the host at start-up.
    `AgentLogging.Configure(dir, content)` sets it; `AgentHost` and the orchestrator pass `ModeOf(options.LogContent)`.
  - **The hash:** `#` + the first 12 hex digits of HMAC-SHA256 under a 32-byte random key + the length. The key is made when the class
    loads, never written or logged, and kept across `Configure` calls (`AgentHost` configures twice). A plain SHA-256 of a town can be
    reversed by hashing a list of towns; a keyed one can't without the key, and the test pins that the plain digest is not what is written.
  - **What changed:** the `[TOOL]` call line (every argument hashed, names kept, in order), a failure's reason and the summary's failure reason
    (an exception's message can quote an argument; the error type stays), the result (`Of(text, cut: 300)`), and both records
    (`Arguments`, and `UserRequest` cut at 500 under `full`). **Unchanged on purpose:** the arrows, the order, tool names, counts and outcomes;
    the `AgentLogMessage` templates (no golden file changed); the usage ledger (it holds no prompt or tool text; see the 4f.3 note on its tags); and the string sent to the summarising
    model, which keeps the full arguments (`TheSummaryRequest_KeepsTheFullArguments…`; mutation: hashing it fails that test).
    `ToolCallGuards.cs` stays at 210 lines.
  - **Red and green:** the stubs threw `NotImplementedException`; 31 tests in the touched classes ran, **22 failed** (the hash and its shape,
    `Full`, the arguments, `ModeOf`, the records, the existing `[TOOL]` test and the new hashed one). With the code, 1488 pass in core (1467 +
    21). Mutation: logging the raw result text fails the hashed-line test.
  - **Where the standards land:** POPIA s13 and s14 (kept only for its purpose): the tool lines and the two records; s19 (protected): the key
    is never written. OWASP LLM02:2025 (logs as a leak): every line that carried an argument, a result or the request.
- **Step 4f.2: the shops' town and Rider's prompt logged as hashes.** The pack reads core's shared `LogContent` (ADR 0028) and names no
  identity. **Hashed in the shops:** the town (`Place`) in every shop line, the centre's latitude and longitude, the store ids (`Stores`, and
  `Store` in `ShopStoreNotUsed`: an id names its town as surely as the coordinates), and the reasons of the two `ShopAreaUnavailable` lines
  (an exception's message can quote the town). The shop's name, the outcome and the counts stay. `CheckersCatalogue.cs` stays at 210 lines (a
  redundant `using` of its parent namespace made room for the new one). **In Rider's host:** `PromptHandler` logs the raw parameters and the
  prompt through `LogContent.Of` (the prompt cut at 300 under `full`); `AcpTranscript.json` (stdout) did not change.
  - **Red and green:** 7 of the 8 new cases failed on the existing code (the town, the coordinates or the ids reached stderr in clear; the
    Rider test found the prompt's words in the log); the eighth, Rider's `LOG_CONTENT=full` case, pins today's lines and passes before and after. With the code: shops 99, ACP server 33, and the whole solution
    green (core 1489, API 277). Mutation: logging the store ids in clear fails `AStoreForTheTown_LogsTheTownTheCentreAndTheStoreIdsAsHashes`.
- **Close of step 4f.** A log keeps what it is for (what ran, in what order, how long, how it ended) and hides what it was given.
  - **Hashed (default):** every tool argument, a tool result and a failure's quoted reason (`[TOOL]` lines); the two JSON-line records'
    arguments and the user's request; the shops' town, coordinates, store ids and reasons; Rider's raw parameters and prompt. Each shows as
    `#` + 12 hex digits of an HMAC-SHA256 under a random per-process key + the length. `LOG_CONTENT=full` writes the values, for a developer's
    own machine and never a hosted instance. An unknown value stops the host at start-up.
  - **Stays in clear:** the tool, shop and event names, the arrows and their order, counts and sizes, the error type, durations, the usage
    ledger (no prompt or tool text; its caller-set tags such as `task` can hold a scenario name, e.g. `cook-malmesbury`: the 4f.3 note), and the
    `AgentLogMessage` templates.
  - **Known limit (for F44's trace and log pass):** the headless browser's two warnings (`RenderRequestRefused` and `RenderRequestUnhandled`)
    still carry the URL of the sub-request and the page. A URL the model reads can hold the user's words, but the lines warn about a refused
    sub-request, testing them needs a live Chrome, and `ChromeWebBrowser.cs` is on the oversized list.
  - **Out of scope:** the orchestrator's own lines (a monitored service's errors, on Louis's machine), and paths and file names in Rider's
    host (a developer's own files).
  - **Standards:** POPIA s13 and s14 (kept only for its purpose): the `[TOOL]` lines, the records, the shops' lines and Rider's prompt lines
    show what ran, not what it was given; s19 (protected): the key is random, per process, never written or logged, so a hash can't be
    reversed by hashing a list. OWASP LLM02:2025 (sensitive information in logs): each of those lines. Concept: skill 17 (observability) with
    skill 15's practice; the audit of every `Log.Log` call site for user-derived fields found no other.
  - **The hand check: done, in the 4f.3 note.** A coding-agent chat in Rider with `LOG_CONTENT=full` logging as today is covered by the unit
    tests, not run by hand.
- **Step 4f.3: a string argument from the model hashes as its text.** Found by the live hand check of 4f (a Puls chat from Malmesbury), not by
  a test: the model's arguments arrive as `JsonElement`, not `string`, and `OfArguments` hashed such an element as its JSON, quotes included. The
  area `Malmesbury` showed as `#138099225789 (12)`, while the shops pack hashes the same town as a plain string, `(10)`. So one town had two
  hashes in one log, and a reader could not follow it from the tool call into the shop's lines, which is what one key per process is for. None
  of 4f.1's tests passed a `JsonElement`: they passed `string` and anonymous objects. The fix is one `switch` in `LogContent.OfArguments`
  (`ArgumentOf`): a string hashes as is, a `JsonElement` of kind string as `GetString()` (which also unescapes `é`, where the serializer
  would hash the escaped form), a JSON null as `null`, anything else as its JSON, as before.
  - **Red and green:** two new tests, `Hashed_Arguments_FromTheModel_AreJsonElements…` (the length is `(10)` and the hash equals
    `LogContent.Of("Malmesbury")`, for a string with `é`, a number, an object and a bool too) and `…AJsonStringAndAPlainStringOfTheSameText_HashAlike`.
    On the 4f.1 code they failed with the live run's own output (`area=#b830e49e284f (12)`, expected `(10)`); with the fix core passes 1491
    (1489 + 2), and the other projects are unchanged.
  - **The ledger's tags (a correction to the close of 4f).** The close said the usage ledger "never held text". It holds no prompt or tool
    text (F1-S4), but its caller-set tags (`task`, `run`, `service`) can hold a name the caller chose: 40 rows from 17 to 20 October carry
    `task: "cook-malmesbury"`, a benchmark scenario's name, not anything a user typed. The grep of `logs/` after the rebuild found exactly those 40
    and nothing in this chat's 11 rows. The wording above is fixed; nothing in the ledger changes.
  - **The hand check, done (2026-10-25, a few cents: $0.0175).** After the rebuild, `grep -ri malmesbury logs/` matched no file written since
    (the new run file, the other run files and the record files); the chat's own stream still showed all 13 calls and their 13 results in
    full, against 13 hashed `[TOOL] ->` and `<-` lines. Not exercised live: Checkers refused the store lookup, so the line with the centre's
    coordinates and the store ids ran only in the unit tests.
- **Notes for step 5** (from the review of 4.2): `PulsEvalSet`'s `TurnView` builds on `EngineEvents`, so the route, the errors and the
  cost come from one reader. If an expected route goes in a scenario's `expected`, `RoutedToGrader` must declare that key in its
  `ExpectedKeys`. When the HTTP client sets a turn's `Failure`, the turn line must carry it, so `ProtocolTranscripts` reads it back. A
  reference may need its mode in its file name (`{scenario}.{mode}.reference.txt`) when an eval's modes expect different routes.
  The HTTP client's body test pins `replay.model` too: a `--replay` run sends `"replay": {"web": "replay", "of": "{trial}", "model": true}`, and the others `model: false`.
- **Step 5.1 (the HTTP session client, and turn failures in the protocol).** `HttpSessionClient` (`Api/`) is the Python `client.py` over one
  `HttpClient` (`CreateHttp`: `PULS_API`, ten minutes, `x-api-key`); its bodies and replies are records (`SessionWire`) serialized with
  `EvalJson`, null fields left out. A turn that fails for the infrastructure is recorded, not thrown: `TurnFailures.Of` maps the exception to a
  `TrialFailure`, the `turn` line carries `failure` (snake case, `FailureName`), `ProtocolTranscripts` reads it back, and a session that
  can't open fails all its turns the same way. A cancelled run still throws. Red before the code: 21 of 157 tests (12 `NotImplementedException`
  from the client, 9 assertions); green after: 157. Not done on purpose: after a failed turn the scenario's next turn is still sent, as the
  Python's per-turn `except` did; 5a decides whether that should stop.
- **Step 5.2 (a stored run as protocol lines).** `GET /v1/admin/benchmarks/runs/{id}/protocol` (admin gate: `x-api-key` or a bearer token, `ADMIN_API_KEY`
  then `AGENT_API_KEY`) returns a stored run as the protocol's own lines, written by `BenchmarkProtocolExport` with `Utf8JsonWriter` (the bytes are the
  contract, ADR 0041). Red before the code: 6 of 7 new tests; green after: 7, and the API's suite is 282. The Python stub's lines, recorded,
  exported and read back by `BenchmarkProtocol`, are the same events key for key; so is an eval run with graders, models, grades and an outcome.
  Not done, on purpose: the by-hand export of the Python's last runs. Louis had deleted them and made the first C# run the baseline
  (plan, *Approved during the build*). `mode.conversations` is counted from the stored conversations, and a line's `t` is the last stream
  time known, as the store keeps no other clock.
- **Step 5.2 review fix (CI run 358, and two findings of the review).** (1) `AdminEndpoints.cs` grew to 202 lines (`FileSizeTests`): the stored-run
  routes (read, events, protocol, delete) are `StoredRunEndpoints.cs`, mapped from the same `/admin` group, so the key gate still covers
  them. (2) The export writes `"scores"`, so `PortalReadsNoScoreKeyTests` flagged it in `Benchmarks/Reports/`: it is the protocol reader's other
  half, not a report, and now sits in `Benchmarks/Export/`; the test's list is untouched, because it may only shrink. (3) `HttpClient.Timeout`
  stops counting once the headers arrive, so a stream that stalled after them hung the runner (shown on a real socket: headers and one event, then
  silence). `HttpSessionClient.SendAsync` now has an idle limit (`DefaultStreamIdle`, 10 minutes, the Python's per-read 600 s) restarted after
  each event and not counting the caller's time between them; its cancellation is a `Timeout` in `TurnFailures`. A learning test pins the
  library behaviour. With the guard off, the two stall tests fail (15 s each, the test's own limit). (4) A turn's failure is now read
  (`BenchmarkProtocol.TurnEnded.Failure`, `BenchmarkTurnFailure`, snake-case names), stored (recorder, in-memory and Mongo) and exported, so a stored
  run read back never grades an infrastructure failure as the agent's (5b needs it). The Mongo contract tests skip without a login; the Mongo
  mapping of `failure` was reviewed, not run. The whole solution passed before the push.
- **Step 5.3 (`CookEval` and `RoutingEval` in `PulsEvalSet`).** `puls-cook` (modes `puls-tools` and `puls-cook`; graders `CookNoError`, `CookRoutedRight`) and
  `puls-routing` (modes `baseline` from `GET /v1/toolsets` minus the delegating toolsets, and `delegated`; grader `RoutingRoutedRight`) load their
  scenarios from the datasets; `CookSetup.Places` and `RoutingSetup.Tasks` are gone, their tests moved to the dataset. A no-error grader would add
  nothing to the routing: a one-turn question is routed right only without an error. `TurnView` now takes its answer, role and error from
  `EngineEvents` (new `ErrorTextOf`), the one reader the 4.2 review asked for, so a grader and the protocol's facts cannot disagree; the parity
  test between the two readers went with it. The three graders return `Unknown` for a turn that failed for the infrastructure (5.1 now records
  those). The conformance cases are 11 references and 6 known-bads, **built, not recorded**: no run exists to cut them from (the Python's were
  deleted; the first C# run is the baseline), and a script wrote them. The portal's `BenchmarkCatalog.Default` is the stub alone; the cook and
  routing scripts stay on disk until step 7. Their manuals (`benchmarks/cook/HANDLEIDING.md`, `routing/`) describe the Python run, so the new
  checks show no manual until 7b writes them. Red before the code: 17 of 101 Puls tests, and 2 conformance cases; green after: Puls 100,
  evals 162, runner 75, API 290. **Guide fixes:** the intro and the dataset bullet said Puls's set was empty and no eval read its dataset; section 11
  said only the stub runs; section 7 now says where references come from when no run exists, and that a reference's mode decides what the graders expect.
- **Step 5.4 (the scenarios state their requirements, and the held-out set).** Every scenario's `expected` says what it asks for, each key read by a named
  grader: `routes_to` (the harness's `RoutedToGrader`, which now takes its keys in its constructor, replaces `CookRoutedRight` and `RoutingRoutedRight`),
  and for the cook `town`, `serves` (`ServesAsAsked`, not required: the real check that quantities scale is step 6's), `budget` (`WithinBudget`, required
  where set, from `Prices.Total`: the total line, else the sum of the priced rows, and the reason says which) and `shop` (`PricedAtShop`, required where
  set). The loader refuses a grader key no scenario feeds; a run asks for `dev` unless `--split held-out` or `--split all` (`SplitSelection`, parsed with
  `SettingChoice`); the portal runs dev. **The datasets were stale against the prompts and were rewritten (Louis, 2026-10-25: "the cook requires town,
  serving size and shop, or just the recipe, then no prices"; "compare it with the prompts").** Puls's routing prompt asks first when a meal doesn't say
  for how many or where the user shops, so a question that should reach the cook now names its people and its shop (the routing's two meals too, and
  a test holds each question to its `expected`); the cook's prompt prices at the user's shop with `area` = the town in every call, or, for `enige`/`any`,
  prices nothing and writes `± R…` estimates under a line that says so. So `shop` is `checkers` or `any`; the old `none` ("no shop near you has a
  catalogue") is gone, because nothing produces that line any more; `cook-no-shop` became `cook-no-branch` (priced at the nearest store), `cook-recipe-only`
  and `cook-asks-first` are new, and the Python-parity test of the inputs went with the Python's runs. Six held-out routing questions and three held-out
  cook scenarios (Paarl, an English budget in Worcester, a follow-up in Somerset-Wes) were written for this. The conformance references are built, not
  recorded, now with answers that really pass every grader (`PulsReferencesTests` holds all of them, the non-required too); after the first paid run
  they should be cut again from that run. The scorer's v1 keys now read the scenario's `shop` and `routes_to` instead of its name. The Puls evals' `Proves`
  text is a message enum member (ADR 0009), as the stub's is. Not done: a grader that the answer to `cook-asks-first` really asks for the people and the
  shop (its route, null, is checked; the wording waits for step 6's model grader). Red before the code: the harness 13 tests, the Puls side its stubs;
  green after: the whole solution.
- **Step 5.4 review fix.** (1) The price reader added a pack's weight to the price: `R45 250 g` read as R45 250, because a space before three digits looks like
  a thousands separator, which would fail a good basket on the required budget grader. `Money` refuses a grouped reading that a unit (`g`, `kg`, `ml`, `l`, `x`)
  follows, and takes the plain digits (red first: 45250). (2) The dataset decision is in the plan under *Approved during the build*. (3) A run records its
  split: the first line says `"split": "dev|held_out|all"`, the store keeps it (`BenchmarkRunSplit`, snake-case names, memory and Mongo), and the dev trend leaves out
  a held-out run or one of all, so the baseline carries it; a run before splits were kept is a dev run. A test pins the harness's and the API's names to one set.
  (4) The three infrastructure-failure checks of 5.3's graders were unproven: removing each still passed the tests, because the failed turn in them had no answer. Each
  test now uses a turn that failed but has a whole, right answer, and checks the reason, so removing a check fails it (6 tests, run by hand). Noted for 5a: a grader
  that does not apply to a scenario (no `budget`, no `shop`) passes, so a pass rate has to read that as "not asked", not as a pass.
- **Step 5.4b (one folder per eval).** An eval project is cut per eval (ADR 0010: the concern is the eval, not the kind of file): `Evals/{Folder}/` holds
  `{Folder}Eval.cs`, its `Grading/`, `Scoring/`, `Dataset/` and `Transcripts/`; `Shared/` holds what two evals or more use, and `Messages/` the set's
  enums; the set class is the only file at the root. `EvalLayoutTests` (the runner's tests) holds it for every set and the stub: red first, 6 of 14 on
  the old tree, then green. Its rule for `Shared/` counts use through another `Shared/` file (the routing uses `ToolHosts`, which reads the shop table,
  so `Shops`, `Links` and `Lists` stay shared, though much of `Shops` is the cook's: a split for later), and `Shared/` may not name an eval. The test
  also found one shared rule that two evals each held (`CookScorer.CookOn`, `RoutingScorer.Delegating`): `PulsModes.CanDelegate` is in `Shared/` now.
  `EvalFiles` (harness) says where an eval's data is at run time, so no eval builds a path; each eval project has one data item with a `TargetPath` of
  `EvalFiles/{assembly}/%(RecursiveDir)...`. **A learning test** (`EvalFilesCopyTests`) showed: `%(RecursiveDir)` is the whole path from the project
  root, so the files land under `EvalFiles/{assembly}/Evals/{Folder}/Dataset/`; the files reach the runner's and the tests' output through the
  project reference alone; and `Link` would show each file a second time in the IDE under a folder named like the project (Louis saw it: "why is this
  referencing itself"), so the item uses `TargetPath`, which MSBuild honours for the copy. The explicit `Transcripts` and `Datasets` link lines of the
  test projects are gone; the fake set keeps its transcripts beside its test. Moved with `git mv`, the tests mirror it; every score, golden file and
  conformance result is unchanged.
- **Step 5.4c (a template eval project).** `evals/louis-agent.evals.template` is a project to copy when you start a new eval set: the 5.4b layout with every
  member present and empty. `TemplateEvalSet` lists one eval, `Example` (`Evals/Example/`): every `IEval` member has a one-line comment saying what goes there and
  which section of `add-an-eval-set.md` explains it; `Name`, `Version`, `Proves` (from `TemplateMessage`, ADR 0009), `Scenarios` (through `EvalFiles`) and `Graders`
  are real; `ModesAsync`, `ScoresOf` (through `ExampleScorer`), `TurnFactsOf` and the grader's `GradeAsync` throw `NotImplementedException` with no text. Its dataset
  is valid, with one scenario, so the loader's rules are shown working. It has no `Transcripts/` (git keeps no empty folder, and the conformance rule refuses a
  stray README): the layout test allows that as the template's one exception, and the same for its `.template.config` at the root. It is never registered
  (`EvalSetKind` doesn't list it, and a test says so); `EvalLayoutTests` includes it through a project reference that only the tests have, so the template can't
  drift from the layout. Its tests are `tests/louis-agent.evals.template.tests` (a project of its own, because tests mirror the project): the dataset loads with
  its grader, the names are set, and each member that should throw does, which tells the next person what is left. The optional `dotnet new` template worked
  at the first try: `dotnet new install evals/louis-agent.evals.template`, then `dotnet new louis-eval-set -n Acme -o louis-agent.evals.acme` (from `evals/`) gave
  `AcmeEvalSet`, `LouisAgent.Evals.Acme`, `acme-example`, `louis-agent.evals.acme.csproj`, and the copy built with no warnings; `Example` is renamed by hand. I
  uninstalled it afterwards (the install lives on the machine, not in the repo). Red before the content: all 5 template tests, and the layout test could not build
  the template at all; green after: template 5, runner 140. The guide's `run-the-evals.md` link waits for 5.5, which writes it.
- **Step 5.4b review fix.** The unit check of the price reader (5.4's weight fix) applied to every amount, and its `x` part caught multipliers: `R69,99 x 2` read as
  R69, and in `R45 x 2` the R45 was lost, which understates a basket and could pass an over-budget trial. The check now applies only to the thousands-grouped
  reading (a unit after a grouped number refuses the grouping and takes the plain digits), and there is no `x` part; four test cases pin it (red first: 69.00 and no
  amount). Two compiler warnings of this branch are gone: a duplicated `using` in `RoutingEval.cs` and a nullable key in the layout test (CS0105, CS8714).

---
[Features](README.md) · Previous: [F17 Orchestrator agent](F17-orchestrator-agent.md)
