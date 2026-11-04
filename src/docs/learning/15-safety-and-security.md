# 15 · Safety and security: what Phase 1 taught

> **Skill:** #15 in the [goal](../GOAL.md)'s map, with #16 (human in the loop) for FX20 · **Stages:** the platform
> plan's Phase 1: FX18 auth fails closed, FX19 git arguments as data, FX20 skill files wait for approval (2026-10-19) ·
> **Written by** Claude, for Louis to read (2026-10-19) · **Next:** one untrusted-content frame, prompt-injection
> simulations, an audit log and rate limits (F42, Phase 7)

What this answers: which three doors Phase 1 closed before the platform cut, and the rule behind each, so the same
mistake isn't made in a new host, tool or pack.

## Before and after

None of the three was seen in a run; each was found by reading the code for the platform plan, and proved by a test
that failed on the old code.

| Door | Before | After | Rule |
|---|---|---|---|
| A host with no key (FX18) | The API and the MCP server over HTTP logged a warning and served everyone, including an agent that runs shell commands | The host refuses to start without a key, unless `AGENT_AUTH=off` says so on purpose | Deny by default (OWASP A01:2021) |
| A value from the model reaching git (FX19) | `git show --output=<file>` wrote a file anywhere; `SetConfig("core.fsmonitor", …)` made the next `git status` run a command | Every value after `--end-of-options` (or `--` for paths), a value starting with `-` refused before git runs, config limited to `user.name` and `user.email` | Arguments as data (OWASP LLM05:2025; CWE-88) |
| What the agent writes for later (FX20) | `CreateSkillFile` loaded its file at once for every later session, and a skill runs bash, PowerShell or Python | It waits in `pending/` until a person types `/approve`; only the engine that wrote it runs it meanwhile | Memory poisoning needs a person in between (OWASP Agentic ASI06) |

## What the three have in common

- **The model is an input, not a user.** Every door was a place where text the model produced, possibly steered by a
  page it read, became something the system trusted: a git option, a config key, a script for every later session. The
  guard belongs where that text crosses into something that acts.
- **Fail closed, and loudly.** A warning that a door is open is not a guard; the operator who never reads the log never
  closes it. FX18's host throws before it listens, the way it already stopped on a bad price table.
- **Allow-lists beat deny-lists.** `SetConfig` doesn't refuse the four keys we know run commands; it allows the two it
  needs. A deny-list is as good as the attacker's imagination is poor.
- **The fix already existed nearby.** The admin gate already failed closed; `RunGitList` already passed a list;
  agent-built tools already waited in `pending/`. Each fix brought the odd one out in line with what the code already
  did well, which is why each was one step.
- **Intent in the prompt, enforcement in code.** The principles file says never to cheat or steal; it stops nothing.
  The three guards are code, tested without a model.

## What the tests taught

- **A test for a guard has to fail for the hole, not for a side effect.** A branch named `x --upload-pack=…` is refused
  by git whatever we do, because it isn't a valid name, so a test with it passes with the hole open. The test that
  proved FX19 was the file `git show --output` wrote outside the repository.
- **Unset and empty are different values.** FX18's review found that a bare `MCP_API_KEY=` in an env file is `""`,
  which `??` never falls back from: the MCP server refused to start with `AGENT_API_KEY` set, or under `off` started
  open with a key there. Every place a key is read now goes through `HostAuth.Key`, which treats blank as missing.
- **A value can be more than a name without being an option.** FX19's review found `+main` (a forced push) and `:main`
  (deletes the remote branch) slipping past both guards; `Push` and `Pull` now refuse a refspec.
- **The platform hides some bugs.** `/approve Greeting` found `greeting.md` on Windows, whose file system ignores case,
  and failed on Linux. The review caught it, and the test now pins it on both.

## Trade-offs accepted

- **A local stack now needs a key** (or `AGENT_AUTH=off` in `config/.env`), one more setting for Louis's machine, so
  that no hosted one is ever open by accident.
- **`SetConfig` lost most of its reach.** The model can't set a remote's URL or an alias any more; a person does that.
- **Approval adds a step,** and a pending skill file is active in every chat on the engine that wrote it, since chats
  with the same toolsets share one engine (F5). Until FX4 (Phase 7), approving or rejecting changes that engine's
  prompt, so start a new chat afterwards (known limitation 9).

## What didn't work (and what it taught)

- **Puls chats with the skills toolset.** A commit on FX20's branch gave Puls's chats `skills`, and Louis reverted it.
  The API takes `/approve` from any chat message, and Puls's chats share one engine, so any user could approve a script
  every other user's chat would run. Who may approve is part of the guard: in a hosted product `/approve` belongs to an
  operator (the admin portal), never to a chat user. [ADR 0033](../adr/0033-what-the-agent-writes-has-a-scope.md) sets
  the scopes (session, agent, shared); who approves each is still to be written into it.
- **Chrome's URL blocking didn't stop WebSockets** (F32, before this phase, the same lesson): a guard is proved by a
  live test of the attack, not by the setting's name.

## Where skill 15 stands

GOAL.md's row now lists Phase 1's three under *Have today*. Still planned: personal data out of the logs (Phase 3); one
untrusted-content frame for every outside result, prompt-injection simulations as the red-team suite, output guardrails
and moderation, rate limits, an audit log and the browser's egress proxy, which closes F32's DNS-rebinding gap (F42,
Phase 7); tenants, a sandbox per tenant and POPIA (F43, Phase 9).

## What I can now explain

- Why a host should refuse to start rather than warn, and why `off` is a setting no compose file sets.
- Why an argument list stops splitting but not options, and what `--end-of-options` adds.
- Why an allow-list of two config keys is safer than a deny-list of four.
- Why what an agent writes for later sessions is untrusted until a person approves it, and why that person must be an
  operator in a hosted product.
- Why a security test has to fail for the hole itself.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md)
