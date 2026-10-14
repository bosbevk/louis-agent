# 0014. Test first: a step's tests are written and seen failing before the code

**Status:** Accepted · **Date:** 2026-10-14 · **Recorded:** 2026-10-14

## Context

Every step already ships with its test, but the test was written after the code, so it could only confirm what the code
already did: a test that never failed may test nothing, or test the wrong thing. The project's goal is to learn by
measuring, and a test that is seen to fail first is the smallest measurement there is: it proves the behaviour was
missing, and then that the code supplied it.

## Decision

A step that changes behaviour starts with its tests. Write the test for the step's behaviour; run it and see it fail
for the expected reason (an assertion, not a compile error: stub the new member with `NotImplementedException` if
needed); then implement until it passes, and tidy with the tests green. A fix starts with a test that reproduces the
bug. The step stays **one commit** with both test and code, so every commit builds and passes; the failing run's output
goes in the step's explanation. Docs-only and configuration-only steps have no test to write first.

## Consequences

- Each test is shown to catch the missing behaviour; the explanation of a step shows red, then green.
- Planning a step means naming its test first, which keeps steps small.
- Two commits per step (red, then green) were considered and not chosen: half the commits would fail, which breaks
  `git bisect` and the "tests pass after every step" rule.
- Some changes are hard to test first (the web app has no automated tests yet): the step says so and how it was checked
  instead.

## Links

[Tests guide: Test first](../../tests/CLAUDE.md#test-first) ·
[Steps, branches and commits](../CLAUDE.md#steps-branches-and-commits)
