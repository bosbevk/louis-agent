# Change the code-review web app

> **How-to.** Change `demos/louis-agent/web/louis-agent.code-review`, the browser app for chatting with the coding agent, browsing the workspace
> and reviewing changes, and since F109.1 the framework's site as well: run it, add a tab, call a new endpoint, add a
> theme, place its words, add a site page, change the site's look, change and export a film, and test it.
> **Elsewhere:** how it works in [louis-agent.code-review](../projects/louis-agent.code-review.md); what it shares with Puls in
> [Change the framework's web](../../../../src/docs/guides/change-the-web.md); the folder rules in [src/CLAUDE.md](../../../../src/CLAUDE.md).

The app is Blazor WebAssembly, served by the API on the same origin, so it has no settings of its own. It's a small site on
the framework's web: `Pages/ChatPage.razor` is a thin page on `ChatShell`, `Layout/AdminLayout.razor` a small one on `AdminShell`.
Files, Changes and Branches aren't its own: they're the files and git packs' views under `packs/web/`, opened as admin screens.

## Run it

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api      # http://127.0.0.1:5080
dotnet run --project demos/louis-agent/hosts/louis-agent.coding-api                             # http://127.0.0.1:5081
```

The coding agent's program, `louis-agent.coding-api`, references this app, so that's what it serves. After a change to the app, rebuild the `api` image or
restart the run. If the API needs `AGENT_API_KEY`, enter it under *API key* in the sidebar; it stays in this browser.

## Add a control

A control for a pack isn't this app's. It goes in that pack's web project under `packs/web/` and opens as an admin screen, as the git pack's
`GitChangesView` does ([Build a pack](../../../../src/docs/guides/build-a-pack.md)). Then:

1. Register it in `Program.cs`, as `GitWebViews.AddTo(views)` does, and list its Sass in `Styles/app.scss` with its folder in `sasscompiler.json`.
2. Add a link in the `Views` slot of `Pages/ChatPage.razor`, if the chat's sidebar should have one. `PackMenu` lists it on the admin pages by itself.
3. A button that asks the agent for something uses `ChatHandoff`, which opens the chat with a draft and sends nothing.

## Call a new endpoint

Components never use `HttpClient`. A call only a pack's views make is an extension method on `AgentApi` in that pack's web project, as
`AgentApiGit` is. A call both apps make goes
in `AgentApi` itself ([Change the framework's web](../../../../src/docs/guides/change-the-web.md)). The endpoint itself is the API's
([Change the API](../../../../src/docs/guides/change-the-api.md)), protected by its key.

## Add a theme

Copy `Styles/themes/retro.scss` to `{id}.scss`, set the framework's colour names (`web/settings/_colours.scss` lists them), and
add `{ "id": "{id}", "name": "…" }` to `themes/themes.json`. Ids are lowercase letters, digits and dashes.

## Words

The shared components' words come from `ChatText`, whose defaults are this app's English (`Program.cs` registers
`new ChatText()`). A label in a `.razor` is markup and stays there. A text built in C# (a notice, an error) goes in a
class of the app's words rather than inline, as Puls keeps its own in `Text/`
([ADR 0009](../../../../src/docs/adr/0009-message-text-as-enum-templates.md)).

## Add a site page

1. A `.razor` under `Pages/Site/` with `@page "/its-route"` and a `<PageTitle>`; markup only, no `@code`. It draws in
   the site's frame by default; a page that shouldn't names its layout, as `ChatPage` does.
2. Its link in `Layout/SiteLayout.razor`'s menu.
3. A row in `SitePagesTests.APage_IsAtItsAddress_WithItsHeading` with the route and the heading, and one in
   `SiteLayoutTests.TheMenu_LeadsToThePage`. Code or a list that mirrors the repo gets a test holding it to the repo,
   as `BuildYourOwnTests`, `PacksPageTests`, `ProofPageTests` (the rand to the rates file), `StandsOnTests` (the packages
   to the project files) and `WhatsComingTests` (the planned features to the TODO's open rows) do.
5. A block that should reveal as it enters gets `reveal` (and a `data-delay`); a section the landing's index should
   name gets an id and a link in `HomePage`'s `site-index`; the frame's module does the rest (F110,
   [Make a page move](../../../../src/docs/guides/change-the-web.md#make-a-page-move)).
6. The guide's voice (`louis-agent.identity.expert`, `voice.md`, *The site you sit on*) gets the page's address, what it
   says and the docs it came from: `GuideKnowsTheSiteTests` fails until it does. A new project of the solution has to be
   named on the Architecture page (`SiteMapTests`); a new identity on Identities, a new pack on Packs.
4. A new section's styles go in the partial of their concern under `Styles/site/` (a landing part in `landing/`); a
   new partial is added to `app.scss`'s list where it belongs in the cascade, and `StylesheetTests` checks the two
   match. The site's element rules hang off `.site`, the frame's root, so they never reach the chat.

## Change the site's look

The colours and fonts are the framework's, in `src/web/louis-agent.web/Styles/web/settings/_colours.scss`, so a change
there reaches every site with none of its own (and means exporting the films). What's only the site's, such as its
corners and gutter, is in `Styles/site/settings/_tokens.scss`; the chrome headline is `.chrome` in
`Styles/site/base/_elements.scss`. `console`, the default theme, is those colours as they are; `retro` is the one the
chat's picker switches to. Check a change at 1440 px and at 390 px: no sideways scroll, the menu wrapped.

## Change a film

A film is markup (`Components/Movies/*.razor`) and three stylesheets under `Styles/site/movie/`:

- `_stage.scss`: what both films share (the stage, a part, the light, the card, the strip). A change here changes both.
- `_short.scss` and `_detail.scss`: one film each. Where a part stands (`.p-host { left; top; width; height }` on a
  1280×720 floor whose centre is 640×380), the camera (`@keyframes camera-s` / `camera-d`: wide at the start and the
  end; to fly in on a part, `translate(640 − its centre x, 380 − its centre y)` and a `scale`), when a part lights up
  (`lit-*`), the light's path (`travel-*`), and when each card and caption shows (`show-*`, `step-*`). A scene's
  window is its share of the loop; keep a transition at 2 to 3% of the loop so the camera doesn't snap.
- A keyframe name ends in the film's letter (`-s`, `-d`, or `-s3`, `-d10`); `StylesheetTests` refuses a name the two
  could share.

To add a scene: its card and caption in the markup (`c7`, `7 / 7`), a step mark (`k7`), the windows re-cut in the
film's stylesheet, the counts in `ShortMovieTests` or `DetailedMovieTests`, and the golden rewritten with
`LOUIS_UPDATE_GOLDEN=1`. Then export the films (below). To see the result frame by frame without waiting, pause the
page's animations in the browser's console and set their time:
`document.getAnimations().forEach(a => { a.pause(); a.currentTime = 12000 })`.

## Export the films

After changing a film, run the tool and commit what it writes, or `FilmExportTests.TheExports_MatchTheFilms` fails:

```bash
dotnet run --project demos/louis-agent/tools/louis-agent.film-export          # both, into demos/louis-agent/docs/films
dotnet run --project demos/louis-agent/tools/louis-agent.film-export -- short # one
```

It needs Docker running, for the encoder. `CHROME_PATH` names the browser; without it, a Chrome for Testing is
downloaded once into the temp folder. It starts `louis-agent.coding-api` itself, from the code as it is, and takes
several minutes a film. Each re-export adds the films' size to git history, so group changes.
[ADR 0077](../adr/0077-films-on-github-are-rendered-webp.md) says why it works this way.

## Test it

```bash
dotnet test demos/louis-agent/tests/louis-agent.code-review.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/louis-agent/tests/louis-agent.code-review.tests     # after a deliberate markup change: then read the diff
```

- The packs' views and their plain classes (`UnifiedDiff`, `Patch`, `RelativeTime`, `CodeLanguage`, `FileSize`) are tested under `packs/tests/`.
- `Pages/ChatPageTests` renders the whole page with bUnit against `FixtureApi`, a stub of the API from
  `src/tests/web/louis-agent.web.testing`. The client is the real `AgentApi`, and the message streams a recorded Puls turn.
  The empty page and the page with the turn are pinned to `ChatPage.empty.html` and `ChatPage.turn.html`.
- The tests run in the invariant culture (`TestCulture`), so the golden files never depend on the machine.

Then check it in a browser: send a message, open a file, stage and commit a change, merge a branch. Nothing pushes:
the API's git endpoints never push.

## Before you ask for a review

- [ ] No `@code` in a `.razor`; new logic in a plain class with its test.
- [ ] New calls go through `AgentApi`, never `HttpClient` in a component.
- [ ] The golden files' diff is exactly the change you meant.

---
[Guides](../../../../src/docs/guides/README.md) · [Docs index](../../../../src/docs/README.md)
