# F13 · Local models: fit every request into a small context window

> **Status:** planned · **Milestone:** Local · **Depends on:** F1, F5
>
> **Spec:** [Optimisation §C](../specs/RESPONSE_OPTIMISATION.md) (toolsets) · [MODELS.md](../MODELS.md) (Ollama)
>
> **In the TODO:** tick **F13 Local models** under *Now* ([TODO](../../TODO.md)) · **Removes the limitation:** local
> models get a truncated prompt

The first ledger record from a local model (F1 step 6, `llama3.1` on Ollama) showed `input: 2050` for a one-word
question. The agent's system prompt alone is about 26,000 tokens, before the tool definitions. Ollama's default context
window is 2,048 tokens, and Ollama drops whatever doesn't fit **without an error**, so the model has been running on a
fragment of its instructions, which goes a long way to explain poor tool use on local models.

F13 makes the agent work *within* that window instead: every request to a local model is built to fit 2,048 tokens, so
nothing is cut off silently, and memory use stays what a small machine can afford. Raising the window is still possible
(one setting), but the default is to fit.

## User stories

**F13-S1 — Nothing is cut off silently.** As a *user* of a local model, I want the agent to send only what fits the
model's context window, so that the model sees all of what it is sent.
- *Given* `LLM_CONTEXT_TOKENS=2048` (the Ollama profile's default) *when* a turn is sent *then* the estimated request
  (system prompt + tool definitions + history + the output reserve) is ≤ 2,048 tokens, and the ledger's `input` for it
  is ≤ 2,048 minus the output reserve.
- *Given* the same setting *then* Ollama is sent `num_ctx` = 2,048, so the server's window and the agent's budget agree
  whatever the server's default is.
- *Given* a provider reports `input` within 2% of `LLM_CONTEXT_TOKENS` *then* a `[WARN]` says the request was probably
  truncated (the guard for anything the estimate misses).

**F13-S2 — A compact prompt and a few tools.** As a *user*, I want a local model to get short instructions and only the
tools it needs, so that they fit and a small model can follow them.
- *Given* the Ollama profile *then* the system prompt is `Skills/local.md`: who the agent is, how to call a tool, and
  one line per loaded tool; a few hundred tokens, not 26,000.
- *Given* the Ollama profile *then* it loads a small toolset through F5 (`AGENT_TOOLSETS=files,git` to start), and the
  startup log prints the prompt and tool-definition sizes against the budget.
- *Given* the compact prompt plus the tools exceed the budget *then* start-up fails with the sizes and which setting to
  change, instead of running truncated.

**F13-S3 — Long chats still fit.** As a *user*, I want a chat with a local model to keep going after the window is full,
so that it doesn't break or truncate on the fifth message.
- *Given* the history would push the request over budget *then* the oldest tool results are replaced by one-line notes
  first, then the oldest turns are dropped, always keeping the system prompt and the latest user message.
- *Given* history was dropped *then* the turn's stderr log says how many turns were left out.

**F13-S4 — It works.** As a *user*, I want a simple task to succeed on `llama3.1`, so that local mode is useful.
- *Given* "What branch am I on?" and "What's in README.md?" *then* `llama3.1` calls the right tool and answers, and every
  ledger record of those turns has `input` ≤ 2,048.

## Design

- **`LLM_CONTEXT_TOKENS`** in `LlmOptions` (unset = no budget, today's behaviour; `.env.ollama` sets `2048`). For
  Ollama, `LlmClientFactory` passes it as `num_ctx` on every request. Provider differences stay in the factory.
- **`LLM_PROMPT=compact`** (set by `.env.ollama`) makes `AgentHost.LoadSkills` build the system prompt from
  `Skills/local.md` and a generated one-line-per-tool list, instead of every skill file.
- **Toolset** from F5's `AGENT_TOOLSETS`, set in `.env.ollama`.
- **`ContextBudget`** (new, core) estimates tokens (characters ÷ 4 to start; step 1 measures how far off that is for
  Ollama), and fits the history per request: tool results first, then oldest turns. It runs in `AgentEngine` before
  each request only when a budget is set, so Claude is unaffected.
- **Relation to F9** (bounded history): F9 compacts long sessions to save money with summaries; F13 is a hard cap per
  request with no extra model calls. If F9 lands first, F13 reuses its clearing of stale tool results.

## Implementation steps

1. **Spike — measure the parts.** With the usage probe and Ollama: confirm the window (a 3,000-token prompt reports
   `input` ≈ 2,048?), how to send `num_ctx` through OllamaSharp's `IChatClient` (`ChatOptions.AdditionalProperties`?),
   the token size of today's system prompt and of each toolset's definitions, and how far characters ÷ 4 is from
   Ollama's count. Record it in MODELS.md. *No production code.*
2. **`LLM_CONTEXT_TOKENS` and `num_ctx`.** *Test:* the Ollama client's requests carry `num_ctx`; Anthropic's don't.
3. **Compact prompt.** `Skills/local.md`, `LLM_PROMPT=compact`. *Test:* the compact prompt is under 500 tokens
   (estimated) and lists exactly the loaded tools.
4. **Toolset for local** through F5 (`AGENT_TOOLSETS` in `.env.ollama`) and the start-up size check. *Test:* over
   budget fails start-up with the sizes; under budget logs them.
5. **`ContextBudget`: fit the history.** *Test:* a long scripted history is trimmed to the budget, tool results before
   turns, system prompt and latest message kept; no budget = untouched.
6. **Truncation warning** from the ledger's `input`. *Test:* `input` at 2,040 of 2,048 warns; 1,500 doesn't.
7. **Measure and document.** The two S4 tasks on `llama3.1`, ledger lines in MODELS.md; remove the known limitation.

## Done when

- The S4 tasks work on `llama3.1`, and no ledger record of a local turn has `input` above 2,048.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)), including removing the known
  limitation from the TODO and docs/CLAUDE.md.

## Not in this feature

Choosing local models by measured quality (skill-map #1, later stage); speed (F12); raising `num_ctx` by default.

---
[Features](README.md) · Previous: [F12 Response speed](F12-response-speed.md)