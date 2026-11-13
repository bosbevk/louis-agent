---
paths:
  - "src/tests/**"
  - "packs/tests/**"
  - "demos/*/tests/**"
---

# Tests: the how of every rule

Loaded when a file in a test project is read or edited: under `src/tests/`, `packs/tests/` or a demo's `tests/`.
The C# rules in `csharp.md` apply to a test too.

The how-to is [Write and run tests](../../src/docs/guides/write-tests.md): the commands, the shared fakes, the table
of golden files, and how a test project is added. This file holds the rules, and names the part of the guide for
each.

## What every test is

- **Written first, and seen failing** (ADR 0014). The how is in `/do-a-step`.
- **Offline.** No network, no model and no Docker. `FakeChatClient` plays the model and `StubHandler` answers HTTP.
  They are in `src/tests/shared/louis-agent.testing` (*Use the shared fakes*).
- **Platform-neutral.** The agent runs the tests on Linux in its containers, and CI runs on Ubuntu. Build a path
  with `Path.Combine` or forward slashes, never a drive letter or a backslash. A test that needs a tool the machine
  may lack skips itself and says why (*Keep tests platform-neutral*).
- **In the folder that mirrors the source**, with that folder as its namespace. A test file holds at most 400
  lines (*Where a test goes*).
- **In an integration project when it leaves its process.** A test that starts a process, opens Mongo or needs a
  tool lives in a `{name}.integration-tests` project, and its class is `[Category("Integration")]`.
  `IntegrationTestsAreMarkedTests` and `IntegrationPlacementTests` hold it.
- **No test project references another.** `ArchitectureTests` holds it.

## Which tests run, and when

- **While a step is built**: the tests of what the step touched. `/do-a-step` says how to find them, and *Run
  them* has the commands.
- **Before a pull request**: `main` merged in, then the full suite of unit tests. That is in `/ship-it`.
- **On every push to a pull request**, CI runs the build, the unit tests, then the integration tests with
  `INTEGRATION_REQUIRED=1`, on Ubuntu.

## Golden files, and the rule tests

- **What the model reads is pinned to a golden file, and so is a component's markup.** A deliberate change
  regenerates the file with `LOUIS_UPDATE_GOLDEN=1`. Its diff is read before the commit: the diff is the review
  of the change (*Regenerate a golden file*).
- **In PowerShell, clear the variable after the run.** Left set, every later run rewrites the golden files
  instead of checking them.
- **A rule test that fails means the change broke a rule.** Fix the change, not the test. Never add a file to a
  ratchet list to get green (*Keep the rule tests green*).
