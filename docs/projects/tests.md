# Test projects — design

> **Kind:** NUnit test projects · **Paths:** `tests/louis-agent.core.tests/`, `tests/louis-agent.api.tests/`,
> `tests/louis-agent.orchestrator.tests/`, `tests/louis-agent.identity.puls.tests/`, `tests/louis-agent.pack.shops.tests/`,
> `tests/louis-agent.composition.tests/`, `tests/louis-agent.web.shared.tests/`, `tests/louis-agent.puls.tests/`,
> `tests/louis-agent.web.tests/`; the fakes they share in `tests/louis-agent.testing/` and, for the web apps,
> `tests/louis-agent.web.testing/` (class libraries, CL15) ·
> **Run:** `dotnet test louis-agent-solution.sln` (or each project)

## Purpose

Fast, offline checks of the behaviour that matters: tools, skills, the engine's loop and guards, configuration, and the
orchestrator's parsing and verification. **No network, model or Docker** — a fake chat client plays the model.

## louis-agent.core.tests

The folders mirror `louis-agent.core`'s, and each test's namespace is its folder (`LouisAgent.Core.Tests.Tools.Web`).

| Folder | Covers |
|---|---|
| `Agent/` | `AgentEngine`: tool loop, streaming, thinking, summarising, cut-off guards, round limit, `toolsets:`, sorted and unique tool names, discovered tools, skill authoring, its usage records |
| `Config/` | `LlmOptions` (provider inference, tool support, thinking, caching), `AgentOptions` (env binding, repository root), `EnvFile` |
| `Hosting/` | `AgentHost`: environment loading, price and rate loading from a URL with the file fallback, the system prompt per toolset profile; `AgentEngineFactory` (one engine per profile, the cap); Rider MCP discovery |
| `Llm/`, `Llm/Providers/AnthropicProvider/` | The factory's caching strategy, `PromptCachingChatClient`, the usage mappers and recorder; Anthropic's wire format and cache-write lifetimes on captured bodies |
| `Logging/` | Serilog behind `ILogger`: stderr only, holes bound by name, tags, the run file and the JSON-lines records |
| `Messages/` | Every message enum member, core's and each host's (from source), has a template with well-formed holes; `ToolMessageSnapshot.json` pins every text tools return to the model |
| `Pricing/` | The price table and the shipped `config/prices.json`, costs, the pricing sink, exchange rates, the display currency |
| `Protocols/Mcp/` | The Rider MCP client with mocked HTTP |
| `Skills/` | `MarkdownSkillLoader` (procedures, parameters, fence languages); `Providers/`: each skill provider and the composite (order, de-duplication, the role first); `Roles/`: the role catalogue, minions' roles, a role's header, and `RoleNamingTests` (nothing in core named "personality", CL13, CL14); the API has `ApiRoleNamingTests` |
| `Tools/{Toolset}/` | Every toolset: workspace boundary and secret files, git, dotnet, Python, PowerShell, bash, web search and fetch, Paymo, DevOps, agent-built tools |
| `Usage/` | Scopes and their totals, the ledger file, reports, the usage text and sessions, `CacheGaps` |
| `ArchitectureTests.cs` | The layout rules on core's source: namespace = folder, one type per file, dependencies one way |
| `FileSizeTests.cs` | A file holds at most 200 lines, a test file 400, over every project (*Separate by concern*): a ratchet whose `Oversized` list names the files S still has to split and can only shrink |
| `SkillsLayoutTests.cs` | Skill files follow the levels (CL8): every built-in prompt in a known folder of `Prompts/` and embedded, no name at two levels, a guide per toolset, and the repo's `Skills/` holding only what the agent writes |
| `TestPrompts.cs` | The `[SetUpFixture]` that registers the tests' own prompts (`Prompts/`: a product, a specialist, a minion) before the first is read; the shared fakes are in `tests/louis-agent.testing` (below) |

- HTTP is stubbed with handlers, so Paymo, DevOps and web tests never call out.
- A few tests skip themselves when an external tool (git, pip, a live network) isn't available.
- The agent runs these tests on Linux in its containers: keep them platform-neutral (forward slashes, no Windows-only
  paths).

## louis-agent.api.tests

`AgentSessionsTests` (F5): a session's toolsets and tools, the `skills` default, an empty list, unknown names, depth and
the `specialists` rule, sessions sharing an engine but not a history. No web host, network or model: the sessions are built on
an `AgentEngineFactory` with a client that throws if called.

## louis-agent.orchestrator.tests

| File | Covers |
|---|---|
| `LouisAgentClientTests.cs` | Parsing the SSE stream (text, tool calls with inputs and results, stop reason, errors, separate text blocks) and `FIX-RESULT` lines |
| `OrchestratorTests.cs` | The error feed (processed ids survive restarts, unsafe ids ignored), decisions, runbook loading, the fix prompt, tool guards, the comms log, returning to `main`, and `VerifyFixCommit` against a throwaway git repository |

`InternalsVisibleTo` gives the test projects access to `internal` types in core and the orchestrator.

## louis-agent.identity.puls.tests, louis-agent.pack.shops.tests, louis-agent.composition.tests

CL15 cut the engine into identities and packs, and each got its tests beside it: the identity's prove its prompts are
its own and read through `SkillFileSource` (`PulsPromptsTests`, the Puls golden `PulsSessionSnapshot.json`); the pack's
are the shop tests that left core, with their fixtures and their own golden (`ShopToolMessageSnapshot.json`); the
composition root's prove `IDENTITIES` and `PACKS` pick (`PluginsTests`) and that every pack stays convertible to an
MCP server (`PackConvertibilityTests`, through the MCP SDK's own `McpServerTool.Create`, a test-only package). Each
registers the prompts its identity or pack brings in a `[SetUpFixture]`, as a host's `Build` does.

`tests/louis-agent.testing` is a class library, not a test project: the fakes and paths every test project shares
(`FakeChatClient`, `FailingStreamClient`, `StubHandler`, `ListUsageSink`, `FakePrompts`, `FakeIdentity`, `FakePack`,
`TestPaths`), a global using in each test project that uses them (all but the API's and the orchestrator's). No test project references another test project, and `core.tests` opens its
internals to none; `ArchitectureTests` holds both.

## louis-agent.web.shared.tests, louis-agent.puls.tests, louis-agent.web.tests

The web apps' first tests (T1, CL15: the proof the web sort changes nothing, written before it). The shared library's
logic with plain NUnit, in folders that mirror it: `TurnStream` (the recorded turn, which lives in `louis-agent.session-stream.tests/Fixtures/` since F18 step 7.2,
moved here from the API's tests, and a case per event it folds), `Delegation` and `WorkStatus` (the states a card goes
through), `UsageFormat` (priced, unpriced, the display currency) and `Markdown` (a table, links, images, raw HTML). Its
components with bUnit, each rendered from the recorded turn and pinned to the `.html` golden beside its test. Puls's
tests pin `WorkPanel`, `PulsTile` and `TemplateCard`, and render `ChatPage` end to end against a stub API that opens a
session and streams the recorded turn: the empty page and the page with the turn, each a golden, and the JS calls the
page makes, listed; its landing page is a golden too. The code-review app's tests (T1b) pin its chat page the same way,
empty and with the turn, before W2 splits it; its components get tests when Phase 4 adds the Branches queue.
`tests/louis-agent.web.testing` holds what the three share: the fixture replay, the stub API (`FixtureApi`: fixed JSON
for what the apps read at start-up, one session, the recorded turn streamed, the workspace and git reads empty) and the
snapshot helper. The shared library's tests also hold the web layer's rule: `RazorHasNoCodeTests` scans every `.razor`
under `src/` for an `@code` block (*Separate by concern*), a ratchet whose `PendingCode` list names the files W2b still
has to move and can only shrink; when it is empty the list goes and the test is a plain rule.

## louis-agent.acp-server.tests

The ACP server's first tests (S, CL15). `AcpTranscriptTests` plays a recorded Rider conversation through `AcpServer`
(a bad line, an unknown method, `initialize`, `session/new`, one prompt with a file attached and one open in the IDE, which
the scripted model answers with a thought, a tool call and a reply) through `ScriptedConsole`, Rider's side of the pipe,
and pins everything the server wrote to stdout, and the prompt the model received, to `AcpTranscript.json` (session and
message ids numbered, the temp folder named; `LOUIS_UPDATE_GOLDEN=1` regenerates it). It is the proof the split of
`AcpServer` by responsibility changed nothing the client sees. The pure parts have their own tests, by folder as the
source: `ToolCards` (a card's kind, the preview cut), `PromptBuilder` (text, an embedded resource, an attached file read
and capped, the open file left out, an unmounted one reported), `ContainerPaths` (a mapping, a root searched by the last
path parts, the drive letter), `GitRoots` and `JsonRpcWriter`.

## What isn't covered yet

- **The API's endpoints and the MCP server** have no automated tests (only the API's sessions do); they're checked by
  hand (curl, Rider). The ACP server has its transcript (`tests/louis-agent.acp-server.tests`) and no more yet. Nothing has exercised the
  API and git endpoints end to end against a real model since CL10 removed the orchestrator demo.
- **The orchestrator's whole loop** against a scripted louis-agent.api.

Both are in the TODO backlog (*Quality and CI*). CI (`.github/workflows/ci.yml`) runs every suite here on each push
and pull request.

## Adding tests

The steps are in [Write and run tests](../guides/write-tests.md). In short: the test goes in the folder that mirrors
the class it tests, with the matching namespace; it uses the fakes in `tests/louis-agent.testing`; and every feature
doc in [docs/features/](../features/README.md) names the tests each step adds.

## Related docs

[Tests guide](../../tests/CLAUDE.md) · [Architecture § Testing](../architecture/08-crosscutting-concepts.md#testing)

---
[Projects](README.md) · Previous: [louis-agent.usage-probe](louis-agent.usage-probe.md) 
