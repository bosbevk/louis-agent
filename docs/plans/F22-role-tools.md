# F22 · Role tools: plan

> Approved 2026-10-13 · Feature: [F22 Role tools](../features/F22-role-tools.md) · Branch: `feature/F20-puls-look`
> (built before F20 continues)

## Context

F21's roles each have a job; Louis approved all five additions found missing, F22 before F20's remaining steps, and set
the rule for this and every plan after it: **reuse before building**. Prefer the repo's components and maintained
third-party packages; write our own only when nothing fits.

## Design

Each tool sits in its toolset's class, is named in its guide, and is tested (*Adding a C# tool*). Text from the model
(a path, an author, a search term) reaches git as an argument list, never inside a command string.

| Piece | Where | Reuses |
|---|---|---|
| `GetToday` | always registered, like `ExecuteSkill` (Puls has only `agents`) | .NET `TimeProvider` and `TimeZoneInfo` (Africa/Johannesburg) |
| Attachments to the specialist | `StartSubSession(…, attachments)`: the turn's attachments by name, forwarded as the peer's attachments | the API's attachment handling; a per-turn context like `UsageScope` (our own: no third party does this) |
| `ReadDocument` (PDF, Word, Excel as text); `FetchUrl` reads PDFs | `files`; `web` | **PdfPig** (Apache-2.0), **DocumentFormat.OpenXml** (MIT), **ExcelDataReader** (MIT) |
| `Blame`, `FileHistory`, `SearchCommits` | `git` | the git CLI the toolset already runs |
| `ReadTable` (CSV, Excel to rows); `ConvertCurrency` | `files`; `web` | ExcelDataReader; core's `ExchangeRates` (F14) |

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; *Reuse before building* in docs/CLAUDE.md | — |
| 1 | `GetToday` | unit tests; golden file |
| 2 | Attachments through `StartSubSession` | unit and API tests; golden file |
| 3 | `ReadDocument`; `FetchUrl` reads PDFs | unit tests with small fixture files; golden file |
| 4 | `Blame`, `FileHistory`, `SearchCommits` | unit tests on a temp repo; golden file |
| 5 | `ReadTable`, `ConvertCurrency` | unit tests; golden file |
| 6 | Roles and guides: the analyst gets `python` + `files`; each role names its tools; docs | — |

## Verification

`dotnet test` for core, API and orchestrator per step; golden diffs reviewed; no model calls.

## Concepts

Tool design for agents (GOAL: tools and function calling): the few tools a role's job needs, with short descriptions
and bounded output; and reuse over invention, so the agent's capabilities rest on maintained libraries.

## Approved during the build

(None yet.)
