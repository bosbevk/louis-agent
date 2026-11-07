# Louis Agent: Development Guide

## How to work with Louis

Louis is a software engineer learning to become an AI engineer through this project; learning matters more than speed.
The goal and the skill map are in [GOAL.md](GOAL.md).

1. **Louis drives the code.** Implement only what Louis asks for, one step at a time; planning, reviews and learning
   write-ups happen in the Claude project, so don't start the next feature on your own.
2. **Small steps, small features.** Each step is one commit (or PR) with its test, small enough to review in about
   ten minutes. A feature holds at most about ten steps; anything bigger is a parent with dotted children (`F49.1`),
   each its own plan, branch, pull request to `main`, done-when and write-up, so `main` moves every few days.
3. **Plan first, and keep the plan.** Before the first step, the plan Louis approved is saved to
   [plans/](plans/README.md) and committed on its branch; it changes only under *Approved during the build*. When
   the work is done the plan goes: its lasting parts move to an ADR, the project's design doc or a write-up, and the
   file is deleted (`.claude/rules/docs.md` has the lifecycle).
4. **Explain every change** in plain words, the way a friend would: what it means first, then what changed, why this
   design and not the alternatives, and what to look at closely; say what a term means the first time; name code only
   where Louis needs to find it. This holds for every explanation: a step, a finding, a review, an answer.
5. **Stop after each step** and wait for Louis to review and understand it before starting the next one.
6. **Teach, don't just do.** Name the AI-engineering concept behind each step and where it sits in the GOAL.md skill map.
7. **Measure, don't assume.** Ask before anything that spends real money (a run against the API) and before pushing.
8. **Write it up.** When a skill or feature finishes, write its learning write-up in [learning/](learning/) for Louis
   to read afterwards.
9. **Source of truth:** [TODO.md](../../TODO.md) (what's next), [plans/](plans/README.md) (how we're building it now),
   [fixes/](fixes/README.md) (what broke and why), [projects/](projects/README.md) (how each project works) and
   [adr/](adr/README.md) (why). Update them rather than duplicating them.
10. **Ask about every finding, with three options.** When a step turns up a choice the plan or the brief doesn't
    settle (a design point, a rule that clashes with the brief, a layout or naming question, a test that can't be
    written as asked), don't choose: put each finding to Louis on its own, with three options, each with what it
    changes and what it costs, and the one you recommend and why. Wait for his pick. A mechanical call (a local
    variable's name, a test's wording) is not a finding.
11. **Look up the pattern, then give three options.** A design choice (how to shape a class, pick an implementation,
    hold a rule) is never "your call" and never the first pattern that comes to mind: look up the patterns that fit
    (Refactoring.Guru, the Gang of Four, Fowler, Microsoft's .NET guidance), then put three options to Louis as rule
    10 says, each naming its pattern in plain words, with the sources and the one you recommend. The pick is named in
    the commit, the explanation and the `<remarks>` of each class that carries it ([ADR 0050](adr/0050-summaries-like-a-person.md)).

## Steps, branches and commits

**Doing a step: test first** ([how](guides/write-tests.md#write-the-test-first)): write the test for the step's
behaviour (a fix: one that reproduces the bug), see it fail for the expected reason and keep the output, then
implement until it passes and tidy with the tests green. Docs-only and configuration-only steps have no test first.

**Finishing a step** ([the how-to](guides/finish-a-step.md)), before rule 5's stop:

1. Build and test only what the step touches (`core.tests` too if it moves files or adds names); the full suite only once the feature is built.
2. If tools, their descriptions, the tool texts or the prompts changed, regenerate the golden files and review their diff.
3. Update the feature or fix doc with what the step learned; on the last step, *Finishing a feature* or *Finishing a
   fix* in their READMEs.
4. Format every C# file in the commit, in order and once: Rider's formatter, Rider's inspections, long comment lines
   by hand, then `dotnet format … --include <its files>` last (CI checks with it). Only the commit's files, whole.
5. Show the step before committing: the files it changes, the commit message and the explanation (with the test
   failing before the code and passing after), and wait for Louis's ok.
6. Commit the step on its own, test and code together, and stop. Never push without asking (rule 7).

**In Rider, Rider's tools come first** ([ADR 0052](adr/0052-edit-never-script.md)). A session starts with `/mcp` to see
what Rider offers, and for any job a Rider tool covers (format, inspect, build, test, rename, find usages, symbol
info, git status) it uses that tool, never a hand edit, a script (Python, sed, PowerShell) or a search-and-replace
that does the same thing less well: formatted with Rider's formatter, not re-typed; renamed with Rider's rename, not
replaced; problems from Rider's inspections, not from re-reading the file. A shell command is for what no Rider tool
does; *Brave mode* stays off. The tool-to-job table per Rider version is in [Finish a step](guides/finish-a-step.md#riders-tools-by-job).

**Branches** are off `main` and carry the plan's id: `feature/F22-role-tools`, `fix/FX14-sub-session-prompt-budget`,
`refactor/CL3-messages-and-logging`, `feature/F49.1-checks-contract`; `docs/{name}` for docs-only work.

**Commit messages** are `type(scope): step N - what changed` (`feat(f22): step 3 - ReadTable for spreadsheets`,
`fix(fx14): the prompt budget counts cached tokens`, `docs(f5): review of PR #27 - toolset guides`), and the body says
why. `type` is `feat`, `fix`, `refactor`, `test` or `docs`; `scope` is the id in lowercase (`f22`, `fx14`, `cl3`,
`f49.1`), or the area when there is none (`ollama`, `guide`); `step N -` only on a plan's steps.

## Rules that always apply

The design decisions every change keeps, one line each: the *why* is its ADR in [src/docs/adr/](adr/README.md), the
*how* of the C#, Razor, CSS and docs rules loads with the file you edit, from `.claude/rules/`.

**The engine and its parts**

- **Reuse before building, and keep it simple** — reuse the repo's components and maintained packages (MIT, Apache,
  BSD); the simplest design that meets the step, nothing for a future no plan names. [ADR 0030](adr/0030-build-or-adopt.md), [ADR 0055](adr/0055-keep-it-simple.md).
- **A tool when it improves the prompt** — if a tool would make a prompt's job work better or more accurately, make the
  tool instead of another prompt rule; judgement stays in the prompt. [ADR 0080](adr/0080-a-tool-when-it-improves-the-prompt.md).
- **Microsoft.Extensions.AI** — one `IChatClient` across providers; `FunctionInvokingChatClient` runs the tool loop; we
  sit at the Messages API tier with our own loop. [ADR 0002](adr/0002-messages-api-tier-with-our-own-loop.md).
- **Framework and implementation are split, and this comes first** — `src/` is framework only, a demo holds its own
  config, docker, skills, deploy and tests; the solution tree is the disk; core ships no agent and every tool the model asks for is a pack. [ADR 0074](adr/0074-the-folders-say-what-ships.md), [0076](adr/0076-core-ships-no-agent.md).
- **Engine, identities and packs** — core names no product or country; an identity is who, a pack what it can do; a pack
  references no other pack, identity or eval; only `demos/puls/hosts/louis-agent.host-packs` knows them all. Weigh every reference against
  the engine split; a shared contract is a shared project or a tested duplicate. [ADR 0027](adr/0027-engine-identities-and-packs.md), [0073](adr/0073-packs-reference-no-evals.md).
- **Packs stay convertible to MCP** (`PackConvertibilityTests` holds the shapes) and **MCP exposes tools, not a
  model**: `louis-agent.mcp` hands an external client the raw tools; to have the agent do a whole task, call
  the API's sessions. [ADR 0028](adr/0028-packs-stay-convertible-to-mcp.md), [AGENT_COMMUNICATION.md](AGENT_COMMUNICATION.md).
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

**Safety and measurement**

- **Arguments as data** (never through a shell) and **guard the context** (oversized results summarised, cut-off
  calls never run, failures carry their reason). [ADR 0004](adr/0004-arguments-as-data.md), [0005](adr/0005-guard-the-context.md).
- **History is append-only** — nothing edits the system prompt, tools or earlier messages mid-conversation; the
  finished streamed turn is appended whole, thinking blocks included. [ADR 0008](adr/0008-history-is-append-only.md).
- **The browser says what it is, and takes a site's no** — Chrome's own user agent or an honest name, never a disguise;
  every fetch obeys `robots.txt` and the page's no-AI signals, in code; a page's own data (JSON-LD) is read in code
  before its text. [ADR 0038](adr/0038-the-browser-says-what-it-is.md), [0068](adr/0068-every-fetch-obeys-robots-txt.md), [0067](adr/0067-a-pages-own-data-before-its-text.md).
- **Measure every request, and show it** — the usage ledger is the only source of cost, priced by a dated table;
  unknown is `null`, never 0; every host shows the same totals. [ADR 0007](adr/0007-measure-every-request.md).
- **Evals in C#** — every check is a C# eval on one harness, under `evals/`, apart from what it proves; one event
  contract; a live run waits for Louis's go. [ADR 0029](adr/0029-simulations-in-csharp.md),
  [ADR 0047](adr/0047-protocol-v2-one-event-contract.md).
- **Nothing is backward compatible until something is sold** — no migration, alias, lenient reader or second parser
  for what we store or send between our own parts. [ADR 0049](adr/0049-nothing-is-compatible-before-a-sale.md).

**The code** (the how in `.claude/rules/csharp.md`)

- **No message text inline in C#**, and stdout is a protocol: logs go through `ILogger` to stderr.
  [ADR 0009](adr/0009-message-text-as-enum-templates.md).
- **Choices are enums, not strings**, parsed once with `SettingChoice.TryParse`, written with `.Wire()`; a repeated
  name is an enum too. [ADR 0024](adr/0024-choices-are-enums.md), [ADR 0043](adr/0043-repeated-names-are-enums.md).
- **Records for values, classes for things; JSON and MongoDB through records**, never an anonymous object, a static
  `Of` to build one from another; **dates are UTC**. [ADR 0040](adr/0040-records-for-values-classes-for-things.md),
  [0041](adr/0041-json-through-records-by-hand-only-for-bytes.md), [0048](adr/0048-mongo-through-records.md),
  [0046](adr/0046-dates-are-utc.md).
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
  [ADR 0014](adr/0014-test-first.md). **Test what you touch** while coding; the full suite once, when the feature is built. [ADR 0075](adr/0075-test-what-you-touch.md).

**The web and the docs** (the how in `.claude/rules/razor.md` and `docs.md`)

- **Every site is built on the framework's web:** what would be the same on every site is in `louis-agent.web`, which names no
  site; a site holds only its words, brand, colours, addresses, own pages and which packs and views it uses.
  [ADR 0084](adr/0084-every-site-is-built-on-the-frameworks-web.md).
- **A `.razor` holds markup only**, a page over about a hundred lines splits, **the web shows the live results**.
  [ADR 0034](adr/0034-code-a-person-can-maintain.md), [ADR 0042](adr/0042-the-web-shows-the-live-results.md).
- **One type per doc**; plans, feature and fix docs have a lifecycle and a done plan is deleted.
  [ADR 0013](adr/0013-documentation-structure.md).

## Project structure

Where things are. What's inside each project is in its design doc in [projects/](projects/README.md); core's folders
in [Project layout](../engine/louis-agent.core/CLAUDE.md#project-layout).

```
src/engine/              # louis-agent.core (the loop, delegation, web access and its guards, skills, providers, usage, bootstrap), .choices (the one enum-name parser), .session-stream, .composition (the contracts everything agrees on); louis-agent.core.web (the browser, search, robots, rendering, the recorders) and .core.telemetry (the tracer, the collector's sink), each on core behind a seam, a host opts in (CL33)
src/hosts/               # louis-agent.cli, .acp-server (Rider), .api (sessions + SSE; serves a web app), .api.contracts, .mcp-server (the tools, no model)
src/web/                 # louis-agent.web: the framework's web, what every site shares; louis-agent.web.accounts: the account pages, a site opts in
src/evals/               # The eval harness, its protocol, the checks contract, the runner and the template folder
packs/                   # What an agent can do, one area (CL32): tools/ (the packs, tools only: files, git, dotnet, shell, web, paymo, devops, calculator, where-to-buy, shop-prices, recipes), evals/ (what proves a pack, apart so someone can take the tools without it; only the runner references these), tests/ and web/ (a pack's own tests and its own web views, apart from its tools, ADR 0086), docs/
demos/puls/              # A demo, as someone's own project would be: identity/ (who it is), evals/ (an eval is a folder of files), web/ (its app), hosts/ (host-packs picks its packs), config/, docker/, skills/ (what the agent writes), deploy/ (Azure), tests/, docs/, and git-ignored data/ and logs/
demos/louis-agent/       # Our own demo: identity/ (the coding agent), web/ (code review, the site), hosts/ (the orchestrator POC, and the programs that start the framework's hosts), config/, docker/, skills/, tools/ (the film export), tests/, docs/, and git-ignored data/ and logs/
demos/starter/           # The smallest site, and what `dotnet new louis-site` makes ([Spawn a site](guides/spawn-a-site.md))
src/tools/               # louis-agent.usage-probe: a provider's raw usage; prices and totals a ledger
src/tests/               # Only the framework's tests, grouped like src/ ([tests](projects/tests.md)); a demo holds its own tests/ and packs/tests/ the packs'; the ones that leave their process are *.integration-tests
src/docs/                # The framework's docs: this guide, adr/, guides/, projects/, plans/, fixes/, learning/, architecture/
.claude/rules/           # The how of the C#, Razor, CSS and docs rules, loaded by file path
```

## Development workflow

Start with [Get started](guides/get-started.md). **The how-tos for one folder live in that folder's `CLAUDE.md`**, and
the how of a kind of file in `.claude/rules/`; Claude Code loads each only when it works on such a file:

| Guide | Loads for | Holds |
|---|---|---|
| [src/tests/CLAUDE.md](../tests/CLAUDE.md) | `src/tests/` | Running the tests, the golden files, fakes and platform-neutral tests |
| [src/CLAUDE.md](../CLAUDE.md) | `src/` | The hosts' own message enums, adding a web theme |
| [src/engine/louis-agent.core/CLAUDE.md](../engine/louis-agent.core/CLAUDE.md) | `src/engine/louis-agent.core/` | Adding a C# tool or a toolset, the project layout, messages and logging, changing the LLM |
| `.claude/rules/csharp.md` | `**/*.cs` | The how of every C# rule: summaries, conditionals, records, enums, size, formatting |
| `.claude/rules/razor.md` | `**/*.razor`, `**/*.razor.cs` | Markup only, split by a hundred lines, the live results |
| `.claude/rules/css.md` | `**/*.css` | 200 lines of rules a file, `app.css` the import list in cascade order, the byte-checked move |
| `.claude/rules/docs.md` | `**/*.md` | One type per doc, the plan and fix-doc lifecycle, write-ups, English, plain words, the 200-line rule |

Every `CLAUDE.md` and every rules file holds at most 200 lines (`ClaudeMdSizeTests`); the rest of the how-tos are
in [guides/](guides/README.md): [Finish a step](guides/finish-a-step.md), [Add a skill](guides/add-a-skill.md),
modes and roles in [Build an identity](guides/build-an-identity.md#modes-roles-and-minions-the-rules),
[Debug and logs](guides/debug-and-logs.md), [Common errors](guides/common-errors.md).

**Known limitations** are the TODO's [list](../../TODO.md#known-limitations); when work removes one, delete it there.
**Further reading:** [the architecture](architecture/README.md), a demo's `config/.env.example` (settings), [the coding agent's mode](../../demos/louis-agent/identity/louis-agent.identity.coding/Prompts/agent/louis-agent/voice.md).

---
[Docs index](README.md) · Previous: [Architecture](architecture/README.md) · Next: [Orchestrator runbooks](../../demos/louis-agent/docs/projects/orchestrator-runbooks.md)
