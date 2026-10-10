# FX3 · The Ollama container starts and pulls the profile's model

> **Status:** done (2026-10-10) · **Area:** Docker, the `ollama` service · **Commit:** `882fdde` (with the LLM profiles)

## Symptom

Two failures, one after the other, when starting the compose `ollama` service to measure Ollama's usage reporting:

1. The container exited at once: `exec /entrypoint.sh: no such file or directory`.
2. Started with `LLM_MODEL` from `config/.env`, the entrypoint would try to pull `claude-haiku-4-5-20251001` from
   Ollama, fail, and stop the container (`set -e`).

## Cause

1. **Line endings.** `services/ollama-entrypoint.sh` had CRLF line endings in the working copy (`file` showed "with CRLF
   line terminators"), so Linux looked for an interpreter called `/bin/bash\r`. The repository's `.gitattributes`
   already says `*.sh text eol=lf`, and git's own copy was LF: the checkout simply predated that rule.
2. **The wrong model variable.** The service set `OLLAMA_MODEL=${LLM_MODEL}`, and `LLM_MODEL` in `config/.env` was the
   Anthropic model, since one `.env` held one provider's settings.

## Fix

1. Check the file out again (`rm` and `git checkout --`), which writes it with LF per `.gitattributes`. No change in git.
   For other `.sh` files with the same problem: `git add --renormalize .` and a fresh checkout.
2. With the LLM profiles (`config/.env.anthropic`, `config/.env.ollama`), the `ollama` service reads `config/.env.ollama`
   as its `env_file`, and the entrypoint pulls `OLLAMA_MODEL` if set, else that profile's `LLM_MODEL`. The Anthropic
   model can no longer reach it.

## Tests

None automated. `docker compose config` shows the service's `LLM_MODEL` from the Ollama profile.

## Confirmed

The container started, pulled `qwen2.5:0.5b` and later `llama3.1`, and the usage probe ran against it.

## Lesson

A shell script run inside a Linux container must have LF endings however it is checked out on Windows; `.gitattributes`
protects new checkouts, not old ones (skill map #18, testing and shipping).

---
[Fixes](README.md) · Previous: [FX2](FX02-probe-ollama-404.md) · Next: [FX4](FX04-append-only-history.md)
