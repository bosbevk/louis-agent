# 0089. The account client is a library a site opts into

**Status:** Accepted · **Date:** 2026-11-07 (Louis: "should we not rather have louis-agent.web.accounts"; "A, finish
step 5 first then do the move") · **Recorded:** 2026-11-07

## Context

F61.2, F61.3 and F61.5 put the account pages, their client, words, styles and passkey script in `louis-agent.web`,
the framework's web that every site is built on (ADR 0084): 56 files, a reference to the accounts contracts and to
the Authorization package. Outside those folders only the chat header's profile menu touched accounts, by looking the
services up by hand so a site without them still drew the guest's menu. On the server the split is already made:
`louis-agent.api` doesn't reference `louis-agent.accounts`; a program composes the two (ADR 0074, 0076: the folders
say what ships).

## Decision

The account client is its own Razor class library, `louis-agent.web.accounts`, on top of `louis-agent.web`, never
the other way round. A site that has accounts references it and calls `AddLouisAgentAccounts()`, as its API references
`louis-agent.accounts`; a site that doesn't ships no Identity client, no Authorization package and no passkey script.
What the framework's web needs from accounts is one named seam, `IAccountMenu`, declared beside the menu that asks it
(Separated Interface) and implemented in the library; without the library the menu holds `NoAccounts` (Null Object)
and draws the guest's menu. The library's folders set its namespaces (`Client/`, `Components/`, `Pages/`, `Layout/`,
`Text/`); its Sass is under `accounts/` as the framework's is under `web/`; its script is served from
`_content/louis-agent.web.accounts/`. The framework's rules (names no site, markup only, no fixed colour, every
script call in its file) read both libraries.

Weighed and not taken: the library underneath `louis-agent.web` (cleaner injection, but every site would ship
accounts, and the web would know Identity's shapes as the API deliberately doesn't); staying put (nothing to move, but
every site carries the client and the web grows with each account feature).

## Consequences

- A site opts in with a project reference, two `@using` lines, `@forward "accounts/shell"` with the library's
  `Styles` on its include path, the script line in `index.html`, and a `using` in `Program.cs`; the starter has them.
- The chat's menu knows accounts only through `IAccountMenu`; a test of the chat registers nothing.
- An account feature lands in the library, with its tests in `louis-agent.web.accounts.tests`; `louis-agent.web`
  gains nothing of accounts but a change to the seam.
- The three framework scripts a site lists are the next cleanup: an ES module beside each component.

## Links

[louis-agent.web.accounts](../projects/louis-agent.web.accounts.md) (the plan, CL30, was deleted when the work was done) ·
[ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md) · [ADR 0074](0074-the-folders-say-what-ships.md) ·
[ADR 0087](0087-identity-is-the-identity-server.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
