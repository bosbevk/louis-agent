# TODO

> **Plan.** What is still open, one row per item, with its id, a few words on what, its phase and where its detail
> lives. **Elsewhere:** what was finished in the [work log](src/docs/work-log.md); the why of the order in
> [the roadmap](ROADMAP.md); the goal it all works toward in [GOAL.md](src/docs/GOAL.md).

**The end product:** three offers on one engine, in [the roadmap](ROADMAP.md): **the engine itself**, the application
framework for AI agents in .NET, for teams that build their own; **a configured agent**, an identity and its packs
hosted for a business; and **Puls**, the public South African assistant. **The rows are in build order** (Louis,
2026-11-07: "focus on the framework first then build the agents"): the framework's track, the demand check that runs
meanwhile and the small leftovers under *Now*; then the configured agents' phases under *Next*; then *Puls*, *Later*,
and the *Cleanups* each done when its code is next touched. The work log's open table keeps the same order
(`WorkLogTests`).

**How this list works.** Every open item is one row with an id (`F` a feature, `FX` a fix, `CL` a cleanup); a child
(`F61.4`) is named in its parent's plan. The detail of an item lives in its plan or feature doc, written before its
first step ([src/docs/CLAUDE.md](src/docs/CLAUDE.md), rule 3), never here; the notes the old list carried are at the
tag `pre-work-log` (2026-11-07). When an item is finished its row moves to the [work log](src/docs/work-log.md). The
known limitations below are facts about today, not tasks.

## Open (119)

| Id | What | Phase | Detail |
|---|---|---|---|
| F139 | A site draws its own cards (Louis, 2026-11-12: "each site should be able to have it's own recepit svg and form svg and card svg. so the fraemwork is only placeholders for the svg", "when you click send, it must be replaced inline by sent, this must be a web framework feature"; his picks: a component per slot, an SVG frame with real inputs, Send is the yes): the framework keeps a card, a receipt and a form slot, each with a plain default, and a site names its own Razor component with its SVG; the form turns to Sending, then into the receipt in its place; louis-agent.dev's "rail" look; the flow's prompts (FX143's rest); its own plan | Now, after F127 | — |
| F140 | A host names its parts with one builder (Louis, 2026-11-12, after ADR 0120: "is there a feature to move the core to a builder pattern?", "do it"): `builder.AddLouisAgent(catalog, agent => agent.Providers(…).Web().Telemetry().Api().Accounts(…).Evals().Site(…))` and `app.UseLouisAgent()`, the middleware in its safe order (accounts before the key) and every route mapped, so no host lists providers, orders middleware by a comment, or re-registers the site's words for the prerender; each part an area a library adds, as on the web side; its own plan | Now, after F139 | [ADR 0120](src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md) |
| F141 | Every part of the framework's web is ready for an agent (Louis, 2026-11-12, during F127.7: "all our framework must work like this, even our account pack this must be a framework rule", "that way we can attach an agent to it easily, even a helper which can explain admin functions then"): the account pages, the admin and the eval studio name their focus and their pieces can be selected, as F127.7 makes the site's pages, so an agent attaches with no change to the pages; and an admin helper that explains the admin's functions; its own plan | Now, after F127 | — |
| F138 | The guide explains with drawings (Louis, 2026-11-12, during F127.4's plan: "we should just give the guide access to the whole diagrams, so it can also explain with drawings"; his pick: its own feature, after F127.4): a tool that finds a docs diagram and gives the model its JSON so it understands the picture, and the widget draws it in the chat with `louis-agent.diagrams`; its plan also settles whether the guide may draw its own (a flow, sequence or classes JSON written on the spot, refused by the reader with its reason when wrong); its own plan | Now, after F127.4 | — |
| F132 | Found for the searches business owners make (Louis, 2026-11-10, of a search for "ai agents for my website": "is our seo geared for a search like this?"; his pick of three): a page for "AI agent for your website" (South Africa, from R500, what it does, an FAQ with FAQPage and Service data), the three demos titled for their business searches ("AI agent for a guesthouse website"), and "AI" in the landing's title; the long and local searches first, since the head term is ads and big brands; its own plan | Next, after F125 | — |
| F130 | The subdomains, together (Louis, 2026-11-10: "demos. and framework. together, with a redesign of the framework website"), a parent with three children: F130.1 the host by name (one app answering several host names: DNS and certificates, a page per host, canonical links, sitemap and robots per host, old addresses redirected for good; about 4 steps), F130.2 `demos.louis-agent.dev` (F127.1's showroom at its root, the guide's page index and the links moved; about 3 steps), F130.3 `framework.louis-agent.dev` redesigned (what the framework is, how it works and how to build on it, for developers, while `louis-agent.dev` sells; the framework's sections move there; research, mockups and Louis's picks first); each child its own plan | Next, after F127.1 | — |
| F106 | The framework's track, before the agents | Now | [draft](src/docs/plans/F106-framework-track.md) |
| F74 | Packaging the engine in place, then its own repo after the engine features | Now | [draft](src/docs/plans/phase-10-engine-package.md) |
| F44 | The engine as a product | Now | [feature doc](src/docs/features/F12-response-speed.md) |
| F39 | Orchestration | Now | [feature doc](demos/louis-agent/docs/features/F17-orchestrator-agent.md) |
| F43 | Accounts, tenants and durable sessions | Now | — |
| F62 | Validate demand | Now | — |
| F18 | F18's leftovers, before F49.1 | Now | — |
| F63 | The reference transcripts in git | Now | — |
| F64 | The judge check can't see pages | Now | — |
| F65 | After F35, the cook's answer | Now | — |
| F66 | Each pack declares what it needs | Now | — |
| F67 | A whole menu | Now | — |
| F68 | The cook scales wrongly | Now | — |
| F69 | The framework's `_Imports.razor` is about 90 lines | Now | — |
| F70 | Long comment lines in the eval pages | Now | — |
| F71 | At the next run, read the hand-off lines | Now | — |
| F72 | Open from the recipes evals | Now | — |
| F73 | Check the 47 new own-prices rows | Now | — |
| F125 | An agentic site: docs the standard way and public over MCP, buttons and cards in the guide, gaps, live demos, tools over MCP and discovery | Next, after F127.5 (Louis, 2026-11-10) | [plan](src/docs/plans/F125-an-agentic-site.md) |
| F126 | Evals, redesigned: one `.eval.json` of conversations with turns, six facts in code, criteria a judge marks, flips to correct it, its own eval host, six screens; today's evals deleted, not ported | Now | [plan](src/docs/plans/F126-evals-redesigned.md) |
| F131 | The chat on any website: one script tag opens our widget in a frame from louis-agent.dev on WordPress, static HTML or any site, nothing lost; a door per site, sites per tenant (after F43), a monthly rand budget (after F7), our own WordPress plugin, an answer that survives a page change; seven children | Now | [plan](src/docs/plans/F131-the-chat-on-any-website.md) |
| F137 | The site's card content as JSON (Louis, 2026-11-11, during CL60.1: "render our website cards with the info this way? that way our pages is built by json which the ai can then easily read"; his yes to cards and lists only, prose pages stay Razor): the demo cards, the pricing, what it does and the custom-agent steps read from JSON beside their pages and drawn by one card component, so an agent that edits the site changes data, not markup; reuses CL60's card records; groundwork for F133; after CL60, its own plan | Next | — |
| F133 | A website with an agent, set up by talking to it (Louis, 2026-11-11: "How about I build a wix with my platform"): not a Wix, but one record per business that draws both its site and its agent, three templates (guesthouse, restaurant, practice), a setup pack that writes the record as the owner talks, preview then checkout then live on its own host name; builds on F131, F43, F130.1 and F7; seven proposed children, four open choices (the record's shape before F131.1 starts) | Next | [draft](src/docs/plans/F133-self-serve-sites.md) |
| F134 | The showroom looks modern (Louis, 2026-11-11: "this looks off and those icons", "everything is a bit too flat"; his picks: look B, showcase with photos, and Tabler Icons): line icons instead of emoji, one raised surface, a rail with room, a photo on each business's cover, the index in a centred column under a photo hero; five steps | Next | [plan](src/docs/plans/F134-the-showroom-looks-modern.md) |
| F142 | Louis's CV as a page at `/cv` on louis-agent.dev (Louis, 2026-11-13: "redo my CV ... going to be a webpage on louis-agent, links to my Linked in and all"; his picks: one data file the page and a tool both read, the grayscale photo, a CV mark at the end of the nav): `cv.json` in the JSON Resume shape, records and a `ResumeView` in the framework's web, Save as PDF, no private details (a test holds it); six steps | Next | [plan](src/docs/plans/F142-the-cv-page.md) |
| F143 | The guide drives Louis's CV (Louis, 2026-11-13: "the agent should also drive my CV", "split it in 2 features"): a generic resume pack with `GetResume` over the same `cv.json`, `/cv` in the place index, a hiring focus skill (facts only from the tool, the honest no, an interview through the leads pack, no salary or puffery), an eval of eight conversations; two open choices asked when it starts; after F142 | Next, after F142 | [plan](src/docs/plans/F143-the-guide-drives-the-cv.md) |
| F48 | Recipe maths and a teaching cook | Next | [plan](demos/puls/docs/plans/F48-cook-and-teacher.md) |
| F46 | Localisation | Next | — |
| F50 | Puls's local specialists | Next | — |
| F40 | The sales playground | Next | — |
| F7 | Cost you can see and stop | Next | [feature doc](src/docs/features/F07-budgets.md) |
| FX4 | Append-only history | Next | [fix doc](src/docs/fixes/FX04-append-only-history.md) |
| F41 | Drafts and approvals | Next | — |
| F42 | Safety and audit | Next | — |
| F36 | MCP servers as packs | Next | — |
| F8 | Reliability | Next | [feature doc](src/docs/features/F08-reliability.md) |
| F15 | Model limits and local models | Next | [feature doc](src/docs/features/F15-model-limits.md) |
| F107 | A guest is sent to sign in before the chat: no composer for nobody (Louis, 2026-11-08, on mypuls.co.za: "I need to log in first"); today the gate answers 401 and the page sends them then; the guide in the site's corner is open on purpose (F109.3, ADR 0099), only the chat is behind the sign-in | Next | — |
| F108 | The admin portal is a role of a signed-in person, not a key: ADMIN_API_KEY goes once an account can carry the admin role (Louis, 2026-11-08: "should this not be a role?") | Next | — |
| F51 | Hosting for public traffic | Next | — |
| F75 | Puls local spec | Puls | — |
| F76 | Cook for the week | Puls | [spec](demos/puls/docs/specs/PULS_COOK.md) |
| CL16 | Reusable roles | Puls | — |
| F77 | A domain stop-list for the browser | Puls | [doc](demos/puls/docs/SITE_OWNERS.md) |
| F78 | Sources as data | Puls | — |
| F79 | Places | Puls | — |
| F80 | Local businesses | Puls | — |
| F81 | Vision: `ScreenshotPage(url)` and images as input | Puls | — |
| F82 | Local lookups | Puls | — |
| F23 | Credits | Puls | — |
| F83 | The cook's unconfirmed prices | Puls | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F25-local-cook.md) |
| F84 | A shorter cook report | Puls | — |
| FX59 | binary attachments sent as files, not text | Puls | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F22-role-tools.md) |
| FX60 | a continued specialist's ledger rows name the turn that continued it | Puls | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F26-specialist-follow-up.md) |
| FX61 | A site whose host has no `ACCOUNTS_SQL_URL` still calls `/account/me` and `/account/providers`, gets `index.html` back and dies on the JSON (the starter's login and chat pages show Blazor's error bar); the client should take a host without accounts as "nobody signed in" (found in CL58) | Fixes | — |
| FX66 | Both Azure apps keep their data-protection keys (which sign the sign-in cookie) in the container, so every cold start after scaling to zero makes new keys and signs everyone out (the start-up log warns); the keys go to each app's share, one setting (found 2026-11-08, F109.2) | Now | — |
| FX83 | The paymo and devops packs' async tools take no CancellationToken (20 tools), which ADR 0028's convertibility check showed when the coding catalogue loaded them (F106.3.2 step 6); since 2026-11-11 the coding agent loads neither (Louis: "we do not use them"), so no catalogue's check reaches them: fix it, with a check of the packs' own, before anyone builds on them | Next | — |
| F21 | Louis's corrections to the Afrikaans | Puls | [feature doc](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F21-personalities.md) |
| F28 | Team talk, the rest | Puls | [feature doc](demos/puls/docs/features/F28-team-talk.md) |
| F135 | Puls on `AskAgent`, measured when Puls resumes: the recipes eval before and after (F127.3.1 moved it with no run, since Puls is paused), and its stored replays recorded again, since they name the old tools | Puls | [ADR 0117](src/docs/adr/0117-one-address-for-every-agent-call.md) |
| F85 | A retailer's own prices, only with its permission or feed | Puls | — |
| F86 | Our own Nominatim, or a paid geocoder, before Puls is public | Puls | — |
| F87 | The open feeds have no key and no limit | Puls | — |
| F60 | Search by meaning | Puls, later | — |
| F88 | The cook's wave 3 | Puls, later | [spec](demos/puls/docs/specs/PULS_COOK.md) |
| F89 | A Puls-owned set of a few hundred South African recipes | Puls, later | — |
| F90 | WhatsApp as a host | Puls, later | — |
| F91 | The eleven official languages, measured | Puls, later | — |
| F92 | Voice: the person talks, our API turns it into text behind the chat's seam, the text lands in the box to check (Louis's pick B, 2026-11-11) | Puls, later | [draft](src/docs/plans/F92-voice-input.md) |
| F93 | The `similar` check, when an eval asks for closeness of meaning | Later | — |
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
| CL34 | The work email out of git history | Cleanups | — |
| CL35 | `Web/Browsing/RedirectBodies` goes when a PuppeteerSharp release awaits its own redirect body ta… | Cleanups | — |
| CL36 | Design docs for the three projects that have none | Cleanups | [doc](src/docs/projects/README.md) |
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
| CL49 | Remove the eval store's old setting names | Cleanups | — |
| CL50 | Remove `PULS_API`, the eval runner's old name for the API's address | Cleanups | — |
| CL52 | The rest of the size list | Cleanups | — |
| CL53 | A price format per country | Cleanups | — |
| CL54 | A pack can't add itself to a router | Cleanups | — |
| CL55 | Does an unanswered request belong in the usage ledger? A cancelled request is a record of 0 toke… | Cleanups | — |
| CL62 | A site's own words win only when registered before `AddSiteAreas` (the builder adds its defaults with `TryAdd`), which is wiring by order: give the builder a `Words(chatText, accountText)` method and have Puls pass its Afrikaans through it, so order never matters (ADR 0120, found in F127.6 step 10) | Cleanups | — |
| CL61 | `ToolCallGuards` is over 200 lines once Rider lays out its message holes, and `Agent/Turns` holds six files: split it by responsibility (the oversized-result summary its own class, the folder into subfolders), its holes into records (ADR 0119), and have it end the turn through `TurnEnding.Ends` as `TurnRunner` does (found in F127.4.3 step 3) | Cleanups | — |
| CL59 | The studio's partials hold rules nothing names any more (`.run-report`, `.run-transcript`, `.admin-cols`, `.eval-card`, `.explorer-q`, `.live-chat`, `.tree-node`, `.conv-row`, `.check-script`…), dead since CL10 by the look of them: confirm and delete (found in CL58) | Cleanups | — |

## Known limitations

Facts about today, not tasks; the one list (the development guide and the docs link here). When work removes one,
delete it here (see [Finishing a feature](src/docs/features/README.md#finishing-a-feature)). Each pointer names the item
above that removes it.

1. **Rider MCP discovery only logs** Rider's tools; they aren't callable yet (needs a full MCP client).
   → **F36** MCP servers as packs (*Next*, Phase 8).
2. **Sessions are in memory** in the API and ACP server; a restart ends them (the web app keeps the transcript).
   → **F43** Accounts, tenants and durable sessions (*Next*, Phase 9;
   [session architecture](src/docs/specs/SESSION_ARCHITECTURE.md)).
3. **Ollama tool support is a name heuristic** (`LlmOptions.KnownNoToolsPrefixes`).
   → [**F15** Model limits and local models](src/docs/features/F15-model-limits.md) (the capability check per model).
4. **Paymo task lookup takes the first match** for a name; an ambiguous name can hit the wrong task.
5. **No rate limiting or audit log** beyond the file logs. → **F42** Safety and audit (*Next*, Phase 7).
6. **10 tool rounds per message** (`MaximumIterationsPerRequest`): a long task stops mid-way and needs a follow-up
   message to continue. The orchestrator does this automatically ("Continue the fix…"); most demo fixes take 2-3
   messages. → [**F10** Settings per role and per session](src/docs/features/F10-route-settings.md) (*Later*).
7. **Running the API with `dotnet run` (unpublished)** only serves the web app in the `Development` environment
   (`ASPNETCORE_ENVIRONMENT=Development`); otherwise `/` returns 404. The Docker images are published, so they're fine.
8. **Local models get a truncated prompt.** The agent sends ~26,000+ tokens (system prompt and tools) and doesn't tell
   Ollama the window, so Ollama uses its own default (2,048 on the version measured here; newer ones pick 4k–256k by
   memory) and drops the rest without an error: the ledger showed `input: 2050`. The `ollama-nvidia` and `ollama-amd`
   containers default to 32k, which covers it; the CPU `ollama` service still uses Ollama's default.
   → [**F15** Model limits and local models](src/docs/features/F15-model-limits.md) (sends `num_ctx`; takes in F13).
9. **Changing the tools or skills during a chat can break it on Claude Haiku 5.5** (the default). The model rejects a
   changed system prompt or tool list once thinking blocks are in the history (enforced for accounts created on or
   after 2026-08-31). `BeginTurn` sends the engine's live tool list, so a tool the model creates (`CreateTool`) breaks
   the rest of that message; `/approve`, `/reject` and a skill reload break the next one; and in the API every open
   chat with the same toolsets shares one engine and its tool list (F5). Start a new chat afterwards.
   → [**FX4** Append-only history](src/docs/fixes/FX04-append-only-history.md) (first in Phase 7).
