# The design rules every change keeps

Loaded in every session, with `src/docs/CLAUDE.md`. It was that file's *Rules that always apply* until CL63.

The design decisions every change keeps, one line each: the *why* is its ADR in [src/docs/adr/](../../src/docs/adr/README.md), the
*how* of the C#, Razor, CSS and docs rules loads with the file you edit, from `.claude/rules/`.

**The engine and its parts**

- **Reuse before building, and keep it simple** — reuse the repo's components and maintained packages (MIT, Apache,
  BSD); the simplest design that meets the step, nothing for a future no plan names. [ADR 0030](../../src/docs/adr/0030-build-or-adopt.md), [ADR 0055](../../src/docs/adr/0055-keep-it-simple.md).
- **A tool when it improves the prompt** — if a tool would make a prompt's job work better or more accurately, make the
  tool instead of another prompt rule; judgement stays in the prompt. [ADR 0080](../../src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md).
- **Microsoft.Extensions.AI** — one `IChatClient` across providers; `FunctionInvokingChatClient` runs the tool loop; we
  sit at the Messages API tier with our own loop. [ADR 0002](../../src/docs/adr/0002-messages-api-tier-with-our-own-loop.md).
- **Framework and implementation are split, and this comes first** — `src/` is framework only, a demo holds its own
  config, docker, skills, deploy and tests; the solution tree is the disk; core ships no agent and every tool the model asks for is a pack. [ADR 0074](../../src/docs/adr/0074-the-folders-say-what-ships.md), [0076](../../src/docs/adr/0076-core-ships-no-agent.md).
- **Engine, identities and packs** — core names no product or country; an identity is who, a pack what it can do; a pack
  references no other pack, identity or eval; only `demos/puls/hosts/louis-agent.host-packs` knows them all. Weigh every reference against
  the engine split; a shared contract is a shared project or a tested duplicate. [ADR 0027](../../src/docs/adr/0027-engine-identities-and-packs.md), [0073](../../src/docs/adr/0073-packs-reference-no-evals.md).
- **Packs stay convertible to MCP** (`PackConvertibilityTests` holds the shapes) and **MCP exposes tools, not a
  model**: `louis-agent.mcp` hands an external client the raw tools; to have the agent do a whole task, call
  the API's sessions. [ADR 0028](../../src/docs/adr/0028-packs-stay-convertible-to-mcp.md), [AGENT_COMMUNICATION.md](../../src/docs/AGENT_COMMUNICATION.md).
- **Provider strategies** — where providers differ, one interface with an implementation per provider, picked in
  `LlmClientFactory`; never branch on the provider's name elsewhere. [ADR 0003](../../src/docs/adr/0003-provider-strategies.md).
- **Toolsets per session** — a session names its toolsets when it opens and keeps them; an engine per toolset list; a
  role's file may add its own, `?` only where the instance has it. [ADR 0006](../../src/docs/adr/0006-toolsets-per-session.md),
  [ADR 0025](../../src/docs/adr/0025-a-role-declares-its-toolsets.md).
- **Skill files follow the levels, a level-0 session is a mode, every agent loads the principles first** — prompts
  are embedded content, by level, one mode file per product, `principles.md` opening every prompt.
  [ADR 0011](../../src/docs/adr/0011-prompts-are-content.md), [0017](../../src/docs/adr/0017-skills-by-level.md), [0018](../../src/docs/adr/0018-built-in-prompts-embedded.md),
  [0019](../../src/docs/adr/0019-modes-one-file-per-product.md), [0020](../../src/docs/adr/0020-principles-for-every-agent.md).
- **Orchestration is an engine function** — agents as tools, typed results checked in code, one policy per turn
  tree; the orchestrator is an identity on core. [ADR 0031](../../src/docs/adr/0031-orchestration-is-an-engine-function.md).

**Safety and measurement**

- **Arguments as data** (never through a shell) and **guard the context** (oversized results summarised, cut-off
  calls never run, failures carry their reason). [ADR 0004](../../src/docs/adr/0004-arguments-as-data.md), [0005](../../src/docs/adr/0005-guard-the-context.md).
- **History is append-only** — nothing edits the system prompt, tools or earlier messages mid-conversation; the
  finished streamed turn is appended whole, thinking blocks included. [ADR 0008](../../src/docs/adr/0008-history-is-append-only.md).
- **The browser says what it is, and takes a site's no** — Chrome's own user agent or an honest name, never a disguise;
  every fetch obeys `robots.txt` and the page's no-AI signals, in code; a page's own data (JSON-LD) is read in code
  before its text. [ADR 0038](../../src/docs/adr/0038-the-browser-says-what-it-is.md), [0068](../../src/docs/adr/0068-every-fetch-obeys-robots-txt.md), [0067](../../src/docs/adr/0067-a-pages-own-data-before-its-text.md).
- **Measure every request, and show it** — the usage ledger is the only source of cost, priced by a dated table;
  unknown is `null`, never 0; every host shows the same totals. [ADR 0007](../../src/docs/adr/0007-measure-every-request.md).
- **Evals in C#** — every check is a C# eval on one harness, under `evals/`, apart from what it proves; one event
  contract; a live run waits for Louis's go. [ADR 0029](../../src/docs/adr/0029-simulations-in-csharp.md),
  [ADR 0047](../../src/docs/adr/0047-protocol-v2-one-event-contract.md).
- **Nothing is backward compatible until something is sold** — no migration, alias, lenient reader or second parser
  for what we store or send between our own parts. [ADR 0049](../../src/docs/adr/0049-nothing-is-compatible-before-a-sale.md).

**The code** (the how in `.claude/rules/csharp.md`)

- **No message text inline in C#**, and stdout is a protocol: logs go through `ILogger` to stderr; **no anonymous
  types or `dynamic`**: a message's holes are a record, an object built for a call a named local first. [ADR 0009](../../src/docs/adr/0009-message-text-as-enum-templates.md), [0119](../../src/docs/adr/0119-declare-name-then-pass.md).
- **Choices are enums, not strings**, parsed once with `SettingChoice.TryParse`, written with `.Wire()`; a repeated
  name is an enum too. [ADR 0024](../../src/docs/adr/0024-choices-are-enums.md), [ADR 0043](../../src/docs/adr/0043-repeated-names-are-enums.md).
- **Records for values, classes for things; JSON and MongoDB through records**, never an anonymous object, a static
  `Of` to build one from another; **dates are UTC**. [ADR 0040](../../src/docs/adr/0040-records-for-values-classes-for-things.md),
  [0041](../../src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md), [0048](../../src/docs/adr/0048-mongo-through-records.md),
  [0046](../../src/docs/adr/0046-dates-are-utc.md).
- **Separate by concern**: 200 lines of code per file (tests 400), one concern per folder, namespace = folder path,
  dependencies one way. [ADR 0010](../../src/docs/adr/0010-folders-by-concern.md), [0034](../../src/docs/adr/0034-code-a-person-can-maintain.md),
  [0044](../../src/docs/adr/0044-the-size-limit-counts-code.md).
- **Every class and method has a summary**, written like a person, with its pattern and its ADRs; a step inside a
  method gets one plain line. [ADR 0050](../../src/docs/adr/0050-summaries-like-a-person.md).
- **No nested conditionals, and an endpoint is a named method** returning `TypedResults`.
  [ADR 0051](../../src/docs/adr/0051-flat-conditionals-and-named-endpoints.md), [ADR 0045](../../src/docs/adr/0045-typed-answers-and-a-shared-contract.md).
- **Edit, never a script; layout only through the formatter; a rename through Rider; Rider's tools first.**
  [ADR 0052](../../src/docs/adr/0052-edit-never-script.md).
- **Test first**, and tests are platform-neutral (they run on Linux in the agent's containers).
  [ADR 0014](../../src/docs/adr/0014-test-first.md). **Test what you touch** while coding; the full suite once, when the feature is built. [ADR 0075](../../src/docs/adr/0075-test-what-you-touch.md).

**The web and the docs** (the how in `.claude/rules/razor.md` and `docs.md`)

- **Every site is built on the framework's web:** what would be the same on every site is in `louis-agent.web`, which names no
  site; a site holds only its words, brand, colours, addresses, own pages and which packs and views it uses, and names its areas with `AddSiteAreas`, **nothing wired by hand from the framework**; every
  place on a page has its own address, `/proof/rand`, never a `#`. [ADR 0084](../../src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md), [0107](../../src/docs/adr/0107-every-section-has-an-address.md), [0120](../../src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md).
- **A `.razor` holds markup only**, a page over about a hundred lines splits, **the web shows the live results**.
  [ADR 0034](../../src/docs/adr/0034-code-a-person-can-maintain.md), [ADR 0042](../../src/docs/adr/0042-the-web-shows-the-live-results.md).
- **A doc is true, and it teaches**: every statement is checked against the code and nothing is invented (unsure while
  writing: read the code first); it explains in detail, in the order a reader needs: what it is for, the main path as
  a story, the why of each part, a name of code after its idea. **No build history in a doc**: who picked what and how the work went stay in git. **One type per doc**; a done plan is deleted. [ADR 0013](../../src/docs/adr/0013-documentation-structure.md).
