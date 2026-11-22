# Project design docs

![The project design docs: one per project, for the engine, the hosts, the web, identities and packs, evals and tests, and the developer tools](diagrams/README.svg)

One design doc per project in the solution: what it's for, how it works, its parts, its settings, how to extend it,
how it's tested, and its limits, with the pictures its kind of project asks for: its overview and its interface
design always, and what its kind adds, such as a library's main path or where a host runs. Its opening quote says
its kind and what it shows beyond that ([Draw a diagram](../guides/draw-a-diagram.md#step-3-link-it)). For the whole system at once, read [Architecture](../architecture/README.md) first; come here
when you're about to change a particular project. Each project's `.csproj` links its design doc and its how-to from
[Guides](../guides/README.md), so both show under `src/docs/` in the IDE's solution explorer.

## The projects

| Project | Kind | In one line | Doc |
|---|---|---|---|
| `LouisAgent.Core` | Class library | The engine: the tool loop and its guards, skills and prompts, usage, the seam to a model's provider, bootstrap; names no product, and ships no tool and no model SDK (CL15, ADR 0076) | [core](LouisAgent.Core.md) |
| `LouisAgent.Core.Web` | Class library | Web access on core (CL33): the browser, the fetcher, the robots policy, search, rendering and the recorders; a host hands it in through `HostServices` | [core.web](LouisAgent.Core.Web.md) |
| `LouisAgent.Core.Telemetry` | Class library | Telemetry on core (CL33): the OpenTelemetry tracer and exporter and the collector's log sink; a host hands it in through `HostServices` | [core.telemetry](LouisAgent.Core.Telemetry.md) |
| `LouisAgent.Core.Anthropic` | Class library | Claude on core (CL57): the provider strategy, the cache-write mapper, the two prompt-cache markers; a program hands `AnthropicProvider.Instance` in through `HostServices` | [core.anthropic](LouisAgent.Core.Anthropic.md) |
| `LouisAgent.Core.Ollama` | Class library | A local model through Ollama on core (CL57), with the long timeout a small GPU needs | [core.ollama](LouisAgent.Core.Ollama.md) |
| `LouisAgent.Core.OpenAi` | Class library | Any server that speaks OpenAI's chat API, on core (CL57) | [core.openai](LouisAgent.Core.OpenAi.md) |
| `demos/puls/identity/LouisAgent.Identities.Puls` | Class library | Who Puls is: its voice and modes, its roles, the packs it names (`where-to-buy`, `shop-prices`, `recipes`, `web`, `calculator`); its prompts embedded with their paths | [identity.puls](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.Identities.Puls.md) |
| `demos/puls/evals/LouisAgent.Identities.Puls.Evals` | Class library | What proves Puls: its evals as folders of files, their readers and expectations; only the runner references it | [identity.puls](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.Identities.Puls.md) |
| `demos/puls/evals/LouisAgent.Identities.Puls.Judges` | Class library | Puls's model judge, the cook's `meal-for-people`, apart from the identity because it needs the harness; only the runner references it | [identity.puls.judges](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.Identities.Puls.Judges.md) |
| `packs/tools/LouisAgent.Packs.Calculator` | Class library | Sums worked out in code: the `calculator` toolset's `Calculate` | [pack.calculator](../../../packs/docs/projects/LouisAgent.Packs.Calculator.md) |
| `packs/tools/LouisAgent.Packs.Docs` | Class library | A project's own docs, read-only: the `docs` toolset's `ListDocs`, `ReadDoc` and `SearchDocs` under `DOCS_ROOT` (F109.3) | [pack.docs](../../../packs/docs/projects/LouisAgent.Packs.Docs.md) |
| `packs/tools/LouisAgent.Packs.Triage` | Class library | What an agent team triages a service's errors with (F106.3.1): the error feed, a run's recorder, the roles' typed endings (`ReportInvestigation`, `ReportReview`, `DecideCase`), a case raised to a person, a run read back, departments as YAML | [pack.triage](../../../packs/docs/projects/LouisAgent.Packs.Triage.md) |
| `src/site/LouisAgent.Identities.Framework` | Class library | Who the framework Orbi is (F164.2): the agent docked beside the framework's own pages, one voice on the docs and site packs | [identity.framework](LouisAgent.Identities.Framework.md) |
| `src/site/LouisAgent.Identities.Guide` | Class library | Who the framework's guide is (F109.3, renamed in F124): the guide in its website's corner, one voice on the docs, leads and site packs | [identity.guide](LouisAgent.Identities.Guide.md) |
| `src/site/LouisAgent.Identities.Showroom` | Class library | Three made-up businesses' agents for the demo pages (F128.2): a restaurant, a law practice and a guesthouse (F127.1, in place of the doctor's practice), each a voice on the leads pack | [identity.showroom](LouisAgent.Identities.Showroom.md) |
| `src/site/LouisAgent.Site`, `src/site/LouisAgent.SiteApi` | Blazor WebAssembly app, and its host | louis-agent.dev (F124): the site that sells websites with agents, its showroom and the CV, and the host whose catalogue is the guide, open to visitors, then the showroom behind the key | [site](LouisAgent.Site.md) |
| `src/site/LouisAgent.SiteFramework`, `src/site/LouisAgent.SiteFrameworkApi` | Blazor WebAssembly app, and its host | The framework's own site (F164.1), for framework.louis-agent.dev: the framework's pages in the docs layout, flat alone, and a host that serves and draws them, with no agent yet; on :5084 | [site-framework](LouisAgent.SiteFramework.md) |
| `packs/tools/LouisAgent.Packs.WhereToBuy` | Class library | Where to buy: the `where-to-buy` toolset (a shop's nearest branches on OpenStreetMap, and which is the town's), its guide and its texts | [pack.where-to-buy](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/packs/docs/projects/LouisAgent.Packs.WhereToBuy.md) |
| `packs/evals/LouisAgent.Packs.WhereToBuy.Checks` | Class library | The shop rules evals name; only the runner references it | [pack.where-to-buy](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/packs/docs/projects/LouisAgent.Packs.WhereToBuy.md) |
| `packs/tools/LouisAgent.Packs.ShopPrices` | Class library | Shop prices: the `shop-prices` toolset (a list estimated from Stats SA's average prices, totals in code), its tables, its store (`SHOP_PRICES_MONGO_URL`), its guide and its texts | [pack.shop-prices](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/packs/docs/projects/LouisAgent.Packs.ShopPrices.md) |
| `packs/tools/LouisAgent.Packs.Recipes` | Class library | What the recipes are: the `recipes` toolset (search an enabled South African recipe site, read any recipe page), its guide, its setting (`RECIPE_SOURCES`) and its texts | [pack.recipes](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/packs/docs/projects/LouisAgent.Packs.Recipes.md) |
| `packs/evals/LouisAgent.Packs.Recipes.Checks` | Class library | The rule evals name to prove a recipe's link is on a site of the pack's table; only the runner references it | [pack.recipes](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/packs/docs/projects/LouisAgent.Packs.Recipes.md) |
| `packs/tools/LouisAgent.Packs.Files`, `.git`, `.dotnet`, `.shell`, `.web`, `.paymo`, `.devops` | Class libraries | The general packs, the tools any agent may want, one pack each: files, git, `dotnet`, scripts in a shell, web search and fetch, Paymo and Azure DevOps (the coding agent loads the first five) | [general packs](../../../packs/docs/projects/general-packs.md) |
| `packs/tools/LouisAgent.Packs.Leads` | Class library | How an agent tells the owner someone is interested: the `leads` toolset's `SendLead`, a sender behind a seam (F112) | [pack.leads](../../../packs/docs/projects/LouisAgent.Packs.Leads.md) |
| `packs/tools/LouisAgent.Packs.Site` | Class library | How an agent acts on the page: it takes a visitor to a place, shows a card, offers choices and asks with a form, each checked here and drawn by the web's widget (F117, F127.4) | [pack.site](../../../packs/docs/projects/LouisAgent.Packs.Site.md) |
| `packs/web/LouisAgent.Packs.Files.Web`, `.git.web`, `.recipes.web` | Razor class libraries | A pack's own screens, apart from its tools so someone can take the tools without them (ADR 0086) | Each pack's doc: [general packs](../../../packs/docs/projects/general-packs.md), [pack.recipes](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/packs/docs/projects/LouisAgent.Packs.Recipes.md) |
| `demos/puls/hosts/LouisAgent.HostPacks` | Class library | Picks what Puls's programs run: the one project that references the identity and packs they can load; `Plugins.FromEnvironment()` builds the `AgentCatalog` a host runs with, from `IDENTITIES` and `PACKS` | [host-packs](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.HostPacks.md) |
| `demos/puls/hosts/LouisAgent.PulsApi`, `LouisAgent.PulsEvalRunner` | Programs | Puls's own programs: each names what the framework's host loads and starts it | [host-packs](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.HostPacks.md) |
| `demos/louis-agent/identity/LouisAgent.Identities.Coding` | Class library | Who the coding agent is: its voice and its mode | — (to write) |
| `demos/louis-agent/identity/LouisAgent.Identities.Triage` | Class library | The triage team as one identity (F106.3.2): a manager with a sorter, an investigator and a reviewer, each ending with a typed tool of the triage pack | [identity.triage](../../../demos/louis-agent/docs/projects/LouisAgent.Identities.Triage.md) |
| `demos/louis-agent/hosts/LouisAgent.Coding.Catalog`, `LouisAgent.CodingCli`, `-acp`, `-mcp`, `-api` | Class library, and programs | The coding agent's catalogue (the one project that references its identities and packs) and its programs, each naming what the framework's host loads | [the coding agent's docs](../../../demos/louis-agent/docs/README.md) |
| `demos/starter/web/LouisAgent.Starter`, `demos/starter/hosts/LouisAgent.Starter.Api` | Blazor WebAssembly app, and its program | The smallest site on the framework's web, and what `dotnet new louis-site` makes | [Spawn a site](../guides/spawn-a-site.md) |
| `src/engine/LouisAgent.Composition` | Class library | The contracts a pack, an identity and the engine share, and what they all use: the workspace and secret-file rules, the one process runner, the message texts and the web's interfaces; it names no pack and references only `LouisAgent.Choices` | [ADR 0076](../adr/0076-core-ships-no-agent.md) |
| `src/hosts/LouisAgent.Cli` | Library | Chat with the agent in a terminal; a demo's program starts it | [cli](LouisAgent.Cli.md) |
| `src/hosts/LouisAgent.Acp` | Library (stdio); a demo's program starts it | The agent inside JetBrains Rider's AI chat (Agent Client Protocol) | [acp-server](LouisAgent.Acp.md) |
| `src/hosts/LouisAgent.Api` | Library (ASP.NET Core); a demo's program starts it | HTTP API with streamed answers, workspace and git endpoints; serves the web app | [api](LouisAgent.Api.md) |
| `src/hosts/LouisAgent.Accounts` | Library (ASP.NET Core); a demo's program adds it beside the API | Sign-in, registration and profiles on ASP.NET Core Identity, a tenant on every account, EF Core on SQL Server; the `/account` routes (F61) | [accounts](LouisAgent.Accounts.md) |
| `src/hosts/LouisAgent.Accounts.Contracts` | Class library | The records the accounts routes send and read, so the browser app reads what the server writes (ADR 0045) | [accounts](LouisAgent.Accounts.md) |
| `demos/louis-agent/web/LouisAgent.CodeReview` | Blazor WebAssembly | The coding agent's browser app: a thin chat page on the framework's frame, with the files and git packs' screens | [web](../../../demos/louis-agent/docs/projects/LouisAgent.CodeReview.md) |
| `demos/puls/web/LouisAgent.Puls` | Blazor WebAssembly | Puls: the general assistant in Afrikaans; a landing page and a three-panel chat that delegates every task | [puls](https://github.com/bosbevk/louis-agent/blob/puls-moved-out/demos/puls/docs/projects/LouisAgent.Puls.md) |
| `src/hosts/LouisAgent.Mcp` | Library (stdio or ASP.NET Core); a demo's program starts it | Hands the agent's tools to any MCP client (no model of its own) | [mcp-server](LouisAgent.Mcp.md) |
| `demos/louis-agent/hosts/LouisAgent.Orchestrator` | Console app | A second agent that watches a service's errors and has louis-agent fix them (POC) | [orchestrator](../../../demos/louis-agent/docs/projects/LouisAgent.Orchestrator.md) |
| `src/tools/LouisAgent.UsageProbe` | Console app (developer tool) | Prints what a provider reports as token usage and what the ledger makes of it | [usage-probe](LouisAgent.UsageProbe.md) |
| `src/tools/LouisAgent.FilmExport` | Console app (developer tool) | Exports the site's films to WebP for the README, so a stale export fails a test (ADR 0077) | [Change the site](../guides/change-the-site.md#export-the-films) |
| `LouisAgent.Choices` | Class library | `SettingChoice` and `[WireName]`: the one parser for a name that is an enum member; references nothing ([ADR 0043](../adr/0043-repeated-names-are-enums.md)) | — (to write) |
| `LouisAgent.SessionStream` | Class library | Reads a session's server-sent events into records, for core's sub-sessions and the web apps; holds the session wire's requests and replies as shared records (`Wire/`, F127.3.1, ADR 0117), so core, the API and the web send and read one shape; and the pace an answer's words show at (`Pacing/WordPace`, F155), where more than the web can read it | — (to write) |
| `src/hosts/LouisAgent.Api.Contracts` | Class library | The API's replies and bodies as records, and each wire's options object, shared by the API and its clients ([ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md)) | — (to write) |
| `src/web/LouisAgent.Web` | Razor class library | What every site shares: the chat's and the admin's frames, a pack's screens, chat models, the API client, markdown, usage and currency | [LouisAgent.Web](LouisAgent.Web.md) |
| `src/web/LouisAgent.Web.Accounts` | Razor class library, on `LouisAgent.Web` | The account pages, their client, words, styles and passkey script; a site that has accounts opts in (CL30, ADR 0089) | [LouisAgent.Web.Accounts](LouisAgent.Web.Accounts.md) |
| `src/web/LouisAgent.Web.Framework` | Razor class library, on `LouisAgent.Web` and `LouisAgent.Web.Docs` | The framework's pages (F164.1): from Overview to Comparison, what they say as records held to the repo, and their pictures; a site turns them on with `.Framework()` | [web.framework](LouisAgent.Web.Framework.md) |
| `src/web/LouisAgent.Web.Docs` | Razor class library, on `LouisAgent.Web` | The docs' parts (F164.1): `.Docs(address)`, a link to a doc by its path in the repo, and the pages in reading order with their menu and page turns | [web.docs](LouisAgent.Web.Docs.md) |
| `src/web/LouisAgent.Web.Places` | Razor class library, on `LouisAgent.Web` | Every place's own address (F164.1, ADR 0107): `.Places(home, site)`, the router's catch-all and the page's own layout for a place | [web.places](LouisAgent.Web.Places.md) |
| `src/web/LouisAgent.Web.Themes` | Class library, on `LouisAgent.Web` | The themes a site can offer (F151.2, ADR 0124), a folder each: glass and flat, named `.Glass()` and `.Flat()` among a site's areas; values only, naming no component | [web.themes](LouisAgent.Web.Themes.md) |
| `src/web/LouisAgent.Diagrams`, `src/web/LouisAgent.Diagrams.Engine`, `src/tools/LouisAgent.DocDiagrams` | Two class libraries (plain C#), and their console tool | Diagrams drawn from data (CL60.1), as two projects (CL65.1): what a diagram is, its kinds, records and reader, and the engine that builds one as an SVG with a light and a dark look; the site's `GraphView` and the tool that draws every docs picture use them | [diagrams](LouisAgent.Diagrams.md) |
| `src/tests`, `demos/*/tests`, `packs/tests` | NUnit | The tests of core, the API's sessions, the orchestrator, the ACP server, the coding catalogue's prompts and goldens, the packs and the rules every pack keeps, and the web projects (bUnit); `src/tests/shared/LouisAgent.Testing` and `src/tests/web/LouisAgent.Web.Testing` hold the fakes they share | [tests](tests.md) |

## How they depend on each other

An arrow means "references".

![How the projects depend on each other: the programs, core and what runs on it, identities, packs and the contracts](diagrams/README-how-they-depend-on-each-other.svg)

A pack or an identity references only the contracts, and core references no pack, identity or host, so only what picks the packs knows them all. Behaviour lives in core, and every host is a thin
adapter between its protocol and `AgentEngine`.

![How the web projects depend on each other: the programs, the sites, packs/web, the framework's web and the API](diagrams/README-how-they-depend-on-each-other-2.svg)

**The diagrams leave a few things out,** to stay readable. They leave out the test projects, and the small shared
projects: `LouisAgent.Choices`, `LouisAgent.SessionStream` and
`LouisAgent.Api.Contracts`. They don't show that the orchestrator and the usage probe are programs of their own,
which no demo's program starts, or the orchestrator's reference to the coding identity. And they don't show the calls
at run time on the engine's side: the API opens sub-sessions on a peer, and the orchestrator opens
sessions on the API, all over HTTP and SSE.

## Where each one runs

| Project | Run it with | Docker service |
|---|---|---|
| core | (library) | — |
| cli | `dotnet run --project demos/louis-agent/hosts/LouisAgent.CodingCli` | `local-agent` |
| acp-server | Rider starts it per chat | `acp-server` (`docker compose run --rm -T acp-server`) |
| api + web | `docker compose … up -d api` → http://127.0.0.1:5082 (`API_PORT`) | `api` |
| mcp-server | an MCP client starts it (stdio), or HTTP with `MCP_TRANSPORT=http` | `mcp-server` |
| orchestrator | `dotnet run --project demos/louis-agent/hosts/LouisAgent.Orchestrator` | — |

## Each doc covers

Every project doc keeps the same sections, by the same names, so you find things in the same place in each one.

**Purpose** says what the project is for and who uses it. **How it works** tells the main path as a story, one
numbered part after another (`### 1. Starting up`), with each picture beside the part it explains. **Structure** is
the lookup: every file and folder, and the types you meet most. **Configuration** lists the settings, **Extending it**
says how to add to it, **Tests** says where the tests are and what they hold, and **Limits and plans** says what it
can't do yet, with links to [features](../features/README.md) and the [TODO](../../../TODO.md). **Related docs**
closes it.

*Purpose* and *How it works* talk the way a friend would explain it, in short paragraphs. The sections after them
are for looking things up.

---
[Docs index](../README.md) · Next: [LouisAgent.Core](LouisAgent.Core.md)
