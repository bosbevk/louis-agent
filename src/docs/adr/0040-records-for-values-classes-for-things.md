# 0040. Records for values, classes for things

**Status:** Accepted · **Date:** 2026-10-25 (Louis: "why are we using records instead of classes?", then "should it not
be recorded as an ADR") · **Recorded:** 2026-10-25

## Context

C# has two ways to declare a type that carries data:
- **A `record`** has value equality: two records with the same contents are equal. Its positional properties are
  init-only, and it comes with `with` copies, deconstruction and a `ToString` that prints every field.
- **A `class`** has reference identity: two instances are two things, whatever they hold.

The code has drawn one line between them without writing it down. On `main` on 2026-10-25, core, the hosts, the
identities, the packs and the tools held 102 sealed records and 131 sealed classes, against 3 records and 7 classes that
aren't sealed:
- **Requests, answers, keys and lines of a file are records:** `WebSearchQuery`, `CreateSessionRequest`, `SessionReplay`,
  `UsageRecord`.
- **What owns a resource or changes over time is a class:** `PageFetcher` owns an `HttpClient`, `JsonlUsageSink` writes a
  file, `AgentSessions` holds the open sessions.
- **Some code depends on the line.** F18's replay (step 4a) finds a recording by comparing two asks with `!=`, which works
  only because the ask is a record.

An agent copies the patterns it sees, so the line has held. But nothing states it, and the mistakes are quiet. A store
made a record would compare equal to another store on the same folder. A key made a class would never equal the key it
was saved under, so every lookup would miss.

These were considered:
- **Leaving it to review.** Rejected: as [0034](0034-code-a-person-can-maintain.md) found, a rule that lives only in a
  prompt states intent, and a session forgets it.
- **Records everywhere.** Rejected: value equality is wrong for a thing with identity. And a record's generated `Equals`
  over a lock or a client compares them by reference, so it promises a value it doesn't have.
- **Classes everywhere, with equality written by hand.** Rejected: it's boilerplate that drifts as fields are added.
- **Record structs.** Not taken: no measured hot path needs them yet.

## Decision

- **A record is a value.** It is data that is passed around, read from JSON, used as a key or compared, and that nobody
  should change on the way: a request, an answer, a key, a settings snapshot, a line of a file, an event. If two
  instances with the same contents should count as the same thing, the type is a record. Positional
  (`record WebSearchResult(string Title, string Url, string Snippet)`) by default; init-only properties where JSON binding
  or many optional fields need them.
- **A class is a thing.** It owns a resource (a lock, an `HttpClient`, a stream, a file), changes over time, or has
  behaviour that matters more than its data: a service, a tool, a host, a decorator. Two of them are two things, even
  when they hold the same.
- **A record holds no resource.** None of its fields is a lock, a client, a stream, an `IDisposable` or a mutable
  collection. A value that needs one is a class.
- **Sealed by default.** A record or a class is `sealed` unless it is made to be inherited. A closed set of cases is an
  abstract record with a private constructor, and its cases are sealed records nested inside it (as F18's
  `ReplayAnswer<T>`), so no case can be added from outside.
- **Equality doesn't look inside a collection.** A record that holds a list compares the list by reference, so two
  records that look equal aren't. A record used as a key holds only values: strings, numbers, enums
  ([0024](0024-choices-are-enums.md)) and other value records. Otherwise its `Equals` is written by hand. A test compares
  a list-holding record field by field.
- **A static class holds functions and constants, and no state,** named for what it does (`TrialFileName`,
  `SettingChoice`). It is neither a value nor a thing, and 0034's ban on `Helpers` and `Utils` still applies.

## Consequences

- **Review holds the rule,** beside 0034's. No test holds it yet. The one mechanical part (no record field typed as a
  lock, a client, a stream or an `IDisposable`) could become a test, added when the first offender shows up.
- **Existing code isn't swept.** What doesn't follow the rule changes when a step touches it:
  - `TokenPrices`, with `ModelPrice` and `LongPromptPrices` built on it, holds init-only prices read from JSON, so it is a
    value written as a class hierarchy;
  - `MarkdownSkillLoader.SkillDef` and seven classes aren't sealed. Each is sealed when a step touches it, unless
    something inherits it (`AnthropicUsageMapper` inherits `StandardUsageMapper`).
- **Two things come free.** A `with` copy changes one field safely. Value equality makes a record work as a dictionary
  key and in a test's `Is.EqualTo`.
- **A record prints every field.** Its generated `ToString` shows them all in a log line or a debugger, so a value that
  carries a key or a token overrides `ToString` or keeps the secret out of the record.
- **Not decided here:** record structs, for when a measured hot path needs one.

## Links

- [0010](0010-folders-by-concern.md) and [0034](0034-code-a-person-can-maintain.md): one concern per class. This record
  says which kind of type that concern is.
- [0024](0024-choices-are-enums.md): a choice is an enum, which is what a record's key holds.
- [0007](0007-measure-every-request.md): unknown is null, never 0, so a value's missing field is null.
- [F18 Evaluation](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md): the replay's asks are compared as values, and its answers are a
  closed set of cases.
