# 0119. Named types, declared, then passed

**Status:** Accepted · **Date:** 2026-11-12 (Louis, F127.4.3 step 3, on a Rider diff of a log call: "coding rule from
now on … first declare and name it what it is and then pass into the method"; then "WHY IS THIS AN ANONOMOUS TYPE?",
his pick of three: a record per message; and "we must not use anonomoustypes and dymanics, only when it is the best
option to use") · **Recorded:** 2026-11-12 · Amends [0009](0009-message-text-as-enum-templates.md) and
[0041](0041-json-through-records-by-hand-only-for-bytes.md)

## Context

A message's holes were an anonymous object (ADR 0009), the one place ADR 0041 let one stay. Built inside the call,
and laid out by the formatter one property a line, a one-line `Log.Log(…)` became six lines, the call's own arguments
split around a block of values that had no name and no type:

```csharp
Log.Log(LogLevel.Information, AgentLogMessage.ToolResultSummarising,
    new
    {
        Tool = toolName,
        Chars = output.Length.ToString("N0", CultureInfo.InvariantCulture)
    }, tag: Tag);
```

Nothing ties those names to the template's `{Tool}` and `{Chars}` but a run-time lookup: a misspelt hole throws only
when the line runs. 469 calls work this way today.

## Decision

- **No anonymous types and no `dynamic`,** unless one is plainly the best option (a LINQ projection that never leaves
  the method, say); then a `//` line says why.
- **A message's holes are a record,** named for the member with `Holes` after it, in a `Holes/` folder beside the
  message enum, and the member names it: `[Holes(typeof(EndWithToolHoles))]` over `EndWithTool`. A test holds each
  record's properties equal to its template's holes, so a misspelt hole fails the tests, not the running program.
- **An object built for a call is first a local, named for what it is, then passed,** never built inside the call's
  parentheses: a message's holes, an initializer (`new Foo { … }`, `new Dictionary<…> { [k] = v }`).

  ```csharp
  var summarising = new ToolResultSummarisingHoles(toolName, chars);
  Log.Log(LogLevel.Information, AgentLogMessage.ToolResultSummarising, summarising, tag: Tag);
  ```

- The name says what the values are (`summarising`, `owedTool`), not their type (`holes`, `args`).
- **Existing code changes as it is touched**, the whole file in a commit (`.claude/rules/csharp.md`), never in a sweep.

## Consequences

- Every call reads on one line with plain names; a message's values have a type a reader can find and a test checks.
- A message with holes costs a small record, a few hundred over time. That is the trade, as in ADR 0057: more parts
  that each say what they are beat fewer that don't.
- Until the last anonymous holes are touched, both shapes exist; `MessageText.Format` reads either by property name.

## Links

[0009](0009-message-text-as-enum-templates.md) · [0040](0040-records-for-values-classes-for-things.md) ·
[0041](0041-json-through-records-by-hand-only-for-bytes.md) · [0050](0050-summaries-like-a-person.md) ·
[0057](0057-regex-is-the-last-option.md) · Fowler, [Extract Variable](https://refactoring.com/catalog/extractVariable.html)
and [Introduce Parameter Object](https://refactoring.com/catalog/introduceParameterObject.html)

---
[Docs index](../README.md) · [ADRs](README.md)
