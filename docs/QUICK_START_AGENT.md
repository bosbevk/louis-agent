# Louis Agent: Quick Start

Louis Agent is an AI agent that works on your repository with real tools: it reads, searches and edits files, runs git,
`dotnet`, Python, PowerShell and bash, searches the web, and (with keys) works with Azure DevOps and Paymo. You see its
thinking and every tool call as it works.

Set up `config/` first ([SETUP.md](SETUP.md)): at least `ANTHROPIC_API_KEY` in `config/.env.secrets`.

## Ways to talk to it

### Web app

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

Open **http://127.0.0.1:5080**.

- **Chats** — answers stream with collapsible thinking, tool cards (click for input and result) and highlighted code.
  📎 attaches text files. Enter sends, Shift+Enter is a new line, **Stop** ends a reply.
- **Files** — browse the repo, open a file, **Ask agent to review**.
- **Changes** — see what changed, open a diff, tick files to **Stage**, **Unstage** or **Discard**, write a message and
  **Commit**, or **Review all** with the agent. Pushing stays with you.
- **Branches** — see every branch and all commits, open a commit or a branch to read its diff, **Merge** a branch into
  the checked-out one, and delete merged branches.
- **Theme** — switch between *Retro terminal* and *Paper (soft light)* at the bottom of the sidebar.

### Rider

Add the agent server from `config/rider-acp.example.json` in Rider's AI Assistant settings, then pick **Louis Agent** in
the AI chat. Attach files with the paperclip; the agent reads them. Each new chat starts a fresh container.

### CLI

```bash
dotnet run --project src/louis-agent.cli                     # interactive; Ctrl+C stops a reply, 'exit' quits
dotnet run --project src/louis-agent.cli -- "Your prompt here" # one-shot
```

### HTTP API

See the [README](../README.md#http-api): create a session, then POST messages and read the event stream.

## Example prompts

**Code and files**
```
Review src/louis-agent.api/GitEndpoints.cs for bugs, most important first
Find all TODO comments in the codebase
Add XML doc comments to the public methods in WorkspaceTools.cs
```

**Build and test**
```
Build the solution and fix any compile errors
Run the tests in tests/louis-agent.core.tests and explain any failures
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

**Azure DevOps** (with `DEVOPS_API_KEY` and `DEVOPS_ORGANIZATION` / `DEVOPS_PROJECT`)
```
Show me all blockers in the current sprint
```

**Paymo** (with `PAYMO_API_KEY`) — see [PAYMO-PROMPTS.md](PAYMO-PROMPTS.md)
```
Log 2 hours on ticket 1234567 – fixed the login redirect
```

## Reading what it did

Every tool call is visible: as a card in the web app and Rider, as a grey `> Tool args` / `done:` line in the CLI, and
in the logs:

```
[INFO] LLM: anthropic:claude-haiku-4-5-20251001 (endpoint=default, apiKey=set); tools=on; thinking=medium
[TOOL] -> GetCurrentBranch({})
[TOOL] <- GetCurrentBranch: main
```

Container logs are kept in `logs/` (`agent-{host}-{time}.log`), plus `oversized-tool-results.jsonl` and
`truncated-tool-calls.jsonl` when a tool returned too much or a reply ran out of room.

## Agent-built tools

The agent can write its own small tools. New ones wait for you:

```
/tools              list approved and pending tools
/approve <Name>     keep a pending tool
/reject <Name>      throw it away
```

These commands work in the CLI, Rider and the web app, and are never sent to the model.

## Tips

1. **Be specific** — "Review GitEndpoints.cs for path-handling bugs" beats "check my code".
2. **Review before keeping** — after the agent edits files, use the web app's **Changes** tab to read the diffs and
   stage only what you want.
3. **Big outputs** — ask for a file path or a filter ("diff of just AgentEngine.cs"); huge tool output gets summarised.
4. **One task per prompt** works better than several unrelated ones.

## Troubleshooting

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required"** — add the key to `config/.env.secrets` and restart.
- **The web app says the API needs a key** — enter `AGENT_API_KEY` in the sidebar's API key field.
- **Committing from the web app fails with an identity error** — set `GIT_AUTHOR_*` / `GIT_COMMITTER_*` in
  `config/.env.secrets` and restart the api container.
- **A Rider chat behaves like old code** — start a **New Chat**; existing chats keep their container until closed.

## More

- [ARCHITECTURE.md](ARCHITECTURE.md) — how it fits together
- [AGENT_INTERACTION.md](AGENT_INTERACTION.md) — tools, skills and a turn step by step
- [MODELS.md](MODELS.md) — choosing a model
- [CLAUDE.md](CLAUDE.md) — development guide
- [samples/README.md](../samples/README.md) — the orchestrator demo: a second agent that has louis-agent fix ten bugs, in Docker
