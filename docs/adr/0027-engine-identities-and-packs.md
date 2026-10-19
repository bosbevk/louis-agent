# 0027. The engine, identities and packs: core names no product

**Status:** Accepted · **Date:** 2026-10-19 (decided in the [platform plan](../plans/platform.md), Phase 2 CL15) ·
**Recorded:** 2026-10-19

## Context

Puls grew inside core: 10 of its 26 prompts, the shop tools and the South African time zone default live in
`louis-agent.core`, so every product built on the engine carries Puls, and a change for Puls is a change to the engine.
Louis: "we have to make a clear cut off when it comes to which is the agent and which is Puls". The engine is also what
we plan to sell, which a product-shaped core can't be.

Considered: a copy of the engine per product (two engines drift apart); packs loaded as DLLs from a folder at start-up
(F37: someone else's code inside our process; cut, see [0028](0028-packs-stay-convertible-to-mcp.md)); doing nothing.

## Decision

- **Core is the engine:** the loop, sessions, usage and prices, guards, the contracts, and the tools every identity
  needs. It names no product and no country.
- **An identity** (`IAgentIdentity`) is who an agent is: its voice, modes and roles, and the packs it uses, named by
  name. Puls, the demo, the coding agent and voorman are identities.
- **A pack** (`IAgentPack`) is what an agent can do: toolsets, providers, guides, settings, and the packs it requires,
  by name. A pack never references another pack or an identity. An identity may hold workflow code that uses its own
  packs' public types (voorman's `TriageWorkflow`). `ArchitectureTests` holds both.
- **One composition project**, `src/louis-agent.composition`, knows them all; a host chooses with `IDENTITIES` and
  `PACKS` at start-up, and an `AgentCatalog` object is passed down, not a static.

## Consequences

- A product is configured, not coded: a new identity or pack adds no branch to core.
- Core's defaults change: the time zone comes from one `HOST_TIME_ZONE` setting, not SAST.
- An image may carry an identity's assembly and load none of it unless `IDENTITIES` names it.
- Identities and packs plug in at start-up only, so a chat's tools never change under it
  ([0006](0006-toolsets-per-session.md), [0008](0008-history-is-append-only.md)).

## Links

[Platform plan](../plans/platform.md) (Part 2, Phase 2) · [0028](0028-packs-stay-convertible-to-mcp.md) ·
[0011](0011-prompts-are-content.md) · [0019](0019-modes-one-file-per-product.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
