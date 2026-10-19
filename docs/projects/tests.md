# Test projects — design

> **Kind:** NUnit test projects · **Paths:** `tests/louis-agent.core.tests/`, `tests/louis-agent.api.tests/`,
> `tests/louis-agent.orchestrator.tests/`, `tests/louis-agent.identity.puls.tests/`, `tests/louis-agent.pack.shops.tests/`,
> `tests/louis-agent.composition.tests/`; the fakes they share in `tests/louis-agent.testing/` (a class library, CL15) ·
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
| `Llm/`, `Llm/AnthropicProvider/` | The factory's caching strategy, `PromptCachingChatClient`, the usage mappers and recorder; Anthropic's wire format and cache-write lifetimes on captured bodies |
| `Logging/` | Serilog behind `ILogger`: stderr only, holes bound by name, tags, the run file and the JSON-lines records |
| `Messages/` | Every message enum member, core's and each host's (from source), has a template with well-formed holes; `ToolMessageSnapshot.json` pins every text tools return to the model |
| `Pricing/` | The price table and the shipped `config/prices.json`, costs, the pricing sink, exchange rates, the display currency |
| `Protocols/Mcp/` | The Rider MCP client with mocked HTTP |
| `Skills/` | `MarkdownSkillLoader` (procedures, parameters, fence languages); `Providers/`: each skill provider and the composite (order, de-duplication, the role first); `Roles/`: the role catalogue, minions' roles, a role's header, and `RoleNamingTests` (nothing in core named "personality", CL13, CL14); the API has `ApiRoleNamingTests` |
| `Tools/{Toolset}/` | Every toolset: workspace boundary and secret files, git, dotnet, Python, PowerShell, bash, web search and fetch, Paymo, DevOps, agent-built tools |
| `Usage/` | Scopes and their totals, the ledger file, reports, the usage text and sessions, `CacheGaps` |
| `ArchitectureTests.cs` | The layout rules on core's source: namespace = folder, one type per file, dependencies one way |
| `SkillsLayoutTests.cs` | Skill files follow the levels (CL8): every built-in prompt in a known folder of `Prompts/` and embedded, no name at two levels, a guide per toolset, and the repo's `Skills/` holding only what the agent writes |
| `TestHelpers.cs` | `TestPaths` (repository root, options), `FakeChatClient` (a scripted `IChatClient` that also streams), `StubHandler`, `ListUsageSink`, `FailingStreamClient` |

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
`TestPaths`), a global using in the four that use them. No test project references another test project, and `core.tests` opens its
internals to none; `ArchitectureTests` holds both.

## What isn't covered yet

- **The API's endpoints, the web app, the ACP and MCP servers** have no automated tests (only the API's sessions do);
  they're checked by hand (curl, browser, Rider). The orchestrator demo exercises the API and git endpoints end to end
  against a real model.
- **The orchestrator's whole loop** against a scripted louis-agent.api.

Both are in the TODO backlog (*Quality and CI*), along with CI to run these suites on every push.

## Adding tests

- Core tool: `tests/louis-agent.core.tests/Tools/{Toolset}/<Class>Tests.cs`, using `TestPaths.Agent()` and, for engine behaviour,
  `FakeChatClient` with a script of responses.
- Anything else: the folder of the class it tests, with the matching namespace.
- Every feature doc in [docs/features/](../features/README.md) names the tests each step adds.

## Related docs

[Tests guide](../../tests/CLAUDE.md) · [Architecture § Testing](../architecture/08-crosscutting-concepts.md#testing)

---
[Projects](README.md) · Previous: [louis-agent.usage-probe](louis-agent.usage-probe.md) 
