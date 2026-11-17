# 0126. A name says what it is, or what it does, by its kind

**Status:** Accepted · **Date:** 2026-11-17 (Louis, of `PacedStream.Of(turn, clock)`: "this Of method is this a
valid naming pattern?"; then "make method name rules and ADR when to use what naming convention", "research method
name rules", "we must apply the new method names rules on all touched files"; and of the proposal, "yes with the
test-builder exception". Widened the same day to types: "the adr we did for method names we must extend it to class
names do the research", and of the proposal, "yes widen the ADR") · **Recorded:** 2026-11-17

**It binds** all C# in the repo: the framework, the packs and every demo.

## Context

F155's paced stream was first written `PacedStream.Of(turn, clock)`. The repo has one written naming rule for a
method, that a record built from another type has a static `Of` ([0040](0040-records-for-values-classes-for-things.md)).
That fits when `X.Of(y)` gives back an `X`. The paced stream gave back a paced version of the turn's stream, not a
`PacedStream`, so the name promised something it didn't do, and nothing said what it should have been.

Counted on the day of the decision, static methods in the repo name their factories `Of` 251 times, `Read` 42,
`Parse` 17, `From` 16, `For` 15, `Load` 13 and `Create` 12. Only `Of` had a rule; the others grew by habit, and a
reader can't tell from `From` and `For` what each one means.

Microsoft's .NET design guidelines settle most of it. A method's name is a verb or a verb phrase, a property's a
noun or an adjective, and a Boolean property reads affirmative. A method that may fail in a common case is `Try…`
and returns a Boolean. A method that returns something to await ends in `Async`, and an operator that combines others
needn't. Extension methods are for helping every implementation of an interface, or for where an instance method
would break a dependency rule, and their namespace says what they do, not "Extensions". No abbreviations, no
Hungarian notation and no `Ex` suffix. The guidelines leave the factory words to each library, so this ADR names
them.

The same day the rule was widened to types. The guidelines say a class or struct is a noun with no prefix, an
interface is `I` and an adjective or noun, its standard implementation the same name without the `I`, an enum
singular unless it holds flags, a generic parameter `T` or `T` and what it is, and a type derived from a common .NET
base ends with that base's word. No type shares its namespace's name, a core .NET type's name, or a bare generic word
such as `Element` or `Message`. Counted that day, the repo's type names end in a word of its own for one kind of
thing: `…Holes` 73 times, `…Reply` 58, `…Request` 34, `…Handlers` 32, `…Options` 17, and `…Tests` on every test
class. None ends in `Helper`, `Utils` or `Manager`, which the C# rules already ban; a few end in a word that says
nothing of what the type is: `…Info` 13 times, `…Services` 5, one `…Data` and one `…Base`.

## Decision

**Methods**

- **A method is a verb or a verb phrase:** `Read`, `Take`, `FindLastWordEnd`. A property or a field is a noun or an
  adjective, and a yes or no reads affirmative, with `Is`, `Can` or `Has` only where it reads better.
- **A method that answers an event is `On` and what happened,** as Blazor's own are (`OnClick`, `OnAfterRender`):
  `OnPointerCame`, `OnBoxLeftAsync`, `OnCaptionLeft`.
- **`Async` ends a method that returns something to await or to stream:** a `Task`, a `ValueTask` or an
  `IAsyncEnumerable`. A stream operator is the one exception.
- **A stream operator is an extension method named for what it gives back,** as LINQ's `Where` and .NET's
  `WithCancellation` are: `turn.Paced(clock)`. An extension method is used only for such an operator on an
  interface, or where an instance method would break a dependency rule ([0010](0010-folders-by-concern.md)). Its
  holding class is named for its idea (`PacedStream`), never `…Extensions`.
- **A factory's word says what it is made from:**
  - `Of(x)`: a record built from another of our types, as [0040](0040-records-for-values-classes-for-things.md) says.
  - `From(x)`: built from a shape from outside: JSON, the settings, a file.
  - `For(key)`: one chosen for a key, as a Strategy is picked by a name ([0003](0003-provider-strategies.md)).
  - `Create` or `Build`: a class that holds something, a client or a file.
  - `Parse` and `TryParse`: from text.
- **`Try…` returns a Boolean,** its result in an `out` parameter. A twin that throws is added only where a caller
  needs one, where Microsoft's guideline asks for one always: here every caller so far has its own answer to a miss.
- **No `Get` on what should be a property, no abbreviation, no `Ex` or `2` suffix:** a new version takes a new name
  that says how it differs.
- **A test's data builder may be named for what it builds,** as `Text("…")` or `Turn([…])` give a test its input. It
  is a test's own shorthand, read beside the test that uses it, and a verb there (`MakeText`) only adds noise.

**Types**

- **A class, record or struct is a noun naming what it is,** in plain words and with no prefix. No word that says
  nothing of what it is, as its name or its ending: `Manager`, `Helper`, `Utils`, `Data`, `Info`, `Service`, `Base`.
- **.NET's own endings where they apply:** `…Exception`, `…Attribute`, `…EventArgs`, `…Stream`, `…Collection`,
  `…Dictionary`, and a delegate's `…Callback` or `…EventHandler`.
- **An interface is `I` and a noun or an adjective,** and its standard implementation is the same name without the
  `I`.
- **An enum is singular, or plural when it is `[Flags]`,** with no `Enum` or `Flag` ending and no prefix on its values.
- **A generic parameter is `T`, or `T` and what it is** (`TSession`).
- **The repo's own endings, each for one kind of thing:** `…Holes` a message's holes
  ([0119](0119-declare-name-then-pass.md)), `…Request` and `…Reply` a record on the wire, `…Handlers` an endpoint
  group's handlers, `…Options` settings read once, `…Tests` a test class; and an extension's holding class named for
  its idea, as above.
- **No type shares its namespace's name, a core .NET type's name, or a bare generic word:** a `Message` is qualified
  as what message it is.

**Both**

- **A touched file is always fixed,** as for [0123](0123-ask-the-type.md): every C# file in a step's commit leaves it
  with every method and type named by this rule, the parts the step didn't change too, a type renamed through Rider
  with its file. A file no step touches waits.

## Consequences

- A name tells a reader what kind of thing a method is before its body is read: a factory and what it's made from,
  an action, an attempt, a wait, an operator, an answer to an event. A type's name says what it is, and its ending
  what kind of thing it is.
- Some old names change as their files are touched, and a rename goes through Rider so its callers follow
  ([0052](0052-edit-never-script.md)). Most of the 251 `Of`s already fit; the rest wait for their file. The types
  ending in `…Info`, `…Services`, `…Data` and `…Base` take a name that says what they are when their files are next
  touched.
- The `Try…` rule parts from Microsoft's guideline on purpose, which a reader coming from .NET may not expect.
- It is held by review. No test holds it yet. `Async` on a method that returns a `Task` could be counted by a rule
  test later, as a ratchet.

## Links

[0003](0003-provider-strategies.md) · [0010](0010-folders-by-concern.md) ·
[0040](0040-records-for-values-classes-for-things.md) · [0052](0052-edit-never-script.md) ·
[0119](0119-declare-name-then-pass.md) · [0123](0123-ask-the-type.md) · Microsoft's design guidelines:
[Names of type members](https://learn.microsoft.com/dotnet/standard/design-guidelines/names-of-type-members),
[Names of classes, structs and interfaces](https://learn.microsoft.com/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces),
[Names of namespaces](https://learn.microsoft.com/dotnet/standard/design-guidelines/names-of-namespaces),
[Extension methods](https://learn.microsoft.com/dotnet/standard/design-guidelines/extension-methods),
[Exceptions and performance](https://learn.microsoft.com/dotnet/standard/design-guidelines/exceptions-and-performance),
[General naming conventions](https://learn.microsoft.com/dotnet/standard/design-guidelines/general-naming-conventions)
and [the Task-based asynchronous pattern](https://learn.microsoft.com/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)

---
[Docs index](../README.md) · [ADRs](README.md)
