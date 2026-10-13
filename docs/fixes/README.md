# Fixes: what broke, why, and how it was fixed

Features add something; fixes repair something that didn't work. Each fix gets a short doc here: the symptom as it was
seen, the cause once found, the fix, how it is tested and how it was confirmed. They are kept, done or not, so the next
person who sees the same symptom finds the cause, and so the [goal](../GOAL.md)'s skills have their real lessons on
record (most fixes so far taught something about a model's API).

## Fixes

| ID | Fix | Status | Area | Found | Commit(s) | Removes the limitation |
|---|---|---|---|---|---|---|
| [FX1](FX01-round-limit-tools.md) | Keep the tools on the request after the 10-round limit | done | core: tool loop | Short demo on Haiku 5.5 lost 2 of 4 fixes (2026-10-10) | `d9c7c45` | A message that uses all 10 tool rounds fails on Haiku 5.5 |
| [FX2](FX02-probe-ollama-404.md) | The usage probe says which model to pull on an Ollama 404 | done | tools: usage probe | Probe run on the Ollama profile (2026-10-10) | `a4e9f7f` | — |
| [FX3](FX03-ollama-container.md) | The Ollama container starts and pulls the profile's model | done | Docker: ollama service | Starting Ollama for the usage probe (2026-10-10) | `882fdde` | — |
| [FX4](FX04-append-only-history.md) | Keep conversations append-only, so changing the tools or skills mid-chat doesn't break Haiku 5.5 chats | open | core: history, tool list | Haiku 5.5 migration guide and the code (`CreateTool` mid-message, from the PR #9 review) (2026-10-10) | — | Changing the tools or skills during a chat can break it on Haiku 5.5 |
| [FX5](FX05-null-conditional-ledger.md) | Requests outside a usage scope are written to the ledger again (a `?.` skipped the write) | done | core: usage recorder | F1's tests while building F3 step 1 (2026-10-11) | `3269fea` | — |
| [FX6](FX06-usage-on-failed-turns.md) | A turn that fails still reports what it used (usage on the `error` event) | done | API stream, web app, orchestrator | Writing F3 steps 5 and 7 (2026-10-11) | `75de390` | — |
| [FX7](FX07-timeout-ends-run.md) | A timed-out request no longer ends the orchestrator's run (only the user's stop does) | done | orchestrator: triage loop | F3's split demo test, triage on Ollama (2026-10-11) | `f2d38d2` | — |
| [FX8](FX08-fix-result-quotes.md) | A FIX-RESULT with quotes in its summary is still read (not answered with "continue") | done | orchestrator: FIX-RESULT parsing | F14's full demo rerun, error 10 (2026-10-11) | `6307226` | — |
| [FX9](FX09-cancelled-request-usage.md) | A request cancelled without streaming is still written to the ledger | open | core: usage recorder | Short demo with triage on llama3.1, a timed-out request missing (2026-10-12) | — | — |
| [FX10](FX10-optional-peer-name.md) | A sub-session call without `agent` uses the only peer (it was refused) | done | core: `agents` toolset | Puls delegation check with roles, 3 refused calls (2026-10-13) | (this commit) | — |
| [FX11](FX11-empty-sub-session-answer.md) | A sub-session that ends without an answer is asked once more, in the same session (not started over) | done | core: `agents` toolset | Puls delegation check with roles, numbers-2 at a third of the run's cost (2026-10-13) | (this commit) | — |
| [FX12](FX12-env-templates-readable.md) | `.env` templates (`.env.example`) can be read; the real `.env` files stay blocked | done | core: `SensitiveFiles` | Puls delegation check with roles, files-2 refused in both runs (2026-10-13) | (this commit) | — |
| [FX13](FX13-counting-to-the-analyst.md) | Counting goes to the analyst (a script), and the reader hands it back instead of reading every file | done | skills: routing, reader role | Delegation check after FX10–FX12, numbers-2 at $0.278 (2026-10-14) | (this commit) | — |

## Conventions

- **IDs** are `FXn`, in the order the problem was found; a doc is `FXnn-short-name.md`.
- **Each doc** has: *Symptom* (what was seen, with the exact error), *Cause* (proved, not guessed: how it was found),
  *Fix* (what changed and why this way), *Tests*, *Confirmed* (the real run that shows it works), and *Lesson* (the
  concept behind it and its skill in the goal's map).
- **Status** is *open* or *done*, the same here and at the top of the fix's doc.
- **Plans:** a fix that takes more than one step gets its approved plan in [plans/](../plans/README.md)
  (`FXnn-name.md`), committed before its first step.
- **An open fix** also has one line in the [TODO](../../TODO.md)'s backlog that links here; a fix that removes a known
  limitation names it.

## Finishing a fix

In the commit that fixes it, or the one right after:

1. **This folder:** *Status* to **done** here and in the doc; fill in *Fix*, *Tests* and *Confirmed*; add the commit.
2. **TODO:** remove its backlog line and add a one-line entry at the top of *Done*.
3. **Limitations:** if it removes a known limitation, delete it from both [docs/CLAUDE.md](../CLAUDE.md#known-limitations)
   and the TODO's *Known limitations*.
4. **Docs:** anything the fix changes for users (settings, MODELS, SETUP).

---
[Docs index](../README.md) · Related: [Features](../features/README.md) · [Plans](../plans/README.md) · [TODO](../../TODO.md)
