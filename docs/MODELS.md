# Models

> **How-to.** Choose and switch the model: Claude, Ollama or an OpenAI-compatible server. **Elsewhere:** every other
> setting in [Setup](SETUP.md), why providers are swappable in [Architecture](architecture/05-building-block-view.md#provider-strategies).

The model is configuration only, no code changes. The LLM settings (`LLM_PROVIDER`, `LLM_MODEL`, `LLM_ENDPOINT`,
`LLM_THINKING`) live in a **profile** file, and `LLM_PROFILE` in `config/.env` picks one:

| Profile | File | Model |
|---|---|---|
| `anthropic` (default) | `config/.env.anthropic` | Claude Haiku 5.5; needs `ANTHROPIC_API_KEY` in `config/.env.secrets` |
| `ollama` | `config/.env.ollama` | `llama3.1` on a local Ollama, free |
| `ollama-puls` | `config/.env.ollama-puls` | `qwen3:4b-instruct-2507` on a local Ollama, for one session (below), free |

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

**Prompt caching** (Anthropic, on by default; `LLM_CACHE=off` turns it off). Every request re-sends the tools and
system prompt (~48,000 tokens); with caching the API reads them at a tenth of the input price after the first request,
and each tool round reads the round before. On the demo it cut the input cost by 87%. Two markers: the system prompt,
whose lifetime is `LLM_CACHE_TTL` (`5m` default; `1h` when requests sharing it come 5–60 minutes apart, as in a chat
with pauses), and the conversation, always 5 minutes. To choose the lifetime from your own traffic:
`dotnet run --project tools/louis-agent.usage-probe -- --cache-gaps logs/usage-YYYY-MM.jsonl`. A cache miss raises no
error; the ledger shows `cache_read: 0` (compare with [the F4 smoke run at `pre-cl10`](https://github.com/bosbevk/louis-agent/tree/pre-cl10/samples/smoke-f4-caching)). Ollama and OpenAI-compatible
endpoints get no markers. Haiku 5.5 caches from 512 tokens, Haiku 4.5 from 4,096.

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

**On a GPU,** start `ollama-nvidia` or `ollama-amd` instead (one Ollama at a time: they share port 11434 and the
models volume):

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env --profile ollama-nvidia up -d ollama-nvidia
docker compose -f docker/docker-compose.yml --env-file config/.env --profile ollama-amd up -d ollama-amd
```

- **NVIDIA** reserves the GPU in compose (needs an NVIDIA driver; Docker Desktop's WSL 2 backend passes it through).
- **AMD** uses the `ollama/ollama:rocm` image with `/dev/kfd` and `/dev/dri`: Linux hosts with ROCm only, since Docker
  Desktop on Windows doesn't pass AMD GPUs to containers.
- **Both are sized for a 4 GB card, split between VRAM and RAM:** a 32k-token context (`OLLAMA_CONTEXT_LENGTH=32768`,
  above the agent's ~26k-token prompt), flash attention and an 8-bit KV cache (`OLLAMA_FLASH_ATTENTION=1`,
  `OLLAMA_KV_CACHE_TYPE=q8_0`), which halve the context's memory. llama3.1 (8B, ~4.9 GB) and its context don't fit in
  4 GB, so Ollama runs part of the model on the CPU from system RAM: slower than all-GPU, but the prompt isn't cut.
  `docker exec louis_ollama_nvidia ollama ps` shows the split in its PROCESSOR column.

### Native Ollama on Windows (AMD)

Docker Desktop doesn't pass an AMD card to a container, so Ollama in Docker on Windows runs on the CPU: minutes just to
read an agent's prompt. Install Ollama for Windows instead (it uses the card through ROCm); the containers reach it at
the same `http://host.docker.internal:11434`, so only the host changes. Set its memory once, as user variables, then
quit Ollama from the tray and start it again (Ollama reads them only at start):

```powershell
[Environment]::SetEnvironmentVariable('OLLAMA_CONTEXT_LENGTH', '32768', 'User')
[Environment]::SetEnvironmentVariable('OLLAMA_FLASH_ATTENTION', '1', 'User')
[Environment]::SetEnvironmentVariable('OLLAMA_KV_CACHE_TYPE', 'q8_0', 'User')
ollama pull qwen3:4b-instruct-2507-q4_K_M
ollama ps    # after a first `ollama run`: PROCESSOR 100% GPU, CONTEXT 32768
```

Measured on an RX 7600 (8 GB), 2026-10-16, `qwen3:4b-instruct-2507-q4_K_M`:

| Settings | Size in `ollama ps` | PROCESSOR |
|---|---|---|
| Ollama's default (4,096 tokens) | 3.2 GB | 100% GPU |
| 32,768 tokens, full-precision cache | 7.7 GB | 7%/93% CPU/GPU |
| 32,768 tokens, flash attention, `q8_0` cache | 5.3 GB (cache 2.4 GB) | 100% GPU |

Without the last two settings the context's cache alone is about 4.5 GB, and part of the model moves to the CPU.
Loaded, it writes about 55 tokens a second. A terminal opened before the install doesn't find `ollama`: open a new one.

### A local model for one session (F30)

An instance can run on Claude and still give one session a local model. `LLM_SESSION_PROFILES` lists the profiles a
session may name (`ollama-puls` on Puls's `puls-agent`); a session opened with `"llm": "ollama-puls"` (a sub-session
through `StartSubSession(…, llm)`) runs on it, every other on `LLM_PROFILE`'s, and `GET /llm-profiles` lists them. The
profile's file gives everything but the API keys, which come from `config/.env.secrets`; nothing else is taken from the
instance's profile, so a local model never gets Claude's `LLM_THINKING`. A listed profile without its file stops
start-up.

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

**Today the prompt doesn't fit.** Ollama's context window was 2,048 tokens on the version measured here (newer Ollama
picks 4k, 32k or 256k by available memory) and the agent sends ~26,000+
(system prompt and tools); Ollama keeps the start and drops the rest without an error. Local models therefore run on
a fragment of their instructions. [F15](features/F15-model-limits.md) sends the window to Ollama (`num_ctx`), so the
default stops mattering, and [F13](features/F13-local-models.md) fits every request into it.

## OpenAI-compatible servers

There is no profile file for these yet, so set them in `config/.env`:

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
  settings fall back to `config/.env` and the defaults (Claude Haiku 5.5).
- **The agent talks about tools instead of using them** — the model can't call tools well; switch model or check
  `tools=on` in the startup line.

---
[Docs index](README.md) · Previous: [Setup](SETUP.md) · Next: [How a turn works](architecture/06-runtime-view.md)
