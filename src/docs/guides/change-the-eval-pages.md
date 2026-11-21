# Change the eval pages

> **How-to.** Change the framework's eval pages: the Evals page, one run's page, the Review page, the studio and the
> prompts; and give a site the pages. **Elsewhere:** how they work in [The eval pages](../projects/LouisAgent.Web.Evals.md);
> the rest of the framework's web in [Change the framework's web](change-the-web.md).

![The eval pages: a site turns them on with .Evals(), and they hold the Evals page, a run's page and the Review page, read through the API's admin routes](diagrams/change-the-eval-pages.svg)

The eval pages are a library a site turns on, `src/web/LouisAgent.Web.Evals`. They're in English, for the builders,
and they show the evals and their stored runs through the API's admin routes, which need the admin key. They name no
site, so whatever differs between sites comes in through the one call that turns them on.

## Give a site the pages

![Giving a site the pages: name the area with .Evals(), add the styles and the using, map the API side](diagrams/change-the-eval-pages-flow.svg)

A site names the eval studio among its areas, in its `WebsiteAreas.Name`, with `.Evals()`. That one call registers the
pages' services, brings their assembly to the site's router, and gives the admin frame its eval links and its card on
the home ([ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md)). The site adds only its styles and a
`using`. The starter has all of it.

1. **Name the area**, in `WebsiteAreas.Name`: `.Evals()`.
2. **Add the styles**: the library's `Styles` folder on `sasscompiler.json`'s include paths, and one line in
   `Styles/app.scss`:

   ```scss
   @forward "evals/pages";
   ```

3. **Add `@using LouisAgent.Web.Evals`** to `_Imports.razor`.
4. **Map the API side** in the site's program, with `MapLouisAgentEvals`
   ([LouisAgent.Api.Evals](../projects/LouisAgent.Api.Evals.md)).

A site that wants its sources chart sorted hands the call its kinds of website:

```csharp
.Evals(new SiteKinds([new SiteKind("shops", "shop.example"), new SiteKind("nearby", ".example")]))
```

An address on its own takes itself and what's under it, and one that starts with a dot takes every address that ends
that way. A website counts under the first kind it fits, and the chart draws the first kind in green.

## Change the Evals page

The pages are organised by job (F53). `/admin/evals` is the Evals page: one list and one details pane.

| You want to add | It goes |
|---|---|
| Something new about an eval | In its details: `EvalDetails`, or a component of its own under `Components/EvalsPage/Details/` |
| Something the reader chooses an eval by | On the list's line: an `EvalLine`, worked out in `EvalListView` with a test, placed by `EvalListItem` |
| A link to an eval | Through `EvalAddress.Of`, since the chosen eval is in the address (`?eval=`); never a string of its own |
| A way to start a run | The page alone starts and follows runs, in `Pages/Running/Evals.razor.cs` (`StartAsync`, `Follow`); a component raises `OnRun` with a `StartRequest` |

The live-run strip in the layout, `LiveRunStrip`, polls the runner's current run, while a page that follows a run keeps
the event stream. No `.cs` or `.razor` may spell `admin/benchmarks` or `admin/maatstawwe` (`PortalAddressesTests`).

## Change the Review page

Grading is one job on one page (F53.3). The Review page, `Pages/Reviewing/Review.razor`, reads the queue the API keeps
and shows one answer at a time through the explorer's `ReviewPanel`, unchanged.

Something new about *which* answers wait goes in the API's `ReviewQueue`, the one rule, which it shares with the
explorer's `ReviewStatus`. It doesn't go in the page. Something new about the page's flow, such as what opens next or
what is counted, goes in `ReviewQueueView` with a test, and its words in `ReviewQueueText`.

A key goes in `ReviewKeys`. A field that takes typing inside the page stops its keys (`@onkeydown:stopPropagation`),
or a number typed in it grades. The count in the sidebar is `ReviewQueueState`: set it where the number changes, and
never poll for it.

## Change a run's page

The run page is tabs, and what it shows is in its address (F53.1, [ADR 0065](../adr/0065-the-view-is-in-the-address.md)).

A new block on the run page is a tab: a member of `RunTab` with its wire name, an entry in `RunTabs.All`, and a case in
`Pages/Running/BenchmarkRun.razor`'s switch. A new thing the explorer can open goes on `ExplorerPlace` (written by
`Query()`, read by `Of`), and into `RunExplorer.Apply` and `Shown`, so Back and a link carry it. A link to a run goes
through `RunAddress.Of`, never a string of its own. The page fetches the run only when its id changes, so a tab or a
view costs no read.

## A new eval's grades

A new eval's grades show with nothing to add. The run page reads only the standard records the API sends: the run's
eval, version, commit and graders, and each conversation's grades and outcome. So an eval added beside any identity
shows its grades the way every other does, with `GraderList` above the explorer and `GradeChips` on each
conversation's row (F18 step 4.5c).

Don't read an eval's own score key in a view. `PortalReadsNoScoreKeyTests`, in `src/tests/web/LouisAgent.Web.Evals.Tests`,
fails on a read of `scores` or a test on a task's name, and its list of files still allowed to do so is empty. The
grades code lives in `Admin/Grades/` (the records and their words in `GradesText`, `Reading/` for the readers and the
outcome parsed once, `Chips/` for the chip list and an outcome's chip class) and in `Components/Grades/`. A new class
there uses the theme's CSS variables and the existing `.chip` kinds.

## Test it

```bash
dotnet test src/tests/web/LouisAgent.Web.Evals.Tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/web/LouisAgent.Web.Evals.Tests    # after a deliberate markup change, then read the diff
```

---
[Guides](README.md) · [Docs index](../README.md)
