# Test projects — design

> **Kind:** test projects (NUnit, with bUnit for components) · **Paths:** the framework's in `src/tests/`, grouped
> like `src/` (`engine/`, `hosts/`, `evals/`, `web/`, `site/`, `tools/`); each demo's in its own `tests/`
> (`demos/puls/tests/`, `demos/louis-agent/tests/`, `demos/starter/tests/`); each pack's in `packs/tests/`; the
> shared fakes in `src/tests/shared/louis-agent.testing/` and, for the web apps, `src/tests/web/louis-agent.web.testing/`
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

A test that starts a real process, opens Mongo or SQL Server, or needs the Puls stack is an integration test. Its class
is marked `[Category("Integration")]`, and it lives in a project named `{name}.integration-tests`. That way a unit
project never starts a process. Today those are core's, the API's, the accounts', the orchestrator's, the starter's,
and the dotnet, git, recipes, shell and shop-prices packs'.

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

`src/tests/shared/louis-agent.testing` is a class library, not a test project. It holds the fakes and paths every test
project shares: `FakeChatClient`, `FailingStreamClient`, `StubHandler`, `ListUsageSink`, `FakePrompts`,
`FakeIdentity`, `FakePack`, `TestPaths` and more, and under `Replay/` the web's fakes, `FakeBrowser` and
`CountingSearch`. Each test project that uses them has a global using for it.

No test project references another test project. `ArchitectureTests` holds that (`NoTestProject_LeansOnCoresTests`).
`InternalsVisibleTo` gives the test projects access to the `internal` types in core and the orchestrator.

`louis-agent.testing` also has neutral sample names (`SampleToolset`, `SampleMode`, `SampleEval`, `SampleRole`,
`SampleIdentity`, `SampleScenario`, with `Fill` for a test's JSON), for a test that only stores, shows or parses a
name. A test that needs the real name uses its owner's enum member.

And it has `Outside`, which is how an integration test asks for the outside world. `Tool` skips a missing tool on
your own machine, but fails it where `INTEGRATION_REQUIRED` is set, which is CI. `Stack` always skips a missing
Puls-stack address.

`src/tests/hosts/louis-agent.api.testing` is a library too. It holds the in-memory API host, the built runner, the
stand-in dlls and the store contracts. Each is written once, and runs against memory in the unit tests and against
Mongo in the integration tests.

*Decided in:* F18 step 7.6.2.

![The types: two shared libraries every test project uses, fakes for the model, HTTP, the composition and the web, and fixtures for the web's snapshots](diagrams/tests-interface.svg)

### 4. Linux, always

The agent runs these tests on Linux, in its containers, and so does CI. So keep them platform-neutral: forward
slashes, and no paths that only work on Windows. HTTP is stubbed with handlers, so the web tests never call out. A
few tests skip themselves when an outside tool, such as git or a live network, isn't there.

### 5. Core's own tests

Core's tests mirror `louis-agent.core`'s folders, and each test's namespace is its folder, such as
`LouisAgent.Core.Tests.Agent.Turns`. The tools' tests aren't here: each pack has its own, in `packs/tests/`. And each
model provider has its own project, such as `louis-agent.core.anthropic.tests`. *Structure* lists the folders.

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
programs the guides hold, and one seam for a pack's screens.

### 6. The API's tests

The API's tests prove the framework's API, so they run on the framework alone. No product's identity, pack or file is
in them. They sit in `src/tests/hosts/` beside the other hosts' tests, and a rule test, `TestsNameNoProductTests`,
reads each of their project files and fails on a reference to a demo or to a pack that belongs to one.

The agent they talk to is made up. `TestAgent`, in `louis-agent.api.testing`, is an identity called `testagent`
with the shape a real one has: two language voices that share a routing voice, a specialist whose role adds the web
and the calculator, a reader that adds nothing of its own, and two minions on a shared base. Its prompts are held in
memory, each with the header the engine reads from a real file. Its packs are fakes under the real packs' names,
`TestTools.WebPack` and `TestTools.CalculatorPack`, because nothing under `src/` may reference a real pack. The
in-memory host, `ApiTestHost`, builds the API on this agent and the tests' own fake tools.

`AgentSessionsTests` covers how a session gets its toolsets and tools: the `skills` default, an empty list, unknown
names, the depth and the `specialists` rule, and sessions that share an engine but not a history. It needs no web
host, no network and no model. The sessions are built on an `AgentEngineFactory` with a chat client that throws if
it's ever called. `AskDepthTests` uses the same agent for who may ask whom: a specialist under itself, or the agent
under the specialist it asked, is refused.

Some API tests start a real child process where the eval runner would be. Two programs are built for that, each with
a project reference that brings no assembly. `src/tests/hosts/louis-agent.api.standin` stands in for the runner. It's
a small console app, not a test project, and the check's id picks what it does: print its arguments, dump the names
in its environment, stream lines, fail, sleep, or never read its stdin. `louis-agent.api.test-runner` is the real
runner with the harness's general checks only. It copies the framework's template eval beside it, so its listing
gives the built-in `stub` and `template-example`, and the integration tests read a file eval's identity, dataset and
graders from it.

The eval studio's API has its own project, `louis-agent.api.evals.tests`. Its scenario and draft tests need an eval
with more in it than the template has. `SampleEvalWorkspace` writes one into a temporary workspace for each test
class: a dev scenario with a follow-up, an expected budget and shop, and its categories, a held-out scenario, and the
name of one that isn't there. It writes the files through the same records the API reads them with.

*Decided in:* F5, F18 step 7.4, F166.1, [ADR 0076](../adr/0076-core-ships-no-agent.md).

### 7. The orchestrator's tests

`LouisAgentClientTests` parses the SSE stream: text, tool calls with their inputs and results, the stop reason,
errors, separate text blocks, and `FIX-RESULT` lines. `LouisAgentClientBodiesTests` holds what a fix session is sent
to the session wire's shared records, and `SessionTagsTests` holds the tags each fix session carries into the usage
records. `OrchestratorTests` covers the built-in workflow and the runbooks on disk, the fix prompt, replays refused
for a method with no runbook, the tools' logs on stderr, and a run that only the user's stop ends. `Comms/`, `Tools/`
and `Triage/` hold the comms log and its usage line, the service repository and state files, and the decisions.

The checks against a real git repository are in `louis-agent.orchestrator.integration-tests`: a fix's commit
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

### 9. The identities', the packs' and the composition root's tests

Each identity and each pack has its tests beside it.

Puls's identity tests prove its prompts are its own and are read through `SkillFileSource` (`PulsPromptsTests`, with
the Puls golden `PulsSessionSnapshot.json`). Its evals and their checks have tests too: `Evals/Routing/`, and
`Evals/Checks/`, with each check run on hand-cut answers, plus the cook's dataset, ids and conformance. The cook
scenarios' ids are test constants in `PulsCookScenarioIds`, because an id is data, not an enum. `PulsIdsTests` holds
each one to its dataset, and `RepeatedNameTests` lists that file and skips only that class. The judge's tests are in
`louis-agent.identity.puls.judges.tests`. The coding agent's catalogue has `louis-agent.coding-catalog.tests`, which
holds the golden of every text its tools return to the model (`ToolMessageSnapshotTests`), and the triage team has
`louis-agent.identity.triage.tests`.

The where-to-buy pack's tests read Nominatim's answers from hand-written fixtures in its `jsonv2` shape, and keep the
shop rules' tests, with their golden (`WhereToBuyToolMessageSnapshot.json`). The shop-prices pack's tests check the
price lists from the shipped tables, with their golden (`ShopPriceToolMessageSnapshot.json`), and its store runs
against a real Mongo in its integration tests.

The recipes pack's tests read a `Recipe` from recorded pages of sites that allow us (`Recipes/Fixtures/`, trimmed
copies under 30 KB, never served), and from small inline blocks for the shapes no page shows.

The composition root's tests (`louis-agent.host-packs.tests`) prove that `IDENTITIES` and `PACKS` pick what they
should (`PluginsTests`), and that every pack stays convertible to an MCP server (`PackConvertibilityTests`, through
the MCP SDK's own `McpServerTool.Create`, a test-only package). They also check that every built-in prompt of every
identity and pack opens with its header, and that its level is its folder's (`EveryPromptHasAHeaderTests`).

Each of these registers the prompts its identity or pack brings in a `[SetUpFixture]`, just as a host's `Build` does.

*Decided in:* CL15, F49.2, F50.2, F35.1, CL27, [ADR 0043](../adr/0043-repeated-names-are-enums.md).

### 10. The web's and the sites' tests

`louis-agent.web.tests` tests the framework's web. Its plain logic runs on NUnit, in folders that mirror the library:
`TurnStream` (the recorded turn, kept in `louis-agent.session-stream.tests/Fixtures/`, with a case per event it
folds), `Delegation` and `WorkStatus` (the states a card goes through), `UsageFormat` (priced, unpriced, the display
currency) and `Markdown` (a table, links, images, raw HTML). Its components run on bUnit, each rendered from the
recorded turn and held to the `.html` golden beside its test.

It also holds the web's rule that a `.razor` file holds markup only. `RazorHasNoCodeTests` scans every `.razor` under
`src/` for an `@code` block. And under `Rules/` it holds every site to the framework's site rules.

Puls's tests (`louis-agent.puls.tests`) cover only what's Puls's own: its pages, its layout, its words and its styles.
They pin `WorkPanel`, `PulsTile` and `TemplateCard`, and render `ChatPage` end to end against a stub API that opens a
session and streams the recorded turn. The empty page and the page with the turn are each a golden, and the JS calls
the page makes are listed. Its landing page is a golden too.

The code-review app's tests (`louis-agent.code-review.tests`) pin its chat page the same way, empty and with the turn.
The starter has its own (`louis-agent.starter.tests`), and `louis-agent.starter.integration-tests` spawns a site the
way `dotnet new louis-site` does.

`src/tests/web/louis-agent.web.testing` holds what they share: the fixture replay, the stub API (`FixtureApi`: fixed
JSON for what the apps read at start-up, one session, the recorded turn streamed, and the workspace and git reads
empty), `RecordingJSRuntime`, which lists every JS call, the snapshot helper and `SassStylesheet`. The eval pages, the
account pages, the docs, the framework's pages, the places, the themes and the diagrams each have a test project of
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

### 12. The eval projects' tests

`louis-agent.evals.tests` tests the harness: the dataset loader (Inspect's Sample shape read strictly, the hash, an
expected key no grader reads), the common graders, the model grader over Microsoft.Extensions.AI.Evaluation, the
roll-up, the conformance cases, the label sheet and the calibration, and the protocol writer against its golden.
`louis-agent.evals.checks.tests` tests the check contract.

`louis-agent.evals.protocol.tests` checks that every event round-trips through `EvalProtocolJson`, with `event` first
and the time written with a `Z`, and that a person's line or an unknown event reads as none.

`louis-agent.evals.runner.tests` covers the arguments, the catalogue, the stub eval's run against its golden, the
conformance of every eval folder, the data files copied beside the assembly, and the template folder
(`src/evals/template/Example/`: it loads with the general checks alone, conforms, and is never listed).

`louis-agent.identity.puls.judges.tests` runs Puls's judge `meal-for-people` on a scripted client, and checks the
prompt it sends against its golden, its rubric's hash, its entry and its list.

The runner end to end through the API is `StubCheckTests`, in `louis-agent.api.integration-tests`. A run that costs
money is never a test: CI grades only recorded transcripts.

*Decided in:* F18.

## Structure

The folders of `src/tests/engine/louis-agent.core.tests`, to look things up.

| Folder | Covers |
|---|---|
| `Agent/` | `AgentEngine`: tool loop, streaming, thinking, summarising, cut-off guards, round limit, `toolsets:`, sorted and unique tool names, discovered tools, skill authoring, its usage records |
| `Config/` | `LlmOptions` (provider inference, tool support, thinking, caching), `AgentOptions` (env binding, repository root), `EnvFile` |
| `Hosting/` | `AgentHost`: environment loading, price and rate loading from a URL with the file fallback, the system prompt per toolset profile; `AgentEngineFactory` (one engine per profile, the cap) |
| `Llm/` | The client factory, the usage recorder and the prompt's hash, and the model's answers recorded and replayed (`Replay/`) |
| `Logging/` | Serilog behind `ILogger`: stderr only, holes bound by name, tags, the run file and the JSON-lines records |
| `Messages/` | Every message enum member, core's and each host's (from source), has a template with well-formed holes |
| `Pricing/` | The price table and the shipped `config/prices.json`, costs, the pricing sink, exchange rates, the display currency |
| `Skills/` | `MarkdownSkillLoader` (procedures, parameters, fence languages); `Providers/`: each skill provider and the composite (order, de-duplication, the role first); `Headers/`: a prompt's header and the catalogue that reads them once (CL27; the loader's side in `Hosting/Prompts/SkillLoaderHeadersTests`); `Roles/`: the role catalogue, minions' roles, and `RoleNamingTests` (nothing in core named "personality", CL13, CL14); the API has `ApiRoleNamingTests` |
| `RepeatedNames/` | The ratchets of ADR 0043: a wire name written as a string outside its enum (`RepeatedNameTests`, with `Scanning/`: a C# lexer, the scan, the compiled-code reader), and `.Wire()` as the one way out (`WireTextTests`: no direct `SettingChoice.Wire`, no case-folded `ToString()`, no `ToString()` of an enum in the compiled code). Five exemptions, each with its reason, narrowed rather than lengthened (F18 step 7.5) |
| `Usage/` | Scopes and their totals, the ledger file, reports, the usage text and sessions, `CacheGaps` |
| `Guardrails/` | The workspace boundary and secret files, the SSRF guard, and `HostAuth`, the key rule every host shares |
| `Delegation/` | Asking another agent: addresses and the `Calls` that allow them, `AskAgent`'s limits, follow-ups and peers, the did line, `ReadAgentWork`, minions, and the sub-session's stream, keys, bodies, usage and trace |
| `Sessions/` | A session's replay and its store, and the sub-sessions a chat keeps with their work |
| `Telemetry/` | The seam with no collector (`HostTelemetryTests`) and a turn's span (`TurnSpansTests`) |
| `Composition/` | What core uses from the composition: the process runner, the workspace listing, an agent's address, a turn's identity |
| `Categories/` | The ratchets on the tests' marks and where they live |
| `Docs/` | The ratchets on the docs: the work log, the roadmap, the write-ups, the guides' listings, one seam for screens, and under `Instructions/` the size of what Claude loads and the skill files |
| `Projects/` | Core's packages (`CorePackagesTests`) |
| `Testing/`, `TestHttp/` | The tests of the shared `Outside` and `TestPaths`, and a header-echo handler |
| The root | `ArchitectureTests`, `FileSizeTests`, `CodeLineCountTests`, `FolderSizeTests`, `SolutionLayoutTests`, `SiteBoundaryTests`, `PackWebBoundaryTests`, `ChoicesProjectTests`, `EvalProtocolProjectTests`, `DiagramsProjectTests`, `EngineAsksItsTypesTests`, `PromptFolderTests`, `SkillsLayoutTests` (part 5), and `TestPrompts`, the `[SetUpFixture]` that registers the tests' own prompts (`prompts/`: a product, a specialist, a minion) before the first is read |

## Extending it

The steps are in [Write and run tests](../guides/write-tests.md). In short: the test goes in the folder that mirrors
the class it tests, with the matching namespace, and it uses the fakes in `src/tests/shared/louis-agent.testing`. A
test that leaves its process goes in an `.integration-tests` project, with the mark. Every step of a plan
([src/docs/plans/](../plans/README.md)) names its test.

## Limits and plans

The orchestrator's whole loop, run against a scripted louis-agent.api, has no test yet.

## Related docs

[Write and run tests](../guides/write-tests.md) · [Architecture § Testing](../architecture/08-crosscutting-concepts.md#testing)

---
[Projects](README.md) · Previous: [louis-agent.evals.runner](louis-agent.evals.runner.md)
