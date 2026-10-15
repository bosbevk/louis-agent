# F26 · Specialist follow-up: Puls asks the same specialist again

> **Status:** planned (2026-10-15) · **Milestone:** — · **Depends on:** F5 (sub-sessions), F21 (roles), F22 (the per-turn
> context a tool reads: attachments, the user's place)
>
> **In the TODO:** **F26 Specialist follow-up** under *Sessions and memory*, beside F21, F22 and F25 ([TODO](../../TODO.md)) ·
> **Plan:** [plans/F26](../plans/F26-specialist-follow-up.md)

A sub-session lives for one task: `StartSubSession` opens it, sends the task, reads the answer and deletes it, even on
failure (F5-S5). When the user follows up ("maak dit vir ses mense", "swap the lamb for chicken, what does it cost
now?"), Puls can only start a new specialist cold: it repeats the task, and the cook searches and fetches the shop pages
again. Louis's ask: Puls must be able to re-ask the specialist that did the work, so a conversation about one piece of
work stays with one specialist, with its context and its cached prefix.

## User stories

**F26-S1 — A follow-up goes to the same specialist.** As a *user*, I want my follow-up on the cook's meal answered by
the cook that planned it, so that it remembers the dish, the shop and the prices and I don't get a different meal.
- *Given* the cook answered in this chat *when* I ask a follow-up about the meal *then* Puls sends it to that cook's
  session, and the answer builds on its first (same dish unless I change it, prices it already found).
- *Given* the follow-up is new work for another role (the prime rate) *then* Puls starts a new specialist as today.

**F26-S2 — Puls keeps its specialists and knows them.** As *Puls*, I want each delegation's answer to tell me the
specialist's handle, and a tool to send it another message, so that I can choose between continuing and starting over.
- *Given* `StartSubSession` answered *then* its result names the open sub-session (`sub-1`, its agent, toolsets and
  role).
- *Given* `ContinueSubSession("sub-1", message)` *then* the message goes to that session, and the answer comes back
  with its usage line, rolled up into my turn like a first answer (F5 follow-up).
- *Given* an unknown or closed handle *then* an error that lists the open ones, so I can start over instead.

**F26-S3 — Nothing leaks or grows without limit.** As an *operator*, I want kept-open specialists bounded and closed,
so that a peer never keeps sessions nobody will use.
- *Given* a chat is deleted, or dropped as idle *then* its open sub-sessions are deleted on the peer.
- *Given* more than the cap of open sub-sessions in one chat (3) *then* the oldest is closed when a new one opens.
- *Given* `StartSubSession` runs with no chat to keep it (an MCP call) *then* it closes the sub-session as today.
- *Given* a continue runs past the sub-session timeout, or the turn already made 3 calls (starts and continues
  together) *then* it fails with the same errors as a start.

## Design notes

- **The alternative, rejected:** stateless re-delegation (Puls repeats the whole context to a new specialist). Nothing to
  build, but the specialist redoes its research every time: the cook refetches every shop page, the answer can drift (a
  different dish), and F4's cached prefix is written again. The roles run showed the cold start is the cost of
  delegation; a kept session turns a follow-up into cache reads.
- **Open sub-sessions belong to the parent session, not the engine** (an engine is shared by every session with the same
  toolsets). A `SubSessionRegistry` in core (`Tools/Agents/`) holds a chat's open sub-sessions by handle (peer, peer
  session id, agent, toolsets, role, last used); the API's `AgentSession` owns one and makes it current for each turn,
  as it does `SessionAttachments` and `UserPlace` (F22). With none current, `StartSubSession` behaves as today.
- **Two tools, both always in `agents`:** `StartSubSession` (unchanged arguments; its answer's footer names the handle)
  and `ContinueSubSession(handle, message)`. The tool list of a conversation never changes (FX4), so the second tool
  exists from the first turn. Handles are short and per chat (`sub-1`, `sub-2`), never the peer's session id.
- **Limits stay in code:** continues count against `MaxPerTurn` with starts; each message gets the sub-session timeout;
  the cap of open sub-sessions per chat is 3 (the oldest closed); the peer's own idle timeout (4 h) is the fallback.
- **The prompt:** `agents-skills.md` gets *Following up* (continue when the follow-up is about work a specialist did in
  this chat; start over when it's new work or another role), and `default.md`'s delegation rule says the same in one
  line. The Werk panel needs nothing: a continue's events carry the tool call's id and the agent, like a start's.

## Done when

- **F26-S2 and S3:** unit tests on `AgentPeerTools` with a stubbed peer (kept open with a registry, deleted without one;
  continue sends to the right session; unknown handle; the cap; the per-turn limit counts both) and API tests (the
  chat's delete and idle drop close its sub-sessions). The golden files show only the new tool and texts.
- **F26-S1:** in the cook check (F25), a follow-up on the meal goes to the same cook (`ContinueSubSession` with the
  handle of the first delegation), the dish is the same, and the follow-up costs less than the first answer.
- Suites pass; *Finishing a feature*.

## What the build changed

*(filled in as the steps land)*

---
[Features](README.md) · [Plan](../plans/F26-specialist-follow-up.md)
