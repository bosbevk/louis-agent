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

- **2026-10-13, ask before any package** (Louis: "when choosing a package ask me first as a rule give me three options
  pros and cons"): every package is chosen by Louis from three options with pros and cons. Step 1's test clock:
  Microsoft.Extensions.TimeProvider.Testing (chosen over a hand-made fake and NodaTime.Testing). Step 3's document
  packages get their three options before they're added.
- **2026-10-13, Puls knows the user's location** (Louis: "I would also like puls to know my location"; chose a setting
  with a 'use my location' button): a new step **2b** after the attachments. Instellings gets *Ligging*, typed or filled
  from the browser's location (asked once; reverse geocoding by a service chosen from three options); the session
  carries it (`POST /sessions` with the user's location and time zone), Puls's prompt says where the user is, and
  `GetToday` uses the user's time zone. Nothing leaves the browser unless the user sets it.
- **2026-10-13, the time zone always** (PR #28 review; Louis chose "Yes, always send it"): the browser's time zone goes with
  every new chat, so `GetToday` is in the user's time without a location; it names a region, not a place. The location
  still goes only when set, now at two decimals (~1 km).
