# 0099. The guide answers without a sign-in, within a cap

**Status:** Accepted · **Date:** 2026-11-08 · **Feature:** F109.3

## Context

Every call under `/v1` needs the agent's key or a signed-in person (FX18, F61.2), and F107 says a guest signs in
before the chat. The site's corner holds a guide, the expert identity on the docs pack, meant for a visitor who has
neither: the site is the demo of what is sold ("do you want a website with an agent that knows your business"), and
a guide behind a sign-in is a guide most visitors never ask. Louis's pick (2026-11-08, C1 of three): the guide
answers without a sign-in, within a cap per address per day; not behind a sign-in, never a key in the page.

## Decision

- **A host may name one open identity** (`OPEN_IDENTITY`) and **a cap** (`OPEN_SESSIONS_PER_DAY`); unset, or a cap
  of 0, the door is shut and nothing changes. The default is shut.
- **The door is a guard beside the key gate, not inside it:** the gate asks the door first; the door admits a
  visitor's `POST /v1/sessions` that names the open identity, under the day's cap for that address (the first
  forwarded address, else the connection's), and the calls on a session it admitted; for anything else it says it's
  not its business and the key's rules stand.
- **The session is narrowed in code:** an admitted request opens as the open identity with the toolsets of that
  identity's own packs and nothing more, whatever the body asked, tagged `open-door` on the ledger so its cost reads
  apart. A route stays closed until the code opens it (ADR 0039, rule 6), and so does a toolset.
- **Past the cap, a 429** with `rate_limit_error` and a plain sentence; the admin routes and every other identity
  stay behind the key.
- **Counts and admitted sessions live in memory:** one replica runs, and a restart forgives the day, the cheap side
  to err on. The spend cap on the model key stays the ceiling of what the door can cost.

## Consequences

- A visitor on `louis-agent.dev` asks the guide and gets an answer from the docs, no account, a few times a day.
- Behind a proxy that doesn't forward the address, every visitor shares one cap; Container Apps forwards it.
- A second replica would keep its own counts; a shared store is the change if that day comes.
- F107's rule narrows to the chat: the guide is open by this decision, the chat is not.

## Links

- [ADR 0039](0039-api-routes-under-v1.md), [ADR 0027](0027-engine-identities-and-packs.md), [ADR 0080](0080-a-tool-when-it-improves-the-prompt.md)
- The feature's record: [ADR 0100](0100-the-site-tells-the-whole-architecture.md) and [the work log](../work-log.md); the tests: `OpenDoorTests`
- [ADR 0116](0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md) (F127.2): the door takes a list of
  identities, the showroom's businesses beside the guide, under one cap

---
[Docs index](../README.md) · Related: [ADRs](README.md)
