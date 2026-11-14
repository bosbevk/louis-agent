# Write and run tests

> **How-to.** Run the tests, write a test before the code, put it in the right place, use the shared fakes, regenerate
> a golden file, keep the rule tests green, and add a test project. **Elsewhere:** what each test project covers in the
> [tests design doc](../projects/tests.md); the rules Claude loads with a test file in `.claude/rules/tests.md`.

Every test runs offline: no network, no model, no Docker. A fake chat client plays the model and a stub handler
answers HTTP, so a run costs nothing and needs no keys.

## Run them

```bash
dotnet build louis-agent-solution.sln
dotnet test louis-agent-solution.sln                                         # everything
dotnet test louis-agent-solution.sln --filter "Category!=Integration"        # the unit tests, as CI's first test step
dotnet test louis-agent-solution.sln --filter "Category=Integration"         # the integration tests, as its second
dotnet test src/tests/engine/louis-agent.core.tests                                     # one project
dotnet test src/tests/engine/louis-agent.core.tests --filter "FullyQualifiedName~AgentEngineTests"   # one class
```

GitHub Actions runs the build, then the unit tests, then the integration tests (the two filters above, the second
with `INTEGRATION_REQUIRED=1`) on Ubuntu, on every push to a pull request (`.github/workflows/ci.yml`). A merge to
main runs no CI: the pull request was tested with main merged in. So a red check is the failure you'd see locally
with that step's command. In Rider, the unit-test window runs the same tests.

## Write the test first

A change that alters behaviour starts with its test ([ADR 0014](../adr/0014-test-first.md)):

1. Write the test for what the change adds. A fix starts with a test that reproduces the bug.
2. Run just that test and see it fail **on an assertion**, not a compile error: give a new member a stub
   (`throw new NotImplementedException()`) so it compiles. Keep the failing output; the review asks for it.
3. Write the code until it passes, then run the project's suite and tidy with everything green.

Commit the test and the code together, so every commit passes. A docs-only or configuration-only change has no test to
write first.

## Where a test goes

- **The folder mirrors the source**, and the namespace is the folder: a test of `src/engine/louis-agent.core.web/Robots/RobotsPolicy.cs`
  is `src/tests/engine/louis-agent.core.web.tests/Robots/RobotsPolicyTests.cs`, in `LouisAgent.Core.Web.Tests.Robots`.
- **A test that leaves its process is an integration test** (F18 step 7.6): it starts a real process (git, bash, an interpreter, `dotnet`, the eval runner), opens Mongo or needs the Puls stack. It lives in a sibling project, `{name}.integration-tests` (`src/tests/engine/louis-agent.core.integration-tests`, `.api.`, `.orchestrator.`, `.pack.shop-prices.`), its class is `[Category("Integration")]`, and it asks for the tool through `Outside.Tool` (a missing tool skips the test on your machine and fails it on CI) or `Outside.Stack` (the Puls stack's browser or SearXNG: always skips when its address is not set). What a unit and an integration project share goes in a library beside them (`louis-agent.api.testing`: the in-memory API host, the built runner dlls, the store contracts that run against memory and Mongo). `IntegrationTestsAreMarkedTests` and `IntegrationPlacementTests` hold it. In the solution they sit in the folders `unit`, `integration` and `shared`.
- **The project mirrors the project:** a pack's tests in `packs/tests/louis-agent.pack.{name}.tests`, an identity's in
  `demos/{name}/tests/louis-agent.identity.{name}.tests`. No test project references another.
- A test file holds at most 400 lines; split a long one by what it tests.

## Use the shared fakes

`src/tests/shared/louis-agent.testing` is a class library every test project can reference:

| Fake | Use it for |
|---|---|
| `FakeChatClient` | The model: a script of responses, streamed or not, and the requests it received |
| `FailingStreamClient` | A model that fails mid-stream |
| `StubHandler` | HTTP: a fixed answer per request, so Paymo, DevOps, web and shop tests never call out |
| `ListUsageSink` | The usage records a request wrote |
| `FakePrompts`, `FakeIdentity`, `FakePack`, `FakeTools` | A prompt source, an identity, a pack and its tool classes built in the test |
| `TestTools` | A fake pack under the real toolset names, for tests of the engine |
| `TestPaths` | The repository root, `Agent()` (options on the repo with no network) and `Toolsets(…)` (a session's tool objects) |

The web apps' tests use `src/tests/web/louis-agent.web.testing` as well: `FixtureApi` (a stub API that streams the recorded
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
| `WhereToBuyToolMessageSnapshot.json` (where-to-buy pack) | `WhereToBuyToolMessageSnapshotTests` | The branch tool's texts |
| `ShopPriceToolMessageSnapshot.json` (shop-prices pack) | `ShopPriceToolMessageSnapshotTests` | The price tool's texts |
| `DocsToolMessageSnapshot.json` (docs pack) | `DocsToolMessageSnapshotTests` | The docs tools' texts |
| `RecipeToolMessageSnapshot.json` (recipes pack) | `RecipeToolMessageSnapshotTests` | The recipe tools' texts |
| `TriageToolMessageSnapshot.json` (triage pack) | `TriageToolMessageSnapshotTests` | The triage tools' texts |
| `PulsSessionSnapshot.json` (Puls identity) | `PulsSessionSnapshotTests` | What the model is sent in Puls's chat and its cook |
| `AcpTranscript.json` (ACP server) | `AcpTranscriptTests` | Everything the server writes to Rider |
| `.html` beside each component test (web) | bUnit tests | A component's or page's markup |

The first two are in `demos/louis-agent/tests/louis-agent.coding-catalog.tests`, since what the model reads depends
on what a demo loads.

After a deliberate change, regenerate, then read the diff before committing: the diff is the review of the change.

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/louis-agent/tests/louis-agent.coding-catalog.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests"
```

```powershell
$env:LOUIS_UPDATE_GOLDEN = '1'
try { dotnet test demos/louis-agent/tests/louis-agent.coding-catalog.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests" }
finally { Remove-Item Env:LOUIS_UPDATE_GOLDEN }
```

The `finally` matters: left set, every later run rewrites the goldens instead of checking them. On Windows, a
regenerated file can show as modified in `git status` with no `git diff`: only its line endings changed, and
`git checkout -- <file>` puts it back.

**The web apps' components are pinned the same way** (T1, CL15): `src/tests/web/louis-agent.web.tests` renders
`TurnView`, `BlocksView`, `DelegationCard` and `CurrencySwitcher` with bUnit from the recorded Puls turn
(`src/tests/engine/louis-agent.session-stream.tests/Fixtures/session-stream.sse`) and compares each one's markup with the `.html` golden beside its test
(`Components/TurnView.html`); `demos/puls/tests/louis-agent.puls.tests` does the same for `WorkPanel`, `PulsTile`, `TemplateCard`,
the landing page and the chat page, empty and with the turn (`Pages/ChatPage.empty.html`, `ChatPage.turn.html`), and
`demos/louis-agent/tests/louis-agent.code-review.tests` for the code-review app's chat page, each against the stub API (`FixtureApi` in
`src/tests/web/louis-agent.web.testing`, the library the three share with the fixture replay and the snapshot helper). The
same switch regenerates them:

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/web/louis-agent.web.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/puls/tests/louis-agent.puls.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/louis-agent/tests/louis-agent.code-review.tests
```

A mismatch writes what was rendered to `louis-agent-snapshots/` under the temp folder, so the diff can be read. The
snapshots normalise what isn't the component's doing (per-instance SVG ids, `@ref` element references, the renderer's
event-handler numbers, a thinking block's seconds, and the line breaks and indentation beside tags, so a page split into
components keeps its golden; a space between tags stays, since it can be seen) and run with the invariant culture, so they never depend on the machine.

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
| `PackConvertibilityTests` | Every pack's tools fit MCP's shapes ([ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md)) |

The two ratchet lists (`Oversized`, `PendingCode`) take a file off when you split it. Never add one to get green.

## Keep tests platform-neutral

The agent runs the tests on Linux in its containers, and CI runs on Ubuntu, while you may be on Windows. Build paths
with `Path.Combine` or forward slashes, never a drive letter or a backslash, and compare text with the line endings
normalised. A test that needs a tool the machine may lack (git, Python, the Mongo store) skips itself with
`Assume.That` or `Assert.Ignore` and says why, rather than failing.

## Add a test project

Copy an existing one's `.csproj` (`packs/tests/louis-agent.pack.shop-prices.tests` is a small one): `net10.0`, NUnit 4.3.2 with its
analyzers and adapter, `Microsoft.NET.Test.Sdk`, `IsPackable` false, a reference to the project under test and to
`louis-agent.testing`, and both as global usings:

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
