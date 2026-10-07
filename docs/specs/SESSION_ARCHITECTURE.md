# Design: Session Architecture

**Status:** idea — not planned yet · **Date:** moved here from the TODO on 2026-10-07 · **First slice:** bounded history
is planned as feature [F9](../features/F09-context-management.md)

## Today

`AgentSession` (`src/louis-agent.api/AgentSessions.cs`) is an in-memory `List<ChatMessage>` per session id, dropped on
idle timeout or restart. It mixes "conversation history" with "what the model sees", and has no durable storage, memory
or retrieval. The ACP server keeps sessions the same way; the web app keeps a copy of each chat in the browser.

## Target shape

![AgentSession architecture](../todo/agentsession-architecture.png)

The target separates **session state** (who, what task, which memories) from **the model's context** (what actually
goes into each prompt), builds that context deliberately each turn, and stores each kind of data where it fits.

## Workstreams

### 1. Session state (runtime)

- Separate **session state** (identity, task state, memory references) from **LLM context** (what goes in the prompt):
  a session is not the model's memory.
- Extend `AgentSession` beyond id + history: lifecycle state, user profile/preferences, task state (goals), memory
  references, tool state. Today only id, history and the turn lock exist.
- Durable storage instead of only the in-process `ConcurrentDictionary`: a database for the full conversation, so a
  restart doesn't lose it (removes the *sessions are in memory* limitation).

### 2. Context-building pipeline

A step between the session and the model call: **retrieve → rank and filter → token budget → compress (only if over
budget) → build the prompt**, instead of sending the whole raw history every turn.

- Distinguish the context sources now blurred into one list: recent conversation, relevant past conversations
  (semantic search), long-term memory (user facts and preferences), external knowledge (RAG), task and tool state —
  each with its own retrieval.
- Replace "send everything" with ranked retrieval: recent + semantic + keyword search + metadata filters +
  recency/importance, then keep only the most relevant.
- A token budget allocator that gives each source a window before compression starts.
- History compaction for long conversations: recent messages verbatim + summarised older history + selective retrieval.
  **The first slice of this — clearing stale tool results and compacting old turns at thresholds — is feature F9.**

### 3. Model call and after the turn

- Make the assembled prompt an explicit object (system instructions, selected context, current query, tool definitions,
  output format) instead of ad hoc message construction.
- After each turn: persist the turn, update session state (tasks, tool state, references), and extract long-term memory
  facts asynchronously. Today the session is only mutated in place.

### 4. Persistence by access pattern

- Hot session state: Redis or in-memory with a TTL.
- Durable transcript, long-term memory, audit and retention: a database.
- Embeddings for semantic search: a vector store.
- Large artifacts (files, images, big tool outputs): blob storage, referenced from the prompt rather than inlined.

### 5. Lifecycle

- Explicit states: created → active ⇄ idle → ended → archived, instead of "exists until the idle timeout removes it";
  resume or rehydrate from archived.

## Open questions

- How much history to include (the token budget), and when to summarise or compact.
- Error handling in the pipeline: retries, idempotency, consistency across storage layers.
- Scaling: keep the agent runtime stateless and push state into shared stores.
- Security and isolation between sessions and users.
- Privacy: PII redaction, retention policy, deletion.
- Memory quality: de-duplication, conflicts, stale long-term facts.
- Short sessions (few turns, recent history only) vs long ones (days, with summaries and semantic search): choose per
  session kind rather than one strategy for all.

---
[Docs index](../README.md) · Related: [TODO](../../TODO.md) · [F9 Bounded history](../features/F09-context-management.md) ·
[Response optimisation](RESPONSE_OPTIMISATION.md)
