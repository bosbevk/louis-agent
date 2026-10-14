# Tests: development guide

> **How-to.** Running the tests, the golden files that pin what the model reads, and how tests are written here.
> **Elsewhere:** what each test project covers in the [tests design doc](../docs/projects/tests.md), the steps for a
> change in the [development guide](../docs/CLAUDE.md). Claude Code loads this file when it works on files in this
> folder.

## Running the tests

```bash
dotnet test tests/louis-agent.core.tests
dotnet test tests/louis-agent.core.tests --filter "FullyQualifiedName~AgentEngineTests"
dotnet test tests/louis-agent.api.tests
dotnet test tests/louis-agent.orchestrator.tests
dotnet test louis-agent-solution.sln          # everything, including the order-service sample's tests
```

## Test first

A step that changes behaviour starts here ([ADR 0014](../docs/adr/0014-test-first.md)):

1. **Write the test** for the behaviour the step adds, in the folder that mirrors the source
   (`tests/louis-agent.core.tests/Tools/Git/…` tests `Tools/Git/…`). A fix starts with a test that reproduces the bug.
2. **See it fail for the right reason:** run just that test (`dotnet test … --filter "FullyQualifiedName~MyTests"`).
   It should fail on an assertion, not a compile error: give a new member a stub (`throw new
   NotImplementedException()`) so the test compiles. Keep the failing output for the step's explanation.
3. **Implement until it passes**, then run the project's suite and tidy with everything green.

The step is one commit with the test and the code, so every commit passes. A change the tests can't reach yet (the web
app) says so in its explanation, with how it was checked instead.

## Golden files

**What the model reads is pinned by two golden files:** `ModelRequestSnapshotTests` compares the tools, their
descriptions, the system prompt and the engine's fixed texts with `Agent/ModelRequestSnapshot.json`;
`ToolMessageSnapshotTests` compares every `ToolMessage` template with `Messages/ToolMessageSnapshot.json`. A deliberate
change (a new tool or text, an edited description or skill file) regenerates them, and you review their diff before
committing:

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.core.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests"
```

```powershell
$env:LOUIS_UPDATE_GOLDEN = '1'
try { dotnet test tests/louis-agent.core.tests --filter "ModelRequestSnapshotTests|ToolMessageSnapshotTests" }
finally { Remove-Item Env:LOUIS_UPDATE_GOLDEN }
```

The PowerShell variable lasts for the session, hence the `finally`: left set, every later test run rewrites the files
instead of checking them. On Windows, `git status` can list a regenerated file as modified while `git diff` shows
nothing: only its line endings changed, and `git checkout -- <file>` puts it back.

## Writing tests

Tests use `FakeChatClient` (a scripted `IChatClient` that also streams) and `StubHandler` for HTTP — no network, model or
Docker. A few tests skip themselves when git, pip or the network isn't available. The agent runs tests on Linux inside
its containers, so keep them platform-neutral (forward slashes, no Windows-only paths).

The API's sessions are tested (`tests/louis-agent.api.tests`); its endpoints and the web app have no automated tests
yet, so check them by hand (curl and a browser) after changes. The
orchestrator demo (`samples/run-demo.ps1`, in Docker; see [samples/README.md](../samples/README.md)) exercises the
whole loop against a real model: API sessions, the fix flow, and the web app's Branches view.
