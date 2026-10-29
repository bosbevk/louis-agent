# 0050. Every class and method has a summary, written like a person; a step inside a method gets one plain line

**Status:** Accepted (Louis, 2026-10-27) · **Date:** 2026-10-27 (Louis, reviewing the summaries a step had added:
"write them like a person, not a robot"; the same day, on the steps inside a method and the blank line between
groups) · **Recorded:** 2026-10-29 (CL25, from the rule text in `docs/CLAUDE.md`)

## Context

Louis learns the code by reading it, and a class with no summary or a robot's one (*True when the browser address
is set and is an absolute URL*) makes him decode the code before he can follow it. The pattern a class carries
(rule 11, *Look up the pattern*) was named in the commit and the explanation and then lost, because nothing in the
code said which pattern it was or why it was chosen here. And a method whose steps run on without a break (a request
built, a `try` that hides a failure path, a trick such as `using var _`) reads as one block, so a reader can't tell
where one job ends and the next begins.

The options were a summary on public members only (the compiler's default with `GenerateDocumentationFile`), which
leaves the private and test members, where most of the reading happens, bare; summaries written as the code reads
(types, null, "returns"), which repeat what the signature already shows; or summaries written like a person, with
the pattern and the ADRs in their own parts, on every class and method.

## Decision

- **Every class and method has a `<summary>`**, private and test ones included: what it does for you, the way you'd
  tell a colleague at your desk, in one or two short sentences. Leave out what the code already shows: the types,
  null, "returns", "true when", every edge case. Step numbers belong in the commit and the feature doc.
- **A class (record, interface) has up to three parts:** the `<summary>`; a `<remarks>` when it carries a pattern,
  naming which one and **why it was chosen here** (the problem it solves for this class, and what we'd lose without
  it); and one `<seealso>` per ADR that shaped the class (not every ADR that applies), linked on GitHub as
  `ADR 0024: choices are enums`. A method has its `<summary>`, and a `<remarks>` when it carries a pattern of its own.
- **Inside a method, a step a reader can't follow at a glance gets one plain `//` line above it:** what it does and
  why, written like a person. Obvious lines (`_busy = true;`) stay bare, so the comments never drown the code.
  **Steps that belong together sit together, with one blank line between groups**, like paragraphs: each group does
  one job and carries its comment line on top when it needs one; a test's arrange, act and assert are three groups.
- **Scope:** existing code is brought up to this rule file by file, and only the files in the step's own commit
  (`git diff --name-only` of the commit): every class and method in those files gets its parts, the ones the step
  didn't change too. A file the step only read, or one outside its commit, waits; never a big refactor of its own.

The how, with the robot-against-person table and the `ChromeSetup` and `NotesPanel.AddAsync` examples, is in
`.claude/rules/csharp.md`, loaded when a `.cs` file is edited.

## Consequences

- A reader gets the what, the pattern and the why without leaving the file; a review checks the summaries as it
  checks the code.
- Comment lines don't count against the size limit ([ADR 0044](0044-the-size-limit-counts-code.md)), so a summary
  never forces a split.
- A step that touches a file pays for that file's missing summaries, which keeps each payment small and spread.
- The exception rule in [ADR 0009](0009-message-text-as-enum-templates.md) stays: the text a person reads at run time
  is a message enum, a summary is for the person reading the code.

## Links

[0034](0034-code-a-person-can-maintain.md) · [0044](0044-the-size-limit-counts-code.md) · `docs/CLAUDE.md`, rules 4
and 11 · [Google's style guides on comments](https://google.github.io/styleguide/) · Fowler, *Refactoring*, on comments
as a smell when they stand in for a name

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
