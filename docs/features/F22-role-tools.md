# F22 · Role tools: what each specialist needs to do its job

> **Status:** building (2026-10-13) · **Milestone:** — · **Depends on:** F21 (the specialists' roles), F5 (sub-sessions)
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

## What the build changed

(Filled in as the steps land.)

---
[Features](README.md) · [Plan](../plans/F22-role-tools.md)
