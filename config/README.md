# config/: what the app reads at run time

> **Reference.** What each file here is and which are yours to fill. **Elsewhere:** every setting and how the files
> are loaded in [Setup](../docs/SETUP.md); the model in [Models](../docs/MODELS.md).

The code finds this folder by its name, searching upward from where it runs, so its env files stay at its root.

| File | In git | What it is |
|---|---|---|
| `.env.example` → `.env` | template / yours | Your non-secret settings; every setting explained in the template |
| `.env.secrets.example` → `.env.secrets` | template / yours | Your API keys; also `TS_AUTHKEY`, used only to deploy to Azure |
| `.env.mongo.example` → `.env.mongo` | template / yours | The benchmark store's root user, read only by `puls-mongo` |
| `.env.searxng.example` → `.env.searxng` | template / yours | The SearXNG secret, read only by `puls-searxng` |
| `.env.anthropic`, `.env.ollama`, `.env.ollama-puls`, `.env.ollama-triage` | tracked | LLM profiles: `LLM_PROFILE` picks the instance's, `LLM_SESSION_PROFILES` the ones a session may name |
| `prices.json`, `rates.json` | tracked | The model price table (dated by `as_of`) and the fallback exchange rates |
| `rider/` | tracked | Templates for Rider's agent-server (ACP) and MCP-client entries; replace `C:\path\to` |

```bash
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets     # then set ANTHROPIC_API_KEY and AGENT_API_KEY
```

The stacks that run here are in [docker/](../docker/README.md); the cloud in [deploy/](../deploy/README.md).
