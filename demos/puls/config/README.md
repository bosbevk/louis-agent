# config/: what Puls reads at run time

> **Reference.** What each file here is and which are yours to fill. **Elsewhere:** every setting and how the files
> are loaded in [Setup](../../../docs/SETUP.md); the model in [Models](../../../docs/MODELS.md).

A host is told this folder with `CONFIG_DIRECTORY`; with nothing set it searches upward for a folder named `config`, so
the env files stay at its root.

| File | In git | What it is |
|---|---|---|
| `.env.example` → `.env` | template / yours | Your non-secret settings; every setting explained in the template |
| `.env.secrets.example` → `.env.secrets` | template / yours | Your API keys; also `TS_AUTHKEY`, used only to deploy to Azure |
| `.env.mongo.example` → `.env.mongo` | template / yours | The benchmark store's root user, read only by `puls-mongo` |
| `.env.searxng.example` → `.env.searxng` | template / yours | The SearXNG secret, read only by `puls-searxng` |
| `.env.anthropic`, `.env.ollama-puls` | tracked | LLM profiles: `LLM_PROFILE` picks the instance's, `LLM_SESSION_PROFILES` the ones a session may name |
| `prices.json`, `rates.json` | tracked | The model price table (dated by `as_of`) and the fallback exchange rates; the coding agent keeps a copy, held equal by a test |

```bash
cp demos/puls/config/.env.example demos/puls/config/.env
cp demos/puls/config/.env.secrets.example demos/puls/config/.env.secrets     # then set ANTHROPIC_API_KEY and AGENT_API_KEY
```

The stack that runs here is in [docker/](../docker/README.md); the cloud in [deploy/](../../../deploy/README.md).
