# Test projects — design

> **Kind:** NUnit test projects · **Paths:** `tests/louis-agent.core.tests/`, `tests/louis-agent.orchestrator.tests/` ·
> **Run:** `dotnet test tests/louis-agent.core.tests` and `dotnet test tests/louis-agent.orchestrator.tests`

## Purpose

Fast, offline checks of the behaviour that matters: tools, skills, the engine's loop and guards, configuration, and the
orchestrator's parsing and verification. **No network, model or Docker** — a fake chat client plays the model.

## louis-agent.core.tests

The folders mirror `louis-agent.core`'s, and each test's namespace is its folder (`LouisAgent.Core.Tests.Tools.Web`).

| Folder | Covers |
|---|---|
| `Agent/` | `AgentEngine`: tool loop, streaming, thinking, summarising, cut-off guards, round limit, `toolsets:`, sorted and unique tool names, discovered tools, skill authoring, its usage records |
| `Config/` | `LlmOptions` (provider inference, tool support, thinking, caching), `AgentOptions` (env binding, repository root), `EnvFile` |
| `Hosting/` | `AgentHost`: environment loading, price and rate loading from a URL with the file fallback; Rider MCP discovery |
| `Llm/`, `Llm/AnthropicProvider/` | The factory's caching strategy, `PromptCachingChatClient`, the usage mappers and recorder; Anthropic's wire format and cache-write lifetimes on captured bodies |
| `Logging/` | Serilog behind `ILogger`: stderr only, holes bound by name, tags, the run file and the JSON-lines records |
| `Messages/` | Every message enum member, core's and each host's (from source), has a template with well-formed holes; `ToolMessageSnapshot.json` pins every text tools return to the model |
| `Pricing/` | The price table and the shipped `config/prices.json`, costs, the pricing sink, exchange rates, the display currency |
| `Protocols/Mcp/` | The Rider MCP client with mocked HTTP |
| `Skills/` | `MarkdownSkillLoader` (procedures, parameters, fence languages), each skill provider and the composite (order, de-duplication, personality first) |
| `Tools/{Toolset}/` | Every toolset: workspace boundary and secret files, git, dotnet, Python, PowerShell, bash, web search and fetch, Paymo, DevOps, agent-built tools |
| `Usage/` | Scopes and their totals, the ledger file, reports, the usage text and sessions, `CacheGaps` |
| `ArchitectureTests.cs` | The layout rules on core's source: namespace = folder, one type per file, dependencies one way |
| `TestHelpers.cs` | `TestPaths` (repository root, options), `FakeChatClient` (a scripted `IChatClient` that also streams), `StubHandler`, `ListUsageSink`, `FailingStreamClient` |

- HTTP is stubbed with handlers, so Paymo, DevOps and web tests never call out.
- A few tests skip themselves when an external tool (git, pip, a live network) isn't available.
- The agent runs these tests on Linux in its containers: keep them platform-neutral (forward slashes, no Windows-only
  paths).

## louis-agent.orchestrator.tests

| File | Covers |
|---|---|
| `LouisAgentClientTests.cs` | Parsing the SSE stream (text, tool calls with inputs and results, stop reason, errors, separate text blocks) and `FIX-RESULT` lines |
| `OrchestratorTests.cs` | The error feed (processed ids survive restarts, unsafe ids ignored), decisions, runbook loading, the fix prompt, tool guards, the comms log, returning to `main`, and `VerifyFixCommit` against a throwaway git repository |

`InternalsVisibleTo` gives the test projects access to `internal` types in core and the orchestrator.

## What isn't covered yet

- **The API, the web app, the ACP and MCP servers** have no automated tests; they're checked by hand (curl, browser,
  Rider). The orchestrator demo exercises the API and git endpoints end to end against a real model.
- **The orchestrator's whole loop** against a scripted louis-agent.api.

Both are in the TODO backlog (*Quality and CI*), along with CI to run these suites on every push.

## Adding tests

- Core tool: `tests/louis-agent.core.tests/Tools/{Toolset}/<Class>Tests.cs`, using `TestPaths.Agent()` and, for engine behaviour,
  `FakeChatClient` with a script of responses.
- Anything else: the folder of the class it tests, with the matching namespace.
- Every feature doc in [docs/features/](../features/README.md) names the tests each step adds.

## Related docs

[Development guide § Tests](../CLAUDE.md#tests) · [Architecture § Testing](../ARCHITECTURE.md#testing)

---
[Projects](README.md) · Previous: [louis-agent.usage-probe](louis-agent.usage-probe.md) · Next: [order-service](order-service.md)
