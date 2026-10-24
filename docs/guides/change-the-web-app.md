# Change the code-review web app

> **How-to.** Change `src/louis-agent.web`, the browser app for chatting with the coding agent, browsing the workspace
> and reviewing changes: run it, add a tab, call a new endpoint, add a theme, place its words, and test it.
> **Elsewhere:** how it works in [louis-agent.web](../projects/louis-agent.web.md); what it shares with Puls in
> [Change the shared web library](change-the-web-shared.md); the folder rules in [src/CLAUDE.md](../../src/CLAUDE.md).

The app is Blazor WebAssembly, served by the API on the same origin, so it has no settings of its own. One page,
`Pages/ChatPage.razor`, holds a sidebar with four tabs (Chats, Files, Changes, Branches) and a main area that shows a
chat, a file, a diff or a commit. What a chat needs (the API client, a turn's life, the turn views, markdown, usage) is
in `src/louis-agent.web.shared`, which Puls uses too.

## Run it

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api      # http://127.0.0.1:5080
dotnet run --project src/louis-agent.api --launch-profile api                             # http://127.0.0.1:5081
```

The API serves this app unless it is built with `WebApp=puls`. After a change to the app, rebuild the `api` image or
restart the run. If the API needs `AGENT_API_KEY`, enter it under *API key* in the sidebar; it stays in this browser.

## Add a tab

1. A `…Panel` component for the sidebar and a viewer for the main area, in the folder of their concern under
   `Components/` (`Workspace/` or `Git/` today, or a new one). The Branches tab (`BranchesPanel`, `HistoryViewer`,
   `PatchView`) is the most recent example to copy.
2. Each `.razor` holds markup only. Its C# goes in the `.razor.cs` code-behind, and any logic that needs no component
   (parsing, formatting, sums) goes in a plain class in a concern folder with its own NUnit tests, as `UnifiedDiff` and
   `RelativeTime` are ([ADR 0034](../adr/0034-code-a-person-can-maintain.md); `RazorHasNoCodeTests` checks it).
3. State that several panels show lives in a `…State` service with a `Changed` event, as `GitState` and `GitHistory`
   do, registered in `Program.cs`. A component takes state as parameters and reports up with `EventCallback`s, and
   never reaches into its page.
4. Wire the tab into `WorkspaceSidebar` and `ChatPage`, keeping `ChatPage.razor` a layout.

## Call a new endpoint

Components never use `HttpClient`. A call only this app makes is an extension method on the shared `AgentApi` in the
app's concern folder, as `Workspace/AgentApiWorkspace.cs` adds the workspace and git calls. A call both apps make goes
in `AgentApi` itself ([Change the shared web library](change-the-web-shared.md)). The endpoint itself is the API's
([Change the API](change-the-api.md)), protected by its key.

## Add a theme

Copy `wwwroot/css/themes/paper.css` to `{id}.css`, set the variables listed at the top of `wwwroot/css/app.css`, and
add `{ "id": "{id}", "name": "…" }` to `themes/themes.json`. Ids are lowercase letters, digits and dashes.

## Words

The shared components' words come from `ChatText`, whose defaults are this app's English (`Program.cs` registers
`new ChatText()`). A label in a `.razor` is markup and stays there. A text built in C# (a notice, an error) goes in a
class of the app's words rather than inline, as Puls keeps its own in `Text/`
([ADR 0009](../adr/0009-message-text-as-enum-templates.md)).

## Test it

```bash
dotnet test tests/louis-agent.web.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.web.tests     # after a deliberate markup change: then read the diff
```

- `Git/` and `Workspace/` test the plain classes (`UnifiedDiff`, `Patch`, `RelativeTime`, `ReviewActions`,
  `CodeLanguage`, `FileSize`).
- `Pages/ChatPageTests` renders the whole page with bUnit against `FixtureApi`, a stub of the API from
  `tests/louis-agent.web.testing`. The client is the real `AgentApi`, and the message streams a recorded Puls turn.
  The empty page and the page with the turn are pinned to `ChatPage.empty.html` and `ChatPage.turn.html`. The test
  also lists every JavaScript call the page makes, so a new call shows up as a review point.
- The tests run in the invariant culture (`TestCulture`), so the golden files never depend on the machine.

Then check it in a browser: send a message, open a file, stage and commit a change, merge a branch. Nothing pushes:
the API's git endpoints never push.

## Before you ask for a review

- [ ] No `@code` in a `.razor`; new logic in a plain class with its test.
- [ ] New calls go through `AgentApi`, never `HttpClient` in a component.
- [ ] The golden files' diff is exactly the change you meant.

---
[Guides](README.md) · [Docs index](../README.md)
