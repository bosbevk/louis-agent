# 0132. Words come from a dictionary factory

**Status:** Accepted · **Date:** 2026-11-22 · **Recorded:** 2026-11-22

![A page's words keyed by an enum of its parts, built once by a factory that refuses a part without words, read in the enum's order](diagrams/0132-words-come-from-a-dictionary-factory.svg)

## Context

A page of the framework's site held its words as lists of string literals in C#: *Build your own*'s steps were a
`new(title, text)` after another, in an `IReadOnlyList<Step>`. Nothing named a step, so a test or another page
couldn't point at one, and a missing or doubled step showed only on the page. Louis, of those lists: "put this in a
dictionary factory and make it a rule", then "can we not make it a frame work function? core framework".

Three ways to hold the words were weighed: an enum-keyed dictionary built by a factory, the words in a JSON file
beside the page read into that dictionary, and an enum member with a description per text, as the engine's
messages are (ADR 0009). And three places for the factory: `LouisAgent.Composition`, which every identity, pack,
host and site references; `LouisAgent.Choices`, the small enum project; and `LouisAgent.Core`, which the web doesn't
reference.

## Decision

A page's words are never a list of string literals in C#: each page's content is a factory that returns a
dictionary keyed by an enum of its parts, and a test holds that every key has its words.

- **The factory is the framework's**: `PartWords<TPart, TValue>` in `LouisAgent.Composition.Words`, so a page, a
  pack's texts or anything else with parts can use it. `PartWords<TPart, TValue>.Of(part => part switch { … })`
  asks the switch for each member of the enum once, when the class that holds the words loads.
- **Every key has its words, or it doesn't load.** A part the switch has no arm for, or nothing for, is refused,
  naming the enum and the part (`WordsErrorMessage.PartWordsMissing`). `PartWordsTests` holds it.
- **The enum is the page's parts**, one enum per list, in a `Parts/` folder beside the content
  (`Content/Build/Parts/IdentityStep.cs`). Its order is the page's order: `InOrder` hands a part of the framework
  that shows a list the words in that order.
- **A page converts when it is touched**, as the code's other rules do: a step that changes a page's content leaves
  it on a factory. *Build your own* is the first (CL68.1).

## Consequences

- A step, a fact or a row has a name in code, so a test, a link or another page can point at it, and a gap is a
  failure at start-up, not a hole on the page.
- The words stay in C#, beside the page, so they change in the same commit as the page and are read in review.
- Each converted list costs an enum and a switch arm per part, more lines than a list.
- Pages not yet touched keep their lists until a step changes them.

## Links

- CL68.1; [ADR 0125](0125-a-site-is-configured-framework-components.md), a site is configured framework components;
  [ADR 0009](0009-message-text-as-enum-templates.md), message text as enum templates
- Factory Method: <https://refactoring.guru/design-patterns/factory-method> · Registry:
  <https://martinfowler.com/eaaCatalog/registry.html>

---
[Docs index](../README.md) · [ADRs](README.md) · Related: [0125](0125-a-site-is-configured-framework-components.md) · [0009](0009-message-text-as-enum-templates.md)
