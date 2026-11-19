# louis-agent.web.evals — design

> **Kind:** Razor library (`net10.0`) · **Path:** `src/web/louis-agent.web.evals/` · **References:** louis-agent.web,
> louis-agent.evals.protocol · **Used by:** a site that opts into the eval studio (Puls, the code-review app, the starter), at
> `/admin/evals`, an area inside the admin's frame (F144) · **How to change them:** [Change the eval pages](../guides/change-the-eval-pages.md)
> **Also shows:** `component`, `sequence`

![The eval studio's screens: evals and their runs, answers to grade, the studio and the prompts](diagrams/louis-agent.web.evals.svg)

The studio is a library on the framework's web, not a part of it. A site that wants it names it; a site that doesn't
downloads none of it.

The rest of this doc says what the pages are for and what a site gives them. Then it walks the pages in the order you
use them: the Evals page's list and details, a run as it goes, the Review page, one run's page, the prompts and
imports, and the grades. It ends with the folders, to look things up, and the tests.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

## Purpose

These are the screens someone building an agent works in. There are the evals and their runs, the answers waiting
for a person's grade, the studio where you compose an eval, and a list of every built-in prompt. They read the API's
admin routes and nothing else, so they look and work the same on every site.

## What a site gives them

Almost nothing, and that's the point.

A site names the studio as one of its areas, with `.Evals()` in its `AddSiteAreas`. That one call registers the
pages' services (through `AddLouisAgentEvals`, which calls `EvalPages.AddTo`), adds the pages to the router
(`EvalLibrary.Assembly`), and gives the admin frame the studio's links (`EvalLinks`) and its card on the admin's home
(`EvalsCard`). Puls, the code-review app and the starter all name it.

The site's `app.scss` lists one name: `@forward "evals/pages";`. That forwards the six partials of shared rules, in
their order. Whatever a single component draws on its own sits beside it, in its `X.razor.scss`, and travels in the
site's styles bundle. None of these sheets draws a surface itself: a card, a box, a tip and a button are each a
kind included from the framework's surface tool ([the framework's web](louis-agent.web.md)), so the studio wears
the theme the site's page wears.

If the site wants the thin strip that says a run is going, it puts `<LiveRunStrip />` in its frame's `Top` slot. Puls
does.

If it wants the sources chart to sort the websites its agent read, it hands a `SiteKinds` list to `.Evals(...)`. Each
`SiteKind` is a name and the addresses it takes. A website goes under the first kind it fits. The first kind is
green, the kinds after it take the charts' series colours, and whatever fits none is grey and called *elsewhere*. If no kinds are named, everything is one grey slice.
Puls's list is `PulsSites`.

*Decided in:* [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md), CL58,
[ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md), F127.6, F144.

## How it works

The Evals page is the studio's front door: the list of evals, the details of the one you picked, and the run in
progress.

![The Evals page holds the list, the chosen eval's details that start a run, and the live panel that follows it](diagrams/louis-agent.web.evals-components-evals.svg)

The pages are organised by job, and they're in English, because they're the builders' screens.

*Decided in:* F53.

### 1. The Evals page: the list

`/admin/evals` is the Evals page. It lives under the admin's home, and its address is written once, as
`EvalAddress.Studio`. It's a list on one side and details on the other.

The list names every eval the runner has, including one that has never run. Each shows its last run's status, when it
ran, and for each mode the last pass@k with how it moved against that mode's run before (`EvalList`,
`EvalListItem`). The numbers are worked out in `EvalListView`, a Presentation Model (a plain class that holds what
the view shows, so the component only draws).

The eval you picked is in the address, as `?eval=` (`EvalAddress`). With none picked, it opens the eval that ran
last.

*Decided in:* F18, CL26, F53.2, F144.

### 2. The Evals page: the details

The details pane (`EvalDetails`) says what the eval proves, and has the controls that run it (`EvalRunControls`):
the number of repeats, the Run button, and the Manual button. The Run button shows an estimate from the last run
that really asked the model, so not a re-score and not a replay (`RunEstimate`).

Below that is the latest run as a scorecard, with a ring per mode. The runs before it run down a timeline, each tagged
*recorded*, *replay of …* or *re-scored from …*, and linked to its source. Then its trend, and its graders,
categories and modes in a fold. `EvalCardView` works out the card, and `EvalText` holds its words.

Under the card, every stored run is a row in a table (`RunsList`, `RunListItem`). A run can be re-scored from there,
and a recorded one replayed, through the page's one way of starting a run.

*Decided in:* FX28.

### 3. Following a run

The page starts a run and follows it. `LiveRunPanel` sits above the details, and stays put while you open another
eval. On every admin page, a thin strip says a run is going (`LiveRunStrip`). It reads the runner's current run when
it opens, and again every ten seconds while that run goes.

### 4. The Review page

`/admin/review` is the Review page. It shows the answers waiting for your grade, across all the stored runs, one at a
time. They come from the queue the API keeps, `GET v1/admin/evals/reviews/queue?reviewer=&limit=`, read for the name
you've kept. Typing a name or letting one go in the panel reads it again.

For each answer, you see where it's from: the eval, a link to its run, the mode, the repeat and the task
(`ReviewItemHead`). You see its place in the queue, a Skip button, and then the explorer's blind review panel,
unchanged (`ReviewPanel`, which reports a stored grade through `OnGraded`).

A grade or a Skip opens the next answer. Past the last one it was given, the page reads the queue again. With nothing
waiting, it says so, by name.

The keys 1, 2 and 3 grade pass, fail or unsure (`ReviewKeys`). The page's box takes the focus when an answer opens,
and the panel's note and name fields keep their keys to themselves.

The page's state is `ReviewQueueView`, a Presentation Model holding the items, which one is open, and which are graded
and left. Its words are in `ReviewQueueText`. How many answers wait is `ReviewQueueState`, a scoped Observer: the page
sets it, and `ReviewCountBadge`, beside *Review* in the sidebar, draws it. It's read once from the queue when the
portal opens.

*Decided in:* F53.3.

### 5. One run's page

`/admin/runs/{run}` is one run. Under its head comes the verdict (`RunVerdict`): for each mode, how many passed out
of those graded, and the change against the eval's run before, from the trend. It also shows the cost, the requests,
and the answers still to grade. The numbers are in `RunVerdictView`, a Presentation Model.

Then there's one tab at a time, and the tab is in the address (`?tab=`). Summary has the results by category, the
pass rates and the trends (`TrendChart`). Conversations has the explorer. Then Judge, Notes and Settings.

The explorer (`RunExplorer`) shows the run, its conversations side by side, a turn, and a request. It reports every
move as an `ExplorerPlace`, and the page puts that in the address (`?view=…&task=…&turn=…`). So Back goes up a
level, and a link opens exactly the same view. `RunAddress` is the one class that spells a run's address. Every
chart explains itself when you hover over it (`ChartInfo`).

*Decided in:* F53.1, [ADR 0065](../adr/0065-the-view-is-in-the-address.md).

### 6. The prompts, and old addresses

`/admin/prompts` lists every built-in prompt by what its header says: one table sorted by path, with each prompt's
name and file, its level, its description and the toolsets it always loads. The page that takes the packs' data
uploads, `/admin/imports`, isn't one of these: it's the framework's web's own.

The old `/admin/benchmarks` and `/admin/maatstawwe` addresses are gone, with no redirect, and `PortalAddressesTests`
fails if any file spells one again.

*Decided in:* CL27, F55, [ADR 0049](../adr/0049-nothing-is-compatible-before-a-sale.md).

### 7. The grades

The grades view shows every eval the same way, from the standard records alone. `GraderList` sits above the
explorer, with the run's eval, version, commit and graders, or one line saying it recorded none.

Each conversation's row gets `GradeChips`: its outcome, and one chip per grade, in the run's grader order, with the
reason on hover. A run from before grades were recorded shows "no grades", never 0. The readers and the chip list are
plain classes in `Admin/Grades/`, and the components are in `Components/Evals/Grades/`.

*Decided in:* F18 step 4.5c, F18 step 2a.

Here's where the pieces sit, and what a site adds:

![The eval studio's pages: from a site's program through the admin frame to the API](diagrams/louis-agent.web.evals-diagrams.svg)

And here's what happens when someone starts a run and watches it:

![Starting a run and watching it: the page starts it through the API, shows its turns as they finish, and lists the run with a link](diagrams/louis-agent.web.evals-diagrams-2.svg)

These are the types involved:

![The types: a site adds the pages in one call, a run's events build its live view and its stored lines its tree](diagrams/louis-agent.web.evals-interface.svg)

## Structure

This is the place to look things up. Paths are from `src/web/louis-agent.web.evals/`.

| Path | What |
|---|---|
| `EvalArea.cs`, `EvalServices.cs`, `EvalLibrary.cs`, `Admin/EvalPages.cs` | `.Evals()`, the area a site names; `AddLouisAgentEvals`, which takes the site's `SiteKinds`; `EvalLibrary.Assembly`, which brings the pages to the router; what the pages need from a site's services |
| `Pages/Running/Evals.razor`, `BenchmarkRun.razor` | The Evals page at `/admin/evals`: the list, the chosen eval's details, the run it starts and follows (F53.2); one stored run at `/admin/runs/{RunId}`: the head, the verdict, the tab bar and the open tab, the tab and the explorer's place read from the address and the run fetched only when its id changes (F53.1) |
| `Pages/Reviewing/Review.razor` | The Review page at `/admin/review` (F53.3): the queue read for the kept name, one answer open with its head and the blind review panel, the keys 1, 2 and 3, the count kept current for the sidebar |
| `Pages/Building/Prompts.razor` | Every built-in prompt as its header says it (CL27, ADR 0063), from `GET /v1/admin/prompts`: one table sorted by path, a row each with its name and file, level, description and the toolsets it always loads (a dash for none) |
| `Pages/Building/EvalStudio.razor` | The Studio page at `/admin/studio` (F49.5, [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)), named `EvalStudio` since the folders' `.Studio` namespaces would shadow `Studio`: the evals being composed on the left, and on the right the form as four numbered steps in the order they are done: *1 Compose* with *Save* and *View JSON*, *2 Check*, *3 Try* and *4 Publish* (the branch named, or a zip downloaded); each action saves first |
| `Components/EvalsPage/`, `List/`, `Details/` | The Evals page's parts: `EvalList` and `EvalListItem` (the list pane), `EvalDetails`, `EvalRunControls` and `ManualDialog` (the details pane), `EvalScorecard`, `EvalRunTimeline`, `EvalRunOrigin` and `EvalChecks` (the card) |
| `Admin/Evals/`, `Admin/Evals/List/` | Their models and words: `EvalCardView`, `RunLine`, `RunOrigin`, `GraderGroup`, `EvalText`; `EvalLine`, `ModeChange`, `EvalListView`, `EvalAddress` |
| `Components/LiveRun/`, `Admin/LiveRuns/`, `Admin/Starting/` | `LiveRunPanel` and `LiveTurnTable` (the run in progress, its turns as they come), `LiveRunStrip` (a run is going, on every page); the live turns (`LiveTurn`, `LiveTurns`, `TurnKey`); what a start sends (`StartRequest`, `StartRunBody`, `NoBody`) |
| `Components/Runs/`, `Components/Runs/Table/`, `Admin/Runs/` | A stored run's row and its buttons (`RunListItem`, `RunSettings`, `ReplayButton`, `RescoreButton`, `ReplayedFrom`, `RescoredFrom`), `RunsList` (the runs table); the runs' rules and words (`RunEstimate`, `RunSummary`, `CheckRuns`, `ModeLabels`, `RepeatSwitch`, `RunText`) and `Admin/RunModelFilter.cs`, the list's model filter |
| `Components/Head/`, `Admin/Store/` | `StoreBanner` (the store's state, `StoreStatus`), `EvalLinks` (the sidebar's links) and `EvalsCard` (the studio's card on the admin's home: the evals, the answers to review, the last run; F144) |
| `Components/RunExplorer.razor`, `Components/Explorer/`, `Admin/Explorer/` | A run at every level, its place handed in as `Place` and reported on every move as `OnMoved`: the crumbs and repeat picker (`Head/`), the overview's tiles, rings and cost bars (`Overview/`), the conversations (`Conversations/`), a conversation's turns, a request and two turns side by side (`Turns/`), the chats dialog (`Chats/`) and the shared parts (`Parts/`); in `Admin/Explorer/`, the run's tree (`Tree/`), where you are (`Place/`: `ExplorerView`, `ExplorerPlace`), the timeline, rings, legends and colours, a turn's replay, the words, and how a site sorts the websites read (`Sites/`: `SiteKind`, `SiteKinds`, `SiteCounts`) |
| `Components/RunPage/`, `Admin/RunPage/`, `Admin/Verdict/` | The run page's parts (F53.1): `RunHead`, `RunVerdict` and `RunTabBar`; `RunTab` and `RunTabs` (the tab, parsed once from `?tab=`, and the bar's entries), `RunAddress` (the one writer of a run's address); `RunVerdictView` and `ModeVerdict` (the verdict's numbers) with `VerdictText` |
| `Components/Summary/`, `Admin/Summary/` | `SummaryPanel`, `SummaryTable`, `RateBar`, `CategoryTrends`: a run's results by category, a rate as a bar; their readers and words |
| `Components/Trends/`, `Admin/Trends/` | `TrendChart`, `TrendPanel`, `TrendPanelHead`: a check's trend; its marks, measures and geometry in plain classes |
| `Components/Rates/`, `Admin/Rates/`, `Components/Notes/`, `Admin/Notes/` | `ModeRatesList` (each mode's pass rates) and `NotesPanel` (a run's notes), each with its reader, state and words |
| `Components/ChartInfo.razor`, `Components/Charts/`, `Admin/Charts/` | A chart's explanation on hover, from `Admin/Charts/Guides/`; `Ring`, `TokenStack`, `TokenLegend` (markup only); the charts' colours and maths (`ChartPalette`, the six series colours, `RingArcs`, `TokenKinds`, `ChartNumbers`) |
| `Components/Grades/`, `Admin/Grades/` | `GradeChips`, `GraderList` (F18 step 4.5c); the records both use (`GradeOutcome`, `GraderRow`, `GradeRow`, `HumanGradeRow`, `RunGraders`, `ConversationGrades`) and their words (`GradesText`); `Reading/`: the outcome parsed once (`GradeOutcomes`); `Chips/`: the chip list (`GradeChip`, `GradeChipList`) and an outcome's chip class (`ChipKind`) |
| `Components/Reviews/`, `Components/Reviews/Queue/` | `ReviewPanel` (a conversation's blind review; it reports a stored grade through `OnGraded` and grades through its public `GradeAsync`), `ReviewPill`, `AgreementPanel` (per judge, how often it agreed with the people), `CalibrationCard` (the judge check: the button, the latest report in sentences, the earlier ones folded); `ReviewItemHead` and `ReviewCountBadge` |
| `Admin/Reviews/`, `Admin/Reviews/Queue/` | The review's rules and words: `BlindReview` (the one place that knows `.human`), `ReviewStatus`, `ReviewText`, `AgreementText`, `AgreementView`, `CalibrationText`, `CalibrationView`, `ReviewerName`, `ReviewNote`, `ReviewerState`, `ConversationRef`; the queue's `ReviewQueueView`, `ReviewQueueText`, `ReviewKeys` and `ReviewQueueState` |
| `Components/Studio/`, `Admin/Studio/` | The studio's drafts (F49.4, [ADR 0061](../adr/0061-evals-grow-from-use.md)): `SampleDialog` under a conversation's card (the scenario it ran as the form's start, posted as a sample draft), `KeepButtons` under a graded review (a pass kept as a reference, a fail as a known-bad with its reason), `DraftList` on an eval's card; the rules in plain classes with their own tests (`SampleForm`, `KeepOffer`, `ReplayChoices`, `StudioText`) |
| `Components/Studio/Compose/`, `Check/`, `Try/`, `Promptfoo/`, `Admin/Studio/Compose/`, `Admin/Studio/Schema/` | The Studio page's steps: `ComposeHead`, `ComposeChecks` and `ComposeRubrics` edit the form, plain classes changed in place (`EvalComposition`, `ModeEntry`, `CheckEntry`, `RubricEntry`); a check's fields come from its settings' schema (`SchemaFields`, `SchemaField`, `FieldKind`: a property named `reader` is a pick of the runner's readers); `StudioVerdict` (the runner's problems in its words); `TryStep` (a replay of a recorded run on the samples typed, this eval's own runs first); `PromptfooPanel` (a pasted promptfoo file imported into a new draft, listing what was left out, and the open draft exported as `promptfooconfig.yaml`) |
| `Styles/evals/_pages.scss` | The one name a site lists (`evals/pages`), which forwards the six partials of shared rules beside it (`_benchmarks`, `_evals`, `_run-page`, `_explorer`, `_reviews`, `_studio`); a component's own rules are beside it, `Components/Summary/SummaryTable.razor.scss` and 63 more (CL58) |

## Tests

The tests are in `src/tests/web/louis-agent.web.evals.tests`, in `Admin/`, `Components/` and `Pages/`, laid out
like the folders here, with the golden files beside them. `PortalReadsNoScoreKeyTests` checks that a view reads only
the standard records, and `PortalAddressesTests` checks that no file spells a retired address.

## Related docs

[louis-agent.web](louis-agent.web.md) · [louis-agent.api](louis-agent.api.md) · [Evals](../architecture/evals.md) · [ADR 0065](../adr/0065-the-view-is-in-the-address.md)

---
[Projects](README.md) · [Docs index](../README.md)
