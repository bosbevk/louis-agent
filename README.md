# Louis Agent

A self-hosted AI coding agent for .NET, built on [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai),
and **Puls**, a South African assistant in everyday Afrikaans built on the same core.

The coding agent works on your repository with real tools — files, git, `dotnet`, Python, PowerShell, bash, web search, Azure DevOps and
Paymo — and talks to you through JetBrains Rider, a web app, an HTTP API or the terminal. Answers stream as they are
written, including the model's thinking and every tool call, and each answer says what it used and cost
(`turn: 3 requests · 28k tokens (26k cached) · $0.006`; `/usage` for the whole session).

Runs on Claude (Anthropic) by default; Ollama and OpenAI-compatible endpoints work too.

## Puls

**Puls** (*Elke vraag op pad na 'n antwoord*) is a general AI assistant that speaks everyday South African Afrikaans.
It doesn't answer with tools itself: it hands each task to a **specialist**, a second louis-agent that opens a
session with only the toolsets the task needs and a role that says how to work and report back. There are five roles:
a researcher (web), a reader (files and documents), a historian (git), an analyst (counting and calculating with a
script) and a general specialist. Puls then writes the answer in its own voice.

- **You see the work.** The Werk panel shows each delegation as it happens: the specialist, its toolsets, its task, each
  step, the sources it read and what it cost.
- **Pay as you go, with visible usage.** Every answer shows what it cost, Puls's part and the specialists'. Costs are
  shown in your currency (rand in South Africa); the ledger keeps the dollars actually charged.
- **It knows where and when you are.** It sends your time zone with every chat, and your location (about 1 km) only if
  you share it, so "today" and "near me" are yours.
- **Measured.** A delegation check asks the same questions with and without specialists and prices both from the usage
  ledger ([samples](samples/sample-output-puls-check-repeat/README.md)). On average, delegating costs about a third more
  than one agent answering everything. It was cheaper for chat and one of the two web questions, about even on the
  other, and dearer for short lookups.

```bash
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
```

Open **http://127.0.0.1:5070**; its specialist runs beside it on 5071. Design: [Puls's web app](docs/projects/louis-agent.puls.md),
[F5 toolset profiles](docs/features/F05-toolset-profiles.md) (sub-sessions), [F21 personalities](docs/features/F21-personalities.md)
(the roles), [F22 role tools](docs/features/F22-role-tools.md).

## Ways to use it

| Host | Project | What it is |
|---|---|---|
| **Rider** | `src/louis-agent.acp-server` | [Agent Client Protocol](https://agentclientprotocol.com) server for Rider's AI chat: streamed answers, thinking, tool cards, file attachments |
| **Web app** | `src/louis-agent.web` | Blazor WebAssembly chat with a file browser, a git Changes view (review diffs, stage, discard, commit), a Branches view (all commits, branch diffs, merge) and switchable themes |
| **Puls** | `src/louis-agent.puls` | The general assistant's web app: a landing page and a dark three-panel chat in Afrikaans, with the Werk panel showing each delegation live |
| **HTTP API** | `src/louis-agent.api` | JSON API with Claude-style streamed events (SSE); also serves the web app |
| **CLI** | `src/louis-agent.cli` | Interactive or one-shot prompts in the terminal |
| **MCP server** | `src/louis-agent.mcp-server` | Publishes the agent's tools to any MCP client (the client brings its own model) |

All hosts share `louis-agent.core` (engine, tools, skills, providers). See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

**Orchestrator demo (proof of concept).** `src/louis-agent.orchestrator` is a second agent, also built on
`louis-agent.core`, that watches a microservice's errors, decides per API method what each needs, has louis-agent fix
the fixable ones (one branch and commit each, never pushed) and verifies every fix itself. `samples/run-demo.ps1` runs
it in Docker (`docker/docker-compose.demo.yml`) against a demo service with ten bugs, logs every message between the
agents to a Markdown file, and leaves the web app up on http://127.0.0.1:5081 for you to review and merge the fixes.
See [samples/README.md](samples/README.md).

## Documentation

All docs, in reading order, are listed in **[docs/README.md](docs/README.md)**. The main ones:

| To… | Read |
|---|---|
| Start using it (web app, Rider, CLI) and try prompts | [Quick start](docs/QUICK_START_AGENT.md) |
| Configure settings and secrets, or pick a model | [Setup](docs/SETUP.md) · [Models](docs/MODELS.md) |
| Understand what happens when you send a message | [How a turn works](docs/AGENT_INTERACTION.md) |
| Connect another program or agent: HTTP API and its event stream, Rider, MCP, agent-to-agent | [How the agents and endpoints talk](docs/AGENT_COMMUNICATION.md) |
| Know the components, security and deployment | [Architecture](docs/ARCHITECTURE.md) · [project by project](docs/projects/README.md) |
| Change the code | [Development guide](docs/CLAUDE.md) |
| Watch two agents fix ten bugs, then merge the fixes | [Orchestrator demo](samples/README.md) · [sample output](samples/sample-output/README.md) |
| See what Puls's delegation costs against one agent | [Delegation check](samples/sample-output-puls-check-repeat/README.md) |
| Learn how the orchestrator decides, and write runbooks | [Runbooks](docs/RUNBOOKS.md) |

## Quick start (Docker)

Prerequisites: Docker Desktop and an [Anthropic API key](https://console.anthropic.com).

```bash
# 1. Secrets (git-ignored)
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets
#    then set ANTHROPIC_API_KEY (and anything else you use) in config/.env.secrets

# 2. Paths: set REPOSITORIES_PATH / ACP_MOUNT_MAPPINGS in config/.env to the folder that holds your repos

# 3. Start the web app + API
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

Open **http://127.0.0.1:5080**.

### Rider

Add an agent server in Rider's AI Assistant settings (see [config/rider-acp.example.json](config/rider-acp.example.json)),
pointing at your checkout:

```json
"Louis Agent": {
  "command": "C:\\Program Files\\Docker\\Docker\\resources\\bin\\docker.exe",
  "args": ["compose", "-f", "<repo>\\docker\\docker-compose.yml", "--env-file", "<repo>\\config\\.env",
           "run", "--rm", "-T", "acp-server"]
}
```

Each Rider chat starts its own `acp-server` container. Files you attach in the chat are read into the prompt.

### CLI

```bash
dotnet run --project src/louis-agent.cli                 # interactive (Ctrl+C stops a reply)
dotnet run --project src/louis-agent.cli -- "your prompt" # one-shot
```

Outside Docker the CLI finds `config/.env` and the repo root from any folder in the repository.

## Configuration

Settings live in `config/.env` and secrets in `config/.env.secrets`; both are git-ignored and created from their
`.example` files. Every setting is in [docs/SETUP.md](docs/SETUP.md); choosing a model in [docs/MODELS.md](docs/MODELS.md).

## HTTP API

Sessions live in memory. Every reply is JSON; a message's answer streams as Server-Sent Events shaped like Claude's
(`message_start`, `content_block_start/delta/stop` for `thinking`, `text`, `tool_use` and `tool_result`, `message_stop`).
The full reference — authentication, errors, the event stream with an example, and how other agents use it — is in
[docs/AGENT_COMMUNICATION.md](docs/AGENT_COMMUNICATION.md).

| Endpoint | |
|---|---|
| `POST /sessions` | Start a session → `{ "session_id" }`. Optional: `toolsets` (only those tools and guides), `personality` (a voice or a specialist's role), `location` and `time_zone`, and tags for the usage ledger (`task`, `run`, `service`) |
| `GET /toolsets` · `GET /personalities` · `GET /model` | What a session can load, the personalities it can choose, and the model this instance runs |
| `POST /sessions/{id}/messages` | `{ "message", "attachments": [{ "name", "content" }] }` → event stream |
| `POST /sessions/{id}/cancel` · `GET` / `DELETE /sessions/{id}` | Stop a reply, inspect, end |
| `GET /workspace/entries?path=` · `GET /workspace/file?path=` | Browse the workspace (read-only) |
| `GET /git/status` · `GET /git/diff?path=&staged=` | Changed files and diffs |
| `POST /git/stage` · `unstage` · `discard` · `commit` | `{ "paths": [...] }` (commit: `{ "message" }`) |
| `GET /git/branches` · `GET /git/log?count=` | Branches (ahead/behind the checked-out branch) and the log across all branches |
| `GET /git/commit?sha=` · `GET /git/branch?name=` | A commit's diff; a branch's commits and what merging it would bring |
| `POST /git/merge` · `POST /git/branch/delete` | `{ "branch" }`: merge into the checked-out branch (`--no-ff`, aborted on conflict); delete a merged branch |
| `GET /prices` · `GET /rates` · `GET /rates/display` | The price table, the exchange rates, and the currency costs are shown in |
| `GET /health` | Liveness (no key needed) |

```bash
SID=$(curl -s -X POST http://127.0.0.1:5080/sessions | jq -r .session_id)
curl -N -X POST http://127.0.0.1:5080/sessions/$SID/messages \
     -H "Content-Type: application/json" -d '{"message":"What branch am I on?"}'
```

## Web app

- **Chats** stream with collapsible thinking, tool cards (input and result) and highlighted code; chats are kept in the browser.
- **Files** — browse the repo, view files with syntax highlighting, ask the agent to review one.
- **Changes** — see what the agent (or you) changed, read each diff, tick files to stage / unstage / discard, commit,
  or ask the agent to review the changes. Pushing stays with you.
- **Branches** — every branch and how far it is ahead, all commits across branches, each commit's or branch's diff;
  merge a branch into the checked-out one, or delete a merged branch.
- **Themes** — *Retro terminal* and *Paper (soft light)*. A theme is one CSS file in
  `src/louis-agent.web/wwwroot/css/themes/` plus a line in `themes.json`; the variables it must set are listed at the top
  of `css/app.css`.

## Skills and tools

Built-in tools cover workspace files, git, `dotnet` build/test/run, Python, PowerShell, bash, web search and page fetching,
Azure DevOps work items and Paymo time tracking. Markdown **skills** in [`Skills/`](Skills) add guidance and repeatable
procedures: `personality.md` gives the agent its character (edit or delete it to change who the agent is), and
`default.md` holds its operating instructions and routes each task to a toolset. A session can load only the toolsets
it needs (`files`, `git`, `dotnet`, `web`, `python`, …), each with its own guide (`Skills/{toolset}-skills.md`), so its
prompt stays small and cacheable. A session can also name a personality from `Skills/personalities/`: Puls's voice
(`puls.md`), or a specialist's role (`puls-researcher.md`, `puls-reader.md`, …). The agent can write new skills, and can build its own typed tools, which
wait in `Skills/tools/pending` until you approve them (`/tools`, `/approve <Name>`, `/reject <Name>`).

## Safety

- File tools stay inside the workspace, refuse secret files (`.env`, keys, certificates; templates such as
  `.env.example` are readable) and don't follow symbolic links.
- Scripts and git run without a shell, with arguments passed as a list.
- The API and web app are published on `127.0.0.1` only; set `AGENT_API_KEY` to require a key. The MCP server in HTTP
  mode is also local-only and takes `MCP_API_KEY`.
- The web app never pushes (its commits and merges stay local), and the agent only pushes when you ask it to.
- Oversized tool output is summarised instead of flooding the model's context, and a tool call cut off by the output
  limit is never run.
- A specialist's sub-session is one level deep (it can't delegate again), limited to 3 per turn and 5 minutes each,
  and has a prompt budget, so a badly routed task can't grow into long-context prices.

## Logs

Containers write their logs to `logs/` (git-ignored) so they survive `--rm`, plus JSON-lines records of oversized tool
results and truncated tool calls for spotting where the agent needs narrower tools.

## Development

```bash
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
dotnet test tests/louis-agent.api.tests
dotnet test tests/louis-agent.orchestrator.tests
```

Requires the .NET 10 SDK. See the [development guide](docs/CLAUDE.md) and the [docs index](docs/README.md).

## License

[MIT](LICENSE)
