# Architecture decisions

> **Reference.** Every lasting architecture decision, one record each, newest last. **Elsewhere:** how the system is
> built in the [architecture](../architecture/README.md), how a change is made in the [development guide](../CLAUDE.md).

Each record is an ADR (architecture decision record) in
[Michael Nygard's format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions): the context that
forced a choice, the decision, and what it costs. Decisions made before 2026-10-14 were recorded
afterwards from the docs that hold them (each ADR links its sources), with the date they were made.

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-messages-api-tier-with-our-own-loop.md) | The Messages API tier, with our own loop behind `IChatClient` | Accepted |
| [0003](0003-provider-strategies.md) | Provider differences are strategies, chosen once in `LlmClientFactory` | Accepted |
| [0004](0004-arguments-as-data.md) | Arguments travel as data, never through a shell | Accepted |
| [0005](0005-guard-the-context.md) | Guard the context: summarise oversized results, never run cut-off calls | Accepted |
| [0006](0006-toolsets-per-session.md) | A session chooses its toolsets when it opens, and keeps them | Accepted |
| [0007](0007-measure-every-request.md) | Measure every request, priced from a dated table | Accepted |
| [0008](0008-history-is-append-only.md) | Conversation history is append-only | Accepted (FX4 still open) |
| [0009](0009-message-text-as-enum-templates.md) | Message text lives in enum templates; logging goes through `ILogger` | Amended by 0119 |
| [0010](0010-folders-by-concern.md) | Folders by concern, the namespace is the folder path, and a test holds it | Accepted |
| [0011](0011-prompts-are-content.md) | Prompts are content: plain markdown skills, no provider class per toolset | Accepted |
| [0012](../../../demos/louis-agent/docs/adr/0012-one-orchestrator-per-microservice.md) | One orchestrator per microservice, which asks louis-agent for fixes | Accepted (its tier question closed by 0031) |
| [0013](0013-documentation-structure.md) | Documentation: one type per doc, guides by folder, arc42 and ADRs | Accepted |
| [0014](0014-test-first.md) | Test first: a step's tests are written and seen failing before the code | Accepted |
| [0015](0015-licence-agpl.md) | Licence: AGPL-3.0, so improvements to a hosted Puls stay open | Superseded by 0016 |
| [0016](0016-licence-all-rights-reserved.md) | Licence: all rights reserved | Accepted |
| [0017](0017-skills-by-level.md) | Skill files follow the levels: a folder per level, and a role opens only at its own | Accepted |
| [0018](0018-built-in-prompts-embedded.md) | Built-in prompts are embedded, read only from the assembly | Accepted |
| [0019](0019-modes-one-file-per-product.md) | A level-0 session is a mode, in a folder per product; a language shares its product's routing | Accepted |
| [0020](0020-principles-for-every-agent.md) | Every agent loads the same principles first: never lie, cheat, steal or murder | Accepted |
| [0021](0021-minions-one-task-one-tool.md) | Minions: a specialist hands one task and one of its own tools down, two levels deep and no further | Accepted |
| [0022](0022-benchmarks-recorded-from-the-protocol.md) | Benchmark results in MongoDB, recorded by the API from the harness's stdout protocol | Amended by 0029 |
| [0023](0023-copyleft-service-beside-us.md) | A copyleft service may run beside us, unchanged; what we link stays MIT, Apache or BSD | Accepted |
| [0024](0024-choices-are-enums.md) | Choices are enums, not strings, parsed once where they enter | Accepted |
| [0025](0025-a-role-declares-its-toolsets.md) | A role declares its own toolsets in its file; a `?` toolset loads only where an instance has it | Accepted |
| [0026](0026-parallel-tool-calls-for-read-only-toolsets.md) | A round's tool calls run at once only where every toolset is parallel-safe | Accepted |
| [0027](0027-engine-identities-and-packs.md) | The engine, identities and packs: core names no product | Accepted |
| [0028](0028-packs-stay-convertible-to-mcp.md) | Packs stay convertible to MCP; MCP plugs in at the toolset | Accepted |
| [0029](0029-simulations-in-csharp.md) | Simulations in C#, called evals: one harness for checks, regressions and demos (amended 2026-10-24: the word is *eval*) | Accepted |
| [0030](0030-build-or-adopt.md) | Build or adopt: the loop stays ours on Microsoft.Extensions.AI | Accepted |
| [0031](0031-orchestration-is-an-engine-function.md) | Orchestration is an engine function, and the orchestrator stays on core | Accepted |
| [0033](0033-what-the-agent-writes-has-a-scope.md) | What the agent writes has a scope: session, agent or shared (0032 is kept for *Sources as data*) | Accepted |
| [0034](0034-code-a-person-can-maintain.md) | Code a person can maintain: one concern per file, at most 200 lines, no code in razor | Amended by 0044 |
| [0037](0037-one-agent-api-products-get-a-front-door.md) | One agent API; products get a front door: the API, its project files and its contract name no product, and what is only a product's (its web app, compose file, cloud app, database name) lives at its own door; renames keep what is stored and linked, and `ApiNamesNoProductTests` holds it (0035 and 0036 are kept for the engine package and CL17) | Accepted |
| [0038](0038-the-browser-says-what-it-is.md) | The browser says what it is: it never disguises itself to a site; an identity names itself with `BROWSER_USER_AGENT`, never a person | Accepted |
| [0039](0039-api-routes-under-v1.md) | The agent API's routes follow one rule, under `/v1`: nouns, 201 on a create, `DELETE`, `POST /{resource}/{verb}`, the key by default, the table pinned by a test | Accepted |
| [0040](0040-records-for-values-classes-for-things.md) | Records for values, classes for things: a record holds no resource, both are sealed by default, and a closed set is an abstract record | Accepted |
| [0041](0041-json-through-records-by-hand-only-for-bytes.md) | JSON goes through records and one options object per wire; the writable DOM only for a shape decided at run time, the read-only DOM for input we don't own, the writer only when the bytes are the contract | Amended by 0119 |
| [0042](0042-the-web-shows-the-live-results.md) | The web shows the live results: a page renders what it last fetched, fetches again after any write it makes, and never keeps a count the API keeps | Accepted |
| [0043](0043-repeated-names-are-enums.md) | Repeated names are enums, one per pluggable project: a name written in more than one place (a toolset, a role, a mode) is an enum even in an open set; a pack, an identity or an eval set keeps its own copy and never references another's, the rest share one; wire names stay text, carried as `[WireName]` and parsed once by `SettingChoice` |
| [0044](0044-the-size-limit-counts-code.md) | The size limit counts code: 200 lines (tests 400) without blank and comment lines, so the comments the rules ask for never force a split | Amended by [0053](0053-stylesheets-by-concern.md) |
| [0045](0045-typed-answers-and-a-shared-contract.md) | Typed answers and a shared contract: endpoints return `TypedResults` with `Results<…>` unions, errors are the `ApiError` record, and the records live in `louis-agent.api.contracts` for the API and its clients | Accepted |
| [0046](0046-dates-are-utc.md) | Dates are UTC: stored, logged and sent as ISO 8601 UTC; a screen converts them for the reader; a future local event keeps its local time and zone | Accepted |
| [0047](0047-protocol-v2-one-event-contract.md) | Protocol v2: one event contract both sides share: the runner and the API read and write the records in `louis-agent.evals.protocol`, JSON Lines with `"event"` first, kebab-case words, UTC `t`; our own field names, not OpenTelemetry's `gen_ai.*`; amends 0022 | Accepted |
| [0048](0048-mongo-through-records.md) | MongoDB goes through records: each collection a typed `IMongoCollection<T>` over a document record with its stored names as attributes, enums as wire names, dates in UTC, typed filters; `BsonDocument` only for a shape decided at run time | Accepted |
| [0049](0049-nothing-is-compatible-before-a-sale.md) | Nothing is backward compatible until something is sold in production: no migration, alias, lenient reader or second parser for what we store or send between our own parts; a changed shape means delete and re-run | Accepted |
| [0050](0050-summaries-like-a-person.md) | Every class and method has a summary written like a person, a `<remarks>` for its pattern and why, a `<seealso>` per ADR that shaped it; a step inside a method gets one plain comment line, and steps sit in groups; fixed file by file, in the step's own commit | Accepted |
| [0051](0051-flat-conditionals-and-named-endpoints.md) | No nested conditionals (a switch on the shape or guard clauses), and an endpoint is a named method on a handler class, its checks first as guard clauses | Accepted |
| [0052](0052-edit-never-script.md) | Code changes only by editing, never a script; layout only through the formatter; a rename only through Rider; in Rider, Rider's tools come first for any job they cover | Amended by [0054](0054-a-move-may-copy-line-ranges.md) |
| [0053](0053-stylesheets-by-concern.md) | A stylesheet is split by concern: at most 200 lines of rules a file, `app.css` the `@import` list in cascade order | Accepted |
| [0054](0054-a-move-may-copy-line-ranges.md) | A pure move may copy whole line ranges with a command, when a byte check proves nothing changed | Accepted |
| [0055](0055-keep-it-simple.md) | Keep it simple: the simplest elegant design that meets the step, nothing for a future that isn't planned | Accepted |
| [0056](0056-a-check-is-a-catalogue-entry.md) | A check is a catalogue entry: the contract in `src/evals/louis-agent.evals.checks` (what a pack references, nothing more), found by its kebab-case type, built from a typed parameter record, listed in a registry each owner fills | Accepted |
| [0057](0057-regex-is-the-last-option.md) | A regular expression is the last option: plain string methods or a reader first; when one is needed, a named [GeneratedRegex] with an example, NonBacktracking on outside input; never in content | Accepted |
| [0058](0058-an-eval-is-a-folder-of-files.md) | An eval is a folder of files (`eval.json`, its manual, dataset and transcripts) beside the identity or pack it proves, found by the runner through the composition, its checks named from the catalogue; a new eval starts as a copy of the template folder | Accepted |
| [0059](0059-an-identity-ships-readers-not-checks.md) | The harness ships every family of check an eval tool ships; a compare reads its value through a reader the file names beside the key, from a second registry; an identity ships readers, not checks; a swap of checks is proven on pinned grades and a replay | Accepted |
| [0060](0060-a-pack-ships-its-checks.md) | A pack ships the checks that prove its tools, in a registry the composition gathers; plain markdown reading is the check contract's; what only an identity knows comes in through a reader the file names; a small shared table lives twice, held equal by a test | Superseded by 0073 |
| [0061](0061-evals-grow-from-use.md) | Evals grow from use: a sample or transcript made in the portal is a draft in a store until F49.5 publishes it; a sample is a variant of its conversation's scenario; a transcript is kept only when people's verdicts back it | Accepted |
| [0062](0062-the-studio-writes-what-git-keeps.md) | The studio writes what git keeps: the runner is the one reader and judge of eval files, reached as a process; an eval being composed is a draft written out as one folder that is checked, tried (always a replay) and published (a commit on a branch, never a push, or a zip); promptfoo in and out by a mapping table that names what it leaves out | Accepted |
| [0063](0063-a-prompt-says-what-it-is.md) | A prompt says what it is: every built-in prompt and judge rubric opens with the Agent Skills spec's front matter (name, description, our level and toolsets under metadata), read once by one strict reader in core; the model and the judge get only the body; the portal lists them | Accepted |
| [0064](0064-observability-through-opentelemetry.md) | Observability through OpenTelemetry: logs through Serilog's OTLP sink and traces through the OpenTelemetry SDK, on the standard `OTEL_*` settings alone; a turn is an `invoke_agent` span with no content, a sub-session nests across the HTTP hop, a crash is one Critical line; the Aspire dashboard on the PC, Application Insights on Azure; and the table of what to log at which level | Accepted |
| [0065](0065-the-view-is-in-the-address.md) | The view is in the address: what a portal page shows (the run page's tab, the explorer's level and what is open in it) is read from the address, parsed once and written by one class; a component that keeps state reports every move and takes a changed place back; a change of address is not a fetch | Accepted |
| [0066](../../../demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md) | The engine's site is static Razor, and its films are CSS: `src/web/louis-agent.site` renders plain HTML on the server with no WebAssembly and no script of ours, references no engine project, shows only what works today, and draws the two films with CSS keyframes over CSS 3D, what both share in one stylesheet and each film's timing in its own | Superseded in part by F109.1 (the pages are the code-review app's; the films stay CSS) and by ADR 0100 (what's planned is shown, labelled) |
| [0067](0067-a-pages-own-data-before-its-text.md) | A page's own data before its text: a tool reads the page's structured data in code and gives the model a short, framed print; the model reads the text only where there is no data | Accepted |
| [0068](0068-every-fetch-obeys-robots-txt.md) | Every fetch obeys robots.txt: one policy checks a site's file (our group, else the strictest AI-reader group, else `*`; read a day, an hour if unreachable) and the page's own no-AI signals, on every fetcher hop and every browser visit | Accepted |
| [0069](../../../packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md) | Shop prices are Stats SA's averages, and where to buy is the public map: the shops pack goes for `where-to-buy` (OpenStreetMap's branches) and `shop-prices` (Stats SA's averages, totals in code, kept in MongoDB per source and month, seeded from the shipped tables), each credited | Accepted |
| [0070](0070-packs-publish-data-feeds.md) | Packs publish data feeds, and the API serves them open: a pack lists read-only feeds (`IDataFeed`: a name, a description, its JSON), the catalogue gathers them, and `GET /v1/feeds` and `/v1/feeds/{name}` serve them without knowing the pack; the first is `shop-prices` | Accepted |
| [0071](0071-packs-take-data-uploads.md) | Packs take data uploads, previewed before they are saved: a pack lists uploads (`IDataImport`: its files, a preview, a save), the admin portal shows every one at `/admin/imports`, the preview lists the new and corrected months and Save writes only those; the first is `shop-prices`, Stats SA's two spreadsheets, and an older month prices what the newest lacks | Accepted |
| [0072](0072-feeds-are-mcp-resources.md) | The MCP server publishes the packs' feeds as resources: one per feed, `feed://{name}`, `application/json`, read whole when asked, set up once for both transports (`McpPrimitives`), behind the MCP key over HTTP; tested with a real client over in-memory pipes | Accepted |
| [0073](0073-packs-reference-no-evals.md) | Packs and identities reference no evals: what proves one is a project beside it that only the runner references (`….checks`, `….evals`); a new check is general when it can be; a contract two sides share is a shared project or a duplicate held equal by a test; every placement is weighed against the engine split | Accepted |
| [0074](0074-the-folders-say-what-ships.md) | Framework and implementation are split, and this comes first: the solution's groups and the disk are one map; `src/` holds only the framework; `packs/` holds tools and `packs/evals/` what proves them; anything naming a product is someone's project under `demos/`, by product then function | Accepted |
| [0075](0075-test-what-you-touch.md) | Test what you touch while working: the touched projects, plus the repo-wide rules when files move; the whole solution before a pull request |
| [0076](0076-core-ships-no-agent.md) | Core ships no agent: the coding agent is an identity in its demo, a tool the model asks for is a pack, and what every pack relies on stays in the engine, reached through a contract |
| [0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md) | The films on GitHub are animated WebP, rendered from the site's CSS, with a still for reduced motion; a stale export fails a test | Accepted |
| [0078](../../../packs/docs/adr/0078-a-recipe-search-asks-several-sites.md) | A recipe search asks several sites and the model doesn't pick which: four at random from those with their own search, merged in a random order; no prompt names a site to start with | Accepted |
| [0082](../../../packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md) | The recipe library keeps each new proper recipe for good, from a page's data or its plain headings, and is an open pack feed: a search hands the cook our API's address for what we hold, the credit stays the site's, and the sites are still asked on every search | Accepted |
| [0083](0083-packs-keep-reports-behind-the-admin-key.md) | Packs keep reports behind the admin key: a pack lists reports (`IDataReport`: a table of numbers about itself), `GET /v1/admin/reports` names no pack, a store that's down is an empty table, and counting is one small record per UTC day, a write beside the work that can only log if it fails |
| [0084](0084-every-site-is-built-on-the-frameworks-web.md) | Every site is built on the framework's web: what would be the same on every site is in `louis-agent.web`, and it names no site | Amended by 0120 |
| [0086](0086-a-packs-screens-are-described.md) | A pack's screens are described: a pack lists `IDataScreen`s as plain data, one generic page draws them, a real component from `packs/web/` replaces one by name and the last to register wins, and a pack and its web views never reference each other | Accepted |
| [0085](0085-styles-are-written-in-sass.md) | Styles are written in Sass and the build makes the CSS; CSS variables stay, each colour is written once, and anything shared is reused, never copied | Accepted |
| [0079](0079-show-a-specialists-report.md) | An asker can show an asked agent's answer as it is and end the turn there, so its model never retypes it (since 0117 the identity's `RelaysAnswersWhole`, before it `SHOW_SPECIALIST_REPORTS`); off by default, since an agent that checks the work has to read it | Amended by 0117 |
| [0080](0080-a-tool-when-it-improves-the-prompt.md) | If a tool would make a prompt's job work better or more accurately, make the tool instead of another prompt rule; judgement stays in the prompt | Accepted |
| [0081](../../../demos/puls/docs/adr/0081-the-cook-searches-for-itself.md) | Puls's cook searches for itself, and a minion stays only if it beats no minion: the same accuracy and time for less cost | Accepted |
| [0087](0087-identity-is-the-identity-server.md) | ASP.NET Core Identity is the identity server, in `louis-agent.accounts`, signing in with an HttpOnly cookie that answers 401, never a redirect; what it lacks is ours beside it; the log is the mail until a sender is picked | Accepted |
| [0088](0088-a-tenant-on-every-account.md) | Every account is in a tenant, resolved from the request's host name; an email is unique within a tenant; the store refuses without one; tenancy is data, a second tenant is a row | Accepted |
| [0089](0089-the-account-client-is-a-library-a-site-opts-into.md) | The account pages and client are `louis-agent.web.accounts`, a library on top of the framework's web that a site opts into; the chat's menu knows accounts through one seam, `IAccountMenu`, with a Null Object when there are none | Accepted |
| [0090](0090-the-packs-are-one-area.md) | The packs are one area, `packs/`, with the same inner names as `src/` and a demo: `tools/`, `evals/`, `tests/`, `web/`, `docs/`; the rules of 0073 and 0086 stand on the new prefixes; a new kind beside the packs is a folder under it, never a new root | Accepted |
| [0091](0091-web-access-and-telemetry-are-libraries-on-core.md) | The web (browser, fetcher, robots, search, rendering, recorders) and the telemetry (tracer, exporter, the collector's sink) are `louis-agent.core.web` and `louis-agent.core.telemetry`, on core behind `IWebAccess` and `IHostTelemetry` with a Null Object each; a host composes them through `HostServices`; core names neither PuppeteerSharp nor OpenTelemetry | Accepted |
| [0092](0092-a-project-holds-what-is-reused-together.md) | A project holds what is reused together and nobody takes what it doesn't use: a plain name sits below core and references nothing heavy, a dotted name sits on the thing before the dot and that thing opts into nothing, a product or SDK never sits in core | Accepted |
| [0093](0093-a-script-is-a-module-beside-its-component.md) | A script is an ES module beside the component that needs it, imported through `JsModule` and let go on dispose; a site lists nothing of ours but `first-paint.js`; the rule reads the modules and the calls | Accepted |
| [0094](0094-a-components-styles-sit-beside-it.md) | A rule one component draws sits beside it in `X.razor.scss`, scoped and bundled by the build; several components' in their ancestor's under `::deep`; what a site's own pages draw, a theme overrides or many components share stays a global a site forwards; a move is checked by the goldens, a weight scan and the browser | Accepted |
| [0095](0095-puls-runs-from-azure-and-nothing-points-at-the-pc.md) | Puls runs from Azure in its own private network (a VM for Mongo, the browser and SearXNG; Azure SQL's free offer for the accounts); the build is one Bicep file in config whose outputs the verbs read back; every secret a Key Vault reference; the old deploy and the F58 tunnel retired once the domain answered; the framework's site merges into the louis-agent demo's web app rather than deploying on its own | Accepted |
| [0097](0097-puls-deploys-from-github-without-a-password.md) | Puls deploys from GitHub after green CI on `main` and from a button, signed in by OpenID Connect with no password, running `deploy.ps1 release` as it is, with Contributor on its resource group and Key Vault Reader only | Accepted |
| [0098](0098-the-demos-app-runs-beside-puls-split-later.md) | The demo's app runs beside Puls, split later: one build, one environment, one VM and one database for both apps, each with its own identity, share, keys, domain and tenant; `-App` on every verb; a Bicep module of its own when it's sold | F109.2 |
| [0099](0099-the-guide-answers-without-a-sign-in-within-a-cap.md) | The guide answers without a sign-in, within a cap: a host names one open identity and a cap per address per day; the door is a guard beside the key gate; an admitted session is narrowed in code to that identity's own toolsets; past the cap a 429 | F109.3 |
| [0100](0100-the-site-tells-the-whole-architecture.md) | The site tells the whole architecture, and shows what's planned as planned: every project that ships is named on a page and a test holds it to the solution; the pages are written from the docs the guide reads, each claim held to the repo by a test (the rand to the rates file, the packages to the project files, the planned features to the TODO's open rows, the guide's voice to the pages); what's planned is listed by id, labelled, never as shipped; the short film is the map, the detailed one the turn; nothing public links to the private repository | F109 |
| [0101](0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md) | A lead is a tool, sent once on the visitor's yes: the guide gets `SendLead` in a pack of its own, the tool refuses an incomplete lead and tells the truth when it only reached the log, the voice holds the consent (ask, repeat back, send on yes, once), and the sender is a seam, Resend with a key and the log without |
| [0102](0102-third-party-marks-follow-their-owners-guidelines.md) | Third-party marks follow their owners' guidelines: the official mark unaltered, their colour and ground, their spelling and wording, the guideline named in the component; WhatsApp's button first |
| [0109](0109-a-page-is-served-as-html.md) | A site's page is served as HTML, rendered once per deploy: a page declares a `PageDescription`, `MapLouisAgentSite` renders it with `HtmlRenderer` and a `HeadOutlet` into the site's `index.html` (title, description, link card, canonical link, body) on its first request and keeps it until the next deploy; a page without a description, or that fails, answers the plain shell; the app takes over in the browser as before |
| [0110](0110-a-page-says-what-it-is-in-one-head.md) | A page says what it is in one head: `PageHead` writes its description and its schema.org facts as JSON-LD in one `HeadContent` (Blazor keeps only the last); the prerender carries them and adds a breadcrumb for every page under the landing; a site's facts are its own; a page about other frameworks says of them only what their docs say, linked |
| [0111](0111-the-frameworks-website-lives-in-src-site.md) | The framework's website lives in `src/site/`, built on the framework like any site: the site, the guide (`louis-agent-guide`) and its own host, whose catalogue is the guide on its three packs; the one place in `src/` that may reference a pack or an identity, and nothing else in `src/` references it (`SiteBoundaryTests`); amends ADR 0074; amended by 0120 |
| [0112](0112-the-site-sells-first.md) | The site sells first, the framework is the proof: louis-agent.dev's hero, landing and menu lead with the website with an agent, `/what-we-build` and `/pricing` are its own pages, the framework's pages sit under `/framework/…` (old addresses redirect) until F130 gives them a site; claims and links held by tests; amends ADR 0100 |
| [0113](0113-the-guide-is-louis-and-says-it-is-the-ai.md) | The guide is Louis, the AI version of him, labelled "Louis · AI" and honest about it: his own story told as "I" and never added to, help first and details only after a yes, proper English or Afrikaans, and a site's chat may hide its work (`ShowWork`) |
| [0114](0114-an-identitys-skills-load-always-or-on-demand.md) | An identity's skills load always or on demand (progressive disclosure, as Anthropic's Agent Skills): files in `prompts/agent/{identity}/skills/` whose header's `metadata.load` says which; an always skill follows the voice, an on-demand one is listed by its description and read with the engine's `ReadSkill`, so the description says when to read it; amended by 0121 (a focus skill per part of a site) |
| [0115](0115-a-sites-showroom-is-an-area-of-the-framework.md) | A site's showroom is an area of the framework: the frame, the stage, the start and the mock website live in `louis-agent.web`, a site gives its demos and words as data; a business's demo plays on a mock of its own website, held to its agent's facts by a test; its agent shows the website with the guide's own `ShowPage`, told as the recording plays; a cue plays from its turn |
| [0116](0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md) | A showroom's demos can be tried live, through the open door: a mock website of pages that register with it (`MockPage`), Try it yourself swaps the recording for `MockLiveChat` on the same corner, `OPEN_IDENTITIES` a list under one cap, a demonstration's lead marked `[demo]` by its identity (`IsDemonstration`, `TurnIdentity`), the brands' faces the framework's own files (F127.2) |
| [0117](0117-one-address-for-every-agent-call.md) | One address for every agent call: `{identity}` or `{identity}.{role}`, read once into `AgentAddress`; each identity lists whom it may call (`Calls`); `AskAgent(address, message)` starts or adds, a minion too with one of the asker's tools (F127.5); everyone may ask whom their `Calls` allow but a minion, never an agent already in the chain, at most `AGENT_MAX_DEPTH` deep; the plumbing stays `PeerSession`; the session wire is shared records in `louis-agent.session-stream`; amends 0021, 0031 and 0079 (F127.3.1 to F127.5) | Accepted |
| [0118](0118-a-diagram-is-data-our-renderer-draws.md) | A diagram in a doc is a JSON our renderer draws, its SVG beside it: flow, cards, sequence and classes; a plan's four diagrams are pictures; no Mermaid anywhere, held by `NoDoc_HoldsMermaid` (CL60) | Accepted |
| [0119](0119-declare-name-then-pass.md) | Named types, declared, then passed: no anonymous types or `dynamic` unless plainly the best option; a message's holes are a `{Member}Holes` record the member names with `[Holes]`, held equal to its template by a test; an object built for a call is first a local named for what it is; existing code changes as touched; amends 0009 and 0041 (F127.4.3) | Accepted |
| [0120](0120-nothing-is-hand-wired-from-the-framework.md) | Nothing is wired by hand from the framework into an implementation: a site takes the framework's parts only through its builders (`AddSiteAreas` and the areas a library adds), never building or registering a part itself or listing its pages; one Composition Root per site that its tests share; a site's own services stay its own; held by `SitesNameTheirAreasTests`; amends 0084 and 0111 (F127.6) | Accepted |
| [0121](0121-every-part-of-the-web-is-ready-for-an-agent.md) | Every part of the framework's web is ready for an agent: every place has an address, a site names the part each page is (`Focuses`, sent with each message and read as a focus skill), a piece can be selected (pressed, a chip, sent with each message), and every place is in an index the host builds from its own rendered pages, searched by the site pack's `FindPlace`, never a hand-kept list; builds on 0107 and 0120, amends 0114 (F127.7) | Accepted |
| [0108](0108-the-public-watches-the-coding-agent-recorded.md) | The public watches the coding agent recorded, and the live agent stays behind the key: the site's doors lead to `/coding-agent`, a session recorded once on a sample and played in the real chat by the framework's `RecordedChat`, nothing run per visitor; a recording is the API's stream saved as `.sse`, made by `record.sh` with only the model key |
| [0107](0107-every-section-has-an-address.md) | Every place on a site's page has its own address: `/proof/rand`, a card `/identities/puls`, a page of a paged section `/how-it-works/parts/2`, the landing's under `/home`; one rule both ways in `SiteAddresses`, a catch-all route opens any place, and the index, the address bar and the guide all use it |
| [0106](0106-the-landing-is-a-business-website-from-the-framework.md) | The landing page is a business's public website, built from the framework: the template of the product, every generic part (shell, bands of named areas, cards, diagrams, the widget that knows and shows the page) in the framework's web, a site brings words; two shapes, the whole site or the agent alone on a page; the demo's explaining pages are not the product's |
| [0105](0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md) | The guide acts on the page through a tool the widget hears: ShowPage is checked in the API and moves nothing, the web's runner raises ToolCalled per call and the widget navigates, scrolls the area into view and lights it up; every area is a named band; the model asks as data, the server validates, the client performs |
| [0104](0104-a-message-may-carry-the-page.md) | A message may carry the page the visitor is reading: address, title and visible text as data, one block in the user's message after the words and the attachments, cut at 6,000 characters, sent once per page and read at send time; no tool, no system line, the history untouched |
| [0103](0103-a-diagram-is-data-on-a-row-grid.md) | A diagram is data on a row grid, drawn with connectors: a site declares boxes and arrows, the framework's web puts every box on one row grid (no jog can happen), orders a column by its neighbours, bends every arrow between two columns at one track, runs back arrows under the drawing, and keeps words on flat pieces with room, so nothing crosses or overlaps |

## When to write one

When a plan makes a decision that later work must follow, or that someone will one day ask "why?" about: a provider,
a protocol, a layout or safety rule, a trade-off between cost and quality. Not for a step's details (the plan keeps
those) or a bug's cause (the fix doc keeps that). The ADR lands in the same PR as the work it decides.

## Rules

- **Numbered** `NNNN-title-in-kebab-case.md`, in order; numbers are never reused.
- **Never edited once accepted**, except its *Status* line. To change a decision, write a new ADR that supersedes it,
  and set the old one's status to *Superseded by NNNN*, linking the new record.
- **Amended** when a later ADR changes part of a decision and the rest stands: the old one's status becomes *Amended by
  NNNN*, linking the new record.
- **Short:** a page at most. The detail stays in the feature, fix, plan or spec it links to.

## Template

```markdown
# NNNN. Title: the decision, in a few words

**Status:** Proposed | Accepted | Superseded by [NNNN](NNNN-….md) | Amended by [NNNN](NNNN-….md) · **Date:** YYYY-MM-DD (decided) ·
**Recorded:** YYYY-MM-DD

## Context

What forced a choice: the problem, the constraints, the options considered.

## Decision

What we do, stated as a rule ("We …").

## Consequences

What gets easier, what gets harder, and what we now have to keep doing.

## Links

The feature, fix, plan or spec where it was decided.
```

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
