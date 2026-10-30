---
paths:
  - "**/*.cs"
---

# C#: the how of every rule

Loaded when a `.cs` file is read or edited. The why of each rule is its ADR: 0009 message enums, 0024 and 0043 choices
are enums, 0034 and 0044 size, 0040 records, 0041 JSON, 0045 typed answers, 0046 UTC, 0048 Mongo, 0050 summaries,
0051 flat conditionals, 0052 edit never script (`docs/adr/`). **Scope of every "fix as you touch" rule:** only the
files in the step's own commit (`git diff --name-only`), each fixed whole, the parts the step didn't change too; a
file the step only read waits; never a big refactor of its own.

## Summaries (ADR 0050)

Every class and method has a `<summary>`, private and test ones included. A class (record, interface) has up to three
parts; a method its `<summary>`, and a `<remarks>` when it carries a pattern of its own:

- `<summary>`: what it does, written like a person (below).
- `<remarks>`: when it carries a pattern, which one and **why it was chosen here**: the problem it solves for this
  class, and what we'd lose without it.
- `<seealso>`: one per ADR that shaped this class (not every ADR that applies), linked on GitHub, as
  `ADR 0024: choices are enums`.

**Write them like a person, not a robot:** say what it does for you, the way you'd tell a colleague at your desk, in
one or two short sentences; leave out what the code already shows (the types, null, "returns", "true when", every
edge case); step numbers belong in the commit and the feature doc.

| Robot | Person |
|---|---|
| *True when the browser address is set and is an absolute URL.* | *We can only use Chrome if we know where it runs.* |
| *Strategy: the setup of the renderer named, else the first ready one in Setups; null when the name is unknown or none is ready.* | *Uses the renderer you named. If you named none, it takes the first one that's set up.* |
| *Chrome.* | *This is the Chrome one.* |
| *Null Object: whichever settings are given, a setup's renderer is the unavailable one exactly when the setup is not ready.* (a test) | *If a renderer isn't set up you get the "not set up" one, and only then.* |

A whole class, `ChromeSetup`:

```csharp
/// <summary>
/// Reads pages through our own headless Chrome. If we don't know where Chrome runs, you get a renderer that tells
/// the model to set BROWSER_URL.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. Each renderer knows for itself whether it's set up and how to build itself, so
/// "is it ready?" and "build it" can't drift apart, and a new renderer is one new class instead of another case in
/// two switches. When Chrome isn't set up it hands back a renderer that explains what's missing, so nothing has to
/// check for null and the model still gets a useful answer.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0038-the-browser-says-what-it-is.md">ADR 0038: the browser says what it is</seealso>
internal sealed class ChromeSetup : IPageRendererSetup
```

**Inside a method, explain the steps:** a step a reader can't follow at a glance (building a request, a `try` that
hides a failure path, a trick such as `using var _`, a rule the code enforces) gets one plain `//` line above it: what
it does and why, like a person. Obvious lines (`_busy = true;`) stay bare. **Steps that belong together sit together,
with one blank line between groups**, like paragraphs: each group does one job and carries its comment line on top
when it needs one; a test's arrange, act and assert are three groups. From `NotesPanel.AddAsync`:

```csharp
// Post it to this run's notes. The run id is escaped because it goes into the web address.
using var request = new HttpRequestMessage(HttpMethod.Post, $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/notes") { Content = JsonContent.Create(body) };
// We only need to know it worked. If the API says no, it throws, and the catch below shows why.
using var _ = await Api.SendAsync(request);
// Saved: empty the box, and tell the run page to fetch the notes again (we never add it to the list ourselves).
_text = "";
State.Written();
```

And grouped, from a test's `StartAsync`:

```csharp
// A store that already holds one finished run.
var store = new InMemoryBenchmarkStore();
await store.SaveRunAsync(new Run(RunId, SampleEval.Beta, 1, BenchmarkRunStatus.Done, T0, T0.AddMinutes(5), 0));

// The API with the admin key, that store, and a clock that never moves.
var app = ApiTestHost.Build(adminKey: AdminKey, store: store, time: new FixedClock(Now));
await app.StartAsync();

return new Host(app, app.GetTestClient(), store);
```

## No nested conditionals, and endpoints (ADR 0051, 0045)

- A choice between more than two outcomes is never `a ? x : b ? y : z` or `if`s inside `if`s. It is a **switch
  expression on the shape**, one arm per case read like a table (`c switch { { Total: 1, Agreed: 1 } => …,
  { Agreed: 0 } => …, _ => … }`), or **guard clauses**, one `if (…) return …;` per case from the top. One plain
  `a ? x : y` stays. The pattern goes in the method's `<remarks>`.
- `MapGet`, `MapPost` and the rest point at a named method (`admin.MapGet("/benchmarks/{check}/manual",
  handlers.GetManual)`), never a lambda that holds the work; an endpoint file's methods live on one handler class that
  holds what they share, as `AdminHandlers` does. Inside: the checks first as guard clauses, each stopping early with
  its error, the happy path last; one condition tests one thing; never `is { } x && … is { } y ? … : …`.
- It returns `TypedResults`, its answers declared (`Results<JsonHttpResult<TrendReply>, JsonHttpResult<ApiError>>`,
  named once with a `using` alias), errors as the `ApiError` record; the records and each wire's options object live
  in `louis-agent.api.contracts`, so a client reads `TrendReply`, never a `JsonElement` picked apart by name.
- **A regular expression is the last option** ([ADR 0057](../../docs/adr/0057-regex-is-the-last-option.md)): plain
  string methods, a small reader or a typed parser first; when nothing plainer does it, a named `[GeneratedRegex]`
  with one matching and one non-matching example in its summary, `NonBacktracking` on outside input; replaced as
  the code is touched, never in a sweep.

## Values, JSON, Mongo, dates, choices, messages

- **Records for values, classes for things** (0040): a request, an answer, a key or a line of a file is a record;
  what owns a lock, a client or a file, or changes over time, is a class; a record holds no resource; both sealed.
- **JSON through records** (0041): a known shape is a record the serializer writes and reads, one options object per
  wire, never changed for one caller; `JsonObject` only for a shape decided at run time, `JsonElement` for input we
  don't own (`GetString()`, never `GetRawText()`), `Utf8JsonWriter` only when the bytes are the contract. **Never an
  anonymous object for anything serialized** (`Results.Json(new { … })`): a reply, a body or a stored document is a
  named record. A message's holes keep theirs (`new { Check = check }`). A record built from another type has a
  static `Of` (`CheckRow.Of(check, hasManual)`), so a reply reads as a few calls, never rows built inline in a lambda.
- **MongoDB through records** (0048): a collection of a known shape is a typed `IMongoCollection<T>` over a document
  record beside its store, stored names as `[BsonElement]`, enums by wire name, dates as UTC BSON dates, filters,
  sorts and indexes typed; `BsonDocument` only for a shape decided at run time.
- **Dates are UTC** (0046): stored, logged or sent as ISO 8601 UTC (`2026-10-28T13:02:21Z`); "now" is
  `TimeProvider.GetUtcNow()`, never `DateTime.Now`; a screen converts for the reader, never the code that writes; a
  golden file holds UTC; a future local event keeps its local time and zone id. Existing uses change as touched.
- **Choices are enums** (0024, 0043): a value from a fixed set is an enum, parsed once where it enters with
  `SettingChoice.TryParse` (`louis-agent.choices`; case ignored, aliases, never a number), written with `.Wire()`
  (the text on `[WireName]`, never `ToString()`); code switches on the enum and the "supported" list comes from it. A
  name that repeats (a toolset, a role, a mode) is an enum too: each pluggable project (pack, identity, eval set) keeps
  its own copy, the rest share one; wire names stay as they are.
- **No message text inline** (0009): exceptions, log lines, tool results and user text are message enum members with
  a `[Description]` template, filled by name (`MessageText.Format`); logs go through `ILogger` to stderr, since
  stdout is a protocol for the ACP and MCP servers. Nothing is backward compatible before a sale (0049): no alias,
  lenient reader or second parser for what we store or send between our own parts.

## Size and shape (ADR 0010, 0034, 0044)

A file holds at most 200 lines of code (a test file 400); blank and comment lines don't count. A folder with two kinds
of thing or more than about six files gets subfolders by concern. Split a bigger class by responsibility: group its
methods by the fields they use and the reason they would change, give each group its own class named for what it
does (`PaymoClient`, `DiffFormatter`); the original keeps the flow, pure logic gets its own tests, I/O stays at the
edge. A `partial` class (other than a code-behind or one `[GeneratedRegex]` requires), `#region`, or a
`Helpers`/`Utils`/`Manager` grab-bag hides the size instead of splitting it. Namespace = folder path; dependencies one
way. `FileSizeTests`, `FolderSizeTests` and `ArchitectureTests` hold it.

## Formatting, and how a file is changed (ADR 0052)

- The layout rules are the root `.editorconfig`: four spaces, a 120-character line, one file-scoped namespace line
  with the usings above it, each brace on its own line, no trailing spaces; an initializer (`new { … }`,
  `new Foo { … }`) with two or more properties puts each on its own line, one property stays on one line. A line long
  only because of a web address (a `<seealso href>`) may run past 120.
- Code and comments change only by editing the file; layout only through Rider's *Reformat Code* and then
  `dotnet format louis-agent-solution.sln --include <the commit's .cs files>`, which has the last word (CI checks
  with it); never a script that rewrites source files (a pure move may copy whole line ranges, proved byte-identical:
  ADR 0054). A `//` or `///` line over 120 is wrapped by hand (Rider never wraps a comment), as is every long line
  where no Rider runs. A rename goes through Rider's rename refactoring,
  never search and replace. **In Rider, Rider's tools come first** for any job they cover; the order and the
  tool-to-job table are in [Finish a step](../../docs/guides/finish-a-step.md).
- **Test first** (0014): a step that changes behaviour starts with its test, run and seen failing for the expected
  reason, then the code; one commit with both. Tests run on Linux too ([Write and run tests](../../docs/guides/write-tests.md)).
