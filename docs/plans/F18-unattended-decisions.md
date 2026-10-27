# F18 unattended run: the decisions log

> **Plan (log).** What the unattended run of 2026-10-27 decided on its own, finding by finding, so Louis can read
> each choice afterwards and have any he disagrees with redone. It is not the plan: nothing here goes under the F18
> plan's *Approved during the build* until Louis has read it. **Elsewhere:** the plan itself in
> [F18-evals.md](F18-evals.md), the feature in [F18-evaluation.md](../features/F18-evaluation.md).

## Where it started

`git pull --rebase` on `feature/F18-evals` at `a86b96e5` (ADR 0045). `dotnet build louis-agent-solution.sln`: 0 errors,
8 warnings. `dotnet test louis-agent-solution.sln` (unit and integration): **3,692 passed, 0 failed**. The integration
tests that need MongoDB, Chrome, SearXNG or the network skip themselves on this machine (71 in the api and core
integration summaries, and the shops catalogue tests), as they are written to.

## Findings

Each finding: what came up, three options (the pattern, what it changes, what it costs, the sources), the pick and
why, and the commit that carries it.

### 1. Where a reply's `Of` lives once the records move to a project that references nothing (Part 1.1)

**What came up.** ADR 0045 puts the reply records in `louis-agent.api.contracts`, which references nothing, and the
guide says a record built from another type gets a static `Of` on the record (`CheckRow.Of(check, hasManual)`). But
`Of` takes the API's own types (`BenchmarkCheck`, `BenchmarkTrends.Point`, `CategoryTrends.Category`), which the
contracts project can't see.

1. **C# 14 static extension members in the API (picked).** The records move; an `extension(CheckRow) { public static
   CheckRow Of(…) }` block in the API adds `Of` to each record where the domain types live, so every call stays
   `CheckRow.Of(…)`. Pattern: Fowler's *Replace Constructor with Factory Function*, placed by PoEAA's *Data Transfer
   Object* advice that the assembler sits on the server side. Changes: one file of extension blocks in the API.
   Costs: a reader has to know C# 14's extension members; `Of` is not on the record's own file.
2. **A mapper class in the API** (`AdminReplies.Check(check, hasManual)`). PoEAA's *Mapper*/*DTO Assembler*.
   Changes: every call site. Costs: breaks the guide's `CheckRow.Of` naming, which Louis just set.
3. **Let the contracts reference the API's domain.** Keeps `Of` on the record. Costs: breaks ADR 0045 ("references
   nothing") and drags the server into every client, the browser included.

**Why 1.** It is the only option that keeps both rules Louis wrote (the contract references nothing, the factory is
`Record.Of`). Sources: Microsoft, *Extension members* (C# 14,
learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/extension-methods); Fowler, *Refactoring*
2nd ed., *Replace Constructor with Factory Function*; Fowler, *PoEAA*, *Data Transfer Object* (the assembler).
**Commit:** Part 1.1.
