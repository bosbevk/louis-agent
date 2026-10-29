# 0037. One agent API; products get a front door: the API names no product, and what is a product's lives at its own door

**Status:** Accepted · **Date:** 2026-10-24 (Louis, *One agent API*, the platform plan's review) · **Recorded:**
2026-10-28, with F18 step 7b (iii)

## Context

`louis-agent.api` is the one host every agent runs behind: the coding agent, Puls, the orchestrator's peer, and any
product a customer builds on the engine ([0027](0027-engine-identities-and-packs.md)). Core already names no product;
the API did. Its eval code said `Benchmarks/` and `PulsCalls`, it started the eval runner with `PULS_API`, and its store
defaulted to the database `puls_benchmarks`. Each was harmless alone, but together they meant a second product would
inherit Puls's names, read its database by default, and need the API changed to get its own. The F18 plan's step 7b
(Louis, 2026-10-20 and 2026-10-24) asked for every eval to be visible in one admin, for any identity or pack, and for
the API to name no product.

A product still has things that are only its own: its web app and its brand, its chat gateway (Phase 9's WhatsApp
door), its compose file and cloud app, the database its runs have always been in, its language. Those need a place
that isn't the API.

Considered: a host project per product, each with its own copy of the endpoints (rejected: every fix lands N times,
and the admin can't list every eval in one place); a product switch inside the API (`if identity == puls`, rejected:
the API would grow a branch per product, which [0003](0003-provider-strategies.md) already refuses for providers);
leaving the names as they are until a second product arrives (rejected: the rename then has stored data and links on
two products to carry, not one).

## Decision

- **The API is the one agent API.** Its code, its project files and its contract (`louis-agent.api.contracts`) name
  no product: no identity's name in a type, a setting, a route, a stored field, a default or a comment. What it says
  about an agent, it says for every agent (`AgentCalls`, `AGENT_API`, `/admin/evals`, the database `evals`).
- **A product gets a front door.** What is only a product's lives where that product is put together and reached:
  its web app (`louis-agent.puls`), its compose file and cloud app (`docker/docker-compose.puls.yml`,
  `deploy/azure/`), its run profile, its identity and packs. That is where Puls names its database
  (`EVALS_MONGO_DATABASE: puls_benchmarks`), its peers and its voice; the API reads them as settings.
- **Renames keep what is stored and linked** (expand, then contract): a new name answers beside the old one, stored
  data is read under either name, and a TODO line removes each alias once nothing uses it.
- **A test holds it.** `ApiNamesNoProductTests` reads the products from `identities/louis-agent.identity.*` and fails
  on any of their names in the API, with two exceptions listed in the test: the project file's choice of web app
  (until Phase 9 gives each product its own host), and `puls_calls`, read from turns stored before the rename and
  still the eval protocol's field until CL22 versions it.

## Consequences

- A new product needs no change to the API: an identity, its packs, and its own front door.
- The admin lists every eval, whoever's it is, from one catalogue.
- Aliases live for a while, each with a TODO line: the `/admin/benchmarks/…` routes, `BENCHMARKS_MONGO_*`,
  `PULS_API`, and the stored `puls_calls`.
- A product's defaults move out of the API: an instance that had runs under the old default names its database, or
  starts an empty `evals` one.

## Links

[0003](0003-provider-strategies.md) · [0022](0022-benchmarks-recorded-from-the-protocol.md) ·
[0027](0027-engine-identities-and-packs.md) · [0039](0039-api-routes-under-v1.md) ·
[0045](0045-typed-answers-and-a-shared-contract.md) · [F18 plan](../plans/F18-evals.md), step 7b ·
[decisions log](../plans/F18-unattended-decisions.md), findings 19 to 23

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
