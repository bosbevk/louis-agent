# F165 The trust page

> **Plan.** How louis-agent.dev gets a page at `/trust` that tells a business owner, in technical terms, what keeps
> their business safe and what isn't built yet. **Elsewhere:** the mockup at
> https://claude.ai/artifact/1vAJDidov2aPHNZ5MKj7f8, the privacy page it sits beside
> (`src/site/louis-agent.site/Pages/About/PrivacyPage.razor`), and the safety write-up it ends in
> ([15-safety-and-security](../learning/15-safety-and-security.md)).

## Context

A reviewer read the site the way a business owner would, and said the one thing he'd change at once is a dedicated
Security and Trust page: "Not marketing material. Actual technical information." He named four parts: the
infrastructure, data protection, AI safety and business continuity. He then listed what he would want before paying
customers come on: operator agreements under POPIA, tested tenant isolation, encryption in transit and at rest,
documented backups and recovery, retention and deletion rules, the model provider and other subprocessors named, an
incident and breach procedure, terms with an SLA, and an independent penetration test.

Louis, 2026-11-19, of the mockup: he picked "Status on every tile" of three (a tile per fact, each saying In place or
Not yet), then "build and merge", and of the reviewer's checklist, "then this finish first".

What was looked up: every claim on the mockup was read from the repo on `main` at 577734ef: the two apps' yaml in
`src/site/deploy/azure`, the Azure architecture page, the site's Dockerfile (no `USER` line), the Bicep file (no backup
policy), the privacy page, ADRs 0004 to 0007, 0064, 0068, 0088, 0095, 0097, 0101, and the CI workflow (unit and
integration tests, no evals). What could not be confirmed from the repo is said on the page as Not yet, or left off.

## Design

The page is configured framework components, like the privacy page (ADR 0125): a `Band` per part, each with its own
address (ADR 0107), `TileRow` and `Tile` for the facts, and the framework's `ComparisonTable` for who handles data for
us. One thing is missing from the framework: a tile can't say whether what it describes is in place. Step 1 adds a
status to `Tile`, a plain enum (`TileStatus`: none, done, open) and the words the site gives it, drawn as a small
badge in the theme's green (`--web-ok`) or amber (`--bs-warning`), both names every theme already sets. The words stay
the site's, so the framework names nothing.

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `TileStatus` | `src/web/louis-agent.web/Components/Site/Tiles` | `LouisAgent.Web.Components.Site.Tiles` | 1 |
| `Tile` (`Status`, `StatusText`) | the same | the same | 1 |
| `TileStatusTests` | `src/tests/web/louis-agent.web.tests/Components/Site/Tiles` | its mirror | 1 |
| `TrustPage` | `src/site/louis-agent.site/Pages/About` | `LouisAgent.Site.Pages.About` | 2 |
| `TrustPageTests` | `src/tests/site/louis-agent.site.tests/Pages/About` | its mirror | 2 |

Reused: `Band`, `TileRow`, `Tile`, `ComparisonTable`, `LinkButton`, `PageHead`, the footer's links. No package.

There are no new interfaces, hosts or flows, so this plan draws no pictures: the page is one Razor file on controls that
exist, and one parameter on a control.

## Steps

1. **A status on a tile.** `Tile` takes `Status` and `StatusText`, and draws the words as a badge above its title.
   Test first: a done tile and an open tile each draw their words with their class, and a tile without one draws none.
2. **The page at `/trust`.** Its opening, four bands (where it runs, your data, the AI's limits, if something breaks)
   and a fifth for the reviewer's checklist before paying customers, every tile with its status. Test first: the page
   answers at `/trust` with its heading and title, each band has its address, and every tile carries a status.
3. **The way to it.** The footer links Trust beside Privacy, and the guide's site map names the page. Test first: the
   footer holds the link.
4. **The docs.** The site's and the web's design docs, the write-up's section, the work log's row, the plan folded.

## Verification

The tests of each step; the site's and the web's test projects; the full unit suite after `main` is merged in. By eye:
the page at 1920 and 390 wide, light and dark.

## Concepts

Skill 15 (safety and security): a trust page is a promise the code must keep, so every line is held to what the code
does, and a gap is named rather than hidden. Skill 16 (human in the loop) shows up as the approvals the page lists.

## Approved during the build

Nothing yet.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [TODO](../../../TODO.md)
