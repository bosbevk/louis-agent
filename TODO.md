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

Current `AgentSession` (`src/louis-agent.api/AgentSessions.cs`) is just an in-memory `List<ChatMessage>` per session
id, dropped on idle timeout or restart. It conflates "conversation history" with "what the model sees" and has no
durable storage, memory, or retrieval. Longer-term direction, in order:

- [ ] Separate **session state** (identity, task state, memory references) from **LLM context** (what actually goes
      in the prompt) — session ≠ model memory
- [ ] Give the session durable storage instead of only the in-process dictionary: a DB for full conversation history,
      so a restart doesn't lose it (ties into the existing "sessions are in memory" limitation above)
- [ ] Introduce a context-building step between session and LLM call: select recent history + relevant history +
      memory + task state for the current query, instead of sending the full/raw history every turn
- [ ] Distinguish three separate concerns currently blurred together: conversation history (what was discussed),
      long-term memory (what to remember about the user), and RAG (external knowledge) — each needs different
      retrieval, not one bucket
- [ ] Replace "send the last N messages" with ranked retrieval: recent messages + semantic/keyword search + metadata
      filters + recency/importance, then select only the most relevant before assembling the prompt
- [ ] Formalize the prompt-assembly pipeline (retrieve → rank → compress → fit token budget → assemble → invoke) as
      an explicit step, combining system instructions, selected context, current query, tool definitions, task state
- [ ] Add history compaction for long conversations: recent messages verbatim + summarized/compacted older history +
      selective retrieval, instead of unbounded growth or a hard cutoff

## Ideas / backlog

- [ ]
