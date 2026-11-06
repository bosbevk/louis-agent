# Change the Puls web app

> **How-to.** Change `demos/puls/web/louis-agent.puls`, the general assistant's web app: run it, change the chat, the landing page
> or the admin portal, add a starter, place its words, and test it. **Elsewhere:** how it works in
> [louis-agent.puls](../projects/louis-agent.puls.md); what it shares with the code-review app in
> [Change the framework's web](../../../../src/docs/guides/change-the-web.md); Puls's voice, roles and packs in
> [Build an identity](../../../../src/docs/guides/build-an-identity.md).

Puls is a Blazor WebAssembly app, served by Puls's program, `louis-agent.puls-api`, which references it. It has three parts: the
landing page at `/`, the chat at `/gesels` (with Verken and Studio), and the admin portal at `/admin` for the builders.
What Puls says and which specialists it calls are the Puls identity's prompts, not this app. The app shows the chat,
the specialists' work in the Werk panel, and what each answer cost.

## Run it

```bash
docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build          # http://127.0.0.1:5070
dotnet run --project demos/puls/hosts/louis-agent.puls-api         # http://127.0.0.1:5071
```

The launch profile gives the API Puls's settings (`IDENTITIES=puls`, its roles, SearXNG and the browser on the
compose stack's ports). The web app is the program's project reference. The compose stack also runs the store (`puls-mongo`), the search (`puls-searxng`) and
the headless browser (`puls-browser`) that the specialists use.

## Change the chat

- **The page** is `Pages/ChatPage.razor`, a layout; its state and handlers are in `ChatPage.razor.cs`, and a turn's
  life (draft, session, stream, save) is the shared `ChatRunner`. A new piece is a component under
  `Components/Chat/` in the folder of its concern (`Sidebar/`, `Header/`, `Turns/`, `Panels/`, `Search/`,
  `Composing/`), with its C# in its `.razor.cs` ([src/CLAUDE.md](../../../../src/CLAUDE.md)).
- **Logic** that needs no component goes in a plain class with its test, as `Search/ChatFinder` matches the search.
- **The session** opens with `["specialists", "web"]` where the instance has them, the role `puls/voice-af` (or the
  chosen language's mode), and the user's place and time zone. A new session option is the API's first
  ([Change the API](../../../../src/docs/guides/change-the-api.md#add-a-session-option)), then `AgentApi.CreateSessionAsync` in the shared library.
- **A placeholder made real:** replace its "kom binnekort" toast with the feature, and keep the label.

## Add a starter

A line in `Content/Templates.cs`: Verken and Studio show it as a card (`TemplateCard`).

## Change the landing page

`Pages/LandingPage.razor` lays out the sections in order. Each section is a component in `Components/Landing/` with
its copy as a C# array in its `.razor.cs`. The motion (the progress line, the reveals, the pinned tour, the demo) is
`wwwroot/js/landing.js`, started and stopped by the page. Keep a new animation behind the motion switch
(`puls.motion`), so a user who asks for less motion gets none.

## Change the admin portal

The portal's frame is the framework's `AdminShell`, and `Layout/AdminLayout.razor` only fills it with what's Puls's own, so a link goes
there and a change to the frame goes in `louis-agent.web`. The portal is English, for the builders: `Layout/AdminLayout.razor`, `Pages/Admin/` and `Components/Admin/`
(`RunExplorer`, `TrendChart`, `ChartInfo`). It shows the evals and their stored runs through the API's admin
endpoints, which need the admin key. `ChartInfo` and `RunExplorer` still carry an `@code` block
(`RazorHasNoCodeTests`' pending list): move it to a `.razor.cs` when you change them.

**The portal is organised by job** (F53): `/admin` is the Evals page, one list and one details pane. Something new
about an eval goes in the details (`EvalDetails`, or a component of its own under `Components/Admin/EvalsPage/Details/`);
something the reader needs to *choose* an eval by goes on the list's line (`EvalLine`, worked out in `EvalListView`
with a test, placed by `EvalListItem`). The chosen eval is in the address (`?eval=`): a link to an eval goes through
`EvalAddress.Of`, never a string of its own. A run is started and followed by the page alone (`Evals.razor.cs`:
`StartAsync`, `Follow`); a component that wants a run started raises `OnRun` with a `StartRequest`. The live-run
strip in the layout (`LiveRunStrip`) polls the runner's current run; a page that follows a run keeps the event stream.
No puls `.cs` or `.razor` may spell `admin/benchmarks` or `admin/maatstawwe` (`PortalAddressesTests`).

**Grading is one job on one page** (F53.3): the Review page (`Pages/Admin/Review.razor`) reads the queue the API
keeps and shows one answer at a time through the explorer's `ReviewPanel`, unchanged. Something new about *which*
answers wait goes in the API's `ReviewQueue` (the one rule, shared with the explorer's `ReviewStatus`), not in the
page; something new about the page's flow (what opens next, what is counted) goes in `ReviewQueueView` with a test,
and its words in `ReviewQueueText`. A key goes in `ReviewKeys`; a field that takes typing inside the page stops its
keys (`@onkeydown:stopPropagation`), or a number typed in it grades. The count in the sidebar is `ReviewQueueState`:
set it where the number changes, never poll for it.

**The run page is tabs, and what it shows is in its address** (F53.1, [ADR 0065](../../../../src/docs/adr/0065-the-view-is-in-the-address.md)).
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
once; `Chips/` the chip list and an outcome's chip class) and `Components/Admin/Grades/`, and names nothing of Puls (no `PulsText`, no `Components.Brand`; the same test holds it), so the
admin can later move out as a folder. A new class there uses the theme's CSS variables and the existing `.chip` kinds.

## Words

Puls speaks Afrikaans. The shared components' words are `Text/PulsText.cs` (`PulsText.Afrikaans`, a `ChatText`). A
label in a `.razor` is markup and stays there. A text built in C# goes in `PulsText` rather than inline
([ADR 0009](../../../../src/docs/adr/0009-message-text-as-enum-templates.md)); a few of the page's toasts are still inline in
`ChatPage.razor.cs`, so move one when you touch it.

## Test it

```bash
dotnet test demos/puls/tests/louis-agent.puls.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/puls/tests/louis-agent.puls.tests    # after a deliberate markup change: then read the diff
```

- `Pages/ChatPageTests` renders the chat page with bUnit against `FixtureApi` and streams a recorded Puls turn (a
  pancake recipe handed to the cook). The empty page and the turn are pinned to `ChatPage.empty.html` and
  `ChatPage.turn.html`, and the test lists the page's JavaScript calls (`pulsUi.*`, `louisChat.*`), so a new call is a
  review point.
- `Pages/LandingPageTests` pins the landing page (`LandingPage.html`). `Components/` pins `WorkPanel`,
  `TemplateCard` and `PulsTile`, and `Search/ChatFinderTests` tests the search.

Then check it in a browser at a wide, a medium and a phone width (1440, 1100 and 420 px): the Werk panel slides in
below 1200 px and the sidebar below 992 px.

## Before you ask for a review

- [ ] No `@code` in a new `.razor`; new logic in a plain class with its test.
- [ ] New words in `PulsText` or in markup, none built inline in C#.
- [ ] Motion behind the motion switch; the golden files' diff is the change you meant.

---
[Guides](../../../../src/docs/guides/README.md) · [Docs index](../../../../src/docs/README.md)
