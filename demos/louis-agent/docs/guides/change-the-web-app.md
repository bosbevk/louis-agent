# Change the code-review web app

> **How-to.** Change `demos/louis-agent/web/louis-agent.code-review`, the browser app for chatting with the coding agent, browsing the workspace
> and reviewing changes: run it, add a tab, call a new endpoint, add a theme, place its words, and test it.
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
