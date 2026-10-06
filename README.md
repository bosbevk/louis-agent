# Louis Agent

A self-hosted AI coding agent for .NET, built on [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai).
It works on your repository with real tools — files, git, `dotnet`, Python, PowerShell, bash, web search, Azure DevOps and
Paymo — and talks to you through JetBrains Rider, a web app, an HTTP API or the terminal. Answers stream as they are
written, including the model's thinking and every tool call.

Runs on Claude (Anthropic) by default; Ollama and OpenAI-compatible endpoints work too.

## Ways to use it

| Host | Project | What it is |
|---|---|---|
| **Rider** | `src/louis-agent.acp-server` | [Agent Client Protocol](https://agentclientprotocol.com) server for Rider's AI chat: streamed answers, thinking, tool cards, file attachments |
| **Web app** | `src/louis-agent.web` | Blazor WebAssembly chat with a file browser, a git Changes view (review diffs, stage, discard, commit) and switchable themes |
| **HTTP API** | `src/louis-agent.api` | JSON API with Claude-style streamed events (SSE); also serves the web app |
| **CLI** | `src/louis-agent.cli` | Interactive or one-shot prompts in the terminal |
| **MCP server** | `src/louis-agent.mcp-server` | Publishes the agent's tools to any MCP client (the client brings its own model) |

All hosts share `louis-agent.core` (engine, tools, skills, providers). See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

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
`.example` files. Full details:
[docs/SETUP.md](docs/SETUP.md) and [docs/MODELS.md](docs/MODELS.md).

| Setting | Purpose |
|---|---|
| `LLM_PROVIDER`, `LLM_MODEL` | `anthropic` (default, `claude-haiku-4-5-20251001`), `ollama` or `openai-compatible` |
| `LLM_THINKING` | Visible reasoning: `off` / `low` / `medium` / `high` (default `medium` for Anthropic) |
| `ANTHROPIC_API_KEY` | Claude API key *(secret)* |
| `AGENT_FUNCTION` | Which `Skills/{name}-skills.md` to load; `louis` loads all |
| `AGENT_API_KEY` | When set, required (as `x-api-key` or `Authorization: Bearer`) for the HTTP API *(secret)* |
| `API_PORT` | Host port for the API / web app (default `5080`, bound to `127.0.0.1`) |
| `GIT_AUTHOR_NAME` / `_EMAIL`, `GIT_COMMITTER_NAME` / `_EMAIL` | Identity for commits made from the web app's Changes view *(secrets file)* |
| `PAYMO_API_KEY`, `DEVOPS_API_KEY` | Enable the Paymo and Azure DevOps tools *(secret)* |
| `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT`, `DEVOPS_TEAM` | Which Azure DevOps project (and optional team) the DevOps tools work on |
| `GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID` | Google for web search; without them it uses DuckDuckGo *(secret)* |
| `REPOSITORIES_PATH`, `ACP_MOUNT_MAPPINGS` | Extra repositories mounted into the containers, and how Rider's Windows paths map to them |

## HTTP API

Sessions live in memory. Every reply is JSON; a message's answer streams as Server-Sent Events shaped like Claude's
(`message_start`, `content_block_start/delta/stop` for `thinking`, `text`, `tool_use` and `tool_result`, `message_stop`).

| Endpoint | |
|---|---|
| `POST /sessions` | Start a session → `{ "session_id" }` |
| `POST /sessions/{id}/messages` | `{ "message", "attachments": [{ "name", "content" }] }` → event stream |
| `POST /sessions/{id}/cancel` · `GET` / `DELETE /sessions/{id}` | Stop a reply, inspect, end |
| `GET /workspace/entries?path=` · `GET /workspace/file?path=` | Browse the workspace (read-only) |
| `GET /git/status` · `GET /git/diff?path=&staged=` | Changed files and diffs |
| `POST /git/stage` · `unstage` · `discard` · `commit` | `{ "paths": [...] }` (commit: `{ "message" }`) |
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
- **Themes** — *Retro terminal* and *Paper (soft light)*. A theme is one CSS file in
  `src/louis-agent.web/wwwroot/css/themes/` plus a line in `themes.json`; the variables it must set are listed at the top
  of `css/app.css`.

## Skills and tools

Built-in tools cover workspace files, git, `dotnet` build/test/run, Python, PowerShell, bash, web search and page fetching,
Azure DevOps work items and Paymo time tracking. Markdown **skills** in [`Skills/`](Skills) add guidance and repeatable
procedures: `personality.md` gives the agent its character (edit or delete it to change who the agent is), `default.md`
holds its operating instructions; the agent can write new skills, and can build its own typed tools, which wait in `Skills/tools/pending`
until you approve them (`/tools`, `/approve <Name>`, `/reject <Name>`).

## Safety

- File tools stay inside the workspace, refuse secret files (`.env`, keys, certificates) and don't follow symbolic links.
- Scripts and git run without a shell, with arguments passed as a list.
- The API and web app are published on `127.0.0.1` only; set `AGENT_API_KEY` to require a key.
- Oversized tool output is summarised instead of flooding the model's context, and a tool call cut off by the output
  limit is never run.

## Logs

Containers write their logs to `logs/` (git-ignored) so they survive `--rm`, plus JSON-lines records of oversized tool
results and truncated tool calls for spotting where the agent needs narrower tools.

## Development

```bash
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
```

Requires the .NET 10 SDK. More docs: [Architecture](docs/ARCHITECTURE.md) · [Setup](docs/SETUP.md) ·
[Models](docs/MODELS.md) · [Agent quick start](docs/QUICK_START_AGENT.md) · [Agent interaction](docs/AGENT_INTERACTION.md) ·
[TODO](TODO.md).

## License

[MIT](LICENSE)
