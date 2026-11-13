---
paths:
  - "**/*.cs"
---

# C#: the how of every rule

Loaded when a `.cs` file is read or edited. How a summary is written is in `csharp-summaries.md`, which loads
beside this file. The ADRs named below are in `src/docs/adr/`.

The rules come in this order: what "fix as you touch" covers, a choice between outcomes, an endpoint, regular
expressions, values and what is stored or sent, choices and texts, size and shape, layout, and how a file is
changed.

## What "fix as you touch" covers

Several rules below say that existing code changes as it is touched. That means only the files in the step's own
commit (`git diff --name-only`). Each is fixed whole, the parts the step didn't change too. A file the step only
read waits. It is never a big refactor of its own.

## A choice between outcomes

- **More than two outcomes are never nested.** Not `a ? x : b ? y : z`, and not `if`s inside `if`s.
- **Use a switch expression on the shape**, one arm per case, read like a table:
  `c switch { { Total: 1, Agreed: 1 } => …, { Agreed: 0 } => …, _ => … }`.
- **Or use guard clauses**, one `if (…) return …;` per case from the top.
- One plain `a ? x : y` stays.
- The pattern goes in the method's `<remarks>`.

*Decided in:* ADR 0051.

## An endpoint

- **`MapGet`, `MapPost` and the rest point at a named method**, as in
  `evals.MapGet("/{check}/manual", handlers.GetManual)`. Never a lambda that holds the work.
- **An endpoint file's methods live on one handler class** that holds what they share, as `AdminHandlers` does.
- **Inside a method the checks come first, as guard clauses**, each stopping early with its error. The happy path
  comes last. One condition tests one thing. Never `is { } x && … is { } y ? … : …`.
- **It returns `TypedResults`, with its answers declared**:
  `Results<JsonHttpResult<TrendReply>, JsonHttpResult<ApiError>>`, named once with a `using` alias. An error is the
  `ApiError` record.
- **The records and each wire's options object live in `louis-agent.api.contracts`.** So a client reads
  `TrendReply`, never a `JsonElement` picked apart by name.

*Decided in:* ADR 0051, ADR 0045.

## A regular expression is the last option

Plain string methods, a small reader or a typed parser come first. When nothing plainer does it, use a named
`[GeneratedRegex]`, with one matching and one non-matching example in its summary, and `NonBacktracking` on outside
input. An existing one is replaced as the code is touched, never in a sweep.

*Decided in:* [ADR 0057](../../src/docs/adr/0057-regex-is-the-last-option.md).

## Values, and what is stored or sent

- **Records for values, classes for things.** A request, an answer, a key or a line of a file is a record. What
  owns a lock, a client or a file, or changes over time, is a class. A record holds no resource. Both are sealed.
- **JSON goes through records.** A known shape is a record the serializer writes and reads, with one options
  object per wire, never changed for one caller.
  - `JsonObject` is only for a shape decided at run time.
  - `JsonElement` is for input we don't own. A string is read with `GetString()`, never `GetRawText()`.
  - `Utf8JsonWriter` is only for when the bytes are the contract.
- **Never an anonymous object for anything serialized**, such as `Results.Json(new { … })`. A reply, a body or a
  stored document is a named record, and a message's holes are too.
- **A record built from another type has a static `Of`**, as in `CheckRow.Of(check, hasManual)`. So a reply reads
  as a few calls, never rows built inline in a lambda.
- **MongoDB goes through records.** A collection of a known shape is a typed `IMongoCollection<T>` over a document
  record beside its store. Stored names are `[BsonElement]`, enums are stored by wire name, and dates as UTC BSON
  dates. Filters, sorts and indexes are typed. `BsonDocument` is only for a shape decided at run time.
- **Dates are UTC.** A date is stored, logged or sent as ISO 8601 UTC, `2026-10-28T13:02:21Z`. "Now" is
  `TimeProvider.GetUtcNow()`, never `DateTime.Now`. A screen converts for the reader, never the code that writes.
  A golden file holds UTC. A future local event keeps its local time and zone id. Existing uses change as touched.
- **Nothing is backward compatible before a sale.** No alias, lenient reader or second parser for what we store
  or send between our own parts.

*Decided in:* ADR 0040, 0041, 0048, 0046, 0049, 0119.

## Choices, texts and named types

- **A choice is an enum.** A value from a fixed set is an enum. It is parsed once, where it enters, with
  `SettingChoice.TryParse` (`louis-agent.choices`: case ignored, aliases, never a number). It is written with
  `.Wire()`, the text on `[WireName]`, never `ToString()`. Code switches on the enum, and the "supported" list
  comes from it.
- **A name that repeats is an enum too**: a toolset, a role, a mode. Each pluggable project (pack, identity, eval
  set) keeps its own copy, and the rest share one. Wire names stay as they are.
- **No message text inline.** Exceptions, log lines, tool results and user text are message enum members with a
  `[Description]` template, filled by name (`MessageText.Format`).
- **Logs go through `ILogger` to stderr**, since stdout is a protocol for the ACP and MCP servers.
- **A text the model reads holds no name and no date of who decided it**: a tool's `[Description]`, a tool's
  message. The rule and its reason are in `prompts.md`.
- **Named types, declared, then passed.** No anonymous types and no `dynamic`, unless one is plainly the best
  option, with a `//` line saying why.
  - A message's holes are a record named `{Member}Holes`, in a `Holes/` folder beside its enum. The member names
    it: `[Holes(typeof(EndWithToolHoles))]`. `MessageHolesTests` holds its properties equal to the template's holes.
  - An object built for a call (holes, an initializer) is first a local named for what it is, then passed:
    `var owedTool = new EndWithToolHoles(owed);` then `MessageText.Format(AgentToolMessage.EndWithTool, owedTool)`.
    It is never built inside the call's parentheses.
  - Existing code changes as it is touched.

*Decided in:* ADR 0024, 0043, 0009, 0119.

## Size and shape

- **A file holds at most 200 lines of code**, and a test file 400. Blank and comment lines don't count.
  `FileSizeTests` holds it.
- **A folder with two kinds of thing, or more than six source files, gets subfolders by concern.**
  `FolderSizeTests` holds the count.
- **Split a bigger class by responsibility.** Group its methods by the fields they use and the reason they would
  change. Give each group its own class, named for what it does, as `GitRunner` and `BranchPrinter` are. The
  original keeps the flow. Pure logic gets its own tests. I/O stays at the edge.
- **Nothing that hides the size instead of splitting it**: a `partial` class (other than a code-behind, or one
  `[GeneratedRegex]` requires), a `#region`, or a `Helpers`/`Utils`/`Manager` grab-bag.
- **Namespace = folder path, and dependencies run one way.** `ArchitectureTests` holds it.

*Decided in:* ADR 0010, 0034, 0044.

## Layout

The layout rules are the root `.editorconfig`:

- four spaces, and a 120-character line
- one file-scoped namespace line, with the usings above it
- each brace on its own line, and no trailing spaces
- an initializer (`new { … }`, `new Foo { … }`) with two or more properties puts each on its own line, and one
  property stays on one line

A line that is long only because of a web address, such as a `<seealso href>`, may run past 120.

## How a file is changed

- **Code and comments change only by editing the file.** Never a script that rewrites source files. A pure move
  may copy whole line ranges, proved byte-identical (ADR 0054).
- **Layout changes only through the formatter**: Rider's *Reformat Code*, and then
  `dotnet format louis-agent-solution.sln --include <the commit's .cs files>`, which has the last word, since CI
  checks with it.
- **A `//` or `///` line over 120 is wrapped by hand.** Rider never wraps a comment. Where no Rider runs, every
  long line is wrapped by hand.
- **A rename goes through Rider's rename refactoring**, never search and replace.
- **In Rider, Rider's tools come first** for any job they cover. The order and the tool-to-job table are in the
  [finish-a-step](../skills/finish-a-step/SKILL.md) skill.
- **Test first.** A step that changes behaviour starts with its test, run and seen failing for the expected
  reason, then the code, in one commit with both. Tests run on Linux too
  ([Write and run tests](../../src/docs/guides/write-tests.md)). The how is in `/do-a-step`.

*Decided in:* ADR 0052, 0054, 0014.
