# Louis Agent

**An AI agent engine in C#, and the agents built on it.**

louis-agent is the engine: the tool loop, streaming, providers, cost tracking and safety guards, built on
[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai). On top of it you put an
**identity** (who the agent is: its voice, its modes, its roles) and **packs** (what it can do: tools, guides,
settings). Two agents run on it today: a **coding agent** that works on your repository, and **Puls**, a South African
assistant in everyday Afrikaans.

Everything is C#, on purpose. It runs on Claude by default; Ollama and OpenAI-compatible endpoints work too.

## Why it exists

It's a learning project first. I'm a software engineer becoming an AI engineer, and every layer of a real agent
(tools, context, evals, cost, safety, orchestration) gets built here by hand, measured, and written up.
[docs/GOAL.md](docs/GOAL.md) has the skill map, and [docs/learning/](docs/learning/README.md) has the write-ups.

It's also heading for a public release: Puls for the public, and the engine for .NET teams who want to build their
own agents.

## Where it stands

| Works today | Next |
|---|---|
| The engine, with identities and packs picked at start-up (`IDENTITIES`, `PACKS`) | Evals as files that belong to their identity, plus an eval studio in the portal |
| The coding agent in Rider, the web app, the CLI, the HTTP API and over MCP | Puls's cook gets recipe sources, recipe maths and teaching |
| Puls with five specialists: a researcher, a reader, a historian, an analyst and a cook | Every text from localisation files |
| A shops pack (Checkers prices and deals) and self-hosted web search | Twelve local Puls specialists (gardener, braaimaster, weatherman and more) |
| **Evals in C#:** one harness for every check, graders, replays for $0, results in the admin portal | Then the sales playground, budgets, orchestration, safety and accounts |
| Cost on every answer, from a usage ledger priced by date | |

The full order is in the [TODO](TODO.md) and the [platform plan](docs/plans/platform.md); what's finished is in the
[CHANGELOG](CHANGELOG.md).

## How it's built

```mermaid
flowchart TB
    hosts["Hosts<br/>Rider (ACP) · web app · Puls · HTTP API · CLI · MCP server"]
    comp["louis-agent.composition<br/>the only project that knows every identity and pack"]
    ids["Identities<br/>who an agent is: puls, the coding agent"]
    packs["Packs<br/>what it can do: shops, …"]
    core["louis-agent.core: the engine<br/>tool loop · streaming · providers · usage ledger · guards"]
    llm["Claude · Ollama · OpenAI-compatible"]
    hosts --> comp
    comp --> ids
    comp --> packs
    ids --> core
    packs --> core
    core --> llm
```

The engine names no product and no country. A pack never knows about another pack or an identity, and only the
composition project knows them all ([ADR 0027](docs/adr/0027-engine-identities-and-packs.md)). A few rules hold
everywhere:

- **Every request is measured.** The usage ledger records each model call with its tokens and its cost. Every host
  shows what a turn cost (`turn: 3 requests · 28k tokens (26k cached) · $0.006`), and an unknown price stays unknown
  instead of turning into 0 ([ADR 0007](docs/adr/0007-measure-every-request.md)).
- **Guards live in code, not in the prompt.** Model text never reaches a shell, file tools stay inside the workspace,
  calls that got cut off never run, and anything the agent writes waits for your `/approve`
  ([ADR 0004](docs/adr/0004-arguments-as-data.md), [ADR 0005](docs/adr/0005-guard-the-context.md)).
- **Everything streams.** You see the thinking and every tool call as they happen, in every host.
- **A session keeps its tools.** It names its toolsets when it opens, so its prompt prefix stays cacheable. Prompt
  caching took the old benchmark from $0.66 to $0.13 a run ([ADR 0006](docs/adr/0006-toolsets-per-session.md)).
- **Packs stay convertible to MCP.** A pack could be served as an MCP server, and an MCP server could stand in for a
  pack ([ADR 0028](docs/adr/0028-packs-stay-convertible-to-mcp.md)).

More detail is in [the architecture](docs/architecture/README.md) and in [the docs for each project](docs/projects/README.md).

## Puls

**Puls** (*Elke vraag op pad na 'n antwoord*) is a general assistant that speaks everyday South African Afrikaans
(English too). It doesn't answer with tools itself. It hands each task to a **specialist**: a second agent session
that has only the tools that task needs, and a role that says how to work and report back. Then Puls writes the answer
in its own voice.

- **You see the work.** The Werk panel shows each hand-over live: the specialist, its task, each step, the sources it
  read and what it cost.
- **It's local.** Costs show in rand, it knows your time zone, and it uses your location (to about 1 km) only if you
  share it.
- **It's checked.** The cook and the routing are evals, so a prompt change shows up as a score.

```bash
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
```

Open **http://127.0.0.1:5070**. The same app runs on Azure ([deploy/azure](deploy/azure/README.md)). Design:
[Puls's identity](docs/projects/louis-agent.identity.puls.md), [Puls's web app](docs/projects/louis-agent.puls.md).

## The coding agent

It works on your repository with real tools: files, git, `dotnet`, Python, PowerShell, bash, web search, Azure DevOps
and Paymo. It can write new skills and build its own tools, and those wait for your approval.

| Host | Project | What it is |
|---|---|---|
| **Rider** | `src/louis-agent.acp-server` | [Agent Client Protocol](https://agentclientprotocol.com) server for Rider's AI chat |
| **Web app** | `src/louis-agent.web` | Blazor chat with a file browser, a Changes view (review, stage, commit) and a Branches view (review, merge) |
| **HTTP API** | `src/louis-agent.api` | Sessions with answers streamed as Server-Sent Events; it also serves the web app |
| **CLI** | `src/louis-agent.cli` | Interactive or one-shot prompts in the terminal |
| **MCP server** | `src/louis-agent.mcp-server` | Hands the tools to any MCP client, which brings its own model |
| **Orchestrator** | `src/louis-agent.orchestrator` | A proof of concept: a second agent reads a service's errors and has louis-agent fix and verify them |

Pushing always stays with you.

## Evals

An eval runs a real task and scores the outcome.
Every check, regression and demo is a C# eval on one harness (`evals/`), and each one sits beside the identity or pack
it proves ([ADR 0029](docs/adr/0029-simulations-in-csharp.md)).

- **Code graders first.** Fixed rules are free and give the same score every time. A model judge comes in only where
  code can't decide, and only after it agrees with human labels.
- **pass@k and pass^k.** Run a task k times: did it pass at least once, and did it pass every time? An agent you sell
  needs the second.
- **Re-runs for $0.** A run records the web pages it read, so it can be replayed and re-scored for nothing. Cost always
  comes from the ledger, never from a grader.
- **Everything shows in the admin portal**, including human review.

The first baseline: Puls's cook passed 23 of 24, its routing 72 of 72, for $0.37 together. How to:
[run the evals](docs/guides/run-the-evals.md) · [add an eval set](docs/guides/add-an-eval-set.md); design:
[F18](docs/features/F18-evaluation.md).

## Quick start

You need Docker Desktop and an [Anthropic API key](https://console.anthropic.com), or the .NET 10 SDK to run it
without Docker.

```bash
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets   # set ANTHROPIC_API_KEY and AGENT_API_KEY
# in config/.env, set REPOSITORIES_PATH to the folder that holds your repos

docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api   # web app + API
```

Open **http://127.0.0.1:5080**. Everything listens on `127.0.0.1` only.

- **Rider:** add an agent server in the AI Assistant settings, from
  [config/rider/rider-acp.example.json](config/rider/rider-acp.example.json). Each chat gets its own container.
- **CLI:** `dotnet run --project src/louis-agent.cli` (interactive), or add `-- "your prompt"` for one shot.

Every setting is in [docs/SETUP.md](docs/SETUP.md), and choosing a model is in [docs/MODELS.md](docs/MODELS.md). For
prompts to try, see the [quick start](docs/QUICK_START_AGENT.md).

## Documentation

All the docs, in reading order, are in **[docs/README.md](docs/README.md)**. The ones you'll want first:

| To… | Read |
|---|---|
| See what happens when you send a message | [How a turn works](docs/architecture/06-runtime-view.md) |
| Connect another program or agent (HTTP API, MCP, agent to agent) | [How the agents and endpoints talk](docs/AGENT_COMMUNICATION.md) |
| Build your own identity or pack | [Build an identity](docs/guides/build-an-identity.md) · [Build a pack](docs/guides/build-a-pack.md) |
| Know why it's built this way | [Architecture decisions](docs/adr/README.md) |
| Change the code | [Development guide](docs/CLAUDE.md) · [how-tos per project](docs/guides/README.md) |

## Licence

**All rights reserved** ([LICENSE](LICENSE), since 2026-10-15). The code is public to read, but you may not use, copy,
change, share or host it without written permission; open an issue to ask. Versions released before under MIT or the
AGPL-3.0 keep that licence. Why: [ADR 0016](docs/adr/0016-licence-all-rights-reserved.md).
