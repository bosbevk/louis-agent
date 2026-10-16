# Configuration Setup Guide

> **Reference.** Every file and setting in `config/`, how they are loaded, and keeping secrets safe. **Elsewhere:**
> choosing a model in [Models](MODELS.md), a first run in the [Quick start](QUICK_START_AGENT.md).

Configuration lives in `config/`. Your settings and secrets are git-ignored; the `.example` files are the tracked
templates.

## Files

| File | In git | Purpose |
|---|---|---|
| `config/.env` | ❌ git-ignored | Your non-secret settings: model, agent function, paths, DevOps project |
| `config/.env.example` | ✅ tracked | Template for `config/.env`, with explanations |
| `config/.env.anthropic`, `config/.env.ollama` | ✅ tracked | LLM profiles: the model settings `LLM_PROFILE` picks ([MODELS.md](MODELS.md)) |
| `config/rates.json` | ✅ tracked | Exchange rates for showing costs in your currency (F14), in our own format: the fallback when `RATES_URL` can't be reached |
| `config/prices.json` | ✅ tracked | Model prices per million tokens for the usage ledger's costs; update it (and its `as_of`) when prices change |
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
example `C:\Users\you\RiderProjects` and `C:\Users\you\RiderProjects=/repositories`), and pick your LLM profile:

```bash
LLM_PROFILE=anthropic   # or ollama: config/.env.anthropic / config/.env.ollama
```

See [MODELS.md](MODELS.md) for other providers.

## How settings are loaded

**Outside Docker** (CLI, `dotnet run`): `AgentHost.LoadEnvironment()` searches upward from the working directory for a
`config/` folder and loads, in order, `config/.env.secrets`, `config/.env`, a legacy `./.env` if present, then the LLM
profile `config/.env.{LLM_PROFILE}`. A variable that is already set is never overwritten, so the precedence is:

1. Variables already in the process environment (shell, CI) — highest, so `LLM_PROFILE=ollama dotnet run …` switches
   one run
2. `config/.env.secrets`
3. `config/.env`
4. `./.env` (legacy)
5. `config/.env.{LLM_PROFILE}` — lowest, so a `LLM_*` line in `config/.env` pins that setting

**In Docker** compose reads `config/.env.${LLM_PROFILE}`, `config/.env` and `config/.env.secrets` as `env_file`s, in that
order (a later file overrides the same key in an earlier one), and the service's own `environment:` entries win over all
of them. `LLM_PROFILE` comes from the `--env-file`, or from the shell for one command. Always pass the env file so compose can
fill in paths such as `REPOSITORIES_PATH`:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

`.env.example`, `.env.secrets.example` and `.env.local` are never loaded.

## Settings

### `config/.env` (non-secret)

| Setting | Meaning |
|---|---|
| `LLM_PROFILE` | Which LLM profile to load: `anthropic` (`config/.env.anthropic`) or `ollama` (`config/.env.ollama`) |
| `LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT` | Model and where it runs; normally set by the profile, and a line here overrides it ([MODELS.md](MODELS.md)) |
| `LLM_THINKING` | Visible reasoning: `off` / `low` / `medium` / `high` (default `medium` for Anthropic, `off` otherwise) |
| `LLM_CACHE`, `LLM_CACHE_TTL` | Prompt caching on Anthropic: `on` (default) / `off`, and the cached prefix's lifetime, `5m` (default) or `1h`, chosen with `usage-probe -- --cache-gaps` ([MODELS.md](MODELS.md)) |
| `LLM_SUPPORTS_TOOLS` | Force tool calling on or off (normally inferred from the model) |
| `AGENT_PEERS` | Other louis-agent APIs this one may open sub-sessions on (`StartSubSession`, the `specialists` toolset): `name=url` pairs, comma separated. Empty by default for the code-review api; Puls's compose sets it to its specialist (`puls=http://puls-agent:8080`). Peers are called with this deployment's `AGENT_API_KEY` |
| `PERSONALITIES_ENABLED` | The personalities this instance may use (F29), comma separated (`puls/voice-af,puls/voice-en,puls-cook`); another name is refused as an unknown one, and `GET /personalities` lists only these. Empty: every built-in one (`louis-agent.core/Prompts/`). Puls's compose sets it on both containers, so only a role that has passed its check is reachable |
| `LLM_SESSION_PROFILES` | The LLM profiles a session may name besides the instance's own model (F30), comma separated (`ollama-web`); each is read from `config/.env.{name}` (in a container, the mounted workspace's `config/`), and only its API key settings come from the environment. A session that names another is refused; a listed profile without its file stops start-up. Empty: every session runs on the instance's model |
| `SUB_SESSION_PROMPT_BUDGET` | The most tokens a sub-session's prompt should reach (FX14): when the next request is expected to pass it, the specialist answers from what it has instead of calling more tools. Default `80000`, under Claude Haiku 5.5's long-prompt price step at 100k; `0` turns it off |
| `REPOSITORIES_PATH` | Host folder mounted at `/repositories` in the containers |
| `ACP_MOUNT_MAPPINGS` | `HOST_PATH=/container/path` pairs, so Rider's Windows paths (attachments, open file) resolve in the container |
| `RIDER_MCP_ENDPOINT`, `RIDER_MCP_AUTO_DISCOVER` | Rider MCP discovery (off by default; it only logs Rider's tools for now) |
| `API_PORT` | Host port for the API and web app (default `5080`, always bound to `127.0.0.1`) |
| `PULS_PORT`, `PULS_CODER_PORT`, `PULS_WORKSPACE` | Puls (`docker/docker-compose.puls.yml`): its port (default `5070`), its specialist's (`5071`), both bound to `127.0.0.1`, and the folder mounted as its workspace (default the repo) |
| `MCP_TRANSPORT`, `MCP_PORT` | MCP server transport: `stdio` (default) or `http`; host port in HTTP mode (default `5090`, bound to `127.0.0.1`) |
| `WEB_SEARCH_PROVIDER` | `tavily`, `google` or `duckduckgo` (default: Tavily when `TAVILY_API_KEY` is set, else Google when its key and engine id are, else DuckDuckGo) |
| `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT` | Azure DevOps organisation (`dev.azure.com/<organisation>`) and project for the DevOps tools |
| `DEVOPS_TEAM` | Optional team: sprints are looked up under it, and an empty sprint means its current sprint |
| `USAGE_LEDGER` | `on` (default) records every model request's tokens and cost to `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`; `off` disables it, and with it every usage line (CLI, web app, Rider, the API's `usage` fields, the comms log's costs): `/usage` then says usage isn't tracked. No `LOG_DIRECTORY` has the same effect |
| `PRICES_FILE` | The price table for those costs (default `config/prices.json`, found like `config/.env`; compose sets it for the containers). Without one, costs are recorded as unknown (`null`); a malformed one stops the host |
| `HOST_CULTURE` | The PC's Windows culture (e.g. `en-ZA`): costs are shown in its region's currency (ZAR, `R0.11`). Containers can't see the PC's region, so write it once with `dotnet run --project tools/louis-agent.usage-probe -- --host-culture`. Outside a container, the process's own culture is used when it's unset |
| `DISPLAY_CURRENCY` | Show costs in this currency instead (an ISO code, e.g. `EUR`); the web app can also pick one per browser |
| `RATES_URL` | Where exchange rates come from (default `https://api.frankfurter.dev/v1/latest?base=USD`, the ECB's reference rates); `off` uses `RATES_FILE` only. If it can't be reached, or answers with rates from another currency, the file is used. The demo's orchestrator reads demo-api's `GET /rates` |
| `RATES_FORMAT` | The format of `RATES_URL`'s reply, turned into ours by its mapper: `frankfurter` (default) or `rates-json` (another louis-agent's `GET /rates`) |
| `RATES_FILE` | The fallback rates (default `config/rates.json`; compose sets it). Costs are only converted for display: the ledger always keeps what was charged |
| `PRICES_URL` | Fetch the price table over HTTP instead, e.g. louis-agent.api's public `GET /prices` (the demo's orchestrator uses `http://demo-api:8080/prices`). If it can't be reached at start-up, the host warns and uses the file above |
| `USAGE_IN_CHAT` | `true` adds each turn's usage line to the chat in Rider (ACP), after the answer; off by default. The CLI and web app always show it beside the answer |
| `LOG_DIRECTORY` | Run logs and the usage ledger. Compose sets it for the containers (`logs/` on the host); for `dotnet run`, set it (for example `LOG_DIRECTORY=logs`) or the ledger is off, with a warning |

Set by compose for the containers, so normally not in `.env`: `WORKSPACE_ROOT`, `SKILLS_DIRECTORY`, `LOG_DIRECTORY`.

**The script tools' interpreters**, only needed when they can't be found (mostly `dotnet run` on Windows); read from the
environment by the tools themselves:

| Setting | Meaning |
|---|---|
| `BASH_PATH` (or `SKILL_SHELL`) | The bash for `BashRun` and bash skills. Default: `/bin/bash`, or Git Bash on Windows (Program Files, or next to `git.exe` on `PATH`); WSL's bash is never used |
| `POWERSHELL_PATH` | The PowerShell for `PowerShellRun`. Default: `pwsh`, then `powershell.exe` on Windows |
| `PYTHON_PATH` | The Python for `PythonRun`. Default: `py -3`, `python` or `python3` on Windows; `python3` or `python` elsewhere |
| `PYTHON_VENV` | Where `PythonRun` keeps the virtual environment for packages it installs. Default: a per-user folder (`%LOCALAPPDATA%/louis-agent/python-venv`, `~/.local/share/louis-agent/python-venv` on Linux) |
| `AGENT_WORKING_DIRECTORY` | The working directory for bash, PowerShell and Python scripts. Default: the workspace root |

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
| `ADMIN_API_KEY` | The admin portal's key (`/admin`, Puls's *Admin* menu); falls back to `AGENT_API_KEY`; with neither the portal is closed |
| `TAVILY_API_KEY` | Tavily web search (1,000 free credits a month; the default once set) and `ReadRenderedPage`, which renders JavaScript pages through Tavily Extract (two credits per five pages) |
| `GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID` | Google web search (discontinued by Google on 2027-01-01; otherwise DuckDuckGo) |
| `RIDER_MCP_PROJECT_PATH` | Host path of the project, sent to Rider's MCP server |

After changing a secret, **recreate** the container that uses it with `up -d`; `docker compose restart` keeps the
container's old environment, so a new key never reaches it that way (found with `TAVILY_API_KEY`, 2026-10-15):

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d api          # the API
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d         # Puls and its specialist
```

A changed file under `Skills/` needs only a restart: the containers mount the folder and read it when a chat opens.

The orchestrator reads the same two files (its demo, removed in CL10, comes back with the rewritten benchmarks).

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
- Secret files are also off limits to the agent: it can't read or write `.env*` (except templates such as `.env.example`), key or credential files.

## Troubleshooting

**"ANTHROPIC_API_KEY (or LLM_API_KEY) is required"** — `config/.env.secrets` is missing or the key is empty. Check with
`grep ANTHROPIC_API_KEY config/.env.secrets`, then recreate the container with `up -d` (not `restart`, which keeps the
old environment).

**Wrong settings in Docker** — run compose with `--env-file config/.env`, and remember that `environment:` in
`docker/docker-compose.yml` wins over the env files. `docker compose ... config` shows the resolved values.

**"must include the anthropic-workspace-id header"** — set `ANTHROPIC_WORKSPACE_ID` in `config/.env.secrets`.

---
[Docs index](README.md) · Previous: [Quick start](QUICK_START_AGENT.md) · Next: [Models](MODELS.md)
