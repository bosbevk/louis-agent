# Louis Agent

**An AI agent framework in C#, and the agents built on it.**

![Louis Agent at a glance: the engine, the hosts, the identities and packs, and the evals and cost that prove them](src/docs/diagrams/README-overview.svg)

louis-agent is a framework: the engine (the tool loop, streaming, providers, cost tracking and safety guards, built on
[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)), the hosts that serve it, and
packs of tools. A few lines of your own say who the agent is (an **identity**: its voice, its modes, its roles) and what
it can do (**packs**: tools and their guides), and start a host. Three kinds of agent run on it here: a **coding agent**
that works on your repository, a **triage team** that sorts a service's errors, and the **guide** on
[louis-agent.dev](https://louis-agent.dev), the website that sells websites with an agent, with the agents of three
made-up businesses in its showroom. The framework explains itself at
[framework.louis-agent.dev](https://framework.louis-agent.dev), where its own guide sits beside the pages. Two
products are built outside this repository on its packages: **Puls**, a South African assistant in everyday
Afrikaans, and the **bill checker**, a phone app still being planned.

Everything is C#, on purpose. It runs on Claude by default; Ollama and OpenAI-compatible endpoints work too.

## Why it exists

It's a learning project first. I'm a software engineer becoming an AI engineer, and every layer of a real agent
(tools, context, evals, cost, safety, orchestration) gets built here by hand, measured, and written up.
[src/docs/GOAL.md](src/docs/GOAL.md) has the skill map, and [src/docs/learning/](src/docs/learning/README.md) has the write-ups.

It's also heading for a public release: Puls for the public, and the engine for .NET teams who want to build their
own agents.

## Where it stands

| Works today | Next |
|---|---|
| The engine, with the identities and packs each program names | A photo or a PDF reaching a tool, the bill checker's first part in the framework |
| The coding agent in Rider, the web app, the CLI, the HTTP API and over MCP | Sessions that survive a restart, behind one store with an in-memory and a MongoDB provider |
| **One address for every agent call:** `AskAgent("seemeeu-guesthouse", …)` asks a subagent, a cheap one-task lookup agent, or an agent on another server, and an identity lists in code whom it may ask | History that is only ever appended to, then retries and a fallback model |
| **Sequences:** an identity can force a fixed order on the agents it calls, run in code, each step checked before the next one opens | Budgets that warn and stop, per agent |
| A triage team: a manager with a sorter, an investigator and a reviewer, each ending its work with a typed tool | Drafts a person approves, then safety and an audit log |
| **Evals:** one file of conversations beside the identity it proves, facts in code and criteria a judge marks; a run kept as one file and replayed for free; started, followed and stopped from the admin, where a person flips a mark and compares a run with the one before | More evals: the framework's guide and the triage team |
| The guide's eval: twelve conversations, a prompt-injection case among them, with a live baseline of 9 of 12 | A host that names its parts with one builder, so a first agent takes a few lines |
| An admin that is a frame its areas plug into: an overview of the talks and what they cost, the evals, and the packs' pages | The rest of the bill checker, then the Google integration suite |
| Cost on every answer, from a usage ledger priced by date; every turn and tool call traced with OpenTelemetry | A `/now` page on both sites: what is being built, and what just shipped |
| **louis-agent.dev**, on the framework's web: a guide in the corner that knows the page you are on, takes you to a section and draws cards and forms in the chat; a showroom of three made-up businesses whose agents you can try live; every page served as HTML so a search engine can read it | |
| **framework.louis-agent.dev**, the framework's own seven pages, Overview to Comparison, with its own guide docked beside them | |
| **A showroom the agent runs:** the guide sits in an agent bar under the page, lights the part it explains, and opens each made-up business's own website in a window with that business's chat beside it | |
| Louis's CV as a page on the site (`/cv`), edited in place in the admin and printed to PDF from the same page | |
| Every diagram in the docs drawn from JSON by our own renderer, from flows and sequences to component and composite structure pictures | |
| Accounts as a library a site opts into: sign-in, social logins, passkeys, a tenant from the host name | |
| A new site from a template (`dotnet new louis-site`): a chat and an admin on the framework's web | |
| Both sites running in Azure Container Apps, deployed from GitHub on every merge with no stored password, on one Bicep file a person deploys | |
| **The framework as packages:** 31 projects published to GitHub Packages, one tag each, and Puls built on them in its own repository | Moving to nuget.org at the first stable 1.0 |

The full order is in the [TODO](TODO.md) and the [roadmap](ROADMAP.md); what's finished is in the
[work log](src/docs/work-log.md).

## How it's built

![Louis Agent: how a program, its hosts, identities and packs sit on the engine](src/docs/diagrams/README-how-it-s-built.svg)

The engine ships no agent and holds no tools, and it names no product and no country. A pack needs only the contracts,
never the engine, and never knows about another pack or an identity. Only a demo's own program knows them all
([ADR 0027](src/docs/adr/0027-engine-identities-and-packs.md), [ADR 0076](src/docs/adr/0076-core-ships-no-agent.md)).

```
src/                 the framework: the engine, the hosts as libraries, the framework's web, the eval harness, its tools, tests and docs
src/site/            the framework's own website and its guide: the one place in src/ that uses packs and an identity
packs/               what an agent can do, tools only; packs/tests/ tests them, packs/web/ holds their screens
demos/louis-agent/   the coding agent and the triage team, as your own project would be: identities, programs, a web app, settings, images, tests
demos/starter/       the smallest site, and what `dotnet new louis-site` makes
```

A few rules hold everywhere:

Every request is measured. The usage ledger records each model call with its tokens and its cost. Every host shows
what a turn cost (`turn: 3 requests · 28k tokens (26k cached) · $0.006`), and an unknown price stays unknown instead
of turning into 0 ([ADR 0007](src/docs/adr/0007-measure-every-request.md)).

Guards live in code, not in the prompt. Model text never reaches a shell, file tools stay inside the workspace, calls
that got cut off never run, and anything the agent writes waits for your `/approve`
([ADR 0004](src/docs/adr/0004-arguments-as-data.md), [ADR 0005](src/docs/adr/0005-guard-the-context.md)).

Everything streams, and is traced. You see the thinking and every tool call as they happen, in every host; with an
OpenTelemetry collector set, each turn and tool call is also a span on a dashboard
([ADR 0064](src/docs/adr/0064-observability-through-opentelemetry.md)).

A site's no is obeyed. Every page the agent fetches checks the site's `robots.txt` and its no-AI signals first, in
code ([ADR 0068](src/docs/adr/0068-every-fetch-obeys-robots-txt.md)).

A session keeps its tools. It names its toolsets when it opens, so its prompt prefix stays cacheable. Prompt caching
took the old benchmark from $0.66 to $0.13 a run ([ADR 0006](src/docs/adr/0006-toolsets-per-session.md)).

Packs stay convertible to MCP. A pack could be served as an MCP server, and an MCP server could stand in for a pack
([ADR 0028](src/docs/adr/0028-packs-stay-convertible-to-mcp.md)).

More detail is in [the architecture](src/docs/architecture/README.md) and in [the docs for each project](src/docs/projects/README.md).

## Puls

**Puls** (*Elke vraag op pad na 'n antwoord*) is a general assistant that speaks everyday South African Afrikaans
(English too). It doesn't answer with tools itself. It hands each task to a **subagent**: a second agent session
that has only the tools that task needs, and a role that says how to work and report back. Then Puls writes the answer
in its own voice.

You see the work. The Werk panel shows each hand-over live: the subagent, its task, each step, the sources it read
and what it cost.

It's local. Costs show in rand, it knows your time zone, and it uses your location (to about 1 km) only if you share
it.

It's checked by its tests and its own evals, in its own repository.

Puls is the first product built on this framework from outside. It lives in a repository of its own,
[bosbevk/puls](https://github.com/bosbevk/puls), and takes the framework only as the packages published from here, so
it is also the proof that someone else's project can do the same. Its README says how to build and run it. It is paused
while the framework comes first, and `mypuls.co.za` shows a coming-soon page published from there. Design:
[Puls's identity](https://github.com/bosbevk/puls/blob/main/docs/projects/Puls.Identity.md),
[Puls's web app](https://github.com/bosbevk/puls/blob/main/docs/projects/Puls.Web.md).

## The coding agent

It works on your repository with real tools: files, git, `dotnet`, Python, PowerShell, bash and web search. It can
write new skills and build its own tools, and those wait for your approval.

| Host | Project | What it is |
|---|---|---|
| **Rider** | `src/hosts/LouisAgent.Acp` | [Agent Client Protocol](https://agentclientprotocol.com) server for Rider's AI chat |
| **Web app** | `demos/louis-agent/web/LouisAgent.CodeReview` | Blazor chat with a file browser, a Changes view (review, stage, commit) and a Branches view (review, merge) |
| **HTTP API** | `src/hosts/LouisAgent.Api` | Sessions with answers streamed as Server-Sent Events; it also serves the web app |
| **CLI** | `src/hosts/LouisAgent.Cli` | Interactive or one-shot prompts in the terminal |
| **MCP server** | `src/hosts/LouisAgent.Mcp` | Hands the tools to any MCP client, which brings its own model |
| **Orchestrator** | `demos/louis-agent/hosts/LouisAgent.Orchestrator` | A proof of concept: a second agent reads a service's errors and has louis-agent fix and verify them |

Each host is a library; a small program in `demos/louis-agent/hosts` names what it loads and starts it. Pushing always stays
with you.

## Evals

An eval is how you know a change made the agent better and not worse. Here an eval is one `{name}.eval.json` file
beside the identity it proves: conversations with turns, each turn with its own facts and criteria. A fact is
checked in code, such as "it called this tool". A criterion is something only a reader can judge, so one model judge
marks each one yes or no, with a reason. A conversation passes only when everything is met in every repeat
([ADR 0134](src/docs/adr/0134-an-eval-is-one-file-of-conversations.md)).

A run plays the file against the agent over the sessions API, one session per repeat, and is kept as one JSON file.
Because the run records what the model said, it can be replayed later for free. You start, follow and stop a run
from the admin, which asks for your yes with an estimate before a live run spends money. On a run's page you can
flip a judge's mark when it got one wrong, have the judge mark again only what changed, and compare the run with the
one before to see whether it got better or worse.

The first eval is the guide's on louis-agent.dev
([guide.eval.json](src/site/LouisAgent.Identities.Guide/agents/louis-agent-guide/guide.eval.json)): twelve
conversations, one of them a page that tries to inject instructions. Its first live run of three repeats passed 9 of
the 12 and cost $0.090, and the three it failed are open fixes in the [TODO](TODO.md). How the parts fit is in
[the evals' design doc](src/docs/projects/LouisAgent.Evals.md).

## Quick start

You need Docker Desktop and an [Anthropic API key](https://console.anthropic.com), or the .NET 10 SDK to run it
without Docker.

```bash
cp demos/louis-agent/config/.env.example demos/louis-agent/config/.env
cp demos/louis-agent/config/.env.secrets.example demos/louis-agent/config/.env.secrets   # set ANTHROPIC_API_KEY and AGENT_API_KEY
# in demos/louis-agent/config/.env, set REPOSITORIES_PATH to the folder that holds your repos

docker compose -f demos/louis-agent/docker/docker-compose.yml --env-file demos/louis-agent/config/.env up -d --build api   # web app + API
```

Open **http://127.0.0.1:5080**. Everything listens on `127.0.0.1` only.

To use it from Rider, add an agent server in the AI Assistant settings, from
[config/rider/rider-acp.example.json](demos/louis-agent/config/rider/rider-acp.example.json); each chat gets its own
container. To use it in a terminal, run `dotnet run --project demos/louis-agent/hosts/LouisAgent.CodingCli` for an
interactive chat, or add `-- "your prompt"` for one shot.

Every setting is in [src/docs/SETUP.md](src/docs/SETUP.md), and choosing a model is in [src/docs/MODELS.md](src/docs/MODELS.md). For
prompts to try, see the [quick start](demos/louis-agent/docs/QUICK_START_AGENT.md).

## Documentation

All the docs, in reading order, are in **[src/docs/README.md](src/docs/README.md)**. The ones you'll want first:

| To… | Read |
|---|---|
| See what happens when you send a message | [How a turn works](src/docs/architecture/06-runtime-view.md) |
| Connect another program or agent (HTTP API, MCP, agent to agent) | [How the agents and endpoints talk](src/docs/AGENT_COMMUNICATION.md) |
| Build your own identity or pack | [Build an identity](src/docs/guides/build-an-identity.md) · [Build a pack](src/docs/guides/build-a-pack.md) |
| Know why it's built this way | [Architecture decisions](src/docs/adr/README.md) |
| Change the code | [Development guide](src/docs/CLAUDE.md) · [how-tos per project](src/docs/guides/README.md) |

## Licence

**Free for noncommercial use**, under the [PolyForm Noncommercial License 1.0.0](LICENSE), since 2026-11-19. Anyone may
use, change and share the framework for a noncommercial purpose: personal projects, study, research, and charities,
schools and public bodies. Commercial use needs a licence from the copyright holder first; open an issue to ask. It is
source-available, not open source, since open source allows commercial use. Versions released before keep the licence
they were released with: MIT, the AGPL-3.0, or all rights reserved. Why:
[ADR 0127](src/docs/adr/0127-licence-polyform-noncommercial.md). The parts others wrote keep their own licences:
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
