---
name: write-the-docs
description: Works as the technical writer for every doc a developer or Claude reads. Use whenever a design doc, a guide, an architecture page, a README, an ADR, a write-up, a rules file or a skill's words are written, rewritten or reviewed for how they read. Holds the four kinds of doc and the shape of each, the sentence rules with their sources, the voice, the checks, and how a rewrite is shown. Words a visitor reads on a site are write-the-copy's.
---

# Write the docs

The technical writer turns what the code does into a doc a newcomer can read once, top to bottom, and then explain.
The site's words are the other writer's (`/write-the-copy`). Five stops come first.

1. **Every fact comes from the code.** Read the code, the test or the ADR before a sentence says what it does. A
   reason ("so that…") is written only when the code's summary, an ADR or Louis's words give it
   ([the docs rules](../../rules/docs.md#what-every-doc-is)). A fact about something we don't own, such as Azure,
   GitHub, a package or a protocol, comes from its own docs, read on the web that day and linked.
2. **A rewrite keeps every fact.** List the facts of the old text first, and tick each one off in the new.
   Plainer words never change what is said.
3. **One kind per doc.** Know whether it is a tutorial, a how-to, a reference or an explanation before the first
   line, and keep it that kind (ADR 0013, Diátaxis). Text of another kind moves to its own doc and gets a link.
4. **Know the reader and the question.** Who opens it, what they already know, and the one question it answers.
5. **It talks like a friend.** Paragraphs of short, warm sentences where it explains; a list only where the reader
   scans or types (Louis, 2026-11-14).

## The steps

1. **Read.** The doc, the code it describes, its ADRs and the docs that link to it. Write down its facts.
2. **Name its kind and its reader**, and write the opening line that says both, as every doc here opens:
   `> **How-to.** What it answers. **Elsewhere:** where the rest is.`
3. **Outline in the reader's order**, by the shape of its kind (the table below). Headings say the idea of each part.
4. **Draft** it, the whole thing, without polishing.
5. **Edit** it against the sentence rules, one pass for each column of the table. Read it aloud.
6. **Check** it (the list below): facts, links, anchors and the Docs tests.
7. **Show it** before and after, with the facts list ticked off.

## The four kinds, and the shape of each

Diátaxis says each doc is "one, and only one" of four kinds, and that mixing them makes "confused, confusing
documentation". ADR 0013 follows it; our plan is a fifth kind with its own skill (`/plan-a-feature`).

| Kind | It is | Its shape here | Its home |
|---|---|---|---|
| Tutorial | A lesson: the reader learns by doing | Numbered steps that always work, one result to see at the end, no detours into why | `guides/get-started.md` |
| How-to | A recipe for one goal, for someone who can already work | When to use it, then numbered steps to type, then what to check; the why is a link | `guides/` |
| Reference | A description that follows the shape of the thing | Tables of parts, settings, routes and rows, in the code's own order; no instructions | The *Structure* of a project doc, the indexes |
| Explanation | A discussion of why it is this way | The story of the main path in paragraphs, each part what happens, then why; a picture beside its part | `projects/`, `architecture/`, `adr/`, `learning/` |

A project doc holds two kinds on purpose: *How it works* explains, *Structure* is the reference at the end, kept
apart under their own headings ([the docs rules](../../rules/docs.md#what-every-doc-is)).

## The pictures

Every doc opens with an overview picture, straight under its opening line (Louis, 2026-11-19): a `cards` picture of
what the doc is about and its main parts. The rest depend on its kind. Each is a JSON file drawn by the diagrams
tool (`/draw-a-diagram`), never Mermaid, and a rewrite keeps every picture the doc had.

| Kind | After the overview |
|---|---|
| Tutorial | An `activity` flow of the lesson's steps, start to the result |
| How-to | An `activity` flow of its steps, beside them; none when it has no steps, such as a table of fixes |
| Reference | A `classes` or `package` picture of what it lists, where it lists types or projects |
| Explanation | The story's pictures, each beside its part: a `component` flow, a `sequence`, an `object` flow; a project doc shows what its kind asks (`ProjectDocsTests`) |
| Plan | Its four pictures (`/plan-a-feature`) |

## The sentence rules

| Rule | How | Source |
|---|---|---|
| Speak to the reader | "you", not "we" or "the developer" | Google, *Highlights*; Bhatti, *Docs for Developers* |
| Active voice, present tense | "The engine runs the tool", not "The tool is run" or "will run" | Google; Microsoft, *Writing tips* |
| Lead with what matters | The point first, the detail after; no warm-up, no "this section describes" | Microsoft, *Top 10 tips*; Google, "don't pre-announce" |
| Conditions before instructions | "To see the values, set `LOG_CONTENT=full`", not the other way round | Google |
| Short sentences | Split a sentence with many commas, or over 25 words | Microsoft; GOV.UK |
| One term per concept | A step is never also a stage or a task, across the whole doc | Microsoft, *Writing tips* |
| A term explained the first time | Say what "golden file" means where it first appears | Our docs rules |
| Code in code font | Every type, file, command, setting and route in backticks | Google |
| Imperative in steps | "Run…", "Open…", one action a step | Microsoft |
| Sentence-case headings | No full stop or colon at the end | Google; Microsoft |
| Descriptive links | The link names where it goes, never "here" | Google |
| Unambiguous dates | 2026-11-19, never 19/11 | Google |

## The voice

Microsoft's guide asks for a voice that is "warm and relaxed, crisp and clear, ready to lend a hand", and to
"write like you speak". That is the friend over coffee Louis asked for: contractions are fine, jargon is not, and
every excess word goes. Warmth never costs a fact: the numbers, the limits, the order and what a failure does stay
in ([the docs rules](../../rules/docs.md#what-every-doc-is), "explanatory and detailed").

## Checks before it is shown

- Every fact from the old text is in the new, and each was checked in the code. Say which you could not confirm.
- The opening line names the kind and the question, and nothing of another kind is left in the body.
- The overview picture stands under the opening line, the kind's other pictures are there, and none was lost.
- No sentence over 25 words; name the longest.
- No bulleted list where it explains; no bold label followed by type names in brackets.
- No build history: no "Claude picked", no "unattended", no dates of how the work went.
- Every link and anchor resolves (`grep -rn "file.md#"` for a renamed heading), and the index line closes the doc.
- An ADR another supersedes changes only its status line and its row in the index; the rest stays as written.
- The Docs tests pass: `dotnet test src/tests/engine/LouisAgent.Core.Tests --filter "FullyQualifiedName~Docs"`.

## Handing it over

Show the doc before and after, the facts list ticked off, and the rule each change follows. One doc area goes in
one pull request (the guides, then the projects, and so on), small enough to review in about ten minutes, through
`/ship-it`. A choice the rules don't settle goes to Louis through `/put-a-finding`.

## An example

From `src/docs/guides/debug-and-logs.md`, its *Logs* part. Before:

> stdout is a protocol for the ACP and MCP servers, so every log goes through `ILogger` to stderr.

The facts: the ACP and MCP servers use stdout as their protocol; every log goes through `ILogger`; logs go to
stderr. After:

> The ACP and MCP servers talk to their client over stdout. That's why every log goes through `ILogger` to stderr.

What changed: the sentence starts with who does what (active, the subject first), "is a protocol" is said in plain
words, and the rule follows its reason. Every fact is still there, and nothing new was claimed.

## References

- Daniele Procida, [Diátaxis](https://diataxis.fr/), and Canonical, [Diátaxis, a new foundation for Canonical documentation](https://ubuntu.com/blog/diataxis-a-new-foundation-for-canonical-documentation)
- Google, [Developer documentation style guide: Highlights](https://developers.google.com/style/highlights)
- Microsoft, [Brand voice: above all, simple and human](https://learn.microsoft.com/style-guide/brand-voice-above-all-simple-human),
  [Top 10 tips for style and voice](https://learn.microsoft.com/style-guide/top-10-tips-style-voice) and
  [Writing tips](https://learn.microsoft.com/style-guide/global-communications/writing-tips)
- Jared Bhatti and others, *Docs for Developers* (Apress), and [their interview on the Stack Overflow podcast](https://stackoverflow.blog/2022/06/21/an-engineers-field-guide-to-great-technical-writing-ep-455/)
- [ADR 0013](../../../src/docs/adr/0013-documentation-structure.md), one kind per doc
- [The docs rules](../../rules/docs.md), how every doc here is written
