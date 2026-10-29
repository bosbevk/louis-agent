# Common errors

> **How-to.** Fix the errors a developer meets most, by their message. **Elsewhere:** every fix's symptom and cause in
> the [fixes table](../fixes/README.md); the known limitations in the [TODO](../../TODO.md#known-limitations).

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'"** — set it in `config/.env.secrets`.
- **"AGENT_API_KEY is required: set it in config/.env.secrets, or set AGENT_AUTH=off …"** (or `MCP_API_KEY (or
  AGENT_API_KEY)` from the MCP server over HTTP) — the host fails closed ([FX18](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX18-auth-fails-closed.md)):
  set the key, or `AGENT_AUTH=off` in `config/.env` on a machine whose ports stay on `127.0.0.1`. Then recreate the
  containers (`up -d`): `restart` keeps the old environment.
- **"prompt is too long"** — something put a huge result into the history; check `logs/oversized-tool-results.jsonl`.
- **"adaptive thinking is not supported on this model"** — a pre-4.6 Claude model not listed in
  `LlmClientFactory.ThinkingBudgetModelPrefixes`; add its prefix.
- **Skill not found** — the session doesn't have the `skills` toolset, the file isn't in `Skills/agent/functions/`, or
  the name differs (case-sensitive).
- **bash not found on Windows** — install Git for Windows or set `BASH_PATH` to `bash.exe`; WSL's bash is never used.
- **400 "Invalid `signature` in `thinking` block … bound to a different conversation"** — the system prompt or tool list
  changed under earlier thinking ([known limitation 9](../../TODO.md#known-limitations)); start a new chat. See
  [FX4](../fixes/FX04-append-only-history.md). This one matters whenever you test a change by hand: on Claude
  Haiku 5.5 (the default), changing the tools or skills during a chat (`CreateTool`, `/approve`, `/reject`, a skill
  reload) breaks that chat, so start a new chat afterwards.
- **"Price table …"** stopping start-up (not valid, empty, a negative price, or a bad `long_prompt.above_tokens`) — fix
  `config/prices.json` (or the `PRICES_FILE` / `PRICES_URL` table): every model needs all five prices, none negative.
  **"[WARN] No price table"** means costs are recorded as unknown.
- **"Model '…' isn't pulled into Ollama"** (usage probe) or a 404 from Ollama — `docker exec louis_ollama ollama pull <model>`.

---
[Guides](README.md) · [Docs index](../README.md)
