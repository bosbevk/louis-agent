# FX4 · Keep conversations append-only, so a mid-chat skill reload or tool approval doesn't break Haiku 5.5 chats

> **Status:** open · **Area:** core, the conversation history (`AgentEngine`) · **Found:** 2026-10-10
>
> **Removes the limitation:** "Reloading skills or approving a tool mid-chat can break the chat on Claude Haiku 5.5"
> ([docs/CLAUDE.md](../CLAUDE.md#known-limitations) #9)

## Symptom (expected, not yet seen in a run)

On Claude Haiku 5.5, the default model, a chat's next message can fail with a 400 like [FX1](FX01-round-limit-tools.md)'s
after the user reloads skills (`ReloadSkills`) or approves an agent-built tool (`/approve`) in the middle of it. Haiku 5.5
enforces this for accounts created on or after 2026-08-31; older accounts only when a request asks for the check.

## Cause

Haiku 5.5 binds its thinking blocks to everything sent before them: system prompt, tools and earlier messages. Two places
change those in an open conversation while its thinking blocks are sent back:

- `AgentEngine.BeginTurn` rewrites `history[0]` (the system prompt) when the skills have been reloaded since the
  conversation started.
- Approving an agent-built tool adds it to the engine's tool list, which every later request sends.

## Fix (to do)

Keep each conversation append-only:

- **System prompt:** freeze it per conversation; say what changed in a new message instead of rewriting `history[0]`
  (Haiku 5.5 accepts mid-conversation `system` messages for this).
- **Tools:** freeze a conversation's tool list when it starts; newly approved tools apply to new chats, and the agent says
  so (or the change is announced the same way as the system prompt).
- **F9 (compaction)** must be designed the same way: summarise by appending, never by editing earlier turns.

## Tests (to do)

- A reload between two turns leaves `history[0]` and the tool list of the open conversation unchanged; the new skill is
  announced in an appended message.
- An approval between two turns doesn't change the tool list an open conversation sends.

## Confirm

A chat on Haiku 5.5: a turn, `/approve` of an agent-built tool (or `ReloadSkills`), another turn: no 400.

## Lesson

The same preserved-thinking rule as FX1, at the conversation level instead of inside one message (skill map #2,
reasoning and extended thinking, and #9, sessions and state).

---
[Fixes](README.md) · Previous: [FX3](FX03-ollama-container.md)