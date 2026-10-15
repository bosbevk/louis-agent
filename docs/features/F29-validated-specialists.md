# F29 · Validated specialists: only a role that passed its check is on

> **Status:** planned (2026-10-15), on `claude/project-thread-381vmw` · **Milestone:** — · **Depends on:** F5
> (sub-sessions), F21 (roles), F25 (the cook, the first validated role), F27 (the checks the portal runs)
>
> **In the TODO:** **F29 Validated specialists** under *Sessions and memory*, beside F21 and F25 ([TODO](../../TODO.md)) ·
> **Plan:** [plans/F29](../plans/F29-validated-specialists.md)

Puls has six roles, and only the cook has been through the process that makes a role trustworthy: a report in a
fixed shape, Puls handing it on whole, and a check that scores it (F25). The others answer in whatever shape they
choose, and nothing measures them. Louis's decision (2026-10-15): switch the other specialists off and let Puls boot
with `web` itself, so it answers the web on its own and hands only cooking to the cook, until each other role has
been through the same validation.

## User stories

**F29-S1 — Only validated roles can be used.** As an *operator*, I want to say which personalities an instance may
use, so that a role nobody has validated can't be reached, by a prompt slip or by hand.
- *Given* `PERSONALITIES_ENABLED=puls,puls-cook` *when* a session or sub-session asks for `puls-researcher` *then*
  it's refused as an unknown personality is (a 400 before any engine is built), and `GET /personalities` lists the
  two.
- *Given* the setting is empty *then* every personality on disk is available, as today (the code-review app, the
  tests).

**F29-S2 — Puls answers the web itself and delegates only cooking.** As a *user*, I want a web question answered by
Puls directly and a cooking question by the cook, so that I get the validated path for food and no unvalidated
specialist for the rest.
- *Given* a chat *then* its session has `agents` and `web`.
- *Given* a question about the world *then* Puls searches and answers itself, with its sources.
- *Given* a question about my files, their history, running code *then* Puls says that isn't available yet, instead
  of handing it to a role that's off.

**F29-S3 — The checks expect it.** As *Louis*, I want the delegation check to score Puls answering a web question
itself as right, and delegating a non-cook question as wrong, so that the checks describe Puls as it is.

## Design notes

- **A gate in code, not only in the prompt.** `PERSONALITIES_ENABLED` (a list, comma separated; empty means all)
  filters `AgentEngineFactory.AvailablePersonalities` and makes `GetOrCreate` refuse a name not on it with the same
  `PersonalityException` as an unknown one, naming what's available. Puls's compose sets it on both containers, so a
  `StartSubSession` naming a role that's off fails at the peer and comes back as a tool error Puls has to report.
- **The prompts say it.** The router's role column keeps the cook and makes the web row Puls's own work; the other
  rows say *not yet*, and a line says a role marked so is never delegated and a toolset Puls doesn't have is said to
  be unavailable for now. The agents guide's roles table keeps the cook and lists the others under *Waiting for
  validation*, with the process: a report shape, a relay rule, a check.
- **The roles' files stay** on disk and embedded; `roles.json`'s fallback stays for a peer that allows them. Turning a
  role back on is one setting, once its check passes.
- **The checks:** a non-cook question expects no delegation (Puls with `web` answering itself is right); the
  delegation check's delegated mode is `agents` and `web`; the cook check's `puls-web` mode is Puls as built. Saved
  samples keep the scores they were recorded with.

## Done when

- **F29-S1:** factory tests (the list filters; a name off the list is refused before anything is built; empty means
  all); the API test on `GET /personalities`.
- **F29-S2:** the chat opens with both toolsets (by hand); prompt tests; the golden request snapshot regenerated.
- **F29-S3:** both checks' scorers; `--report` on the saved samples unchanged.

---
[Features](README.md) · [Plan](../plans/F29-validated-specialists.md)
