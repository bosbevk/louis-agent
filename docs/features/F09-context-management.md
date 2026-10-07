# F9 · Bounded history: clear stale tool results, compact old turns

> **Status:** planned · **Milestone:** M4 · **Depends on:** F1, F4
>
> **Spec:** [Optimisation §D](../specs/RESPONSE_OPTIMISATION.md) · [Session architecture §2](../specs/SESSION_ARCHITECTURE.md)
>
> **In the TODO:** tick **F9 Bounded history** under *Now* ([TODO](../../TODO.md)) · **Replaces:** "Consider context editing and compaction"

History only grows: every file read and test output stays in the conversation and is re-sent every round until the
session ends. F9 bounds it in two steps — first replacing stale tool results with short stubs, then summarising old
turns — at token thresholds, so the work is rare and caching keeps working in between.

## User stories

**F9-S1 — Clear stale tool results.** As an *operator*, I want old tool results replaced with short stubs once a
conversation is large, so that later rounds don't keep paying for files the model has finished with.
- *Given* the last request's input tokens (F1) passed `CONTEXT_CLEAR_AT` (default 60,000) *when* the next turn (or the
  next "continue" message) starts *then* tool results older than the last `CONTEXT_KEEP_ROUNDS` rounds (default 4) are
  replaced with `[cleared: ReadWorkspaceFile src/…/OrderTotal.cs — 3,120 chars; read it again if needed]`.
- *And* tool calls and results stay paired (same call ids), so the history stays valid for every provider.

**F9-S2 — Compact old turns.** As a *user* in a long chat, I want older turns summarised once the chat is very large, so
that it keeps working and stays affordable.
- *Given* the conversation still passes `CONTEXT_COMPACT_AT` (default 120,000) *when* a turn starts *then* everything
  before the last `CONTEXT_KEEP_TURNS` user turns (default 3) is replaced by one summary message written by a separate
  call (`purpose: "compaction"`), and the kept part starts at a user message.

**F9-S3 — Nothing is lost for good.** As a *developer*, I want the original messages saved before they are cleared or
compacted, so that I can always see what the model saw.
- *Then* the replaced messages are appended to `logs/sessions/<session id>.jsonl` before replacement.

**F9-S4 — Still remembers.** As a *user*, I want a compacted chat to still answer questions about its early turns, so
that compaction doesn't make it forget.
- *Given* the scripted 30-turn chat (the benchmark) *then* it stays under `CONTEXT_COMPACT_AT` and answers the recall
  questions about turns 1–3 correctly.

**F9-S5 — Doesn't hurt fixes.** As an *operator*, I want the demo's results unchanged, so that bounding history isn't a
quality trade.
- *Then* 10/10 fixes confirmed and cost per fix not higher than the M2 run.

## Design

- **When:** only at **turn boundaries and before "continue" messages** — never inside the tool loop of one message.
  Within one message the loop is bounded (≤ `MaximumIterationsPerRequest` rounds), and rewriting history mid-loop would
  break the cache every round. Rewrites invalidate the message cache from the first changed message, so thresholds
  (not every turn) keep that rare; the tools-and-system cache (F4 marker 1) is never affected.
- **Measuring size:** the last request's `input + cache_read + cache_write` tokens for this session from F1 — real
  counts, no tokenizer needed.
- **`ContextManager`** (`louis-agent.core/tools/` or `context/`), called from `AgentEngine.BeginTurn` with the stored
  history:
  1. `ClearStaleToolResults(history, keepRounds)` — rebuilds old `FunctionResultContent` items with stub results
     (tool name and argument summary from the matching `FunctionCallContent`, original size).
  2. `CompactAsync(history, keepTurns)` — summarises the older part with `_innerChatClient` (tool-less, like
     `SummariseToolResultAsync`; F10 can route it to a cheaper model) into one user-role message labelled as a summary
     of earlier conversation; thinking blocks in the replaced part go with it.
- **Archive** through `AgentLog` before each rewrite (S3).
- **Benchmark chat:** a script (`samples/benchmarks/long-chat.ps1` or a test harness) that drives 30 turns against
  louis-agent.api on the demo repository, then asks recall questions with known answers.
- **Provider-native alternatives** (Anthropic context editing / compaction) stay out: they need the beta client, and
  server-side compaction isn't offered for Haiku 4.5.

## Implementation steps

1. **`ClearStaleToolResults`.** *Test:* rounds older than the window get stubs; recent ones untouched; call/result pairs
   intact; stub text names tool, target and size.
2. **Archive** to `logs/sessions/…`. *Test:* replaced messages appear in the archive before the rewrite.
3. **`CompactAsync`.** *Test (fake client):* older part replaced by one summary message; kept part starts at a user
   message; the summary request has no tools; usage recorded as `compaction`.
4. **Thresholds + wiring in `BeginTurn` and before continuations.** *Test:* below thresholds nothing changes; above, the
   right step runs once.
5. **Benchmark chat script + recall check.** *Test:* runs against the demo API; prints tokens per turn and recall result.
6. **Tune thresholds** on the benchmark (chat and demo); record numbers.
7. **Docs:** AGENT_INTERACTION (history management), SETUP settings, spec status, TODO context-pipeline items.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Benchmark chat passes recall under the threshold; demo 10/10 with cost per fix ≤ M2.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F9** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F8 Reliability](F08-reliability.md) · Next: [F10 Route settings](F10-route-settings.md)
