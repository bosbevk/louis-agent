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

## A credential the page can't read (F61.1, 2026-11-07)

**What was built.** The framework's accounts: ASP.NET Core Identity in `louis-agent.accounts`, signing in with a
cookie, a tenant on every account from the host name, SQL Server behind EF Core ([ADR 0087](../adr/0087-identity-is-the-identity-server.md),
[ADR 0088](../adr/0088-a-tenant-on-every-account.md)). Skill 15, and skill 9's first half: who a session belongs to.

**Where the credential lives is the whole question.** A bearer token the browser keeps in its storage is readable by
any script on the page, including one an attacker slips into a markdown answer. A cookie marked HttpOnly never
reaches a script at all; the browser sends it, and only the browser. The price is that the browser also sends it on a
form another site posts to us. Two rules close that: the cookie is SameSite Lax (a cross-site POST doesn't carry it),
and the agent's routes will take the cookie only with a header a cross-site form can't set (F61.2). A cookie that
isn't there gets a 401, never a redirect to a login page: the API answers JSON and the page draws itself.

**Fail closed, twice.** The user store refuses to look anyone up when the request's tenant isn't set, so a route
mapped before the tenant middleware fails loudly rather than landing in the wrong tenant; and the routes refuse to
map until the middleware is in, the same shape as the API's key check. Both are the same lesson as FX18: a host that
can't be sure says no.

**The log is the mail, carefully.** Until a mail sender is picked, a confirmation link goes to the log. On a hosted
instance that line would be a sign-in anyone could use, so it goes through `LogContent` like every personal value:
a keyed hash unless `LOG_CONTENT=full` on a developer's own machine.

**What the tests taught.** Identity writes a link for an HTML mail, `&amp;` and all; following it raw gave a 400 with
no body, the kind of failure only a test that follows the real link finds. And `puls` can't be a tenant in a test:
it's an identity's wire name, and the names rule (ADR 0043) is right to flag it.

## A cookie that counts only on the app's own call (F61.2, 2026-11-07)

**What was built.** The framework's sign-in, registration, reset and profile pages, and the agent's key gate taking
a signed-in person in place of `AGENT_API_KEY`.

**The header is the whole trick.** A browser sends our cookie on any request to our address, a form on another site
included. It adds a custom header only to a request the page's own script made, because a cross-site form can't set
one and a cross-site script is stopped by the browser's same-origin rule. So the gate's rule is two things, not one:
the cookie names the person, and `X-Requested-With: louis-agent.web` proves the call is ours. `HostAuth.IsSignedIn`
holds it as a pure function beside `HasKey`, and the test is three lines: cookie alone 401, header alone 401, both
through.

**Where a sign-in page may send you.** `?returnUrl=` is the page the person came from; a sign-in page that forwards
anywhere is a phishing tool. `SafeReturn` follows a relative path of ours and nothing else: not `//evil.test`, not
`https://…`, not `javascript:`. The test lists the shapes an attacker would try.

**Say the same thing either way.** Forgot-password and resend-confirmation answer the same whether or not the address
has an account, on the server and on the page, so neither can be used to find out which addresses do.

**What the tests taught.** Identity answers `Results<ContentHttpResult, UnauthorizedHttpResult>`, a wrapper, so a
filter that switches on the answer's type must unwrap `INestedHttpResult` first; the redirect never fired until it
did. And a query parameter in a Blazor page can't be passed as a parameter in a test; the test navigates with the
query, as a browser would.
