# Configuration: how a host reads its settings

> **Reference.** How the framework finds and loads settings, and every setting its code reads. **Elsewhere:** a demo's own
> files, commands and settings in [Puls's Setup](../../demos/puls/docs/SETUP.md) and
> [the coding agent's Setup](../../demos/louis-agent/docs/SETUP.md); choosing a model in [Models](MODELS.md).

The framework assumes nothing about your folders. Whoever runs a host keeps a `config/` folder of their own, and on this page
`config/` means that folder. Your settings and secrets in it are git-ignored; the `.example` files are the tracked templates.

## How settings are loaded

**Each project has its own `config/`**, and the paths below are inside it. `CONFIG_DIRECTORY` tells a host which one; every launch profile and compose service sets it. It comes from the
process, never a file, since it says which files to read.

**Outside Docker** (CLI, `dotnet run`): `AgentHost.LoadEnvironment()` uses `CONFIG_DIRECTORY`, or else searches upward
from the working directory for a `config/` folder, and loads, in order, `config/.env.secrets`, `config/.env`, a legacy `./.env` if present, then the LLM
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

A demo's Setup has its own command.

`.env.example`, `.env.secrets.example` and `.env.local` are never loaded.

**Debugging in Rider or with `dotnet run`:** every host has a `Properties/launchSettings.json` (Rider lists each
profile as a run configuration) that sets what compose sets for its container, as paths relative to the project folder
(`../../../demos/{demo}/logs`), which is the working directory in Rider and under `dotnet run` alike: `CONFIG_DIRECTORY`,
`WORKSPACE_ROOT`, `SKILLS_DIRECTORY`, `LOG_DIRECTORY`, `PRICES_FILE`, `RATES_FILE`. The ports sit beside the containers',
so both can run at once. Secrets still come from `config/.env.secrets`.

## Settings

### `config/.env` (non-secret)

| Setting | Meaning |
|---|---|
| `LLM_PROFILE` | Which LLM profile to load: `anthropic` (`config/.env.anthropic`) or `ollama` (`config/.env.ollama`) |
| `LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT` | Model and where it runs; normally set by the profile, and a line here overrides it ([MODELS.md](MODELS.md)) |
| `LLM_THINKING` | Visible reasoning: `off` / `low` / `medium` / `high` (default `medium` for Anthropic, `off` otherwise) |
| `LLM_CACHE`, `LLM_CACHE_TTL` | Prompt caching on Anthropic: `on` (default) / `off`, and the cached prefix's lifetime, `5m` (default) or `1h`, chosen with `usage-probe -- --cache-gaps` ([MODELS.md](MODELS.md)) |
| `LLM_SUPPORTS_TOOLS` | Force tool calling on or off (normally inferred from the model) |
| `AGENT_PEERS` | Other louis-agent APIs this one may ask agents on (`AskAgent`, the `agents` toolset; the addresses an identity may ask are in its code, `Calls`): `name=url` pairs, comma separated. Empty by default for the code-review api; Puls's compose sets it to itself (`puls=http://puls:8080`): one container, its specialists on the same instance (CL19). With more than one, this instance is `self`, and an address names another server before a colon (`partner:shop`, F127.3.2). A peer is called with its own key from `AGENT_PEER_KEYS`, else this deployment's `AGENT_API_KEY` |
| `AGENT_PEER_KEYS` | A secret, in `.env.secrets`: each other server's own key, `name=key` pairs by commas, the names those of `AGENT_PEERS` (F127.3.2). A server with none is sent `AGENT_API_KEY` |
| `AGENT_MAX_DEPTH` | The deepest an agent may be asked (F127.5), 0 being the agent a person talks to. Any agent but a minion may ask the ones its identity's `Calls` allow, never one already in its chain of asks. Default `4` |
| `ROLES_ENABLED` | The roles this instance may use (F29), comma separated (`puls/voice-af,puls/voice-en,puls-cook,puls-minion-web-price-lookup,puls-minion-find-recipe`); another name is refused as an unknown one, and `GET /v1/roles` lists only these. Empty: every built-in one (`src/engine/louis-agent.core/prompts/`). `PERSONALITIES_ENABLED`, its name before CL14, is read when it's unset, with a warning. Puls's compose sets it, so only a role that has passed its check is reachable |
| `MINION_LLMS` | The model a minion runs on (F30): `role=profile` entries, comma separated, `*` for every role not named, `instance` for this instance's own model, `>` for a cascade cheapest first (`*=ollama-puls>instance`). Every profile must be in `LLM_SESSION_PROFILES` (`instance` needs none), else start-up stops; unset: no `minions` toolset. Puls's compose leaves it unset, since the cook uses no minion ([ADR 0081](../../demos/puls/docs/adr/0081-the-cook-searches-for-itself.md)); a minion always runs a local model |
| `RECIPES_MONGO_URL` | The MongoDB the recipe library keeps each new read in, for good (ADR 0082); a secret. Unset: nothing is kept and no feed is published |
| `RECIPES_MONGO_DATABASE` | The database on that server. Default `recipe_library` |
| `RECIPE_LIBRARY_URL` | The API whose open feed the cook reads the library from, this instance's own in Puls's compose. Unset: the cook reads no library |
| `RECIPE_LIBRARY_IN_EVALS` | `true` lets an eval's trials use the library, for a run that measures it. Unset: they leave it alone, so one run doesn't change the next |
| `LLM_SESSION_PROFILES` | The LLM profiles a session may name besides the instance's own model (F30), comma separated (`ollama-puls`); each is read from `config/.env.{name}` (in a container, the mounted workspace's `config/`), and only its API key settings come from the environment. A session that names another is refused; a listed profile without its file stops start-up. Empty: every session runs on the instance's model |
| `SUB_SESSION_PROMPT_BUDGET` | The most tokens a sub-session's prompt should reach (FX14): when the next request is expected to pass it, the specialist answers from what it has instead of calling more tools. Default `80000`, under Claude Haiku 5.5's long-prompt price step at 100k; `0` turns it off |
| `ACP_MOUNT_MAPPINGS` | `HOST_PATH=/container/path` pairs, so Rider's Windows paths (attachments, open file) resolve in the container |
| `RIDER_MCP_ENDPOINT`, `RIDER_MCP_AUTO_DISCOVER` | Rider MCP discovery, read by the ACP server (CL57; it only logs Rider's tools for now) |
| `MCP_TRANSPORT`, `MCP_PORT` | MCP server transport: `stdio` (default) or `http`; host port in HTTP mode (default `5090`, bound to `127.0.0.1`) |
| `AGENT_AUTH` | `required` (default) or `off` (FX18): whether the API, and the MCP server over HTTP, may start without a key. `off` is for a development machine whose ports stay on `127.0.0.1`; a key that is set is checked either way. Never set in a compose file |
| `WEB_SEARCH_PROVIDER` | `tavily`, `google`, `searxng` or `duckduckgo` (default: Tavily when `TAVILY_API_KEY` is set, else Google when its key and engine id are, else SearXNG when `SEARXNG_URL` is, else DuckDuckGo). SearXNG is for development and benchmarks only (ADR 0023) |
| `PAGE_RENDERER` | `chrome` or `tavily`, for `ReadRenderedPage` (default: Chrome when `BROWSER_URL` is set, else Tavily when its key is) |
| `BROWSER_URL` | The headless Chrome we run (F32): `http://puls-browser:9222` in the Puls stack (set there); from the host, for its live tests, `http://127.0.0.1:9222` |
| `BROWSER_USER_AGENT` | What that browser tells every site it reads for a user (FX21): the Puls stack and the Azure app set `Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/demos/puls/docs/SITE_OWNERS.md; reads a page on a user's behalf)`, who reads and why, with a public page to learn more. Unset, Chrome's own. Never a person's name or place |
| `DEVOPS_ORGANIZATION`, `DEVOPS_PROJECT` | Azure DevOps organisation (`dev.azure.com/<organisation>`) and project for the DevOps tools |
| `DEVOPS_TEAM` | Optional team: sprints are looked up under it, and an empty sprint means its current sprint |
| `USAGE_LEDGER` | `on` (default) records every model request's tokens and cost to `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`; `off` disables it, and with it every usage line (CLI, web app, Rider, the API's `usage` fields, the comms log's costs): `/usage` then says usage isn't tracked. No `LOG_DIRECTORY` has the same effect |
| `PRICES_FILE` | The price table for those costs (default `config/prices.json`, found like `config/.env`; compose sets it for the containers). Without one, costs are recorded as unknown (`null`); a malformed one stops the host |
| `HOST_CULTURE` | The PC's Windows culture (e.g. `en-ZA`): costs are shown in its region's currency (ZAR, `R0.11`). Containers can't see the PC's region, so write it once with `dotnet run --project src/tools/louis-agent.usage-probe -- --host-culture`. Outside a container, the process's own culture is used when it's unset |
| `DISPLAY_CURRENCY` | Show costs in this currency instead (an ISO code, e.g. `EUR`); the web app can also pick one per browser |
| `RATES_URL` | Where exchange rates come from (default `https://api.frankfurter.dev/v1/latest?base=USD`, the ECB's reference rates); `off` uses `RATES_FILE` only. If it can't be reached, or answers with rates from another currency, the file is used. The demo's orchestrator reads demo-api's `GET /v1/rates` |
| `RATES_FORMAT` | The format of `RATES_URL`'s reply, turned into ours by its mapper: `frankfurter` (default) or `rates-json` (another louis-agent's `GET /v1/rates`) |
| `RATES_FILE` | The fallback rates (default `config/rates.json`; compose sets it). Costs are only converted for display: the ledger always keeps what was charged |
| `PRICES_URL` | Fetch the price table over HTTP instead, e.g. louis-agent.api's public `GET /v1/prices` (the demo's orchestrator uses `http://demo-api:8080/v1/prices`). If it can't be reached at start-up, the host warns and uses the file above |
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
| `AGENT_API_KEY` | The HTTP API's key, required (FX18): every call but `/health`, `/v1/prices`, `/v1/rates` and `/v1/rates/display` sends it as `x-api-key` or `Authorization: Bearer`. Without it the API refuses to start, unless `AGENT_AUTH=off` |
| `MCP_API_KEY` | Key for the MCP server in HTTP mode, same header rules; falls back to `AGENT_API_KEY`. With neither set, HTTP mode refuses to start, unless `AGENT_AUTH=off` |
| `GIT_AUTHOR_NAME`, `GIT_AUTHOR_EMAIL`, `GIT_COMMITTER_NAME`, `GIT_COMMITTER_EMAIL` | Identity for commits made from the web app's Changes view |
| `PAYMO_API_KEY` | Enables the Paymo tools |
| `DEVOPS_API_KEY` | Azure DevOps personal access token; enables the DevOps tools (with the settings below) |
| `ADMIN_API_KEY` | The admin portal's key (`/v1/admin`, Puls's *Admin* menu); falls back to `AGENT_API_KEY`; with neither the portal is closed |
| `OPEN_IDENTITIES` | The guide's door (F109.3, ADR 0099; a list since F127.2): the identities, by commas, a visitor may open a session as with no key and no sign-in, with that identity's own toolsets only, under one daily cap across them all; unset, the door is shut |
| `OPEN_SESSIONS_PER_DAY` | How many such sessions one address may open a day; 0 or unset shuts the door; past it the API answers 429 |
| `TAVILY_API_KEY` | Tavily web search (1,000 free credits a month; the default once set, and the sold endpoint's search) and, with `PAGE_RENDERER=tavily` or no `BROWSER_URL`, `ReadRenderedPage` through Tavily Extract (two credits per five pages) |
| `GOOGLE_SEARCH_API_KEY`, `GOOGLE_SEARCH_ENGINE_ID` | Google web search (discontinued by Google on 2027-01-01; otherwise DuckDuckGo) |
| `RIDER_MCP_PROJECT_PATH` | Host path of the project, sent to Rider's MCP server |

After changing a secret, **recreate** the container that uses it with `up -d`; `docker compose restart` keeps the
container's old environment, so a new key never reaches it that way (found with `TAVILY_API_KEY`, 2026-10-15):

A demo's Setup has the command.

A changed file under a demo's `skills/` needs only a restart: the containers mount the folder and read it when a chat opens.


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

**Wrong settings in Docker** — run compose with `--env-file demos/{demo}/config/.env`, and remember that `environment:` in
the demo's compose file wins over the env files. `docker compose ... config` shows the resolved values.

**"must include the anthropic-workspace-id header"** — set `ANTHROPIC_WORKSPACE_ID` in `config/.env.secrets`.

---
[Docs index](README.md) · Next: [Models](MODELS.md)
