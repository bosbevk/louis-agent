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

### 2. A 201 on the admin wire can't take the wire's options (Part 2, notes and re-scores)

**What came up.** A note and a re-score answer 201 with a Location. `TypedResults.Created(uri, value)` has no
`JsonSerializerOptions` overload: it writes with the app's default options (camelCase), while the admin wire is
snake_case (`AdminJson.SnakeCase`). For a note every name is one word, so the bytes happen to match; for a re-score
(`exit_code`, `started_at`) they would not.

1. **A small result of our own, `CreatedJson<T>` (picked).** It sets the status and the Location and writes the value
   with the options it is given, and says what it produces for OpenAPI. Pattern: a custom `IResult`, Microsoft's way to
   shape an answer the built-in ones don't (minimal APIs, *Responses*, "Customizing responses"). Changes: one class in
   the API (`Endpoints/Answers/`). Costs: one more type to keep, and its OpenAPI metadata written by hand.
2. **A JSON answer with status 201, the handler setting Location itself** (`TypedResults.Json(…, statusCode: 201)` and
   an `HttpResponse` parameter). Changes: the handlers only. Costs: the declared answer says "JSON", not "created";
   a handler writes a header as a side effect, and a no-HTTP test needs a fake response.
3. **`[JsonPropertyName]` on the records the 201s send**, keeping `TypedResults.Created`. Changes: two records. Costs:
   it undoes fix 2 of the review (the names live twice, once as an attribute and once as the wire's rule), on exactly
   the records where it matters.

**Why 1.** It keeps one options object per wire (ADR 0041) and lets the signature say what the route does (ADR 0045's
point). Sources: learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses (custom `IResult`,
`IStatusCodeHttpResult`, `IValueHttpResult<T>`, `IEndpointMetadataProvider`); ADR 0041; ADR 0045. **Commit:** the
notes file of the sweep.

### 3. The session request bodies: move them now, or when a change touches them (Part 2, sessions)

**What came up.** The brief puts "replies and bodies" in the contract. The session bodies
(`CreateSessionRequest`, `SendMessageRequest`, `AttachmentRequest`, `ReplayRequest`) carry the API's own parsing
(`RoleOrNull`, `StreamFormatOrNull`, `Place`, `InvalidTag`, which use core's types), and five test files and the
replay rules use them. Their clients (the eval runner's `HttpSessionClient`, the orchestrator) keep their own copies.

1. **Move them now**, the parsing as C# 14 extension members in the API (as finding 1). Changes: four records, a
   rules class, `ReplayResolution`, `TurnStream` and five test files brought up whole. Costs: the largest commit of
   the sweep for no change on the wire and no client that reads the contract yet.
2. **Move the replies now, the bodies when a change touches them (picked).** ADR 0045's consequences already say
   other clients move "when a change touches them". Changes: the session file only. Costs: the brief's "bodies" is not
   met for the sessions API in this run; the session bodies stay internal records in the API for now.
3. **Move only the plain records**, leaving thin API wrappers that parse. Changes: four records and four wrappers.
   Costs: two types per body, the worst of both.

**Why 2.** The bodies' JSON is the sessions API's, which must not change, and no client reads the contract for it yet;
moving them is churn with no reader. A TODO line names the move, so the first change to a session body (or the eval
runner switching to the contract) does it. Sources: ADR 0045 *Consequences*; Fowler, *Refactoring* 2nd ed., "the
Rule of Three" (refactor when the need is there). **Commit:** the sessions file of the sweep. Louis may prefer 1;
it is a mechanical redo.
