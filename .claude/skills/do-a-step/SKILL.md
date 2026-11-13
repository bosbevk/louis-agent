---
name: do-a-step
description: Build one step, test first. Use it when a step of a plan starts, when Louis says go for a step, or when a fix or a small change is about to be built. It says what to read first, how the test is written and seen failing, which tests to run while building, and when to stop and ask.
---

# Do a step

A step is one commit with its test, small enough for Louis to review in about ten minutes. Four stops come first.

1. **Build only the step Louis asked for.** Not the next step, and not a tidy-up beside it.
2. **The test comes first, and you see it fail.** It fails on an assertion, not on a compile error. Keep the
   failing output. The step is shown with it.
3. **A choice the plan doesn't settle stops the step.** Put it to Louis with `/put-a-finding`. Nothing that depends
   on the choice is built first.
4. **Code changes by editing the file, never by a script.** A rename goes through Rider's rename, which follows
   every reference (ADR 0052). A pure move may copy whole line ranges, proved byte-identical (ADR 0054).

## Read before you write

- **The step in the plan**: what it builds, its test, and the types its namespace map names.
- **The code the step touches**, and how the repo solved the nearest problem.
- **The tests that will run against it.** *Which tests to run while you build*, below, says how to find them.

## Test first

A step that changes behaviour starts with its test (ADR 0014).

1. **Write the test** for the behaviour the step adds, in the folder that mirrors the source.
   `src/tests/engine/louis-agent.core.web.tests/Robots/RobotsPolicyTests.cs` tests
   `src/engine/louis-agent.core.web/Robots/RobotsPolicy.cs`. A fix starts with a test that reproduces the bug.
2. **See it fail for the right reason.** Run just that test, with
   `dotnet test <project> --filter "FullyQualifiedName~MyTests"`. Give a new member a stub,
   `throw new NotImplementedException()`, so the test compiles and fails on its assertion. Keep the output.
3. **Write the code until it passes.** Then run the project's whole suite, not only the filter, and tidy with
   everything green.

A docs-only or configuration-only step has no test first. A change the tests can't reach says so when it is shown,
with how it was checked instead.

The shared fakes, the golden files and where a new test project goes are in `src/docs/guides/write-tests.md`.

## Which tests to run while you build

Louis, 2026-11-07: "We should only run the tests of the code we touched, and when done at the end run a full
suite".

- **The projects the step touches.** That is the step's own test project, and the project that tests anything it
  changed.
- **`louis-agent.core.tests` too, when the step moves files, adds a project or adds a repeated name.** It holds
  the rules that read the whole repo: `SolutionLayoutTests`, the file and folder sizes, `ArchitectureTests` and
  the repeated names.
- **Every project that reads a shared file.** What you touch is every project that reads the file, not the folder
  the file is in. A name added to the framework's colour list,
  `src/web/louis-agent.web/Styles/web/settings/_colours.scss`, is tested twice outside the framework. Puls must set
  every name on that list (`SiteColoursTests` in `louis-agent.puls.tests`). And each film's export records a hash
  of the film's own files (`louis-agent.film-export.tests`). Before you change a shared file, search for who
  reads it, and run their tests.
- **The full suite of unit tests runs before the pull request**, after `main` is merged into the branch, and it
  must pass. Louis, 2026-11-13: "before doing a PR main must be merged and full suite unit tests passes". It is
  not run while a step is built (ADR 0075).

## Look at the real thing

Something a visitor sees is checked by looking at it, not only by its tests. Use `/check-a-page` before the step
is shown.

## Then

Finish the step with `/finish-a-step`.
