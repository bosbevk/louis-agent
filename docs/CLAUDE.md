# Louis Agent: Development Guide

## How to work with Louis

Louis is a software engineer learning to become an AI engineer through this project; learning matters more than speed.
The goal and the skill map are in [GOAL.md](GOAL.md).

1. **Louis drives the code.** Implement only what Louis asks for, one step at a time; planning, reviews and learning
   write-ups happen in the Claude project, so don't start the next feature on your own.
2. **Small steps.** Follow the steps in the feature doc. Each step is one commit (or PR) with its test, small enough to
   review in about ten minutes.
3. **Plan first, and keep the plan.** Before the first step of a feature, or of a fix that takes more than one step,
   the plan Louis approved (made here or in the Claude project) is saved to [plans/](plans/README.md) as `Fnn-name.md`
   or `FXnn-name.md` and committed on its branch. Don't rewrite it afterwards: a change Louis approves during the build
   is added under *Approved during the build*; anything else the build learns goes in the feature or fix doc.
4. **Explain every change:** what changed, why this design and not the alternatives, and what to look at closely.
5. **Stop after each step** and wait for Louis to review and understand it before starting the next one.
6. **Teach, don't just do.** Name the AI-engineering concept behind each step and where it sits in the GOAL.md skill map.
7. **Measure, don't assume.** Ask before anything that spends real money (benchmark or demo runs against the API) and
   before pushing.
8. **Write it up.** When a skill or feature finishes, help write its learning write-up in [learning/](learning/).
9. **Source of truth:** [TODO.md](../TODO.md) (what's next), [features/](features/README.md) (how), [plans/](plans/README.md)
   (how we're building it now), [fixes/](fixes/README.md) (what broke and why) and [specs/](specs/) (why). Update them
   rather than duplicating them.

## Steps, branches and commits

**Doing a step: test first** ([how](../tests/CLAUDE.md#test-first)):

1. Write the test for the step's behaviour (a fix: a test that reproduces the bug).
2. Run it and see it fail for the expected reason; keep the output for the explanation.
3. Implement until it passes, then tidy with the tests green.

Docs-only and configuration-only steps have no test to write first.

**Finishing a step** (before rule 5's stop):

1. `dotnet build louis-agent-solution.sln`, and `dotnet test` on the projects the step touches ([Tests](../tests/CLAUDE.md)); the whole
   solution before a PR.
2. If tools, their descriptions, `ToolMessage` texts or the prompts in `louis-agent.core/Prompts/` changed, regenerate the golden files and review
   their diff ([golden files](../tests/CLAUDE.md#golden-files)).
3. Update the feature or fix doc with what the step learned; on the last step, follow *Finishing a feature* or
   *Finishing a fix* in their READMEs. The plan changes only under *Approved during the build*. New text goes in the
   doc of its type (*Rules that always apply*: one type per doc).
4. Commit the step on its own, test and code together, then explain it (rules 4 and 6), showing the test failing
   before the code and passing after, and stop.

**Branches** are off `main` and carry the plan's id: `feature/F22-role-tools`, `fix/FX14-sub-session-prompt-budget`,
`refactor/CL3-messages-and-logging`; `docs/{name}` for docs-only work. The Claude project's `claude/*` branches keep
their names.

**Commit messages** are `type(scope): step N - what changed`, and the body says why:

```
feat(f22): step 3 - ReadTable for spreadsheets
fix(fx14): the prompt budget counts cached tokens
docs(f5): review of PR #27 - toolset guides
```

`type` is `feat`, `fix`, `refactor`, `test` or `docs`; `scope` is the id in lowercase (`f22`, `fx14`, `cl3`), or the
area when there is none (`ollama`, `samples`, `guide`); `step N -` only on a plan's steps. Never push without asking
(rule 7).

## Rules that always apply

The design decisions every change keeps; the sections below have the details, and each *Why* links the
decision's record (its context, and what it cost) in [docs/adr/](adr/README.md).

- **Reuse before building** — reuse the repo's components and maintained third-party packages (MIT, Apache or BSD);
  write our own only when nothing fits, and say in the plan which were considered.
- **Microsoft.Extensions.AI** — one `IChatClient` across providers; `FunctionInvokingChatClient` runs the tool loop.
  Why: [ADR 0002](adr/0002-messages-api-tier-with-our-own-loop.md).
- **Provider strategies** — where Anthropic, OpenAI-compatible and Ollama differ (usage, caching, thinking, limits),
  one interface with an implementation per provider, picked in `LlmClientFactory`; never branch on the provider's
  name anywhere else ([Changing the LLM](../louis-agent.core/CLAUDE.md#changing-the-llm),
  [Architecture](architecture/05-building-block-view.md#provider-strategies)).
  Why: [ADR 0003](adr/0003-provider-strategies.md).
- **Streaming everywhere** — hosts render `StreamPromptAsync` updates; the finished turn is appended to the history,
  thinking blocks included (Anthropic requires them when continuing after tool calls).
- **Arguments as data** — skill and tool arguments travel as environment variables or argument lists, never through a
  shell, so model text can't become commands.
  Why: [ADR 0004](adr/0004-arguments-as-data.md).
- **Guard the context** — oversized results are summarised, cut-off calls are never run, failures carry their reason.
  Why: [ADR 0005](adr/0005-guard-the-context.md).
- **Toolsets per session** (F5) — a session names its toolsets when it opens and keeps them, so its prefix stays
  cacheable; an engine per toolset list, with only those tools and their guides
  ([Adding a toolset](../louis-agent.core/CLAUDE.md#adding-a-toolset)).
  Why: [ADR 0006](adr/0006-toolsets-per-session.md).
- **Measure every request, and show it** — the usage ledger records each model request with its tokens and its cost,
  priced from a table dated by `as_of`, so later price changes never rewrite history; unknown prices are `null`, never
  0. Every host shows a turn's usage from the same records and totals (`UsageReport.Total`), so the views and the
  ledger agree.
  Why: [ADR 0007](adr/0007-measure-every-request.md).
- **History is append-only** — newer Claude models (Haiku 5.5 and later) bind thinking blocks to the system prompt,
  tools and earlier messages, so nothing may edit them mid-conversation ([FX1](fixes/FX01-round-limit-tools.md);
  [FX4](fixes/FX04-append-only-history.md) is still open).
  Why: [ADR 0008](adr/0008-history-is-append-only.md).
- **No message text inline in C#** — exceptions, log lines, tool results and user text are message enum members
  ([Messages and logging](../louis-agent.core/CLAUDE.md#messages-and-logging)).
  Why: [ADR 0009](adr/0009-message-text-as-enum-templates.md).
- **Namespace = folder path, dependencies one way, prompts in markdown, hosts stay thin** — `ArchitectureTests`
  holds the first two ([Project layout](../louis-agent.core/CLAUDE.md#project-layout)).
  Why: [ADR 0010](adr/0010-folders-by-concern.md), [ADR 0011](adr/0011-prompts-are-content.md).
- **Skill files follow the levels** — a built-in prompt lives in `louis-agent.core/Prompts/`, in the folder of the
  level that reads it (`agent/` level 0, `specialists/` level 1, `minions/` level 2) or in `toolsets/` and `shared/`
  when every level shares it; a session loads its personality only from its own level's folder. They're read only from the assembly, so
  a wording change is a rebuild; the repo's `Skills/` holds only what the agent writes. `SkillsLayoutTests` holds it.
  Why: [ADR 0017](adr/0017-skills-by-level.md), [ADR 0018](adr/0018-built-in-prompts-embedded.md).
- **A level-0 session is a mode** — one file of voice and routing in a folder per product
  (`agent/louis-agent/voice.md`, the default; `agent/louis-agent/voice-orchestrator.md`), or a language
  (`agent/puls/voice-af.md`, `voice-en.md`) that loads its product's shared `voice.md` after it. A session names
  `{product}/{mode}`; each product routes only what it does. Why: [ADR 0019](adr/0019-modes-one-file-per-product.md).
- **Every agent loads the principles first** — `Prompts/shared/principles.md` (never lie, cheat, steal or murder)
  opens every prompt, at every level and in the orchestrator. It states intent; the guards in code enforce.
  Why: [ADR 0020](adr/0020-principles-for-every-agent.md).
- **stdout is a protocol** — the ACP and MCP servers speak on it, so logs go through `ILogger` to stderr
  ([Messages and logging](../louis-agent.core/CLAUDE.md#messages-and-logging)).
  Why: [ADR 0009](adr/0009-message-text-as-enum-templates.md).
- **Test first** — a step that changes behaviour starts with its tests, run and seen failing, then the code; one
  commit with both. Why: [ADR 0014](adr/0014-test-first.md).
- **Tests are platform-neutral** — the agent runs them on Linux in its containers
  ([Writing tests](../tests/CLAUDE.md#writing-tests)).
- **One type per doc** — every doc is a tutorial, how-to, reference, explanation or plan, and opens with what it answers;
  new text goes in the doc of its type ([docs index: Writing docs](README.md#writing-docs-one-type-per-doc)).
  Why: [ADR 0013](adr/0013-documentation-structure.md).

## Quick start

```bash
cp config/.env.example config/.env
cp config/.env.secrets.example config/.env.secrets   # add ANTHROPIC_API_KEY
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
dotnet run --project src/louis-agent.cli               # interactive CLI
```

Docker (web app + API on http://127.0.0.1:5080):

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

Puls (its web app on http://127.0.0.1:5070, its specialist on 5071):

```bash
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
```

Always pass `--env-file config/.env`; compose needs it for `REPOSITORIES_PATH`. Requires the .NET 10 SDK.

## Project structure

Where things are. What's inside each project (files, key types, settings, tests, limits) is in its design doc in
[projects/](projects/README.md); core's folders are in [Project layout](../louis-agent.core/CLAUDE.md#project-layout).

```
louis-agent.core/        # The engine: the loop, tools, skills, providers, usage and prices, bootstrap; Prompts/: the built-in prompts
src/
├── louis-agent.cli/          # Terminal host
├── louis-agent.acp-server/   # Rider (Agent Client Protocol over stdio)
├── louis-agent.api/          # Minimal API: sessions + SSE, workspace and git endpoints; serves a web app
├── louis-agent.web/          # Blazor WebAssembly app for code review
├── louis-agent.puls/         # Puls: the general assistant's web app (the API built with -p:WebApp=puls)
├── louis-agent.web.shared/   # What both web apps share
├── louis-agent.mcp-server/   # Publishes the tools over MCP (no model)
└── louis-agent.orchestrator/ # POC: a second agent on core that has louis-agent fix a service's errors
tools/louis-agent.usage-probe/ # A provider's raw usage; prices and totals a ledger (--ledger, --cache-gaps)
tests/                   # NUnit: core.tests (mirrors core, plus ArchitectureTests, SkillsLayoutTests), api.tests, orchestrator.tests
docker/                  # Dockerfiles and compose files (main, demo, puls)
config/                  # .env + .env.secrets (ignored) and their examples, LLM profiles, prices.json, rates.json
Skills/                  # What the agent writes: agent/functions/ (CreateSkillFile), tools/ (agent-built tools)
docs/                    # This guide, features/, plans/, fixes/, specs/, projects/, learning/
```

## Development workflow

**The how-tos for one folder live in that folder's `CLAUDE.md`**, which Claude Code loads only when it works on files
there, so a session pays for them only when they apply:

| Guide | Holds |
|---|---|
| [tests/CLAUDE.md](../tests/CLAUDE.md) | Running the tests, the golden files (bash and PowerShell), fakes and platform-neutral tests |
| [src/CLAUDE.md](../src/CLAUDE.md) | The hosts' own message enums, adding a web theme |
| [louis-agent.core/CLAUDE.md](../louis-agent.core/CLAUDE.md) | Adding a C# tool or a toolset, the project layout, messages and logging, another agent on core, changing the LLM |

### Adding a skill

A function skill is `Skills/agent/functions/{name}.md`, on disk: the agent writes one with `CreateSkillFile`, or you
drop one in. Every one loads with the **skills** toolset, no setting needed (CL9 retired `AGENT_FUNCTION`); a session
without it (Puls's chats: `specialists` and `web`) gets its mode and its toolsets' guides, no skill file. A toolset's
guide is [Adding a toolset](../louis-agent.core/CLAUDE.md#adding-a-toolset). A name that is a built-in prompt (a
toolset's guide) is refused: what is embedded is built in (`SkillFiles.IsBuiltIn`). Procedures use `## Skill: Name` with an `Execution:` fence (bash, powershell or python); arguments
arrive as `SKILL_ARG_*` environment variables, so quote them and never build commands from them:

````markdown
## Skill: MySkill

- Description: What it does.
- Parameters:
  - `name` (string): What it is.
- Execution:

```bash
echo "Hello, ${SKILL_ARG_name}"
```
````

### Adding a mode or a role

A session names its personality when it opens (`POST /sessions` with `"personality": "{name}"`; a sub-session through
`StartSubSession(…, personality)`), and keeps it, as it keeps its toolsets (F21). Without one it gets the default mode,
`louis-agent/voice`. Everything here is under `louis-agent.core/Prompts/`, embedded and read only from the assembly, so
rebuild the images that use core.

1. **A mode** (level 0) is one file in a product's folder: its voice (the first `# ` section), then its routing (from
   the second `# ` heading on), named `{product}/{mode}` (`louis-agent/voice`). A **language** is
   `{product}/voice-{xx}.md` with only the voice; it loads the product's shared `voice.md` (the routing) after it, and
   Puls's *Taal* menu offers it as soon as `GET /personalities` does. Each product routes only what it does.
2. **A role** (level 1) is `specialists/{name}.md`: a specialist's function and how it reports to the agent that called
   it. A parent names it in `StartSubSession`; the specialists guide (`toolsets/specialists.md`) says which goes with
   which toolsets. **A minion's function** (level 2, F30) is `minions/{product}-minion-{function}.md`, loaded after its
   product's base (`minions/{product}-minion.md`: what every minion is, which is no role itself); the specialist hands
   the minion one tool of its own, so no file lists tools per role.
3. Names are lowercase letters, digits and dashes (a mode adds one `/`), used at one level only; a name from another
   level is refused as unknown. `PERSONALITIES_ENABLED` turns on only the listed ones, so a test can run Puls and the
   one role under test.
4. `GET /personalities` lists them by level, with the default; an unknown or invalid name is a 400 before any engine is
   built.

### Running and debugging

- Tool calls are logged to stderr: `[TOOL] -> Name(args)` and `[TOOL] <- Name: result` (or `failed: …`).
- In Docker, logs persist in `logs/`; `docker logs louis_agent_api` shows the API.
- With `LOG_DIRECTORY` set, each process's run file is there
  ([Messages and logging](../louis-agent.core/CLAUDE.md#messages-and-logging)); the orchestrator's lines are tagged
  `[orchestrator]`, louis-agent's progress under it `[louis-agent]`.

- Every model request is in the usage ledger, `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl` (tokens, cost, host, session,
  turn, fix and run; no prompt text). Total one with
  `dotnet run --project tools/louis-agent.usage-probe -- --ledger logs/usage-YYYY-MM.jsonl`.
- After changing the web app or API, rebuild the `api` image; after changing core, rebuild every image you use. Rider
  chats keep their container until you start a **New Chat**.
- After changing `config/.env.secrets`, recreate the containers with `docker compose … up -d`: `restart` keeps the old
  environment, so the new key never arrives. A changed prompt (`louis-agent.core/Prompts/`) is a change to core:
  rebuild the images that use it.
- Every image rebuild leaves build cache behind; a day of rebuilds filled Docker Desktop's disk (120 GB of cache) and
  stopped its engine (2026-10-15). `docker system df` shows it; `docker builder prune -af` clears it, safely, and
  the next build re-caches. If the daemon stops answering, restart Docker Desktop first.

## Which Anthropic tier

louis-agent sits at the Messages API tier on purpose, with its own loop, so the provider stays swappable. The four
tiers and why are in [ADR 0002](adr/0002-messages-api-tier-with-our-own-loop.md).

## MCP exposes tools, not a model

`louis-agent.mcp-server` hands an *external* client's model the raw tools, one call at a time; it has no model of its
own, so `AgentEngine` never calls its own tools through it. To have louis-agent do a whole task and report back (as the
orchestrator does), call `louis-agent.api`'s session endpoint instead. A `StartSubSession` made over MCP keeps nothing
open: only a chat's session holds sub-sessions for a follow-up (F26). `MCP_TRANSPORT=http` serves MCP over the network
and needs `MCP_API_KEY` (falls back to `AGENT_API_KEY`). The full comparison, both transports and the agent-to-agent
flow are in [AGENT_COMMUNICATION.md](AGENT_COMMUNICATION.md).

## Known limitations

The list is the TODO's [Known limitations](../TODO.md#known-limitations); when work removes one, delete it there.
One matters whenever you test a change by hand: on Claude Haiku 5.5 (the default), changing the tools or skills during
a chat (`CreateTool`, `/approve`, `/reject`, a skill reload) breaks that chat with a 400, so start a new chat
afterwards (limitation 9, [FX4](fixes/FX04-append-only-history.md)).

## Common errors

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'"** — set it in `config/.env.secrets`.
- **"prompt is too long"** — something put a huge result into the history; check `logs/oversized-tool-results.jsonl`.
- **"adaptive thinking is not supported on this model"** — a pre-4.6 Claude model not listed in
  `LlmClientFactory.ThinkingBudgetModelPrefixes`; add its prefix.
- **Skill not found** — the session doesn't have the `skills` toolset, the file isn't in `Skills/agent/functions/`, or
  the name differs (case-sensitive).
- **bash not found on Windows** — install Git for Windows or set `BASH_PATH` to `bash.exe`; WSL's bash is never used.
- **400 "Invalid `signature` in `thinking` block … bound to a different conversation"** — the system prompt or tool list
  changed under earlier thinking ([known limitation 9](../TODO.md#known-limitations)); start a new chat. See
  [FX4](fixes/FX04-append-only-history.md).
- **"Price table …"** stopping start-up (not valid, empty, a negative price, or a bad `long_prompt.above_tokens`) — fix
  `config/prices.json` (or the `PRICES_FILE` / `PRICES_URL` table): every model needs all five prices, none negative.
  **"[WARN] No price table"** means costs are recorded as unknown.
- **"Model '…' isn't pulled into Ollama"** (usage probe) or a 404 from Ollama — `docker exec louis_ollama ollama pull <model>`.

## Further reading

[the architecture](architecture/README.md) for the design, `config/.env.example` for settings, and
[the coding agent's mode](../louis-agent.core/Prompts/agent/louis-agent/voice.md) and [Puls's](../louis-agent.core/Prompts/agent/puls/voice-af.md) for how the agent is instructed to behave.

---
[Docs index](README.md) · Previous: [Architecture](architecture/README.md) · Next: [Orchestrator runbooks](projects/orchestrator-runbooks.md)
