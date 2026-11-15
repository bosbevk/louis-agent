# Test projects — design

> **Kind:** test projects (NUnit) · **Paths:** `src/tests/engine/louis-agent.core.tests/`, `demos/puls/tests/louis-agent.api.tests/`,
> `demos/louis-agent/tests/louis-agent.orchestrator.tests/`, `demos/puls/tests/louis-agent.identity.puls.tests/`, `packs/tests/louis-agent.pack.where-to-buy.tests/`, `packs/tests/louis-agent.pack.shop-prices.tests/`,
> `packs/tests/louis-agent.pack.recipes.tests/`, `demos/puls/tests/louis-agent.identity.puls.evals.tests/`, `packs/tests/louis-agent.pack.where-to-buy.checks.tests/`, `packs/tests/louis-agent.pack.recipes.checks.tests/`, `demos/puls/tests/louis-agent.host-packs.tests/`, `src/tests/hosts/louis-agent.mcp.tests/`, `src/tests/web/louis-agent.web.tests/`, `demos/puls/tests/louis-agent.puls.tests/`,
> `demos/louis-agent/tests/louis-agent.code-review.tests/`; the tests that leave their process in `src/tests/engine/louis-agent.core.integration-tests/`, `.api.`, `.orchestrator.` and `.pack.shop-prices.integration-tests/` (F18 step 7.6); the fakes they share in `src/tests/shared/louis-agent.testing/` and, for the web apps,
> `src/tests/web/louis-agent.web.testing/` (class libraries, CL15) ·
> **Run:** `dotnet test louis-agent-solution.sln` (or each project)
> **Also shows:** `package`, `sequence`

![The framework's tests: a fake model, golden files, and the integration tests apart](diagrams/tests.svg)

## Purpose

The tests are fast, offline checks of the behaviour that matters: the tools, the skills, the engine's loop and its
guards, the configuration, and the orchestrator's parsing and checking. A unit test needs **no network, no model and
no Docker**. Where a test needs a model, a fake chat client plays it.

This doc first says how the tests are organised, then goes through the test projects one by one.

## How it works

### 1. Unit and integration tests, kept apart

A test that starts a real process, opens Mongo or needs the Puls stack is an integration test. Its class is marked
`[Category("Integration")]`, and it lives in a project named `{name}.integration-tests`. That way a unit project
never starts a process.

The solution folders `unit`, `integration` and `shared` hold the projects. CI first runs
`--filter "Category!=Integration"`, then `--filter "Category=Integration"` with `INTEGRATION_REQUIRED=1`.

`louis-agent.api.testing`, in the shared folder, holds the in-memory API host, the built runner, the stand-in dlls and
the store contracts. Each is written once, and runs against memory in the unit tests and against Mongo in the
integration tests.

Two ratchets in `core.tests/Categories/` keep this honest. `IntegrationTestsAreMarkedTests` checks that a test that
asks for the outside, or uses a process, Mongo or the runner, is marked, with no raw skip. `IntegrationPlacementTests`
checks that the mark and the project agree.

*Decided in:* F18 step 7.6.

![The tests: what CI runs, and the two testing libraries they share](diagrams/tests-diagrams.svg)

### 2. Golden files

Some tests compare what the code produces with a file saved beside the test, called a golden file. The test passes
when they match, and fails when they differ. When a change is meant, you write the golden again with
`LOUIS_UPDATE_GOLDEN=1`.

![A golden file: the snapshot is written when you ask, and otherwise the test passes when it's the same and fails when it differs](diagrams/tests-diagrams-2.svg)

### 3. What the test projects share

`src/tests/shared/louis-agent.testing` is a class library, not a test project. It holds the fakes and paths every test
project shares: `FakeChatClient`, `FailingStreamClient`, `StubHandler`, `ListUsageSink`, `FakePrompts`,
`FakeIdentity`, `FakePack`, `TestPaths` and more. Each test project that uses them has a global using for it.

No test project references another test project, and `core.tests` opens its internals to none. `ArchitectureTests`
holds both rules. `InternalsVisibleTo` gives the test projects access to the `internal` types in core and the
orchestrator.

`louis-agent.testing` also has neutral sample names (`SampleToolset`, `SampleMode`, `SampleEval`, `SampleRole`,
`SampleIdentity`, `SampleScenario`, with `Fill` for a test's JSON), for a test that only stores, shows or parses a
name. A test that needs the real name uses its owner's enum member.

And it has `Outside`, which is how an integration test asks for the outside world. `Tool` skips a missing tool on
your own machine, but fails it where `INTEGRATION_REQUIRED` is set, which is CI. `Stack` always skips a missing
Puls-stack address.

*Decided in:* F18 step 7.6.2.

![The types: two shared libraries every test project uses, fakes for the model, HTTP, the composition and the web, and fixtures for the web's snapshots](diagrams/tests-interface.svg)

### 4. Linux, always

The agent runs these tests on Linux, in its containers. So keep them platform-neutral: forward slashes, and no paths
that only work on Windows. HTTP is stubbed with handlers, so the web tests never call out. A few tests skip
themselves when an outside tool, such as git or a live network, isn't there.

## louis-agent.core.tests

Core's own tests. Their folders mirror `louis-agent.core`'s, and each test's namespace is its folder, such as
`LouisAgent.Core.Tests.Agent.Turns`. The tools' tests aren't here: each pack has its own, in `packs/tests/`.

| Folder | Covers |
|---|---|
| `Agent/` | `AgentEngine`: tool loop, streaming, thinking, summarising, cut-off guards, round limit, `toolsets:`, sorted and unique tool names, discovered tools, skill authoring, its usage records |
| `Config/` | `LlmOptions` (provider inference, tool support, thinking, caching), `AgentOptions` (env binding, repository root), `EnvFile` |
| `Hosting/` | `AgentHost`: environment loading, price and rate loading from a URL with the file fallback, the system prompt per toolset profile; `AgentEngineFactory` (one engine per profile, the cap) |
| `Llm/` | The client factory, the usage recorder and the prompt's hash, and the model's answers recorded and replayed (`Replay/`); each provider's own tests are in its project, such as `louis-agent.core.anthropic.tests` |
| `Logging/` | Serilog behind `ILogger`: stderr only, holes bound by name, tags, the run file and the JSON-lines records |
| `Messages/` | Every message enum member, core's and each host's (from source), has a template with well-formed holes; the golden of every text tools return to the model is the coding catalogue's (`ToolMessageSnapshotTests`) |
| `Pricing/` | The price table and the shipped `config/prices.json`, costs, the pricing sink, exchange rates, the display currency |
| `Skills/` | `MarkdownSkillLoader` (procedures, parameters, fence languages); `Providers/`: each skill provider and the composite (order, de-duplication, the role first); `Headers/`: a prompt's header and the catalogue that reads them once (CL27; the loader's side in `Hosting/Prompts/SkillLoaderHeadersTests`); `Roles/`: the role catalogue, minions' roles, and `RoleNamingTests` (nothing in core named "personality", CL13, CL14); the API has `ApiRoleNamingTests` |
| `RepeatedNames/` | The ratchets of ADR 0043: a wire name written as a string outside its enum (`RepeatedNameTests`, with `Scanning/`: a C# lexer, the scan, the compiled-code reader), and `.Wire()` as the one way out (`WireTextTests`: no direct `SettingChoice.Wire`, no case-folded `ToString()`, no `ToString()` of an enum in the compiled code). Five exemptions, each with its reason, narrowed rather than lengthened (F18 step 7.5) |
| `Usage/` | Scopes and their totals, the ledger file, reports, the usage text and sessions, `CacheGaps` |
| `Guardrails/` | The workspace boundary and secret files, the SSRF guard, and `HostAuth`, the key rule every host shares |
| `Delegation/` | Asking another agent: addresses and the `Calls` that allow them, `AskAgent`'s limits, follow-ups and peers, the did line, `ReadAgentWork`, minions, and the sub-session's stream, keys, bodies, usage and trace |
| `Sessions/` | A session's replay and its store, and the sub-sessions a chat keeps with their work |
| `Telemetry/` | The seam with no collector (`HostTelemetryTests`) and a turn's span (`TurnSpansTests`) |
| `Composition/` | What core uses from the composition: the process runner, the workspace listing, an agent's address, a turn's identity |
| `Categories/`, `Docs/`, `Projects/` | The ratchets on the tests' marks, on the docs (the work log, the roadmap, the write-ups, the size of what Claude loads) and on core's packages |
| `ArchitectureTests.cs` | The layout rules on core's source: namespace = folder, one type per file, dependencies one way |
| `FileSizeTests.cs` | A file holds at most 200 lines, a test file 400, over every project (*Separate by concern*): a ratchet whose `Oversized` list names the files S still has to split and can only shrink |
| `SkillsLayoutTests.cs` | Skill files follow the levels (CL8): every built-in prompt in a known folder of `prompts/` and embedded, no name at two levels, a guide per toolset, and the repo's `Skills/` holding only what the agent writes |
| `TestPrompts.cs` | The `[SetUpFixture]` that registers the tests' own prompts (`prompts/`: a product, a specialist, a minion) before the first is read; the shared fakes are in `src/tests/shared/louis-agent.testing` (below) |

## louis-agent.api.tests

`AgentSessionsTests` covers how a session gets its toolsets and tools: the `skills` default, an empty list, unknown
names, the depth and the `specialists` rule, and sessions that share an engine but not a history. It needs no web
host, no network and no model. The sessions are built on an `AgentEngineFactory` with a chat client that throws if
it's ever called.

Some API tests start a real child process where the eval runner would be. For those, `demos/puls/tests/louis-agent.api.standin`
stands in for the runner. It's a small console app, not a test project. The check's id picks what it does: print its
arguments, dump the names in its environment, stream lines, fail, sleep, or never read its stdin. It's built along
with these tests through a project reference that brings no assembly, just like the runner itself.

*Decided in:* F5, F18 step 7.4.

## louis-agent.orchestrator.tests

| File | Covers |
|---|---|
| `LouisAgentClientTests.cs` | Parsing the SSE stream (text, tool calls with inputs and results, stop reason, errors, separate text blocks) and `FIX-RESULT` lines |
| `OrchestratorTests.cs` | The error feed (processed ids survive restarts, unsafe ids ignored), decisions, runbook loading, the fix prompt, tool guards, the comms log, returning to `main`, and `VerifyFixCommit` against a throwaway git repository |

## louis-agent.mcp.tests

These run the MCP SDK's real server and client, joined by two in-memory pipes, so there's no port and no process.
`InMemoryMcp.cs` sets that up; it isn't a test itself.

`Resources/FeedResourcesTests.cs` checks that each feed is listed as `feed://{name}` with its description and
`application/json`, that it reads back as its JSON, and that an unknown URI gets the protocol's error.
`McpPrimitivesTests.cs` checks the one setup both transports use: the catalogue's feeds as resources, beside the
engine's tools.

*Decided in:* F56.

## The identities', the packs' and the composition root's tests

Each identity and each pack has its tests beside it.

Puls's identity tests prove its prompts are its own and are read through `SkillFileSource` (`PulsPromptsTests`, with
the Puls golden `PulsSessionSnapshot.json`). Its evals and their checks have tests too: `Evals/Routing/`, and
`Evals/Checks/`, with each check run on hand-cut answers, plus the cook's dataset, ids and conformance. The cook
scenarios' ids are test constants in `PulsCookScenarioIds`, because an id is data, not an enum. `PulsIdsTests` holds
each one to its dataset, and `RepeatedNameTests` lists that file and skips only that class. The judge's tests are in
`louis-agent.identity.puls.judges.tests`.

The where-to-buy pack's tests read Nominatim's answers from hand-written fixtures in its `jsonv2` shape, and keep the
shop rules' tests, with their golden (`WhereToBuyToolMessageSnapshot.json`). The shop-prices pack's tests check the
price lists from the shipped tables, with their golden (`ShopPriceToolMessageSnapshot.json`), and its store runs
against a real Mongo in its integration tests.

The recipes pack's tests read a `Recipe` from recorded pages of sites that allow us (`Recipes/Fixtures/`, trimmed
copies under 30 KB, never served), and from small inline blocks for the shapes no page shows.

The composition root's tests prove that `IDENTITIES` and `PACKS` pick what they should (`PluginsTests`), and that
every pack stays convertible to an MCP server (`PackConvertibilityTests`, through the MCP SDK's own
`McpServerTool.Create`, a test-only package). They also check that every built-in prompt of every identity and pack
opens with its header, and that its level is its folder's (`EveryPromptHasAHeaderTests`).

Each of these registers the prompts its identity or pack brings in a `[SetUpFixture]`, just as a host's `Build` does.

*Decided in:* CL15, F49.2, F50.2, F35.1, CL27, [ADR 0043](../adr/0043-repeated-names-are-enums.md).

## The web's tests

`louis-agent.web.tests` tests the framework's web. Its plain logic runs on NUnit, in folders that mirror the library:
`TurnStream` (the recorded turn, kept in `louis-agent.session-stream.tests/Fixtures/`, with a case per event it
folds), `Delegation` and `WorkStatus` (the states a card goes through), `UsageFormat` (priced, unpriced, the display
currency) and `Markdown` (a table, links, images, raw HTML). Its components run on bUnit, each rendered from the
recorded turn and held to the `.html` golden beside its test.

It also holds the web's rule that a `.razor` file holds markup only. `RazorHasNoCodeTests` scans every `.razor` under
`src/` for an `@code` block.

Puls's tests (`louis-agent.puls.tests`) cover only what's Puls's own: its pages, its layout, its words and its styles.
They pin `WorkPanel`, `PulsTile` and `TemplateCard`, and render `ChatPage` end to end against a stub API that opens a
session and streams the recorded turn. The empty page and the page with the turn are each a golden, and the JS calls
the page makes are listed. Its landing page is a golden too.

The code-review app's tests (`louis-agent.code-review.tests`) pin its chat page the same way, empty and with the turn.

`src/tests/web/louis-agent.web.testing` holds what they share: the fixture replay, the stub API (`FixtureApi`: fixed
JSON for what the apps read at start-up, one session, the recorded turn streamed, and the workspace and git reads
empty), and the snapshot helper. The eval pages, the account pages and the diagrams each have test projects of their
own in `src/tests/web/`.

*Decided in:* F18 step 7.2, F59.10.

## louis-agent.acp.tests

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

## The eval projects' tests

`louis-agent.evals.tests` tests the harness: the dataset loader (Inspect's Sample shape read strictly, the hash, an
expected key no grader reads), the common graders, the model grader over Microsoft.Extensions.AI.Evaluation, the
roll-up, the conformance cases, the label sheet and the calibration, and the protocol writer against its golden.

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

## What isn't covered yet

The orchestrator's whole loop, run against a scripted louis-agent.api, has no test yet. It's in the TODO's backlog,
under *Quality and CI*. CI (`.github/workflows/ci.yml`) runs every suite here on each pull request and on each merge
to main.

## Adding tests

The steps are in [Write and run tests](../guides/write-tests.md). In short: the test goes in the folder that mirrors
the class it tests, with the matching namespace, and it uses the fakes in `src/tests/shared/louis-agent.testing`. Every
feature doc in [src/docs/features/](../features/README.md) names the tests each step adds.

## Related docs

[Write and run tests](../guides/write-tests.md) · [Architecture § Testing](../architecture/08-crosscutting-concepts.md#testing)

---
[Projects](README.md) · Previous: [louis-agent.evals.runner](louis-agent.evals.runner.md)
