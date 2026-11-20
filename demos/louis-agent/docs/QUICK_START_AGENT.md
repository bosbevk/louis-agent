# Louis Agent: Quick Start

> **Tutorial.** Start the web app, Puls, Rider or the CLI and try your first prompts. **Elsewhere:** every setting in
> [Setup](../../../src/docs/SETUP.md), choosing a model in [Models](../../../src/docs/MODELS.md), how it works in [Architecture](../../../src/docs/architecture/README.md).

Louis Agent is an AI agent that works on your repository with real tools: it reads, searches and edits files, runs git,
`dotnet`, Python, PowerShell and bash, and searches the web. You see its thinking and every tool call as it works.

![Quick start: start the web app, Puls, Rider or the CLI, try a first prompt, read what it did and what it cost](diagrams/QUICK_START_AGENT.svg)

Set up the coding agent's `config/` first ([its setup](SETUP.md)): at least `ANTHROPIC_API_KEY` in
`demos/louis-agent/config/.env.secrets`.

## Ways to talk to it

### Web app

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api
```

Open **http://127.0.0.1:5082**.

The web app has a view for each job, and a theme switch:

| View | What you do there |
|---|---|
| **Chats** | Answers stream with collapsible thinking, tool cards (click for input and result) and highlighted code. 📎 attaches text files. Enter sends, Shift+Enter is a new line, **Stop** ends a reply. |
| **Files** | Browse the repo, open a file, **Ask agent to review**. |
| **Changes** | See what changed, open a diff, tick files to **Stage**, **Unstage** or **Discard**, write a message and **Commit**, or **Review all** with the agent. Pushing stays with you. |
| **Branches** | See every branch and all commits, open a commit or a branch to read its diff, **Merge** a branch into the checked-out one, and delete merged branches. |
| **Theme** | Pick *Glass* (what a first visit gets), *Flat* or the app's own *Retro*, each light or dark. |

### Puls (the general assistant, in Afrikaans)

```bash
docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build
```

Open **http://127.0.0.1:5070** and ask anything, in Afrikaans or English: *"Wat is die nuutste weergawe van .NET?"*,
*"Lees README.md en som dit op"*, *"Hoeveel .cs-lêers is daar?"*. Puls answers from the web itself and hands cooking
to its cook, a specialist on the same instance; the work panel shows the specialist's steps, the sources it read and
what it cost. Attach text files with **+** (PDF, Word files and spreadsheets are read from the workspace; attaching one
doesn't work yet), and set your location in its settings if you want "near me" to work.

### Rider

Add the agent server from `demos/louis-agent/config/rider/rider-acp.example.json` in Rider's AI Assistant settings, then pick **Louis Agent** in
the AI chat. Attach files with the paperclip; the agent reads them. Each new chat starts a fresh container.

### CLI

```bash
dotnet run --project demos/louis-agent/hosts/louis-agent.coding-cli                     # interactive; Ctrl+C stops a reply, 'exit' quits
dotnet run --project demos/louis-agent/hosts/louis-agent.coding-cli -- "Your prompt here" # one-shot
```

### HTTP API

See [How the agents and endpoints talk](../../../src/docs/AGENT_COMMUNICATION.md#the-http-api-louis-agentapi): create a session, then POST messages and read the event stream.

## Example prompts

**Code and files**
```
Review src/hosts/louis-agent.api/Endpoints/Git/GitBranchesEndpoints.cs for bugs, most important first
Find all TODO comments in the codebase
Add XML doc comments to the public methods in WorkspaceFileTools.cs
```

**Build and test**
```
Build the solution and fix any compile errors
Run the tests in src/tests/engine/louis-agent.core.tests and explain any failures
```

**Git**
```
What changed since the last commit?
Create a branch feature/email-notifications and commit the current changes with a good message
```

**Research**
```
Search the web for the latest Microsoft.Extensions.AI release notes and summarise what's new
```

## Reading what it did

Every tool call is visible: as a card in the web app and Rider, as a grey `> Tool args` / `done:` line in the CLI, and
in the logs:

```
[INFO] LLM: anthropic:claude-haiku-5-5 (endpoint=default, apiKey=set); tools=on; thinking=medium
[TOOL] -> GetCurrentBranch({})
[TOOL] <- GetCurrentBranch: main
```

Container logs are kept in `demos/louis-agent/logs/` (`agent-{host}-{time}-{process}.log`), plus `oversized-tool-results.jsonl` and
`truncated-tool-calls.jsonl` when a tool returned too much or a reply ran out of room.

## Agent-built tools

The agent can write its own small tools. New ones wait for you:

```
/tools              list approved and pending tools
/approve <Name>     keep a pending tool
/reject <Name>      throw it away
```

These commands work in the CLI, Rider and the web app, and are never sent to the model.

## What it costs

Every answer says what it used: a grey line on stderr in the CLI, a quiet line under the answer in the web app (with
the chat's total above the message box), and in Rider when `USAGE_IN_CHAT=true`:

```
turn: 3 requests · 28k tokens (26k cached) · $0.006 · session $0.04
```

Costs are shown in your PC's currency once `HOST_CULTURE` is set (run
`dotnet run --project src/tools/louis-agent.usage-probe -- --host-culture` once): `R0.11`, with the rate and the dollars
charged in `/usage`; the web app's sidebar can pick another currency. `/usage` gives the session's totals by purpose and
by token kind. Local models are priced at $0; a model missing from
`demos/louis-agent/config/prices.json` shows "price unknown". Every request is also in the usage ledger
(`{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`); outside Docker, set `LOG_DIRECTORY` (an absolute path) to see the line at all.

## Tips

1. **Be specific** — "Review GitBranchesEndpoints.cs for path-handling bugs" beats "check my code".
2. **Review before keeping** — after the agent edits files, use the web app's **Changes** tab to read the diffs and
   stage only what you want.
3. **Big outputs** — ask for a file path or a filter ("diff of just AgentEngine.cs"); huge tool output gets summarised.
4. **One task per prompt** works better than several unrelated ones.

## Troubleshooting

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required"** — add the key to `demos/louis-agent/config/.env.secrets` and restart.
- **The web app says the API needs a key** — enter `AGENT_API_KEY` in the sidebar's API key field.
- **Committing from the web app fails with an identity error** — set `GIT_AUTHOR_*` / `GIT_COMMITTER_*` in
  `demos/louis-agent/config/.env.secrets` and restart the api container.
- **A Rider chat behaves like old code** — start a **New Chat**; existing chats keep their container until closed.

---
[Docs index](../../../src/docs/README.md) · Previous: [Project README](../../../README.md) · Next: [Setup](../../../src/docs/SETUP.md)
