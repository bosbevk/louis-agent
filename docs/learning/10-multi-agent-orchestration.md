# 10 · Multi-agent orchestration: what Puls taught (draft)

> **Skill:** #10 in the [goal](../GOAL.md)'s map · **Stages:** F5 sub-sessions (live delegation), F21 personalities and
> roles, F22 role tools, F20's Werk panel (2026-10-13) · **Status:** a first draft by Claude for Louis to edit

## Before and after

**Before,** one agent did everything: every tool and every guide in one prompt, about 28,600 tokens of fixed prefix
(109 tools), in one voice ("Louis's Minion") whatever the host.

**After,** Puls is a router that does no work itself. It reads the request, picks the toolsets and the **role** from one
table (`default.md`), and hands the task to a specialist on a peer (`StartSubSession`), which runs with only those tools
and a personality that is its job description. The user watches the work beside the answer.

| Prompt (fixed prefix, estimated) | Tools | Tokens |
|---|---|---|
| Every toolset, `personality.md` (before) | 109 | ~28,600 |
| Puls: `agents`, `puls.md`, the router | 2 | ~2,200 |
| A researcher: `web`, `puls-researcher.md`, no router | 3 | ~1,900 |

F5's short benchmark measured the same idea on the orchestrator demo: the prefix about 48.5k → 24.4k tokens, the run
$0.0488 → $0.0378, 4/4 fixes.

## What changed, and why this way

- **A role is a personality** (F21). The specialist's system prompt starts with its function and how to report
  (`puls-researcher`: sources with dates; `puls-historian`: commits; `puls-analyst`: the method and the script), on a
  shared base: one task, never the user, verify, say what's missing, can't delegate. Considered: one generic specialist
  prompt (simpler, but every report came back in a different shape), and roles as toolset guides (but a guide describes
  tools, not a job).
- **The router names toolsets and role together.** The decision starts from the request ("the world" → `web` +
  `puls-researcher`), so both live in one table row; a separate toolset → role table was one more lookup to forget.
- **Sub-sessions get no router** (`default.md`): their toolsets were chosen for them and they can't delegate, so the
  router only cost tokens and could mislead them.
- **Context crosses the boundary as data, not through the model.** Attachments go by name (the session supplies the
  content), the user's place and time zone are set per turn (`UserPlace`); the model never copies a file into a tool call.
- **Tools per role** (F22): what each job needed and lacked (today's date, documents, blame and history search, tables,
  currency), on maintained packages, not our own parsers.
- **Transparency is part of the design** (F20): the Werk panel shows each specialist's steps, sources and cost, so
  delegation isn't a black box, and the chat's cost includes the specialists.

## Trade-offs and what didn't work

- **The roll-up isn't done:** the chat's usage still excludes its sub-sessions; the app adds them up client-side and
  says so, until the F5 roll-up lands.
- **A role is advice, not enforcement:** Puls can pass the wrong role or none; the specialist then works with the default.
- **Two models' worth of latency:** every task costs Puls's turn plus the specialist's; worth it for tool-heavy work,
  not for chit-chat (Puls answers those itself).
- **Mistakes found by looking:** `AGENT_FUNCTION=louis` loaded every skill file into Puls's prompt (fixed: skill files
  come with the `skills` toolset); the model guessed a peer name from an example (fixed: an empty name means the only peer).

## Next

Measure Puls on real questions (quality and cost per role), the F5 roll-up, evals per role (F18), and parallel
specialists for a task with independent parts.

---
[Learning](README.md) · [Goal](../GOAL.md)
