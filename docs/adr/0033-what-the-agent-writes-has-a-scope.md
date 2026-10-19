# 0033. What the agent writes has a scope: session, agent or shared

**Status:** Accepted · **Date:** 2026-10-19 (Louis, during Phase 1, while FX20 was planned) · **Recorded:** 2026-10-19

## Context

The agent writes things for itself: skill files (`CreateSkillFile`) and agent-built tools (`CreateTool`). Today an
approved one goes to one folder (`Skills/agent/functions/`, `Skills/tools/`), and every agent that loads skills or
tools gets it. That was fine with one agent; with identities (CL15, [0027](0027-engine-identities-and-packs.md)) the
coding agent's git skill would show up in Puls's cook. Louis, 2026-10-19: tools and skills "should be specific for a
/agent session", like memory is kept per project.

FX20 adds the first level: a skill file waits in `pending/`, active only in the engine that wrote it, until a person
types `/approve`. But approval itself still has one target, everyone.

The standard pattern, as in Claude Code's permission prompt (allow once, for this session, always, per project or per
user), its skills and CLAUDE.md (per user, per project, local), and LangGraph's memory (a thread's own, or a long-term
store with namespaces), has three rules: the narrowest scope by default; a person widens it, never the agent; and
*who* sees a thing is a separate question from *how long* it lasts.

Considered: one scope for everything (today; leaks between identities); scoping by identity only (no way to trust a
skill for the job at hand without keeping it for every later chat); letting the agent choose its scope (rejected: a
person widens).

## Decision

- **Three scopes, for skill files and agent-built tools alike:** *session* (only the chat that wrote or approved it),
  *agent* (every chat of that identity or role) and *shared* (every agent).
- **The narrowest by default:** what the agent writes starts pending in its own session (FX20). A person widens it with
  `/approve {name} session|agent|shared`; the model never approves or widens its own.
- **Who and how long are separate:** the scope says who sees it; how long it lasts follows from where it is kept.
  While sessions live in memory, a session scope lasts as long as the chat; durable sessions (F43) keep it with the
  session.
- **An agent loads its own scope plus the shared one**, never another agent's.
- **What the agent writes reaches the model as a tool result, not by reloading the system prompt** (with FX4), so
  creating or approving one never rewrites a chat's history ([0008](0008-history-is-append-only.md)).
- **Approval checks intent, not behaviour:** an executable skill runs sandboxed whatever its scope (the skill sandbox,
  from [0030](0030-build-or-adopt.md)'s Existo lessons).

## Consequences

- FX20 (Phase 1) builds the first level only: pending in the session, and `/approve` to today's one folder.
- Phase 2 (CL15) adds the session, agent and shared targets of `/approve` and `/reject`, for skills and tools
  together, once identities exist; a session's scope needs its session id, which the API and CLI already have.
- Phase 7 (FX4) makes a written or approved skill arrive as a tool result, which also removes known limitation 9 for
  skills; F41's approvals screen shows each item's scope.
- More folders to keep and test, and one more argument on `/approve`.

## Links

[FX20](../fixes/FX20-skill-files-wait-for-approval.md) · [Platform plan](../plans/platform.md) (Phases 1, 2 and 7) ·
[0006](0006-toolsets-per-session.md) · [0008](0008-history-is-append-only.md) ·
[0027](0027-engine-identities-and-packs.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
