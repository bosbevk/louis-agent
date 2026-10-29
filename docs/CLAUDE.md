# Louis Agent: Development Guide

## How to work with Louis

Louis is a software engineer learning to become an AI engineer through this project; learning matters more than speed.
The goal and the skill map are in [GOAL.md](GOAL.md).

1. **Louis drives the code.** Implement only what Louis asks for, one step at a time; planning, reviews and learning
   write-ups happen in the Claude project, so don't start the next feature on your own.
2. **Small steps, small features.** Follow the steps in the feature doc. Each step is one commit (or PR) with its
   test, small enough to review in about ten minutes. **A feature holds at most about ten steps** (Louis, 2026-10-28,
   after F18 ran to twenty-odd steps and ten days on one branch: "we also need to keep features smaller by making sub
   features"): anything bigger is a parent with dotted children, `F49.1`, `F49.2`, each with its own plan, branch, pull
   request to `main`, done-when and write-up, so `main` moves every few days. The parent keeps the goal, the order and
   its children's done-whens; a child's plan is written when the child starts.
3. **Plan first, and keep the plan.** Before the first step of a feature, or of a fix that takes more than one step,
   the plan Louis approved (made here or in the Claude project) is saved to [plans/](plans/README.md) as `Fnn-name.md`
   or `FXnn-name.md` and committed on its branch. Don't rewrite it afterwards: a change Louis approves during the build
   is added under *Approved during the build*; anything else the build learns goes in the feature or fix doc. **When
   the work is done, the plan goes** (Louis, 2026-10-28: "do we still need implemented feature plans? and feature
   docs?", then "yes add it"): the plan file is deleted, the feature or fix doc's lasting parts move to the project's
   design doc in [projects/](projects/README.md) and its write-up in [learning/](learning/), then that doc is deleted
   too; its README keeps one line per id, and git keeps the rest (the files as they were are at the tag `pre-cl24`).
4. **Explain every change:** what changed, why this design and not the alternatives, and what to look at closely.
   **In plain words** (Louis, 2026-10-26): the way a friend would explain it, casual but straight to the point, in
   short everyday sentences. Lead with what it means, then the detail; when a technical term is needed, say what it
   means the first time; name code (a class, a file) only where Louis needs to find it. This holds for every
   explanation: a step, a finding, a review, an answer.
5. **Stop after each step** and wait for Louis to review and understand it before starting the next one.
6. **Teach, don't just do.** Name the AI-engineering concept behind each step and where it sits in the GOAL.md skill map.
7. **Measure, don't assume.** Ask before anything that spends real money (benchmark or demo runs against the API) and
   before pushing.
8. **Write it up.** When a skill or feature finishes, write its learning write-up in [learning/](learning/) for Louis
   to read afterwards (Louis, 2026-10-19: Claude types the lesson).
9. **Source of truth:** [TODO.md](../TODO.md) (what's next), [features/](features/README.md) (how), [plans/](plans/README.md)
   (how we're building it now), [fixes/](fixes/README.md) (what broke and why) and [specs/](specs/) (why). Update them
   rather than duplicating them.
10. **Ask about every finding, with three options** (Louis, 2026-10-26). When a step turns up a finding that needs a
    choice the plan or the brief doesn't settle (a design point, a rule that clashes with the brief, a layout or
    naming question, a test that can't be written as asked), don't choose: stop and put **each finding to Louis on its
    own**, with **three options**, each with what it changes and what it costs, and the one you recommend and why.
    Wait for his pick before writing the code it decides. A mechanical call (a local variable's name, a test's
    wording) is not a finding.
11. **Look up the pattern, then give three options** (Louis, 2026-10-27). A design choice (how to shape a class, pick an
    implementation, hold a rule) is never "your call" and never the first pattern that comes to mind: look up the
    software patterns that fit what we are trying to achieve (a catalogue such as Refactoring.Guru, the Gang of Four,
    Fowler's refactorings and *Patterns of Enterprise Application Architecture*, Microsoft's .NET guidance), then put
    **three options** to Louis as rule 10 says, each naming its pattern in plain words, what it changes and what it
    costs, with the sources and the one you recommend for this goal. The pick is named in the commit, the
    explanation and the `<remarks>` of each class that carries it, with why it was chosen (*Every class and
    method has a summary*). A brief that leaves a design choice open says so as a finding instead.

## Steps, branches and commits

**Doing a step: test first** ([how](../tests/CLAUDE.md#test-first)):

1. Write the test for the step's behaviour (a fix: a test that reproduces the bug).
2. Run it and see it fail for the expected reason; keep the output for the explanation.
3. Implement until it passes, then tidy with the tests green.

Docs-only and configuration-only steps have no test to write first.

**Finishing a step** (before rule 5's stop):

1. Build the projects the step touches (in Rider, `build_project` through its MCP server; elsewhere
   `dotnet build <project>`), and `dotnet test` on them ([Tests](../tests/CLAUDE.md)); the whole solution
   (`dotnet build louis-agent-solution.sln`) before a PR.
2. If tools, their descriptions, the tool texts (the `*ToolMessage` enums) or the prompts in `louis-agent.core/Prompts/` changed, regenerate the golden files and review
   their diff ([golden files](../tests/CLAUDE.md#golden-files)).
3. Update the feature or fix doc with what the step learned; on the last step, follow *Finishing a feature* or
   *Finishing a fix* in their READMEs. The plan changes only under *Approved during the build*. New text goes in the
   doc of its type (*Rules that always apply*: one type per doc).
4. Format every C# file in the commit (*Formatting*), in this order and once: `git_status` (or `git diff --name-only`)
   for the commit's files; Rider's own formatter on each, through Rider's MCP server (`reformat_file`), which wraps
   long lines; `get_file_problems` on each (or `lint_files` on all of them) to see what Rider's inspections still
   flag, instead of re-reading the file; the `//` and `///` lines over 120 wrapped by hand; then
   `dotnet format louis-agent-solution.sln --include <its .cs files>` once, which moves usings and is what CI checks,
   so it goes last. Never a whole-solution `dotnet format` in a loop. CI fails a push whose commits' files aren't
   formatted.
5. Before committing, show the step (Louis, 2026-10-28): the files it changes, the commit message (what changed and
   why) and the explanation (rules 4 and 6, with the test failing before the code and passing after), and wait for
   Louis's ok. He reviews the step while it can still change; a commit is harder to reshape than a diff.
6. Commit the step on its own, test and code together, and stop.

**Branches** are off `main` and carry the plan's id: `feature/F22-role-tools`, `fix/FX14-sub-session-prompt-budget`,
`refactor/CL3-messages-and-logging`, a sub-feature `feature/F49.1-checks-contract`; `docs/{name}` for docs-only work.
The Claude project's `claude/*` branches keep their names.

**Commit messages** are `type(scope): step N - what changed`, and the body says why:

```
feat(f22): step 3 - ReadTable for spreadsheets
fix(fx14): the prompt budget counts cached tokens
docs(f5): review of PR #27 - toolset guides
```

`type` is `feat`, `fix`, `refactor`, `test` or `docs`; `scope` is the id in lowercase (`f22`, `fx14`, `cl3`, `f49.1`), or the
area when there is none (`ollama`, `samples`, `guide`); `step N -` only on a plan's steps. Never push without asking
(rule 7).

## Rules that always apply

The design decisions every change keeps, one line each. The *why* of each is its ADR in [docs/adr/](adr/README.md);
the *how* of the C#, Razor and docs rules loads with the file you edit, from `.claude/rules/` (`csharp.md`,
`razor.md`, `docs.md`), so it costs a session nothing until it applies.

**The engine and its parts**

- **Reuse before building** — reuse the repo's components and maintained packages (MIT, Apache, BSD); write our own
  only when nothing fits, and say in the plan which were considered. [ADR 0030](adr/0030-build-or-adopt.md).
- **Microsoft.Extensions.AI** — one `IChatClient` across providers; `FunctionInvokingChatClient` runs the tool loop; we
  sit at the Messages API tier with our own loop. [ADR 0002](adr/0002-messages-api-tier-with-our-own-loop.md).
- **Engine, identities and packs** — core names no product or country; an identity is who an agent is, a pack what it
  can do; a pack never references another pack or an identity; only `src/louis-agent.composition` knows them all
  (`IDENTITIES`, `PACKS`). [ADR 0027](adr/0027-engine-identities-and-packs.md).
- **Packs stay convertible to MCP** — a pack could be served as an MCP server and an MCP server stand in for a pack;
  `PackConvertibilityTests` holds the shapes. [ADR 0028](adr/0028-packs-stay-convertible-to-mcp.md).
- **Provider strategies** — where providers differ, one interface with an implementation per provider, picked in
  `LlmClientFactory`; never branch on the provider's name elsewhere. [ADR 0003](adr/0003-provider-strategies.md).
- **Toolsets per session** — a session names its toolsets when it opens and keeps them; an engine per toolset list; a
  role's file may add its own, `?` only where the instance has it. [ADR 0006](adr/0006-toolsets-per-session.md),
  [ADR 0025](adr/0025-a-role-declares-its-toolsets.md).
- **Skill files follow the levels, a level-0 session is a mode, every agent loads the principles first** — prompts
  are embedded content, by level, one mode file per product, `principles.md` opening every prompt.
  [ADR 0011](adr/0011-prompts-are-content.md), [0017](adr/0017-skills-by-level.md), [0018](adr/0018-built-in-prompts-embedded.md),
  [0019](adr/0019-modes-one-file-per-product.md), [0020](adr/0020-principles-for-every-agent.md).
- **Orchestration is an engine function** — agents as tools, typed results checked in code, one policy per turn
  tree; the orchestrator is an identity on core. [ADR 0031](adr/0031-orchestration-is-an-engine-function.md).
- **MCP exposes tools, not a model** — `louis-agent.mcp-server` hands an external client the raw tools; to have the
  agent do a whole task, call the API's sessions ([AGENT_COMMUNICATION.md](AGENT_COMMUNICATION.md)).

**Safety and measurement**

- **Arguments as data** — skill and tool arguments travel as environment variables or argument lists, never through
  a shell. [ADR 0004](adr/0004-arguments-as-data.md).
- **Guard the context** — oversized results summarised, cut-off calls never run, failures carry their reason.
  [ADR 0005](adr/0005-guard-the-context.md).
- **History is append-only** — nothing edits the system prompt, tools or earlier messages mid-conversation; the
  finished streamed turn is appended whole, thinking blocks included. [ADR 0008](adr/0008-history-is-append-only.md).
- **The browser says what it is** — Chrome's own user agent or an honest name set by the identity, never a disguise.
  [ADR 0038](adr/0038-the-browser-says-what-it-is.md).
- **Measure every request, and show it** — the usage ledger is the only source of cost, priced by a dated table;
  unknown is `null`, never 0; every host shows the same totals. [ADR 0007](adr/0007-measure-every-request.md).
- **Evals in C#** — every check is a C# eval on one harness, beside the identity or pack it proves; one event
  contract; a live run waits for Louis's go. [ADR 0029](adr/0029-simulations-in-csharp.md),
  [ADR 0047](adr/0047-protocol-v2-one-event-contract.md).
- **Nothing is backward compatible until something is sold** — no migration, alias, lenient reader or second parser
  for what we store or send between our own parts. [ADR 0049](adr/0049-nothing-is-compatible-before-a-sale.md).

**The code** (the how in `.claude/rules/csharp.md`)

- **No message text inline in C#**, and stdout is a protocol: logs go through `ILogger` to stderr.
  [ADR 0009](adr/0009-message-text-as-enum-templates.md).
- **Choices are enums, not strings**, parsed once with `SettingChoice.TryParse`, written with `.Wire()`; a repeated
  name is an enum too. [ADR 0024](adr/0024-choices-are-enums.md), [ADR 0043](adr/0043-repeated-names-are-enums.md).
- **Records for values, classes for things.** [ADR 0040](adr/0040-records-for-values-classes-for-things.md).
- **JSON through records**, never an anonymous object, a static `Of` to build one from another.
  [ADR 0041](adr/0041-json-through-records-by-hand-only-for-bytes.md).
- **MongoDB through records.** [ADR 0048](adr/0048-mongo-through-records.md).
- **Dates are UTC.** [ADR 0046](adr/0046-dates-are-utc.md).
- **Separate by concern**: 200 lines of code per file (tests 400), one concern per folder, namespace = folder path,
  dependencies one way. [ADR 0010](adr/0010-folders-by-concern.md), [0034](adr/0034-code-a-person-can-maintain.md),
  [0044](adr/0044-the-size-limit-counts-code.md).
- **Every class and method has a summary**, written like a person, with its pattern and its ADRs; a step inside a
  method gets one plain line. [ADR 0050](adr/0050-summaries-like-a-person.md).
- **No nested conditionals, and an endpoint is a named method** returning `TypedResults`.
  [ADR 0051](adr/0051-flat-conditionals-and-named-endpoints.md), [ADR 0045](adr/0045-typed-answers-and-a-shared-contract.md).
- **Edit, never a script; layout only through the formatter; a rename through Rider; Rider's tools first.**
  [ADR 0052](adr/0052-edit-never-script.md).
- **Test first**, and tests are platform-neutral (they run on Linux in the agent's containers).
  [ADR 0014](adr/0014-test-first.md).

**The web and the docs** (the how in `.claude/rules/razor.md` and `docs.md`)

- **A `.razor` holds markup only**, a page over about a hundred lines splits, **the web shows the live results**.
  [ADR 0034](adr/0034-code-a-person-can-maintain.md), [ADR 0042](adr/0042-the-web-shows-the-live-results.md).
- **One type per doc**; plans, feature and fix docs have a lifecycle and a done plan is deleted.
  [ADR 0013](adr/0013-documentation-structure.md).

## Quick start

[Get started](guides/get-started.md): the settings files, the build, the tests, the CLI and the two Docker stacks.

## Project structure

Where things are. What's inside each project (files, key types, settings, tests, limits) is in its design doc in
[projects/](projects/README.md); core's folders are in [Project layout](../louis-agent.core/CLAUDE.md#project-layout).

```
louis-agent.core/        # The engine: the loop, tools, skills, providers, usage and prices, bootstrap; Prompts/: the built-in prompts (no product)
louis-agent.choices/     # SettingChoice: the one parser for a name that is an enum member (a wire name is `[WireName]`, built on .NET's `JsonStringEnumMemberName`); references nothing, so every project, ours or anyone's, can use it (ADR 0043)
identities/              # Who an agent is (CL15): louis-agent.identity.puls (its voice, modes, roles and the packs it names)
packs/                   # What an agent can do (CL15): louis-agent.pack.shops (the shops toolset, its guide and settings)
src/
├── louis-agent.cli/          # Terminal host
├── louis-agent.acp-server/   # Rider (Agent Client Protocol over stdio)
├── louis-agent.api/          # Minimal API: sessions + SSE, workspace and git endpoints; serves a web app
├── louis-agent.web/          # Blazor WebAssembly app for code review
├── louis-agent.puls/         # Puls: the general assistant's web app (the API built with -p:WebApp=puls)
├── louis-agent.web.shared/   # What both web apps share
├── louis-agent.mcp-server/   # Publishes the tools over MCP (no model)
├── louis-agent.orchestrator/ # POC: a second agent on core that has louis-agent fix a service's errors
└── louis-agent.composition/  # The one project that knows every identity and pack; a host picks with IDENTITIES and PACKS
tools/louis-agent.usage-probe/ # A provider's raw usage; prices and totals a ledger (--ledger, --cache-gaps)
tests/                   # NUnit: core.tests (mirrors core, plus ArchitectureTests, SkillsLayoutTests), api.tests, orchestrator.tests,
                         #   identity.puls.tests, pack.shops.tests, composition.tests, web.shared.tests, puls.tests and web.tests (bUnit);
                         #   louis-agent.testing, web.testing and api.testing hold what they share; the tests that leave their process (git, bash, a browser, Mongo,
                         #   the eval runner) are apart in core., api., orchestrator. and pack.shops.integration-tests (F18 step 7.6)
docker/                  # This PC: images/ (the Dockerfiles; ollama/ for the local model), the compose stacks (main, puls) and searxng/
deploy/azure/            # The cloud: Puls on Azure Container Apps (the app yamls, the Tailscale sidecar, deploy.ps1, the how-to)
config/                  # What the app reads at run time: .env + .env.secrets (ignored) and their examples, LLM profiles, prices.json, rates.json; rider/ for the IDE client templates
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
| [louis-agent.core/CLAUDE.md](../louis-agent.core/CLAUDE.md) | Adding a C# tool or a toolset, the project layout, messages and logging, building an identity or a pack, another agent on core, changing the LLM |

The rest of the how-tos are in [guides/](guides/README.md): [Finish a step](guides/finish-a-step.md),
[Add a skill](guides/add-a-skill.md), modes and roles in [Build an identity](guides/build-an-identity.md#modes-roles-and-minions-the-rules),
[Debug and logs](guides/debug-and-logs.md), [Common errors](guides/common-errors.md).

## Known limitations

The list is the TODO's [Known limitations](../TODO.md#known-limitations); when work removes one, delete it there.

## Further reading

[the architecture](architecture/README.md) for the design, `config/.env.example` for settings, and
[the coding agent's mode](../louis-agent.core/Prompts/agent/louis-agent/voice.md) and [Puls's](../identities/louis-agent.identity.puls/Prompts/agent/puls/voice-af.md) for how the agent is instructed to behave.

---
[Docs index](README.md) · Previous: [Architecture](architecture/README.md) · Next: [Orchestrator runbooks](projects/orchestrator-runbooks.md)
