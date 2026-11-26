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
| Features (`F`) | 168 | 76 |
| Fixes (`FX`) | 242 | 4 |
| Cleanups (`CL`) | 49 | 25 |

A child (`F59.3`) counts as its own row. "Open" is the TODO's table, one row per item.

## Open (105 in the TODO)

| Id | What | Phase |
|---|---|---|
| F171 | The bill checker (Louis, 2026-11-21; 2026-11-22: "this feature is next"): a phone app that reads a Western Cape municipal account from a photo or a PDF, checks every line against Cape Town's, Swartland's and Overstrand's own tariffs and drafts the dispute, free to download with one subscription; the product in its own private `bill-checker` repository on the framework's packages, the framework's parts here: F171.1 a photo or PDF reaches a tool (done 2026-11-24); F171.2 the bills pack reads and checks; F171.3 the dispute letter; F171.4 the identity and evals; F171.5 the account, consent and our own subscriptions part; F171.6 the phone app; F171.7 the stores; F171.8 the monthly report | Now, F171.2 on after F140 |
| F43 | Accounts, tenants and durable sessions; its first child, durable sessions, after F171.1 and before F157 | Now, F43.1 done; accounts and tenants not yet planned |
| F8 | Reliability | Now, after FX4 |
| F7 | Cost you can see and stop | Now, after F8 |
| F41 | Drafts and approvals | Now, after F7 |
| F42 | Safety and audit | Now, after F41 |
| F140 | A host names its parts with one builder: `AddLouisAgent(catalog, agent => …)` and `UseLouisAgent()`, the middleware in its safe order; its own plan | Now, after F42 |
| F157 | The Google integration suite: connect Google per tenant, read a sheet, a booking request, an Integrations section; four children | Next, after F171 |
| F172 | What we are building, and what just landed: a `/now` page on both sites, read live from GitHub's TODO, work log and commit scopes; ten steps | Now |
| F158 | A chart kind in the docs' diagram tool (Louis, 2026-11-17, of the stream probe's fluency graph, his pick B of three): a line and bar chart drawn from JSON in the pictures' own look, so a measured graph is drawn like every other picture (ADR 0118) | Next |
| F159 | The stream probe as a tool (Louis, 2026-11-17: "and the probe under tools", "store test results"): `src/tools/LouisAgent.StreamProbe` asks a site's guide with pacing on and off, stores each run's timings and the fluency chart's JSON in the docs, drawn by the tool; after F158 | Next |
| F151 | Two themes a visitor picks between, glass and flat, each light and dark, remembered by the browser; each theme a package of values a site opts into, worn by every page and component we have, with the agent bar on it (Louis, 2026-11-15: "you will be able to choose your theme, it defaults to glass, light", "2 themes, 2 styles light and dark", "we should just have the user pick"); eight children: the contract, the two packages, the two switches, the framework's components, the libraries and the other sites, louis-agent.dev's pages, the pictures, and the demos recorded again | Now, next |
| F148 | One docs endpoint: a small header on every doc, one docs library behind `/v1/docs`, every doc with its path, commit and GitHub address; the guide's tools and `doc://` read the same library; its own plan | Now, after F151 |
| F150 | Every doc follows its kind's pattern: one pattern per kind of doc, held by a test whose list can only shrink; the framework's and the packs' docs, the 34 project docs first; its own plan | Now, after F148 |
| F149 | The docs' lists are generated: the project tables, the settings table, the indexes and a project doc's fact sections written from the code and its comments by the docs' tool and checked in CI; its own plan | Now, after F150 |
| F146 | A business's agent reports back by address: its `window` role tells the guide everything with `ReportBack`; the visitor still takes over; its own plan | Now |
| F147 | Page squares under the guide's choices: three grouped "go to" squares the guide picks; its own plan | Now, after F146 |
| F145 | Who a talk was with: the visitor's IP on the overview, a retention rule, the privacy page told about both (the page itself came in F145.1); its own plan | Now, after F147 |
| F139 | A site draws its own cards: a card, a receipt and a form slot, each site its own SVG; the form turns Sending then Sent in place; its own plan | Now |
| F141 | Every part of the framework's web ready for an agent: the account pages and the admin name their focus and selectable pieces; an admin helper; its own plan | Now |
| F138 | The guide explains with drawings: a tool gives the model a docs diagram's JSON and the widget draws it in the chat with `LouisAgent.Diagrams`; its own plan, after F127.4 | Now |
| F132 | Found for the searches business owners make (Louis, 2026-11-10, of a search for "ai agents for my website": "is our seo geared for a search like this?"; his pick of three): a page for "AI agent for your website" (South Africa, from R500, what it does, an FAQ with FAQPage and Service data), the three demos titled for their business searches ("AI agent for a guesthouse website"), and "AI" in the landing's title; the long and local searches first, since the head term is ads and big brands; its own plan | Next, after F127.2 |
| F130 | The subdomains, together (Louis, 2026-11-10: "demos. and framework. together, with a redesign of the framework website"), a parent with three children: F130.1 the host by name (one app answering several host names: DNS and certificates, a page per host, canonical links, sitemap and robots per host, old addresses redirected for good; about 4 steps), F130.2 `demos.louis-agent.dev` (F127.1's showroom at its root, the guide's page index and the links moved; about 3 steps), F130.3 `framework.louis-agent.dev` redesigned (what the framework is, how it works and how to build on it, for developers, while `louis-agent.dev` sells; the framework's sections move there; research, mockups and Louis's picks first); each child its own plan | Next |
| F106 | The framework's track, before the agents | Now |
| F74 | Packaging the engine in place, then its own repo after the engine features | Now |
| F44 | The engine as a product | Now |
| F39 | Orchestration | Now |
| F62 | Validate demand | Now |
| F63 | The reference transcripts in git | Now |
| F64 | The judge check can't see pages | Now |
| F66 | Each pack declares what it needs | Now |
| F67 | A whole menu | Now |
| F69 | The framework's `_Imports.razor` is about 90 lines | Now |
| F71 | At the next run, read the hand-off lines | Now |
| F73 | Check the 47 new own-prices rows | Now |
| F125 | An agentic site: docs the standard way and public over MCP, buttons and cards in the guide, gaps, live demos, tools over MCP and discovery | Now |
| F168 | Facebook auto-answers (Louis, 2026-11-21: "plan integration into facebook to auto answer questions"): the guide answers the Page's Messenger in his voice through a framework library, `louis-agent.api.messenger`, over the sessions API with its own key and a daily cap per person, and steps back when Louis answers himself; a data-deletion callback; eleven steps, after F126 | Now, after F126 |
| F169 | The framework's site, as the design review picked it (Louis, 2026-11-21: "review from a branding and look and feel perspective the framework site design and flat themeing", then "I want you to rhoroughly plan this"): his picks on thirteen findings, built in seven children. F169.1 flat's lines and inks: every text on a flat page at 4.5 to 1 or more, the ring at 3 to 1, held by a test (done 2026-11-21); F169.2 the docs page's type and edges: three heading sizes, 74, 40 and 28 (40, 32 and 24 at 390 wide), the Overview's families h3s under one h2, four corner names and a step's six set by every theme, Orbi's panel at 18, no step on a disc in flat, the rows of terms and the page turn ending on the page's edge (done 2026-11-21); F169.3 the brand's parts: the mark the only ball, the name's ball one size, the footer's three links (done 2026-11-22); F169.4 flat has no depth: no shadow, blur or gradient on a flat page, glass's talk on its own small shadow (done 2026-11-22); F169.5 the phone: no page scrolling sideways at 390 wide, every picture read without zooming (done 2026-11-22); F169.6 a card for each page: a link to a page previews that page's own card (done 2026-11-22); F169.7 the sequence drawn for a phone: on a 390 screen the sequence on How it works reads without sliding (done 2026-11-22) | Now |
| F131 | The chat on any website: one script tag opens our widget in a frame from louis-agent.dev on WordPress, static HTML or any site, nothing lost; a door per site, sites per tenant (after F43), a monthly rand budget (after F7), our own WordPress plugin, an answer that survives a page change; seven children | Now |
| CL66 | The `ticket` kind's name, for Louis | Later |
| F137 | The site's card content as JSON: the demo cards, the pricing, what it does and the custom-agent steps read from JSON beside their pages and drawn by one card component, so an agent that edits the site changes data, not markup; groundwork for F133, after CL60 | Next |
| F133 | A website with an agent, set up by talking to it (Louis, 2026-11-11: "How about I build a wix with my platform"): not a Wix, but one record per business that draws both its site and its agent, three templates (guesthouse, restaurant, practice), a setup pack that writes the record as the owner talks, preview then checkout then live on its own host name; builds on F131, F43, F130.1 and F7; seven proposed children, four open choices (the record's shape before F131.1 starts) | Next |
| F152 | The showroom's demos, fuller, each part Louis's pick: a business's chat draws a card for the place its agent opened, as the start's window shows it (Louis, 2026-11-15: "like the inner agent do not do this?"), and the three websites gain sections, which means recording their conversations again (ADR 0115) | Next |
| F143 | The guide drives Louis's CV: a resume pack with `GetResume` over the same `cv.json`, `/cv` in the place index, a hiring focus skill, an eval of eight conversations; after F142 | Next, after F142 |
| F46 | Localisation | Next |
| F40 | The sales playground | Next |
| F36 | MCP servers as packs | Next |
| F15 | Model limits and local models | Next |
| F108 | The admin portal is a role of a signed-in person, not a key: ADMIN_API_KEY goes once an account can carry the admin role (Louis, 2026-11-08: "should this not be a role?") | Next |
| F51 | Hosting for public traffic | Next |
| FX59 | binary attachments sent as files, not text | Later |
| FX60 | a continued specialist's ledger rows name the turn that continued it | Later |
| FX61 | A site whose host has no `ACCOUNTS_SQL_URL` still calls `/account/me` and `/account/providers`, gets `index.html` back and dies on the JSON (the starter's login and chat pages show Blazor's error bar); the client should take a host without accounts as "nobody signed in" (found in CL58) | Fixes |
| FX252 | The coding agent's chat page lost its Stop button during a ten-minute turn and took messages the server refused as "still answering" | Next, after F97.1 |
| F90 | WhatsApp as a host | Later |
| F92 | Voice: the person talks, our API turns it into text behind the chat's seam, the text lands in the box to check (Louis's pick B, 2026-11-11) | Later |
| F45 | Knowledge and retrieval | Later |
| F94 | Searching PDFs, investigate much later | Later |
| F47 | The CLI and Rider's host on the agent API | Later |
| F9 | Long chats | Later |
| F10 | Settings per role and per session | Later |
| F95 | What's left of CL29 | Later |
| CL17 | what's left of the coding identity | Later |
| F96 | The browser's user agent names the user's town per visit | Later |
| F97 | A skill usable in the chat that made it (F97.1 one skill, used in the chat that made it, F97.3 tool search for the built-in tools) | Now, F97.1 next |
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
| F173 | Google Drive for a business's agent, was F157.5 | Later |
| F174 | Gmail for a business's agent, a reply drafted for the owner, was F157.6 | Later |
| F175 | Visitors connect their own Google, was F157.7 | Later |
| F178 | Skills over MCP as SEP-2640 has it | Later |
| F179 | The coding agent sees an image or a PDF you attach | Next, after F97.1 |
| CL34 | The work email out of git history | Cleanups |
| CL35 | `Web/Browsing/RedirectBodies` goes when a PuppeteerSharp release awaits its own redirect body ta… | Cleanups |
| CL36 | Design docs for the four projects that have none | Cleanups |
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
| CL52 | The rest of the size list | Cleanups |
| CL53 | A price format per country | Cleanups |
| CL54 | A pack can't add itself to a router | Cleanups |
| CL55 | Does an unanswered request belong in the usage ledger? A cancelled request is a record of 0 toke… | Cleanups |
| CL71 | The ACP server on Microsoft's StreamJsonRpc, our read loop and writer gone; after FX4, plan first | Cleanups |
| CL72 | The CLI's console drawn with Spectre.Console; after FX4, plan first | Cleanups |

## Features (168 done)

| Id | When | What |
|---|---|---|
| F43.1 | 2026-11-25 | Durable sessions: a chat survives a restart, kept in a store behind one seam (in memory, or MongoDB locally and a free DocumentDB cluster in Azure) for 30 days after its last message; a visitor's chat resumes without the key, a keyed one still needs it |
| F171.1 | 2026-11-24 | A photo or a PDF reaches a tool: a message carries one as bytes, the API keeps it checked (five kinds, 10 MB a file, 20 MB a chat) and the model sees only its name; a pack's tool finds it through the pack context and reads it with the model the program's catalogue handed it (`AgentHost.Build(models => …)`), each call a `tool` row in the ledger, unmarked for the cache, and replayed by its bytes; the live readings wait on a run with a key |
| F177 | 2026-11-24 | Work with us: how a job with a client goes, as a page of slides at `/work-with-us`, in the menu before Contact; FX246 and FX247 fixed the slides layout for it |
| F176 | 2026-11-23 | Sequences: an identity may declare a fixed order of agents from its own `Calls`; asking a sequence's first step runs the whole sequence in code for one of the turn's asks, and a later step can't be asked alone; each step held to one ask's checks, given the first message with the answer before (or the whole conversation, or only the answer before), and on a failure stopping (or asking once more, or skipping it, named); the rules checked before a step opens |
| F126 | 2026-11-24 | Evals, redesigned, closed: one `.eval.json` of conversations beside each agent, facts in code and criteria a judge marks, runs started, followed, flipped and re-graded from an admin page, replays that play back the model's and the tools' answers for free; the guide, the framework's Orbi and the triage team each measured, ADR 0134 |
| F126.5 | 2026-11-24 | New evals, closed: the guide's, the framework Orbi's and the triage team's, each on its own host with an admin page, each baseline replayed free |
| F126.5.3 | 2026-11-24 | The triage team's evals: the demo's host takes the eval routes, a data folder and the evals' admin page, the manager measured with its team on the sample service with routing proved in code, a baseline at $0.0067 a chat replayed free; the evals' pages tell them as they are; FX240 to FX244 done in it |
| F126.5.2 | 2026-11-24 | The framework Orbi's evals: the framework site's host takes the eval routes and a data folder here and on the Azure share, an admin page with the evals in the framework's frame, twelve conversations from Orbi's rules with an injection case, a live baseline (8 of 12, $0.066) replayed free over no docs; the recordings a folder per eval |
| F126.5.1 | 2026-11-24 | The guide's evals, F126.5's first child: twelve conversations from its rules and its turn log, a prompt-injection case on a page given in the eval file (`GivenPage`), no lead ever sent, a live baseline of three repeats (9 of 12, $0.090) and every conversation's first repeat replayed free; FX228 and FX230 done in it |
| F126.4 | 2026-11-23 | Flips, re-grade, better-or-worse and Copy as conversation, F126's fourth child: a person flips a judge's mark, a re-grade asks the judge only for what changed, a run is compared with the one before, and a played conversation is copied back as eval JSON; the re-grade proved against the real provider on 2026-11-24 |
| F126.3.4 | 2026-11-23 | Starting, following and stopping a run from the admin, F126.3's fourth child: Run live with its estimate and a yes, Replay free, the run page following a run as it plays, and Stop |
| F126.3.3 | 2026-11-23 | The page a conversation names, F126.3's third child: the site's API answers a page as a message carries it, read on the server as the widget reads it (`GET /v1/site/page`), the focus rule in the shared contract; a run asks it, sends it with the first ask and keeps it for its replay; a missing page stops a run; the guide's eval recorded again with its page and its expectations rewritten for the landing |
| F126.3.2 | 2026-11-23 | The run pages, F126.3's second child: `LouisAgent.Web.Evals`, an area a site names with `.Evals()`, louis-agent.dev among them; the evals' page, a card an eval; a run's page, its number first, its conversations failed first with their first miss, its states and the eval's other runs; a conversation's page, one repeat turn by turn with each mark's reason, walking the failed ones and to the run before; every figure a method on the run's records |
| F126.3.1 | 2026-11-23 | Runs, F126.3's first child: an eval file played against the agent over the sessions API, one session per repeat, graded, kept as one JSON file and replayed from its recording for free; the contracts, and the eval routes a site's API maps behind the admin key, one run at a time in the background; an operator's run passes the open door as the agent visitors get; the guide's eval replayed as one test; ADR 0134 gains two lines |
| F126.2 | 2026-11-22 | The file and the grading, F126's second child: `LouisAgent.Evals` reads an eval file of conversations into records and refuses a wrong one at its first fault with its place, makes its schema from the records, tells seven facts from a turn's transcript, and has a judge mark one criterion with one model call, proved on Claude; ADR 0134 gains its line |
| F126.1 | 2026-11-22 | The old evals deleted, F126's first child: the harness, its protocol, check contract and runner, the studio's API, the eval pages, their stores and tests, from the outside in, nothing ported; 32,892 lines of code and 35,466 of tests; ADR 0134 supersedes nine; the trust page shows one open eval tile; F70, F93, CL49 and CL59 closed with it |
| F169.7 | 2026-11-22 | The sequence drawn for a phone, F169's seventh child: on a phone a sequence is its steps down the page (`StepListLayout`, `StepListDrawing`), each call or answer naming both ends with the wide drawing's own beam or dashes between them, its words under them, blocks as the wide drawing's frames a level in; `NarrowFit` says every sequence has a narrow form, so no picture on the framework's nine pages slides |
| F169.6 | 2026-11-22 | A card for each page, F169's sixth child: every site's head says what its picture shows (`og:image:alt`); the host gives a page its own share card by where its file is (`social-cards/{route}.png` and `.txt`, `PageCards`); the card tool's `framework-cards` draws the framework's site's card and its eight pages' cards in flat from the running site, refusing one whose type didn't load or whose words pass the middle; `SiteCardsTests` holds each card's words to its page |
| F169.5 | 2026-11-22 | The phone, F169's fifth child: in the docs layout the frame draws the site's pages in the header's sheet as well as in the column, one shown by width; the words 342 of a 390 screen; a long code word breaks; a cards picture can run down from a trunk, and the engine draws a flow or a cards picture for a phone on asking (`PictureFit`), its type under its title; a figure holds both drawings, Bootstrap showing one, and slides a picture with no narrow form, the sequence until F169.7 |
| F169.4 | 2026-11-22 | Flat has no depth, F169's fourth child: the search window, the typing row, the send and a talk's pieces read the shadow and blur names every theme sets, so flat draws a line or nothing and glass's talk takes its small shadow; a picture on a flat page with no glow and no fade, its six tones kept; the working line plain words in flat by `--web-working-light`; the grain over every page gone, retro's scanlines keeping their strength |
| F170 | 2026-11-22 | The framework's own Bicep for what the two sites stand on in Azure, proved by what-if and deployed as louis-agent-infra; Rebuilt from code in place |
| F169.3 | 2026-11-22 | The brand's parts, F169's third child: the name's ball takes the name's size, 0.41 of the letters in flat and 0.62 in glass, retro and the default; Orbi wears the mark on its header button, its panel's head and its phone button, a quiet pill with "Ask Orbi"; a `Mark` slot on `Band`, the Overview opening with the mark; the framework site's footer in one row, GitHub, Licence and louis-agent.dev; a picture's ball Geist's own bullet on the line, every picture drawn again |
| F169.2 | 2026-11-21 | The docs page's type and edges, F169's second child: a band that is a part of a section draws an h3, the Overview's five families parts of one section; headings by rank in the docs layout, 74, 40 and 28 (40, 32 and 24 on a phone), louis-agent.dev's unchanged; four corner names and a step's six set by every theme, Orbi's panel at 18; flat's step a squared label, glass's circle kept; a list of terms as wide as a list of steps, the page turn in the gutter, the menu's line to the page's foot |
| F166 | 2026-11-21 | Puls in its own repository, closed: its four children built (F166.1 the framework's parts out of `demos/puls`, F166.2 the framework as packages, F166.3 Puls moved to `github.com/bosbevk/puls`, F166.4 Puls gone from louis-agent); louis-agent is the framework and its own demos, and Puls the first product built on it from outside |
| F166.4 | 2026-11-21 | Puls gone from louis-agent, F166's fourth child: `demos/puls` and the three packs leave with their 26 projects, the framework's pages show Puls as a product built elsewhere (an `ExampleHome`, a `RepositoryLink`, ADR 0100's narrow exception), the pack rules that left with Puls come back over every pack in `LouisAgent.Packs.Rules.Tests` (DevOps and Paymo listed under FX83), and every doc says what is here |
| F169.1 | 2026-11-21 | Flat's lines and inks, F169's first child: flat's dark line one firmer grey, `#404048`, pictures on a flat page too; the faint grey and a listing's comments readable on a card, held by `FlatInksTests` on a shared `Contrast`; the keyboard's ring drawn once; a topic line in the theme's purple, glass light's now the deep violet; a quiet control on the chip's line; a code word drawn from three names every theme sets, on a page and in the agent's answer |
| F166.3 | 2026-11-21 | Puls in its own repository, F166's third child: Puls and its three packs moved to `github.com/bosbevk/puls` as `Puls.*` in `src/` and `tests/`, by once-off commands kept there, its history left here at the tag `puls-moved-out`; on the framework's packages alone, which learned to work outside this repository on the way (the web packages carry their Sass, the test helpers and the runner's stub their files, `LeadImage` public); its CI green from the feed with its own token, the coming-soon page published from there |
| F166.2 | 2026-11-21 | The framework as packages, F166's second child: the 31 projects a product reaches published to GitHub Packages at 0.1.0, each versioned by MinVer from a tag of its own name, a commit's tags published as one batch with a guard against a dependency nobody can restore, and a project outside the repository that restores them with only a workflow's token and runs an agent (ADR 0130) |
| F167 | 2026-11-21 | Every project named for its namespace, before F166.2: all 155 projects, their folders, docs and pictures renamed to their `RootNamespace` (`louis-agent.pack.web` to `LouisAgent.Packs.Web`), nine names listed by hand, by git and shell commands run once and kept in `src/tools/rename-projects`; what found projects by the old spelling follows them, the log's service name unchanged, the eval studio's publish folder fixed |
| F166.1 | 2026-11-20 | The framework's parts out of `demos/puls`, F166's first child: the API's tests run on a made-up `TestAgent` and the framework's fakes, a test runner on the general checks and the template eval, and a sample eval each test writes for itself, moved to `src/tests/hosts`; `deploy.ps1` beside the sites' yaml without `infra`, `vm` and `sql-access`; its guide and architecture in `src/docs`; its tests in the site's; the workflow `deploy-sites.yml` |
| F164 | 2026-11-19 | The framework's own site, closed: its three children built (F164.1 the seven pages as a package, F164.2 the framework Orbi with its search and four ways to ask, F164.3 the second Azure app), live at framework.louis-agent.dev with its certificate, louis-agent.dev's menu and CV leading there; tested live by Louis |
| F165 | 2026-11-19 | The trust page at `/trust` on louis-agent.dev, after a reviewer asked for real technical facts (Louis's pick: a status on every tile): five parts, where it runs, your data with every company that handles it, AI safety, if something breaks, and the reviewer's checklist before paying customers; every fact In place or Not yet, each in place held to a file of the repo by a test; a tile's status in the framework; the footer and the guide lead to it |
| F164.3 | 2026-11-19 | framework.louis-agent.dev as a second Azure app, F164's last child: one more row in `deploy.ps1`'s table, on louis-agent.dev's identity and share with its logs in `framework/logs` and keys of its own; the `domain` verb tells a subdomain from an apex; a release makes the folders its app names; every merge releases both sites; 20 sessions a day everywhere; "Framework" on louis-agent.dev's menu and the CV's four cards on the new site; the Azure docs rewritten to what runs today |
| F164.2 | 2026-11-19 | The framework Orbi, F164's second child: `LouisAgent.Identities.Framework`, an identity of its own on the docs and site packs, run by the framework site's host with its door open and the docs in its image; the chat docked beside a page as a third `WidgetLayout`, in the docs layout's right column in place of "On this page"; the door moved into what every site gets; Ask Orbi in the header with Ctrl+I, Ask about this page, Ask on every code listing; a search window with Ctrl+K over `GET /v1/site/places`, the site's places with no model and Orbi asked last; one live check, R0.037 and R0.06 |
| F164.1 | 2026-11-18 | The framework's pages, the first of F164's three children: the old pages thrown away from louis-agent.dev and seven written new, Overview to Comparison, as `LouisAgent.Web.Framework`, a package a site turns on with `.Framework()` (Louis: "the framework must be a package like accounts"); `.Docs(address)` in `LouisAgent.Web.Docs` for a doc linked by its path; `.Places(home, site)` in `LouisAgent.Web.Places`, the catch-all moved out; the framework's own site and its host on :5084, flat alone, in the docs layout; every picture in the page's theme, with the brand in its footer and the site's fonts; what each page says held to the repo by its test |
| F163 | 2026-11-18 | The free websites, hosted free for the first year, move to their own page, `/free-websites`, and the landing keeps one line under the hero that leads there: the framework's new announcement strip, in a new slot of its hero, a plain pill with no glass; Orbi gains an always-on writing skill, short sentences with no colon, semicolon or bracket |
| F162 | 2026-11-18 | Two free websites: a band right after the landing's hero where any business enters through Orbi, who takes the entry in one form and mails it on a yes; two are chosen; the theme page shows nothing twice, four cards with one switch, one off button and one status pill |
| F161 | 2026-11-18 | The brand speaks to owners: the guide is Orbi, Louis's AI agent, telling his story as his (ADR 0113 amended); the slogan "The website that answers." in the header, footer, tab, link preview and a one-ball social card; the hero "Your doors close at five. Your website doesn't.", one sentence to a line (the framework hero's `Lines`); no price in the description; a face band naming Louis once, with his CV; every line on the landing rewritten for an owner |
| F145.1 | 2026-11-18 | The privacy page: `/privacy` says what the site keeps and sends and the visitor's rights under POPIA, linked from every footer and the landing's data tile; the guide says what Send it does before it sends a lead |
| F160 | 2026-11-17 | The guide walks the demo (Louis: "do not make it autoplay, give the ability to our agent to guide him through the experience"): a demo waits to be started, its conversation is a place of its own with an address per turn (his pick of three), showing a turn plays it and stops, and the guide walks it a turn at a time, saying what to watch, then offers to try the business live |
| F156 | 2026-11-17 | The web's pacing switch (Louis's pick A of three): the API answers a turn the engine paced with `Stream-Paced: on`, the page leaves its own pacer off for it, and louis-agent.dev turns `STREAM_PACE` on, so the guide's words are paced once; ADR 0126 widened to types |
| F155 | 2026-11-17 | Pacing in the engine (Louis's pick A of three, after research): `STREAM_PACE=on` hands a turn's text out at `WordPace`'s pace in whole words, everything else in order, outside the turn; `WordPace` moved to session-stream and made continuous, which the caption shares; ADR 0126 on method names |
| F153 | 2026-11-17 | Reset my session on the admin's home (Louis's pick A of three): one press ends every session this browser saved on the API, forgets both storages but the admin key, and reloads, so the owner sees the site as a first visit (`SessionReset`, `ResetSession`) |
| F154 | 2026-11-17 | The answer's motion (Louis's pick A of three): the guide's words come at a steady pace while they stream (`WordPace`), its answer grows out of the bar's orb or the card it covers, and it fades before it goes (`CaptionHandOver`, `wwwroot/js/caption-motion.js`); an answer in a card's place fits its words (his pick C); the caption lets go of the page first when it goes |
| F151.5 | 2026-11-16 | The libraries and the other sites: the account pages, the eval studio and the packs' views drawn by the surface tool, so they wear the site's theme, held by the same rule test; code-review on glass, flat and its own retro, now with a light style, its picker gone; Puls keeps its brand; the pulsing buttons' glow on a layer of its own |
| F151.4 | 2026-11-16 | The framework's components: one surface tool, `web/tools/_surface.scss`, draws every pane, bar, press and field of `LouisAgent.Web` from the names a theme sets, so the site's parts, the stage, the widget and the mark, the chat, the admin and the rest follow glass and flat, light and dark; a rule test refuses any sheet that draws a surface by hand; the showroom's mock website wears its business's colours through the same tool and a mock tool of its own; the mark has a fifth state, Listening (ADR 0124) |
| F151.3 | 2026-11-15 | The two switches: `LookSwitch`, the site's themes and Dark and Light, drawn by the three frames themselves (the site's header, the chat's settings and, Louis's pick, the admin sidebar's foot) with no line in a site; a press writes the look on the page and the browser keeps it, a reload opens the same, another tab follows; on a phone the header's actions take their own row; tried in the browser on the starter (ADR 0124) |
| F151.2 | 2026-11-15 | The two themes: glass and flat in one project, `LouisAgent.Web.Themes`, a folder each (Louis: "we should only have one project called themes"), each light and dark from the mockups and held to all 85 of the framework's names by a sheet its own build compiles; the starter offers glass then flat and opens in glass, light; louis-agent.dev names the project on its Architecture page and otherwise looks as it did (ADR 0124) |
| F151.1 | 2026-11-15 | The theme contract, the first of F151's eight children: what a theme and a style are (`SiteTheme`, `SiteStyle`, `SiteLook`), a theme as one more slot on an area with the first named the default, the 23 surface names a theme sets beside the colours with today's look as their values, `first-paint.js` applying the remembered pick, every site's page saying light, and `ThemeSheet` and `IndexLook` to hold a theme and a page to them; no site names a theme yet, so no page looks different (ADR 0124) |
| F134 | 2026-11-15 | The showroom is a stage the agent runs, closed: its thirteen children built, a demo's own website sized by its window with its chat beside it (Louis: "each inner website, needs to be expanded, and made pretty and bigger", "everything must kinda fit 1080p"), and its rules kept in the web's design doc and ADR 0115 |
| F134.6 | 2026-11-15 | Better photos (Louis's pick of the three candidates): a lounge onto the sea, a stoep in the late sun and a study as the three hero photos, from Pexels, two of them cut so that no real business's sign, flag or painted face shows; each listed with its page in the site's design doc |
| F134.5 | 2026-11-15 | See how it did that (unattended): the caption turns over to the answer's tools, thinking time and usage line (`AnswerWork`), its cost on the front; the bar carries the talk's total and opens the drawer; a selected piece's chip in the bar; a demo's notes are pieces; a live answer's back matched its four ledger rows exactly; walks US$0.0074 |
| F134.4 | 2026-11-15 | The agent runs the room (unattended): the guide's answer as a caption beside the place it lit, with its offered choices; a demo's window forward when the guide shows it; a business's demo held while the guide talks (`GuideActivity`, `RecordedChat.Hold`); the whole talk a drawer on a wide screen and a sheet on a phone; the chips give way; the site's design doc rewritten as talk; live checks about US$0.0125 |
| F134.3 | 2026-11-15 | The agent bar (the J and K mockups' bar, unattended): the chat widget's second layout, one engine; the orb in four states read from the chat by a rule; the box always there; the site's suggestions as chips; the showroom keeps the bar's room; the showroom's guide is the bar; a live walk with a typed question cost US$0.0051 |
| F134.2 | 2026-11-15 | A demo on the stage (the K mockup's look, unattended): a demo's page on the lit stage in its colour, the title and the lede side by side, the player the whole width, what to watch as chapters under it, the rail a slim column of named icon links, the coding agent and the triage team in a lit window; the guide's stops held by their ids |
| F134.7 | 2026-11-15 | Component diagrams (Louis: "all projects which has components should have an component diagram"): a kind for a component and what it holds, now `nested`, its type a composite structure; one for every project that holds components, the four packs' web views and the starter last, each read from its markup; a rule holds it |
| F134.1 | 2026-11-15 | The start is a stage (Louis's pick F, centre stage; "things that can be shared, must go to framework"): five general parts in the framework's web, each tested alone (a line icon, a lit stage, a deck with `DeckPlace`, a framed window with three bodies, a dock), and `/demos` built from them; each window at its own address, swiped on a phone, its words held to its recording; ADR 0115 amended |
| F134.13 | 2026-11-14 | A composite structure diagram, a new kind (Louis: "no do research", UML's picture of the inside of one class; his picks: a port a named pin on the class's edge, and the flow's layout through an adapter): `"kind": "composite"` read into a class, its parts, ports and connectors and refused with a reason; `CompositeFlow` turns it into a flow so the flow's layout places it; a drawing with the frame's edges through the pins, a plain line between parts that work together and a running line for what is passed on; the picture says what type of diagram it is ("the type of diagram must always display"); `AgentEngine` drawn in core's design doc |
| F134.12 | 2026-11-14 | Flows and component diagrams in the other kinds' look (Louis: "make the theme of the flow and component diagrams resemble the others"): one small title over a soft light, a plain panel with a little body that needs no rule for its depth, a decision as a box with pointed ends, a group's frame as a panel, shapes with a thin outline and no glow, both keys with a tone as a small filled badge and the flow's under a rule, and on a light page glass with more body; on the way, a long line that runs straight and passes a box by the shortest way |
| F134.11 | 2026-11-14 | The class chart, redone in the cards' look (Louis's approved design): a type a glass card with a badge in its tone and a letter for what it is, read from its kind's words (`TypeMarks`); a link's kind in its line, violet for holds, is a and implements, blue for creates, thin grey for uses, with a UML chart's ends, a diamond, a hollow triangle and an open arrowhead (`LineEnd` in `SvgWriter`), its word in a flow's pill on the line; the layout still the flow's through `ClassFlow`, now by the rule a flow is drawn by; a key of what the picture has; the docs' frame the note bar alone; built beside the old drawing, switched in one commit, 58 pictures redrawn, and the old flow drawing deleted |
| F134.10 | 2026-11-14 | The sequence picture, redone in the cards' look (Louis's pick of three designs, C, with its key): a participant a badge in its tone with its name and no box, a lifeline that fades down from a node, a call a beam from violet to blue with its dots and arrowhead, an answer a quiet dashed line, a note and a block glass, and a key of what the picture has (`SequenceKeyLayout`); the layout handed its measures as one value (`SequenceMeasures`, a Parameter Object) so one layout served the old look and the new until the switch; what the cards and the sequence share moved to `MarqueeLook`, proved by the same 92 images before and after; `SequenceDrawing` built beside the old renderer and switched in one commit, 64 pictures redrawn |
| F134.9 | 2026-11-14 | The cards picture, redone (Louis's pick of three designs, "Marquee"): the overview every project doc opens with drawn as a headline on a light with its parts on glass cards hung from a lit rail, one accent, the palette's violet running to its blue; a link drawn on a rail in the gap between two rows, the cards above that feed the same cards below sharing one (`CardRails`, `RailPlaces`), a link that joins no two neighbouring rows refused; `OverviewLayout` with `CardSizes`, `OverviewDrawing` on the shared look with `CardsStyle`, and `SvgWriter` able to define a paint; built beside the old layout and switched in one commit, 45 pictures redrawn |
| F134.8 | 2026-11-14 | Flows in the new look (ADR 0103 amended): every flow in the docs drawn with its own title, tinted panels, marching lines and a key; a box level with the box that feeds it, each arrow into a box at its own point, the lines of a gap turning on one line (`FlowRules.Straight`: `ApartArrows` with `LevelRows`), class charts kept on the old rules until their turn; a box may name a shape (`decision`, `data`, `terminal`), named on 157 boxes of 60 of the 93 flows by what the box is; the site's eight diagrams laid out the same way, a word on its line on a ground |
| F142 | 2026-11-13 | Louis's CV is a page, and the page is its own editor (ADR 0122, the site's own decision): one `cv.json` in the JSON Resume shape, read strictly, drawn at `/cv` and kept on the site's API (the built-in copy the fallback, a save checked for what is private); the site's own Resume area in the admin, where the same view is edited where it stands (Louis: "can we not just edit it inline?", click, edit, confirm), a part editable exactly when someone listens for the whole; the framework's web gains the editing pieces (`TextField`, `FieldGroup`, `TagInput`, `InlineEdit`, `InlineForm`, `ItemTools`, `ItemAdd`, `ListEdits`, `PartChanges`), the API a seam for a host's own routes, and an address under `/admin` is no public page (ADR 0120); Save as PDF is a print stylesheet |
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
| F127.4.1 | 2026-11-12 | Cards the agent draws: a `ticket` kind in `LouisAgent.Diagrams` (a band with a kicker, a title and a status pill, a torn edge, rows of an icon, a label and a value, a note; 320 wide; colours `--ticket-*` variables a business's window sets), `TicketCard` from a call's parts, six new Tabler icons; the site pack's `ShowCard` and `Offer` (two to four choices, one suggested), checked by a tested duplicate of the library's rules, a refusal an error; the widget draws both with the work hidden (`DrawnCard` inline through `RenderForPage`, its styles in `@scope`; `OfferCards` as buttons), under the answer's words, a click sent as the visitor's message and the cards folded once answered; the three showroom businesses book through a ticket and *Book it*, the guide offers choices and every walkthrough stop ends in three cards; live on the PC at desktop and 390 px, $0.0148 |
| F127.3.1 | 2026-11-11 | Ask an agent by its address on this instance (ADR 0117, amends 0079): the session wire as shared records in `LouisAgent.SessionStream`; `AgentAddress` (`acme`, `acme.cook`) resolved against the identities' files; an identity's `Calls` checked in code and listed in its prompt; a role's level from its file's folder, so a business asked at depth 1 answers in its own voice; `AskAgent` starts or carries on one conversation per address (a busy one says wait, a gone one starts over), its answer under a line built from its own tool calls, `ReadAgentWork` for its steps; the triage team and Puls ask by address, Puls relaying answers whole (`RelaysAnswersWhole`); `StartSubSession`, `ContinueSubSession`, the `specialists` toolset and `SHOW_SPECIALIST_REPORTS` deleted; two live guide runs, $0.0053 |
| F127.2 | 2026-11-10 | The demos as working websites, tried live (ADR 0116): each business's mock website has three pages that register with it (`MockPage`, `MockPaths`), a menu a visitor clicks through, Wills and estates opened from its card, and a page change that loads like a browser; the agents' voices know their pages and the conversations were recorded again, each moving between pages and sections; Try it yourself swaps the recording for `MockLiveChat`, the business's agent live in the same corner, reading the mock's page, with a tap-to-ask hint, a Live tag, cues that wait and no usage line; `OPEN_IDENTITIES` opens the guide and the three businesses under one cap, the session narrowed to the identity admitted; a demonstration's lead marked `[demo]` by its identity (`IsDemonstration`, `TurnIdentity`); the three brands reworked (mockup 0) with self-hosted OFL faces, `MockHeader` and a label colour; minimise and maximise on both chats; the guide's demo help became F127.3 to F127.5 (ADR 0117) |
| F127.1 | 2026-11-10 | The demo showroom: a site's demos in an area of the framework (ADR 0115), a rail of every demo grouped by what a visitor comes for, a stage per demo in its colour whose parts each have an address, `/demos` its start featuring the guesthouse; a business's agent plays on a mock of its own website (sample brand, three looks, real Pexels photos, a real menu), held to its agent's facts by a test, and shows the website with the guide's own `ShowPage` (the guide's spotlight on a section, a press on a card), told as the recording plays; cues a visitor clicks to play from a turn; notes of what a custom agent could add; the guesthouse in the doctor's place and leading; the coding agent and the triage team on the stage; a place's address loaded afresh draws in its page's layout (`PlaceLayout`) |
| F129 | 2026-11-10 | An identity's skills, and `prompts/`: every `Prompts` folder is `prompts`, named once (`EmbeddedPrompts.Folder`); a skill beside its voice (`prompts/agent/{identity}/skills/`) says in its header whether it loads `always` (after the voice) or `on-demand` (listed, read with the engine's `ReadSkill`, a session's only when its identity has one); the guide keeps who Louis is, an always index of the pages, and reads the docs map and the page map on demand, its fixed prefix 14% smaller, measured live (ADR 0114) |
| F128 | 2026-11-10 | The site sells websites with agents and custom agents, the framework as the proof: F128.1 and F128.2, both below (ADR 0112, ADR 0113) |
| F128.2 | 2026-11-10 | Three business demos: Wheatfield Kitchen, Koringland Attorneys and Graanveld Family Practice, made up, each an identity on the leads pack (`LouisAgent.Identities.Showroom`), recorded once with real booking mails and played on its own page (`/restaurant-demo`, `/law-practice-demo`, `/doctors-practice-demo`), first on the demos list; the leads pack names no one; `RecordedChat` plays several turns and can hide its work; the practices never advise |
| F128.1 | 2026-11-09 | The site sells (ADR 0112, ADR 0113): the facts made true (one Container App, the guide's three packs, 100+ ADRs, the film re-exported); the business pages named for what they sell and the framework's pages under `/framework/…` with redirects; `/pricing` (two plans) and `/what-we-build`; the menu led by what we sell; the hero, landing and last strip offer first; the guide became Louis, labelled AI, tuned live (his story, help first, fun, three sentences, proper English or Afrikaans, a greeting by the time); a chat may hide its work (`ShowWork`); tiles may wear the eval screens' four colours; the loader shows again on a hard reload |
| F124 | 2026-11-09 | The framework's website leaves the code-review demo for `src/site/` (ADR 0111, ADR 0074 amended): `LouisAgent.Site` (the visitor area), the guide renamed `LouisAgent.Identities.Guide` (`louis-agent-guide`), and its own host `LouisAgent.SiteApi`, whose catalogue is the guide on the docs, leads and site packs, so louis-agent.dev runs no coding agent; `SiteBoundaryTests` lets only `src/site/` use packs and an identity, and nothing else in `src/` reference it; its own image (the ASP.NET runtime, the docs, its price tables), compose on 5080 (code review's API on 5082) and Azure yaml at half the CPU; code review keeps the chat (now at `/`) and the admin; the film export moves to `src/tools/` and the films to `src/docs/films` |
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
| F106.3.1 | 2026-11-08 | The service and the triage pack: the order-service sample restored from `pre-cl10` as the agent's workspace (a sample skips the size rules), `LouisAgent.Packs.Triage` with the feed moved from the orchestrator, a run's recorder, the roles' typed endings checked in code, a case raised to a person, a run read back, departments as YAML held to their repositories by the boundary; the orchestrator's checks of a fix split into `FixChecks` |
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
| F18 | 2026-10-29 | Evaluation; its leftover, the judge checked against Louis's own labels, closed on 2026-11-03 (60 answers he graded in the portal, run `cf37574d0f23`) |
| F14 | 2026-10-11 | Display currency |
| F5 | 2026-10-14 | follow-up, sub-session usage rolled up |
| F4 | 2026-10-11 | Prompt caching |
| F3 | 2026-10-11 | Show usage |
| F2 | 2026-10-10 | Prices and cost |
| F1 | 2026-10-10 | Usage ledger |

## Fixes (242 done)

| Id | When | What |
|---|---|---|
| FX253 | 2026-11-26 | The guide and the framework Orbi show a page on its own, then write, then offer, and Offer and AskFor say they end the answer; a `has-words` eval fact on every turn holds it in the replay tests |
| FX251 | 2026-11-26 | The eval test host stops before it disposes, so a run cut by its shutdown is left as it was and no late write races the folders' clean-up; a model that gives up when its call is cancelled |
| FX4 | 2026-11-25 | A chat keeps the tools and the system prompt it started with: a Conversation freezes them, frozen tools keep what they were sent and a rejected one never runs, the round limit's final request reads them from TurnTools, a resume sends its saved prompt and handles stale thinking per provider (stripped, or Anthropic's drop_block, proved live); five tidies and CL61 on the way |
| FX250 | 2026-11-25 | The framework's site keeps its chats too, in the same database as louis-agent.dev's, and its Comparison page no longer says that is planned |
| FX249 | 2026-11-25 | The sites moved to an environment on no network, in a group of their own, so no load balancer or public IP is billed (about R370 a month) |
| FX248 | 2026-11-25 | The sites' images build with Docker on the release's runner and are pushed, not built in the registry by the minute (about R850 a month) |
| FX247 | 2026-11-24 | On a page of slides the guide's bar stands as the corner button, so a slide has the whole screen |
| FX246 | 2026-11-24 | A long slide keeps its heading on its first page: a row of tiles or a list breaks between its items |
| FX245 | 2026-11-24 | The triage payments criterion asks for the rule, not the words |
| FX238 | 2026-11-24 | The framework Orbi ends its words with the doc's path, and sums a listing up in one sentence |
| FX237 | 2026-11-24 | The framework Orbi always writes words before a choice card |
| FX236 | 2026-11-24 | The framework Orbi names every setting under the code, one a line |
| FX234 | 2026-11-24 | The live estimate scales with an eval's conversations |
| FX233 | 2026-11-24 | The guide writes its words before any card |
| FX232 | 2026-11-24 | The guide names no doc anywhere, by its title or its path |
| FX231 | 2026-11-24 | Asked several things at once, the guide answers one part at a time, the greeting not counted |
| FX244 | 2026-11-24 | A search can stay inside one folder, such as the repository a triage case names, and the investigator and reviewer search there |
| FX243 | 2026-11-24 | A search hit shows a giant line only around its match, marked as cut, so a search's answer stays small and is never summarised |
| FX242 | 2026-11-24 | The triage manager raises a person's case as well as deciding it, since a decision alone gives it to nobody |
| FX241 | 2026-11-24 | The triage manager decides again when a case comes back with something new |
| FX240 | 2026-11-24 | Each triage voice declares the toolsets it always loads, and the demo names itself as a peer, so the team has its tools however a session opens |
| FX239 | 2026-11-24 | A failed chat turn tells the visitor sorry and to try again, never the exception's name and text; the exception stays in the log |
| FX235 | 2026-11-24 | A free replay plays back what each tool answered, keyed by compact JSON, so a docs edit or a system's newline can't make a recording stale |
| FX230 | 2026-11-24 | A docs search goes through the docs in the order of their paths with `/`, so it answers the same on Windows and Linux |
| FX228 | 2026-11-24 | The guide no longer knows the closed offer of two free websites: its voice, its entry skill and its maps of the site are cleared, and its eval was recorded again |
| FX229 | 2026-11-23 | The public site's guide names no doc, a developer's or anyone's, and its eval no longer asks it to; naming the doc is the framework site's agent's |
| FX227 | 2026-11-23 | The offer of two free websites is closed: its page, the line under the hero, its sitemap entry and what Orbi said and did for it are gone |
| FX226 | 2026-11-22 | The packages' integration test turns MSBuild's node reuse off too, the second test that waited 15 minutes for a leftover worker |
| FX225 | 2026-11-22 | The pack-install test turns MSBuild's node reuse off, so CI's integration step no longer waits 15 minutes for a leftover worker |
| FX224 | 2026-11-22 | deploy.ps1 update writes its yaml with no byte-order mark, so it runs from Windows too |
| FX223 | 2026-11-22 | Two of the vault's rights out, given to Puls's apps that are gone |
| FX222 | 2026-11-22 | The band test's id is no longer the core toolset's wire name, so main's repeated-names rule passes again |
| FX221 | 2026-11-22 | Two leftovers out of Azure: the old VM's empty subnet and three image-pull rights of apps that are gone |
| FX220 | 2026-11-22 | The parallel engine's test holds its round open until all three calls have started, so a busy machine can't make it count two |
| FX83 | 2026-11-21 | The DevOps and Paymo packs retired: their waiting tools took no cancellation token, and nothing loaded them |
| FX219 | 2026-11-21 | The re-score's 409 test keeps the runner busy with the stand-in's sleeper, not a stub run a fast machine finishes before the request arrives |
| FX218 | 2026-11-21 | The pack-as-a-package integration test packs at the version it asks for, since MinVer versions every package from its tags after F166.2 and an untagged pack is no longer 1.0.0 |
| FX217 | 2026-11-21 | framework.louis-agent.dev names a PNG icon and an Apple touch icon beside its SVG, since Google Search shows no SVG favicon and put a grey globe by every result |
| FX216 | 2026-11-20 | One address per page, for Google: a section's canonical names its page, both sitemaps list pages only, and with `SITE_ADDRESS` a page on www or any other host is sent to the site's address (ADR 0128) |
| FX215 | 2026-11-20 | The trust page's Azure facts rest on the deploy guide instead of Puls's Bicep file, and "Rebuilt from code" says Not yet, as is true |
| FX214 | 2026-11-20 | Puls's own docs and settings no longer say its Azure app runs on a VM, untrue since FX122 |
| FX213 | 2026-11-20 | The framework's stub eval says it proves the portal "not an agent" instead of naming Puls, held by a test |
| FX212 | 2026-11-20 | The git tests turn off git's background housekeeping in each repo they make, so its lock can't vanish mid-teardown |
| FX211 | 2026-11-20 | The recorded chat's three-turn tests play a four-event turn instead of the 1,503-event session, so CI finishes them in time |
| FX210 | 2026-11-20 | An upload's file name reaches its pack: `ImportFileBody` names its wire name `file_name`, which the page sends and the API now reads |
| FX209 | 2026-11-19 | The Blazor WebAssembly server package moves to 10.0.15, past CVE-2026-102870, in all five hosts that serve a Blazor app |
| FX208 | 2026-11-19 | On a phone the header has one three-line button, its sheet holding the settings too, and the page no longer scrolls sideways |
| FX207 | 2026-11-18 | The admin's latest talks say which are the admin's own and which are visitors' |
| FX206 | 2026-11-18 | The header says beta-1, a quiet tag with a slow violet dot |
| FX205 | 2026-11-18 | The orb's bar stays open after a hover and a click, a chip without a price, a one-line slogan, a tape in plain words |
| FX204 | 2026-11-18 | The free-website entry no longer crashes the guide's bar when the form comes |
| FX203 | 2026-11-18 | Orbi asks again for a free-website entry that makes no sense, and never sends it |
| FX202 | 2026-11-18 | A long answer's box keeps its corner while it is typed in |
| FX201 | 2026-11-18 | Enter your business opens Orbi's entry form first, with nothing before it |
| FX200 | 2026-11-18 | Orbi asks for a free-website entry in a form it can draw, the step numbers stand centred, and a long answer's box is no longer a capsule |
| FX199 | 2026-11-18 | The theme page is Theme in the menu, every control on it works, and its words speak to an owner |
| FX198 | 2026-11-18 | Home leads the top menu on every page, the CV mark left the settings, and the privacy page names Orbi |
| FX197 | 2026-11-17 | The footer shows again, above the fade under the guide's bar, clear of the corner's link and the orb |
| FX196 | 2026-11-17 | A big heading is solid ink in every theme, with no gradient through its letters |
| FX195 | 2026-11-17 | The coding agent and triage demos are gone from the site, with their links, recordings and the triage web screens; the Demos menu is back |
| FX194 | 2026-11-17 | A demo page shows only the agent, and its Try it yourself is the theme's button |
| FX193 | 2026-11-17 | The light looks' accents are deep enough to read, so a demo's button and its dock icon show |
| FX192 | 2026-11-17 | `/demos` is the framework's controls, with its deck at full size and every word readable in each look |
| FX191 | 2026-11-17 | The CV is the framework's controls again, on screen and as an A4 sheet |
| FX190 | 2026-11-17 | The guide's answer keeps its small face on a phone: no font mixes a size with inherit |
| FX189 | 2026-11-17 | Glass's drawings and silk carry their version in their address |
| FX188 | 2026-11-17 | Every WhatsApp button on the contact page says the number |
| FX187 | 2026-11-17 | A button in a column grows to hold its words |
| FX186 | 2026-11-17 | A tile's icon plate is the theme's: white or black in flat, with no shadow |
| FX185 | 2026-11-17 | Flat's hovers are the design's: a card's line changes colour, the buttons and chips answer |
| FX184 | 2026-11-17 | The wordmark's o is the theme's size, smaller in flat |
| FX183 | 2026-11-17 | Flat's headline is solid: a heading's band is a theme value |
| FX182 | 2026-11-17 | Flat's page has no lights, its box no glow, its dark chips the design's grey |
| FX181 | 2026-11-17 | Flat's orb carries the brand's stepped l |
| FX180 | 2026-11-17 | The contact page's offer cards: their buttons in two even columns, the wordmark at their foot |
| FX179 | 2026-11-17 | The alpha flag keeps its words on one line on a phone |
| FX178 | 2026-11-17 | The glass orb's drifting lights blur inside the whole ball, so no straight line cuts it drawn large |
| FX174 | 2026-11-17 | The share card carries the site's own glass orb, light, drawn at 2400 by 1260 |
| FX177 | 2026-11-17 | The way back up is switched off on louis-agent.dev, kept in the framework for later |
| FX176 | 2026-11-17 | The Demos and The framework menus are hidden for now, kept, the menu shows Contact alone |
| FX170 | 2026-11-17 | The header flags alpha-1, and a press says the site may show inconsistencies while it is built |
| FX169 | 2026-11-17 | A section for the brand in the landing's middle, with the theme, nothing in it to pick |
| FX175 | 2026-11-17 | The section index is switched off on louis-agent.dev, kept in the framework for later |
| FX173 | 2026-11-17 | The agent bar is larger, its chips take six places round the closed orb, the agent's ideas first |
| FX172 | 2026-11-17 | The agent bar starts closed as the orb, and its box opens out only while one side needs the other |
| FX171 | 2026-11-17 | The "What we build" page is gone, with its menu link, its sitemap addresses and the guide's lines about it |
| FX168 | 2026-11-16 | The site's name stands left of the agent bar, and its tagline over Louis's LinkedIn right of it |
| FX167 | 2026-11-16 | The landing's demos stand in glass windows with the business's address, and the one picked comes to the front |
| FX166 | 2026-11-16 | The site no longer lags: the words cast no stack of shadows, and a heading's colour stands still |
| FX165 | 2026-11-16 | The agent bar's send is the design's round press, never greyed while it waits |
| FX164 | 2026-11-16 | The pricing page removed and moved to contact, and the guide quotes no price: every quote is made on request |
| FX161 | 2026-11-16 | A closed answer stays closed after a reload: the turn carries that it was closed, saved with the chat |
| FX163 | 2026-11-16 | The glass works as the design's: every panel's glass a value (colour, amount, plain, water or frost) with lens, tilt, pick and reflection switches, the lens ported from the design and seeing the page again, the design's piece states and their reset; the product window, the tape, an ask button and the header's settings bar in the framework; the landing retold, flowing, with no proof band and no price |
| FX162 | 2026-11-16 | The site is the theme's: every sheet of its own removed and its pages configured from the framework's controls in the design page's look (ADR 0125), with the design's controls, cards, films and fan built in the framework, a theme guide at `/framework/theme`, and `lede` and `kicker` renamed `summary` and `topic` |
| FX160 | 2026-11-16 | The hero is the design's: no line above the headline, the design's words, "Ask for yours" and "See what it does", and nothing under them; the window of a chat that stood there is gone, and the second button names its section so the frame moves to it |
| FX159 | 2026-11-16 | The header is white glass, with glass buttons: in glass light the bar was a solid pale band and its switch and CV were drawn by hand; now the bar is white the page shows through over a white edge, the switch is the theme's group and main button, and the way to ask and the CV are its main and quiet buttons |
| FX158 | 2026-11-16 | Each menu in the header opens alone: the dropdown and the frame's menu round it shared the class `site-menu`, and a scoped rule marks only its last part, so a pointer anywhere in the menu opened every list; the dropdown's classes are `site-drop` now |
| FX157 | 2026-11-16 | A visitor whose session a restart forgot is told it is gone, not asked for a key: the door left a session it didn't know to the key's check, which answered 401; now it asks the host, and a session the host doesn't have goes on to its handler's 404, so the page ends the chat with its words for a restart |
| FX156 | 2026-11-14 | The card an agent draws in the chat is glass, in the look every picture wears: it had a navy band, a torn edge and notched sides of its own from before the pictures shared a look; now the look's glass with the accent along its top, a soft light down its head, icon chips and the note in a well, and the brand's `--ticket-*` colours still reach it |
| FX155 | 2026-11-14 | A search engine's showroom has its demos: the host drew `/demos` for a crawler with no demo in it, since its hand-written list of what a page asks for lacked the site's areas; the list is one named class now, with the site's addresses and areas in it, and a test draws every page with it |
| FX154 | 2026-11-13 | The CV prints as its page, on white paper, every link a QR code: Save as PDF gave seven pages in the phone's layout with black margins, a grey cast and a 13 MB file; it is now the desktop layout set for A4 in four pages and 238 KB, the framework's grain and dark canvas are off on paper, and a link is printed as its QR code (`PaperLink`, in the framework's web) |
| FX153 | 2026-11-13 | The CV's editor is behind the admin's key: `/admin/resume` opened for anyone, because it read the CV on the public page's open route and only Save asked for the key; the editor and its card now read it on the admin's own route, so without the key there is only the place to enter it |
| FX152 | 2026-11-13 | A choice with a box asks in the agent's voice, and its answer draws no bubble: the guide's card read "Tell you my business", the tool's own example, over a line in the guide's voice; the example and the toolset guide now make a written choice the agent's ask ("Tell me about your business"), and words sent from its box leave their bubble out, as a click's and a form's do |
| FX151 | 2026-11-13 | The section list marks the section you see most of: on a scrolling page with short sections at its end (Louis's CV), a section whose top could never reach the line under the header was never marked; the list now marks the one filling the most of the screen, and the last at the page's end |
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

## Cleanups (49 done)

| Id | When | What |
|---|---|---|
| CL73 | 2026-11-25 | Every licence of the framework's to MIT (ADR 0135 supersedes 0127): LICENSE, the packages' SPDX name, the README, the notices, the guide's voice; earlier versions keep theirs |
| CL61 | 2026-11-25 | Agent/Turns split, inside FX4 (Louis's pick): the guards in Guards/, the oversized-result summary its own class (Extract Class), their holes named records, the turn ended through TurnEnding.Ends |
| CL69 | 2026-11-22 | The old words left in code: specialist and minion leave everything outside the evals (Louis's pick A), held by OldWordsTests, a ratchet that ended empty; the web texts' 48 holes records, a long tool description as a raw string, C# on LF everywhere |
| CL70 | 2026-11-22 | A session opens in steps: AgentSessions.Create takes one SessionRequest record (Introduce Parameter Object) and hands it to SessionOpening, a method object whose steps run in order, with LookupChecks beside it; the eleven refusals' holes records (Louis's pick A, over a builder and a chain of checks) |
| CL68 | 2026-11-22 | Agents and their subagents, all three children done: no prompts/ wrapper, every agent at agents/{identity}/ with its voice, its skills (SKILL.md folders), its subagents and its lookup agents, each named by its address; ADR 0133, ADR 0021 amended, PartWords (ADR 0132), PromptFolder and PromptFileName |
| CL68.3 | 2026-11-22 | Minions are lookup agents: agents/{identity}/lookups/{name}/voice.md with level: lookup and the base agents/{identity}/lookup.md (Louis's pick B, a folder of their own), RoleLevel.Lookup, LookupAsk, LOOKUP_LLMS, the API's lookups, the chat's Lookup agent and every word the model reads; ADR 0021 amended |
| CL68.2 | 2026-11-22 | Subagents inside their agent (ADR 0133, supersedes 0017): a specialist is a subagent at agents/{identity}/subagents/{name}/voice.md, its address (triage.sorter) its one name everywhere, its level from its header held to its place by a test; the prompt folders and files are shared enums (PromptFolder, PromptFileName); the API, the chat, the framework pages and the docs say subagent |
| CL68.1 | 2026-11-22 | No prompts/ folder: every project's prompt folders at its root, agent/ is agents/, the showroom's businesses named once on one provider, each skill a folder with its SKILL.md; a page's words from a dictionary factory, PartWords (ADR 0132) |
| CL64 | 2026-11-18 | Orbi's prompts in the skill shape (narrowed by Louis to the guide alone): the voice is an index, its seven rules first and a table of on-demand skills, 4,356 words to about 3,100; five procedures became skills; every skill, Orbi's and Claude Code's, has its rules first, an example and its references, held by tests |
| CL67 | 2026-11-17 | The feeds' MCP scheme is `feeds://` (Louis: "A feed must change to Feeds"), plural like `/v1/feeds` and the docs' coming `docs://`; no alias for `feed://` (ADR 0049); ADR 0072's line changed with it |
| CL65.5 | 2026-11-15 | The pictures a project needs, and CL65 done (Louis: "the project type and requirements must drive which diagrams is required", "placed where it is used ort needed not a section"; his pick of three): every project doc states its kind from a list of nine and what it shows beyond the kind's floor, a test holds it to exactly that and the guide's table to the floor; 27 missing pictures drawn from the code, 8 deployment, 5 nested, 14 activity; no Diagrams section, each picture in its story; the `components` kind is `nested`; the `ticket` kind's name left open as CL66 |
| CL65.4 | 2026-11-15 | What the old looks left (with its parent, Louis: "approve go unattended"): the engine holds only what a picture is drawn with today; the two layout rules nothing drew by are gone and the strategy round them is two static classes; the rules of 26 classes no drawing writes, a banner and three colours are out of the style every picture carries; the kinds' styles stand in one folder and what two kinds styled alike is styled once; a path is written one way and an arrow keeps its exact numbers; a fade is a small type named beside its style; three rule tests hold it, and each rewrite of every picture was proved by taking its one change out again; the names of the `components` and `ticket` kinds put to Louis, neither renamed |
| CL65.6 | 2026-11-14 | The engine asks its types (Louis: "can we not have something like a calculator which does it neatly", "we cannot write spaghetti code", "keep ours and MS's but use ours."): a box and a point behind one geometry, in two families, ours on generic math in `double` and in use, Microsoft's on `RectangleF` and `Vector2` kept; a face for a width of words and a path writer for a path; every placed record holds a box, the geometry handed in from the list of renderers; a ratchet that shrank to four plain rules and a fifth; the docs' tool wrote none of the 269 pictures after each move, and a test draws each with both families |
| CL65.3 | 2026-11-14 | Every picture says its type (Louis: "our graph types must match", "the type of diagram must always display"): a `type` beside the kind, UML's names and our own `overview`; a kind that is one type gets it filled in, a flow says which of eight it is or is refused; every kind shows its type at the right of its title's line; the 95 flows read and typed; each redrawn picture held against the one before with that line taken out |
| CL65.2 | 2026-11-14 | The picture builder (Louis: "then use builder pattern for implementation"): one fluent `PictureBuilder` puts six kinds' pictures together, each kind's drawing its director, the four numbers that differ a Parameter Object; one placer for a key's rows where four kinds had the same loop; each renderer beside its drawing; a rule test that no drawing writes its own title, light or caption; no picture changed by a byte |
| CL65.1 | 2026-11-14 | The diagrams as two projects (Louis: ".diagrams must be split into a diagrams --> and what builds the diagrams"): `LouisAgent.Diagrams` keeps what a diagram is, its kinds, records, reader and refusals, and `LouisAgent.Diagrams.Engine` holds what builds one, 130 files moved with their history; the one message enum became two, one formatter filling both; a rule test holds the direction; no picture changed by a byte |
| CL63 | 2026-11-13 | Claude's instructions, by when they are needed: `CLAUDE.md` keeps working with Louis and the hard stops in one list, the design rules and the map are always-loaded rules files, eight rules files load by kind of file (the tests' and the hosts' follow their files wherever they live), nine skills hold the procedures, and 22 standing instructions moved from Claude's notes into the repo in Louis's words. Eight `CLAUDE.md` files became three, and the *Finish a step* guide folded into its skill |
| CL60.5 | 2026-11-12 | Four diagrams on every project doc, and CL60 done: each of the 42 project docs shows its overview, a flow, a sequence of its main case and its interface design, 42 interface pictures, 8 flows and 13 sequences written from each project's code and looked at; `EveryProjectDoc_ShowsItsFourDiagrams` holds it, and both project-doc tests now see the demos' docs. CL60 as a whole: every diagram in the docs is data our own renderer draws (ADR 0118), no Mermaid left |
| CL60.4 | 2026-11-12 | The classes, and the rule: a `classes` kind in the diagrams library (types with members, their relations: inherits, implements, holds, uses, creates) drawn as a flow through an Adapter, a flow box's members under a divider, the import reading Mermaid class diagrams, and all 11 class diagrams swapped for pictures (F39's and F40's split in two); `NoDoc_HoldsMermaid` holds that no doc has Mermaid of any kind, ADR 0118 the rule, and the plans README asks for the four diagrams as pictures |
| CL60.3 | 2026-11-12 | The sequences: a `sequence` kind in the diagrams library (participants with icons and tones, steps as a Composite of records: messages, replies, notes, loop, opt and alt with else, nested), its layout, its renderer in the flows' look, the import reading Mermaid sequences, and all 45 sequence diagrams in 44 docs swapped for pictures, each looked at; `DocsDiagramsTests` holds their words inside and no sequence left |
| CL60.2 | 2026-11-12 | The flows: the layout grown (columns from the arrows, labels over lines, a down direction, groups and dashed arrows, waypoints, tracks and detours), Louis's joined look (a title bar, an icon and a tone per box, arrows and word chips in their tone, a legend, a note on every chart), a one-time import, and all 75 Mermaid flowcharts in 58 docs swapped for pictures, each looked at; `DocsDiagramsTests` holds clear words and lines and no flowchart left |
| CL60.1 | 2026-11-11 | The renderer and the overviews: `LouisAgent.Diagrams` (plain C#, the flow engine moved in, cards in Louis's style with a highlight and a heading look, 22 Tabler icons, one SVG with a light and a dark look), the `LouisAgent.DocDiagrams` tool, `DocsDiagramsTests`, and an overview picture on all 42 project docs and two of Louis's figures |
| CL58 | 2026-11-07 | A component's styles sit beside it (ADR 0094): `X.razor.scss` scoped and bundled by Blazor's CSS isolation for the accounts, the admin, the studio (64 files) and the chat (21); the globals a site's own pages draw or a theme overrides stay partials; accounts/, web/admin/ and their forwards gone; the snapshot helper and the stylesheet helper know the scope and the bundle |
| CL31 | 2026-11-07 | The framework's scripts as ES modules beside their components, imported through JsModule (ADR 0093); chat.js and passkeys.js gone from every site's head; the download the framework owns |
| CL57 | 2026-11-07 | What a project holds (ADR 0092): the API off core.web, the runners' programs hand in services, Rider to the ACP server, the SDKs as core.anthropic/ollama/openai, the eval studio as api.evals and web.evals, the hosts lose -server, a starter API; CL51 with it |
| CL33 | 2026-11-07 | Core's Web/ and Telemetry/ as LouisAgent.Core.Web and LouisAgent.Core.Telemetry, on core behind a seam each; a host composes through HostServices |
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
| 2026-11-13 | Finishing means the write-up, the touched projects' docs and their diagrams, and a test holds the write-ups |
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
[Docs index](README.md) · Related: [Fixes](fixes/README.md) · [TODO](../../TODO.md)
