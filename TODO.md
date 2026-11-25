# TODO

> **Plan.** What is still open, one row per item, with its id, a few words on what, its phase and where its detail
> lives. **Elsewhere:** what was finished in the [work log](src/docs/work-log.md); the why of the order in
> [the roadmap](ROADMAP.md); the goal it all works toward in [GOAL.md](src/docs/GOAL.md).

**The end product:** four offers on one engine, in [the roadmap](ROADMAP.md): **a website with an agent**, which
louis-agent.dev sells today (Louis, 2026-11-09: "Our goal is to sell websites with agents, and custom agents, for
now"); **the engine itself**, the application framework for AI agents in .NET, for teams that build their own;
**a configured agent**, an identity and its packs hosted for a business; and **Puls**, the public South African
assistant. **The rows are in build order** (Louis,
2026-11-07: "focus on the framework first then build the agents"): the framework's track, the demand check that runs
meanwhile and the small leftovers under *Now*; then the configured agents' phases under *Next*; then *Later*, and the
*Cleanups* each done when its code is next touched. **The first rows make the framework ready for production, before
the products built on it** (Louis, 2026-11-22: "to get it ready production ready for apps to be built on it", "The
framework production ready"): the old words renamed, the evals, a photo or a PDF reaching a tool, durable sessions,
then append-only history, reliability, budgets, drafts and approvals, safety and audit, and the builder; the bill
checker's other children and the Google suite come after them. The work log's open table keeps the same order (`WorkLogTests`).
Puls's own work, and the few framework rows only Puls needs, are in [Puls's TODO](https://github.com/bosbevk/puls/blob/main/TODO.md),
with the ids they had here.

**How this list works.** Every open item is one row with an id (`F` a feature, `FX` a fix, `CL` a cleanup); a child
(`F61.4`) is named in its parent's plan. The detail of an item lives in its plan or feature doc, written before its
first step ([src/docs/CLAUDE.md](src/docs/CLAUDE.md), rule 3), never here; the notes the old list carried are at the
tag `pre-work-log` (2026-11-07). When an item is finished its row moves to the [work log](src/docs/work-log.md). The
known limitations below are facts about today, not tasks.

## Open (108)

| Id | What | Phase | Detail |
|---|---|---|---|
| F171 | The bill checker (Louis, 2026-11-21; 2026-11-22: "this feature is next"): a phone app that reads a Western Cape municipal account from a photo or a PDF, checks every line against Cape Town's, Swartland's and Overstrand's own tariffs and drafts the dispute, free to download with one subscription; the product in its own private `bill-checker` repository on the framework's packages, the framework's parts here: F171.1 a photo or PDF reaches a tool (done 2026-11-24); F171.2 the bills pack reads and checks; F171.3 the dispute letter; F171.4 the identity and evals; F171.5 the account, consent and our own subscriptions part; F171.6 the phone app; F171.7 the stores; F171.8 the monthly report | Now, F171.2 on after F140 | [plan](src/docs/plans/F171-the-bill-checker.md) |
| F43 | Accounts, tenants and durable sessions; its first child is durable sessions (a session store behind one seam, with an in-memory and a MongoDB provider, and a session that resumes after a restart), built after F171.1 and before F157 (Louis's pick, 2026-11-22, after an outside comparison with Microsoft Agent Framework, LangGraph and OpenAI's Agents SDK found sessions lost on a restart the biggest gap) | Now, F43.1 durable sessions | [F43.1](src/docs/plans/F43.1-durable-sessions.md) |
| FX4 | Append-only history | Now, after F43's first child | [fix doc](src/docs/fixes/FX04-append-only-history.md) |
| F8 | Reliability | Now, after FX4 | [feature doc](src/docs/features/F08-reliability.md) |
| F7 | Cost you can see and stop | Now, after F8 | [feature doc](src/docs/features/F07-budgets.md) |
| F41 | Drafts and approvals | Now, after F7 | — |
| F42 | Safety and audit | Now, after F41 | — |
| F140 | A host names its parts with one builder (Louis, 2026-11-12, after ADR 0120: "is there a feature to move the core to a builder pattern?", "do it"): `builder.AddLouisAgent(catalog, agent => agent.Providers(…).Web().Telemetry().Api().Accounts(…).Evals().Site(…))` and `app.UseLouisAgent()`, the middleware in its safe order (accounts before the key) and every route mapped, so no host lists providers, orders middleware by a comment, or re-registers the site's words for the prerender; each part an area a library adds, as on the web side; its own plan | Now, after F42 | [ADR 0120](src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md) |
| F157 | The Google integration suite (Louis, 2026-11-17: "let's go for full google integration suite"): a business connects its Google once (OAuth, offline token in Identity's table, per tenant), then its agent reads the sheets the owner picked (F157.2) and puts booking requests in its calendar for the owner to confirm (F157.3); an Integrations section on louis-agent.dev with the showroom's guesthouse (F157.4); after the host keeps its data-protection keys (a fix row to come); four children, with Drive, Gmail and visitors' own Google as F173 to F175 | Next, after F171 | [plan](src/docs/plans/F157-google-integration-suite.md) |
| F172 | What we are building, and what just landed (Louis, 2026-11-22: "a section on the framework and public site which displays what we are actually busy working on and what was implemented new recently"): a `/now` page on both sites, busy with as status tiles and shipped lately by day, read live from GitHub's TODO, work log and commit scopes by each site's host and kept 15 minutes, the framework's Overview showing a band; ten steps | Now | [plan](src/docs/plans/F172-what-we-are-building.md) |
| F158 | A chart kind in the docs' diagram tool (Louis, 2026-11-17, of the stream probe's fluency graph, his pick B of three): a line and bar chart drawn from JSON in the pictures' own look, so a measured graph is drawn like every other picture (ADR 0118) | Next | — |
| F159 | The stream probe as a tool (Louis, 2026-11-17: "and the probe under tools", "store test results"): `src/tools/LouisAgent.StreamProbe` asks a site's guide with pacing on and off, stores each run's timings and the fluency chart's JSON in the docs, drawn by the tool; after F158 | Next | — |
| F151 | Two themes a visitor picks between, glass and flat, each light and dark, remembered by the browser; each theme a package of values a site opts into, worn by every page and component we have, with the agent bar on it (Louis, 2026-11-15: "you will be able to choose your theme, it defaults to glass, light", "2 themes, 2 styles light and dark", "we should just have the user pick"); eight children: the contract, the two packages, the two switches, the framework's components, the libraries and the other sites, louis-agent.dev's pages, the pictures, and the demos recorded again | Now, next | [plan](src/docs/plans/F151-themes-and-styles.md) |
| F148 | One docs endpoint (Louis, 2026-11-13, after the guide could not say whether the README it read was GitHub's: "can we plan a docs api end point all docs must be served from that endpoint. this is next"; his pick of three: the words stay markdown, the facts are generated): every doc gets a small header (type, title, about), one docs library behind `/v1/docs` builds its list from the headers, every doc with its path, commit and GitHub address; the guide's tools and the MCP `doc://` resources read the same library; three small choices asked one at a time | Now, after F151 | [draft](src/docs/plans/F148-one-docs-endpoint.md) |
| F150 | Every doc follows its kind's pattern (Louis, 2026-11-13: "Project docs should all follow same pattern", "all docs", "all our docs must be layered", "each with their template"): the docs in five layers (the front door, the index, the whole, one project, the why), one template per kind of doc in `src/docs/templates/` (project design doc, ADR, how-to, write-up, plan, fix doc), the same sections in the same order, a fix's lesson one file each in `learning/fixes/` (his pick), held by a test that reads the templates and whose list of docs that don't fit yet can only shrink; the framework's and the packs' docs only, the demos' left as they are ("only caring about the framework docs at this point no implementation docs, and offcourse packs"); a docs folder per owner (his: "in the case of .site, it will get its own docs folder, of it's stuff and ADR's", "all docs must work like this"), so `src/site/docs/` is new and the ADRs that are the site's, a pack's or Puls's move out of `src/docs`; the ten feature docs fold into six draft plans (his pick) and `features/` and `specs/` go; the 34 project docs first, then the guides and the write-ups; its own plan | Now, after F148 | [the patterns](src/docs/plans/F148-one-docs-endpoint.md#the-doc-design-one-pattern-per-kind-of-doc) |
| F149 | The docs' lists are generated (Louis, 2026-11-13: "regenrate graphs, regenrate docs, that way both are always the latest"; his pick: only what a generator can own): the project tables, the settings table and the indexes written from the code and the docs' headers by the docs' tool before each PR, and checked in CI as the diagrams are; a project doc's fact sections come from the code's comments, around a short hand-written story (his pick, 2026-11-14); its own plan | Now, after F150 | [the pick](src/docs/plans/F148-one-docs-endpoint.md#picked-louis-2026-11-13) |
| F146 | A business's agent reports back by address (Louis, 2026-11-12: "the role should know to tell the guide back via api(Address)", "the user should still be able to take over, it is just i would like the guide to know everything"): a business's `window` role tells the guide everything with `ReportBack` over the API; the visitor still takes over in the window; its own plan | Now | — |
| F147 | Page squares under the guide's choices (Louis, 2026-11-12: "3 squres there with take me to a different page suggestion", "grouped"; his pick: the guide picks): three grouped "go to" squares, the pages the guide picks; its own plan | Now, after F146 | — |
| F145 | Who a talk was with (Louis, 2026-11-13: "can we display the ip of the person?"; his pick of three: the full IP, "a privacy policy page, and display accept if needed"): the turn log keeps the visitor's IP (the forwarded one on Azure), the overview shows it, a retention rule, and the privacy page told about both (the page itself came in F145.1); its own plan | Now, after F147 | — |
| F139 | A site draws its own cards (Louis, 2026-11-12: "each site should be able to have it's own recepit svg and form svg and card svg. so the fraemwork is only placeholders for the svg", "when you click send, it must be replaced inline by sent, this must be a web framework feature"; his picks: a component per slot, an SVG frame with real inputs, Send is the yes): the framework keeps a card, a receipt and a form slot, each with a plain default, and a site names its own Razor component with its SVG; the form turns to Sending, then into the receipt in its place; louis-agent.dev's "rail" look; the flow's prompts (FX143's rest); its own plan | Now | — |
| F141 | Every part of the framework's web is ready for an agent (Louis, 2026-11-12, during F127.7: "all our framework must work like this, even our account pack this must be a framework rule", "that way we can attach an agent to it easily, even a helper which can explain admin functions then"): the account pages and the admin name their focus and their pieces can be selected, as F127.7 makes the site's pages, so an agent attaches with no change to the pages; and an admin helper that explains the admin's functions; its own plan | Now | — |
| F138 | The guide explains with drawings (Louis, 2026-11-12, during F127.4's plan: "we should just give the guide access to the whole diagrams, so it can also explain with drawings"; his pick: its own feature, after F127.4): a tool that finds a docs diagram and gives the model its JSON so it understands the picture, and the widget draws it in the chat with `LouisAgent.Diagrams`; its plan also settles whether the guide may draw its own (a flow, sequence or classes JSON written on the spot, refused by the reader with its reason when wrong); its own plan | Now | — |
| F132 | Found for the searches business owners make (Louis, 2026-11-10, of a search for "ai agents for my website": "is our seo geared for a search like this?"; his pick of three): a page for "AI agent for your website" (South Africa, from R500, what it does, an FAQ with FAQPage and Service data), the three demos titled for their business searches ("AI agent for a guesthouse website"), and "AI" in the landing's title; the long and local searches first, since the head term is ads and big brands; its own plan | Next, after F125 | — |
| F130 | The subdomains, together (Louis, 2026-11-10: "demos. and framework. together, with a redesign of the framework website"), a parent with three children: F130.1 the host by name (one app answering several host names: DNS and certificates, a page per host, canonical links, sitemap and robots per host, old addresses redirected for good; about 4 steps), F130.2 `demos.louis-agent.dev` (F127.1's showroom at its root, the guide's page index and the links moved; about 3 steps), F130.3 `framework.louis-agent.dev` redesigned (what the framework is, how it works and how to build on it, for developers, while `louis-agent.dev` sells; the framework's sections move there; research, mockups and Louis's picks first); each child its own plan | Next | — |
| F106 | The framework's track, before the agents | Now | [draft](src/docs/plans/F106-framework-track.md) |
| F74 | Packaging the engine in place, then its own repo after the engine features | Now | [draft](src/docs/plans/phase-10-engine-package.md) |
| F44 | The engine as a product | Now | [feature doc](src/docs/features/F12-response-speed.md) |
| F39 | Orchestration | Now | [feature doc](demos/louis-agent/docs/features/F17-orchestrator-agent.md) |
| F62 | Validate demand | Now | — |
| F63 | The reference transcripts in git | Now | — |
| F64 | The judge check can't see pages | Now | — |
| F66 | Each pack declares what it needs | Now | — |
| F67 | A whole menu | Now | — |
| F69 | The framework's `_Imports.razor` is about 90 lines | Now | — |
| F71 | At the next run, read the hand-off lines | Now | — |
| F73 | Check the 47 new own-prices rows | Now | — |
| F125 | An agentic site: docs the standard way and public over MCP, buttons and cards in the guide, gaps, live demos, tools over MCP and discovery | Now | [plan](src/docs/plans/F125-an-agentic-site.md) |
| F168 | Facebook auto-answers (Louis, 2026-11-21: "plan integration into facebook to auto answer questions"): the guide answers the Page's Messenger in his voice through a framework library, `louis-agent.api.messenger`, over the sessions API with its own key and a daily cap per person, and steps back when Louis answers himself; a data-deletion callback; eleven steps, after F126 | Now, after F126 | [plan](src/docs/plans/F168-facebook-auto-answers.md) |
| F169 | The framework's site, as the design review picked it (Louis, 2026-11-21: "review from a branding and look and feel perspective the framework site design and flat themeing", then "I want you to rhoroughly plan this"): his picks on thirteen findings, built in seven children. F169.1 flat's lines and inks: every text on a flat page at 4.5 to 1 or more, the ring at 3 to 1, held by a test (done 2026-11-21); F169.2 the docs page's type and edges: three heading sizes, 74, 40 and 28 (40, 32 and 24 at 390 wide), the Overview's families h3s under one h2, four corner names and a step's six set by every theme, Orbi's panel at 18, no step on a disc in flat, the rows of terms and the page turn ending on the page's edge (done 2026-11-21); F169.3 the brand's parts: the mark the only ball, the name's ball one size, the footer's three links (done 2026-11-22); F169.4 flat has no depth: no shadow, blur or gradient on a flat page, glass's talk on its own small shadow (done 2026-11-22); F169.5 the phone: no page scrolling sideways at 390 wide, every picture read without zooming (done 2026-11-22); F169.6 a card for each page: a link to a page previews that page's own card (done 2026-11-22); F169.7 the sequence drawn for a phone: on a 390 screen the sequence on How it works reads without sliding (done 2026-11-22) | Now | [plan](src/docs/plans/F169-the-design-reviews-picks.md) |
| F131 | The chat on any website: one script tag opens our widget in a frame from louis-agent.dev on WordPress, static HTML or any site, nothing lost; a door per site, sites per tenant (after F43), a monthly rand budget (after F7), our own WordPress plugin, an answer that survives a page change; seven children | Now | [plan](src/docs/plans/F131-the-chat-on-any-website.md) |
| CL66 | The `ticket` kind's name, for Louis: since FX156 it is drawn as a glass card (Louis: "glass but not a ticket"), and kept for now (CL65, unattended). Three options, in CL65's plan as of commit a9a875cda: `chat-card`, `card` with the `cards` kind renamed `overview`, or keep it, with what each costs | Later | Nothing to build until Louis picks |
| F137 | The site's card content as JSON (Louis, 2026-11-11, during CL60.1: "render our website cards with the info this way? that way our pages is built by json which the ai can then easily read"; his yes to cards and lists only, prose pages stay Razor): the demo cards, the pricing, what it does and the custom-agent steps read from JSON beside their pages and drawn by one card component, so an agent that edits the site changes data, not markup; reuses CL60's card records; groundwork for F133; after CL60, its own plan | Next | — |
| F133 | A website with an agent, set up by talking to it (Louis, 2026-11-11: "How about I build a wix with my platform"): not a Wix, but one record per business that draws both its site and its agent, three templates (guesthouse, restaurant, practice), a setup pack that writes the record as the owner talks, preview then checkout then live on its own host name; builds on F131, F43, F130.1 and F7; seven proposed children, four open choices (the record's shape before F131.1 starts) | Next | [draft](src/docs/plans/F133-self-serve-sites.md) |
| F152 | The showroom's demos, fuller, each part Louis's pick: a business's chat draws a card for the place its agent opened, as the start's window shows it (Louis, 2026-11-15: "like the inner agent do not do this?"), and the three websites gain sections, which means recording their conversations again (ADR 0115) | Next | — |
| F143 | The guide drives Louis's CV (Louis, 2026-11-13: "the agent should also drive my CV", "split it in 2 features"): a generic resume pack with `GetResume` over the same `cv.json`, `/cv` in the place index, a hiring focus skill (facts only from the tool, the honest no, an interview through the leads pack, no salary or puffery), an eval of eight conversations; two open choices asked when it starts; after F142 | Next, after F142 | [plan](src/docs/plans/F143-the-guide-drives-the-cv.md) |
| F46 | Localisation | Next | — |
| F40 | The sales playground | Next | — |
| F36 | MCP servers as packs | Next | — |
| F15 | Model limits and local models | Next | [feature doc](src/docs/features/F15-model-limits.md) |
| F108 | The admin portal is a role of a signed-in person, not a key: ADMIN_API_KEY goes once an account can carry the admin role (Louis, 2026-11-08: "should this not be a role?") | Next | — |
| F51 | Hosting for public traffic | Next | — |
| FX59 | binary attachments sent as files, not text | Later | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F22-role-tools.md) |
| FX60 | a continued specialist's ledger rows name the turn that continued it | Later | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F26-specialist-follow-up.md) |
| FX61 | A site whose host has no `ACCOUNTS_SQL_URL` still calls `/account/me` and `/account/providers`, gets `index.html` back and dies on the JSON (the starter's login and chat pages show Blazor's error bar); the client should take a host without accounts as "nobody signed in" (found in CL58) | Fixes | — |
| F90 | WhatsApp as a host | Later | — |
| F92 | Voice: the person talks, our API turns it into text behind the chat's seam, the text lands in the box to check (Louis's pick B, 2026-11-11) | Later | [draft](src/docs/plans/F92-voice-input.md) |
| F45 | Knowledge and retrieval | Later | — |
| F94 | Searching PDFs, investigate much later | Later | [spec](src/docs/specs/PDF_SEARCH.md) |
| F47 | The CLI and Rider's host on the agent API | Later | — |
| F9 | Long chats | Later | [feature doc](src/docs/features/F09-context-management.md) |
| F10 | Settings per role and per session | Later | [feature doc](src/docs/features/F10-route-settings.md) |
| F95 | What's left of CL29 | Later | [doc](src/docs/adr/0076-core-ships-no-agent.md) |
| CL17 | what's left of the coding identity | Later | — |
| F96 | The browser's user agent names the user's town per visit | Later | — |
| F97 | A tool added mid-chat, after FX4 | Later | — |
| F38 | Tool search, when a measured need comes | Later | — |
| F136 | How much of an asked agent's work the asker reads, as a framework choice (Louis, 2026-11-11, during F127.3.1: "AskAgentWithTools and AskAgentWithToolsAndThinking"; "we will not use it"): its tool calls, or those and its thinking as text, beside the answer; three options weighed then (one `ReadAgentWork` with a detail level, three tools, one `show` parameter); when a framework user needs it | Later | — |
| F98 | Planning and a self-check before answering, tried on a simulation first | Later | — |
| F99 | An error tracker behind `IErrorFeed`, after F39 | Later | — |
| F100 | A webhook trigger beside polling, after F39 | Later | — |
| F101 | Computer use, when a simulation needs it | Later | — |
| F102 | An Inspect AI log export for a run | Later | — |
| CL21 | Mutation testing with Stryker.NET | Later | — |
| F103 | Function skills in the Agent Skills format | Later | [doc](src/docs/adr/0063-a-prompt-says-what-it-is.md) |
| CL23 | Explore a Roslyn analyzer for the names rule | Later | [doc](src/docs/adr/0043-repeated-names-are-enums.md) |
| F30 | F30's minions tested | Later | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F30-local-web-peer.md) |
| F104 | Build mode, a side note | Later | — |
| F173 | Google Drive for a business's agent: a `google-drive` toolset in `LouisAgent.Packs.Google` on `drive.file` only, the files the owner picked or the app made; was F157.5, planned when a buyer asks | Later | — |
| F174 | Gmail for a business's agent: a `google-gmail` toolset, a reply drafted for the owner to send, never sent by the agent; restricted scopes, so Google's security assessment comes first; was F157.6, planned when a buyer asks | Later | — |
| F175 | Visitors connect their own Google: a try-it on the Integrations section with a visitor's own sheet, once Google has verified the app, with a privacy notice (POPIA, F145); was F157.7, after F157.4 | Later | — |
| CL34 | The work email out of git history | Cleanups | — |
| CL35 | `Web/Browsing/RedirectBodies` goes when a PuppeteerSharp release awaits its own redirect body ta… | Cleanups | — |
| CL36 | Design docs for the four projects that have none | Cleanups | [doc](src/docs/projects/README.md) |
| CL37 | The orchestrator's `Program.cs` up to the rules | Cleanups | — |
| F16 | F16-S1: the oversized-results report, and the worst offenders paged | Cleanups | — |
| CL38 | `VerifyQuote`'s prices in the host's currency | Cleanups | — |
| CL39 | Remove CL14's last aliases now, with no deploy to wait for | Cleanups | [doc](src/docs/adr/0049-nothing-is-compatible-before-a-sale.md) |
| CL40 | Typed peer bodies | Cleanups | — |
| CL41 | Choices still made by string, the LLM provider first | Cleanups | — |
| CL42 | The session request bodies | Cleanups | — |
| CL43 | An OpenAPI document for the agent API | Cleanups | [doc](src/docs/adr/0039-api-routes-under-v1.md) |
| CL44 | SearXNG on only on purpose | Cleanups | — |
| CL45 | SearXNG's results filtered to the site asked for | Cleanups | — |
| F31 | F31's review follow-ups | Cleanups | — |
| CL46 | Web tests | Cleanups | — |
| CL47 | The portal's old benchmark routes and words removed, after Phase 3 | Cleanups | — |
| CL48 | Remove the `/v1/admin/benchmarks/…` route aliases | Cleanups | — |
| CL52 | The rest of the size list | Cleanups | — |
| CL53 | A price format per country | Cleanups | — |
| CL54 | A pack can't add itself to a router | Cleanups | — |
| CL55 | Does an unanswered request belong in the usage ledger? A cancelled request is a record of 0 toke… | Cleanups | — |
| CL61 | `ToolCallGuards` is over 200 lines once Rider lays out its message holes, and `Agent/Turns` holds six files: split it by responsibility (the oversized-result summary its own class, the folder into subfolders), its holes into records (ADR 0119), and have it end the turn through `TurnEnding.Ends` as `TurnRunner` does (found in F127.4.3 step 3) | Cleanups | — |

## Known limitations

Facts about today, not tasks; the one list (the development guide and the docs link here). When work removes one,
delete it here (see [Finishing a feature](src/docs/features/README.md#finishing-a-feature)). Each pointer names the item
above that removes it.

1. **Rider MCP discovery only logs** Rider's tools; they aren't callable yet (needs a full MCP client).
   → **F36** MCP servers as packs (*Next*, Phase 8); its MCP client is built first, for the guide, in F125.8
   ([plan](src/docs/plans/F125-an-agentic-site.md)).
2. **Sessions are in memory** in the API and ACP server; a restart ends them (the web app keeps the transcript).
   → **F43** Accounts, tenants and durable sessions (*Now*, its first child after F171.1;
   [session architecture](src/docs/specs/SESSION_ARCHITECTURE.md)).
3. **Ollama tool support is a name heuristic** (`LlmOptions.KnownNoToolsPrefixes`).
   → [**F15** Model limits and local models](src/docs/features/F15-model-limits.md) (the capability check per model).
4. **No rate limiting or audit log** beyond the file logs. → **F42** Safety and audit (*Now*, Phase 7).
5. **10 tool rounds per message** (`MaximumIterationsPerRequest`): a long task stops mid-way and needs a follow-up
   message to continue. The orchestrator does this automatically ("Continue the fix…"); most demo fixes take 2-3
   messages. → [**F10** Settings per role and per session](src/docs/features/F10-route-settings.md) (*Later*).
6. **Running the API with `dotnet run` (unpublished)** only serves the web app in the `Development` environment
   (`ASPNETCORE_ENVIRONMENT=Development`); otherwise `/` returns 404. The Docker images are published, so they're fine.
7. **Local models get a truncated prompt.** The agent sends ~26,000+ tokens (system prompt and tools) and doesn't tell
   Ollama the window, so Ollama uses its own default (2,048 on the version measured here; newer ones pick 4k–256k by
   memory) and drops the rest without an error: the ledger showed `input: 2050`. The `ollama-nvidia` and `ollama-amd`
   containers default to 32k, which covers it; the CPU `ollama` service still uses Ollama's default.
   → [**F15** Model limits and local models](src/docs/features/F15-model-limits.md) (sends `num_ctx`; takes in F13).
8. **Changing the tools or skills during a chat can break it on Claude Haiku 5.5** (the default). The model rejects a
   changed system prompt or tool list once thinking blocks are in the history (enforced for accounts created on or
   after 2026-08-31). `BeginTurn` sends the engine's live tool list, so a tool the model creates (`CreateTool`) breaks
   the rest of that message; `/approve`, `/reject` and a skill reload break the next one; and in the API every open
   chat with the same toolsets shares one engine and its tool list (F5). Start a new chat afterwards.
   → [**FX4** Append-only history](src/docs/fixes/FX04-append-only-history.md) (first in Phase 7).
