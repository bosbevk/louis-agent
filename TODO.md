# TODO

Open items for Louis Agent. Add to this list as new work comes up; check items off (or delete them) once done.

## Known limitations ([docs/CLAUDE.md](docs/CLAUDE.md#known-limitations))

- [ ] Rider MCP discovery only logs Rider's tools; they aren't callable yet (needs a full MCP client)
- [ ] Sessions are in memory in the API and ACP server; a restart ends them (the web app keeps the transcript)
- [ ] Ollama tool support is a name heuristic (`LlmOptions.KnownNoToolsPrefixes`)
- [ ] Paymo task lookup takes the first match for a name; ambiguous names can hit the wrong task
- [ ] No rate limiting or audit log beyond the file logs

## Testing

- [ ] The API and web app have no automated tests yet; only checked by hand (curl and a browser)

## Session architecture (AgentSession)

![AgentSession Architecture](docs/todo/agentsession-architecture.png)

Current `AgentSession` (`src/louis-agent.api/AgentSessions.cs`) is just an in-memory `List<ChatMessage>` per session
id, dropped on idle timeout or restart. It conflates "conversation history" with "what the model sees" and has no
durable storage, memory, or retrieval. The diagram above is the target shape; broken down into workstreams:

### AgentSession (runtime state)

- [ ] Separate **session state** (identity, task state, memory references) from **LLM context** (what actually goes
      in the prompt) — session ≠ model memory
- [ ] Extend `AgentSession` beyond id + history: lifecycle state, user profile/preferences, task state (goals), memory
      references, tool state — currently only id, history and the turn lock exist
- [ ] Give the session durable storage instead of only the in-process `ConcurrentDictionary`: a DB for full
      conversation history, so a restart doesn't lose it (ties into the existing "sessions are in memory" limitation)

### Context engineering pipeline

- [ ] Introduce a context-building step between session and LLM call: retrieve → rank & filter → token budget →
      compress/summarize (only if over budget) → build prompt, instead of sending the full/raw history every turn
- [ ] Distinguish the context sources currently blurred into one history list: recent conversation, relevant past
      conversations (semantic search), long-term memory (user facts/prefs), external knowledge (RAG), task & tool
      state — each has a different retrieval method
- [ ] Replace "send the last N messages" with ranked retrieval: recent + semantic search + keyword search + metadata
      filters + recency/importance, then select only the most relevant before assembling the prompt
- [ ] Add a token budget allocator that assigns a window per context source before compression kicks in
- [ ] Add history compaction for long conversations: recent messages verbatim + summarized/compacted older history +
      selective retrieval, instead of unbounded growth or a hard cutoff

### LLM invocation & response

- [ ] Formalize the assembled prompt's shape: system instructions, selected context, current query, tool
      definitions, output format/schema — as an explicit object, not ad hoc message construction
- [ ] After the turn: persist the new turn to history, update session state (tasks/tool state/refs), and extract
      long-term memory facts asynchronously (none of this happens today — the session is mutated in place only)

### Persistence layer

- [ ] Split storage by access pattern instead of one in-memory dictionary for everything: Redis/in-memory for hot
      session state (fast lookup, TTL), DB for durable transcript + long-term memory + audit/retention, vector store
      for embeddings/semantic search, blob storage for large artifacts (files, images, tool outputs) kept out of the
      prompt

### Session lifecycle

- [ ] Model explicit lifecycle states (create → active ⇄ idle → ended → archived) instead of only "exists until idle
      timeout removes it"; consider resume/rehydrate from archived state

### Open design questions (from "Key Considerations")

- [ ] How much history to include (token budget) and when to trigger summarization/compaction
- [ ] Error handling for the context pipeline: retries, idempotency, consistency across storage layers
- [ ] Scaling: keep agent runtime stateless, push state into distributed stores
- [ ] Security & tenant isolation between sessions/users
- [ ] Privacy: PII redaction, retention policy, deletion
- [ ] Memory quality: dedupe, conflict resolution, staleness of long-term memory facts
- [ ] Decide the short-session vs. long-session tradeoff explicitly (few turns/recent-only vs. days-long with
      summarization + semantic search) rather than one fixed strategy for every session

## Ideas / backlog

### Ops / CI

- [ ] Add CI (`.github/workflows`) to run `dotnet build` / `dotnet test` on push/PR — there's no CI at all today, so
      regressions are only caught locally
- [ ] Track token usage / cost per session — useful given this proxies to paid Anthropic calls
- [ ] Add metrics/tracing (e.g. OpenTelemetry) beyond the stderr + JSONL file logs (`AgentLog`) — no
      latency/error dashboards today
- [ ] Add model fallback: retry against a secondary provider/model if the configured one errors or rate-limits

### Web app

- [ ] Add automated tests for `louis-agent.web` (bUnit/Playwright) — currently hand-checked only
- [ ] Sync chat history across devices/browsers instead of only `localStorage` (lost on clearing site data)
- [ ] Add a UI for managing skills (`Skills/*.md`) and for reviewing/approving agent-built tools — `/approve` is
      CLI-only today

### CLI

- [ ] Let the CLI resume a previous session's history across restarts (the web app keeps it in `localStorage`; the
      CLI and ACP server don't persist it)

### Tooling / multi-repo

- [ ] Add a tool for searching/operating across more than one mounted repo at once — `REPOSITORIES_PATH` mounts
      multiple repos, but every tool call is scoped to a single workspace root
- [ ] Add a marketplace/sharing mechanism for agent-built tools (`Skills/tools/pending`) between projects or
      teammates — each repo builds its own from scratch today
