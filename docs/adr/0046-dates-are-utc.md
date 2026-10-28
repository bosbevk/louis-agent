# 0046. Dates are UTC: stored, logged and sent as UTC; a screen converts them for the reader

**Status:** Accepted · **Date:** 2026-10-28 (Louis: "all dates must be UTC, for localisation later", then "it must also
be an ADR, do some research to find out if it is the case to use UTC in the system for localisation") ·
**Recorded:** 2026-10-28

## Context

A date and time can be written three ways: as an instant in UTC (`2026-10-28T13:02:21Z`), as a local time with its
offset (`15:02:21+02:00`), or as a local time with nothing (`15:02:21`), which only means something on the machine that
wrote it. The code had no rule, and it showed twice in one week:

- On 2026-10-28 CI went red because a golden file (`RunExplorer.conversation.html`) held a note's time in South
  African time; the test rendered `11:05` on the UTC runner against `13:05` in the file (F18, fixed in b71e6eaf).
- The eval protocol's `t` was Unix seconds with three decimals, a leftover of the Python harness, and the 7c brief
  guessed it was ISO. Protocol v2 asked which one to write.

What the code already does right: the engine takes "now" from `TimeProvider.GetUtcNow()` and converts it to the
user's zone only to show it (`ClockTools.GetToday`), the user's zone travels as its own field (`time_zone` on a
session and on an eval scenario), and the stores use `DateTimeOffset`. The chat views and the orchestrator's log use
`DateTimeOffset.Now` or `DateTime.Now`; the web app's relative times are display, the orchestrator's lines are a
log.

Localisation (F46) will render every date in the reader's language and zone. That is only possible if what is stored
means one instant, whoever reads it.

What the sources say (checked 2026-10-28):

- **Microsoft, *Choosing between DateTime, DateTimeOffset, TimeZoneInfo, and TimeZone*:** "consider `DateTimeOffset` as
  the default date and time type for application development"; a `DateTimeOffset` "always unambiguously identifies a
  single point in time"; "When saving or sharing `DateTime` data, use UTC and set the `DateTime` value's `Kind`
  property to `DateTimeKind.Utc`"; a local or unspecified `DateTime` "is often ambiguous or limited in its
  portability", and only a UTC one identifies a point in time "regardless of the system or time zone in which the
  value is used".
- **Microsoft, *Performing arithmetic operations with dates and times*:** arithmetic on local values ignores
  daylight-saving rules; to respect them, convert "to UTC, performing the arithmetic operation, and then converting
  from UTC back to the time in the time zone".
- **RFC 3339** (the internet profile of ISO 8601): "true interoperability is best achieved by using Coordinated
  Universal Time (UTC)"; `Z` is a UTC offset of 00:00; `-00:00` means the offset is unknown; and "Internet clients
  SHOULD be prepared to transform dates into a display format suitable for the locality", which is exactly the split
  between what is stored and what is shown.
- **System.Text.Json** reads and writes `DateTime` and `DateTimeOffset` in the ISO 8601-1:2019 extended profile, RFC
  3339 compliant, with no converter; a `DateTime` of kind `Utc` writes `Z`, a `DateTimeOffset` at offset zero writes
  `+00:00`; a value that doesn't conform throws.
- **MongoDB** stores a BSON Date as a 64-bit count of milliseconds since the Unix epoch, which the BSON specification
  calls "UTC datetime": the database keeps no zone, whatever the driver was handed.
- **Jon Skeet, *Storing UTC is not a silver bullet* (2019):** UTC is right for anything that has happened (a log line,
  a run, a message), because an instant loses nothing in UTC. It is wrong for a **future local event** (a meeting at
  nine in Amsterdam next year): governments change zone rules, so a UTC value computed today can drift from the
  local time the person meant. There, store the local time and the zone id as the source, and derive UTC from it when
  needed.

These were considered:

- **Local time everywhere, the server's zone.** Rejected: the server's zone is an accident of hosting (Azure runs UTC,
  Louis's PC runs South African time), and a golden file written on one fails on the other, as happened.
- **`DateTimeOffset` with the writer's offset kept.** Rejected: it identifies the instant, but two writers in two zones
  produce two spellings of one instant, a golden still depends on where it was made, and Mongo drops the offset
  anyway. The reader's zone is the one that matters, and the writer doesn't know it.
- **Unix seconds, as the v1 protocol did.** Rejected: an instant, but unreadable in a log line and a unit the reader
  has to know; System.Text.Json needs a converter for it.

## Decision

- **Every date or time that is stored, logged or sent on a wire is UTC**, written in ISO 8601 with an explicit UTC
  designator (`2026-10-28T13:02:21Z`, or `+00:00` where the serializer writes that); never a local time, never a time
  without an offset. This covers the stores, the usage ledger, the eval protocol, the API's replies and the golden
  files.
- **In C#, "now" is `TimeProvider.GetUtcNow()`**, never `DateTime.Now` or `DateTimeOffset.Now`; a type that holds an
  instant is a `DateTimeOffset` (Microsoft's default) or a `DateTime` of kind `Utc`. A test gets a fixed clock.
- **A screen converts for the reader.** The reader's zone is a separate value (`time_zone` on a session, the
  browser's zone in the web app, the host's as a fallback), applied at the last moment, in the view, as
  `ClockTools.GetToday` already does for the model. Localisation (F46) owns the language and the format; this record
  only guarantees it has one instant to work from.
- **The one exception is a future local event**, if the product ever stores one (an appointment, a reminder, a shop's
  opening hours): then the local time and the zone id are the source, kept as entered, and the UTC instant is derived
  from them and recomputed when zone rules change (Skeet's third option). Nothing in the system stores one today.

## Consequences

- **The golden files are safe on any machine**: a time in one is UTC, so Louis's PC and CI render the same text. A
  test that renders a time passes the reader's zone in, never the machine's.
- **Protocol v2's `t` is an ISO 8601 UTC string**, a `DateTimeOffset` on the base record, which System.Text.Json
  writes and reads with no converter (F18 step 7c).
- **Existing code isn't swept.** The eight uses of `DateTime.Now` and `DateTimeOffset.Now` (the chat views in
  `web.shared` and Puls, the orchestrator's comms log, the web app's relative-time text) change when a step touches
  their file, as the summaries rule does; a display-only use becomes a conversion of a UTC value for the reader.
- **Review holds the rule** (docs/CLAUDE.md, *Dates are UTC*) until a test does: a `DateTime.Now`, a `DateTimeOffset.Now`, a local time in a golden or
  a stored document, or an offset-less time on a wire is a finding. A ratchet test over the sources (as
  `RepeatedNameTests` reads them) can hold the `Now` part when the first new offender appears.
- **What gets harder:** reading a stored time by hand means converting in your head (or `date -d`), and a view has to
  know the reader's zone before it can show a time. Both are the price of one meaning per value.

## Links

- [docs/CLAUDE.md](../CLAUDE.md), *Rules that always apply*: *Dates are UTC*.
- [F18 Evaluation](../features/F18-evaluation.md): the time-zone golden (b71e6eaf) and protocol v2's `t`.
- F46 Localisation (TODO): the view that renders a UTC instant in the reader's language and zone.
- [0022](0022-benchmarks-recorded-from-the-protocol.md): the protocol this rule's `t` travels on.
- Microsoft Learn, [Choosing between DateTime, DateTimeOffset, TimeZoneInfo, and TimeZone](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)
  and [Performing arithmetic operations with dates and times](https://learn.microsoft.com/en-us/dotnet/standard/datetime/performing-arithmetic-operations);
  [RFC 3339](https://www.rfc-editor.org/rfc/rfc3339);
  [DateTime and DateTimeOffset support in System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/datetime/system-text-json-support);
  [MongoDB BSON types: Date](https://www.mongodb.com/docs/manual/reference/bson-types/#date);
  Jon Skeet, [Storing UTC is not a silver bullet](https://codeblog.jonskeet.uk/2019/03/27/storing-utc-is-not-a-silver-bullet/).
