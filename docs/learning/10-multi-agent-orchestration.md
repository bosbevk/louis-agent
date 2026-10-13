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

## Measured: delegation against one agent (2026-10-13/14)

A delegation check (`samples/puls-check/run_check.py`) asks the same ten questions twice, each in its own session:
once to one agent with every toolset (the baseline), once to Puls with its specialists. It prices both from the usage
ledger, by token kind.

| Run | Baseline | Delegated | What it found |
|---|---:|---:|---|
| [First, five questions](../../samples/sample-output-puls-check/README.md) | $0.0167 | $0.0218 (+31%) | Fewer tokens (−60%), but dearer kinds: each specialist writes its own prefix to the cache, and the answer is written twice. Puls passed a role 1 time in 4 |
| [Roles derived on the peer](../../samples/sample-output-puls-check-roles/README.md) | $0.0324 | $0.0509 (+57%) | Every delegation had a role. Three refused calls (a required `agent`), an empty answer retried from scratch, a blocked `.env.example` |
| [After FX10–FX12](../../samples/sample-output-puls-check-fixes/README.md) | $0.0279 | $0.3128 | One counting task went to a files-only reader, which read 74 files into a 298k-token prompt at the long-prompt price: $0.278 |
| [Three runs, after FX10–FX14](../../samples/sample-output-puls-check-repeat/README.md) | $0.0276 mean | $0.0363 mean (+31%) | All 24 delegations routed by the table; the dearest question $0.0174; runs varied +6%, +11%, +86% |

What the numbers say:
- **Fewer tokens isn't cheaper.** The baseline's tokens are mostly cache reads, the cheapest kind; a specialist starts
  cold and writes its prefix, and output (the dearest kind) roughly triples.
- **Delegation pays for research and chat, not for short lookups.** It was cheaper for chat and web questions in every
  run, and 1.2–3.5× dearer for short files, git and counting questions. The TODO now asks whether Puls delegates too
  often.
- **The tail matters more than the average.** One badly routed task cost eight times the other nine together. The fix
  was two layers: a prompt that routes and tells every agent what tool calls cost (FX13), and a limit in code, a prompt
  budget per sub-session under the long-prompt price step (FX14).
- **One run per mode isn't enough** to decide: the same ten questions varied from +6% to +86%.

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
- **A role in the prompt wasn't enough:** Puls passed one in 1 delegation of 4. The peer now derives a role from the
  toolsets when none is passed (`roles.json`), so code, not the prompt, guarantees every specialist has one.
- **Routing is still a prompt,** and a wrong route can be expensive (the $0.278 reader). Each role now says what is not
  its job (the reader hands counting back), and a prompt budget limits what a wrong route can cost.
- **Contracts between agents need the same care as tools:** a parameter the description said to leave out, but the
  schema required, was refused before the tool ran (FX10); an empty answer needs a follow-up in the same session, not
  a new specialist (FX11).
- **Two models' worth of latency:** every task costs Puls's turn plus the specialist's; worth it for tool-heavy work,
  not for chit-chat (Puls answers those itself).
- **Mistakes found by looking:** `AGENT_FUNCTION=louis` loaded every skill file into Puls's prompt (fixed: skill files
  come with the `skills` toolset); the model guessed a peer name from an example (fixed: an empty name means the only peer).

## Next

Decide how often Puls should delegate (keep research and long work, answer short read-only questions itself?) and
measure it the same way; the F5 roll-up; evals per role for quality, not only cost (F18); and parallel specialists for
a task with independent parts.

---
[Learning](README.md) · [Goal](../GOAL.md)
