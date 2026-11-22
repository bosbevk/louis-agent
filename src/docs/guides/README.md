# Guides

> **How-to.** The step-by-step guides for a developer changing the code: one per project, and one per thing you build
> across projects (a pack, an identity). Each says what it covers first, then the steps, the tests and a checklist for
> review. **Elsewhere:** how each project works in its [design doc](../projects/README.md); the rules every change keeps
> in the [design rules](../../../.claude/rules/design-rules.md).

![The guides at a glance: one set for building something new across projects, one guide per project to change, and each project's design doc beside them](diagrams/README.svg)

There are two kinds of guide here. The first table is for building something new that spans projects, such as a pack,
an identity, a site or an eval, plus the guides every developer needs on day one. The second table has one guide per
project, for when you change that project's code.

## Build something new

| Guide | Read it to… |
|---|---|
| [Build a pack](build-a-pack.md) | Give an agent a new capability: a pack's project, tools, guide, settings, registration and tests |
| [Draw a diagram](draw-a-diagram.md) | Put a picture in a doc: its JSON beside the doc, the tool that draws the SVG, the link, the test that holds it |
| [Spawn a site](spawn-a-site.md) | Make a new site with a chat and an admin from the starter, then make it yours |
| [Get a site found](get-a-site-found.md) | Make a site show up in search: what the framework does, what a site adds, Search Console, the sitemap, links |
| [Publish the framework](publish-the-framework.md) | Release a package of the framework to GitHub Packages with a tag, several together, what stops a release, and checking it from outside |
| [Deploy the sites](deploy-the-sites.md) | Release louis-agent.dev and framework.louis-agent.dev to Azure: a merge's release, one from the PC, secrets, a domain, rolling back |
| [Build an identity](build-an-identity.md) | Make a new agent: its voice and routing, its subagents, the packs it names, its tests, running it; the rules for modes, roles and minions |
| [Add an eval](add-an-eval.md) | Give an identity an eval: copy the template folder, write its `eval.json`, dataset, manual and transcripts, pick its checks, and list it with `EVALS=` |
| [Run the evals](run-the-evals.md) | Run an eval from the command line or the portal: the flags, record and replay, dev and held-out, what a run costs |
| [Write and run tests](write-tests.md) | Run the tests, write one first, use the shared fakes, regenerate a golden, add a test project |
| [Get started](get-started.md) | Build and run the agent on your machine for the first time: the settings files, the build, the tests, the CLI, the Docker stacks |
| [Add a skill](add-a-skill.md) | Give the agent a function skill: the file, the pending folder and `/approve`, the `## Skill:` shape |
| [Debug and logs](debug-and-logs.md) | Find what the agent did: the tool log, the run files, the usage ledger, the Docker habits |
| [Common errors](common-errors.md) | Fix the errors a developer meets most, by their message |

## Change a project

| Guide | Project | Read it to… |
|---|---|---|
| [Change the CLI](change-the-cli.md) | `src/hosts/LouisAgent.Cli` | Change how the terminal shows a turn, add a chat command |
| [Change the ACP server](change-the-acp-server.md) | `src/hosts/LouisAgent.Acp` | Run it in Rider, add a protocol method or a `session/update` kind |
| [Change the API](change-the-api.md) | `src/hosts/LouisAgent.Api` | Add an endpoint (and protect it), a session option, an event; serve the other web app |
| [Change the MCP server](change-the-mcp-server.md) | `src/hosts/LouisAgent.Mcp` | Publish a tool, add an MCP-only tool, pick the transport, check it with a client |
| [Change the orchestrator](../../../demos/louis-agent/docs/guides/change-the-orchestrator.md) | `demos/louis-agent/hosts/LouisAgent.Orchestrator` | Change a runbook, add a tool or a setting, test and run it |
| [Change the usage probe](change-the-usage-probe.md) | `src/tools/LouisAgent.UsageProbe` | Check a provider's usage, fix a mapper, total a ledger, add a command |
| [Change the code-review web app](../../../demos/louis-agent/docs/guides/change-the-web-app.md) | `demos/louis-agent/web/LouisAgent.CodeReview` | Add a tab, call a new endpoint, add a theme, test the page |
| [Change the site](change-the-site.md) | `src/site/LouisAgent.Site` | Run it, add a page, change its look, change the CV, draw the share card again, test it |
| [Change the framework's web](change-the-web.md) | `src/web/LouisAgent.Web` | Decide what is shared, add an API call, follow a new event, add a component |
| [Register a sign-in provider](register-a-sign-in-provider.md) | `src/hosts/LouisAgent.Accounts` | What to do at Google, Microsoft and LinkedIn, the callback each wants, the keys to set |
| [Change the eval pages](change-the-eval-pages.md) | `src/web/LouisAgent.Web.Evals` | Give a site the pages, sort its sources chart, change a page |
| [Change the engine](change-the-engine.md) | `src/engine/LouisAgent.Core` | Add a tool or a toolset, change a built-in prompt, add a setting or a message, handle a provider difference |
| [Build a pack](build-a-pack.md#6-register-it-in-the-composition-root), [Build an identity](build-an-identity.md#5-register-it-in-the-composition-root) | `demos/louis-agent/hosts/LouisAgent.Coding.Catalog` | Register a new pack or identity: one more entry in a program's catalogue, and its reference |
| [Write and run tests](write-tests.md) | `tests/` | Everything about the test projects |

Each project also links its guide and design doc in its `.csproj`, so they show under `src/docs/` in the IDE's solution
explorer.

---
[Docs index](../README.md)
