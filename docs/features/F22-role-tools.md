# F22 · Role tools: what each specialist needs to do its job

> **Status:** built (2026-10-13), on `feature/F20-puls-look` · **Milestone:** — · **Depends on:** F21 (the specialists' roles), F5 (sub-sessions)
>
> **In the TODO:** **F22 Role tools** under *Puls* ([TODO](../../TODO.md)) · **Plan:** [plans/F22](../plans/F22-role-tools.md)

F21 gave Puls's specialists roles: a researcher, a reader, a historian, an analyst and a general specialist. Looking at
what each has to do its job, some tools are missing: no agent knows today's date, the user's attachments never reach
the specialist, PDFs and Office documents can't be read, the history can't be searched or blamed, spreadsheets need a
hand-written parser, and money can't be converted.

## User stories

**F22-S1 — Today.** As *any agent*, I want today's date and time in South Africa, so that "what's new", "this week" and
"how long ago" aren't guesses.

**F22-S2 — Attachments reach the specialist.** As *Puls*, I want to pass the files the user attached to the specialist
I delegate to, so that "summarise this PDF" works.

**F22-S3 — Documents.** As *the reader*, I want PDFs, Word documents and spreadsheets as text; as *the researcher*, a
PDF on the web as text.

**F22-S4 — History.** As *the historian*, I want who wrote a line (blame), a file's history, and commits found by
text, author or date.

**F22-S5 — Tables and money.** As *the analyst*, I want a CSV or a spreadsheet as rows without writing a parser; as
*the researcher*, an amount converted between currencies at the day's rate.

## Reuse before building

Each piece reuses what exists: .NET's time zones, the API's attachment handling, PdfPig (PDF), DocumentFormat.OpenXml
(Word), ExcelDataReader (Excel and CSV), the git CLI, and core's exchange rates (F14). Only the plumbing that passes
attachments to a sub-session is our own: nothing third-party does that for us.

## Done when

- **Each tool works and is bounded:** `GetToday`, attachments by name, `ReadDocument` and PDFs in `FetchUrl`, `Blame`,
  `FileHistory`, `SearchCommits`, `ReadTable`, `ConvertCurrency`. *Met:* unit tests for each (documents built in the
  test, git on a real repository, a fake clock), suites pass.
- **Model text never becomes a git option:** *Met:* a test passes `--output=...` as a path and a search term.
- **The roles use their tools** in real delegations. *Met* in the [three-run delegation check](../../samples/sample-output-puls-check-repeat/README.md): the
  researcher called `GetToday` in all 6 web delegations, the historian `FileHistory`, the analyst `PythonRun` (the
  specialist's log). **Open:** the reader reading an attached PDF, which the check doesn't ask for.
- **The user's place:** the time zone with every new chat, the location only when set, at ~1 km. *Met* by tests and the
  live API (a 400 for an unknown time zone).

## What the build changed

- **Packages, chosen by Louis from three options each:** PdfPig (the package is `PdfPig`; `UglyToad.PdfPig` is only its
  namespace, and that ID holds an unrelated pre-release), DocumentFormat.OpenXml, ExcelDataReader; and for tests,
  Microsoft.Extensions.TimeProvider.Testing.
- **Step 2b, the user's place** (approved during the build): Ligging in Puls's settings, typed or from the browser via
  OpenStreetMap's Nominatim; the session carries it, the first message says it, `GetToday` uses the user's time zone,
  and specialists get it.
- **Attachments by name:** the model names a file and the session supplies it, so a file isn't copied through the model's
  output; the session remembers attachments across turns.
- **The new git tools use argument lists** (`ProcessRunner`); the older git tools still build one command string, a gap
  outside F22.
- **Roles name their tools**, and the analyst gets `python` with `files` when the data is the user's.
- **PR #28 review:** any failure reading a PDF in `FetchUrl` is `DocumentUnreadable` (PdfPig throws more than one kind);
  the browser's position goes to Nominatim at two decimals (~1 km, a town) instead of four (~11 m); the time zone is sent
  with every new chat (Louis's choice), the location still only when set.

---
[Features](README.md) · [Plan](../plans/F22-role-tools.md)
