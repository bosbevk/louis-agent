# louis-agent.web.evals — design

> **Kind:** Razor class library (`net10.0`) · **Path:** `src/web/louis-agent.web.evals/` · **References:** louis-agent.web,
> louis-agent.evals.protocol · **Used by:** a site that opts into the eval studio (Puls, the code-review app, the starter), at
> `/admin` · **How to change them:** [Change the eval pages](../guides/change-the-eval-pages.md)

Since CL57 ([ADR 0092](../adr/0092-a-project-holds-what-is-reused-together.md)) the studio is a library on the
framework's web, not a part of it: a site references it, calls `AddLouisAgentEvals()` and lists
`EvalLibrary.Assembly` in its router through `FrameworkPages.With(...)`; a site that doesn't downloads none of it.

![The eval studio's screens: evals and their runs, answers to grade, the studio and the prompts](diagrams/louis-agent.web.evals.svg)

## Purpose

The screens someone building an agent works in: the evals and their runs, the answers waiting for a person's grade, the studio that
composes an eval, and every built-in prompt. They read the API's admin routes and nothing else, so they're the same on every site.

## What a site gives them

Almost nothing, which is the point ([ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md)):

- **One call in `Program.cs`:** `EvalPages.AddTo(builder.Services)`. Every site has these addresses, as a router finds every page in the
  framework, so without the call `/admin` would crash on a missing service.
- **One name in `app.scss`:** `@forward "evals/pages";`, which forwards the six partials of shared rules in their order;
  what one component draws alone sits beside it in its `X.razor.scss` and travels in the site's styles bundle (CL58,
  [ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md)).
- **Its links, if it has evals:** `<EvalLinks />` in its frame's `Links` slot, and `<LiveRunStrip />` in `Top` if it wants the strip. A site with
  no evals leaves them out, so the pages are there but nothing leads to them. Today only Puls shows them.
- **Which websites matter, if it wants the sources chart to sort them:** a `SiteKinds` list handed to the same call. Each `SiteKind` is a name
  and the addresses it takes. A website goes under the first kind it fits, the first kind is green, and what fits none is grey and called
  *elsewhere*. With none named, everything is one grey slice. Puls's list is `PulsSites`.

## Diagrams

Where the pieces sit, and what a site adds:

![The eval studio's pages: from a site's program through the admin frame to the API](diagrams/louis-agent.web.evals-diagrams.svg)

What happens when someone starts a run and watches it:

![Starting a run and watching it: the page starts it through the API, shows its turns as they finish, and lists the run with a link](diagrams/louis-agent.web.evals-diagrams-2.svg)

## How it works

**Organised by job** (F53), in English, as they're the builders' screens. `/admin` is the Evals page (F18, CL26, F53.2), in list/details shape:
the list pane names every eval the runner has, an eval that never ran included, with its last run's status, when it
ran and per mode the last pass@k with its move against the mode's run before (`EvalList`, `EvalListItem`; the
numbers in `EvalListView`, a Presentation Model); the chosen eval is in the address (`?eval=`, `EvalAddress`; none
opens the eval that ran last) and its details fill the pane beside it (`EvalDetails`): what it proves, the controls
that run it (`EvalRunControls`: the repeats, the Run button with its estimate from the last run that asked the
model, `RunEstimate`: not a re-score, not a replay; the Manual button, FX28), its latest run as a scorecard with a
ring per mode, the runs before it down a timeline, each tagged *recorded*, *replay of …* or *re-scored from …* and
linked to its source, its trend, its graders, categories and modes in a fold (`EvalCardView` works out the card,
`EvalText` its words), and under the card every stored run as a table (`RunsList`, `RunListItem`: a run can be
re-scored, a recorded one replayed through the page's one start path). The page starts and follows a run
(`LiveRunPanel` above the details, kept while the reader opens another eval), and a thin strip in the layout says a
run is going on every admin page (`LiveRunStrip`: it reads the runner's current run on open and every ten seconds
while that run goes).

`/admin/review` is the Review page (F53.3): the answers waiting for this reviewer's grade across the stored runs,
one at a time, from the queue the API keeps (`GET v1/admin/evals/reviews/queue?reviewer=&limit=`, read for the
kept name; a name typed or let go in the panel reads it again): where the answer is from (the eval, a link to its
run, the mode, the repeat, the task; `ReviewItemHead`), its place in the queue and Skip, then the explorer's blind
review panel unchanged (`ReviewPanel`, which reports a stored grade through `OnGraded`); a grade or Skip opens the
next, past the last one handed over the queue is read again, and with nothing waiting the page says so by name.
The keys 1, 2 and 3 grade pass, fail or unsure (`ReviewKeys`; the page's box takes the focus when an answer
opens, and the panel's note and name fields keep their keys to themselves). The page's state is `ReviewQueueView`
(a Presentation Model: the items, which is open, graded and left) with its words in `ReviewQueueText`; how many
wait is `ReviewQueueState`, a scoped Observer the page sets and `ReviewCountBadge` beside *Review* in the sidebar
draws, read once from the queue when the portal opens.

`/admin/runs/{run}` is one run (F53.1, [ADR 0065](../adr/0065-the-view-is-in-the-address.md)): under its
head the verdict (`RunVerdict`: per mode passed of graded and the change against the eval's run before, from the
trend; the cost, the requests, the answers still to grade; the numbers in `RunVerdictView`, a Presentation Model),
then one tab at a time, the tab in the address (`?tab=`: Summary with the results by category, the pass rates and
the trends, `TrendChart`; Conversations with the explorer; Judge; Notes; Settings). The explorer (`RunExplorer`:
the run, its conversations side by side, a turn, a request) reports every move as an `ExplorerPlace` and the page
puts it in the address (`?view=…&task=…&turn=…`), so Back goes up a level and a link opens the same view;
`RunAddress` is the one class that spells a run's address. Each chart explains itself on hover (`ChartInfo`).

`/admin/prompts` lists every built-in prompt by its header (CL27); `/admin/imports` takes the packs' data uploads (F55); the old `/admin/benchmarks` and `/admin/maatstawwe`
addresses are gone (ADR 0049). **The grades view** (F18 step 4.5c) shows every eval the same way, from
the standard records alone: `GraderList` above the explorer (the run's eval, version, commit and graders, or one line saying
it recorded none) and `GradeChips` on each conversation's row (its outcome and one chip per grade, in the run's grader order,
the reason on hover; "no grades" for a run from before F18 step 2a, never 0). The readers and the chip list are plain classes in
`Admin/Grades/`, the components in `Components/Evals/Grades/`.

## Structure

Paths are from `src/web/louis-agent.web/`.

| Path | What |
|---|---|
| `Pages/Reviewing/Review.razor`, `Review.razor.cs` | The Review page at `/admin/review` (F53.3): the queue read for the kept name, one answer open with its head and the blind review panel, the keys 1, 2 and 3, the count kept current for the sidebar |
| `Pages/Building/Prompts.razor`, `Prompts.razor.cs` | Every built-in prompt as its header says it (CL27, ADR 0063), from `GET /v1/admin/prompts`: one table sorted by path, a row each with its name and file, level, description and the toolsets it always loads (a dash for none); styles in `Styles/web/admin/` |
| `Pages/Running/Evals.razor`, `BenchmarkRun.razor` | The Evals page at `/admin`: the list, the chosen eval's details, the run it starts and follows (F53.2); one stored run: the head, the verdict, the tab bar and the open tab, the tab and the explorer's place read from the address and the run fetched only when its id changes (F53.1) |
| `Components/EvalsPage/`, `List/`, `Details/`, `Admin/Evals/`, `Admin/Evals/List/` | The Evals page's parts: `EvalList` and `EvalListItem` (the list pane; `EvalLine`, `ModeChange`, `EvalListView`, `EvalAddress`), `EvalDetails`, `EvalRunControls` and `ManualDialog` (the details pane), `EvalScorecard`, `EvalRunTimeline`, `EvalRunOrigin`, `EvalChecks` (`EvalCardView`, `RunLine`, `RunOrigin`, `GraderGroup`, `EvalText`) |
| `Components/LiveRun/`, `Components/Runs/`, `Runs/Table/`, `Components/Head/` | `LiveRunPanel` and `LiveTurnTable` (the run in progress, its turns as they come), `LiveRunStrip` (a run is going, on every page); a stored run's row and its buttons, `RunsList` (the runs table); `StoreBanner` (the store's state) and `EvalLinks` (the sidebar's links) |
| `Components/` | `RunExplorer`, `TrendChart`, `ChartInfo`: a run at every level (its place handed in as `Place` and reported on every move as `OnMoved`), a check's trend, a chart's explanation |
| `Components/RunPage/`, `Admin/RunPage/`, `Admin/Verdict/`, `Admin/Explorer/Place/` | The run page's parts (F53.1): `RunHead`, `RunVerdict` (the card under the head) and `RunTabBar` (a link per tab); `RunTab` and `RunTabs` (the tab, parsed once from `?tab=`, and the bar's entries), `RunAddress` (the one writer of a run's address); `RunVerdictView` and `ModeVerdict` (the verdict's numbers, worked out from the run and its trend) with `VerdictText`; `ExplorerView` and `ExplorerPlace` (where you are in the explorer, written to and read from the address) |
| `Components/Grades/` | `GradeChips`, `GraderList`: a conversation's outcome and grades, the run's eval and graders, from the standard records (F18 step 4.5c) |
| `Components/Charts/` | `Ring`, `TokenStack`, `TokenLegend`: the explorer's donut, stacked token bar and token legend, markup only (F18 step 6a.3b) |
| `Components/Reviews/` | `ReviewPanel` (a conversation's blind review; it reports a stored grade through `OnGraded` and grades through its public `GradeAsync`, F53.3), `Queue/` (`ReviewItemHead`, where the open answer is from and Skip; `ReviewCountBadge`, how many wait, beside *Review* in the sidebar), `ReviewPill`, `AgreementPanel` (per judge, how often it agreed with the people), `CalibrationCard` (the judge check from the portal: the button, the latest report in sentences, the earlier ones folded; F18 step 6a.5c) |
| `Components/Studio/`, `Admin/Studio/` | The studio's drafts (F49.4, [ADR 0061](../adr/0061-evals-grow-from-use.md)): `SampleDialog` under a conversation's card (the scenario it ran as the form's start, posted as a sample draft), `KeepButtons` under a graded review (a pass kept as a reference, a fail as a known-bad with its reason), `DraftList` on an eval's card (folded under its count, each draft with a delete, the page reloaded after); the rules in plain classes with their own tests (`SampleForm`, `KeepOffers`, `StudioText`); styles in `Styles/web/admin/_studio.scss` |
| `Pages/Building/EvalStudio.razor`, `Components/Studio/Compose/`, `Components/Studio/Promptfoo/`, `Components/Studio/Check/`, `Components/Studio/Try/`, `Admin/Studio/`, `Admin/Studio/Compose/`, `Admin/Studio/Schema/` | The Studio page at `/admin/studio` (F49.5, [ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md); named `EvalStudio`, since the folders' `.Studio` namespaces would shadow `Studio`): the evals being composed on the left, the form on the right as four numbered steps in the order they are done (the admin review's finding 6): *1 Compose* with *Save* and *View JSON*, *2 Check* (the runner's problems in its words, `StudioVerdict`), *3 Try* (`TryStep`: a replay of a run picked from the recorded runs the Evals page lists, `ReplayChoices`, this eval's own first, on the samples typed, linked to its run page) and *4 Publish* (the branch named, or a zip downloaded); each action saves first. The form is plain classes the components change in place (`EvalComposition`, `ModeEntry`, `CheckEntry`, `RubricEntry`), edited by `ComposeHead`, `ComposeChecks` and `ComposeRubrics`; a check's fields come from its settings' schema (`SchemaFields`, `SchemaField`, `FieldKind`: a property named `reader` is a pick of the runner's readers). `PromptfooPanel` imports a pasted promptfoo file into a new draft, which the page opens, listing what was left out, and exports the open draft as `promptfooconfig.yaml` |
| `Components/Summary/` | `SummaryPanel`, `SummaryTable`, `RateBar`: a run's results by category, a rate as a bar (F18 step 5c.3, 6a.3c) |
| `Admin/` | `RunModelFilter`: the runs list's model filter; `Grades/`: the records both components use (`GradeOutcome`, `GraderRow`, `GradeRow`, `RunGraders`, `ConversationGrades`) and their words (`GradesText`); `Grades/Reading/`: the outcome parsed once (`GradeOutcomes`); the run's graders and a conversation's grades come from their typed rows (`RunGraders.Of`, `ConversationGrades.Of`); `Grades/Chips/`: the chip list (`GradeChip`, `GradeChipList`) and an outcome's chip class (`ChipKind`); `Charts/`: the charts' colours and maths, no markup (`ChartPalette`, the six series colours for the trend chart and the explorer, `RingArcs`, `TokenKinds`, `ChartNumbers`); `Reviews/`: the review's rules and words in plain classes: `BlindReview` (the one place that knows `.human`: `IsReview`, `JudgeOf`; who answered: `OwnGrades`, `Answered`; which review a conversation shows: `ReviewOf`; what hides the judge's verdict; whether the person agreed: `Compare`), `ReviewStatus` (to review, reviewed, no review, and the list's filter `Keeps`), `ReviewText` and `AgreementText` (the sentences), `CalibrationText` and `CalibrationView` (the judge check), `ReviewerName` and `ReviewNote` (their limits), `ReviewerState`; `Reviews/Queue/` (F53.3): `ReviewQueueView` (the Review page's model), `ReviewQueueText` (its words), `ReviewKeys` (1, 2, 3 as verdicts), `ReviewQueueState` (how many wait, shared with the badge) |
| `Admin/EvalPages.cs` | The one call a site makes |
| `Admin/Explorer/Sites/` | `SiteKind`, `SiteKinds`, `SiteCounts`: how a site sorts the websites its agent read, and the counts for the sources chart |
| `Styles/evals/_pages.scss` | The one name a site lists (`evals/pages`), which forwards the pages' six partials of shared rules; a component's own rules are beside it, `Components/Summary/SummaryTable.razor.scss` and 63 more (CL58) |

## Tests

`src/tests/web/louis-agent.web.evals.tests`, in `Admin/`, `Components/` and `Pages/`, the same way as the folders here, with the
goldens beside them. `PortalReadsNoScoreKeyTests` holds that a view reads only the standard records, and `PortalAddressesTests` that no file
spells a retired address.

## What the build decided (CL57, 2026-11-07, unattended)

- The library keeps the folders' names (`Admin/`, `Components/`, `Pages/`) and its Sass under `evals/`, listed by a site
  as `evals/pages`; `EvalLibrary.Assembly` is what the router takes, `AddLouisAgentEvals()` what the services take.
- Found on the way: the account pages answered "not found" on every site, since no router listed their assembly;
  `AccountLibrary.Assembly`, `FrameworkPages.With` and `SiteRouterTests` fix and hold it.

## Related docs

[louis-agent.web](louis-agent.web.md) · [louis-agent.api](louis-agent.api.md) · [Evals](../architecture/evals.md) · [ADR 0065](../adr/0065-the-view-is-in-the-address.md)

---
[Projects](README.md) · [Docs index](../README.md)
