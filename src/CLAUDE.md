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

## Adding a web theme

Copy `src/louis-agent.web/wwwroot/css/themes/paper.css` to `{id}.css`, set the variables listed at the top of
`css/app.css`, and add `{ "id": "{id}", "name": "…" }` to `themes.json`. Ids are lowercase letters, digits and dashes.
