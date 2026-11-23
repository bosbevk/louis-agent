# Test projects — design

> **Kind:** test projects (NUnit, with bUnit for components) · **Paths:** the framework's in `src/tests/`, grouped
> like `src/` (`engine/`, `hosts/`, `evals/`, `web/`, `site/`, `tools/`); each demo's in its own `tests/`
> (`demos/louis-agent/tests/`, `demos/starter/tests/`); each pack's in `packs/tests/`; the
> shared fakes in `src/tests/shared/LouisAgent.Testing/` and, for the web apps, `src/tests/web/LouisAgent.Web.Testing/`
> (class libraries) · **Run:** `dotnet test louis-agent-solution.sln` (or each project)
> **Also shows:** `package`, `sequence`

![The framework's tests: a fake model, golden files, and the integration tests apart](diagrams/tests.svg)

## Purpose

The tests are fast, offline checks of the behaviour that matters: the tools, the skills, the engine's loop and its
guards, the configuration, the sites, and the orchestrator's parsing and checking. A unit test needs no network, no
model and no Docker. Where a test needs a model, a fake chat client plays it.

This doc first says how the tests are organised: unit and integration tests apart, golden files, what they share, and
why they run on Linux. Then it goes through the test projects, one group at a time. It ends with the folders of core's
tests, to look things up, and what isn't covered yet.

## How it works

### 1. Unit and integration tests, kept apart

A test that starts a real process, opens Mongo or SQL Server, or needs a running stack is an integration test. Its class
is marked `[Category("Integration")]`, and it lives in a project named `{name}.integration-tests`. That way a unit
project never starts a process. Today those are core's, the API's, the accounts', the orchestrator's, the starter's,
the packages', and the dotnet, git and shell packs'.

CI (`.github/workflows/ci.yml`) runs on Ubuntu, on each pull request and each merge to main. It first runs the whole
solution with `--filter "Category!=Integration"`, then with `--filter "Category=Integration"` and
`INTEGRATION_REQUIRED=1`.

Two ratchets in `core.tests/Categories/` keep this honest. `IntegrationTestsAreMarkedTests` checks that a test that
asks for the outside, or uses a process, Mongo or the runner, is marked, with no raw skip. `IntegrationPlacementTests`
checks that the mark and the project agree.

*Decided in:* F18 step 7.6.

![The tests: what CI runs, and the two testing libraries they share](diagrams/tests-diagrams.svg)

### 2. Golden files

Some tests compare what the code produces with a file saved beside the test, called a golden file. The test passes
when they match, and fails when they differ. When a change is meant, you write the golden again with
`LOUIS_UPDATE_GOLDEN=1`, read the diff, and commit it.

![A golden file: the snapshot is written when you ask, and otherwise the test passes when it's the same and fails when it differs](diagrams/tests-diagrams-2.svg)

### 3. What the test projects share

`src/tests/shared/LouisAgent.Testing` is a class library, not a test project. It holds the fakes and paths every test
project shares: `FakeChatClient`, `FailingStreamClient`, `StubHandler`, `ListUsageSink`, `FakePrompts`,
`FakeIdentity`, `FakePack`, `TestPaths` and more, and under `Replay/` the web's fakes, `FakeBrowser` and
`CountingSearch`. Each test project that uses them has a global using for it.

No test project references another test project. `ArchitectureTests` holds that (`NoTestProject_LeansOnCoresTests`).
`InternalsVisibleTo` gives the test projects access to the `internal` types in core and the orchestrator.

`LouisAgent.Testing` also has neutral sample names (`SampleToolset`, `SampleMode`, `SampleEval`, `SampleRole`,
`SampleIdentity`, `SampleScenario`, with `Fill` for a test's JSON), for a test that only stores, shows or parses a
name. A test that needs the real name uses its owner's enum member.

And it has `Outside`, which is how an integration test asks for the outside world. `Tool` skips a missing tool on
your own machine, but fails it where `INTEGRATION_REQUIRED` is set, which is CI. `Stack` always skips a missing
stack's address.

`src/tests/hosts/LouisAgent.Api.Testing` is a library too. It holds the in-memory API host, `ApiTestHost`, and the
test agent every API test opens its sessions on, `TestAgent`. Both are written once, for the unit and the
integration tests alike.

*Decided in:* F18 step 7.6.2.

![The types: two shared libraries every test project uses, fakes for the model, HTTP, the composition and the web, and fixtures for the web's snapshots](diagrams/tests-interface.svg)

### 4. Linux, always

The agent runs these tests on Linux, in its containers, and so does CI. So keep them platform-neutral: forward
slashes, and no paths that only work on Windows. HTTP is stubbed with handlers, so the web tests never call out. A
few tests skip themselves when an outside tool, such as git or a live network, isn't there.

### 5. Core's own tests

Core's tests mirror `LouisAgent.Core`'s folders, and each test's namespace is its folder, such as
`LouisAgent.Core.Tests.Agent.Turns`. The tools' tests aren't here: each pack has its own, in `packs/tests/`. And each
model provider has its own project, such as `LouisAgent.Core.Anthropic.Tests`. *Structure* lists the folders.

Core's tests also hold the ratchets that keep the whole repo in shape. A ratchet is a test with a list of today's
exceptions that may only shrink: a new offender fails it, and so does a listed one that's fixed and not taken off.
`FileSizeTests` holds a file to 200 lines of code, a test file to 400, and `CodeLineCountTests` pins what counts:
code only, never a summary or a blank line. `FolderSizeTests` holds a folder to six source files, a component's
markup and code-behind counting as one. `ArchitectureTests` holds the layout and the references: namespace = folder,
dependencies one way, no pack referencing another pack or an identity, no framework project referencing a demo, and
core naming no product.

A handful more each guard one boundary. `SolutionLayoutTests` holds the folders on disk to what ships: `src/` holds
only the framework, and anything that names a product is under `demos/`. `SiteBoundaryTests` holds that nothing else
in `src/` references the site. `PackWebBoundaryTests` keeps a pack and its web views apart. `ChoicesProjectTests`,
`EvalProtocolProjectTests` and `DiagramsProjectTests` keep those three projects leaves. `EngineAsksItsTypesTests`
holds the diagram engine to asking its types. `PromptFolderTests` holds that the prompts' folder is named in one place,
and `SkillsLayoutTests` that skill files follow the levels.

The docs have their ratchets in `Docs/`: the work log, the roadmap, the write-ups, the size of what Claude loads, the
programs the guides hold, and one seam for a pack's screens. `OldWordsTests` sits there too: no file outside a few
kept places says "specialist" or "minion", and no text a model or a person reads ever does, since the levels below
a voice are a subagent and a lookup agent. It was a ratchet while CL69 renamed them, and holds every file now.

### 6. The API's tests

The API's tests prove the framework's API, so they run on the framework alone. No product's identity, pack or file is
in them. They sit in `src/tests/hosts/` beside the other hosts' tests, and a rule test, `TestsNameNoProductTests`,
reads each of their project files and fails on a reference to a demo or to a pack that belongs to one.

The agent they talk to is made up. `TestAgent`, in `LouisAgent.Api.Testing`, is an identity called `testagent`
with the shape a real one has: two language voices that share a routing voice, a subagent whose role adds the web
and the calculator, a reader that adds nothing of its own, and two lookup agents on a shared base. Its prompts are held in
memory, each with the header the engine reads from a real file. Its packs are fakes under the real packs' names,
`TestTools.WebPack` and `TestTools.CalculatorPack`, because nothing under `src/` may reference a real pack. The
in-memory host, `ApiTestHost`, builds the API on this agent and the tests' own fake tools.

`AgentSessionsTests` covers how a session gets its toolsets and tools: the `skills` default, an empty list, unknown
names, the depth and the lookup agent's toolsets, and sessions that share an engine but not a history. It needs no web
host, no network and no model. The sessions are built on an `AgentEngineFactory` with a chat client that throws if
it's ever called. `AskDepthTests` uses the same agent for who may ask whom: a subagent under itself, or the agent
under the subagent it asked, is refused.

*Decided in:* F5, F18 step 7.4, F166.1, [ADR 0076](../adr/0076-core-ships-no-agent.md).

### 7. The orchestrator's tests

`LouisAgentClientTests` parses the SSE stream: text, tool calls with their inputs and results, the stop reason,
errors, separate text blocks, and `FIX-RESULT` lines. `LouisAgentClientBodiesTests` holds what a fix session is sent
to the session wire's shared records, and `SessionTagsTests` holds the tags each fix session carries into the usage
records. `OrchestratorTests` covers the built-in workflow and the runbooks on disk, the fix prompt, replays refused
for a method with no runbook, the tools' logs on stderr, and a run that only the user's stop ends. `Comms/`, `Tools/`
and `Triage/` hold the comms log and its usage line, the service repository and state files, and the decisions.

The checks against a real git repository are in `LouisAgent.Orchestrator.IntegrationTests`: a fix's commit
verified on its branch, and a run that returns to main whatever a fix left behind. They need git, and skip without
it.

### 8. The MCP server's tests

These run the MCP SDK's real server and client, joined by two in-memory pipes, so there's no port and no process.
`InMemoryMcp.cs` sets that up; it isn't a test itself.

`Resources/FeedResourcesTests.cs` checks that each feed is listed as `feeds://{name}` with its description and
`application/json`, that it reads back as its JSON, and that an unknown URI gets the protocol's error.
`McpPrimitivesTests.cs` checks the one setup both transports use: the catalogue's feeds as resources, beside the
engine's tools.

*Decided in:* F56.

### 9. The identities', the packs' and the catalogue's tests

Each identity and each pack has its tests beside it. The coding agent's catalogue has
`LouisAgent.Coding.Catalog.Tests`, which holds the golden of every text its tools return to the model
(`ToolMessageSnapshotTests`) and runs the shared MCP check on the packs it loads, and the triage team has
`LouisAgent.Identities.Triage.Tests`.

The rules every pack keeps are held over all of them at once, in `LouisAgent.Packs.Rules.Tests`, the one test project
that references every pack in `packs/tools`. Its `EveryPack` lists one of each, and a test holds that list to the
projects in the folder, so a new pack is held as soon as it exists. Every pack stays convertible to an MCP server
(`PackConvertibilityTests`, through the MCP SDK's own `McpServerTool.Create`, which `LouisAgent.Testing` carries),
against every identity's name in the repository, read from the identities' folders, with no pack excused. And every
built-in prompt of every pack and identity, read from the prompt folders its project file names on the disk, opens
with its header, whose level is its folder's, and every skill of an identity is a folder holding its `SKILL.md`
(`EveryPromptHasAHeaderTests`).

Puls's own tests, its identity's, its packs' and its catalogue's, live with it in
[its own repository](https://github.com/bosbevk/puls).

Each of these registers the prompts its identity or pack brings in a `[SetUpFixture]`, just as a host's `Build` does.

*Decided in:* CL15, F49.2, F50.2, F35.1, CL27, [ADR 0043](../adr/0043-repeated-names-are-enums.md).

### 10. The web's and the sites' tests

`LouisAgent.Web.Tests` tests the framework's web. Its plain logic runs on NUnit, in folders that mirror the library:
`TurnStream` (the recorded turn, kept in `LouisAgent.SessionStream.Tests/Fixtures/`, with a case per event it
folds), `Delegation` and `WorkStatus` (the states a card goes through), `UsageFormat` (priced, unpriced, the display
currency) and `Markdown` (a table, links, images, raw HTML). Its components run on bUnit, each rendered from the
recorded turn and held to the `.html` golden beside its test.

It also holds the web's rule that a `.razor` file holds markup only. `RazorHasNoCodeTests` scans every `.razor` under
`src/` for an `@code` block. And under `Rules/` it holds every site to the framework's site rules.

The code-review app's tests (`LouisAgent.CodeReview.Tests`) pin its chat page, empty and with the turn, against a
stub API that opens a session and streams the recorded turn.
The starter has its own (`LouisAgent.Starter.Tests`), and `LouisAgent.Starter.IntegrationTests` spawns a site the
way `dotnet new louis-site` does.

The framework as a product installs it is tested from outside the repository. `LouisAgent.Packages.IntegrationTests`,
in `src/tests/packages/`, packs the framework's packages into a folder at a version of its own, checks that a package names its
framework dependencies at the versions it was packed with, and writes a console project in a temporary folder that
knows only that folder and nuget.org, then runs an agent from it. `PackagesTests` in `core.tests` holds which projects
are packages, and `PublishWorkflowTests` and `ConsumeWorkflowTests` hold the two workflows that publish them and
restore them from GitHub Packages ([Publish the framework](../guides/publish-the-framework.md)).

`src/tests/web/LouisAgent.Web.Testing` holds what they share: the fixture replay, the stub API (`FixtureApi`: fixed
JSON for what the apps read at start-up, one session, the recorded turn streamed, and the workspace and git reads
empty), `RecordingJSRuntime`, which lists every JS call, the snapshot helper and `SassStylesheet`. The account pages, the docs, the framework's pages, the places, the themes and the diagrams each have a test project of
their own in `src/tests/web/`. The framework's sites have theirs in `src/tests/site/`.

*Decided in:* F18 step 7.2, F59.10.

### 11. The ACP server's tests

`AcpTranscriptTests` plays a recorded Rider conversation through `AcpServer`. The conversation has a bad line, an
unknown method, `initialize`, `session/new`, and one prompt with a file attached and another open in the IDE, which
the scripted model answers with a thought, a tool call and a reply. Rider's side of the pipe is `ScriptedConsole`.

Everything the server wrote to stdout, and the prompt the model received, is pinned to `AcpTranscript.json`. Session
and message ids are numbered, and the temp folder is named, so the file stays the same from run to run.
`LOUIS_UPDATE_GOLDEN=1` writes it again. It's the proof that what the client sees doesn't change when the server's
inside does.

The pure parts have their own tests, in folders that match the source. `ToolCards` checks a card's kind and the cut
preview. `PromptBuilder` checks text, an embedded resource, an attached file read and capped, the open file left out,
and an unmounted one reported. `ContainerPaths` checks a mapping, a root searched by the last parts of the path, and
the drive letter. And there are `GitRoots` and `JsonRpcWriter`.

### 12. The evals' tests

`LouisAgent.Evals.Tests` holds the evals' unit tests, over transcripts written by hand and a fake judge's model.
They read each fact and refuse a wrong one, read the guide's file of seven conversations, refuse every fault of a
file at its place, hold the schema to its golden file, tell the seven facts, and pin what the judge's model is told to
`Grading/Golden/JudgeRequest.txt`. `LouisAgent.Evals.IntegrationTests` calls a real Claude model, to prove it answers
as a typed record and to have the judge mark two criteria. It asks for the key with `Outside.Key`, so it skips without
one, in CI too. The design is in [LouisAgent.Evals](LouisAgent.Evals.md).

The same project holds a run's tests: a turn's stream written down, the session client over a handler that answers,
one conversation played over a fake sessions API, the pass rule, a run's repeats and its stops, the run's store over
a folder of its own, and the evals found in file providers built in memory.

`LouisAgent.Api.Evals.Tests` runs the API in memory with the evals mapped, a test identity whose folder holds an
eval, a fake model for the agent and one for the judge. It holds the four routes and every refusal of a start, the
desk reading a cut-off run, the proof that the engine's replay client serves the judge, and a live run recording and
its replay. `LouisAgent.Api.Evals.Testing` is a class library, not a test project: `EvalReplay` replays an eval from
recordings kept in git on a product's own API, facts only and with no key, and the site's tests use it for the
guide. The design is in [LouisAgent.Api.Evals](LouisAgent.Api.Evals.md).

What a page asks of a run, its counts, cost, time, first miss and order, is `RunAnswersTests` in
`LouisAgent.Evals.Tests`. The admin's eval pages are `LouisAgent.Web.Evals.Tests`, in `src/tests/web/`: the client
over a stub of the API, the addresses, and each page with bUnit over `EvalPagesHost`, a stub of every route the pages
read. The design is in [LouisAgent.Web.Evals](LouisAgent.Web.Evals.md).

## Structure

The folders of `src/tests/engine/LouisAgent.Core.Tests`, to look things up.

| Folder | Covers |
|---|---|
| `Agent/` | `AgentEngine`: tool loop, streaming, thinking, summarising, cut-off guards, round limit, `toolsets:`, sorted and unique tool names, discovered tools, skill authoring, its usage records |
| `Config/` | `LlmOptions` (provider inference, tool support, thinking, caching), `AgentOptions` (env binding, repository root), `EnvFile` |
| `Hosting/` | `AgentHost`: environment loading, price and rate loading from a URL with the file fallback, the system prompt per toolset profile; `AgentEngineFactory` (one engine per profile, the cap) |
| `Llm/` | The client factory, the usage recorder and the prompt's hash, and the model's answers recorded and replayed (`Replay/`) |
| `Logging/` | Serilog behind `ILogger`: stderr only, holes bound by name, tags, the run file and the JSON-lines records |
| `Messages/` | Every message enum member, core's and each host's (from source), has a template with well-formed holes |
| `Pricing/` | The price table and the shipped `config/prices.json`, costs, the pricing sink, exchange rates, the display currency |
| `Skills/` | `MarkdownSkillLoader` (procedures, parameters, fence languages); `Providers/`: each skill provider and the composite (order, de-duplication, the role first); `Headers/`: a prompt's header and the catalogue that reads them once (CL27; the loader's side in `Hosting/Prompts/SkillLoaderHeadersTests`); `Roles/`: the role catalogue, lookup agents' roles, and `RoleNamingTests` (nothing in core named "personality", CL13, CL14); the API has `ApiRoleNamingTests` |
| `RepeatedNames/` | The ratchets of ADR 0043: a wire name written as a string outside its enum (`RepeatedNameTests`, with `Scanning/`: a C# lexer, the scan, the compiled-code reader), and `.Wire()` as the one way out (`WireTextTests`: no direct `SettingChoice.Wire`, no case-folded `ToString()`, no `ToString()` of an enum in the compiled code). Five exemptions, each with its reason, narrowed rather than lengthened (F18 step 7.5) |
| `Usage/` | Scopes and their totals, the ledger file, reports, the usage text and sessions, `CacheGaps` |
| `Guardrails/` | The workspace boundary and secret files, the SSRF guard, and `HostAuth`, the key rule every host shares |
| `Delegation/` | Asking another agent: addresses and the `Calls` that allow them, `AskAgent`'s limits, follow-ups and peers, the did line, `ReadAgentWork`, lookup agents, and the sub-session's stream, keys, bodies, usage and trace |
| `Sessions/` | A session's replay and its store, and the sub-sessions a chat keeps with their work |
| `Telemetry/` | The seam with no collector (`HostTelemetryTests`) and a turn's span (`TurnSpansTests`) |
| `Composition/` | What core uses from the composition: the process runner, the workspace listing, an agent's address, a turn's identity |
| `Categories/` | The ratchets on the tests' marks and where they live |
| `Docs/` | The ratchets on the docs: the work log, the roadmap, the write-ups, the guides' listings, one seam for screens, and under `Instructions/` the size of what Claude loads and the skill files |
| `Projects/` | Core's packages (`CorePackagesTests`) |
| `Testing/`, `TestHttp/` | The tests of the shared `Outside` and `TestPaths`, and a header-echo handler |
| The root | `ArchitectureTests`, `FileSizeTests`, `CodeLineCountTests`, `FolderSizeTests`, `SolutionLayoutTests`, `SiteBoundaryTests`, `PackWebBoundaryTests`, `ChoicesProjectTests`, `DiagramsProjectTests`, `EngineAsksItsTypesTests`, `PromptFolderTests`, `SkillsLayoutTests` (part 5), and `TestPrompts`, the `[SetUpFixture]` that registers the tests' own prompts (`agents/` with a subagent and a lookup agent inside: a product, a subagent, a lookup agent and its base) before the first is read |

## Extending it

The steps are in [Write and run tests](../guides/write-tests.md). In short: the test goes in the folder that mirrors
the class it tests, with the matching namespace, and it uses the fakes in `src/tests/shared/LouisAgent.Testing`. A
test that leaves its process goes in an `.integration-tests` project, with the mark. Every step of a plan
([src/docs/plans/](../plans/README.md)) names its test.

## Limits and plans

The orchestrator's whole loop, run against a scripted LouisAgent.Api, has no test yet.

## Related docs

[Write and run tests](../guides/write-tests.md) · [Architecture § Testing](../architecture/08-crosscutting-concepts.md#testing)

---
[Projects](README.md) · Previous: [LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)
