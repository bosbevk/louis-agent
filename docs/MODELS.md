# Models

The model is configuration only: set `LLM_PROVIDER` and `LLM_MODEL` in `config/.env` (and `LLM_ENDPOINT` /
keys where needed), then restart the host. No code changes.

| Provider | `LLM_PROVIDER` | Needs |
|---|---|---|
| Anthropic Claude (default) | `anthropic` | `ANTHROPIC_API_KEY` in `config/.env.secrets` |
| Ollama (local) | `ollama` | `LLM_ENDPOINT` (`http://ollama:11434` in Docker); the model pulled |
| OpenAI-compatible (LM Studio, vLLM, …) | `openai-compatible` | `LLM_ENDPOINT` (e.g. `http://localhost:8000/v1`), `LLM_API_KEY` if the server wants one |

If `LLM_PROVIDER` is unset it is inferred from the model: `claude*` → Anthropic, anything else → Ollama.

## Claude (recommended)

```bash
LLM_PROVIDER=anthropic
LLM_MODEL=claude-haiku-4-5-20251001
```

Claude Haiku 4.5 is the default: fast, cheap, and reliable at tool calling, which is what this agent does most. Any
current Claude model id works in `LLM_MODEL` (Sonnet or Opus for harder work, at a higher price per token); check
Anthropic's documentation (https://docs.claude.com) for ids and pricing.

**Thinking.** `LLM_THINKING` (`off` / `low` / `medium` / `high`, default `medium` for Anthropic) turns on visible
reasoning, streamed to Rider, the web app and the CLI. Thinking uses output tokens and adds a short pause before the
answer; use `low` or `off` for speed. Claude models before 4.6 (including Haiku 4.5) get a fixed thinking budget
(2,048 / 4,096 / 8,192 tokens); newer ones use adaptive thinking.

**Output limit.** Each reply is capped at 16,000 tokens (thinking included). Large files are written in chunks; a reply
that hits the limit mid tool call is handled (the call is not run and the model continues).

**Workspace-scoped keys.** If the API returns "must include the anthropic-workspace-id header", set
`ANTHROPIC_WORKSPACE_ID` in `config/.env.secrets`.

## Ollama (local)

Run models on your own machine; nothing leaves it. Compose has an `ollama` service behind a profile, which pulls
`LLM_MODEL` on first start:

```bash
# config/.env
LLM_PROVIDER=ollama
LLM_MODEL=llama3.1
LLM_ENDPOINT=http://ollama:11434

docker compose -f docker/docker-compose.yml --env-file config/.env --profile ollama up -d ollama
```

Outside Docker use `LLM_ENDPOINT=http://localhost:11434` (port 11434 is published).

**Tool calling decides how useful a local model is.** The agent works by calling tools, so pick a model whose
Ollama page lists tool support (for example `llama3.1`, `qwen3`, `mistral`). Models known to reject tools, or to answer
with tool-call JSON as plain text, run **without tools** automatically — chat and analysis only:

| Prefix | Why |
|---|---|
| `llama2`, `codellama`, `deepseek-r1`, `deepseek-coder` | Reject tool definitions |
| `qwen2` (including `qwen2.5-coder`) | Writes tool calls as text instead of calling them |

Override the guess with `LLM_SUPPORTS_TOOLS=true` or `false`. Without tools the agent says plainly that it can't act,
rather than pretending.

Local models are slower than Claude and need memory roughly in line with their size (a 7–8B model fits in about
8 GB of VRAM; larger ones need more). The first request after start is slow while the model loads.
`LLM_THINKING` is off for Ollama unless you set it.

## OpenAI-compatible servers

```bash
LLM_PROVIDER=openai-compatible
LLM_MODEL=your-model-name
LLM_ENDPOINT=http://localhost:8000/v1
LLM_API_KEY=...            # if the server needs one
```

Works with anything that speaks the OpenAI Chat Completions API. Tool calling depends on the server and model.

## Switching models

1. Edit `config/.env` (and `config/.env.secrets` for a new key).
2. Restart what you use:
   - Rider: start a **New Chat** (each chat starts a fresh container with the new settings).
   - Web app / API: `docker compose -f docker/docker-compose.yml --env-file config/.env up -d api`
   - CLI: just run it again.
3. Check the startup line in the logs (`logs/agent-*.log`):
   `[INFO] LLM: anthropic:claude-haiku-4-5-20251001 (...); tools=on; thinking=medium`

## Troubleshooting

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required"** — add the key to `config/.env.secrets`.
- **"adaptive thinking is not supported on this model"** — shouldn't happen for known models; set `LLM_THINKING=off`
  and report the model id.
- **Ollama "model not found"** — pull it: `docker exec louis_ollama ollama pull <model>` (or restart the ollama
  service, which pulls `LLM_MODEL`).
- **Ollama connection refused** — inside Docker the endpoint is `http://ollama:11434`, not `localhost`, and the
  service only runs with `--profile ollama`.
- **The agent talks about tools instead of using them** — the model can't call tools well; switch model or check
  `tools=on` in the startup line.

---
[Docs index](README.md) · Previous: [Setup](SETUP.md) · Next: [Paymo prompts](PAYMO-PROMPTS.md)
