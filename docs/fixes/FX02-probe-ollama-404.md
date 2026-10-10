# FX2 · The usage probe says which model to pull on an Ollama 404

> **Status:** done (2026-10-10) · **Area:** tools, `louis-agent.usage-probe` · **Commit:** `a4e9f7f`

## Symptom

Running the usage probe on the Ollama profile ended in a stack trace:

```text
provider=ollama model=llama3.1 …
Unhandled exception. System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
   at OllamaSharp.OllamaApiClient.EnsureSuccessStatusCodeAsync(…)
```

## Cause

The Ollama profile asks for `llama3.1`, but the `louis_ollama` container had only pulled `qwen2.5:0.5b`. Ollama answers
**404** for a model it hasn't pulled (reproduced with a direct `POST /api/chat`), and the trace doesn't say so.

## Fix

The probe catches an `HttpRequestException` with status 404 when the provider is Ollama, and prints the model, the
endpoint and the pull command for the compose service and for native Ollama, then exits with 1:

```text
Model 'llama3.1' isn't pulled into Ollama at http://host.docker.internal:11434. Pull it with:
  docker exec louis_ollama ollama pull llama3.1    (the compose ollama service)
  ollama pull llama3.1                             (Ollama installed natively)
```

Any other error still shows the full trace.

## Tests

None automated (the probe calls real providers). Checked by hand with a model name that doesn't exist.

## Confirmed

After `docker exec louis_ollama ollama pull llama3.1`, the probe ran cleanly on the Ollama profile.

## Lesson

An error a person will meet should say what to do next, not only what failed (skill map #14, reliability).

---
[Fixes](README.md) · Previous: [FX1](FX01-round-limit-tools.md) · Next: [FX3](FX03-ollama-container.md)