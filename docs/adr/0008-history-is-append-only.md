# 0008. Conversation history is append-only

**Status:** Accepted · **Date:** 2026-10-10 · **Recorded:** 2026-10-14

## Context

Claude Haiku 5.5, the default model, binds each thinking block to everything sent before it: the system prompt, the
set of tools, and every earlier message. A request that replays a thinking block after any of those changed fails with a
400 (enforced for accounts created on or after 2026-08-31). FX1 met it first: at the 10-round limit the engine removed
the tools from the request, and the short demo confirmed only 2 of 4 fixes.

## Decision

Nothing that was already sent is edited mid-conversation: not the system prompt, not the tool list, not earlier
messages. A change is made by adding (a new message), not by rewriting. At the round limit the tools stay on the request
with `tool_choice: none` (FX1); the finished turn is appended with its thinking blocks.

## Consequences

- Chats on Haiku 5.5 keep working across tool rounds (FX1 took the short demo from 2/4 to 4/4 fixes).
- Toolsets are fixed per session ([0006](0006-toolsets-per-session.md)), which this rule requires as well as caching.
- **Not yet fully held:** a tool the model creates (`CreateTool`), `/approve`, `/reject` and a skill reload still change
  the tool list or prompt mid-chat, and break it. That is known limitation 9, removed by
  [FX4](../fixes/FX04-append-only-history.md), which is open.

## Links

[FX1](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX01-round-limit-tools.md) · [FX4](../fixes/FX04-append-only-history.md) ·
[Known limitations](../../TODO.md#known-limitations)
