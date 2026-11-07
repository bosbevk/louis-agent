# 0098. The demo's app runs beside Puls, split later

**Status:** Accepted · **Date:** 2026-11-08 · **Feature:** F109.2

## Context

The framework's site became the louis-agent demo's web app (F109.1) and needs to be on `louis-agent.dev` from Azure,
as Puls is on `mypuls.co.za` (ADR 0095). The F61 plan had given it a resource group and an environment of its own.
Louis, while F61.4 was going live (2026-11-07): "we will have to deploy louis-agent to the same VM, to save money";
"can be split later"; "louis-agent goes into the same directory". The VS Pro credit is about R900 a month, and a
second environment, VM and SQL server would double the fixed part of the bill for a demo nobody pays for yet.

## Decision

- **One build, two apps.** `demos/puls/config/azure/main.bicep` declares both apps' needs and `demos/puls/deploy/azure/`
  holds both apps' yaml and the one script; every verb that touches an app takes `-App puls-app` (the default) or
  `-App louis-agent-app`, and a table at the top of the script says what is each app's.
- **Shared:** the resource group, the network, the environment `puls-env` (so the two apps share its static IP and
  certificates' validation id), the VM with Mongo, the browser and SearXNG, the SQL server and the database
  `puls_accounts` (an app is a tenant, ADR 0088), the vault, the registry, the storage account, the model key.
- **Each app's own:** its identity on the vault, its file share and environment storage, its image, its yaml, its
  API keys and sign-in providers (the vault names carry the suffix `-louis-agent`), its domain, its eval database on
  the shared Mongo, its tenant.
- **The deploy workflow releases both** from one merge after green CI, each tried even when the other fails
  (ADR 0097).
- **Split later:** when the demo's app is sold or outgrows the shared VM, its declarations move to a Bicep module and
  a folder of their own under `demos/louis-agent/`, and `-App` goes with them; nothing in an app knows it shares.

## Consequences

- The month's fixed cost stays one VM, one environment and one registry; the second app adds a share of 5 GB and a
  replica only while someone is on it.
- A change to the shared parts (the VM's compose, the network) touches both apps at once; the plan's split is the
  way out when that stops being acceptable.
- The two apps' sign-in providers are registered twice, once per domain, since an OAuth client is bound to its
  redirect address.
- Puls's folder holds a file named for another demo until the split; the test `DeployFilesTests` and this ADR say
  why.

## Links

- [ADR 0095](0095-puls-runs-from-azure-and-nothing-points-at-the-pc.md), [ADR 0097](0097-puls-deploys-from-github-without-a-password.md), [ADR 0088](0088-a-tenant-on-every-account.md)
- [The deploy README](../../../demos/puls/deploy/azure/README.md), [the architecture](../../../demos/puls/deploy/azure/ARCHITECTURE.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
