# 0051. No nested conditionals, and an endpoint is a named method on a handler class

**Status:** Accepted (Louis, 2026-10-27) · **Date:** 2026-10-27 (Louis, on the manual endpoint's one-line chain of
`is { } x && … is { } y ? … : …`: "no human will do that") · **Recorded:** 2026-10-29 (CL25, from the rule text in
`docs/CLAUDE.md`)

## Context

Two shapes kept coming back in reviews. A choice between more than two outcomes written as a ternary inside a
ternary (`a ? x : b ? y : z`), or as `if`s nested inside `if`s, which a reader has to unfold in their head. And a
minimal-API endpoint written as a lambda inside `MapGet`, holding its checks and its work in one expression that
declares variables for a ternary, which can't be read, tested or summarised on its own. Microsoft's minimal API
guidance allows both the lambda and the named method; Fowler's *Replace Nested Conditional with Guard Clauses* and
C#'s switch expression with property patterns are the flat forms.

## Decision

- **A choice between more than two outcomes is never nested.** It is a **switch expression on the shape**, one arm
  per case read like a table (`c switch { { Total: 1, Agreed: 1 } => …, { Agreed: 0 } => …, _ => … }`), or **guard
  clauses**, one `if (…) return …;` per case from the top. One plain `a ? x : y` stays. The pattern goes in the
  method's `<remarks>`.
- **An endpoint is a named method.** `MapGet`, `MapPost` and the rest point at a named method
  (`admin.MapGet("/benchmarks/{check}/manual", handlers.GetManual)`), never a lambda that holds the work. An endpoint
  file's methods live on one handler class that holds what they share (the catalogue, the runner, the store), as
  `AdminHandlers` does (Fowler's *Introduce Parameter Object*). Inside it, the checks come first as guard clauses,
  each stopping early with its error, and the happy path last; one condition tests one thing. Its answers are
  typed ([ADR 0045](0045-typed-answers-and-a-shared-contract.md)).
- **Scope:** as the summaries ([ADR 0050](0050-summaries-like-a-person.md)): a file in a step's commit is fixed
  whole, untouched files wait.

## Consequences

- A method reads top to bottom: the ways out first, then the one path that does the work.
- An endpoint can be unit-tested by calling the method, and its summary names what it answers.
- A few more lines per method than the one-line chain; the size limit counts them
  ([ADR 0044](0044-the-size-limit-counts-code.md)), which is the point: a method that no longer fits is one to split.

## Links

[0045](0045-typed-answers-and-a-shared-contract.md) · [0050](0050-summaries-like-a-person.md) ·
[Fowler, Replace Nested Conditional with Guard Clauses](https://refactoring.com/catalog/replaceNestedConditionalWithGuardClauses.html) ·
[Microsoft, minimal API route handlers](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/route-handlers)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
