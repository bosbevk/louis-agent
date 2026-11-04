# Project design docs

One design doc per project in the solution: what it's for, how it works, its parts, its settings, how to extend it,
how it's tested, and its limits. For the whole system at once, read [Architecture](../architecture/README.md) first; come here
when you're about to change a particular project. Each project's `.csproj` links its design doc and its how-to from
[Guides](../guides/README.md), so both show under `docs/` in the IDE's solution explorer.

## The projects

| Project | Kind | In one line | Doc |
|---|---|---|---|
| `louis-agent.core` | Class library | The engine: tools, skills, the tool loop, providers, bootstrap; names no product (CL15) | [core](louis-agent.core.md) |
| `configured/puls/identity/louis-agent.identity.puls` | Class library | Who Puls is: its voice and modes, its roles, the packs it names (`where-to-buy`, `shop-prices`, `recipes`); its prompts embedded with their paths | [identity.puls](louis-agent.identity.puls.md) |
| `configured/puls/evals/louis-agent.identity.puls.evals` | Class library | What proves Puls: its evals as folders of files, their readers and expectations; only the runner references it | [identity.puls](louis-agent.identity.puls.md) |
| `configured/puls/evals/louis-agent.identity.puls.judges` | Class library | Puls's model judge, the cook's `meal-for-people`, apart from the identity because it needs the harness; only the runner references it | [identity.puls.judges](louis-agent.identity.puls.judges.md) |
| `packs/louis-agent.pack.where-to-buy` | Class library | Where to buy: the `where-to-buy` toolset (a shop's nearest branches on OpenStreetMap, and which is the town's), its guide and its texts | [pack.where-to-buy](louis-agent.pack.where-to-buy.md) |
| `packs-evals/louis-agent.pack.where-to-buy.checks` | Class library | The shop rules evals name; only the runner references it | [pack.where-to-buy](louis-agent.pack.where-to-buy.md) |
| `packs/louis-agent.pack.shop-prices` | Class library | Shop prices: the `shop-prices` toolset (a list estimated from Stats SA's average prices, totals in code), its tables, its store (`SHOP_PRICES_MONGO_URL`), its guide and its texts | [pack.shop-prices](louis-agent.pack.shop-prices.md) |
| `packs/louis-agent.pack.recipes` | Class library | What the recipes are: the `recipes` toolset (search an enabled South African recipe site, read any recipe page), its guide, its setting (`RECIPE_SOURCES`) and its texts | [pack.recipes](louis-agent.pack.recipes.md) |
| `packs-evals/louis-agent.pack.recipes.checks` | Class library | The rule evals name to prove a recipe's link is on a site of the pack's table; only the runner references it | [pack.recipes](louis-agent.pack.recipes.md) |
| `configured/puls/hosts/louis-agent.host-packs` | Class library | Picks what a host runs: the one project that references every identity and pack; `Plugins.FromEnvironment()` builds the `AgentCatalog` a host runs with, from `IDENTITIES` and `PACKS` | [host-packs](louis-agent.host-packs.md) |
| `src/engine/louis-agent.composition` | Class library | The contracts a pack, an identity and the engine share; it names no pack and leans on no project (empty until CL29.1's step 2) | [CL29 plan](../plans/CL29-packs-are-just-tools.md) |
| `src/hosts/louis-agent.cli` | Console app | Chat with the agent in a terminal | [cli](louis-agent.cli.md) |
| `src/hosts/louis-agent.acp-server` | Console app (stdio) | The agent inside JetBrains Rider's AI chat (Agent Client Protocol) | [acp-server](louis-agent.acp-server.md) |
| `src/hosts/louis-agent.api` | ASP.NET Core app | HTTP API with streamed answers, workspace and git endpoints; serves the web app | [api](louis-agent.api.md) |
| `configured/louis-agent/web/louis-agent.web` | Blazor WebAssembly | The browser app: chats, files, changes, branches | [web](louis-agent.web.md) |
| `configured/puls/web/louis-agent.puls` | Blazor WebAssembly | Puls: the general assistant in Afrikaans; a landing page and a three-panel chat that delegates every task | [puls](louis-agent.puls.md) |
| `configured/louis-agent/web/louis-agent.site` | ASP.NET Core app (static Razor) | The engine's public website: the Console theme and two CSS films of how the engine works; references nothing of ours | [site](louis-agent.site.md) |
| `src/hosts/louis-agent.mcp-server` | Console / ASP.NET Core | Hands the agent's tools to any MCP client (no model of its own) | [mcp-server](louis-agent.mcp-server.md) |
| `configured/louis-agent/hosts/louis-agent.orchestrator` | Console app | A second agent that watches a service's errors and has louis-agent fix them (POC) | [orchestrator](louis-agent.orchestrator.md) |
| `tools/louis-agent.usage-probe` | Console app (developer tool) | Prints what a provider reports as token usage and what the ledger makes of it | [usage-probe](louis-agent.usage-probe.md) |
| `src/evals/louis-agent.evals` | Class library | The eval harness: `IEval` and the eval read from files, datasets, trials, the common graders, the judge and its calibration, the runner loop, conformance (F18) | [evals](louis-agent.evals.md) |
| `src/evals/louis-agent.evals.checks` | Class library | The check contract: what a check reads and returns, out of the harness so a pack can reference it (Separated Interface; empty until F49.1 steps 2 and 3, ADR 0056) | [evals.checks](louis-agent.evals.checks.md) |
| `src/evals/louis-agent.evals.protocol` | Class library | The eval protocol v2: the events the runner writes and the API and portal read, records and enums only (ADR 0047) | [evals.protocol](louis-agent.evals.protocol.md) |
| `src/evals/louis-agent.evals.runner` | Console app | The command that lists and runs evals, re-scores a stored run and checks a judge; the API starts it as a child process | [evals.runner](louis-agent.evals.runner.md) |
| `louis-agent.choices` | Class library | `SettingChoice` and `[WireName]`: the one parser for a name that is an enum member; references nothing ([ADR 0043](../adr/0043-repeated-names-are-enums.md)) | — (to write) |
| `louis-agent.session-stream` | Class library | Reads a session's server-sent events into records, for core's sub-sessions, the web apps and the eval runner | — (to write) |
| `src/hosts/louis-agent.api.contracts` | Class library | The API's replies and bodies as records, and each wire's options object, shared by the API and its clients ([ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md)) | — (to write) |
| `src/web/louis-agent.web.shared` | Razor class library | What both web apps share: chat models, the API client, the turn and delegation views, markdown, usage and currency | [web.shared](louis-agent.web.shared.md) |
| `tests/louis-agent.*.tests` | NUnit | The tests of core, the API's sessions, the orchestrator, the ACP server, Puls's prompts and golden, the packs, the composition root and the three web projects (bUnit); `tests/louis-agent.testing` and `tests/louis-agent.web.testing` hold the fakes they share | [tests](tests.md) |

## How they depend on each other

```mermaid
flowchart BT
    Core["louis-agent.core"]
    CLI["louis-agent.cli"] --> Core
    ACP["louis-agent.acp-server"] --> Core
    API["louis-agent.api"] --> Core
    API --> Web["louis-agent.web<br/>(served by the API)"]
    API --> Puls["louis-agent.puls<br/>(served by the API built with -p:WebApp=puls)"]
    Web --> Shared["louis-agent.web.shared"]
    Puls --> Shared
    API -. "HTTP + SSE<br/>(sub-sessions on a peer)" .-> API
    MCP["louis-agent.mcp-server"] --> Core
    Orch["louis-agent.orchestrator"] --> Core
    Probe["louis-agent.usage-probe"] --> Core
    Orch -. "HTTP + SSE<br/>(sessions)" .-> API
    Comp["louis-agent.composition"] --> Core
    Comp --> Puls_id["louis-agent.identity.puls"] & WhereToBuy["louis-agent.pack.where-to-buy"] & ShopPrices["louis-agent.pack.shop-prices"] & Recipes["louis-agent.pack.recipes"]
    Puls_id --> Core
    WhereToBuy --> Core
    ShopPrices --> Core
    Recipes --> Core
    CLI & ACP & API & MCP --> Comp
    CoreTests["louis-agent.core.tests"] --> Core
    OrchTests["louis-agent.orchestrator.tests"] --> Orch
    ApiTests["louis-agent.api.tests"] --> API
```

Solid arrows are project references; dotted ones are runtime calls. Only `louis-agent.core` and the test projects are
referenced by other projects, and `louis-agent.core` references none of them. Behaviour lives in core; every host is a
thin adapter between its protocol and `AgentEngine`.

## Where each one runs

| Project | Run it with | Docker service |
|---|---|---|
| core | (library) | — |
| cli | `dotnet run --project src/hosts/louis-agent.cli` | `local-agent` |
| acp-server | Rider starts it per chat | `acp-server` (`docker compose run --rm -T acp-server`) |
| api + web | `docker compose … up -d api` → http://127.0.0.1:5080 | `api` |
| mcp-server | an MCP client starts it (stdio), or HTTP with `MCP_TRANSPORT=http` | `mcp-server` |
| orchestrator | `dotnet run --project configured/louis-agent/hosts/louis-agent.orchestrator` (its demo returns with the benchmarks) | — |

## Each doc covers

**Purpose** · **How it works** · **Structure** (files and what's in them) · **Key types** · **Configuration** ·
**Extending it** · **Tests** · **Limits and plans** (with links to [features](../features/README.md) and the
[TODO](../../TODO.md)) · **Related docs**.

---
[Docs index](../README.md) · Next: [louis-agent.core](louis-agent.core.md)
