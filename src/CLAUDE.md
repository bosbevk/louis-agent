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
`Chat/`, `Usage/`, `Storage/`, `Text/`; the code-review app: `Workspace/`, `Git/`; Puls: `Text/`, `Content/`), one type
per file, the namespace equal to the folder path. No `Services/` or `Models/`: each names a kind of thing, not a concern.
`_Imports.razor` carries a `@using` per folder the pages use. The shared library has a namespace named `Chat` and a type
named `Chat`, so a C# file under `LouisAgent.Web.Shared` puts its usings after its `namespace` line, where they resolve
the type; a Razor file gets this for free.

## Adding a web theme

Copy `src/louis-agent.web/wwwroot/css/themes/paper.css` to `{id}.css`, set the variables listed at the top of
`css/app.css`, and add `{ "id": "{id}", "name": "…" }` to `themes.json`. Ids are lowercase letters, digits and dashes.
