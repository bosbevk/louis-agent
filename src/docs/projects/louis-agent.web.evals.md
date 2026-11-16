# louis-agent.web.evals — design

> **Kind:** Razor library (`net10.0`) · **Path:** `src/web/louis-agent.web.evals/` · **References:** louis-agent.web,
> louis-agent.evals.protocol · **Used by:** a site that opts into the eval studio (Puls, the code-review app, the starter), at
> `/admin/evals`, an area inside the admin's frame (F144) · **How to change them:** [Change the eval pages](../guides/change-the-eval-pages.md)
> **Also shows:** `component`, `sequence`

The studio is a library on the framework's web, not a part of it. A site that wants it names it; a site that doesn't
downloads none of it.

*Decided in:* CL57, [ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md).

![The eval studio's screens: evals and their runs, answers to grade, the studio and the prompts](diagrams/louis-agent.web.evals.svg)

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
green, and whatever fits none is grey and called *elsewhere*. If no kinds are named, everything is one grey slice.
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

### 6. Prompts, imports, and old addresses

`/admin/prompts` lists every built-in prompt by its header. `/admin/imports` takes the packs' data uploads. The old
`/admin/benchmarks` and `/admin/maatstawwe` addresses are gone.

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

Paths are from `src/web/louis-agent.web/`.

| Path | What |
|---|---|
| `Pages/Reviewing/Review.razor`, `Review.razor.cs` | The Review page at `/admin/review` (F53.3): the queue read for the kept name, one answer open with its head and the blind review panel, the keys 1, 2 and 3, the count kept current for the sidebar |
| `Pages/Building/Prompts.razor`, `Prompts.razor.cs` | Every built-in prompt as its header says it (CL27, ADR 0063), from `GET /v1/admin/prompts`: one table sorted by path, a row each with its name and file, level, description and the toolsets it always loads (a dash for none); styles in `Styles/web/admin/` |
| `Pages/Running/Evals.razor`, `BenchmarkRun.razor` | The Evals page at `/admin/evals`: the list, the chosen eval's details, the run it starts and follows (F53.2); one stored run: the head, the verdict, the tab bar and the open tab, the tab and the explorer's place read from the address and the run fetched only when its id changes (F53.1) |
| `Components/EvalsPage/`, `List/`, `Details/`, `Admin/Evals/`, `Admin/Evals/List/` | The Evals page's parts: `EvalList` and `EvalListItem` (the list pane; `EvalLine`, `ModeChange`, `EvalListView`, `EvalAddress`), `EvalDetails`, `EvalRunControls` and `ManualDialog` (the details pane), `EvalScorecard`, `EvalRunTimeline`, `EvalRunOrigin`, `EvalChecks` (`EvalCardView`, `RunLine`, `RunOrigin`, `GraderGroup`, `EvalText`) |
| `Components/LiveRun/`, `Components/Runs/`, `Components/Runs/Table/`, `Components/Head/` | `LiveRunPanel` and `LiveTurnTable` (the run in progress, its turns as they come), `LiveRunStrip` (a run is going, on every page); a stored run's row and its buttons, `RunsList` (the runs table); `StoreBanner` (the store's state), `EvalLinks` (the sidebar's links) and `EvalsCard` (the studio's card on the admin's home: the evals, the answers to review, the last run; F144) |
| `Components/` | `RunExplorer`, `TrendChart`, `ChartInfo`: a run at every level (its place handed in as `Place` and reported on every move as `OnMoved`), a check's trend, a chart's explanation |
| `Components/RunPage/`, `Admin/RunPage/`, `Admin/Verdict/`, `Admin/Explorer/Place/` | The run page's parts (F53.1): `RunHead`, `RunVerdict` (the card under the head) and `RunTabBar` (a link per tab); `RunTab` and `RunTabs` (the tab, parsed once from `?tab=`, and the bar's entries), `RunAddress` (the one writer of a run's address); `RunVerdictView` and `ModeVerdict` (the verdict's numbers, worked out from the run and its trend) with `VerdictText`; `ExplorerView` and `ExplorerPlace` (where you are in the explorer, written to and read from the address) |
| `Components/Grades/` | `GradeChips`, `GraderList`: a conversation's outcome and grades, the run's eval and graders, from the standard records (F18 step 4.5c) |
| `Components/Charts/` | `Ring`, `TokenStack`, `TokenLegend`: the explorer's donut, stacked token bar and token legend, markup only (F18 step 6a.3b) |
| `Components/Reviews/` | `ReviewPanel` (a conversation's blind review; it reports a stored grade through `OnGraded` and grades through its public `GradeAsync`, F53.3), `Queue/` (`ReviewItemHead`, where the open answer is from and Skip; `ReviewCountBadge`, how many wait, beside *Review* in the sidebar), `ReviewPill`, `AgreementPanel` (per judge, how often it agreed with the people), `CalibrationCard` (the judge check from the portal: the button, the latest report in sentences, the earlier ones folded; F18 step 6a.5c) |
| `Components/Studio/`, `Admin/Studio/` | The studio's drafts (F49.4, [ADR 0061](../adr/0061-evals-grow-from-use.md)): `SampleDialog` under a conversation's card (the scenario it ran as the form's start, posted as a sample draft), `KeepButtons` under a graded review (a pass kept as a reference, a fail as a known-bad with its reason), `DraftList` on an eval's card (folded under its count, each draft with a delete, the page reloaded after); the rules in plain classes with their own tests (`SampleForm`, `KeepOffers`, `StudioText`); styles in `Styles/web/admin/_studio.scss` |
| `Pages/Building/EvalStudio.razor`, `Components/Studio/Compose/`, `Components/Studio/Promptfoo/`, `Components/Studio/Check/`, `Components/Studio/Try/`, `Admin/Studio/`, `Admin/Studio/Compose/`, `Admin/Studio/Schema/` | The Studio page at `/admin/studio` (F49.5, [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md); named `EvalStudio`, since the folders' `.Studio` namespaces would shadow `Studio`): the evals being composed on the left, the form on the right as four numbered steps in the order they are done (the admin review's finding 6): *1 Compose* with *Save* and *View JSON*, *2 Check* (the runner's problems in its words, `StudioVerdict`), *3 Try* (`TryStep`: a replay of a run picked from the recorded runs the Evals page lists, `ReplayChoices`, this eval's own first, on the samples typed, linked to its run page) and *4 Publish* (the branch named, or a zip downloaded); each action saves first. The form is plain classes the components change in place (`EvalComposition`, `ModeEntry`, `CheckEntry`, `RubricEntry`), edited by `ComposeHead`, `ComposeChecks` and `ComposeRubrics`; a check's fields come from its settings' schema (`SchemaFields`, `SchemaField`, `FieldKind`: a property named `reader` is a pick of the runner's readers). `PromptfooPanel` imports a pasted promptfoo file into a new draft, which the page opens, listing what was left out, and exports the open draft as `promptfooconfig.yaml` |
| `Components/Summary/` | `SummaryPanel`, `SummaryTable`, `RateBar`: a run's results by category, a rate as a bar (F18 step 5c.3, 6a.3c) |
| `Admin/` | `RunModelFilter`: the runs list's model filter; `Grades/`: the records both components use (`GradeOutcome`, `GraderRow`, `GradeRow`, `RunGraders`, `ConversationGrades`) and their words (`GradesText`); `Grades/Reading/`: the outcome parsed once (`GradeOutcomes`); the run's graders and a conversation's grades come from their typed rows (`RunGraders.Of`, `ConversationGrades.Of`); `Grades/Chips/`: the chip list (`GradeChip`, `GradeChipList`) and an outcome's chip class (`ChipKind`); `Charts/`: the charts' colours and maths, no markup (`ChartPalette`, the six series colours for the trend chart and the explorer, `RingArcs`, `TokenKinds`, `ChartNumbers`); `Reviews/`: the review's rules and words in plain classes: `BlindReview` (the one place that knows `.human`: `IsReview`, `JudgeOf`; who answered: `OwnGrades`, `Answered`; which review a conversation shows: `ReviewOf`; what hides the judge's verdict; whether the person agreed: `Compare`), `ReviewStatus` (to review, reviewed, no review, and the list's filter `Keeps`), `ReviewText` and `AgreementText` (the sentences), `CalibrationText` and `CalibrationView` (the judge check), `ReviewerName` and `ReviewNote` (their limits), `ReviewerState`; `Reviews/Queue/` (F53.3): `ReviewQueueView` (the Review page's model), `ReviewQueueText` (its words), `ReviewKeys` (1, 2, 3 as verdicts), `ReviewQueueState` (how many wait, shared with the badge) |
| `Admin/EvalPages.cs`, `EvalServices.cs`, `EvalArea.cs` | What the pages need from a site's services; `AddLouisAgentEvals`; `.Evals()`, the area a site names |
| `Admin/Explorer/Sites/` | `SiteKind`, `SiteKinds`, `SiteCounts`: how a site sorts the websites its agent read, and the counts for the sources chart |
| `Styles/evals/_pages.scss` | The one name a site lists (`evals/pages`), which forwards the pages' six partials of shared rules; a component's own rules are beside it, `Components/Summary/SummaryTable.razor.scss` and 63 more (CL58) |

## Tests

The tests are in `src/tests/web/louis-agent.web.evals.tests`, in `Admin/`, `Components/` and `Pages/`, laid out
like the folders here, with the golden files beside them. `PortalReadsNoScoreKeyTests` checks that a view reads only
the standard records, and `PortalAddressesTests` checks that no file spells a retired address.

## Related docs

[louis-agent.web](louis-agent.web.md) · [louis-agent.api](louis-agent.api.md) · [Evals](../architecture/evals.md) · [ADR 0065](../adr/0065-the-view-is-in-the-address.md)

---
[Projects](README.md) · [Docs index](../README.md)
