# FX4 · Keep conversations append-only, so changing tools or skills mid-chat doesn't break Haiku 5.5 chats

> **Status:** open · **Area:** core, the conversation history and tool list (`AgentEngine`) · **Found:** 2026-10-10
>
> **Removes the limitation:** "Changing the tools or skills during a chat can break it on Claude Haiku 5.5"
> ([docs/CLAUDE.md](../CLAUDE.md#known-limitations) #9)

## Symptom (expected from the code and Anthropic's docs; not yet seen in a run)

On Claude Haiku 5.5, the default model, a request fails with a 400 like [FX1](FX01-round-limit-tools.md)'s ("Invalid
`signature` in `thinking` block … The `tools` list differs …") when the system prompt or the tool list changed since
earlier thinking blocks in the conversation were written. Haiku 5.5 enforces this for accounts created on or after
2026-08-31, 00:00 UTC; older accounts only when a request asks for the check.

## Cause

Haiku 5.5 binds its thinking blocks to everything sent before them. Anthropic's
[preserved thinking](https://platform.claude.com/docs/en/build-with-claude/preserved-thinking) page: the checked prefix
is the `system` prompt, the set of `tools`, and every message before the block; "Add, remove, rename, or edit a tool in
`tools`" is an edit that invalidates later thinking. These change them today:

| Trigger | What changes | When it bites |
|---|---|---|
| **The model calls `CreateTool`** (likeliest) | `RegisterScriptTool` adds the tool to `_tools`, and `BeginTurn` hands every request that same list object (`chatOptions.Tools = _tools`, `AgentEngine.cs` ~line 567), not a copy | The **next request in the same message**: CreateTool's own result says "You can call it now" |
| `/approve <Name>` | The tool's description drops ", pending user approval" (`ScriptTool.Description`) | The next message |
| `/reject <Name>` | The pending tool is removed from `_tools` | The next message |
| `ReloadSkills` | `BeginTurn` rewrites `history[0]`, the system prompt | The next message |
| **Any of the above in another session** | `_tools` belongs to the engine, and louis-agent.api has **one engine for all sessions** (`AddSingleton(host.Engine)`), so one chat's new tool changes every open chat's tool list | Every other open conversation's next request |

The first and last rows were found by reading the code (the first from a PR review); none has been seen in a run yet.

## Fix (to do)

**Freeze each conversation's system prompt and tool list when it starts, and only ever append.**

- **Tools:** a conversation keeps a copy of the tool list from its first request; `BeginTurn` sends that copy, never the
  engine's live `_tools`. Tools created, approved or rejected later apply to **new** conversations. `CreateTool`'s reply
  changes accordingly ("available in your next chat"), so the model doesn't promise what it can't call.
- **FX1's final-request middleware** must restore the **conversation's** tool list, not the engine's: today
  `KeepToolsOnFinalRequest` sets `final.Tools = _tools`, the live list, so after freezing, a message that hits the round
  limit after a tool changed elsewhere would send a different list on its final request and fail with FX1's 400 again.
  `BeginTurn` puts the frozen list in `ChatOptions.AdditionalProperties` as well; `FunctionInvokingChatClient` clears
  `Tools` on the final request but keeps the rest of the cloned options, so the middleware reads the list from there
  (confirmed in the Microsoft.Extensions.AI 10.10.0 source by the PR #9 review). An adapter may forward
  `AdditionalProperties` into the request body, where the API would reject an unknown field: captured locally, the
  Anthropic adapter (12.53.0) does **not** (the body had only `max_tokens`, `messages`, `model`, `tools`), but the Ollama
  and OpenAI-compatible adapters aren't checked, and a later version could change. So the middleware **removes the key**
  before passing the request on (or the frozen list lives in an `AsyncLocal`, as `UsageScope` does), and a test asserts
  the key never reaches the inner client. It removes it from a **copy** of the options on every request:
  `FunctionInvokingChatClient` reuses one options object for all the rounds and copies it only for the final one, so
  removing the key from the shared object on round 1 would leave the final request without the frozen list.
- **System prompt:** keep `history[0]` as it was; after a skill reload, append a short system message saying what
  changed (Haiku 5.5 accepts mid-conversation system messages). An appended message doesn't invalidate the thinking
  written before it; it becomes part of the checked prefix for thinking written after it, which is what append-only
  means.
- **F9 (compaction)** must be designed the same way: summarise by appending, never by editing earlier turns.

**Alternatives considered:**

| Option | What it does | Why not (for now) |
|---|---|---|
| `tool_addition` / `tool_removal` blocks in an appended system message | Adds or removes a tool mid-conversation without touching `tools`, so thinking stays valid and the cache keeps hitting | Beta (`inline-tools-2026-09-15`) and Claude API only; the engine must stay provider-neutral (`IChatClient`). Worth adding later for Anthropic, behind `LlmClientFactory`, so a new tool can be used in the same chat |
| `prefix_mismatch_behavior: "drop_block"` | The API drops the stale thinking blocks instead of failing | Beta (`thinking-binding-controls-2026-08-01`). The request succeeds but the model loses its earlier reasoning, the prompt cache restarts at the edit, and Anthropic measured 4.6% (low effort) to 67% (max) more output tokens. A safety net, not a design |
| Leave it, and tell users to start a new chat | Today's known limitation | The `CreateTool` case fails in the middle of a message, with no chance to start a new chat |

## Tests (to do)

- `CreateTool` mid-message: the requests after it in the same message send the same tool list as before it.
- `/approve` or `/reject` between two messages leaves the open conversation's tool list (names and descriptions) unchanged.
- A tool created in one session doesn't change the tool list another open session sends.
- A message that hits the round limit after a tool changed elsewhere: its final request carries the conversation's frozen
  tool list (same as its earlier requests) with `tool_choice: none`, not the engine's current one
  (`AgentEngineRoundLimitTests`, extended).
- The frozen-list key never reaches the provider on any round, and the options object the tool loop reuses still has it
  when the final request is made.
- A skill reload between two messages leaves `history[0]` unchanged and appends a system message.

## Confirm

On Haiku 5.5: a message in which the model creates a tool and keeps working; then `/approve`, `ReloadSkills` and a second
open chat, each followed by another message: no 400.

## Lesson

The same preserved-thinking rule as FX1, at the conversation and session level: shared mutable state (one engine's tool
list) leaks into every conversation's checked prefix. Skill map #2 (reasoning and extended thinking) and #9 (sessions
and state).

---
[Fixes](README.md) · Previous: [FX3](FX03-ollama-container.md)
