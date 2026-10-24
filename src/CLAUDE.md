# Hosts: development guide

> **How-to.** What to know before changing a host in `src/` (the CLI, ACP server, API, web apps, MCP server,
> orchestrator): their own texts and the web themes. **Elsewhere:** the message rule itself in
> [Messages and logging](../louis-agent.core/CLAUDE.md#messages-and-logging), hosts staying thin in
> [Project layout](../louis-agent.core/CLAUDE.md#project-layout), how to work in the
> [development guide](../docs/CLAUDE.md). Claude Code loads this file when it works on files in this folder.

## Each host's own texts

**Each host keeps its own texts** in its project's `Messages/` folder, named for the host (`AcpLogMessage`,
`CliUserText`, `OrchestratorToolMessage`, `ProbeUserText`…): core can't know them. `HostMessageEnumTests` checks them
from source.

## A web app's folders

A web app (`louis-agent.web`, `louis-agent.puls`, and `louis-agent.web.shared` between them) keeps Blazor's `Pages/`,
`Layout/` and `Components/`, with subfolders by concern inside `Components/` (the code-review app: `Workspace/`, `Git/`;
Puls: `Brand/`, `Landing/`, `Chat/`, `Admin/`), and puts its C# code in a folder per concern (the shared library: `Api/`,
`Chats/`, `Usage/`, `Storage/`, `Text/`; the code-review app: `Workspace/`, `Git/`; Puls: `Text/`, `Content/`), one type
per file, the namespace equal to the folder path. No `Services/` or `Models/`: each names a kind of thing, not a concern.
`_Imports.razor` carries a `@using` per folder the pages use. A page keeps its layout and hands each piece to a component
(W2): state flows down as parameters and up as `EventCallback`s, a component never reaches into its page, and a
`.razor` holds markup only: its C# is the `X.razor.cs` code-behind (a `partial class` in the folder's namespace), and
logic that needs no component (text, search, matching, formatting, sums) a plain class in a concern folder with NUnit
tests (`RazorHasNoCodeTests` holds it; its `PendingCode` list is what W2b still has to move). A string parameter is passed as
`@Value`, never `Value`: Razor reads the bare name as a literal. A turn's life (the draft, the session, the stream, the
save) is the shared `ChatRunner`, never a page's own copy. The shared library's chat types are in `Chats/`, not `Chat/`:
a namespace named like a type shadows the type for every file under its parent.

## Running a host from Rider

Each host's `Properties/launchSettings.json` holds its run profiles: the settings its compose service gets, as paths
relative to the project folder (`../../logs`), on a port beside the container's (5071, 5081, 5091), and (`WORKSPACE_ROOT`, `SKILLS_DIRECTORY`, `LOG_DIRECTORY`, the price and rates
files; the API's `puls` profile adds Puls's own). A new setting a compose file passes to a container goes in the host's
profile too, so a debug run sees what the container sees ([Setup](../docs/SETUP.md#how-settings-are-loaded)).

## Adding a web theme

Copy `src/louis-agent.web/wwwroot/css/themes/paper.css` to `{id}.css`, set the variables listed at the top of
`css/app.css`, and add `{ "id": "{id}", "name": "…" }` to `themes.json`. Ids are lowercase letters, digits and dashes.
