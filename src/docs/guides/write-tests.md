# Write and run tests

> **How-to.** Run the tests, write a test before the code, put it in the right place, use the shared fakes, regenerate
> a golden file, keep the rule tests green, and add a test project. **Elsewhere:** what each test project covers in the
> [tests design doc](../projects/tests.md); the rules Claude loads with a test file in `.claude/rules/tests.md`.

![Tests at a glance: unit tests run offline on the shared fakes, integration tests leave their process, golden files pin what the model reads and what a page draws, and rule tests hold the code's shape](diagrams/write-tests.svg)

Every test runs offline: no network, no model, no Docker. A fake chat client plays the model and a stub handler
answers HTTP, so a run costs nothing and needs no keys.

## Run them

```bash
dotnet build louis-agent-solution.sln
dotnet test louis-agent-solution.sln                                         # everything
dotnet test louis-agent-solution.sln --filter "Category!=Integration"        # the unit tests, as CI's first test step
dotnet test louis-agent-solution.sln --filter "Category=Integration"         # the integration tests, as its second
dotnet test src/tests/engine/LouisAgent.Core.Tests                                     # one project
dotnet test src/tests/engine/LouisAgent.Core.Tests --filter "FullyQualifiedName~AgentEngineTests"   # one class
```

GitHub Actions runs the same steps on Ubuntu, on every push to a pull request (`.github/workflows/ci.yml`). It
builds, checks the formatting of the C# files the pull request's own commits touched (`dotnet format
--verify-no-changes`), then runs the unit tests and the integration tests with the two filters above, the second with
`INTEGRATION_REQUIRED=1`. It brings a throwaway MongoDB and SQL Server, so the stores' contracts run against the real
thing. A push to a branch with no pull request runs nothing, and neither does a merge to main, because the pull
request was tested with main merged in. So a red check is the failure you'd see locally with that step's command. In
Rider, the unit-test window runs the same tests.

## Write the test first

![Test first: write the test, see it fail on an assertion, write the code until it passes, run the project's suite, then commit the test and the code together](diagrams/write-tests-flow.svg)

A change that alters behaviour starts with its test ([ADR 0014](../adr/0014-test-first.md)):

1. Write the test for what the change adds. A fix starts with a test that reproduces the bug.
2. Run just that test and see it fail **on an assertion**, not a compile error: give a new member a stub
   (`throw new NotImplementedException()`) so it compiles. Keep the failing output; the review asks for it.
3. Write the code until it passes, then run the project's suite and tidy with everything green.

Commit the test and the code together, so every commit passes. A docs-only or configuration-only change has no test to
write first.

## Where a test goes

A test's folder mirrors the source, and its namespace is the folder. So a test of
`src/engine/LouisAgent.Core.Web/Robots/RobotsPolicy.cs` is
`src/tests/engine/LouisAgent.Core.Web.Tests/Robots/RobotsPolicyTests.cs`, in `LouisAgent.Core.Web.Tests.Robots`. The
project mirrors the project, too. A pack's tests are in `packs/tests/LouisAgent.Packs.{Name}.Tests`, and an identity's
in `demos/{name}/tests/LouisAgent.Identities.{Name}.Tests`. No test project references another, and a test file holds
at most 400 lines, so split a long one by what it tests.

A test that leaves its process is an integration test. That's one that starts a real process (git, bash, an
interpreter, `dotnet`, the eval runner), opens Mongo, or needs a running stack. It lives in a sibling project named
`{name}.integration-tests`, such as `src/tests/engine/LouisAgent.Core.IntegrationTests`, the API's in
`src/tests/hosts/`, the orchestrator's in `demos/louis-agent/tests/`, and the packs' in `packs/tests/`. Its class is
`[Category("Integration")]`.

It asks for what it needs from outside in one of two ways. `Outside.Tool` asks for a program, and a missing one skips
the test on your machine but fails it on CI. `Outside.Stack` asks for a running stack's service, such as a browser or a
search engine, and always skips when its address isn't set. Whatever a unit project and an integration project share goes in a library beside
them, such as `LouisAgent.Api.Testing`, which holds the in-memory API host, the built runner dlls, and the store
contracts that run against both memory and Mongo. `IntegrationTestsAreMarkedTests` and `IntegrationPlacementTests`
hold all this, and in the solution the projects sit in the folders `unit`, `integration` and `shared`.

*Decided in:* F18 step 7.6.

## Use the shared fakes

`src/tests/shared/LouisAgent.Testing` is a class library every test project can reference:

| Fake | Use it for |
|---|---|
| `FakeChatClient` | The model: a script of responses, streamed or not, and the requests it received |
| `FailingStreamClient` | A model that fails mid-stream |
| `StubHandler` | HTTP: a fixed answer per request, so Paymo, DevOps, web and shop tests never call out |
| `ListUsageSink` | The usage records a request wrote |
| `FakePrompts`, `FakeIdentity`, `FakePack` | A prompt source, an identity and a pack built in the test |
| `FakeTools.cs` | Small tool classes for a pack, among them ones whose tool names clash with core's or the engine's |
| `FakeFeed`, `FakeImport`, `FakeScreen` | A pack's feed, import and screen, for the catalogue's and the API's tests |
| `FakeDns` | A DNS answer that gets past the SSRF guard without leaving the machine |
| `SampleEval`, `SampleIdentity`, `SampleMode`, `SampleRole`, `SampleScenario`, `SampleToolset` | Neutral names, for a test that stores, shows or parses a name and needs no real one |
| `TestTools` | A fake pack under the real toolset names, for tests of the engine |
| `TestPaths` | The repository root, `Agent()` (options on the repo with no network) and `Toolsets(…)` (a session's tool objects) |

The web's tests, the sites' and the apps' use `src/tests/web/LouisAgent.Web.Testing` as well: `FixtureApi` (a stub API that streams the recorded
Puls turn), `SessionStreamFixture`, `Snapshots` and `RecordingJSRuntime`. Components render with bUnit (MIT, 2.12.5), its JS
interop in loose mode where a component calls the page's scripts, so a call is answered with nothing and listed,
never a failure. A test that needs a clock uses Microsoft's `FakeTimeProvider`.

## Regenerate a golden file

Some tests compare what is produced with a file in the repo, so a change to what the model reads, or to a page's
markup, can't happen by accident:

| Golden | Test | What it pins |
|---|---|---|
| `Golden/ModelRequestSnapshot.json` (the coding catalog's tests) | `ModelRequestSnapshotTests` | The tools, their descriptions, the system prompt and the engine's fixed texts |
| `Golden/ToolMessageSnapshot.json` (the coding catalog's tests) | `ToolMessageSnapshotTests` | Every `*ToolMessage` enum's template, the shared one and each area's |
| `CalculatorToolMessageSnapshot.json` (calculator pack) | `CalculatorToolMessageSnapshotTests` | The calculator's texts |
| `DocsToolMessageSnapshot.json` (docs pack) | `DocsToolMessageSnapshotTests` | The docs tools' texts |
| `TriageToolMessageSnapshot.json` (triage pack) | `TriageToolMessageSnapshotTests` | The triage tools' texts |
| `FrameworkSessionSnapshot.json` (framework Orbi) | `FrameworkSessionSnapshotTests` | What the model is sent in a session of the framework's guide |
| `AcpTranscript.json` (ACP server) | `AcpTranscriptTests` | Everything the server writes to Rider |
| `ApiRoutesSnapshot.json` (API) | `ApiRoutesSnapshotTests` | Every route of the API, with who may call it |
| `EvalRoutesSnapshot.json` (eval studio's API) | `EvalRoutesSnapshotTests` | Every route with the studio mapped, with who may call it |
| `AccountsRoutesSnapshot.json` (accounts) | `AccountsRoutesSnapshotTests` | Every account route, with who may call it |
| `.html` beside each component test (web) | bUnit tests | A component's or page's markup |

The first two are in `demos/louis-agent/tests/LouisAgent.Coding.Catalog.Tests`, since what the model reads depends
on what a demo loads.

After a deliberate change, regenerate, then read the diff before committing: the diff is the review of the change.

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/louis-agent/tests/LouisAgent.Coding.Catalog.Tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests"
```

```powershell
$env:LOUIS_UPDATE_GOLDEN = '1'
try { dotnet test demos/louis-agent/tests/LouisAgent.Coding.Catalog.Tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests" }
finally { Remove-Item Env:LOUIS_UPDATE_GOLDEN }
```

The `finally` matters: left set, every later run rewrites the goldens instead of checking them. On Windows, a
regenerated file can show as modified in `git status` with no `git diff`: only its line endings changed, and
`git checkout -- <file>` puts it back.

**The web's components are pinned the same way.** `src/tests/web/LouisAgent.Web.Tests` renders `TurnView`,
`BlocksView`, `DelegationCard` and `CurrencySwitcher` with bUnit from the recorded Puls turn
(`src/tests/engine/LouisAgent.SessionStream.Tests/Fixtures/session-stream.sse`), and compares each one's markup with
the `.html` golden beside its test (`Components/TurnView.html`). `demos/louis-agent/tests/LouisAgent.CodeReview.Tests`
does the same for the code-review app's chat page. Each runs against the stub API, `FixtureApi`, from the shared web testing library.
The same switch regenerates them:

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/web/LouisAgent.Web.Tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/louis-agent/tests/LouisAgent.CodeReview.Tests
```

A mismatch writes what was rendered to `louis-agent-snapshots/` under the temp folder, so the diff can be read.

The snapshots leave out what isn't the component's doing: per-instance SVG ids, `@ref` element references, the
renderer's event-handler numbers, a thinking block's seconds, and the line breaks and indentation beside tags, so a
page split into components keeps its golden. A space between tags stays, since it can be seen. They run with the
invariant culture, so they never depend on the machine.

## Keep the rule tests green

A few tests check the code's shape, not its behaviour. When one fails, the change broke a rule; fix the change, not the
test.

| Test | Holds |
|---|---|
| `ArchitectureTests` | Namespace = folder, core's layers used one way, no command line built as one string, core names no product or pack, no pack references another, no test project leans on core's tests |
| `FileSizeTests` | At most 200 lines a file (400 a test file); its `Oversized` list can only shrink ([ADR 0034](../adr/0034-code-a-person-can-maintain.md)) |
| `RazorHasNoCodeTests` | No `@code` block in a `.razor`; its `PendingCode` list can only shrink |
| `SkillsLayoutTests` | Every built-in prompt in its level's folder, embedded, a guide per toolset |
| `HostMessageEnumTests` | Every host, pack and identity message has a template with well-formed holes |
| `MessageEnumTests` | Every core message has a template; a record named by `[Holes]` matches it, and in an enum on its done list every member with holes names one ([ADR 0119](../adr/0119-declare-name-then-pass.md)) |
| `OldWordsTests` | No file outside the kept places says "specialist" or "minion", and no `[Description]` does ([ADR 0133](../adr/0133-subagents-live-inside-their-agent.md)) |
| `PackConvertibilityTests` | Every pack's tools fit MCP's shapes ([ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)) |

The two ratchet lists (`Oversized`, `PendingCode`) take a file off when you split it. Never add one to get green.

## Keep tests platform-neutral

The agent runs the tests on Linux in its containers, and CI runs on Ubuntu, while you may be on Windows. Build paths
with `Path.Combine` or forward slashes, never a drive letter or a backslash, and compare text with the line endings
normalised. A test that needs a tool the machine may lack (git, Python, the Mongo store) skips itself with
`Assume.That` or `Assert.Ignore` and says why, rather than failing.

## Add a test project

Copy an existing one's `.csproj` (`packs/tests/LouisAgent.Packs.Calculator.Tests` is a small one): `net10.0`, NUnit 4.3.2 with its
analyzers and adapter, `Microsoft.NET.Test.Sdk`, `IsPackable` false, a reference to the project under test and to
`LouisAgent.Testing`, and both as global usings:

```xml
<ItemGroup>
  <Using Include="NUnit.Framework" />
  <Using Include="LouisAgent.Testing" />
</ItemGroup>
```

Set `RootNamespace` to the tested project's namespace plus `.Tests`, add the project to `louis-agent-solution.sln`, and
CI runs it with no change to the workflow. If the tested code needs its prompts registered, do it once in a
`[SetUpFixture]`, as the identity and pack tests do.

## Before you ask for a review

- [ ] The test was seen failing before the code, and the output is in the explanation.
- [ ] It runs offline, on Linux and Windows, and lives in the folder that mirrors the source.
- [ ] Every golden diff is the change you meant; no rule test was loosened.

---
[Guides](README.md) · [Docs index](../README.md)
