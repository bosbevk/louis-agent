# config/: what the coding agent reads at run time

> **Reference.** What each file here is and which are yours to fill. **Elsewhere:** every setting and how the files
> are loaded in [Setup](../docs/SETUP.md); the model in [Models](../../../src/docs/MODELS.md).

A host is told this folder with `CONFIG_DIRECTORY`; with nothing set it searches upward for a folder named `config`, so
the env files stay at its root.

| File | In git | What it is |
|---|---|---|
| `.env.example` → `.env` | template / yours | Your non-secret settings; every setting explained in the template |
| `.env.secrets.example` → `.env.secrets` | template / yours | Your API keys |
| `.env.anthropic`, `.env.ollama`, `.env.ollama-triage` | tracked | LLM profiles: `LLM_PROFILE` picks the instance's, `LLM_SESSION_PROFILES` the ones a session may name |
| `prices.json`, `rates.json` | tracked | The model price table (dated by `as_of`) and the fallback exchange rates; Puls keeps a copy, held equal by a test |
| `rider/` | tracked | Templates for Rider's agent-server (ACP) and MCP-client entries; replace `C:\path\to` |

```bash
cp demos/louis-agent/config/.env.example demos/louis-agent/config/.env
cp demos/louis-agent/config/.env.secrets.example demos/louis-agent/config/.env.secrets     # then set ANTHROPIC_API_KEY and AGENT_API_KEY
```

The stacks that run here are in [docker/](../docker/README.md).
