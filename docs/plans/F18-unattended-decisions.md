# F18 unattended run: the decisions log

> **Plan (log).** What the unattended run of 2026-10-27 decided on its own, finding by finding, so Louis can read
> each choice afterwards and have any he disagrees with redone. It is not the plan: nothing here goes under the F18
> plan's *Approved during the build* until Louis has read it. **Elsewhere:** the plan itself in
> [F18-evals.md](F18-evals.md), the feature in [F18-evaluation.md](../features/F18-evaluation.md).

## Where it started

`git pull --rebase` on `feature/F18-evals` at `a86b96e5` (ADR 0045). `dotnet build louis-agent-solution.sln`: 0 errors,
8 warnings. `dotnet test louis-agent-solution.sln` (unit and integration): **3,692 passed, 0 failed**. The integration
tests that need MongoDB, Chrome, SearXNG or the network skip themselves on this machine (71 in the api and core
integration summaries, and the shops catalogue tests), as they are written to.

## Observed along the way

- **A timing test flaked once.** Before pushing Part 2, the whole-solution run failed
  `EachEventRestartsTheIdleClock_SoALongTurnOfLiveEventsIsNotCutOff` (louis-agent.evals.tests) once; that project
  alone passed three times straight after (322 of 322), and the run changed nothing under `evals/`. It looks like the
  idle clock losing to a busy machine with every test project running at once; worth a look, not fixed here.
- **`aria-checked` is written as a bare attribute.** The explorer's repeat and chat-task buttons, and the Benchmarks
  page's, write `aria-checked="@(bool)"`, which Blazor renders as a bare `aria-checked` when true and leaves out when
  false; ARIA wants `"true"`/`"false"`. Kept as it was through Part 3 (the goldens pin it); a small fix for later.

## Findings

Each finding: what came up, three options (the pattern, what it changes, what it costs, the sources), the pick and
why, and the commit that carries it.

### 1. Where a reply's `Of` lives once the records move to a project that references nothing (Part 1.1)

**What came up.** ADR 0045 puts the reply records in `louis-agent.api.contracts`, which references nothing, and the
guide says a record built from another type gets a static `Of` on the record (`CheckRow.Of(check, hasManual)`). But
`Of` takes the API's own types (`BenchmarkCheck`, `BenchmarkTrends.Point`, `CategoryTrends.Category`), which the
contracts project can't see.

1. **C# 14 static extension members in the API (picked).** The records move; an `extension(CheckRow) { public static
   CheckRow Of(…) }` block in the API adds `Of` to each record where the domain types live, so every call stays
   `CheckRow.Of(…)`. Pattern: Fowler's *Replace Constructor with Factory Function*, placed by PoEAA's *Data Transfer
   Object* advice that the assembler sits on the server side. Changes: one file of extension blocks in the API.
   Costs: a reader has to know C# 14's extension members; `Of` is not on the record's own file.
2. **A mapper class in the API** (`AdminReplies.Check(check, hasManual)`). PoEAA's *Mapper*/*DTO Assembler*.
   Changes: every call site. Costs: breaks the guide's `CheckRow.Of` naming, which Louis just set.
3. **Let the contracts reference the API's domain.** Keeps `Of` on the record. Costs: breaks ADR 0045 ("references
   nothing") and drags the server into every client, the browser included.

**Why 1.** It is the only option that keeps both rules Louis wrote (the contract references nothing, the factory is
`Record.Of`). Sources: Microsoft, *Extension members* (C# 14,
learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/extension-methods); Fowler, *Refactoring*
2nd ed., *Replace Constructor with Factory Function*; Fowler, *PoEAA*, *Data Transfer Object* (the assembler).
**Commit:** Part 1.1.

### 2. A 201 on the admin wire can't take the wire's options (Part 2, notes and re-scores)

**What came up.** A note and a re-score answer 201 with a Location. `TypedResults.Created(uri, value)` has no
`JsonSerializerOptions` overload: it writes with the app's default options (camelCase), while the admin wire is
snake_case (`AdminJson.SnakeCase`). For a note every name is one word, so the bytes happen to match; for a re-score
(`exit_code`, `started_at`) they would not.

1. **A small result of our own, `CreatedJson<T>` (picked).** It sets the status and the Location and writes the value
   with the options it is given, and says what it produces for OpenAPI. Pattern: a custom `IResult`, Microsoft's way to
   shape an answer the built-in ones don't (minimal APIs, *Responses*, "Customizing responses"). Changes: one class in
   the API (`Endpoints/Answers/`). Costs: one more type to keep, and its OpenAPI metadata written by hand.
2. **A JSON answer with status 201, the handler setting Location itself** (`TypedResults.Json(…, statusCode: 201)` and
   an `HttpResponse` parameter). Changes: the handlers only. Costs: the declared answer says "JSON", not "created";
   a handler writes a header as a side effect, and a no-HTTP test needs a fake response.
3. **`[JsonPropertyName]` on the records the 201s send**, keeping `TypedResults.Created`. Changes: two records. Costs:
   it undoes fix 2 of the review (the names live twice, once as an attribute and once as the wire's rule), on exactly
   the records where it matters.

**Why 1.** It keeps one options object per wire (ADR 0041) and lets the signature say what the route does (ADR 0045's
point). Sources: learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses (custom `IResult`,
`IStatusCodeHttpResult`, `IValueHttpResult<T>`, `IEndpointMetadataProvider`); ADR 0041; ADR 0045. **Commit:** the
notes file of the sweep.

### 3. The session request bodies: move them now, or when a change touches them (Part 2, sessions)

**What came up.** The brief puts "replies and bodies" in the contract. The session bodies
(`CreateSessionRequest`, `SendMessageRequest`, `AttachmentRequest`, `ReplayRequest`) carry the API's own parsing
(`RoleOrNull`, `StreamFormatOrNull`, `Place`, `InvalidTag`, which use core's types), and five test files and the
replay rules use them. Their clients (the eval runner's `HttpSessionClient`, the orchestrator) keep their own copies.

1. **Move them now**, the parsing as C# 14 extension members in the API (as finding 1). Changes: four records, a
   rules class, `ReplayResolution`, `TurnStream` and five test files brought up whole. Costs: the largest commit of
   the sweep for no change on the wire and no client that reads the contract yet.
2. **Move the replies now, the bodies when a change touches them (picked).** ADR 0045's consequences already say
   other clients move "when a change touches them". Changes: the session file only. Costs: the brief's "bodies" is not
   met for the sessions API in this run; the session bodies stay internal records in the API for now.
3. **Move only the plain records**, leaving thin API wrappers that parse. Changes: four records and four wrappers.
   Costs: two types per body, the worst of both.

**Why 2.** The bodies' JSON is the sessions API's, which must not change, and no client reads the contract for it yet;
moving them is churn with no reader. A TODO line names the move, so the first change to a session body (or the eval
runner switching to the contract) does it. Sources: ADR 0045 *Consequences*; Fowler, *Refactoring* 2nd ed., "the
Rule of Three" (refactor when the need is there). **Commit:** the sessions file of the sweep. Louis may prefer 1;
it is a mechanical redo.

### 4. Where a page's own words go once its `@code` leaves (Part 3, `ChartInfo`)

**What came up.** `ChartInfo`'s `@code` held the chart notes, about 55 sentences of user text. The guide's rule says
user text in C# is a message enum member (ADR 0009), but those enums are read through core's `MessageText`, which the
browser app can't reference; Puls keeps its own words in plain static text classes (`PulsText`).

1. **A plain table class, as `PulsText` (picked).** `ChartGuides.For(ChartTopic)`, a switch expression with one arm per
   measure. Changes: three small files in `Admin/Charts/Guides/` and their NUnit tests. Costs: the words are C#
   strings, as Puls's others are, not message enum templates.
2. **A message enum per note part** (`ChartGuideText.CostWhat`, 55 members with `[Description]`). Changes: an enum
   and a template formatter in the browser app. Costs: a second `MessageText` in Puls for plain sentences with no
   holes, and 55 members where 11 records do.
3. **Keep the words in markup**, a component per measure. Changes: eleven components. Costs: eleven files for one
   table, and the explorer and the trend still have to pick the right one.

**Why 1.** It follows the browser app's own precedent and keeps one place for the notes, which ADR 0009's message
enums can't reach. Sources: ADR 0009; `src/louis-agent.puls/Text/PulsText.cs` (precedent); Fowler, *Refactoring*
2nd ed., *Replace Conditional with Polymorphism*'s simpler cousin, a switch on a closed set. **Commit:** the
`ChartInfo` commit of Part 3.

### 5. A trend line across a run where the mode has no value (Part 3, `TrendChart`; the open review note)

**What came up.** The chart joined a mode's known points in one path, so a run where the mode wasn't run (or its
value was unknown) sat under a straight segment with nothing to show it was missing.

1. **Break the line at the missing run (picked).** The path starts a new segment after each gap (`M … L …`, then `M`
   again); a point alone between gaps is still drawn as its dot. Pattern: d3's `line.defined()` and Excel's "show
   empty cells as gaps", the usual reading of a missing value. Changes: the path is built by a plain class
   (`TrendGeometry`) with NUnit tests. Costs: a mode run every other time shows as dots, not a line.
2. **Dash the segment across the gap.** Keeps the eye on the trend and still marks the gap. Changes: two paths per
   line (solid and dashed). Costs: a dashed line still reads as a value between the runs, which nobody measured.
3. **Draw through, mark the missing run on the axis.** Changes: an axis tick. Costs: the line still claims a value
   where there is none; the mark is easy to miss.

**Why 1.** The rule since F31 is "a value nobody measured is null, never 0", and the honest drawing of null is no
line. Sources: d3-shape `line.defined` (d3js.org/d3-shape/line#line_defined); Microsoft Excel, *Display empty cells,
null values* ("Gaps"). **Commit:** the `TrendChart` commit of Part 3.

### 6. The run page's child components still read the run as JSON (Part 3, `BenchmarkRun`)

**What came up.** The brief names five files; the run page hands the run, as a `JsonElement`, to six more components
(`NotesPanel` through `NoteReader`, `SummaryPanel`, `AgreementPanel`, `ModeRatesList`, `RunSettings`, `GraderList`)
and about twenty readers that pick it apart by name. ADR 0045 wants the whole portal on the records.

1. **The five pages first, each reading the record for its own code, and the run handed on as JSON until each child
   switches in a commit of its own (picked).** Changes: a page reads the reply once as its record and still passes
   the same JSON down. Costs: for a while a page holds the run twice (the record and the JSON), and the children keep
   their name-by-name reads until they are switched.
2. **Switch every child in the page's commit.** Changes: a page commit becomes twenty-odd files. Costs: commits nobody
   can review in ten minutes, against the guide's step size.
3. **Leave the children on JSON for good**, the record only at the page. Costs: ADR 0045's point (a renamed field
   fails the build) is lost exactly where the fields are read.

**Why 1.** It keeps each commit reviewable and moves every reader to the record in the end. **Commit:** the
`BenchmarkRun` commit of Part 3; the children follow one by one as far as this run gets (the final report says how
far).

### 7. The runs list's rows: switch them to the contract's record, or build them from it (Part 3, `Benchmarks`)

**What came up.** The page now reads the list as `BenchmarkListReply`, whose runs are `StoredRunRow`s. But the run's
row (`RunListItem`), the cost estimate (`RunEstimate`) and the model filter were written and tested for the page's
own small `RunSummary` (id, check, repeats, status, start, total, store error, model names, re-scored from).

1. **A factory on the small record, `RunSummary.Of(StoredRunRow)` (picked).** Pattern: Fowler's *Replace Constructor
   with Factory Function*, the `Record.Of` the guide names. Changes: one method with its tests; the page maps the
   list once when it reads it. Costs: two shapes of a stored run in the browser app until the children move.
2. **Switch `RunListItem`, `RunEstimate` and their tests to `StoredRunRow`.** Changes: three classes and three test
   files in this commit. Costs: a page commit that rewrites children, against finding 6's rule of one child per
   commit; the tests build fifteen-field rows where they need five.
3. **Let the page keep `RunSummary` and read it from the JSON by name, as before.** Costs: the page picks a
   `JsonElement` apart, which the brief rules out.

**Why 1.** It keeps the page on the contract and the children untouched, and the mapping is one tested place to
delete when the children move. Sources: Fowler, *Refactoring* 2nd ed., *Replace Constructor with Factory Function*;
ADR 0045. **Commit:** the `Benchmarks` commit of Part 3.

### 8. Where the run in progress lives once the page splits (Part 3, `Benchmarks`)

**What came up.** Following a run (its event stream, the table of turns, the open turn, the costs at the end) was
about half the page's code. The page starts a run (a card's button), resumes one (a reload) and follows a re-score
(a run's row); the panel shows it.

1. **The panel follows the run; the page only hands it over and hears when it ends (picked).** The page sets the run
   (and whether to read the store first); `LiveRunPanel` streams it, keeps its table in a plain `LiveTurns` class,
   and raises `OnEnded` with the finished run, after which the page reads the list again. Pattern: Blazor's
   parameters down, `EventCallback` up (Microsoft, *ASP.NET Core Razor components*, "EventCallback"), with the state as
   a plain class (Fowler's *Extract Class*). Changes: a component that owns its stream, and a tested class. Costs:
   the following runs in the panel's `OnParametersSetAsync`, keyed on the run's id, so handing it the same run again
   doesn't follow it twice.
2. **The page follows, the panel only draws.** The page keeps the stream and passes `LiveTurns` down. Changes: the
   page's code-behind stays the owner. Costs: the page's code-behind goes back over 200 lines, holding two concerns
   (the list and the live run).
3. **A scoped service for the live run**, injected into both, with a `Changed` event (as `RunNotesState`). Changes:
   a new service registered in `Program.cs`. Costs: a run's state outlives the page (a second tab, a navigation away),
   which nothing asks for; more moving parts for one page.

**Why 1.** It gives each part one concern (the page the list, the panel the run) and keeps the rule that a component
never reaches into its page. Sources: Microsoft Learn, *ASP.NET Core Razor components* ("Component parameters",
"EventCallback"); Fowler, *Refactoring* 2nd ed., *Extract Class*. **Commit:** the `Benchmarks` commit of Part 3.

### 9. The page's start-run body: a record of its own, or the API's moved to the contract (Part 3, `Benchmarks`)

**What came up.** The page posted `new { repeat = … }`, which the guide now rules out (*never an anonymous object*).
The API reads it as its own internal `StartRunRequest(Repeat, Web, Replay)`, which parses the eval's web settings.

1. **A named body in the browser app, `StartRunBody(Repeat)` (picked).** Changes: one record and a test of its wire
   shape. Costs: the API and the page each name the body; a rename on one side is caught by the test only for the
   page's side.
2. **Move `StartRunRequest` to the contract now.** Changes: the API's endpoint file and its parsing, in a page
   commit. Costs: finding 3 already put the bodies' move "when a change touches them" on the API's side; this
   commit doesn't touch the API.
3. **Send a `StartRequest` (check and repeats) as it is.** Costs: a stray `check` and a field named `repeats` that
   the API doesn't read; the run would start with one repeat.

**Why 1.** It meets the rule in this commit without reaching into the API, and finding 3's move takes it over when the
API's body changes. Sources: the guide's *JSON through records*; finding 3. **Commit:** the `Benchmarks` commit of
Part 3.

### 10. The runs list's heading printed Razor code as text (Part 3, `Benchmarks`)

**What came up.** The old page's `<strong>Runs@if (chosen is not null) { … }</strong>`: Razor reads `Runs@if` as an
e-mail address, so the heading shows "Runs@if (chosen is not null) { · eval-beta }", and with no check chosen
`@chosen.Id` would throw. The pin (`Benchmarks.html`) was written from the old page, so it holds the bug.

1. **Keep it through the rebuild, then fix it in the next commit with the golden's one-line diff (picked).** Changes:
   the rebuild proves itself against the untouched golden; the fix is its own commit. Costs: the rebuild carries the
   bug's text on purpose for one commit.
2. **Fix it in the rebuild's commit.** Costs: the golden changes in the same commit as the split, so the pin no
   longer proves the split kept the page.
3. **Fix it first, before the pin.** Costs: the golden is no longer the old page's markup, and the fix has no pin
   before it.

**Why 1.** Each commit proves one thing: the split changes nothing, then the fix changes one line. Sources: Fowler,
*Refactoring* 2nd ed. ("the two hats": refactor or add behaviour, never both at once). **Commit:** the `Benchmarks`
commit of Part 3, and the fix after it.

### 11. The explorer's conversation chips: reuse the live table's `ScoreChips`, or keep a copy (Part 3, `RunExplorer`)

**What came up.** The explorer's `Chips` and `IngredientChips` read the same score keys, in the same order and
colours, as `ScoreChips` (made for the live run's table in the `Benchmarks` commit). The two differ only at the end:
the live table adds "shops N" and "foute N" from the scores, the explorer adds "errors N" from the conversation's own
error count. The copies also differ at the edges: `ScoreChips` shows a numeric `follow_up` and skips a count that
isn't a whole number, where the explorer's copy skipped the first and would have thrown on the second.

1. **Split `ScoreChips` into the chips every view shares and the live table's own two, and build the explorer's chips
   from the shared ones plus its errors (picked).** Pattern: Fowler's *Extract Function* and *Remove Duplicated
   Code* ("Once and Only Once"). Changes: `ScoreChips.Common` (with a test), `ExplorerScores.Chips` on top of it.
   Costs: the two edge cases above now behave as the live table does, which no stored run has hit; the pins show
   the same markup.
2. **Keep the explorer's own copy, moved to a plain class.** Changes: one more class of score reads. Costs: two
   lists of the same score keys to keep in step, in the files step 7c has to empty.
3. **Pass the live table's extra chips through a flag** (`ScoreChips.Of(role, scores, live: true)`). Changes: one
   method. Costs: a boolean argument that switches behaviour (Fowler's *Remove Flag Argument* warns against it).

**Why 1.** One place reads the shared chips' keys, so step 7c has one fewer reader to move, and the explorer's chips
can't drift from the live table's. Sources: Fowler, *Refactoring* 2nd ed., *Extract Function*, *Remove Flag
Argument*; Hunt and Thomas, *The Pragmatic Programmer* (DRY). **Commit:** the `RunExplorer` commit of Part 3.

### 12. Where a turn's replay and the chats side by side live once the explorer splits (Part 3, `RunExplorer`)

**What came up.** The explorer replays a turn as typed (level 3) and plays the chats side by side (a dialog over any
level). Every move in the explorer (deeper, back, another repeat) stops the turn's replay and forgets it; the chats
are stopped only by closing them or playing another conversation.

1. **The turn's replay stays in the explorer's code-behind, the chats dialog owns its playing (picked).** The level-3
   component only shows the replay and raises "replay" and "stop"; the dialog plays on its own and the explorer bumps
   a counter to ask it to play again. Pattern: Blazor's parameters down, `EventCallback` up, as finding 8, with the
   fetching and the pace in a plain `TurnReplays` class. Changes: one stateful dialog and a stateless level. Costs:
   the explorer's code-behind keeps the replay's fields (it stays under 200 lines).
2. **The level-3 component owns the replay too.** Changes: the explorer drops the replay. Costs: going back to the
   same level (the crumb you are on) keeps the component, so the replay wouldn't stop as it does now, unless the
   level's key changes on every move, which replays the slide-in animation.
3. **A scoped replay service** shared by the explorer and its levels. Changes: a service in `Program.cs`. Costs: a
   replay that outlives the page, which nothing asks for (finding 8's option 3).

**Why 1.** It keeps the old rule (any move stops the replay) without a trick, and gives the dialog, which no move
touches, its own life. Sources: Microsoft Learn, *ASP.NET Core Razor components* ("Component parameters",
"EventCallback"); finding 8. **Commit:** the `RunExplorer` commit of Part 3.

### 13. Where a run keeps its prompt hashes per agent (step 7a.3)

**What came up.** A run keeps its prompt hashes as one joined setting (`prompt_hashes`, `a,b,c`): the set of prompts
it *used*. A run where the cook wasn't called has fewer hashes without any prompt changing, so the trend can't compare
two of those strings. It needs each agent's hashes, per mode (a trend line is a mode), kept somewhere the trend reads.

1. **A typed field on the run, `Prompts`: a list of `AgentPrompt(mode, agent, hashes)` (picked).** Pattern: PoEAA's
   *Embedded Value* (a small value object kept inside its owner's record; in MongoDB an embedded array on the run's
   document, as the run's slices already are). Changes: one record in `BenchmarkDocuments`, the ledger join fills it,
   `PromptDocuments` maps it for MongoDB (as `SliceDocuments` does), a contract test holds both stores. Costs: one
   more run field to carry through both stores; a run stored before it reads `null`, so it never gets a marker.
2. **More keys in the run's settings** (`prompt_hash:{mode}:{agent}` = the hashes). Pattern: Entity-Attribute-Value
   (a string bag of name and value). Changes: the ledger join only; no store change. Costs: meaning hidden in key
   names that the trend has to parse back; the run page lists every setting, so it would show dozens of 64-character
   hashes under *What it ran with*.
3. **The hash and the agent on every stored request**, and the trend works out each agent's hashes when it reads.
   Pattern: Fowler's *Replace Derived Variable with Query* (keep the facts, compute the summary on read). Changes: two
   fields on `Request` in both stores and in its exports. Costs: the same 64 characters on every request of a run (a
   run has hundreds), and a field on a record many readers and the protocol export share, for one reader.

**Why 1.** It keeps the per-agent hashes as what they are, a small typed summary of the run, read back the same in
both stores, with `null` for "not known" (ADR 0007), and it follows the run's slices, the nearest precedent. The joined
`prompt_hashes` setting stays as it is. Sources: Fowler, *PoEAA*, *Embedded Value*; MongoDB, *Embedded Data Models*
(mongodb.com/docs/manual/data-modeling/embedding); Fowler, *Refactoring* 2nd ed., *Replace Derived Variable with
Query*. **Commit:** step 7a.3 (a).

### 14. Which agent a ledger row belongs to (step 7a.3)

**What came up.** The brief groups a run's rows by the task tag: the main session, `sub-session:{peer}`,
`minion:{role}`. Two kinds of row would blur that. A model judge's row carries the scenario as its task, like the main
session's, but its own prompt; and a summary or compaction request inside a session uses its own prompt only when it
happens. Mixed into the agent's hashes, either would make two runs on the same prompts look different. And the main
session's prompt differs per mode, so the hashes must be per mode as well.

1. **The agent is the task tag (any other task is the main session), with the purpose added when the row is not a
   turn or its continuation, per mode from the row's run tag (picked).** A judge is `main (grading)`, a summary in the
   cook `sub-session:puls-cook (summary)`. Pattern: a *composite key* (PoEAA's *Identity Field* with several columns):
   what the prompt belongs to is mode, agent and purpose together. Changes: one small rule in `AgentPrompts`. Costs:
   names a bit longer in the tooltip.
2. **Only turn and continue rows count.** Changes: the same rule, dropping the rest. Costs: a change to the judge's
   prompt never shows on the trend, and it does change what a run measures.
3. **The task tag alone, as the brief says.** Costs: a run where a result was summarised gets a false marker, which
   the brief rules out ("never a false one").

**Why 1.** It keeps the brief's grouping and its rule (mark only an agent present in both runs whose hash differs),
and gives a judge or a summary its own name, so it is compared only with itself. Sources: Fowler, *PoEAA*, *Identity
Field* (compound keys). **Commit:** step 7a.3 (a).

### 15. Which notes a trend point carries (step 7a.3)

**What came up.** A point is one run in one mode. A run's notes are on the run or on one of its conversations, and a
conversation belongs to a mode.

1. **The notes on the run, plus the notes on this mode's conversations (picked).** Changes: the point filters the
   run's notes by the conversation's mode. Costs: a note on another mode's conversation shows only on that mode's line.
2. **Every note of the run on every point.** Costs: a note about the cook's conversation shows on Puls's line too.
3. **Only the notes on the run itself.** Costs: a note on a conversation never reaches the trend.

**Why 1.** Each point shows what was said about what it measured. No pattern catalogue covers this; it follows the
trend's own rule that a point is a run in a mode (*Specification*-style filtering, Evans and Fowler). **Commit:** step
7a.3 (b).

### 16. Where the chart draws its markers (step 7a.3)

**What came up.** A run can carry notes and a prompt change on several lines at once, and the full chart has eleven
panels side by side.

1. **In every panel, once per run: a dashed upright line where a prompt changed and a small dot at the top for notes,
   each with its tooltip (picked).** Pattern: *annotations* on a time series (Grafana's annotations, a marker at a
   time drawn on every panel; Tufte's *small multiples* keep the same marks in every panel). Changes: the markers come
   from a plain class (`TrendMarks`) with tests, and each panel draws them; the legend names both. Costs: the same
   marker repeats across eleven panels.
2. **One strip of markers above the panels.** Costs: the panels sit in a grid, so a strip's x positions don't line up
   with any panel's runs.
3. **On the points themselves** (a ring around a point with a note). Costs: a point is per line, and a note on the run
   would ring every line's point; a prompt change has no point of its own to mark.

**Why 1.** The reader sees a change right where the line moved, in whichever panel they are reading. Sources: Grafana
docs, *Annotate visualizations* (grafana.com/docs/grafana/latest/dashboards/build-dashboards/annotate-visualizations);
Tufte, *The Visual Display of Quantitative Information*, small multiples. The card's small chart draws no markers (it
is the cost at a glance). **Commit:** step 7a.3 (c).

### 17. Where the eval list gets what an eval is before it ever ran (step 7b (i))

**What came up.** The row asks each eval to show its graders (name, version, kind), its dataset and categories, and
one never run is listed with its graders. The runner's `--list` line carried only the name, identity, version, what it
proves and the manual, so the API had nothing else to show for an eval with no run. Modes can't come from the listing:
routing reads its baseline from the instance (`GET /toolsets`), so listing them would mean calling the API.

1. **The runner's listing line grows: `dataset` (name, version), `categories` (each once, in the order the scenarios
   first name them) and `graders` (name, version, kind as the protocol writes it) (picked).** Pattern: a Data
   Transfer Object widened in place (Fowler, *PoEAA*, *Data Transfer Object*): the one record the API reads from the
   runner carries the whole description. Changes: `ListedEval` and `EvalCatalog.Listing` in the runner, the API's
   `EvalLine` reads the three (optional, so an older line still reads; an unknown grader kind refuses the line), and
   each `BenchmarkCheck` keeps its line. Costs: the listing line is longer; modes stay with the runs.
2. **Read them from the eval's latest stored run** (its first line carries graders and dataset). Changes: the API
   only. Costs: an eval never run shows no graders, which is what the row asks the list to show.
3. **A second runner command, `--describe {eval}`, per eval.** Pattern: *Lazy Load* (PoEAA). Changes: a new command
   and a process per eval on each list. Costs: N process starts per page load, for facts that never change between
   builds.

**Why 1.** The catalogue is already the one place that knows every eval, and the plan wants the portal's list to come
from it (`run --list`), never from a list kept by hand; one wider line keeps that, at start-up cost only. Sources:
Fowler, *PoEAA*, *Data Transfer Object*, *Lazy Load*. **Commit:** step 7b (i), the listing commit.

### 18. How much of each eval's runs and trend the eval list carries (step 7b (i))

**What came up.** The row asks each eval to show every run with its outcome and cost, its overall and category lines
(5c) and its pass@k and pass^k trend (5a). The trend and the category charts already have their own route
(`GET /admin/benchmarks/checks/{check}/trend`), which joins every finished run's conversations and slices; the list
is one call for every eval.

1. **The list carries each eval's newest runs (up to 50), each as the stored run's own row (`StoredRunRow`: outcome,
   cost from the ledger, models, graders) beside its modes' rates (`ModeRow`: pass@k, pass^k, k), plus the modes and
   models the eval ran on; the charts stay on the trend route, which the page opens per eval (picked).** Pattern:
   *Remote Facade* with coarse-grained *Data Transfer Objects* for the list (Fowler, *PoEAA*), the heavy detail behind
   its own call (*Lazy Load*). Changes: `EvalListReply`, `EvalRow`, `EvalRunRow` in the contract (reusing
   `StoredRunRow` and `ModeRow`), one store read per eval. Costs: the page needs a second call to draw an eval's
   charts.
2. **The whole `TrendReply` inside every eval's row.** Changes: the list computes every eval's trend and categories.
   Costs: the slowest read the portal has, once per eval on every list load, for charts most visits never open.
3. **Only the last run per eval**, the rest fetched when an eval is opened. Costs: "every run with its outcome and
   cost" needs another call per eval, and the list can't say which models an eval ran on.

**Why 1.** One call answers the row's list-level questions (what each eval is, whether and when it ran, what it cost,
how each mode passed), reusing the records the runs page already reads, and the trend keeps its one route.
`never_run` is `true` only when the store answered with no runs; without a store it is `null`, not known (ADR 0007's
rule: unknown is null, never a guessed value). Sources: Fowler, *PoEAA*, *Remote Facade*, *Data Transfer Object*, *Lazy
Load*. **Commit:** step 7b (i), the `GET /admin/evals` commit.

### 19. What `ApiNamesNoProductTests` reads, and how its two exceptions are held (step 7b (iii))

**What came up.** The row asks for a ratchet that holds "the API names no product", with two exceptions (the web app
reference, the stored `puls_calls` key). It doesn't say which files are "the API", where the product names come from,
or whether comments count.

1. **The C# and project files of `src/louis-agent.api` and `src/louis-agent.api.contracts`, comments included; the
   products read from `identities/louis-agent.identity.*`; the exceptions held apart from the ratchet (a whole file,
   or a word anywhere), and the rest a "not yet renamed" list that may only shrink (picked).** Pattern: a ratchet, as
   `FileSizeTests` and `RazorHasNoCodeTests` (Fowler, *Strangler Fig*: the old names die one commit at a time).
   Changes: one test file. Costs: `Properties/launchSettings.json` is left out (its `puls` profile is how a person
   starts the host as Puls, the host's choice of identity, not the API's code); a comment that names Puls counts.
2. **Identifiers only (a Roslyn walk), comments left out.** Costs: a summary saying "Puls's turn" stays, and the
   API's prose keeps naming a product; Roslyn's syntax package is already referenced only by the analyzers.
3. **A fixed list of words (`PULS_API`, `PulsCalls`, `puls_benchmarks`).** Costs: a new identity, or a new word
   naming Puls, passes unseen.

**Why 1.** It finds a product named anywhere a reader of the API would see it, and grows with the identities. The
exceptions are two lines a reader can check, and the waiting list says exactly what the rename still has to do.
Sources: Fowler, *StranglerFigApplication* (martinfowler.com/bliki/StranglerFigApplication.html); the repo's own
`FileSizeTests`. **Commit:** step 7b (iii), the ratchet commit.

### 20. The files the rename moves were past their limits: bring them up first, or in the move (step 7b (iii))

**What came up.** A moved file counts as in the commit, so every file under `Benchmarks/` and its tests had to meet
the rules whole: about 490 classes and methods had no summary, `MongoBenchmarkStore` held 454 lines of code and
`BenchmarkRecorder` 208 (both on `FileSizeTests`' list), and `Runs/` and `Store/` held 19 and 13 files.

1. **Bring the files up in place first, in their own commits, then move them in one mechanical commit (picked).**
   The Mongo store becomes a Repository with a Data Mapper per document (`RunDocuments`, `ConversationDocuments`,
   `TurnDocuments`, `RequestDocuments`, the shared `BsonValues`); the recorder keeps the subscription, the run's state
   goes to `RunRecording` (its switch arms named methods with guard clauses) and the stop-after-the-first-failure
   rule to `GuardedWrites`. Pattern: Fowler's *Extract Class*; PoEAA's *Repository* and *Data Mapper*. Changes: the
   bring-up commits touch the same files the move does. Costs: more commits; the move's diff is the namespace map
   only, which is what the plan's Risks note says its review should be.
2. **Everything in the move's commit.** Costs: one commit of about 150 files where the summaries hide the renames.
3. **Move now, bring up later.** Costs: breaks the rule that a file in a commit is brought up whole.

**Why 1.** Each commit stays reviewable for one thing: what a file says, or where it lives. Sources: Fowler,
*Refactoring* 2nd ed., *Extract Class*; Fowler, *PoEAA*, *Repository*, *Data Mapper*. **Commit:** step 7b (iii), the
bring-up commits before the move.

### 21. How the old `/admin/benchmarks/…` routes keep answering beside `/admin/evals/…` (step 7b (iii))

**What came up.** The row asks for `/admin/evals/{name}/…` routes with the old `/admin/benchmarks/…` kept until a
TODO line removes them, and a saved link must keep opening. It doesn't say how the two names are served.

1. **A route group per name: each endpoint file maps its routes relative to the group it's given, and
   `AdminEndpoints` calls them once for `/evals` and once for `/benchmarks` (picked).** Pattern: *Parallel Change*
   (expand, migrate, contract; Fowler, martinfowler.com/bliki/ParallelChange.html), with ASP.NET Core's route
   groups. Changes: the endpoint files drop the `/benchmarks` prefix; one `Prefixes` list; the trend gets its new
   address beside its old one by hand (the old one had a `checks/` in it). Costs: the route golden lists every route
   twice until the alias goes; removing it is one word and two lines.
2. **URL rewriting** (`Microsoft.AspNetCore.Rewrite`, in the framework): one rule maps `/admin/benchmarks/…` to
   `/admin/evals/…`. Costs: the rewriter has to run before routing, which the minimal host and the test host would
   each have to order by hand; the old routes vanish from the golden, so the access rule on them isn't pinned.
3. **Each route mapped twice, line by line.** Costs: twenty duplicated lines across five files, and removing the
   alias is a hunt.

**Why 1.** Both names are real routes with the same handler, gate and access rule, pinned by the golden, and the
contract step is a one-word change the TODO line names. Sources: Fowler, *ParallelChange*; Microsoft, *Route groups*
(learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers#route-groups). **Commit:** step 7b (iii),
the routes commit.

### 22. Which store settings the rename covers, and how Puls keeps its database (step 7b (iii))

**What came up.** The row names the database (`puls_benchmarks` gives way to `evals`, `BENCHMARKS_MONGO_DATABASE`
read as an alias, Puls's compose sets `EVALS_MONGO_DATABASE: puls_benchmarks`). It doesn't say whether the store's
address, `BENCHMARKS_MONGO_URL`, moves too, and the Azure app and the Rider profile also reach Puls's store without
going through the compose file.

1. **Both settings get `EVALS_` names, the old ones read after them; every Puls entry point names
   `puls_benchmarks`: the compose (`${EVALS_MONGO_DATABASE-${BENCHMARKS_MONGO_DATABASE-puls_benchmarks}}`, so a value
   set in `config/.env` under either name still wins), the Azure app and the `puls` Rider profile (picked).**
   Pattern: *Parallel Change* on configuration (expand now, contract by a TODO line). Changes: `AdminOptions`, the
   two "no store" texts, the examples and the setup doc. Costs: the CI workflow and the Mongo contract tests still
   use the old address name until the TODO line moves them (they read the environment themselves).
2. **Only the database renamed, as the row says.** Costs: one store with an `EVALS_` database and a `BENCHMARKS_`
   address, and a second rename later.
3. **Rename both and move CI and the contract tests now.** Costs: touches the CI workflow and three more test files
   in this commit; on the machines where `config/.env.secrets` holds the old name nothing breaks either way.

**Why 1.** One consistent name for the store, nothing anywhere has to change today, and Puls's Azure app (which the
compose file doesn't reach) doesn't silently start writing to an empty `evals` database. Sources: Fowler,
*ParallelChange*; Microsoft, *Configuration in ASP.NET Core* (environment variables). **Commit:** step 7b (iii), the
store settings commit.

### 23. Where `puls_calls` becomes `agent_calls`, and where it stays (step 7b (iii))

**What came up.** The row renames `PulsCalls` to `AgentCalls` and allows "the stored `puls_calls` key, read as an
alias until a TODO line removes it". The same key is on three wires: the Mongo document of a turn, the API's stored
run reply (the contract's `TurnRow`, which the Puls pages read), and the eval protocol's `turn` line (written by the
runner, read by the API and by the Puls page live, and written back by the API's export). The row names only the
stored one.

1. **C# names `AgentCalls` everywhere in the API and the contract; the store writes `agent_calls` and reads
   `puls_calls` as the alias; the reply's wire name follows the record (`agent_calls`, the Puls page reads it in the
   same deploy); the protocol keeps `puls_calls` until CL22's version 2 (picked).** Pattern: *Parallel Change* for the
   store (Fowler), *Tolerant Reader* for its old documents. Changes: `TurnDocuments`, the records, the reply golden
   and three Puls fixtures. Costs: `puls_calls` stays in the API in two places, the alias read and the protocol, both
   under the ratchet's one allowed word.
2. **Rename the protocol's key too** (the runner writes `agent_calls`, readers accept both). Costs: the runner's
   recorded Python fixtures and transcripts hold `puls_calls` ("recorded data stays as recorded"), so the writer's
   comparison with them breaks; it is CL22's protocol change, done early and halfway.
3. **Keep every wire name, rename only C#** (`[JsonPropertyName("puls_calls")]` on the contract). Costs: the API's
   reply keeps a product's name on its wire for good, which ADR 0037 is about.

**Why 1.** It does what the row says for the stored key, keeps the reply honest, and leaves the protocol to the one
change that versions it. Sources: Fowler, *ParallelChange*, *TolerantReader* (martinfowler.com/bliki). **Commit:**
step 7b (iii), the `AgentCalls` commit.

### 24. How callers get the four record stores (step 7b, the store split)

**What came up.** The plan's *Step 7b splits the store by record* names the four stores (`IRunStore`,
`IConversationStore`, `ITurnStore`, `IRequestStore`) and says `IEvalStore` "goes, or stays only as the four together
for the recorder". About forty places take an `IEvalStore` today (the recorder, every admin handler, the trends, the
export, `ApiTestHost` and some thirty tests), and two things reach across the records: deleting a run with everything
under it, and a note, which may only go on a run that exists. The entry says nothing about notes, reviews,
calibrations or slices.

1. **`IEvalStore` stays as a Facade over the four: it inherits each record's interface, adds the notes and the
   cascading delete, and `InMemoryEvalStore` and `MongoEvalStore` hand each call to the record's own store (picked).**
   Pattern: *Facade* (Gang of Four) over one *Repository* per record (Fowler, PoEAA), with *Interface Segregation*
   (Martin): since `IEvalStore` *is* each record interface, a caller that needs only runs can take `IRunStore`
   later. Changes: four interfaces with a MongoDB and an in-memory class and a contract test each; the two facades
   shrink to one-line hand-offs, the notes and the delete. Costs: a hand-off line per method in each facade, and a
   record-level delete per store (`IRunStore.DeleteAsync`, the conversations', turns' and requests' of a run) that the
   facade's `DeleteRunAsync` calls; the test decorator in `EvalRecorderTests` gains a line per new member.
2. **Callers take the stores they need** (constructor injection, Interface Segregation all the way): `IEvalStore` goes;
   each handler, report and the recorder names its record stores; the delete and the notes get a small service of
   their own. Costs: every handler's constructor, `Program.cs`, `ApiTestHost` and about thirty tests change in the
   same commits, the four commits stop being one record each, and every test has to build up to four stores.
3. **`IEvalStore` as a Facade with the four as properties** (`store.Runs.GetRunAsync(…)`). Costs: every call site
   changes (about a hundred and forty lines), for a name at each call that the type already says.

**Why 1.** It is what the plan allows ("stays only as the four together for the recorder"), it keeps each commit to
one record and green, and it leaves option 2 open caller by caller. Notes, reviews, calibrations and slices stay where
they are: notes in the facade (they reach across to the runs, and the plan's entry doesn't move them), reviews and
calibrations already have their own stores (`IHumanReviewStore`, `ICalibrationStore`), and slices are part of the run
document (`SliceDocuments` maps them inside it). Sources: Gamma et al., *Design Patterns*, Facade; Fowler, *Patterns
of Enterprise Application Architecture*, Repository and Data Mapper; Martin, *Agile Software Development*, the
Interface Segregation Principle. **Commits:** step 7b, the four "store splits by record" commits.

### 25. What the commit's build argument is called (step 7b, the images stamp their commit)

**What came up.** The brief suggests `ARG GIT_COMMIT` with `GIT_COMMIT=$(git rev-parse --short HEAD)`, but Puls's
image already takes the commit as `EVAL_COMMIT` (step 7a: `Dockerfile.puls`, `docker-compose.puls.yml`, the
*Run the evals* guide), and only the code-review API's image and the cloud build lacked it.

1. **Keep `EVAL_COMMIT` and add it where it was missing: `Dockerfile.api`, the main compose file, `deploy.ps1 build`
   (picked).** Changes: three files and the guides. Costs: the name says what reads it (the eval runner) rather than
   where it comes from.
2. **Rename to `GIT_COMMIT` everywhere, reading `EVAL_COMMIT` as an alias** (Fowler, *Parallel Change*). Costs: two
   names in the compose files until a TODO line removes one, for a variable nothing else reads yet.
3. **`GIT_COMMIT` for the API's image only.** Costs: two names for one thing, one per image.

**Why 1.** One name for one value, already documented; a later reader of the commit (a version endpoint, say) can
rename it then. Sources: Docker, *Build variables* (`ARG`, `--build-arg`); Compose, *build.args*; Azure,
`az acr build --build-arg`. **Commit:** step 7b, the images stamp their commit.

## Report: where the run stopped

Louis stopped the run at Part 6 (2026-10-28: "stop at part 6, leave unattended mode when you hit part 6"), so Part 6
(step 7c, protocol v2) is not started; the paid runs and step 8 were never in scope. Every Part below was pushed
after the whole solution built and every unit and integration test passed. Nothing is stashed.

**Tests.** At the start (`a86b96e5`): 3,692 passed, 0 failed. At the end (`a79ce036`): **3,980 passed, 0 failed,
78 skipped** (the integration tests that need MongoDB, Chrome, SearXNG or the network skip on this machine, as
written; so the new MongoDB record stores of the store split were never run against a real MongoDB).

| Part | Done | Commits (first … last) | Pushed |
|---|---|---|---|
| 1 · ADR 0045 on the admin endpoints | yes: the contract project, `ApiError`, typed answers on `AdminHandlers` | `8e18ffeb` … `3b86e7f7` | `a86b96e5..3b86e7f7` |
| 2 · The endpoint sweep | yes: every endpoint file a handler class with declared answers, replies in the contract, JSON pinned first; folders by concern; CL20 ticked | `82b44beb` … `fa651dee` | `3b86e7f7..fa651dee` |
| 3 · 7b (ii) the admin pages | yes: `ChartInfo`, `TrendChart`, `BenchmarkRun`, `Benchmarks`, `RunExplorer` markup with code-behind, under 200 lines, off both lists; review note settled (finding 5) | `3885b038` … `88830ac1` | `fa651dee..88830ac1` |
| 4 · 7a.3 notes and prompt changes on the trend | yes: per-agent prompt hashes on the run, the markers in the API and the chart | `2934550a` … `9ba3b144` | `88830ac1..9ba3b144` |
| 5 · 7b (i) and (iii), the store split, the build arg | yes: `GET /admin/evals` and its page; `Benchmarks/` to `Evals/`, ADR 0037, the ratchet, route aliases, `EVALS_MONGO_*`, `AGENT_API`, `agent_calls`; four record stores behind the `IEvalStore` facade; `EVAL_COMMIT` build arg | `3422478b` … `a79ce036` | `9ba3b144..a79ce036` |
| 6 · 7c protocol v2 | **not started** (stopped by Louis) | | |

**Findings** (each above with its three options, sources, pick and commit): 1 `Of` as C# 14 extension members;
2 `CreatedJson` for a 201 on a snake_case wire; 3 the session request bodies stay in the API for now (a TODO line);
4 Puls keeps its words in plain classes; 5 a trend line breaks at a gap; 6 the run page's children still take the run
as JSON; 7–10 the Benchmarks page (rows from the record, the live run's panel, a named start body, the heading bug kept
then fixed); 11–12 the explorer (shared chips, the replay's home); 13–16 the trend markers (a typed `Run.Prompts`, the
agent from the task tag, which notes, where marks draw); 17–18 the eval list (a richer listing line; runs on the list,
charts on the trend route); 19–23 the rename (the ratchet, bring-up before the move, route aliases, store settings,
`puls_calls`); 24 the record stores behind an `IEvalStore` facade; 25 the build arg keeps the name `EVAL_COMMIT`.

**Worth Louis's eye.**
- Five commits fixed earlier ones in this run (never by rewriting history): long comment lines (`f838872d`), a missing
  feature-doc line (`be5b3e6f`), strings that should have been enums (`54267013`), trend goldens that held South
  African times and would have failed on CI (`632f4410`), and the Benchmarks heading bug (`49ab6a23`).
- Tests written after their code, so never seen failing: `CreatedJsonTests`, `StoredRunHandlersTests`,
  `SessionHandlersTests`; and some guard cases in 7a.3 that held before the code (the agent's report says which).
- `CategoryTrendsTests` changed one expectation on purpose (finding 5: no stroke across a gap).
- Still on JSON: the run page's child panels (finding 6); the Puls pages still call the old `/admin/benchmarks/…`
  routes and read `BenchmarkListReply`; the Mongo contract tests and CI read only `BENCHMARKS_MONGO_URL` (TODO lines).
- Folders still over about six files that the run added to rather than restructured: `tests/louis-agent.api.tests/
  Endpoints` (24), `api.tests` root, `evals/…/Running`, `Admin/Replies`, the evals `Store/`.
- Small things noted, not fixed: the bare `aria-checked`; the trend markers can't be reached by keyboard; a timing test
  that flaked once under the full run; the namespace `Evals.Store.Runs` beside `Evals.Runs`; the contracts in
  `Contracts/Records/` keep the namespace `LouisAgent.Api.Testing`.
- Much of Parts 3 to 5 was done by subagents under the same rules and the same commit gate; every Part was checked here
  with a whole-solution build and test run before it was pushed, but not every summary they wrote was read here.
