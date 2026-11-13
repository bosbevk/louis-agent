# Setup: the coding agent

> **Reference.** The coding agent's own files, first run and settings. **Elsewhere:** how a host loads settings, and every
> setting the code reads, in the framework's [Configuration](../../../src/docs/SETUP.md); what each file here is in
> [config/](../config/README.md); a first run in the [Quick start](QUICK_START_AGENT.md).

The coding agent keeps its settings in `demos/louis-agent/config/`. Its stack sets `CONFIG_DIRECTORY` to it, and so do the
hosts' launch profiles.

## Files

| File | In git | Purpose |
|---|---|---|
| `.env` | ❌ git-ignored | Your non-secret settings: model, paths |
| `.env.example` | ✅ tracked | Template for `.env`, with explanations |
| `.env.secrets` | ❌ git-ignored | Your API keys and other secrets |
| `.env.secrets.example` | ✅ tracked | Template listing every secret |
| `.env.anthropic`, `.env.ollama`, `.env.ollama-triage` | ✅ tracked | LLM profiles: the model settings `LLM_PROFILE` picks |
| `prices.json`, `rates.json` | ✅ tracked | Model prices and fallback exchange rates; Puls keeps a copy, held equal by a test |
| `rider/rider-acp.example.json` | ✅ tracked | Rider agent-server entry for the ACP server (replace `C:\path\to`) |
| `rider/rider-mcp-servers.example.json` | ✅ tracked | MCP client entry for the MCP server (replace `C:\path\to`) |

## First run

```bash
cp demos/louis-agent/config/.env.example demos/louis-agent/config/.env
cp demos/louis-agent/config/.env.secrets.example demos/louis-agent/config/.env.secrets     # at least ANTHROPIC_API_KEY
```

In `.env`, point `REPOSITORIES_PATH` and `ACP_MOUNT_MAPPINGS` at the folder that holds your repositories (for example
`C:\Users\you\RiderProjects` and `C:\Users\you\RiderProjects=/repositories`), and pick your LLM profile. Then:

```bash
docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api
```

After changing a secret, recreate the container with `up -d`; `restart` keeps the old environment.

What the stack keeps stays in this demo, git-ignored: `demos/louis-agent/logs/` (run logs and the usage ledger) and
`demos/louis-agent/data/` (what an orchestrator run leaves).

## Debugging in Rider

The API's `api` profile (port 5081), the CLI's, the ACP server's and `mcp-http` (port 5091) set what this stack sets. Which web
app the API serves is the build's choice.

## The coding agent's own settings

These are read only by this stack's compose file.

| Setting | Meaning |
|---|---|
| `REPOSITORIES_PATH` | Host folder mounted at `/repositories` in the containers |
| `API_PORT` | Host port for the API and web app (default `5082`, always bound to `127.0.0.1`; the framework's site has 5080) |

The orchestrator reads its own from the environment when it is run by hand ([its doc](projects/louis-agent.orchestrator.md)):

| Setting | Meaning |
|---|---|
| `ORCHESTRATOR_SERVICE_ROOT` | The watched service's repository; required |
| `ORCHESTRATOR_SERVICE`, `ORCHESTRATOR_SERVICE_PROJECT` | The service's name, which picks its runbook folder (default `order-service`), and its project inside the repository (default `src/OrderService`) |
| `LOUIS_AGENT_URL`, `LOUIS_AGENT_API_KEY` | The coding agent's API the fixes are asked of (default `http://127.0.0.1:5081`) and its key, a secret (else `AGENT_API_KEY`) |
| `ORCHESTRATOR_STATE_DIRECTORY`, `ORCHESTRATOR_COMMS_LOG` | Where it keeps what it has handled (default `logs/orchestrator/{service}`), and another file for the conversation log |
| `ORCHESTRATOR_POLL_SECONDS` | How often it reads the error log (default `15`) |

---
[Docs index](README.md) · Related: [Configuration](../../../src/docs/SETUP.md) · [Quick start](QUICK_START_AGENT.md)
