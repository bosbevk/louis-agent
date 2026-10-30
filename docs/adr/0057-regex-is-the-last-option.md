# 0057. A regular expression is the last option

**Status:** Accepted · **Date:** 2026-10-30 (Louis, F49.1 step 8: "we should just ban the use of regex, difficult to
read, like only use when it is the last option") · **Recorded:** 2026-10-30

## Context

A regular expression packs a rule into a line nobody can read back: `(?<=R\s?)\d{1,3}(?:[ ,]\d{3})*(?:[.,]\d{2})?`
says "a rand amount" to the person who wrote it and nothing to the next. It can't be stepped through, its tests
show what it matched and never what it missed, and on some inputs .NET's default engine runs for seconds (ReDoS),
which matters wherever the text comes from outside: a model's answer, a web page, a file someone else wrote.
The repo has them in 71 files today, 40 of them source-generated. Dropping the `regex` check from the eval
catalogue (F49.1, step 8) raised the question for the code too: when is one allowed?

## Decision

- **A regular expression is the last option.** Before writing one, use what reads as words: `Contains`,
  `StartsWith`, `Split`, `IndexOf` with `StringComparison`, `char.IsDigit`, `decimal.TryParse`, a small
  hand-written reader that walks the text, or a typed parser (JSON, HTML, Markdown) where the text has a format.
- **When nothing plainer does it**, the expression is a `[GeneratedRegex]` on a method whose name says what it finds
  (`RandAmount()`), with a `///` summary that gives one text it matches and one it doesn't, and
  `RegexOptions.NonBacktracking` wherever the input comes from outside the program.
- **A regular expression is never a language in our content.** No eval file, setting, role file or tool argument
  carries a pattern; a check that needs a shape is a named C# check with a plain parameter.
- **Existing ones go as the code is touched**, never in a sweep (as ADR 0050's summaries). A step that edits a
  method with a regex replaces it when a plain form exists, and says so in the commit.

## Consequences

- Text rules read as prose and step through in the debugger; a reviewer can see what is missed, not only what hits.
- Some matchers get longer: a ten-line reader for a four-character pattern. That is the trade: longer and readable
  beats short and opaque (ADR 0055: fewer parts, not fewer lines).
- Reviews flag a new regex with no note on why nothing plainer worked.

## Links

[0055](0055-keep-it-simple.md) · [F49.1 The checks catalogue](../plans/F49.1-checks-catalogue.md) (step 8: no
`regex` check) · Microsoft, [Regular expression source generators](https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-source-generators)
and [NonBacktracking](https://devblogs.microsoft.com/dotnet/regular-expression-improvements-in-dotnet-7/#backtracking-and-regexoptions-nonbacktracking)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
