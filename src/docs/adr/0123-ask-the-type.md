# 0123. Ask the type: what is known about a thing lives on a named type, and code asks it

**Status:** Accepted · **Date:** 2026-11-14 (Louis, of the diagram engine: "with all these math in the egine, can
we not have something like a calculator which does it neatly"; then "CL 65.6 must be an ADR, we cannot write
spaghetti code", "and a csharp standard"; of the first wording, which bound only the engine's sums: "a rule for all
C# not just sums it is for everything"; and of its shape, "how about one adr and list in csharprules?") ·
**Recorded:** 2026-11-14

**It binds** all C# in the repo: the framework, the packs and every demo.

## Context

The diagram engine places boxes and words by arithmetic, and the arithmetic was written out wherever it was needed.
Measured on the day of the decision: 275 named numbers in 49 files, a box's middle or edge worked out inline in 56
places, words measured by their length in 19 places with 12 different widths for a character, and 20 path strings
built by hand.

No single line of it was wrong. The trouble is what it does to a change. A box's edge is known in 56 places, so a
change to what a box is has 56 places to be made in, and nothing says which were missed. And a reader of a layout
has to do the sums in their head to see what the method is for.

The sums are one case of a wider habit: code that needs something about a thing works it out on the spot, where it
could ask a type that knows. The habit has names. A bare number or string standing in for a small idea is Primitive
Obsession. A method that works on another type's data more than on its own is Feature Envy. One change that needs
many small edits in many classes is Shotgun Surgery.

Three ADRs each hold a part of keeping code readable, and none holds this. [0034](0034-code-a-person-can-maintain.md)
holds a file's size and one concern to a class. [0040](0040-records-for-values-classes-for-things.md) says a value is
a record. [0055](0055-keep-it-simple.md) says to build no more than a step needs.

## Decision

- **What is known about a thing lives on a named type for it, and code asks that type.** A box's edge, how wide
  words are in a face, a price's total, a name as it is written on the wire: each is a member of the type that owns
  it. Code that needs one calls it, and never works it out where it stands.
- **It holds for every kind of knowledge, not only sums:** a measure, a format, a parse, a default, a check of what
  is allowed.
- **A number with a meaning has a name,** as a constant on the type that owns the meaning. A plain 0 or 1, or a
  loop's step, needs none.
- **The type need not be the data's own record.** Where a decision keeps records plain data, as a diagram's records
  hold no layout ([0092](0092-a-project-holds-what-is-reused-together.md)), the knowledge lives on a type beside what
  uses it.
- **Behaviour kept apart on purpose is no breach.** A Strategy is picked so that an implementation can be swapped
  ([0003](0003-provider-strategies.md)), and its behaviour is meant to stand apart from the data it works on.
- **No type is built for knowledge that no code asks for.** [0055](0055-keep-it-simple.md) still holds.
- **How it is held.** At review, by one question: could a type answer this? The shapes that show no type was asked,
  each with its cure, are a list in `.claude/rules/csharp.md`. Where a breach can be counted, a rule test holds it as
  a ratchet, whose list can only shrink.
- **A touched file is always fixed** (Louis: "always fix touched files in csharprules", "for this"). Every C# file
  in a step's commit leaves it following this rule, the parts the step didn't change too. A file no step touches
  waits, or is fixed in a sweep that a plan names.

## Consequences

- A change to how something is worked out is made in one place, and a method reads as what it does, not as its sums.
- More small types, each of which needs a name. As in 0034, the name is where a missing idea shows.
- A step that touches an old file takes that file's fix with it, so some steps grow. A file that would grow a step
  past what can be reviewed is fixed in a step of its own first.
- Most of it is held by review. No test holds it for all C# today. The first comes with the diagram engine's
  geometry, which is the first code brought under the rule whole.
- Keeping what is known beside the data is not absolute. Refactoring.Guru's page on Feature Envy and Fowler's on
  Tell Don't Ask both say it sometimes gives way, to a pattern that swaps behaviour or to layering. The two bullets
  above on plain records and on a Strategy are those cases here.

## Links

[0003](0003-provider-strategies.md) · [0034](0034-code-a-person-can-maintain.md) ·
[0040](0040-records-for-values-classes-for-things.md) · [0055](0055-keep-it-simple.md) ·
[0092](0092-a-project-holds-what-is-reused-together.md) · Refactoring.Guru,
[Primitive Obsession](https://refactoring.guru/smells/primitive-obsession),
[Feature Envy](https://refactoring.guru/smells/feature-envy),
[Shotgun Surgery](https://refactoring.guru/smells/shotgun-surgery),
[Duplicate Code](https://refactoring.guru/smells/duplicate-code) and
[Replace Magic Number with Symbolic Constant](https://refactoring.guru/replace-magic-number-with-symbolic-constant) ·
Fowler, [Value Object](https://martinfowler.com/bliki/ValueObject.html) and
[Tell Don't Ask](https://martinfowler.com/bliki/TellDontAsk.html)

---
[Docs index](../README.md) · [ADRs](README.md)
