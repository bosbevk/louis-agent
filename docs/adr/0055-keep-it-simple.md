# 0055. Keep it simple: the simplest elegant design that meets the step, nothing for a future that isn't planned

**Status:** Accepted · **Date:** 2026-10-30 (Louis, starting F49: "we have to keep things simple no over
engineering"; "always find a simple elegant solution") · **Recorded:** 2026-10-30

## Context

The rules that keep this code readable can each be pushed too far. Rule 11 asks for a named pattern behind every
design choice; taken as "add a pattern", it grows layers nobody asked for. Rule 10 asks for three options; the richest
option reads as the most thorough. An ADR per decision invites a decision per detail. And an evals redesign (F49)
is exactly the kind of work where a framework of our own could grow: a catalogue, a schema, a reader, a studio.

Louis said it while approving the F49.1 plan: a new eval must only be a JSON file, a check is C#, and we must keep
things simple, with no over-engineering. The field has names for this: *YAGNI* (Extreme Programming, Fowler: don't
build for a need you only suppose), the *rule of three* (abstract on the third case, not the first), and *simple
design* (Beck: passes the tests, says its intent, no duplication, the fewest parts, in that order).

## Decision

- **We build what the step's test needs, and no more.** No type, setting, interface, layer or option for a case that
  no plan names. An interface with one implementation and no second one planned is a class.
- **When two designs meet the step, the one with fewer parts wins**: fewer types, files, moving pieces and words.
  "Elegant" means a reader sees at once what it does and why; a clever shape that needs explaining loses.
- **A pattern names the shape we already need; it never adds one.** Rule 11's three options are judged by this rule
  first, and each card says which option is the simplest. The recommended option is the simplest that is not naive.
- **The bigger shape waits for its second case**, and then comes as its own refactoring step, named in a plan, with
  the case that forced it.
- **Data stays data, code stays code.** An eval is a JSON file and a check is C#; nothing of our own sits between
  them (no script engine, no expression language, no plugin loader) until a plan proves the need.

## Consequences

- Plans and cards get shorter: an option that builds for a supposed future is named as such and loses.
- Reviews flag a part nothing uses, a knob nobody turns, a base class with one child, a generic with one argument.
- Some things will be rebuilt when the second case arrives. That is the cheaper mistake: the rebuilt part is sized by a
  real case, and the one built on a guess rarely fits it.
- "Keep it simple" is never a reason to skip a rule that keeps the code safe or readable: tests first, summaries,
  flat conditionals, records for values. Simple means fewer parts, not fewer standards.

## Links

[0030](0030-build-or-adopt.md) (build only what nothing offers) · [0034](0034-code-a-person-can-maintain.md) ·
[0050](0050-summaries-like-a-person.md) · [F49.1 The checks catalogue](../learning/20-extensibility.md) · Fowler,
[Yagni](https://martinfowler.com/bliki/Yagni.html) · Kent Beck, *Extreme Programming Explained* (simple design) ·
[Refactoring.Guru: rule of three](https://refactoring.guru/refactoring/when)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
