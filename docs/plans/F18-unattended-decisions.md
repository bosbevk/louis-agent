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
