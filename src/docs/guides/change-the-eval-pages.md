# Change the eval pages

> **How-to.** Change the framework's eval pages: the Evals page, one run's page, the Review page, the studio and the prompts; and give a
> site the pages. **Elsewhere:** how they work in [The eval pages](../projects/louis-agent.web.evals.md); the rest of the framework's web in
> [Change the framework's web](change-the-web.md).

## Give a site the pages

The studio is a library a site opts into (CL57, `src/web/louis-agent.web.evals`): a project reference, one call in
`Program.cs`, the library's assembly in the router, one name in `Styles/app.scss` with the library's `Styles` on
`sasscompiler.json`'s include paths, and `@using LouisAgent.Web.Evals` in `_Imports.razor`; the starter has all of it.

```csharp
builder.Services.AddLouisAgentEvals();
```

```razor
<Router AppAssembly="@typeof(App).Assembly" AdditionalAssemblies="FrameworkPages.With(AccountLibrary.Assembly, EvalLibrary.Assembly)" …>
```

```scss
@forward "evals/pages";
```

A site that has evals also puts the links in its frame, `<Links><EvalLinks /></Links>`. A site that wants its sources chart sorted hands the
call its kinds of website:

```csharp
builder.Services.AddLouisAgentEvals(new SiteKinds([new SiteKind("shops", "shop.example"), new SiteKind("nearby", ".example")]));
```

An address on its own takes itself and what's under it, and one starting with a dot takes every address that ends that way. A website counts
under the first kind it fits, and the chart draws the first kind in green. The API side is `MapLouisAgentEvals` in the site's program
([louis-agent.api.evals](../projects/louis-agent.api.evals.md)).

## Change a page

They're in English, for the builders, and they show the evals and their stored runs through the API's admin routes, which need the admin key.
They name no site, so what differs comes in through the call above.

**The pages are organised by job** (F53): `/admin` is the Evals page, one list and one details pane. Something new
about an eval goes in the details (`EvalDetails`, or a component of its own under `Components/Admin/Evals/EvalsPage/Details/`);
something the reader needs to *choose* an eval by goes on the list's line (`EvalLine`, worked out in `EvalListView`
with a test, placed by `EvalListItem`). The chosen eval is in the address (`?eval=`): a link to an eval goes through
`EvalAddress.Of`, never a string of its own. A run is started and followed by the page alone (`Evals.razor.cs`:
`StartAsync`, `Follow`); a component that wants a run started raises `OnRun` with a `StartRequest`. The live-run
strip in the layout (`LiveRunStrip`) polls the runner's current run; a page that follows a run keeps the event stream.
No `.cs` or `.razor` may spell `admin/benchmarks` or `admin/maatstawwe` (`PortalAddressesTests`).

**Grading is one job on one page** (F53.3): the Review page (`Pages/Admin/Reviewing/Review.razor`) reads the queue the API
keeps and shows one answer at a time through the explorer's `ReviewPanel`, unchanged. Something new about *which*
answers wait goes in the API's `ReviewQueue` (the one rule, shared with the explorer's `ReviewStatus`), not in the
page; something new about the page's flow (what opens next, what is counted) goes in `ReviewQueueView` with a test,
and its words in `ReviewQueueText`. A key goes in `ReviewKeys`; a field that takes typing inside the page stops its
keys (`@onkeydown:stopPropagation`), or a number typed in it grades. The count in the sidebar is `ReviewQueueState`:
set it where the number changes, never poll for it.

**The run page is tabs, and what it shows is in its address** (F53.1, [ADR 0065](../adr/0065-the-view-is-in-the-address.md)).
A new block on the run page is a tab: a member of `RunTab` with its wire name, an entry in `RunTabs.All`, and a case
in `BenchmarkRun.razor`'s switch. A new thing the explorer can open goes on `ExplorerPlace` (written by `Query()`,
read by `Of`) and into `RunExplorer.Apply` and `Shown`, so Back and a link carry it. A link to a run goes through
`RunAddress.Of`, never a string of its own. The page fetches the run only when its id changes, so a tab or a view
costs no read.

**A new eval's grades show with nothing to add.** The run page reads only the standard records the API sends (the run's eval,
version, commit and graders; each conversation's grades and outcome), so an eval added beside any identity shows its grades
the way every other does: `GraderList` above the explorer, `GradeChips` on each conversation's row (F18 step 4.5c). Don't
read an eval's own score key in a view: `PortalReadsNoScoreKeyTests` (in `src/tests/web/louis-agent.web.tests`) fails on a
new read of `scores` or a test on a task's name; the three files still doing so are listed there and only come off. The
grades code lives in `Admin/Grades/` (the records and their words in `GradesText`; `Reading/` the readers and the outcome parsed
once; `Chips/` the chip list and an outcome's chip class) and `Components/Admin/Evals/Grades/`. A new class there uses the theme's CSS variables and the existing `.chip` kinds.

## Test it

```bash
dotnet test src/tests/web/louis-agent.web.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/web/louis-agent.web.tests    # after a deliberate markup change: then read the diff
```

---
[Guides](README.md) · [Docs index](../README.md)
