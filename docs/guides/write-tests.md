# Write and run tests

> **How-to.** Run the tests, write a test before the code, put it in the right place, use the shared fakes, regenerate
> a golden file, keep the rule tests green, and add a test project. **Elsewhere:** what each test project covers in the
> [tests design doc](../projects/tests.md); the same rules in the agent's words in [tests/CLAUDE.md](../../tests/CLAUDE.md).

Every test runs offline: no network, no model, no Docker. A fake chat client plays the model and a stub handler
answers HTTP, so a run costs nothing and needs no keys.

## Run them

```bash
dotnet build louis-agent-solution.sln
dotnet test louis-agent-solution.sln                                         # everything
dotnet test louis-agent-solution.sln --filter "Category!=Integration"        # the unit tests, as CI's first test step
dotnet test louis-agent-solution.sln --filter "Category=Integration"         # the integration tests, as its second
dotnet test tests/louis-agent.core.tests                                     # one project
dotnet test tests/louis-agent.core.tests --filter "FullyQualifiedName~AgentEngineTests"   # one class
```

GitHub Actions runs the build and the whole solution's tests on Ubuntu on every push and pull request
(`.github/workflows/ci.yml`), so a red check is the failure you'd see locally with the same command. In Rider, the
unit-test window runs the same tests.

## Write the test first

A change that alters behaviour starts with its test ([ADR 0014](../adr/0014-test-first.md)):

1. Write the test for what the change adds. A fix starts with a test that reproduces the bug.
2. Run just that test and see it fail **on an assertion**, not a compile error: give a new member a stub
   (`throw new NotImplementedException()`) so it compiles. Keep the failing output; the review asks for it.
3. Write the code until it passes, then run the project's suite and tidy with everything green.

Commit the test and the code together, so every commit passes. A docs-only or configuration-only change has no test to
write first.

## Where a test goes

- **The folder mirrors the source**, and the namespace is the folder: a test of `louis-agent.core/Tools/Git/GitTools.cs`
  is `tests/louis-agent.core.tests/Tools/Git/GitToolsTests.cs`, in `LouisAgent.Core.Tests.Tools.Git`.
- **A test that leaves its process is an integration test** (F18 step 7.6): it starts a real process (git, bash, an interpreter, `dotnet`, the eval runner), opens Mongo or needs the Puls stack. It lives in a sibling project, `{name}.integration-tests` (`tests/louis-agent.core.integration-tests`, `.api.`, `.orchestrator.`, `.pack.shops.`), its class is `[Category("Integration")]`, and it asks for the tool through `Outside.Tool` (a missing tool skips the test on your machine and fails it on CI) or `Outside.Stack` (the Puls stack's browser or SearXNG: always skips when its address is not set). What a unit and an integration project share goes in a library beside them (`louis-agent.api.testing`: the in-memory API host, the built runner dlls, the store contracts that run against memory and Mongo). `IntegrationTestsAreMarkedTests` and `IntegrationPlacementTests` hold it. In the solution they sit in the folders `unit`, `integration` and `shared`.
- **The project mirrors the project:** a pack's tests in `tests/louis-agent.pack.{name}.tests`, an identity's in
  `tests/louis-agent.identity.{name}.tests`. No test project references another.
- A test file holds at most 400 lines; split a long one by what it tests.

## Use the shared fakes

`tests/louis-agent.testing` is a class library every test project can reference:

| Fake | Use it for |
|---|---|
| `FakeChatClient` | The model: a script of responses, streamed or not, and the requests it received |
| `FailingStreamClient` | A model that fails mid-stream |
| `StubHandler` | HTTP: a fixed answer per request, so Paymo, DevOps, web and shop tests never call out |
| `ListUsageSink` | The usage records a request wrote |
| `FakePrompts`, `FakeIdentity`, `FakePack`, `FakeTools` | A prompt source, an identity, a pack and its tool classes built in the test |
| `TestPaths` | The repository root, `Agent()` (options on the repo with no network) and `Toolsets(…)` (a session's tool objects) |

The web apps' tests use `tests/louis-agent.web.testing` as well: `FixtureApi` (a stub API that streams the recorded
Puls turn), `SessionStreamFixture`, `Snapshots`, `FakeClock` and `RecordingJSRuntime`. Components render with bUnit.

## Regenerate a golden file

Some tests compare what is produced with a file in the repo, so a change to what the model reads, or to a page's
markup, can't happen by accident:

| Golden | Test | What it pins |
|---|---|---|
| `Agent/ModelRequestSnapshot.json` (core) | `ModelRequestSnapshotTests` | The tools, their descriptions, the system prompt |
| `Messages/ToolMessageSnapshot.json` (core) | `ToolMessageSnapshotTests` | Every text a tool returns |
| `ShopToolMessageSnapshot.json` (shops pack) | `ShopToolMessageSnapshotTests` | The shop tools' texts |
| `PulsSessionSnapshot.json` (Puls identity) | `PulsSessionSnapshotTests` | What the model is sent in Puls's chat and its cook |
| `AcpTranscript.json` (ACP server) | `AcpTranscriptTests` | Everything the server writes to Rider |
| `.html` beside each component test (web) | bUnit tests | A component's or page's markup |

After a deliberate change, regenerate, then read the diff before committing: the diff is the review of the change.

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.core.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests"
```

```powershell
$env:LOUIS_UPDATE_GOLDEN = '1'
try { dotnet test tests/louis-agent.core.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests" }
finally { Remove-Item Env:LOUIS_UPDATE_GOLDEN }
```

The `finally` matters: left set, every later run rewrites the goldens instead of checking them. On Windows, a
regenerated file can show as modified in `git status` with no `git diff`: only its line endings changed, and
`git checkout -- <file>` puts it back. A web golden that fails writes what was rendered to `louis-agent-snapshots/`
under the temp folder, so you can compare the two.

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

Copy an existing one's `.csproj` (`tests/louis-agent.pack.shops.tests` is a small one): `net10.0`, NUnit 4.3.2 with its
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
