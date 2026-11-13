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
| Features (`F`) | 104 | 92 |
| Fixes (`FX`) | 138 | 6 |
| Cleanups (`CL`) | 32 | 28 |

A child (`F59.3`) counts as its own row. "Open" is the TODO's table, one row per item.

## Open (126 in the TODO)

| Id | What | Phase |
|---|---|---|
| F146 | A business's agent reports back by address: its `window` role tells the guide everything with `ReportBack`; the visitor still takes over; its own plan | Now, after F144 |
| F147 | Page squares under the guide's choices: three grouped "go to" squares the guide picks; its own plan | Now, after F146 |
| F145 | Who a talk was with: the visitor's IP on the overview, a privacy page, a retention rule, a consent notice if POPIA needs one; its own plan | Now, after F147 |
| F139 | A site draws its own cards: a card, a receipt and a form slot, each site its own SVG; the form turns Sending then Sent in place; its own plan | Now, after F127 |
| F140 | A host names its parts with one builder: `AddLouisAgent(catalog, agent => …)` and `UseLouisAgent()`, the middleware in its safe order; its own plan | Now, after F139 |
| F141 | Every part of the framework's web ready for an agent: the account pages, the admin and the studio name their focus and selectable pieces; an admin helper; its own plan | Now, after F127 |
| F138 | The guide explains with drawings: a tool gives the model a docs diagram's JSON and the widget draws it in the chat with `louis-agent.diagrams`; its own plan, after F127.4 | Now, after F127.4 |
| F132 | Found for the searches business owners make (Louis, 2026-11-10, of a search for "ai agents for my website": "is our seo geared for a search like this?"; his pick of three): a page for "AI agent for your website" (South Africa, from R500, what it does, an FAQ with FAQPage and Service data), the three demos titled for their business searches ("AI agent for a guesthouse website"), and "AI" in the landing's title; the long and local searches first, since the head term is ads and big brands; its own plan | Next, after F127.2 |
| F130 | The subdomains, together (Louis, 2026-11-10: "demos. and framework. together, with a redesign of the framework website"), a parent with three children: F130.1 the host by name (one app answering several host names: DNS and certificates, a page per host, canonical links, sitemap and robots per host, old addresses redirected for good; about 4 steps), F130.2 `demos.louis-agent.dev` (F127.1's showroom at its root, the guide's page index and the links moved; about 3 steps), F130.3 `framework.louis-agent.dev` redesigned (what the framework is, how it works and how to build on it, for developers, while `louis-agent.dev` sells; the framework's sections move there; research, mockups and Louis's picks first); each child its own plan | Next, after F127.1 |
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
| F125 | An agentic site: docs the standard way and public over MCP, buttons and cards in the guide, gaps, live demos, tools over MCP and discovery | Now |
| F126 | Evals, redesigned: one `.eval.json` of conversations with turns, six facts in code, criteria a judge marks, flips to correct it, its own eval host, six screens; today's evals deleted, not ported | Now |
| F131 | The chat on any website: one script tag opens our widget in a frame from louis-agent.dev on WordPress, static HTML or any site, nothing lost; a door per site, sites per tenant (after F43), a monthly rand budget (after F7), our own WordPress plugin, an answer that survives a page change; seven children | Now |
| F137 | The site's card content as JSON: the demo cards, the pricing, what it does and the custom-agent steps read from JSON beside their pages and drawn by one card component, so an agent that edits the site changes data, not markup; groundwork for F133, after CL60 | Next |
| F133 | A website with an agent, set up by talking to it (Louis, 2026-11-11: "How about I build a wix with my platform"): not a Wix, but one record per business that draws both its site and its agent, three templates (guesthouse, restaurant, practice), a setup pack that writes the record as the owner talks, preview then checkout then live on its own host name; builds on F131, F43, F130.1 and F7; seven proposed children, four open choices (the record's shape before F131.1 starts) | Next |
| F134 | The showroom looks modern (Louis, 2026-11-11: "this looks off and those icons", "everything is a bit too flat"; his picks: look B, showcase with photos, and Tabler Icons): line icons instead of emoji, one raised surface, a rail with room, a photo on each business's cover, the index in a centred column under a photo hero; five steps | Next |
| F142 | Louis's CV as a page at `/cv`: `cv.json` in the JSON Resume shape, records and a `ResumeView` in the framework's web, the nav's CV mark, Save as PDF, no private details; six steps | Next |
| F143 | The guide drives Louis's CV: a resume pack with `GetResume` over the same `cv.json`, `/cv` in the place index, a hiring focus skill, an eval of eight conversations; after F142 | Next, after F142 |
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
| F107 | A guest is sent to sign in before the chat: no composer for nobody (Louis, 2026-11-08, on mypuls.co.za: "I need to log in first"); today the gate answers 401 and the page sends them then | Next |
| F108 | The admin portal is a role of a signed-in person, not a key: ADMIN_API_KEY goes once an account can carry the admin role (Louis, 2026-11-08: "should this not be a role?") | Next |
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
| FX66 | Both Azure apps keep their data-protection keys in the container, so every cold start signs everyone out; the keys go to each app's share | Now |
| FX83 | The paymo and devops packs' async tools take no CancellationToken; the coding agent no longer loads them, so no catalogue's check reaches them | Next |
| F21 | Louis's corrections to the Afrikaans | Puls |
| F28 | Team talk, the rest | Puls |
| F135 | Puls on `AskAgent`, measured when Puls resumes: the recipes eval before and after, and its replays recorded again (ADR 0117) | Puls |
| F85 | A retailer's own prices, only with its permission or feed | Puls |
| F86 | Our own Nominatim, or a paid geocoder, before Puls is public | Puls |
| F87 | The open feeds have no key and no limit | Puls |
| F60 | Search by meaning | Puls, later |
| F88 | The cook's wave 3 | Puls, later |
| F89 | A Puls-owned set of a few hundred South African recipes | Puls, later |
| F90 | WhatsApp as a host | Puls, later |
| F91 | The eleven official languages, measured | Puls, later |
| F92 | Voice: the person talks, our API turns it into text behind the chat's seam, the text lands in the box to check (Louis's pick B, 2026-11-11) | Puls, later |
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
| F136 | How much of an asked agent's work the asker reads, as a framework choice: its tool calls, or those and its thinking, beside the answer; when a framework user needs it | Later |
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
| CL62 | A site's own words win only when registered before `AddSiteAreas`: a `Words(…)` builder method so order never matters | Cleanups |
| CL61 | `ToolCallGuards` past 200 lines once laid out: split it, holes into records (ADR 0119), and end the turn through `TurnEnding.Ends` | Cleanups |
| CL59 | The studio's partials hold rules nothing names any more (`.run-report`, `.run-transcript`, `.admin-cols`, `.eval-card`, `.explorer-q`, `.live-chat`, `.tree-node`, `.conv-row`, `.check-script`…), dead since CL10 by the look of them: confirm and delete (found in CL58) | Cleanups |

## Features (101 done)

| Id | When | What |
|---|---|---|
| F144 | 2026-11-13 | The admin is a frame its areas plug into (Louis's picks: a home of cards; a packs page; the cards report a refused key): `.Admin(layout)` the frame and its home at `/admin`, a card per area in the order named, Home first in the sidebar; `.Overview(counts)`, `.Packs(views)` (with `/admin/packs`) and `.Evals()` (moved to `/admin/evals`) each an area with its pages, links and card; `AdminCardBase` and `AdminAccess` ask for the key once; an admin area without the frame refused at start-up (`WebErrorMessage`, the web's first message enum); the sidebar's brand keeps its mark beside its name |
| F127 | 2026-11-13 | A site's areas in the framework, the parent: its eight children built (F127.1 the showroom, F127.2 the demos tried live, F127.3 one address for every agent call, F127.4 the guide mentors, F127.5 the minions on the address, F127.6 a site names its areas, F127.7 one guide with a focus per part, F127.8 the site's admin); ADRs 0115 to 0117, 0120 and 0121 |
| F127.8 | 2026-11-13 | louis-agent.dev's admin, an overview first (Louis's pick of three designs): a line per turn beside the ledger (`turns-{yyyy-MM}.jsonl`: the page, the part, who answered, the tools, no words), `GET /v1/admin/overview` joining it with the ledger (today and the month, by part and by who answered, the latest talks; an unpriced cost unknown), the tools a site counts named on its admin (`SendLead` as leads), the framework's overview page first in every admin, and the site's own admin layout behind `ADMIN_API_KEY`, which falls back to `AGENT_API_KEY`; `SessionHandlers`' holes became records first |
| F127.7 | 2026-11-12 | One guide, a focus per part, ready for an agent (ADR 0121, amends 0114): a site names each page's part with `Focuses(…)` (louis-agent.dev: selling, demos for `/demos` and every demo, framework), sent with every message and said in the page block, and the guide reads that part's skill (`focus-selling`, which guides to the demos and hooks honestly, `focus-demos`, `focus-framework`); tiles and cards are pieces with their own addresses, a click asks the guide about one, a card opened or a piece the guide shows is selected quietly, drawn pressed with a chip, and carried with every message; the number tiles take colours; the API builds an index of every section, card and piece from its own rendered pages (`SitePlaces`, `IPackContext.Places`) and the site pack's `FindPlace` searches it, so the guide shows a piece on any page without a hand-kept map; `ShowPage` takes a piece's digit-first address |
| F127.6 | 2026-11-12 | A site names its areas (ADR 0120): `AddSiteAreas` and `SiteAreasBuilder` in the framework's web register the chat's plumbing for every site and each area a site names, `Chat()`, `Admin(layout, views)`, `Showroom(demos)`, and a library's own through `Add` (`Evals()`, `Accounts(layout)`), a name twice counting once; `SiteAreas` gives the router its pages and `AdminShell` each area's links; code-review, the starter, Puls and louis-agent.dev each name theirs in one Composition Root (`CodeReviewAreas`, `StarterAreas`, `PulsAreas`, `LouisAgentDevAreas`) that their tests share; Puls's hand-drawn eval links gone; `SitesNameTheirAreasTests` holds every site to it; with it, no empty frame where a card's answer was (FX142's follow-up) |
| F127.5 | 2026-11-12 | Minions on the address, on `AskAgent` (ADR 0117, Louis's correction): `AskAgent(address, message, tool, stronger)` asks a minion (`puls.find-recipe`, listed in the identity's `Calls`, refused with a server, its model from `MINION_LLMS`, 16 a turn, never kept); `StartMinion` and the `minions` toolset deleted, its guide folded into the agents guide; everyone may ask whom their `Calls` allow but a minion, a session's level its role's folder below the top; no agent twice in a chain of asks (`TurnChain`, the API too); `AGENT_MAX_DEPTH` (default 4); the web's card reads a minion's ask, the dead follow-up chip deleted; a mode's prompt lists only the agents among its `Calls`; Puls lists its two minions; no live run (a local model, Puls paused) |
| F127.3 | 2026-11-12 | One address for every agent call, the parent: F127.3.1 (this instance) and F127.3.2 (another server), both below (ADR 0117) |
| F127.3.2 | 2026-11-12 | Ask an agent on another server (ADR 0117): an address may name its server (`partner:shop.cook`), `self` this instance among several; `AGENT_PEER_KEYS` a key per server on every request to it; a server's `GET /v1/identities` read with its key and kept ten minutes before an address on it is trusted; an unknown server, a refused key, a server that doesn't answer and an identity it doesn't list each a reason; the agents guide says to tell the person first; two API hosts in one test prove it, no model cost; the privacy line waits for a site agent that asks another server |
| F127.4 | 2026-11-12 | The guide mentors, the parent: F127.4.1 (cards the agent draws), F127.4.2 (the guided live demo) and F127.4.3 (a form the agent draws), all below |
| F127.4.3 | 2026-11-12 | A form the agent draws: the site pack's `AskFor(question, fields)`, one to four fields of a label and a kind (Name, Phone, Email, Text, LongText), checked by `FormCheck` and ending the turn; core's `TurnEnding`, so an answer ended by a form owes no closing choices; the web's `AskForCall`, `FormCard` (a box per kind, Send once every box is filled), `FormAnswer` (the form as one message, each field by its label, read back to fold the form) and `FormAlone` (a form is the only choice drawn); the field and chat-card looks shared as mixins with `ScreenForm` and the Offer cards; the three businesses and the guide's lead and job flows ask with one form, never a choice that only says to type; ADR 0119 (no anonymous types or `dynamic`, message holes as `[Holes]` records, declared then passed) with its check; live on the PC at desktop in Seemeeu's window and the guide's corner, R0.02 for the guide's form |
| F127.4.2 | 2026-11-12 | The guided live demo (ADR 0117): the did line tells each call's result; `GET /v1/sessions/{id}/work` serves a session's steps and `ReadAgentWork` reads the whole session from its server, a visitor's turns in a window included; the open door counts a session an admitted session asks for as that visitor's and admits it for them; the web learns an asked agent's session, `AgentWindows` hands a guide's ask to the business's window, which shows the talk as it streams and carries on in that session, the guide's chat saying so in one line and folding on a phone; the guide's `live-demo` skill offers it with `Offer` and asks in the visitor's words; live on the PC at desktop and 390 px, $0.0284 for the day's checks |
| F127.4.1 | 2026-11-12 | Cards the agent draws: a `ticket` kind in `louis-agent.diagrams` (a band with a kicker, a title and a status pill, a torn edge, rows of an icon, a label and a value, a note; 320 wide; colours `--ticket-*` variables a business's window sets), `TicketCard` from a call's parts, six new Tabler icons; the site pack's `ShowCard` and `Offer` (two to four choices, one suggested), checked by a tested duplicate of the library's rules, a refusal an error; the widget draws both with the work hidden (`DrawnCard` inline through `RenderForPage`, its styles in `@scope`; `OfferCards` as buttons), under the answer's words, a click sent as the visitor's message and the cards folded once answered; the three showroom businesses book through a ticket and *Book it*, the guide offers choices and every walkthrough stop ends in three cards; live on the PC at desktop and 390 px, $0.0148 |
| F127.3.1 | 2026-11-11 | Ask an agent by its address on this instance (ADR 0117, amends 0079): the session wire as shared records in `louis-agent.session-stream`; `AgentAddress` (`acme`, `acme.cook`) resolved against the identities' files; an identity's `Calls` checked in code and listed in its prompt; a role's level from its file's folder, so a business asked at depth 1 answers in its own voice; `AskAgent` starts or carries on one conversation per address (a busy one says wait, a gone one starts over), its answer under a line built from its own tool calls, `ReadAgentWork` for its steps; the triage team and Puls ask by address, Puls relaying answers whole (`RelaysAnswersWhole`); `StartSubSession`, `ContinueSubSession`, the `specialists` toolset and `SHOW_SPECIALIST_REPORTS` deleted; two live guide runs, $0.0053 |
| F127.2 | 2026-11-10 | The demos as working websites, tried live (ADR 0116): each business's mock website has three pages that register with it (`MockPage`, `MockPaths`), a menu a visitor clicks through, Wills and estates opened from its card, and a page change that loads like a browser; the agents' voices know their pages and the conversations were recorded again, each moving between pages and sections; Try it yourself swaps the recording for `MockLiveChat`, the business's agent live in the same corner, reading the mock's page, with a tap-to-ask hint, a Live tag, cues that wait and no usage line; `OPEN_IDENTITIES` opens the guide and the three businesses under one cap, the session narrowed to the identity admitted; a demonstration's lead marked `[demo]` by its identity (`IsDemonstration`, `TurnIdentity`); the three brands reworked (mockup 0) with self-hosted OFL faces, `MockHeader` and a label colour; minimise and maximise on both chats; the guide's demo help became F127.3 to F127.5 (ADR 0117) |
| F127.1 | 2026-11-10 | The demo showroom: a site's demos in an area of the framework (ADR 0115), a rail of every demo grouped by what a visitor comes for, a stage per demo in its colour whose parts each have an address, `/demos` its start featuring the guesthouse; a business's agent plays on a mock of its own website (sample brand, three looks, real Pexels photos, a real menu), held to its agent's facts by a test, and shows the website with the guide's own `ShowPage` (the guide's spotlight on a section, a press on a card), told as the recording plays; cues a visitor clicks to play from a turn; notes of what a custom agent could add; the guesthouse in the doctor's place and leading; the coding agent and the triage team on the stage; a place's address loaded afresh draws in its page's layout (`PlaceLayout`) |
| F129 | 2026-11-10 | An identity's skills, and `prompts/`: every `Prompts` folder is `prompts`, named once (`EmbeddedPrompts.Folder`); a skill beside its voice (`prompts/agent/{identity}/skills/`) says in its header whether it loads `always` (after the voice) or `on-demand` (listed, read with the engine's `ReadSkill`, a session's only when its identity has one); the guide keeps who Louis is, an always index of the pages, and reads the docs map and the page map on demand, its fixed prefix 14% smaller, measured live (ADR 0114) |
| F128 | 2026-11-10 | The site sells websites with agents and custom agents, the framework as the proof: F128.1 and F128.2, both below (ADR 0112, ADR 0113) |
| F128.2 | 2026-11-10 | Three business demos: Wheatfield Kitchen, Koringland Attorneys and Graanveld Family Practice, made up, each an identity on the leads pack (`louis-agent.identity.showroom`), recorded once with real booking mails and played on its own page (`/restaurant-demo`, `/law-practice-demo`, `/doctors-practice-demo`), first on the demos list; the leads pack names no one; `RecordedChat` plays several turns and can hide its work; the practices never advise |
| F128.1 | 2026-11-09 | The site sells (ADR 0112, ADR 0113): the facts made true (one Container App, the guide's three packs, 100+ ADRs, the film re-exported); the business pages named for what they sell and the framework's pages under `/framework/…` with redirects; `/pricing` (two plans) and `/what-we-build`; the menu led by what we sell; the hero, landing and last strip offer first; the guide became Louis, labelled AI, tuned live (his story, help first, fun, three sentences, proper English or Afrikaans, a greeting by the time); a chat may hide its work (`ShowWork`); tiles may wear the eval screens' four colours; the loader shows again on a hard reload |
| F124 | 2026-11-09 | The framework's website leaves the code-review demo for `src/site/` (ADR 0111, ADR 0074 amended): `louis-agent.site` (the visitor area), the guide renamed `louis-agent.identity.guide` (`louis-agent-guide`), and its own host `louis-agent.site-api`, whose catalogue is the guide on the docs, leads and site packs, so louis-agent.dev runs no coding agent; `SiteBoundaryTests` lets only `src/site/` use packs and an identity, and nothing else in `src/` reference it; its own image (the ASP.NET runtime, the docs, its price tables), compose on 5080 (code review's API on 5082) and Azure yaml at half the CPU; code review keeps the chat (now at `/`) and the admin; the film export moves to `src/tools/` and the films to `src/docs/films` |
| F123 | 2026-11-09 | Found by search: a page writes its head through one `PageHead`, its description and its schema.org facts as JSON-LD (Blazor keeps only the last `HeadContent`, so `PageDescription` became `PageHead` on every page); the prerender carries them and adds a breadcrumb for every page under the landing; the landing names the organisation, the website and the software, with a 180 by 180 Apple touch icon; `/csharp-ai-agent-framework` compares louis-agent, on your own hardware, with Microsoft Agent Framework and Semantic Kernel from their own docs (ADR 0110); a how-to for Search Console |
| F122 | 2026-11-09 | The demos in one place: a Demos menu in the header and a Demos button in the hero lead to `/demos`, a tile per demo; the triage demo moves to `/triage-demo`, two columns with the team as swimlanes, one whole section (a band may stay unpaged); `docs/demos.md` explains each demo for the guide; the recordings end with a plain small guide button inside their window |
| F121 | 2026-11-09 | Every page served as HTML, for search engines: a page declares a `PageDescription` beside its title (all of the three sites' pages do, held by a test per site), and the framework's `MapLouisAgentSite` renders it with `HtmlRenderer` into the site's `index.html` on its first request, with its own title, description, link card, canonical link and words, kept until the next deploy (ADR 0109); a chat or admin page keeps the plain shell, an unknown address 404 |
| F106.3.4 | 2026-11-09 | The triage demo on /try: one real triage of the order service, recorded by a script that does the workflow's part, played step by step by the triage pack's new web views (the stage, a popup per step, the steps as a list), the hero's "Watch a triage" leading there; found FX107 and FX108 |
| F106.3.2 | 2026-11-09 | The triage identity: the manager's voice, the sorter, investigator and reviewer as roles, each ending with a typed tool of the triage pack (`SortCase` new), loaded by the coding catalogue; the convertibility check shared; an eval per role at a specialist's level (an eval mode names its depth), passed live on Haiku for about R0.15 |
| F120 | 2026-11-09 | Watch the coding agent work: the site's doors lead to `/coding-agent`, where the agent fixing a sample's two bugs, recorded once on Haiku for about R0.10, plays in the framework's chat view (`RecordedChat`, a shared replay loop, a pace by kind), then the offer; the live chat stays behind the key (ADR 0108); a diagram's gaps fit their own words |
| F119 | 2026-11-09 | How it works splits the loop from the web: the web's slide says the agent runs without it and the web turns an agent into a website, names how the site, the guide, the API and the accounts work with the engine, and has its own diagram; a sitemap of every page and place, held to the pages by a test |
| F118 | 2026-11-08 | A site of slides from the framework's parts: the section index from the bands, slides or a flowing page as a layout option, a section that pages sideways when it holds more, the hero, tiles and closing strip as framework parts; every place its own address (ADR 0107); the guide reads the section in view, opens a card, turns a page, lights a place with a spotlight, and walks a page from a tour button on every page |
| F117 | 2026-11-08 | The guide takes you to a page, and to a part of it: the site pack's ShowPage, checked in the API and heard by the widget, which navigates, scrolls the area into view and lights it up; every section a framework Band with an id; the page block lists the areas; ADRs 0105 and 0106 |
| F115 | 2026-11-08 | The guide knows the page: a message may carry the page the visitor is reading, one block in the user's message, sent once per page by the widget's own module, the guide's voice says what it is (ADR 0104) |
| F116 | 2026-11-08 | The guide's chat on the landing: the shape of Louis's own lead chat as an animated panel beside the custom-agents tiles, on the hero's live-turn styles, with made-up details |
| F112 | 2026-11-08 | The guide mails Louis a lead: a leads pack with one tool that refuses an incomplete lead and tells the truth when it only reached the log, Resend behind a seam with the log sender on the PC, the rule in the voice (ask, repeat back, send on yes, once), ADR 0101 |
| F113 | 2026-11-08 | Cards that open: one CardDeck in the framework's web draws a row of short cards and, on a click, one story sliding in under the row; the agents on Built on it, the identities, the packs and how the rand come about are decks, each held by a test |
| F111 | 2026-11-08 | A diagram on every page: one GraphView in the framework's web draws an SVG from data a page declares and reveals it once as it enters; seven diagrams on the louis-agent site, each held to the code by a test (the identity's parts, the solution's groups, the pipeline's order, the demos on disk) |
| F110 | 2026-11-08 | The landing like Puls: the lockup in the header and the menu on the right (the frame's), a section index, a progress line and reveals driven by the frame's own motion module, the guide's personality (fun, bubbly, excited; teaches and sells by showing; the roadmap's money stays Louis's), and a custom-agents band on the landing and on Contact |
| F106.3.1 | 2026-11-08 | The service and the triage pack: the order-service sample restored from `pre-cl10` as the agent's workspace (a sample skips the size rules), `louis-agent.pack.triage` with the feed moved from the orchestrator, a run's recorder, the roles' typed endings checked in code, a case raised to a person, a run read back, departments as YAML held to their repositories by the boundary; the orchestrator's checks of a fix split into `FixChecks` |
| F109.4 | 2026-11-08 | The site tells the whole architecture: an Architecture page held to the solution file, an Identities page held to the identity folders, 100% C# and South Africa up front, the rand beside the dollars at the rates file's rate, what the framework stands on, What's coming held to the TODO, How it works the whole way, the short film as the map, the guide's voice naming every page, no link to the private repository; ADR 0100 takes F109's lasting parts and the parent closes |
| F109.3 | 2026-11-08 | The guide in the corner: a session names its identity, the docs pack, the expert identity beside the coding agent, the docs in the image, the guide's door (ADR 0099), the framework's `ChatWidget` and the demo's `ExpertChat` in the site's corner |
| F109.2 | 2026-11-08 | The demo's app beside Puls: `louis-agent-app` on `puls-env` from the same Bicep file and script (`-App`), its own identity, share, keys and tenant, live on `louis-agent.dev` with its certificates, released by the same workflow after green CI; ADR 0098 |
| F59 | 2026-11-08 | The framework's web: its leftovers closed with the site on the framework's web (F109.1) and deployed (F109.2) |
| F109.5 | 2026-11-07 | Puls deploys from GitHub: a workflow after green CI on `main` and from a button, signed in to Azure by OpenID Connect with no password, running `deploy.ps1 release` as it is; planned as F109 in another thread, folded into F109 as its fifth child (ADR 0097) |
| F109.1 | 2026-11-08 | The site in the web app: the framework's static site becomes the code-review app's landing (its frame the framework's `SiteShell`, its pages, films and look moved in, the look inside the frame, `console` the default theme, the films exported from the app), and the site project goes |
| F61 | 2026-11-08 | Accounts, tenants and the two sites in Azure, the parent: its five children built; the site's move (F61.6, PR #221) is the same work as F109 and folded into it at Louis's pick; the plan is deleted, its full text at commit `20f28c9f` |
| F61.4 | 2026-11-07 | Everything in Azure: Puls live on mypuls.co.za from a Container Apps environment on its own network, Mongo 9, the browser and SearXNG on a private VM, the accounts on Azure SQL's free offer, every secret in Key Vault, the build one Bicep file in config (ADR 0095); the CL19 sidecar, the SearXNG app and the F58 tunnel retired |
| F58 | 2026-11-07 | The two sites from this PC through Cloudflare tunnels: superseded by F61.4 before the Puls placeholder was needed; the framework's site waits for its merge into the louis-agent demo's web app |
| F105 | 2026-11-07 | The Azure app behind a key: folded into F61.4, the gate takes a signed-in person or `AGENT_API_KEY` (F61.2) and the ingress lock went |
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

## Fixes (108 done)

| Id | When | What |
|---|---|---|
| FX150 | 2026-11-13 | A late redraw never reaches for a page that is gone: CI's test host crashed when the stream's redraw throttle fired after a test let the page's services go, and the widget looked its business windows up in them; it takes them once when it starts |
| FX149 | 2026-11-13 | The guide shows every place it explains: on the landing's demos section it explained two proof tiles without showing them, so the page stayed put and the old chip went with every message; the site toolset's guide now shows a section, tile or card before explaining it, wherever it is |
| FX147 | 2026-11-13 | A choice can hold a box: the guide asked a visitor's business and offered a card that only said to type; an offered choice may now name the kind of words it asks for (`writes`), and its card holds a box and a send button, its words sent after the label as a form's answer is, the offer folding to it |
| FX146 | 2026-11-12 | The selection follows the guide's walk: walking the landing's tiles, the guide showed the section around them, so the first tile stayed pressed and its chip went with every message; the widget now lets a selected piece go when the guide shows another place, and the site toolset's guide shows each piece of a walk by its own address |
| FX145 | 2026-11-12 | A site spawned from the starter carries no "Starter": the starter's areas' Composition Root, `StarterAreas` (F127.6), kept its name in a spawned site, since the template renames only `LouisAgent.Starter`, and CI's spawn test failed; it is `WebsiteAreas` now |
| FX144 | 2026-11-12 | The guide reads the business's work whenever the visitor comes back after a live ask: after a booking in the guesthouse's window (its card said Sent) it told the visitor to carry on and book, answering from its last view (Louis: "it should have known"; his pick, a rule to read the work); the live-demo skill now calls `ReadAgentWork` first, every time, and never asks for what is already done |
| FX142 | 2026-11-12 | A card's or form's answer draws no bubble: the folded card already shows it, so a chat leaves the visitor's matching message out, a `ChatSettings.ShowCardAnswers` a site may turn on (Louis: "should not have done the bottom part there", "by default it would be off") |
| FX141 | 2026-11-12 | No choices while a business waits for the visitor: an answer that asked another agent owes no closing tool, and the guide says to answer in the business's window (Louis: "it is waiting for my name, so now no card should display") |
| FX140 | 2026-11-12 | On Ask it live the guide offers two or three questions to ask the business as cards, each sent as it is, and never a card that only says to type (Louis: "it should have given me a prompt for seemeeu") |
| FX139 | 2026-11-12 | The guide always ends with choices: an identity names the tool each answer ends with (`EndsEachAnswerWith`, the guide's `Offer`), and when an answer ends without it the engine asks once more with a notice, the tool required and thinking off (Louis: "the guide should always display options, even to go to another page after it was done"; his pick of three: the engine asks for them) |
| FX138 | 2026-11-12 | Offering choices ends the guide's turn: a tool marked `[EndsTurn]` (the site pack's `Offer`) ends it once it succeeds, so the guide waits for the visitor's pick instead of running on to the next stop (Louis: "why would it do this?"; his pick of three: an attribute core honours) |
| FX137 | 2026-11-12 | The page's tour asks the guide to start from its top ("Walk me through this page from its top, one section at a time."), so it no longer skips the heading for the section on screen (Louis: "it also skipped the top part") |
| FX135 | 2026-11-12 | One guide for the whole site: the widget's runner, chat, session, open panel and size are kept per identity (`KeptGuide`, `GuideKeeper`), so a move between the showroom's layout and the site's carries the talk on, even mid-answer (Louis: "the agentsession must be for the whole site", "except the demos"); the guide above both frames comes after |
| FX134 | 2026-11-12 | The business the guide asks knows the page it sits in: each window's page goes with the guide's message and `AskAgent` sends it to that business's voice, so it shows a room on its first answer (Louis: "the demo agent should know on which page he is on") |
| FX133 | 2026-11-12 | The guide offers a demo live: the cards gain Ask it live in code where a business's window shows (the prompt rule alone lost to the walkthrough's three choices), and after the business answers it coaches what it did and why, then offers questions to ask next (Louis: "I do not get the guide to teach me", "when he is the teacher he must give me offers of what I could ask") |
| FX132 | 2026-11-11 | The guesthouse demo is blue and the law practice amber, in the showroom's rail and on their pages (Louis: "guesthouse must be blue") |
| FX131 | 2026-11-11 | A test that lists the demos skips a dot-folder (`TestPaths.Demos`): the starter's spawn test leaves `demos/.spawn-…` while it runs, and the demos diagram test counted it in the full suite |
| FX129 | 2026-11-11 | The cook's file names `web` among its toolsets, as its text said: Puls's old hand-over sent it, an `AskAgent` sends none (F127.3.1 step 11c-1) |
| FX128 | 2026-11-10 | The landing's lead chat says who speaks (a Visitor tag in blue, a Louis tag in green), and a window's three dots wear a Mac window's red, amber and green |
| FX127 | 2026-11-10 | `GetToday` names the part of the day, the guide greets by it, and a visitor never gets an ADR's number |
| FX126 | 2026-11-10 | A card a page break would cut in two starts on the next page of a paged section (a forced break the frame's script sets, as Chrome ignores `break-inside: avoid` there); one taller than a page still runs on |
| FX125 | 2026-11-10 | Every page has one h1: the framework's `BandTitle` heads the page in its top band and is an h2 elsewhere, and a document inside a page starts its headings at h2 |
| FX124 | 2026-11-10 | A section's own address opens from a link, a reload or a search again: the site's router carries the place page itself (lost in F124's move), without the framework's admin pages |
| FX123 | 2026-11-09 | A release numbers its revision from every revision the app has, so one made by hand in Azure never stops the next |
| FX122 | 2026-11-09 | The louis-agent site runs alone on Azure, sleeping when unused, with no VM and no databases; Puls is not deployed (Louis's option A, after the SQL free offer ran out and took the site down) |
| FX121 | 2026-11-09 | The guide's spotlight on a site's hero rings what the hero holds, not the whole screen-tall section |
| FX120 | 2026-11-09 | The guide can open where a link on the visitor's screen leads: the page it reads lists its links by the address each opens |
| FX119 | 2026-11-09 | The custom agents' section opens with a paragraph under its heading, so its first page is never a heading alone |
| FX118 | 2026-11-09 | A film fits under its section's heading on any screen, so the landing's How it works is one page with its film |
| FX117 | 2026-11-09 | A visitor who closes the guide mid-tour can resume it, where it stopped or on the page they moved to |
| FX116 | 2026-11-09 | On a phone the guide is a sheet over the bottom half of the screen, so the page it walks stays in view |
| FX115 | 2026-11-09 | On a phone the guide opens full screen again, above the header: the bottom bar lost its blur, which had trapped the panel |
| FX114 | 2026-11-09 | With a page's tour, the folded corner offers the tour alone, which opens the guide; "Ask the guide" goes |
| FX113 | 2026-11-09 | On a phone the tour and the guide's button sit side by side in a slim bar across the bottom, and the page's room ends above it |
| FX112 | 2026-11-09 | A film fits a slide's page on any screen: its frame is never wider than the page is high at 16:9, so it never overflows into an empty page |
| FX111 | 2026-11-09 | The site's header stays at the top of the screen, so the strip above a slide holds the menu, not the slide before |
| FX110 | 2026-11-09 | A section paged sideways keeps its heading with what follows it, so a page never holds only a title while its content starts on the next |
| FX109 | 2026-11-09 | The guide's walkthrough is told every page of a paged section: the page list counts as the pager does, so a page a long list flows on to is a stop too |
| FX108 | 2026-11-09 | A tool call missing an argument is answered with that argument's own description and choices, from the tool's schema, for every tool |
| FX107 | 2026-11-09 | The git pack's SwitchBranch runs `git switch`: on the API image's git 2.43 checkout refused it, and checkout could throw a file's changes away |
| FX106 | 2026-11-09 | The guide knows the card open on the visitor's screen, so "this pack" is the one they opened |
| FX105 | 2026-11-09 | The coding agent's page is one slide, side by side: the words on the left, the recorded session in a full-height window on the right |
| FX104 | 2026-11-09 | The offer names its fees: a once-off fee to build the site beside the monthly price, and Louis writing a customer's first Markdown files, for a fee, on the landing, the Contact page and in the guide |
| FX103 | 2026-11-08 | Every page's footer links to Louis's LinkedIn profile, LinkedIn's mark small beside the word |
| FX102 | 2026-11-08 | The guide lights what it talks about, offers a walkthrough or one section, walks it one stop per answer, and sells and takes the lead at its end |
| FX101 | 2026-11-08 | The chat follows the answer to its end until the reader scrolls up, however much text arrives at once |
| FX100 | 2026-11-08 | The WhatsApp and LinkedIn buttons sit quietly: outlined, the owner's mark and name, the owner's colour on the edge under the pointer |
| FX97 | 2026-11-08 | The Contact page links to Louis's LinkedIn profile, with LinkedIn's own mark and blue |
| FX92 | 2026-11-08 | The slogan reads "100% C#, because you can" |
| FX91 | 2026-11-08 | The band and the guide say what drives the work: a stoic and Kierkegaard outlook, meaning and purpose, a startup of one; a described job is a custom agent to offer, and the agent can run where the customer is |
| FX90 | 2026-11-08 | The guide opens on every copy of the site: the run profile and the compose file carry the guide's settings as the release does |
| FX99 | 2026-11-08 | The social card shows the website's address and the slogan, no longer the private repository's |
| FX98 | 2026-11-08 | The chat follows the answer to its end: one more scroll on the draw after the stream ends |
| FX96 | 2026-11-08 | A developer who asks the guide for code gets a whole Program.cs in a C# block: the API guide holds the listing, the voice says to write it |
| FX95 | 2026-11-08 | The guide knows its own leads: yes, shown, the leads pack it uses itself, a mailbox read for leads as a custom agent |
| FX94 | 2026-11-08 | A lead goes to Louis's Gmail address: Outlook accepted the guide's mail and dropped it without a trace |
| FX93 | 2026-11-08 | The deploy runs on the merge itself, CI on pull requests only (ADR 0097 amended) |
| FX89 | 2026-11-08 | The gradient headings keep their descenders: the chrome class's box reaches down for them |
| FX88 | 2026-11-08 | The coding agent's chat is closed to visitors until it is ready: "Coming soon" pills in the header and the hero, and the chat's address leads back to the landing |
| FX87 | 2026-11-08 | The site's release carries the guide's mail key: `secrets` copies it into the vault and `release` names it only once the vault holds it |
| FX86 | 2026-11-08 | The site's release names Resend's key only once the vault holds it; main's deploy after FX82 had failed on the missing secret |
| FX85 | 2026-11-08 | The diagrams read: the listing's box, wrapped notes, a column ordered by its neighbours, every box on a row grid and every arrow a connector with right-angle bends, back arrows under the drawing (ADR 0103) |
| FX84 | 2026-11-08 | A line between an answer's paragraphs: the chat's markdown says its own paragraph margin |
| FX82 | 2026-11-08 | WhatsApp's mark, green and words on its buttons as its guidelines ask (ADR 0102), a hover on every button, the hairline above the contact strip, and the index marks the last section at the page's end |
| FX81 | 2026-11-08 | The landing says the one rule the startup runs on, first principles, with a short summary |
| FX80 | 2026-11-08 | The chat widget shows a turn's cost in the site's currency: it loads the rates when it opens |
| FX79 | 2026-11-08 | The slogan under the brand: a Tagline slot on the frame, "100% C#, because I can" on the site |
| FX78 | 2026-11-08 | The guide is on Built on it, the third agent on the engine, on the landing's tiles and the page |
| FX77 | 2026-11-08 | The guide talks like a person, one thing at a time with a question back, and names the doc it read only to a developer |
| FX76 | 2026-11-08 | The landing's contact strip stands clear of the band above it and rises as it enters |
| FX75 | 2026-11-08 | Louis's WhatsApp on the Contact page: a button beside each mail link opens a chat with 066 391 2470, and the guide's doc names the number |
| FX74 | 2026-11-08 | The principles are public: the Identities page shows the engine's own principles file, embedded by link and held equal to it by a test |
| FX73 | 2026-11-08 | What's coming in the menu, and the framework's five pages grouped under one label that opens on hover or on focus: a `SiteMenu` component in the framework's web, CSS only |
| FX72 | 2026-11-08 | The guide is a salesperson that reads the person: plain words for a normie, the full technical answer once it knows it talks to a software developer |
| FX71 | 2026-11-08 | The guide's button pops as a pill that says what it is and pulses; the hero's turn takes its own class so the chat's turns in the widget keep their spacing |
| FX70 | 2026-11-08 | Enter sends in the guide's widget: the message box's key rule is its own module, attached to the box wherever it is drawn |
| FX69 | 2026-11-08 | The guide's widget works on a phone: the composer's glyphs instead of clipped words, the panel the whole screen under 600 px, the round button gone while it is open |
| FX68 | 2026-11-08 | Google sign-in works on Azure: both apps read the visitor's https from the proxy, so the return address matches |
| FX67 | 2026-11-07 | Each sign-in provider's button in the provider's own look: Google's, Microsoft's and LinkedIn's colours, font and logo |
| FX65 | 2026-11-07 | The sidebar's settings line up, and a picker's list can be read |
| FX64 | 2026-11-07 | The account pages look like their site: the action's own text colour, Puls's glass card on the water |
| FX63 | 2026-11-07 | The message box keeps its look: a scoped rule sits in the component that draws its element |
| FX62 | 2026-11-07 | The libraries' `sasscompiler.json` stays out of a publish |
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

## Cleanups (32 done)

| Id | When | What |
|---|---|---|
| CL60.5 | 2026-11-12 | Four diagrams on every project doc, and CL60 done: each of the 42 project docs shows its overview, a flow, a sequence of its main case and its interface design, 42 interface pictures, 8 flows and 13 sequences written from each project's code and looked at; `EveryProjectDoc_ShowsItsFourDiagrams` holds it, and both project-doc tests now see the demos' docs. CL60 as a whole: every diagram in the docs is data our own renderer draws (ADR 0118), no Mermaid left |
| CL60.4 | 2026-11-12 | The classes, and the rule: a `classes` kind in the diagrams library (types with members, their relations: inherits, implements, holds, uses, creates) drawn as a flow through an Adapter, a flow box's members under a divider, the import reading Mermaid class diagrams, and all 11 class diagrams swapped for pictures (F39's and F40's split in two); `NoDoc_HoldsMermaid` holds that no doc has Mermaid of any kind, ADR 0118 the rule, and the plans README asks for the four diagrams as pictures |
| CL60.3 | 2026-11-12 | The sequences: a `sequence` kind in the diagrams library (participants with icons and tones, steps as a Composite of records: messages, replies, notes, loop, opt and alt with else, nested), its layout, its renderer in the flows' look, the import reading Mermaid sequences, and all 45 sequence diagrams in 44 docs swapped for pictures, each looked at; `DocsDiagramsTests` holds their words inside and no sequence left |
| CL60.2 | 2026-11-12 | The flows: the layout grown (columns from the arrows, labels over lines, a down direction, groups and dashed arrows, waypoints, tracks and detours), Louis's joined look (a title bar, an icon and a tone per box, arrows and word chips in their tone, a legend, a note on every chart), a one-time import, and all 75 Mermaid flowcharts in 58 docs swapped for pictures, each looked at; `DocsDiagramsTests` holds clear words and lines and no flowchart left |
| CL60.1 | 2026-11-11 | The renderer and the overviews: `louis-agent.diagrams` (plain C#, the flow engine moved in, cards in Louis's style with a highlight and a heading look, 22 Tabler icons, one SVG with a light and a dark look), the `louis-agent.doc-diagrams` tool, `DocsDiagramsTests`, and an overview picture on all 42 project docs and two of Louis's figures |
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
| 2026-11-07 | The site's two films exported again, so main's CI is green and F109's deploy can follow it |
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
