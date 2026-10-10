# Models

The model is configuration only, no code changes. The LLM settings (`LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT`,
`LLM_THINKING`) live in a **profile** file, and `LLM_PROFILE` in `config/.env` picks one:

| Profile | File | Model |
|---|---|---|
| `anthropic` (default) | `config/.env.anthropic` | Claude Haiku 4.5; needs `ANTHROPIC_API_KEY` in `config/.env.secrets` |
| `ollama` | `config/.env.ollama` | `llama3.1` on a local Ollama, free |

```bash
# config/.env
LLM_PROFILE=ollama

# or for one run, without editing .env
LLM_PROFILE=ollama dotnet run --project src/louis-agent.cli
```

The profiles are tracked in git (they hold no secrets). Precedence, highest first: the shell, `config/.env.secrets`,
`config/.env`, the profile; so a `LLM_*` line in `config/.env` pins that one setting over the profile. A new profile
is a new `config/.env.{name}` file (lowercase letters, digits and dashes).

| Provider | `LLM_PROVIDER` | Needs |
|---|---|---|
| Anthropic Claude (default) | `anthropic` | `ANTHROPIC_API_KEY` in `config/.env.secrets` |
| Ollama (local) | `ollama` | `LLM_ENDPOINT` (the profile uses `http://host.docker.internal:11434`); the model pulled |
| OpenAI-compatible (LM Studio, vLLM, …) | `openai-compatible` | `LLM_ENDPOINT` (e.g. `http://localhost:8000/v1`), `LLM_API_KEY` if the server wants one |

If `LLM_PROVIDER` is unset it is inferred from the model: `claude*` → Anthropic, anything else → Ollama.

## Claude (recommended)

```bash
LLM_PROVIDER=anthropic
LLM_MODEL=claude-haiku-5-5
```

Claude Haiku 5.5 is the default: fast, the cheapest current Claude model ($0.10 / $0.50 per million input / output
tokens for prompts up to 100,000 tokens, 5× that above), and good at tool calling, which is what this agent does most.
It counts the same text as more tokens than Haiku 4.5 (+52% on the usage probe), and is still about 7.5× cheaper for
the same work. `claude-haiku-4-5-20251001` still works. Any current Claude model id works in `LLM_MODEL` (Sonnet or Opus
for harder work, at a higher price per token); check Anthropic's documentation (https://docs.claude.com) for ids and
pricing, and run `tools/louis-agent.usage-probe` before relying on a new model's usage numbers. A model also needs its
prices in `config/prices.json` (matched by longest prefix); until then the usage ledger records its cost as unknown.

**Thinking.** `LLM_THINKING` (`off` / `low` / `medium` / `high`, default `medium` for Anthropic) sets how much the model
reasons; the reasoning is streamed to Rider, the web app and the CLI. Thinking uses output tokens and adds a short
pause before the answer; use `low` for speed. Haiku 5.5 and other current models use adaptive thinking: they decide per
request whether to think at all, and `off` leaves the model's own default rather than turning thinking off. Claude
models before 4.6 (including Haiku 4.5) get a fixed thinking budget (2,048 / 4,096 / 8,192 tokens) instead.

**History must stay append-only on Haiku 5.5.** It rejects a request whose system prompt, tools or earlier messages
changed while thinking blocks are sent back (enforced for accounts created on or after 2026-08-31). Reloading skills or
approving an agent-built tool in the middle of a chat does that today: start a new chat afterwards (see the known
limitations).

**Output limit.** Each reply is capped at 16,000 tokens (thinking included). Large files are written in chunks; a reply
that hits the limit mid tool call is handled (the call is not run and the model continues).

**Workspace-scoped keys.** If the API returns "must include the anthropic-workspace-id header", set
`ANTHROPIC_WORKSPACE_ID` in `config/.env.secrets`.

## Ollama (local)

Run models on your own machine; nothing leaves it. Set `LLM_PROFILE=ollama` (`config/.env.ollama`). Compose has an
`ollama` service behind a compose profile, which pulls the Ollama profile's `LLM_MODEL` on first start:

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env --profile ollama up -d ollama
```

The profile's endpoint, `http://host.docker.internal:11434`, reaches the published port 11434 both from the host
(Docker Desktop adds the name to the hosts file) and from the agent containers, so `dotnet run` and Docker use the same
setting. With Ollama installed natively and no Docker Desktop, set `LLM_ENDPOINT=http://localhost:11434`.

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

**Today the prompt doesn't fit.** Ollama's context window is 2,048 tokens by default and the agent sends ~26,000+
(system prompt and tools); Ollama keeps the start and drops the rest without an error. Local models therefore run on
a fragment of their instructions. [F13](features/F13-local-models.md) fits every request into the window.

## OpenAI-compatible servers

```bash
LLM_PROVIDER=openai-compatible
LLM_MODEL=your-model-name
LLM_ENDPOINT=http://localhost:8000/v1
LLM_API_KEY=...            # if the server needs one
```

Works with anything that speaks the OpenAI Chat Completions API. Tool calling depends on the server and model.

## Switching models

1. Set `LLM_PROFILE` in `config/.env` (or edit the profile file; `config/.env.secrets` for a new key).
2. Restart what you use:
   - Rider: start a **New Chat** (each chat starts a fresh container with the new settings).
   - Web app / API: `docker compose -f docker/docker-compose.yml --env-file config/.env up -d api`
   - CLI: just run it again.
3. Check the startup line in the logs (`logs/agent-*.log`):
   `[INFO] LLM: anthropic:claude-haiku-5-5 (...); tools=on; thinking=medium`

## Troubleshooting

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required"** — add the key to `config/.env.secrets`.
- **"adaptive thinking is not supported on this model"** — shouldn't happen for known models; set `LLM_THINKING=off`
  and report the model id.
- **Ollama "model not found"** — pull it: `docker exec louis_ollama ollama pull <model>` (or restart the ollama
  service, which pulls `LLM_MODEL`).
- **Ollama connection refused** — the service only runs with `--profile ollama`; check
  `curl http://host.docker.internal:11434/api/tags` from the host. Inside Docker `localhost` is the container itself.
- **`[WARN] LLM_PROFILE 'x' has no config/.env.x`** — a typo in `LLM_PROFILE`, or the profile file is missing; the
  settings fall back to `config/.env` and the defaults (Claude Haiku 4.5).
- **The agent talks about tools instead of using them** — the model can't call tools well; switch model or check
  `tools=on` in the startup line.

---
[Docs index](README.md) · Previous: [Setup](SETUP.md) · Next: [Paymo prompts](PAYMO-PROMPTS.md)
