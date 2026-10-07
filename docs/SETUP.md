# Configuration Setup Guide

Configuration lives in `config/`. Your settings and secrets are git-ignored; the `.example` files are the tracked
templates.

## Files

| File | In git | Purpose |
|---|---|---|
| `config/.env` | ❌ git-ignored | Your non-secret settings: model, agent function, paths, DevOps project |
| `config/.env.example` | ✅ tracked | Template for `config/.env`, with explanations |
| `config/.env.secrets` | ❌ git-ignored | Your API keys and other secrets |
| `config/.env.secrets.example` | ✅ tracked | Template listing every secret |
| `config/rider-acp.example.json` | ✅ tracked | Rider agent-server entry for the ACP server (replace `C:\path\to`) |
| `config/.rider-mcp-servers.json` | ✅ tracked | MCP client entry for the MCP server (replace `C:\path\to`) |

## First-time setup

```bash
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets
# then fill in config/.env.secrets (at least ANTHROPIC_API_KEY)
```

In `config/.env`, point `REPOSITORIES_PATH` and `ACP_MOUNT_MAPPINGS` at the folder that holds your repositories (for
example `C:\Users\you\RiderProjects` and `C:\Users\you\RiderProjects=/repositories`), and pick your model:

```bash
LLM_PROVIDER=anthropic
LLM_MODEL=claude-haiku-4-5-20251001
```

See [MODELS.md](MODELS.md) for other providers.

## How settings are loaded

**Outside Docker** (CLI, `dotnet run`): `AgentHost.LoadEnvironment()` searches upward from the working directory for a
`config/` folder and loads, in order, `config/.env.secrets`, `config/.env`, then a legacy `./.env` if present. A
variable that is already set is never overwritten, so the precedence is:

1. Variables already in the process environment (shell, CI) — highest
2. `config/.env.secrets`
3. `config/.env`
4. `./.env` (legacy)

**In Docker** compose reads `config/.env` then `config/.env.secrets` as `env_file`s (a key in `.env.secrets` overrides the
same key in `.env`), and the service's own `environment:` entries win over both. Always pass the env file so compose can
fill in paths such as `REPOSITORIES_PATH`:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

`.env.example`, `.env.secrets.example` and `.env.local` are never loaded.

## Settings

### `config/.env` (non-secret)

| Setting | Meaning |
|---|---|
| `LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT` | Model and where it runs ([MODELS.md](MODELS.md)) |
| `LLM_THINKING` | Visible reasoning: `off` / `low` / `medium` / `high` (default `medium` for Anthropic, `off` otherwise) |
| `LLM_SUPPORTS_TOOLS` | Force tool calling on or off (normally inferred from the model) |
| `AGENT_FUNCTION` | Which `Skills/{name}-skills.md` to load; `louis` loads every skill file |
| `REPOSITORIES_PATH` | Host folder mounted at `/repositories` in the containers |
| `ACP_MOUNT_MAPPINGS` | `HOST_PATH=/container/path` pairs, so Rider's Windows paths (attachments, open file) resolve in the container |
| `RIDER_MCP_ENDPOINT`, `RIDER_MCP_AUTO_DISCOVER` | Rider MCP discovery (off by default; it only logs Rider's tools for now) |
| `API_PORT` | Host port for the API and web app (default `5080`, always bound to `127.0.0.1`) |
| `MCP_TRANSPORT`, `MCP_PORT` | MCP server transport: `stdio` (default) or `http`; host port in HTTP mode (default `5090`, bound to `127.0.0.1`) |
| `WEB_SEARCH_PROVIDER` | `google` or `duckduckgo` (default: Google when its key and engine id are set) |
| `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT` | Azure DevOps organisation (`dev.azure.com/<organisation>`) and project for the DevOps tools |
| `DEVOPS_TEAM` | Optional team: sprints are looked up under it, and an empty sprint means its current sprint |

Set by compose for the containers, so normally not in `.env`: `WORKSPACE_ROOT`, `SKILLS_DIRECTORY`, `LOG_DIRECTORY`.

### `config/.env.secrets`

| Setting | Meaning |
|---|---|
| `ANTHROPIC_API_KEY` | Claude API key (required for the default provider) |
| `ANTHROPIC_WORKSPACE_ID` | Only if the key isn't scoped to a workspace |
| `AGENT_API_KEY` | When set, required for the HTTP API (`x-api-key` or `Authorization: Bearer`) |
| `MCP_API_KEY` | Key for the MCP server in HTTP mode, same header rules; falls back to `AGENT_API_KEY`. With neither set, HTTP mode is unauthenticated |
| `GIT_AUTHOR_NAME`, `GIT_AUTHOR_EMAIL`, `GIT_COMMITTER_NAME`, `GIT_COMMITTER_EMAIL` | Identity for commits made from the web app's Changes view |
| `PAYMO_API_KEY` | Enables the Paymo tools |
| `DEVOPS_API_KEY` | Azure DevOps personal access token; enables the DevOps tools (with the settings below) |
| `GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID` | Google web search (otherwise DuckDuckGo) |
| `RIDER_MCP_PROJECT_PATH` | Host path of the project, sent to Rider's MCP server |

After changing a secret, restart the container that uses it (for the API:
`docker compose -f docker/docker-compose.yml --env-file config/.env up -d api`).

The orchestrator demo (`samples/run-demo.ps1`) reads the same two files; its own settings are set in
`docker/docker-compose.demo.yml` (see [samples/README.md](../samples/README.md)).

## Adding a setting

- **Non-secret:** add it to `config/.env.example` (documented) and `config/.env`.
- **Secret:** add a placeholder to `config/.env.secrets.example` and the real value to your `config/.env.secrets`.
- **In code:** read it in `AgentOptions.FromEnvironment` or `LlmOptions.FromEnvironment`, not with ad-hoc
  `Environment.GetEnvironmentVariable` calls inside the tools.

## CI

Set secrets as environment variables; they take precedence over the files:

```yaml
env:
  ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
```

The unit tests need no secrets, network or Docker.

## Security

- Never commit `config/.env.secrets`, and don't paste keys into commits, PRs or chat.
- Secret files are also off limits to the agent: it can't read or write `.env*`, key or credential files.

## Troubleshooting

**"ANTHROPIC_API_KEY (or LLM_API_KEY) is required"** — `config/.env.secrets` is missing or the key is empty. Check with
`grep ANTHROPIC_API_KEY config/.env.secrets`, then restart the container.

**Wrong settings in Docker** — run compose with `--env-file config/.env`, and remember that `environment:` in
`docker/docker-compose.yml` wins over the env files. `docker compose ... config` shows the resolved values.

**"must include the anthropic-workspace-id header"** — set `ANTHROPIC_WORKSPACE_ID` in `config/.env.secrets`.

---
[Docs index](README.md) · Previous: [Quick start](QUICK_START_AGENT.md) · Next: [Models](MODELS.md)
