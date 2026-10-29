# Change the Puls web app

> **How-to.** Change `src/louis-agent.puls`, the general assistant's web app: run it, change the chat, the landing page
> or the admin portal, add a starter, place its words, and test it. **Elsewhere:** how it works in
> [louis-agent.puls](../projects/louis-agent.puls.md); what it shares with the code-review app in
> [Change the shared web library](change-the-web-shared.md); Puls's voice, roles and packs in
> [Build an identity](build-an-identity.md).

Puls is a Blazor WebAssembly app, served by the API when the API is built with `WebApp=puls`. It has three parts: the
landing page at `/`, the chat at `/gesels` (with Verken and Studio), and the admin portal at `/admin` for the builders.
What Puls says and which specialists it calls are the Puls identity's prompts, not this app. The app shows the chat,
the specialists' work in the Werk panel, and what each answer cost.

## Run it

```bash
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build          # http://127.0.0.1:5070
dotnet run --project src/louis-agent.api --launch-profile puls --property:WebApp=puls         # http://127.0.0.1:5071
```

The launch profile gives the API Puls's settings (`IDENTITIES=puls`, its roles, SearXNG and the browser on the
compose stack's ports). It doesn't pick the web app: that is the build property, so without `WebApp=puls` the same
API serves the code-review app. The compose stack also runs the store (`puls-mongo`), the search (`puls-searxng`) and
the headless browser (`puls-browser`) that the specialists use.

## Change the chat

- **The page** is `Pages/ChatPage.razor`, a layout; its state and handlers are in `ChatPage.razor.cs`, and a turn's
  life (draft, session, stream, save) is the shared `ChatRunner`. A new piece is a component under
  `Components/Chat/` in the folder of its concern (`Sidebar/`, `Header/`, `Turns/`, `Panels/`, `Search/`,
  `Composing/`), with its C# in its `.razor.cs` ([src/CLAUDE.md](../../src/CLAUDE.md)).
- **Logic** that needs no component goes in a plain class with its test, as `Search/ChatFinder` matches the search.
- **The session** opens with `["specialists", "web"]` where the instance has them, the role `puls/voice-af` (or the
  chosen language's mode), and the user's place and time zone. A new session option is the API's first
  ([Change the API](change-the-api.md#add-a-session-option)), then `AgentApi.CreateSessionAsync` in the shared library.
- **A placeholder made real:** replace its "kom binnekort" toast with the feature, and keep the label.

## Add a starter

A line in `Content/Templates.cs`: Verken and Studio show it as a card (`TemplateCard`).

## Change the landing page

`Pages/LandingPage.razor` lays out the sections in order. Each section is a component in `Components/Landing/` with
its copy as a C# array in its `.razor.cs`. The motion (the progress line, the reveals, the pinned tour, the demo) is
`wwwroot/js/landing.js`, started and stopped by the page. Keep a new animation behind the motion switch
(`puls.motion`), so a user who asks for less motion gets none.

## Change the admin portal

The portal is English, for the builders: `Layout/AdminLayout.razor`, `Pages/Admin/` and `Components/Admin/`
(`RunExplorer`, `TrendChart`, `ChartInfo`). It shows the stored benchmark runs (soon: eval runs) through the API's
admin endpoints, which need the admin key. `ChartInfo` and `RunExplorer` still carry an `@code` block
(`RazorHasNoCodeTests`' pending list): move it to a `.razor.cs` when you change them.

**A new eval's grades show with nothing to add.** The run page reads only the standard records the API sends (the run's eval,
version, commit and graders; each conversation's grades and outcome), so an eval added beside any identity shows its grades
the way every other does: `GraderList` above the explorer, `GradeChips` on each conversation's row (F18 step 4.5c). Don't
read an eval's own score key in a view: `PortalReadsNoScoreKeyTests` (in `tests/louis-agent.web.shared.tests`) fails on a
new read of `scores` or a test on a task's name; the three files still doing so are listed there and only come off. The
grades code lives in `Admin/Grades/` (the records and their words in `GradesText`; `Reading/` the readers and the outcome parsed
once; `Chips/` the chip list and an outcome's chip class) and `Components/Admin/Grades/`, and names nothing of Puls (no `PulsText`, no `Components.Brand`; the same test holds it), so the
admin can later move out as a folder. A new class there uses the theme's CSS variables and the existing `.chip` kinds.

## Words

Puls speaks Afrikaans. The shared components' words are `Text/PulsText.cs` (`PulsText.Afrikaans`, a `ChatText`). A
label in a `.razor` is markup and stays there. A text built in C# goes in `PulsText` rather than inline
([ADR 0009](../adr/0009-message-text-as-enum-templates.md)); a few of the page's toasts are still inline in
`ChatPage.razor.cs`, so move one when you touch it.

## Test it

```bash
dotnet test tests/louis-agent.puls.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.puls.tests    # after a deliberate markup change: then read the diff
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
[Guides](README.md) · [Docs index](../README.md)
