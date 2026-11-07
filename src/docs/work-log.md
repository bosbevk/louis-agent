# Work log

> **Reference.** The record of what was finished: every feature (`F`), fix (`FX`) and cleanup (`CL`), one row each,
> when and what, newest first, with the counts at the top and what is still open below them. **Elsewhere:** the why
> of each in the [ADRs](adr/README.md) and the design docs ([projects](projects/README.md)), what a fix broke and
> why in [fixes](fixes/README.md), what is next in [TODO.md](../../TODO.md). A feature, fix or cleanup that finishes
> adds its row here (*Finishing a feature*, *Finishing a fix*) and leaves the TODO; `WorkLogTests` holds the counts,
> that every fix in the fixes table has a row, and that the open list is the TODO's. The changelog this replaced
> (Louis, 2026-11-07: "we just need something simple as this") is in the history before this file.

## Counts

| Kind | Done | Open in the TODO |
|---|---|---|
| Features (`F`) | 58 | 12 |
| Fixes (`FX`) | 55 | 0 |
| Cleanups (`CL`) | 22 | 1 |

A child (`F59.3`) counts as its own row. "Open" counts the TODO's unticked lines that start with an id, so a parent
and its children each count once.

## Open (13 in the TODO)

| Id | What |
|---|---|
| CL31 | Scripts as ES modules beside their components |
| F18 | F18's leftovers, before F49.1 |
| F40 | The sales playground |
| F46 | Localisation |
| F48 | Recipe maths and a teaching cook |
| F50 | Puls's local specialists |
| F51 | Hosting for public traffic |
| F54 | F54's leftovers |
| F57 | The site tells the new structure |
| F58 | The two sites go live from this PC |
| F59 | The framework's web |
| F60 | Search by meaning |
| F61 | Accounts, tenants and the two sites in Azure |

## Features (58 done)

| Id | When | What |
|---|---|---|
| F61.5 | 2026-11-07 | Passkeys and a second factor |
| F61.3 | 2026-11-07 | Social logins |
| F61.2 | 2026-11-07 | The pages, and the gate takes a signed-in person |
| F61.1 | 2026-11-07 | The accounts library |
| F59.12 | 2026-11-07 | The coding agent's landing page |
| F59.11 | 2026-11-07 | Spawning a site |
| F59.10 | 2026-11-07 | The eval pages |
| F59.9 | 2026-11-07 | Uploads the same way |
| F59.8 | 2026-11-07 | Recipe management |
| F59.7 | 2026-11-07 | The code-review app on the frame |
| F59.6 | 2026-11-07 | The git and file controls |
| F59.5 | 2026-11-07 | Described screens |
| F59.4 | 2026-11-07 | Base styles and scripts |
| F59.3 | 2026-11-06 | The chat's frame |
| F59.2 | 2026-11-06 | The admin's frame |
| F59.1 | 2026-11-06 | The name and the rule |
| F57 | 2026-11-05 | The site tells the new structure |
| F56 | 2026-11-04 | The packs' feeds as MCP resources |
| F55 | 2026-11-04 | Stats SA's prices uploaded in the admin portal |
| F54 | 2026-11-04 | The engine's website |
| F53.3 | 2026-11-04 | The review queue, and F53 closed |
| F53.2 | 2026-11-03 | One Evals page |
| F53.1 | 2026-11-03 | The run page opens on its answer |
| F52 | 2026-11-04 | Observability through OpenTelemetry |
| F50.2 | 2026-11-04 | What a list couldn't price, and packs and identities off the evals |
| F49.5 | 2026-11-03 | Studio: compose, try, publish |
| F49.4 | 2026-11-03 | Studio: samples from chats and human review |
| F49.3 | 2026-10-31 | Checks in packs |
| F49.2 | 2026-10-30 | Evals as files |
| F49.1 | 2026-10-30 | The checks catalogue |
| F49 | 2026-10-31 | .2b The generic checks |
| F48.3 | 2026-11-06 | A recipe or a meal |
| F48.2 | 2026-11-06 | Yields |
| F48.1 | 2026-11-06 | `ScaleRecipe` |
| F35.8 | 2026-11-06 | The long recipes eval |
| F35.7 | 2026-11-06 | The library's counter |
| F35.5 | 2026-11-05 | The minion and the cook work together |
| F35.4 | 2026-11-05 | Many recipe sites |
| F35.3 | 2026-11-05 | The recipe library |
| F35 | 2026-11-07 | is done |
| F34 | 2026-10-18 | Shop deals |
| F33 | 2026-10-17 | Shop catalogues |
| F32 | 2026-10-17 | Self-hosted web |
| F31 | 2026-10-17 | Benchmarks in the admin portal |
| F29 | 2026-10-19 | Validated specialists |
| F26 | 2026-10-19 | Specialist follow-up |
| F25 | 2026-10-19 | Local cook |
| F24 | 2026-10-15 | Landing motion |
| F22 | 2026-10-19 | Role tools |
| F21 | 2026-10-19 | Personalities |
| F20 | 2026-10-14 | Puls look |
| F18 | 2026-10-29 | Evaluation |
| F14 | 2026-10-11 | Display currency |
| F5 | 2026-10-14 | follow-up, sub-session usage rolled up |
| F4 | 2026-10-11 | Prompt caching |
| F3 | 2026-10-11 | Show usage |
| F2 | 2026-10-10 | Prices and cost |
| F1 | 2026-10-10 | Usage ledger |

## Fixes (55 done)

| Id | When | What |
|---|---|---|
| FX58 | 2026-11-07 | The last update of a burst reaches the screen |
| FX57 | 2026-11-06 | A context lost by Chrome's library is no error |
| FX56 | 2026-11-06 | Our own rules for amounts |
| FX55 | 2026-11-06 | The country stays out of a search's list |
| FX54 | 2026-11-06 | A method told in numbered parts |
| FX53 | 2026-11-06 | A search names the dish, in a random order |
| FX52 | 2026-11-06 | A whole method in one paragraph |
| FX51 | 2026-11-06 | A cake is per cake |
| FX50 | 2026-11-06 | The reader sees real recipes |
| FX49 | 2026-11-06 | Nothing is pantry |
| FX48 | 2026-11-06 | The cook keeps looking until it has a recipe |
| FX47 | 2026-11-05 | Puls really stops asking which shop |
| FX46 | 2026-11-05 | A photo we may not check stays in the print: only a 403 or 404, no answer, no such server, or something that isn't an image drops one; a robots refusal or a server error keeps it, an internal address is still never shown, and a dropped photo is logged by host. Not confirmed on a full run |
| FX45 | 2026-11-05 | A paid judge that can tell |
| FX44 | 2026-11-05 | Ten shops near the user |
| FX43 | 2026-11-05 | Shops near the user |
| FX40 | 2026-11-04 | A recipe's headings aren't ingredients |
| FX39 | 2026-11-04 | `FindBranches` finds the town's own branch: it searches about 16 km around the town first and widens only when a box finds none |
| FX38 | 2026-11-04 | One CI run per change |
| FX37 | 2026-11-04 | A git past its deadline is always a timeout |
| FX36 | 2026-11-04 | A redirect's body is observed |
| FX35 | 2026-11-03 | The films play on a PC with animations switched off: the site's reduced-motion rule stills the decoration (the hero's typing) and leaves the stage out, so the two films play as a video would (Louis's pick of three, 2026-11-03) |
| FX34 | 2026-11-03 | A live run shows each turn's cost |
| FX33 | 2026-11-03 | The header test reads the same on every checkout |
| FX32 | 2026-11-03 | An edit in the studio keeps its eval's folder |
| FX31 | 2026-11-03 | The trend's change badge stays one pill |
| FX30 | 2026-11-03 | The delegation card's tile |
| FX29 | 2026-11-03 | A failed send says why |
| FX28 | 2026-10-29 | A replay is marked, and can be started, on the Benchmarks page |
| FX27 | 2026-10-29 | A sub-session's answer carries no usage |
| FX26 | 2026-10-29 | A replay miss is marked `replay-miss` |
| FX25 | 2026-10-27 | The eval runner sees only what it reads |
| FX24 | 2026-10-26 | The cook's recipe photo and its estimates: `recipe-image` v2, estimated rows count as priced, a fixed photo box, and the fetch reads JSON-LD and og:image first |
| FX23 | 2026-10-29 | A script that never reads its input keeps its exit code and output |
| FX22 | 2026-10-25 | The recipe's image renders |
| FX21 | 2026-10-24 | The browser says what it is |
| FX20 | 2026-10-19 | Skill files wait for approval |
| FX19 | 2026-10-19 | Git arguments as data |
| FX18 | 2026-10-19 | Auth fails closed |
| FX17 | 2026-10-18 | The cook's labels and counts |
| FX16 | 2026-10-17 | A price is checked against its product |
| FX15 | 2026-10-14 | A test needed `config/.env` |
| FX14 | 2026-10-14 | Sub-session prompt budget |
| FX13 | 2026-10-14 | Counting to the analyst |
| FX12 | 2026-10-14 | Env templates readable |
| FX11 | 2026-10-14 | Empty sub-session answer |
| FX10 | 2026-10-14 | Optional peer name |
| FX9 | 2026-10-14 | Cancelled requests in the ledger |
| FX8 | 2026-10-11 | A FIX-RESULT with quotes in its summary is still read (not answered with "continue") |
| FX7 | 2026-10-11 | A timed-out request no longer ends the orchestrator's run (only the user's stop does) |
| FX6 | 2026-10-11 | A turn that fails still reports what it used (usage on the `error` event) |
| FX5 | 2026-10-11 | Requests outside a usage scope are written to the ledger again |
| FX3 | 2026-10-10 | The Ollama container starts and pulls the profile's model |
| FX2 | 2026-10-10 | The usage probe says which model to pull on an Ollama 404 |
| FX1 | 2026-10-10 | Keep the tools on the request after the 10-round limit |

## Cleanups (22 done)

| Id | When | What |
|---|---|---|
| CL32 | 2026-11-07 | The packs as one area |
| CL30 | 2026-11-07 | The account client as its own library |
| CL28 | 2026-11-04 | The folders mirror the solution |
| CL27 | 2026-11-03 | Prompt headers |
| CL26 | 2026-10-29 | The eval card, reworked, and the stylesheets split |
| CL25 | 2026-10-29 | Every CLAUDE.md in 200 lines |
| CL24 | 2026-10-29 | Done docs folded |
| CL20 | 2026-10-25 | API standards |
| CL19 | 2026-10-24 | Config and Docker by place |
| CL15 | 2026-10-20 | Engine, identities and packs |
| CL14 | 2026-10-17 | The public word: role |
| CL13 | 2026-10-17 | Skills by role |
| CL10 | 2026-10-17 | Clear the decks |
| CL9 | 2026-10-17 | Modes per product |
| CL8 | 2026-10-16 | Skills by level |
| CL7 | 2026-10-14 | Docs tidy |
| CL6 | 2026-10-14 | arc42, ADRs and C4 |
| CL5 | 2026-10-14 | Guides by folder |
| CL4 | 2026-10-14 | One type per doc |
| CL3 | 2026-10-12 | Messages and logging |
| CL2 | 2026-10-12 | Split AgentEngine |
| CL1 | 2026-10-11 | Project layout |

## Other changes (no id)

| When | What |
|---|---|
| 2026-11-07 | Project diagrams |
| 2026-11-05 | Directions to the shop |
| 2026-11-05 | Photos that load |
| 2026-11-05 | The cook's answers, after the measure |
| 2026-11-05 | The cook searches for itself |
| 2026-11-05 | The calculator pack |
| 2026-11-05 | Puls shows the cook's report |
| 2026-11-05 | Core ships no agent |
| 2026-11-04 | The hosts are libraries |
| 2026-11-04 | The folders say what ships |
| 2026-11-04 | The Studio's numbered steps |
| 2026-11-04 | MCP_TRANSPORT is an enum |
| 2026-11-04 | Third-party notices, and ADR 0067 and 0068 in the rules list |
| 2026-11-03 | The cook's judge recalibrated |
| 2026-10-15 | Licence: all rights reserved |
| 2026-10-15 | Licence: AGPL-3.0 |
| 2026-10-14 | CI |
| 2026-10-14 | Test first |
| 2026-10-12 | Ollama on the GPU and local triage |

---
[Docs index](README.md) · Related: [CHANGELOG](../../CHANGELOG.md) · [Fixes](fixes/README.md) · [TODO](../../TODO.md)
