# Guides

> **How-to.** The step-by-step guides for a developer changing the code: one per project, and one per thing you build
> across projects (a pack, an identity). Each says what it covers first, then the steps, the tests and a checklist for
> review. **Elsewhere:** how each project works in its [design doc](../projects/README.md); the rules every change keeps
> in the [development guide](../CLAUDE.md#rules-that-always-apply).

## Build something new

| Guide | Read it to… |
|---|---|
| [Build a pack](build-a-pack.md) | Give an agent a new capability: a pack's project, tools, guide, settings, registration and tests |
| [Build an identity](build-an-identity.md) | Make a new agent: its voice and routing, its specialists, the packs it names, its tests, running it |
| [Add a shop](add-a-shop.md) | Price a list at another retailer: its catalogue, throttle, tests on recorded answers, and turning it on |
| [Write and run tests](write-tests.md) | Run the tests, write one first, use the shared fakes, regenerate a golden, add a test project |

## Change a project

| Guide | Project | Read it to… |
|---|---|---|
| [Change the CLI](change-the-cli.md) | `src/louis-agent.cli` | Change how the terminal shows a turn, add a chat command |
| [Change the ACP server](change-the-acp-server.md) | `src/louis-agent.acp-server` | Run it in Rider, add a protocol method or a `session/update` kind |
| [Change the API](change-the-api.md) | `src/louis-agent.api` | Add an endpoint (and protect it), a session option, an event; serve the other web app |
| [Change the MCP server](change-the-mcp-server.md) | `src/louis-agent.mcp-server` | Publish a tool, add an MCP-only tool, pick the transport, check it with a client |
| [Change the orchestrator](change-the-orchestrator.md) | `src/louis-agent.orchestrator` | Change a runbook, add a tool or a setting, test and run it |
| [Change the usage probe](change-the-usage-probe.md) | `tools/louis-agent.usage-probe` | Check a provider's usage, fix a mapper, total a ledger, add a command |
| [Change the code-review web app](change-the-web-app.md) | `src/louis-agent.web` | Add a tab, call a new endpoint, add a theme, test the page |
| [Change the Puls web app](change-the-puls-app.md) | `src/louis-agent.puls` | Run it, change the chat, the landing page or the admin portal, add a starter |
| [Change the shared web library](change-the-web-shared.md) | `src/louis-agent.web.shared` | Decide what is shared, add an API call, follow a new event, add a component |
| [Change the engine](change-the-engine.md) | `louis-agent.core` | Add a tool or a toolset, change a built-in prompt, add a setting or a message, handle a provider difference |
| [Change Puls](change-puls.md) | `identities/louis-agent.identity.puls` | Change what Puls says or routes, add a language, a specialist or a minion's function |
| [Add a shop](add-a-shop.md) | `packs/louis-agent.pack.shops` | Add a retailer to the shops pack |
| [Build a pack](build-a-pack.md#6-register-it-in-the-composition-root), [Build an identity](build-an-identity.md#5-register-it-in-the-composition-root) | `src/louis-agent.composition` | Register a new pack or identity: its enum member, its arm, its reference |
| [Write and run tests](write-tests.md) | `tests/` | Everything about the test projects |

Each project also links its guide and design doc in its `.csproj`, so they show under `docs/` in the IDE's solution
explorer.

---
[Docs index](../README.md)
