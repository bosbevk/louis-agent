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
dotnet test tests/louis-agent.acp-server.tests     # the ACP transcript (LOUIS_UPDATE_GOLDEN=1 regenerates AcpTranscript.json)
dotnet test louis-agent-solution.sln          # everything
```

GitHub Actions runs the last two commands (build, then the whole solution's tests) on Ubuntu on every push and pull
request (`.github/workflows/ci.yml`); a red check on a PR is the same failure you'd see locally with that command.

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

**The web apps' components are pinned the same way** (T1, CL15): `tests/louis-agent.web.shared.tests` renders
`TurnView`, `BlocksView`, `DelegationCard` and `CurrencySwitcher` with bUnit from the recorded Puls turn
(`benchmarks/fixtures/session-stream.sse`) and compares each one's markup with the `.html` golden beside its test
(`Components/TurnView.html`); `tests/louis-agent.puls.tests` does the same for `WorkPanel`, `PulsTile`, `TemplateCard`,
the landing page and the chat page, empty and with the turn (`Pages/ChatPage.empty.html`, `ChatPage.turn.html`), and
`tests/louis-agent.web.tests` for the code-review app's chat page, each against the stub API (`FixtureApi` in
`tests/louis-agent.web.testing`, the library the three share with the fixture replay and the snapshot helper). The
same switch regenerates them:

```bash
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.web.shared.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.puls.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.web.tests
```

A mismatch writes what was rendered to `louis-agent-snapshots/` under the temp folder, so the diff can be read. The
snapshots normalise what isn't the component's doing (per-instance SVG ids, `@ref` element references, the renderer's
event-handler numbers, a thinking block's seconds, and the line breaks and indentation beside tags, so a page split into
components keeps its golden; a space between tags stays, since it can be seen) and run with the invariant culture, so they never depend on the machine.

## Writing tests

Tests use `FakeChatClient` (a scripted `IChatClient` that also streams) and `StubHandler` for HTTP — no network, model or
Docker. The web apps' components render with bUnit (MIT, 2.12.5), its JS interop in loose mode where a component calls
the page's scripts, so a call is answered with nothing and listed, never a failure. Those, `FakePrompts`, `FakeIdentity`, `FakePack` and `TestPaths` live in `tests/louis-agent.testing`, a class library the
test projects that use them reference as a global using (CL15); no test project references another, and `ArchitectureTests` holds it. A few tests skip themselves when git, pip or the network isn't available. The agent runs tests on Linux inside
its containers, so keep them platform-neutral (forward slashes, no Windows-only paths).

The API's sessions are tested (`tests/louis-agent.api.tests`); its endpoints and the web app have no automated tests
yet, so check them by hand (curl and a browser) after changes. The orchestrator demo that exercised the whole loop
against a real model was removed in CL10; it comes back with the rewritten benchmarks.
