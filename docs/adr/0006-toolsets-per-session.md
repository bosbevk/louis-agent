# 0006. A session chooses its toolsets when it opens, and keeps them

**Status:** Accepted · **Date:** 2026-10-13 · **Recorded:** 2026-10-14

## Context

Every engine registered every toolset whose keys were set: in the demo, 108 tools (68,162 characters of definitions),
35 of them Paymo and DevOps tools a code-fixing container never uses, plus their guidance in the system prompt. That
prefix is sent with every request. Changing tools in the middle of a chat breaks the cached prefix, and on Haiku 5.5 the
thinking blocks already in the history (ADR 0008).

## Decision

A session names its toolsets when it opens (`POST /sessions` with `toolsets`) and keeps them for its lifetime. Only
those tools are registered, and only their guides are in the system prompt. Nothing loads at start-up; without a list
a session gets `skills` only. An engine is built per toolset list and shared by the sessions that use it. A general
agent can open a sub-session on a peer agent with the toolsets a task needs; sub-sessions are one level deep.

## Consequences

- The prefix shrank by about half (~48.5k → ~24.4k tokens) and the short demo run by 23%, with the cache still hitting.
- Clients must send the toolsets they need; one without a list gets almost no tools.
- Each toolset list has its own cached prefix and first cache write: keep the number of lists small.
- Delegating to a specialist is not always cheaper; measuring when it pays continues in the TODO.

## Links

[F5 Toolset profiles](../features/F05-toolset-profiles.md) · [F5 plan](../plans/F05-toolset-profiles.md) ·
[F5 benchmark](../BENCHMARKS.md#f5-benchmark-short-run-2026-10-13)
