# Common errors

> **How-to.** Fix the errors you're most likely to meet, by the message you see. **Elsewhere:** every fix's symptom
> and cause in the [fixes table](../fixes/README.md); the known limitations in the [TODO](../../../TODO.md#known-limitations).

![Common errors, in four groups: keys and start-up, the model, skills and bash, prices and the web](diagrams/common-errors.svg)

Find the message you got in the left column. The settings files named here are your demo's, in
`demos/{demo}/config/`.

| You see | What it means | Fix |
|---|---|---|
| "ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'" | The host has no key for the model | Set it in `config/.env.secrets` |
| "AGENT_API_KEY is required: set it in config/.env.secrets, or set AGENT_AUTH=off …", or "MCP_API_KEY (or AGENT_API_KEY)" from the MCP server over HTTP | The host fails closed: with no key, it refuses to serve ([FX18](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX18-auth-fails-closed.md)) | Set the key, or set `AGENT_AUTH=off` in `config/.env` on a machine whose ports stay on `127.0.0.1`. Then recreate the containers with `up -d`, since `restart` keeps the old environment |
| "prompt is too long" | Something put a huge result into the history | Look in `logs/oversized-tool-results.jsonl` for what it was |
| "adaptive thinking is not supported on this model" | A Claude model older than 4.6 that our list of thinking-budget models doesn't name | Add its prefix to `ThinkingBudgetModelPrefixes` in `LouisAgent.Core.Anthropic`'s `ThinkingBudget` |
| Skill not found | `UseSkill` answers with the skills it may use instead | Check that the session has the `skills` toolset, that the skill's folder is in `demos/louis-agent/skills/` (a draft, in `drafts/`, is only its own chat's), that its folder name and its `SKILL.md`'s `name` match exactly, and, for one you dropped in by hand, that `ReloadSkills` has run since |
| bash not found on Windows | No bash was found | Install Git for Windows, or set `BASH_PATH` to `bash.exe`. WSL's bash is never used |
| 400 "Invalid `signature` in `thinking` block … bound to a different conversation" | The system prompt or the tool list changed under earlier thinking. Since [FX4](../fixes/README.md) a chat keeps the ones it started with, and a resumed chat on moved tools strips or drops the stale thinking, so this means something edited a request after the engine built it | Find what changed the request: a middleware or adapter that rewrites the tools or the system prompt |
| "Price table …" stopping start-up: not valid, empty, a negative price, or a bad `long_prompt.above_tokens` | The price table can't be used | Fix `config/prices.json`, or the table `PRICES_FILE` or `PRICES_URL` points at. Every model needs all five prices, and none may be negative |
| "[WARN] No price table" | There is no price table, so costs are recorded as unknown | Set `PRICES_FILE` to price them |
| "…'s robots.txt disallows reading …", "…'s robots.txt couldn't be read …" or "… says no to AI readers …" | The site or the page said no to automated readers, or its `robots.txt` was down. Every fetch and browser visit obeys that ([ADR 0068](../adr/0068-every-fetch-obeys-robots-txt.md)) | Nothing to fix: that page isn't read. A replay that ends in "couldn't be read" is an old recording with no `robots.txt` in it |
| "Model '…' isn't pulled into Ollama" from the usage probe, or a 404 from Ollama | Ollama doesn't have that model | `docker exec louis_ollama ollama pull <model>` |

## The thinking error when you test by hand

The 400 about a thinking block's signature comes when what a chat was sent changes under it: a model that thinks checks
its earlier thinking against the tools and the system prompt it was sent with. So an open chat keeps the tools it
started with. `CreateSkill`, `/approve`, `/reject` and a skill reload change nothing it was sent: a new skill is
reached through `UseSkill`, and an approval reaches the chat as a note on its next message. A chat brought back after
a release whose tools moved has its earlier thinking taken out before it goes on. If you meet the 400 all the same,
start a new chat, and keep the chat's id for the log.

---
[Guides](README.md) · [Docs index](../README.md)
