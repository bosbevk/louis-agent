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
| Features (`F`) | 58 | 77 |
| Fixes (`FX`) | 55 | 4 |
| Cleanups (`CL`) | 27 | 26 |

A child (`F59.3`) counts as its own row. "Open" is the TODO's table, one row per item.

## Open (107 in the TODO)

| Id | What | Phase |
|---|---|---|
| F106 | The framework's track, before the agents | Now |
| F74 | Packaging the engine in place, then its own repo after the engine features | Now |
| F44 | The engine as a product | Now |
| F39 | Orchestration | Now |
| F43 | Accounts, tenants and durable sessions | Now |
| F62 | Validate demand | Now |
| F18 | F18's leftovers, before F49.1 | Now |
| F63 | The reference transcripts in git | Now |
| F64 | The judge check can't see pages | Now |
| F65 | After F35, the cook's answer | Now |
| F66 | Each pack declares what it needs | Now |
| F67 | A whole menu | Now |
| F68 | The cook scales wrongly | Now |
| F69 | The framework's `_Imports.razor` is about 90 lines | Now |
| F70 | Long comment lines in the eval pages | Now |
| F71 | At the next run, read the hand-off lines | Now |
| F72 | Open from the recipes evals | Now |
| F73 | Check the 47 new own-prices rows | Now |
| F54 | F54's leftovers | Now |
| F57 | The site tells the new structure | Now |
| F59 | The framework's web | Now |
| F48 | Recipe maths and a teaching cook | Next |
| F46 | Localisation | Next |
| F50 | Puls's local specialists | Next |
| F40 | The sales playground | Next |
| F7 | Cost you can see and stop | Next |
| FX4 | Append-only history | Next |
| F41 | Drafts and approvals | Next |
| F42 | Safety and audit | Next |
| F36 | MCP servers as packs | Next |
| F8 | Reliability | Next |
| F15 | Model limits and local models | Next |
| F58 | The two sites go live from this PC | Next |
| F61 | Accounts, tenants and the two sites in Azure | Next |
| F51 | Hosting for public traffic | Next |
| F75 | Puls local spec | Puls |
| F76 | Cook for the week | Puls |
| CL16 | Reusable roles | Puls |
| F77 | A domain stop-list for the browser | Puls |
| F78 | Sources as data | Puls |
| F79 | Places | Puls |
| F80 | Local businesses | Puls |
| F81 | Vision: `ScreenshotPage(url)` and images as input | Puls |
| F82 | Local lookups | Puls |
| F23 | Credits | Puls |
| F83 | The cook's unconfirmed prices | Puls |
| F84 | A shorter cook report | Puls |
| FX59 | binary attachments sent as files, not text | Puls |
| FX60 | a continued specialist's ledger rows name the turn that continued it | Puls |
| FX61 | A site whose host has no `ACCOUNTS_SQL_URL` still calls `/account/me` and `/account/providers`, gets `index.html` back and dies on the JSON (the starter's login and chat pages show Blazor's error bar); the client should take a host without accounts as "nobody signed in" (found in CL58) | Fixes |
| F21 | Louis's corrections to the Afrikaans | Puls |
| F28 | Team talk, the rest | Puls |
| F85 | A retailer's own prices, only with its permission or feed | Puls |
| F86 | Our own Nominatim, or a paid geocoder, before Puls is public | Puls |
| F87 | The open feeds have no key and no limit | Puls |
| F60 | Search by meaning | Puls, later |
| F88 | The cook's wave 3 | Puls, later |
| F89 | A Puls-owned set of a few hundred South African recipes | Puls, later |
| F90 | WhatsApp as a host | Puls, later |
| F91 | The eleven official languages, measured | Puls, later |
| F92 | Voice | Puls, later |
| F93 | The `similar` check, when an eval asks for closeness of meaning | Later |
| F45 | Knowledge and retrieval | Later |
| F94 | Searching PDFs, investigate much later | Later |
| F47 | The CLI and Rider's host on the agent API | Later |
| F9 | Long chats | Later |
| F10 | Settings per role and per session | Later |
| F95 | What's left of CL29 | Later |
| CL17 | what's left of the coding identity | Later |
| F96 | The browser's user agent names the user's town per visit | Later |
| F97 | A tool added mid-chat, after FX4 | Later |
| F38 | Tool search, when a measured need comes | Later |
| F98 | Planning and a self-check before answering, tried on a simulation first | Later |
| F99 | An error tracker behind `IErrorFeed`, after F39 | Later |
| F100 | A webhook trigger beside polling, after F39 | Later |
| F101 | Computer use, when a simulation needs it | Later |
| F102 | An Inspect AI log export for a run | Later |
| CL21 | Mutation testing with Stryker.NET | Later |
| F103 | Function skills in the Agent Skills format | Later |
| CL23 | Explore a Roslyn analyzer for the names rule | Later |
| F30 | F30's minions tested | Later |
| F104 | Build mode, a side note | Later |
| F105 | The Azure app behind a key | Later |
| CL34 | The work email out of git history | Cleanups |
| CL35 | `Web/Browsing/RedirectBodies` goes when a PuppeteerSharp release awaits its own redirect body ta… | Cleanups |
| CL36 | Design docs for the three projects that have none | Cleanups |
| CL37 | The orchestrator's `Program.cs` up to the rules | Cleanups |
| F16 | F16-S1: the oversized-results report, and the worst offenders paged | Cleanups |
| CL38 | `VerifyQuote`'s prices in the host's currency | Cleanups |
| CL39 | Remove CL14's last aliases now, with no deploy to wait for | Cleanups |
| CL40 | Typed peer bodies | Cleanups |
| CL41 | Choices still made by string, the LLM provider first | Cleanups |
| CL42 | The session request bodies | Cleanups |
| CL43 | An OpenAPI document for the agent API | Cleanups |
| CL44 | SearXNG on only on purpose | Cleanups |
| CL45 | SearXNG's results filtered to the site asked for | Cleanups |
| F31 | F31's review follow-ups | Cleanups |
| CL46 | Web tests | Cleanups |
| CL47 | The portal's old benchmark routes and words removed, after Phase 3 | Cleanups |
| CL48 | Remove the `/v1/admin/benchmarks/…` route aliases | Cleanups |
| CL49 | Remove the eval store's old setting names | Cleanups |
| CL50 | Remove `PULS_API`, the eval runner's old name for the API's address | Cleanups |
| CL52 | The rest of the size list | Cleanups |
| CL53 | A price format per country | Cleanups |
| CL54 | A pack can't add itself to a router | Cleanups |
| CL55 | Does an unanswered request belong in the usage ledger? A cancelled request is a record of 0 toke… | Cleanups |
| CL59 | The studio's partials hold rules nothing names any more (`.run-report`, `.run-transcript`, `.admin-cols`, `.eval-card`, `.explorer-q`, `.live-chat`, `.tree-node`, `.conv-row`, `.check-script`…), dead since CL10 by the look of them: confirm and delete (found in CL58) | Cleanups |

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

## Cleanups (27 done)

| Id | When | What |
|---|---|---|
| CL58 | 2026-11-07 | A component's styles sit beside it (ADR 0094): `X.razor.scss` scoped and bundled by Blazor's CSS isolation for the accounts, the admin, the studio (64 files) and the chat (21); the globals a site's own pages draw or a theme overrides stay partials; accounts/, web/admin/ and their forwards gone; the snapshot helper and the stylesheet helper know the scope and the bundle |
| CL31 | 2026-11-07 | The framework's scripts as ES modules beside their components, imported through JsModule (ADR 0093); chat.js and passkeys.js gone from every site's head; the download the framework owns |
| CL57 | 2026-11-07 | What a project holds (ADR 0092): the API off core.web, the runners' programs hand in services, Rider to the ACP server, the SDKs as core.anthropic/ollama/openai, the eval studio as api.evals and web.evals, the hosts lose -server, a starter API; CL51 with it |
| CL33 | 2026-11-07 | Core's Web/ and Telemetry/ as louis-agent.core.web and louis-agent.core.telemetry, on core behind a seam each; a host composes through HostServices |
| CL56 | 2026-11-07 | One roadmap: the platform plan and its summary became ROADMAP.md, the feature plans inside it drafts |
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
