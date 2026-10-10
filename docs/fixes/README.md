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
| [FX4](FX04-append-only-history.md) | Keep conversations append-only, so a mid-chat skill reload or tool approval doesn't break Haiku 5.5 chats | open | core: history | Haiku 5.5 migration guide; FX1 is the same rule (2026-10-10) | — | Reloading skills or approving a tool mid-chat can break the chat on Haiku 5.5 |

## Conventions

- **IDs** are `FXn`, in the order the problem was found; a doc is `FXnn-short-name.md`.
- **Each doc** has: *Symptom* (what was seen, with the exact error), *Cause* (proved, not guessed: how it was found),
  *Fix* (what changed and why this way), *Tests*, *Confirmed* (the real run that shows it works), and *Lesson* (the
  concept behind it and its skill in the goal's map).
- **Status** is *open* or *done*, the same here and at the top of the fix's doc.
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
[Docs index](../README.md) · Related: [Features](../features/README.md) · [TODO](../../TODO.md)