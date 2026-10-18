# 0026. A round's tool calls run at once only where every toolset is parallel-safe

**Status:** Accepted · **Date:** 2026-10-18 (decided in F34) · **Recorded:** 2026-10-18

## Context

`FunctionInvokingChatClient` runs the tool calls of one model answer one after another unless
`AllowConcurrentInvocation` is on ([0002](0002-messages-api-tier-with-our-own-loop.md)). In Louis's melktert chat
(F34) the cook planned eleven shop searches in one answer and they ran in turn, 4-5 s each: 45 s of a card that showed
nothing new. The searches don't depend on each other.

Other calls do. The coding agent writes a file and then runs the tests in one answer; a shell script reads what the one
before it wrote; git stages, then commits. Run at once, those race, and the result depends on timing. Puls's own
`StartSubSession` calls open sub-sessions a chat keeps (F26), and two continues of one specialist must not interleave.

Considered: concurrency for every engine (the coding agent's calls race); concurrency inside our `FunctionInvoker`
with a lock for unsafe tools (the lock doesn't keep their order); the model asked to put independent calls in one
answer and dependent ones in turns (a prompt can't guarantee it); a flag per toolset, chosen.

## Decision

- **A toolset can be parallel-safe** (`ToolsetDefinition.ParallelSafe`): its calls read and change nothing another call
  reads. Today `web` and `shops`.
- **An engine runs a round's calls at once only when it has toolsets and every one is parallel-safe**
  (`ToolsetCatalog.RunsInParallel`, passed to `AgentEngine` as `concurrentToolCalls`). One unsafe toolset, or the
  default set (no toolsets named), keeps every call in order.
- **A shared resource bounds itself**, not the loop: the browser opens at most four pages, a shop takes two searches at
  once and pauses after a refusal (`ShopThrottle`), a town's store is looked up once for every search waiting on it
  (`StoreSessionCache`).

## Consequences

- The cook's searches take a round's slowest call, not their sum; its meals went from 100-350 s to 80-155 s (cook check
  `d94597a51d4d`).
- Marking a toolset parallel-safe is a claim about every tool in it, now and later: a tool that writes added to `web`
  or `shops` must take the flag off, or go in a toolset of its own. `OnlyTheWebAndTheShops_RunInParallel` holds the
  list.
- Parallel calls reach outside services faster, so each needs its own limit (Checkers refused searches for four minutes
  before `ShopThrottle`).
- A round's tool results arrive in the order they finish; the history keeps each result with its call's id, so the
  model reads them as before.

## Links

[F34 plan](../plans/F34-shop-deals.md) (*Approved during the build*, the cook's long pause) ·
[F34 Shop deals](../features/F34-shop-deals.md) · [0006](0006-toolsets-per-session.md) ·
[0002](0002-messages-api-tier-with-our-own-loop.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
