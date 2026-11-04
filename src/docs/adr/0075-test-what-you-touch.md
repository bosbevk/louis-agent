# 0075. Test what you touch

**Status:** Accepted · **Date:** 2026-11-04 (Louis: "we do not have to run every test each time we do something we only
test what we touch cause we have alot of unit tests", "we should make an ADR") · **Recorded:** 2026-11-04

## Context

The unit suite has passed 5,000 tests across 25 projects. A habit had grown of running all of it before showing every
step, because `louis-agent.core.tests` holds rules that read the whole repo. Each run takes minutes and rarely finds what
the touched project's own tests didn't.

## Decision

- **While working, run the test projects the change touches.** That is the step's own project, and the project that tests
  anything it changed.
- **When a change moves files, adds a project or adds a repeated name, also run the repo-wide rules** in
  `louis-agent.core.tests`: `SolutionLayoutTests`, the file and folder sizes, `ArchitectureTests` and the repeated names.
  They are the tests such a change touches.
- **The whole solution still runs before a pull request**, and CI runs all of it on every one.

## Consequences

- A step is shown sooner, and its explanation says which tests ran.
- A break outside the touched projects can wait until the run before the PR. CI catches what that run misses, so it never
  reaches `main`.

## Links

[0014](0014-test-first.md) · [Finish a step](../guides/finish-a-step.md)

---
[Docs index](../README.md) · Related: [Write and run tests](../guides/write-tests.md) · [ADRs](README.md)
