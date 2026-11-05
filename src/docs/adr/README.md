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
| [0009](0009-message-text-as-enum-templates.md) | Message text lives in enum templates; logging goes through `ILogger` | Accepted |
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
| [0041](0041-json-through-records-by-hand-only-for-bytes.md) | JSON goes through records and one options object per wire; the writable DOM only for a shape decided at run time, the read-only DOM for input we don't own, the writer only when the bytes are the contract | Accepted |
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
| [0066](../../../demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md) | The engine's site is static Razor, and its films are CSS: `src/web/louis-agent.site` renders plain HTML on the server with no WebAssembly and no script of ours, references no engine project, shows only what works today, and draws the two films with CSS keyframes over CSS 3D, what both share in one stylesheet and each film's timing in its own | Accepted |
| [0067](0067-a-pages-own-data-before-its-text.md) | A page's own data before its text: a tool reads the page's structured data in code and gives the model a short, framed print; the model reads the text only where there is no data | Accepted |
| [0068](0068-every-fetch-obeys-robots-txt.md) | Every fetch obeys robots.txt: one policy checks a site's file (our group, else the strictest AI-reader group, else `*`; read a day, an hour if unreachable) and the page's own no-AI signals, on every fetcher hop and every browser visit | Accepted |
| [0069](../../../packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md) | Shop prices are Stats SA's averages, and where to buy is the public map: the shops pack goes for `where-to-buy` (OpenStreetMap's branches) and `shop-prices` (Stats SA's averages, totals in code, kept in MongoDB per source and month, seeded from the shipped tables), each credited | Accepted |
| [0070](0070-packs-publish-data-feeds.md) | Packs publish data feeds, and the API serves them open: a pack lists read-only feeds (`IDataFeed`: a name, a description, its JSON), the catalogue gathers them, and `GET /v1/feeds` and `/v1/feeds/{name}` serve them without knowing the pack; the first is `shop-prices` | Accepted |
| [0071](0071-packs-take-data-uploads.md) | Packs take data uploads, previewed before they are saved: a pack lists uploads (`IDataImport`: its files, a preview, a save), the admin portal shows every one at `/admin/imports`, the preview lists the new and corrected months and Save writes only those; the first is `shop-prices`, Stats SA's two spreadsheets, and an older month prices what the newest lacks | Accepted |
| [0072](0072-feeds-are-mcp-resources.md) | The MCP server publishes the packs' feeds as resources: one per feed, `feed://{name}`, `application/json`, read whole when asked, set up once for both transports (`McpPrimitives`), behind the MCP key over HTTP; tested with a real client over in-memory pipes | Accepted |
| [0073](0073-packs-reference-no-evals.md) | Packs and identities reference no evals: what proves one is a project beside it that only the runner references (`….checks`, `….evals`); a new check is general when it can be; a contract two sides share is a shared project or a duplicate held equal by a test; every placement is weighed against the engine split | Accepted |
| [0074](0074-the-folders-say-what-ships.md) | Framework and implementation are split, and this comes first: the solution's groups and the disk are one map; `src/` holds only the framework; `packs/` holds tools and `packs-evals/` what proves them; anything naming a product is someone's project under `demos/`, by product then function | Accepted |
| [0075](0075-test-what-you-touch.md) | Test what you touch while working: the touched projects, plus the repo-wide rules when files move; the whole solution before a pull request |
| [0076](0076-core-ships-no-agent.md) | Core ships no agent: the coding agent is an identity in its demo, a tool the model asks for is a pack, and what every pack relies on stays in the engine, reached through a contract |
| [0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md) | The films on GitHub are animated WebP, rendered from the site's CSS, with a still for reduced motion; a stale export fails a test | Accepted |
| [0078](../../../packs/docs/adr/0078-a-recipe-search-asks-several-sites.md) | A recipe search asks several sites and the model doesn't pick which: four at random from those with their own search, merged taking turns; no prompt names a site to start with | Accepted |
| [0082](../../../packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md) | The recipe library keeps each new proper recipe for good, from a page's data or its plain headings, and is an open pack feed: a search hands the cook our API's address for what we hold, the credit stays the site's, and the sites are still asked on every search | Accepted |
| [0079](0079-show-a-specialists-report.md) | An instance can show a specialist's report as it is and end the turn there, so its model never retypes it (`SHOW_SPECIALIST_REPORTS`); off by default, since an agent that checks the work has to read it | Accepted |
| [0080](0080-a-tool-when-it-improves-the-prompt.md) | If a tool would make a prompt's job work better or more accurately, make the tool instead of another prompt rule; judgement stays in the prompt | Accepted |
| [0081](../../../demos/puls/docs/adr/0081-the-cook-searches-for-itself.md) | Puls's cook searches for itself, and a minion stays only if it beats no minion: the same accuracy and time for less cost | Accepted |

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
