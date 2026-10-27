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

1. `dotnet build louis-agent-solution.sln`, and `dotnet test` on the projects the step touches ([Tests](../tests/CLAUDE.md)); the whole
   solution before a PR.
2. If tools, their descriptions, the tool texts (the `*ToolMessage` enums) or the prompts in `louis-agent.core/Prompts/` changed, regenerate the golden files and review
   their diff ([golden files](../tests/CLAUDE.md#golden-files)).
3. Update the feature or fix doc with what the step learned; on the last step, follow *Finishing a feature* or
   *Finishing a fix* in their READMEs. The plan changes only under *Approved during the build*. New text goes in the
   doc of its type (*Rules that always apply*: one type per doc).
4. Format the commit's C# files: `dotnet format louis-agent-solution.sln --include <its .cs files>`, or Rider's
   **Reformat Code** on each (*Formatting*); CI fails a push whose commits' files aren't formatted.
5. Commit the step on its own, test and code together, then explain it (rules 4 and 6), showing the test failing
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
  Why: [ADR 0002](adr/0002-messages-api-tier-with-our-own-loop.md), [ADR 0030](adr/0030-build-or-adopt.md) (build or
  adopt: what we take from libraries instead of hand-building).
- **Engine, identities and packs** (CL15) — core is the engine and names no product or country; an identity
  (`IAgentIdentity`) is who an agent is (voice, modes, roles, the packs it uses, by name), a pack (`IAgentPack`) what it
  can do (toolsets, providers, guides, settings); a pack never references another pack or an identity; only
  `src/louis-agent.composition` knows them all, and a host picks with `IDENTITIES` and `PACKS` at start-up.
  Why: [ADR 0027](adr/0027-engine-identities-and-packs.md).
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
- **The browser says what it is** (FX21) — the headless browser sends Chrome's own user agent or a name that says who
  reads and why (`BROWSER_USER_AGENT`, set by an identity, never a person's name or place), and never alters what it
  reports about automation; a site that refuses automated readers refuses this one.
  Why: [ADR 0038](adr/0038-the-browser-says-what-it-is.md).
- **Toolsets per session** (F5) — a session names its toolsets when it opens and keeps them, so its prefix stays
  cacheable; an engine per toolset list, with only those tools and their guides
  ([Adding a toolset](../louis-agent.core/CLAUDE.md#adding-a-toolset)). A role's file may add its own (F33), and a
  name ending in `?` loads only where the instance has that toolset.
  Why: [ADR 0006](adr/0006-toolsets-per-session.md), [ADR 0025](adr/0025-a-role-declares-its-toolsets.md).
- **Packs stay convertible to MCP** (CL15) — a pack could be served as an MCP server, and an MCP server could stand in
  for a pack (F36); we convert only when a real need comes. MCP plugs in at the toolset: a server becomes a
  one-toolset pack, listed when the host starts (or on an admin refresh, for new chats only), so every chat keeps its
  tool list. A pack's tools take plain data and return text, a failure as text with its reason; what they read from
  the session also arrives as a parameter, and no description names an identity; a pack frames the outside data it
  returns and bounds its own output; its guides are prose with no `## Skill:`; it reads its own settings and core's
  shared services, and names the packs it needs, never referencing their code; its tool names are unique and
  MCP-safe. `PackConvertibilityTests` holds the shapes, review the rest.
  Why: [ADR 0028](adr/0028-packs-stay-convertible-to-mcp.md).
- **The web shows the live results** (Louis, 2026-10-26) — a page renders what it last fetched from the API and fetches
  again after any write it makes (a grade, a re-score, a kept name); it never adds its own write to a copy, never keeps a
  count the API keeps, and never asks the person to reload. Change travels through a scoped state's `Changed` event.
  Why: [ADR 0042](adr/0042-the-web-shows-the-live-results.md).
- **Measure every request, and show it** — the usage ledger records each model request with its tokens and its cost,
  priced from a table dated by `as_of`, so later price changes never rewrite history; unknown prices are `null`, never
  0. Every host shows a turn's usage from the same records and totals (`UsageReport.Total`), so the views and the
  ledger agree.
  Why: [ADR 0007](adr/0007-measure-every-request.md).
- **Evals in C#** (F18) — every check, regression and demo is a C# eval of a real task on one harness
  (`evals/louis-agent.evals/`), each eval beside the identity or pack it proves; cost comes from the
  ledger, never from a grader; CI runs the graders on recorded transcripts, and a live run waits for Louis's go.
  Why: [ADR 0029](adr/0029-simulations-in-csharp.md).
- **Orchestration is an engine function** (F39) — one delegation function: agents as tools, typed results checked in
  code, one policy per turn tree, credentials per peer; the orchestrator (voorman) is an identity on core.
  Why: [ADR 0031](adr/0031-orchestration-is-an-engine-function.md).
- **History is append-only** — newer Claude models (Haiku 5.5 and later) bind thinking blocks to the system prompt,
  tools and earlier messages, so nothing may edit them mid-conversation ([FX1](fixes/FX01-round-limit-tools.md);
  [FX4](fixes/FX04-append-only-history.md) is still open).
  Why: [ADR 0008](adr/0008-history-is-append-only.md).
- **No message text inline in C#** — exceptions, log lines, tool results and user text are message enum members
  ([Messages and logging](../louis-agent.core/CLAUDE.md#messages-and-logging)).
  Why: [ADR 0009](adr/0009-message-text-as-enum-templates.md).
- **Choices are enums, not strings** — a value that names one of a fixed set (a provider, a renderer, a freshness, a
  mode) is an enum, parsed once where it enters with `SettingChoice.TryParse` (in `louis-agent.choices`; case ignored, aliases, never a number) and written with `.Wire()`, the text on `[WireName]`, never `ToString()`;
  code switches on the enum, and the "supported" list comes from it. Text stays text only up to that point. A name that
  repeats (a toolset, a role, a mode) is an enum too, even in an open set: each pluggable project (pack, identity, eval
  set) keeps its own copy, the rest share one; wire names stay as they are (Louis, 2026-10-27).
  Why: [ADR 0024](adr/0024-choices-are-enums.md), [ADR 0043](adr/0043-repeated-names-are-enums.md).
- **Records for values, classes for things** — a request, an answer, a key or a line of a file is a record; what owns a
  lock, a client or a file, or changes over time, is a class; a record holds no resource; both are sealed by default.
  Why: [ADR 0040](adr/0040-records-for-values-classes-for-things.md).
- **JSON through records** — a known shape (a body, a reply, a file) is a record the serializer writes and reads, with
  one options object per wire, never changed for one caller; `JsonObject` only for a shape decided at run time,
  `JsonElement` for input we don't own (a string with `GetString()`, never `GetRawText()`), `Utf8JsonWriter` only when
  the bytes are the contract (the protocol's lines, a hash's input).
  Why: [ADR 0041](adr/0041-json-through-records-by-hand-only-for-bytes.md).
- **Separate by concern** — a folder, class or component holds one concern; a folder with two kinds of thing or more
  than about six files gets subfolders by concern, and a page over about a hundred lines splits into components. A
  `.razor` holds markup only, no `@code` block: a component's C# goes in its `.razor.cs` code-behind, and logic that
  needs no component (text, search, formatting) goes in a plain class with NUnit tests. A file holds at most 200
  lines (a test file 400). Split a bigger class by responsibility: group its methods by the fields they use and the
  reason they would change, and give each group its own class named for what it does (`PaymoClient`, `DiffFormatter`);
  the original keeps only the flow and calls them, pure logic gets its own tests, I/O stays at the edge. A `partial`
  class (other than a code-behind, or one the `[GeneratedRegex]` generator requires), `#region`, or a `Helpers`/`Utils`/`Manager` grab-bag hides the size instead of
  splitting it. `FileSizeTests` and `RazorHasNoCodeTests` hold it.
  Why: [ADR 0010](adr/0010-folders-by-concern.md), [ADR 0034](adr/0034-code-a-person-can-maintain.md).
- **Every class and method has a summary** (Louis, 2026-10-27) — private and test ones included. Existing code is
  brought up to this rule file by file, and only the files in the step's own commit (`git diff --name-only` of the
  commit): every class and method in those files gets its parts, the ones the step didn't change too, and any that
  don't follow the rule are rewritten. A file the step only read, or one outside its commit, waits; never a big
  refactor of its own. A class (record, interface) has up to three parts, a method its `<summary>` (and a `<remarks>` when it carries
  a pattern of its own):
  - `<summary>`: what it does, written like a person (below).
  - `<remarks>`: when it carries a pattern, which one and **why it was chosen here**: the problem it solves for this
    class, and what we'd lose without it (rule 11).
  - `<seealso>`: one per ADR that shaped this class (not every ADR that applies), linked on GitHub, as
    `ADR 0024: choices are enums`.

  **Write them like a person, not a robot** (Louis, 2026-10-27; rule 4):
  1. Say what it does for you, the way you'd tell a colleague at your desk, in one or two short sentences.
  2. Leave out what the code already shows: the types, null, "returns", "true when", every edge case.
  3. Step numbers belong in the commit and the feature doc; the pattern and the ADRs have their own parts.

  | Robot | Person |
  |---|---|
  | *True when the browser address is set and is an absolute URL.* | *We can only use Chrome if we know where it runs.* |
  | *Strategy: the setup of the renderer named, else the first ready one in Setups; null when the name is unknown or none is ready.* | *Uses the renderer you named. If you named none, it takes the first one that's set up.* |
  | *Chrome.* | *This is the Chrome one.* |
  | *Null Object: whichever settings are given, a setup's renderer is the unavailable one exactly when the setup is not ready.* (a test) | *If a renderer isn't set up you get the "not set up" one, and only then.* |

  A whole class, `ChromeSetup`:

  ```csharp
  /// <summary>
  /// Reads pages through our own headless Chrome. If we don't know where Chrome runs, you get a renderer that tells
  /// the model to set BROWSER_URL.
  /// </summary>
  /// <remarks>
  /// Strategy, with a Null Object. Each renderer knows for itself whether it's set up and how to build itself, so
  /// "is it ready?" and "build it" can't drift apart, and a new renderer is one new class instead of another case in
  /// two switches. When Chrome isn't set up it hands back a renderer that explains what's missing, so nothing has to
  /// check for null and the model still gets a useful answer.
  /// </remarks>
  /// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
  /// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0038-the-browser-says-what-it-is.md">ADR 0038: the browser says what it is</seealso>
  internal sealed class ChromeSetup : IPageRendererSetup
  ```

  **Inside a method, explain the steps** (Louis, 2026-10-27): a step a reader can't follow at a glance (building a
  request, a `try` that hides a failure path, a trick such as `using var _`, a rule the code enforces) gets one plain
  `//` line above it: what it does and why, written like a person. Obvious lines (`_busy = true;`) stay bare, so the
  comments never drown the code. **Steps that belong together sit together, with one blank line between groups**,
  like paragraphs: each group does one job (set up the data, start the host, hand it back) and carries its comment
  line on top when it needs one; a test's arrange, act and assert are three groups. The same commit-files scope
  applies. From `NotesPanel.AddAsync`:

  ```csharp
  // Post it to this run's notes. The run id is escaped because it goes into the web address.
  using var request = new HttpRequestMessage(HttpMethod.Post, $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/notes") { Content = JsonContent.Create(body) };
  // We only need to know it worked. If the API says no, it throws, and the catch below shows why.
  using var _ = await Api.SendAsync(request);
  // Saved: empty the box, and tell the run page to fetch the notes again (we never add it to the list ourselves).
  _text = "";
  State.Written();
  ```

  And grouped, from a test's `StartAsync`:

  ```csharp
  // A store that already holds one finished run.
  var store = new InMemoryBenchmarkStore();
  await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5), 0));

  // The API with the admin key, that store, and a clock that never moves.
  var app = ApiTestHost.Build(adminKey: AdminKey, store: store, time: new FixedClock(Now));
  await app.StartAsync();

  return new Host(app, app.GetTestClient(), store);
  ```
- **No nested conditionals** (Louis, 2026-10-27) — a choice between more than two outcomes is never a ternary inside a
  ternary (`a ? x : b ? y : z`) or `if`s nested inside `if`s. It is a **switch expression on the shape**, one arm per
  case read like a table (`c switch { { Total: 1, Agreed: 1 } => …, { Agreed: 0 } => …, _ => … }`, C#'s property
  patterns), or **guard clauses**, one `if (…) return …;` per case from the top (Fowler's *Replace Nested Conditional
  with Guard Clauses*). One plain `a ? x : y` stays. The pattern goes in the method's `<remarks>`. Same scope as the
  summaries: a file in a step's commit is fixed whole, untouched files wait.
- **Formatting** (Louis, 2026-10-27) — the layout rules live in the root `.editorconfig`: four spaces, a 120-character
  line, one file-scoped namespace line with the usings above it, each brace on its own line, no trailing spaces.
  A line that is long only because of a web address (an ADR's `<seealso href>`) may run past 120: an address can't be
  wrapped without breaking it (Louis, 2026-10-27; the exception Google's style guides make).
  **Code and comments change only by editing the file** (an editor, Rider, Claude Code's own edit tool), and layout
  only through `dotnet format` or Rider's **Reformat Code**: never a home-made script (Python, sed, awk, PowerShell)
  that rewrites source files. Louis, 2026-10-27, after a script that added comments walked a sentence letter by letter
  and left 478 one-character comment lines in a test file.
  Rider applies them with **Reformat Code**; `dotnet format` applies them from the command line. Same scope as the
  summaries: the files in a step's commit are formatted whole, untouched files keep their old layout, never a big
  reformat of its own. CI's *Formatting* step checks the C# files of the commits each push brings (merges left out).
- **Namespace = folder path, dependencies one way, prompts in markdown, hosts stay thin** — `ArchitectureTests`
  holds the first two ([Project layout](../louis-agent.core/CLAUDE.md#project-layout)).
  Why: [ADR 0010](adr/0010-folders-by-concern.md), [ADR 0011](adr/0011-prompts-are-content.md).
- **Skill files follow the levels** — a built-in prompt lives in `louis-agent.core/Prompts/`, in the folder of the
  level that reads it (`agent/` level 0, `specialists/` level 1, `minions/` level 2) or in `toolsets/` and `shared/`
  when every level shares it; a session loads its role only from its own level's folder. They're read only from the
  assembly, so a wording change is a rebuild; the repo's `Skills/` holds only what the agent writes.
  `SkillsLayoutTests` holds it.
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
cp config/.env.secrets.example config/.env.secrets   # add ANTHROPIC_API_KEY and AGENT_API_KEY
dotnet build louis-agent-solution.sln
dotnet test tests/louis-agent.core.tests
dotnet run --project src/louis-agent.cli               # interactive CLI
```

Docker (web app + API on http://127.0.0.1:5080):

```bash
docker compose -f docker/docker-compose.yml --env-file config/.env up -d --build api
```

Puls (its web app on http://127.0.0.1:5070):

```bash
docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build
```

Always pass `--env-file config/.env`; compose needs it for `REPOSITORIES_PATH`. Requires the .NET 10 SDK.

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

### Adding a skill

A function skill is `Skills/agent/functions/{name}.md`, on disk: the agent writes one with `CreateSkillFile`, or you
drop one in. What the agent writes waits in `Skills/agent/functions/pending/` (git-ignored), active only in the engine
that wrote it, until you type `/approve {name}` in the chat, which moves it up; `/reject {name}` deletes it and `/tools`
lists what is pending (FX20, as agent-built tools do). Every approved one loads with the **skills** toolset, no setting needed (CL9 retired `AGENT_FUNCTION`); a session
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

A session names its role when it opens (`POST /v1/sessions` with `"role": "{name}"`; a sub-session through
`StartSubSession(…, role)`), and keeps it, as it keeps its toolsets (F21). In the code a role is
`Skills/Roles/RoleCatalog` and its level `RoleLevel` (CL13); CL14 made "role" the public word. Without one it gets the
default mode, `louis-agent/voice`. Everything here is under `louis-agent.core/Prompts/`, embedded and read only from the
assembly, so rebuild the images that use core.

1. **A mode** (level 0) is one file in a product's folder: its voice (the first `# ` section), then its routing (from
   the second `# ` heading on), named `{product}/{mode}` (`louis-agent/voice`). A **language** is
   `{product}/voice-{xx}.md` with only the voice; it loads the product's shared `voice.md` (the routing) after it, and
   Puls's *Taal* menu offers it as soon as `GET /v1/roles` does. Each product routes only what it does.
2. **A role** (level 1) is `specialists/{name}.md`: a specialist's function and how it reports to the agent that called
   it. A parent names it in `StartSubSession`; the specialists guide (`toolsets/specialists.md`) says which goes with
   which toolsets. A role that always needs a toolset declares it in a header at the top of its file, which the
   loader strips before the model sees it (F33, [ADR 0025](adr/0025-a-role-declares-its-toolsets.md)):

   ```markdown
   ---
   toolsets: shops?
   ---
   ```

   Those toolsets are added to what the parent passed; `?` loads one only where this instance has it (`shops` needs
   `SHOP_CATALOGUES` and `BROWSER_URL`), and skips it with a log line elsewhere.

   **A minion's function** (level 2, F30) is `minions/{product}-minion-{function}.md`, loaded after its product's base
   (`minions/{product}-minion.md`: what every minion is, which is no role itself); the specialist hands the minion one
   tool of its own, so no file lists tools per role.
3. Names are lowercase letters, digits and dashes (a mode adds one `/`), used at one level only; a name from another
   level is refused as unknown. `ROLES_ENABLED` turns on only the listed ones, so a test can run Puls and the
   one role under test.
4. `GET /v1/roles` lists them by level, with the default; an unknown or invalid name is a 400 before any engine is
   built.

### Running and debugging

- Tool calls are logged to stderr: `[TOOL] -> Name(args)` and `[TOOL] <- Name: result` (or `failed: …`). By default the arguments, the result and a
  failure's quoted reason show as hashes and lengths (`area=#3f9c0a1b2d4e (10)`), so a log shows what ran and how it ended, never what
  the user typed; `LOG_CONTENT=full` shows the values, on your own machine only (F18 step 4f).
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
and needs `MCP_API_KEY` (falls back to `AGENT_API_KEY`; without one it refuses to start, FX18). The full comparison, both transports and the agent-to-agent
flow are in [AGENT_COMMUNICATION.md](AGENT_COMMUNICATION.md).

## Known limitations

The list is the TODO's [Known limitations](../TODO.md#known-limitations); when work removes one, delete it there.
One matters whenever you test a change by hand: on Claude Haiku 5.5 (the default), changing the tools or skills during
a chat (`CreateTool`, `/approve`, `/reject`, a skill reload) breaks that chat with a 400, so start a new chat
afterwards (limitation 9, [FX4](fixes/FX04-append-only-history.md)).

## Common errors

- **"ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'"** — set it in `config/.env.secrets`.
- **"AGENT_API_KEY is required: set it in config/.env.secrets, or set AGENT_AUTH=off …"** (or `MCP_API_KEY (or
  AGENT_API_KEY)` from the MCP server over HTTP) — the host fails closed ([FX18](fixes/FX18-auth-fails-closed.md)):
  set the key, or `AGENT_AUTH=off` in `config/.env` on a machine whose ports stay on `127.0.0.1`. Then recreate the
  containers (`up -d`): `restart` keeps the old environment.
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
