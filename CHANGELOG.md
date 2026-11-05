# Changelog

> **Reference.** What was finished, newest first, one entry per feature, fix or cleanup. **Elsewhere:** what's still
> open in the [TODO](TODO.md); what each built lives on in its project's design doc ([projects](src/docs/projects/README.md)); the
> feature, fix and plan docs an entry links are at the tag `pre-cl24`, since CL24 folded them.

- 2026-11-05 — **The cook's answers, after the measure**: Puls no longer asks which shop, and the cook names the nearest ones unless you name one. Every sum of a recipe goes in one `Calculate` call: the amounts, the ones in the steps, an ingredient used twice, the packs and the total time. What can't be split rounds to a whole one (3 eggs, never 2⅔). The shop lookup runs with the first calls, which saves a trip to the model. Servings are people, so 12 servings is scaled; a count of pies isn't. A recipe's data saying `1 serving` gives way to the page's own words, since Taste's says that on a bobotie that serves 6 to 8.
- 2026-11-05 — **The cook searches for itself**: on one commit, the local minion matched no minion on accuracy (27 of 30 each) but took 60 s against 52 s and cost 7% more, so it failed the test it was built to pass. The cook calls `FindRecipes` itself, Puls sets no `MINION_LLMS`, and Azure no longer needs a local model before the next deploy ([ADR 0081](demos/puls/docs/adr/0081-the-cook-searches-for-itself.md)).
- 2026-11-05 — **The calculator pack**: `louis-agent.pack.calculator` gives a model one tool, `Calculate`, so its sums are worked out in code. The cook now scales a recipe's amounts with it, after it wrote 1 teaspoon × ⅔ as 2⅔. [ADR 0080](src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md) makes that the rule: if a tool would make a prompt's job work better or more accurately, make the tool.
- 2026-11-05 — **Puls shows the cook's report**: with `SHOW_SPECIALIST_REPORTS=true`, which Puls sets, the turn ends on a specialist's report and the user gets it as written. Retyping was about 96% a copy, at the output rate, the most expensive thing we pay for ([ADR 0079](src/docs/adr/0079-show-a-specialists-report.md)). Also for the cook: one recipe from one page, each pantry item by name, and 500 g or 500 ml or more of a staple as a row. `recipe-image` version 2 says it can't tell from a page's plain text, `recipe-as-source` version 2 reads a bare `Makes 12` as not people, and a search keeps a dish's other name (*melktert*, *milk tart*).
- 2026-11-05 — **F35.5 The minion and the cook work together**: a minion always runs a local model. It ranks the plain dish first and never gives another dish, and the cook takes a variant and uses every link before the web, saying so under *Unsure* when it does go there. A second search can skip the sites already asked, a site's top hits come in any order, and a site's suggestions no longer read as search results (*Puttanesca* for "pap and sous").
- 2026-11-05 — **FX45 A paid judge that can tell**: the local judge passed answers we broke on purpose, among them an amount times 7 and 200 ml of added truffle oil. Haiku 5.5 caught all 12, and agrees with Louis's labels on 27 of 30, for about a tenth of a cent an answer. A paid judge couldn't run before this, because every judge sent a temperature Anthropic refuses.
- 2026-11-05 — **F35.4 Many recipe sites**: 19 of 21 recipes came from Food & Home, because the prompt said to start there. With no site named, `FindRecipes` now asks four of the sites with their own search, at random and at once, and merges the hits, each saying whose page it is. No prompt names a site to start with. [ADR 0078](packs/docs/adr/0078-a-recipe-search-asks-several-sites.md).
- 2026-11-05 — **FX44 Ten shops near the user**: `FindShopsNear` lists the ten nearest supermarkets, since three hid Malmesbury's Checkers, 2.2 km away. The cook's format example no longer shows a made-up branch name.
- 2026-11-05 — **FX43 Shops near the user**: someone who named no shop got *Any supermarket near you.*, which tells them nothing. A new tool, `FindShopsNear`, lists the three supermarkets nearest their town from OpenStreetMap,
  and the cook names them under *Where to buy*. With no town it asks for one. `estimates-at-shop` version 3 needs the call and a shop with its place when the town is known; it used to pass `any` whatever the answer said.
- 2026-11-05 — **F57 The site tells the new structure**: every page and both films now describe the framework: a demo's program, its catalogue, and a pack's tool. Two new pages, *Packs* and *Build your own*, the second honest that the
  packages aren't on NuGet yet. The films play at the top of the README and in the runtime view as animated WebP, rendered from the site's CSS by `louis-agent.film-export`, with a still for reduced motion. Tests hold the film's names, the program
  shown and the pack list to the code, and a stale export fails one. [ADR 0077](demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md).
- 2026-11-05 — **Core ships no agent** (CL29.3): the coding agent is an identity in `demos/louis-agent/identity`, and every tool the model asks for is a pack: `files`, `git`, `dotnet`, `shell`, `web`, `paymo`, `devops`.
  Core has no `Tools/`: it keeps delegation, skills and the clock, which need its insides, and web access with its guards, which a pack reaches through `IPackContext`. A console app runs a pack from a NuGet package with no
  engine in its dependencies (`APackInstallsAsAPackageTests`). The golden files moved to the coding agent's tests and stayed byte-equal. [ADR 0076](src/docs/adr/0076-core-ships-no-agent.md).
- 2026-11-04 — **The hosts are libraries** (CL29.2): the coding agent's own programs, `louis-agent.coding-cli`, `louis-agent.coding-acp`, `louis-agent.coding-mcp`, `louis-agent.coding-eval-runner` and `louis-agent.coding-api`, and Puls's `louis-agent.puls-eval-runner` and `louis-agent.puls-api`, say what they load, and no framework project references a demo. `-p:WebApp` is gone: a program serves the web app it references. The code-review stack no longer carries Puls's evals.
- 2026-11-04 — **The folders say what ships** (CL29.1): `src/` holds only the framework, now with the eval harness in
  `src/evals/`; `packs/` holds tools and `packs-evals/` what proves them; anything that names a product moved to
  `demos/`, as someone's own project would be: `demos/puls/{identity,evals,web,hosts,tests,config,docker,skills,deploy}` and
  `demos/louis-agent/{web,hosts,tests,config,docker,skills}`, each told apart by `CONFIG_DIRECTORY`. Framework tests moved to `src/tests/`, pack tests to `packs-tests/`, framework docs to `src/docs/`, and a demo's or the
  packs' docs and ADRs to their own `docs/`, with a Setup per demo. The solution's groups match, and `SolutionLayoutTests` holds it. Paths only: no
  namespace changed. [ADR 0074](src/docs/adr/0074-the-folders-say-what-ships.md).
- 2026-11-04 — **CL28 The folders mirror the solution**: `src/` is `engine/` (core, choices, session-stream, moved in
  from the repository's root), `hosts/` and `web/`, as Rider's groups are, and what proves a pack or an identity moved
  under `evals/packs/` and `evals/identities/`, because someone can take the tools without the evals, or not own them.
  `SolutionLayoutTests` holds it. Paths only: no namespace changed.
- 2026-11-04 — **F50.2 What a list couldn't price, and packs and identities off the evals**: a total with unpriced rows
  says so (`± R312 + 2 items unpriced`), each eval run lists those rows (`tool-rows-marked`, never required), and 18 new
  rows of our own for what the saved recipe pages missed or priced from the wrong product (white fish had been priced as
  beef fillet); the rows are our own estimates, checked by Louis. What proves a pack or an identity moved to a
  project beside it that only the runner references (`….checks`, `….evals`), so they stand on core alone, ready for the
  engine's split. [ADR 0073](src/docs/adr/0073-packs-reference-no-evals.md).
- 2026-11-04 — **FX40 A recipe's headings aren't ingredients**: `ReadRecipe` numbered `<h3>CURRIED MINCE</h3>` and
  `***Filling` as ingredients. A heading in the list is now the section of the lines under it
  (`RecipeIngredient(Section, Text)`, like a step), printed as a sub-heading, and every other HTML tag leaves every
  text.
- 2026-11-04 — **F56 The packs' feeds as MCP resources**: the MCP server publishes every feed in the catalogue as a
  resource, `feed://{name}`, `application/json`, read whole when a client asks (`FeedResources`, one SDK resource per
  feed); `McpPrimitives.Add` sets up the tools and the resources once for both transports, and `Program.cs` keeps only
  the transports and auth (`EngineToolSync`, `SkillsToolProvider` moved to `Tools/`). The server's first tests,
  `tests/louis-agent.mcp-server.tests`, run the SDK's real client against it over in-memory pipes. Over HTTP the
  feeds sit behind the MCP key; the API still serves them open. [ADR 0072](src/docs/adr/0072-feeds-are-mcp-resources.md).
- 2026-11-04 — **F55 Stats SA's prices uploaded in the admin portal**: a pack can declare data uploads (`IDataImport`
  in core: a name, a description, the files it takes, a preview and a save, both answering a report; `IAgentPack.Imports`,
  empty by default; the catalogue gathers them and refuses a name twice), served behind the admin key at `GET
  /v1/admin/imports` and `POST /v1/admin/imports/{name}/preview` and `/save`, and shown on the portal's new Uploads
  page (`/admin/imports`). The first is `shop-prices`: Stats SA's two P0141 spreadsheets (all urban areas,
  provinces), every month in them read by column name; the preview lists the new months, the corrected ones with the
  prices that changed, the unchanged and any product with no names, and Save writes the new and the corrected. The
  names a list may use are kept in `statssa-names.json` by Stats SA's code. A product the newest month lacks is
  priced from the newest month that has it, and the line names that month. The shipped August table gains the
  Limpopo price the hand-made file missed. [ADR 0071](src/docs/adr/0071-packs-take-data-uploads.md);
  [write-up](src/docs/learning/16-human-in-the-loop.md#f55-the-preview-is-the-check-2026-11-04)
- 2026-11-04 — **F50.2 The prices as an open feed**: a pack can publish read-only data feeds (`IDataFeed` in core: a
  name, a description and its JSON; `IAgentPack.Feeds`, empty by default; the catalogue gathers them and refuses a name
  twice), and the API serves them open without knowing the pack: `GET /v1/feeds` lists each with its path, `GET
  /v1/feeds/{name}` returns the pack's JSON as it wrote it, `404` for a name no pack publishes. The first is
  `shop-prices`: Stats SA's tables and our own rows, read through the tool's cache, under a notice that says they are
  averages and whose they are. [ADR 0070](src/docs/adr/0070-packs-publish-data-feeds.md)
- 2026-11-04 — **F50.2 Where to buy and shop prices**: the shops pack (Checkers' catalogue read through our
  browser, for development only, since no retailer answered the 2026-10-25 emails) is gone, and two packs with one job
  each take its place, neither tied to food. `where-to-buy`: `FindBranches(shop, town)` finds a shop's three nearest
  branches on OpenStreetMap (Nominatim, one request a second), says which are in the town's municipality and gives
  the province; the shop rules (`estimates-at-shop`, `untooled-shops`, both at version 2) moved with it.
  `shop-prices`: `EstimatePrices(items, province, serves)` prices a list from Stats SA's average prices (August 2026,
  220 products, per province) and 18 rows of our own, the scaling, total and price per person in code, `?` for what it
  can't match, and the credit line; kept in MongoDB (`SHOP_PRICES_MONGO_URL`) one document per source and month,
  seeded from the shipped tables, read once an hour, the shipped tables when unset or down. The cook loads both beside
  its recipes, prices with `EstimatePrices`, names the branch with `FindBranches` and credits both; the cook eval is
  at version 14, the routing eval at 3. `SHOP_CATALOGUES` and `SHOP_STORE_CACHE` are gone.
  [ADR 0069](packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md);
  [write-up](src/docs/learning/04-tool-design.md#f502-one-job-per-pack-and-the-numbers-in-code-2026-11-04)
- 2026-11-04 — **The Studio's numbered steps** (F53's leftover): the Studio's form is four numbered steps in the
  order they are done, 1 Compose, 2 Check, 3 Try, 4 Publish (GOV.UK's task list; the admin review's finding 6), and
  *Try* picks the run to replay from the recorded runs the Evals page lists, this eval's own first, instead of an id
  typed from memory; its link opens the new run's page, not Benchmarks (`TryStep`, `ReplayChoices`, `StudioVerdict`)
- 2026-11-04 — **MCP_TRANSPORT is an enum**: the MCP server read its transport by comparing a string, so a typo
  (`htpp`) started it on stdio without a word; `McpTransportSetting.Parse` reads it once into `McpTransport` (ADR
  0024), unset is stdio and anything else stops start-up with the names it takes
- 2026-11-04 — **Third-party notices, and ADR 0067 and 0068 in the rules list**: `THIRD-PARTY-NOTICES.md` names
  every package, CDN script and font we hand out and the licence each keeps (the Blazor apps send their DLLs to every
  visitor's browser), `LICENSE` points to it, ADR 0016 says so; `docs/CLAUDE.md` names ADR 0067 (a page's own data
  before its text) and 0068 (every fetch obeys `robots.txt`) and stays at 200 lines; the work email left in git
  history is a TODO line for Louis's call (licence review, findings 4 and 5)
- 2026-11-04 — **FX38 One CI run per change**: every push to a pull request's branch started two runs in one concurrency group, so one cancelled the other (24 of the last 60 runs), and the cancelled one was usually the push run, the only one that checked formatting. CI now runs on pull requests and on `main`, the formatting check runs on both (a pull request against its base), `main`'s runs never cancel, the token is read-only, and the actions are on their Node 24 majors; the CI review's leftovers are done
- 2026-11-04 — **FX37 A git past its deadline is always a timeout**: CI now and then failed `AGitThatTakesTooLong_IsARefusalInWords` with `git read-tree took longer than 0 seconds` where the test expected `rev-parse`: `WaitForExitAsync` lets a process that already exited through a token that already ran out, so a fast `rev-parse` counted as in time. The publisher now asks the deadline itself after the wait, and the test runs a hundred times; [lesson](src/docs/learning/fixes-lessons.md#fx37--a-test-that-races-is-not-a-test)
- 2026-11-04 — **FX36 A redirect's body is observed**: the browser's log said `A task failed and nothing awaited it`
  (`Response body is unavailable for redirect responses`) at odd moments after a visit that followed a redirect:
  PuppeteerSharp sets every redirect's body task to that failure and never awaits it, and F52's last-chance handler
  reports such a task when the collector finds it. `Browsing/RedirectBodies` asks each redirect for its body, which
  awaits the failure and so counts it seen; a workaround for the library (25.14, still so at 25.16), removed when a
  release awaits its own task; [lesson](src/docs/learning/fixes-lessons.md#fx36--a-librarys-failed-task-is-yours-to-see)
- 2026-11-04 — **F53.3 The review queue, and F53 closed**: `/admin/review` shows the answers waiting for this
  reviewer's grade across the stored runs, one at a time, from one new API route (`GET
  /v1/admin/evals/reviews/queue?reviewer=&limit=`: every conversation whose human review is still pending and that
  this reviewer, or nobody, has not graded, with what the review panel needs); where the answer is from, its place,
  Skip, the blind review panel unchanged, a grade opening the next; the keys 1, 2 and 3 grade pass, fail or unsure;
  the sidebar's *Review* entry counts what waits and falls as you grade. The third child of F53, the admin portal
  organised by job (option B of the admin review, built unattended at Louis's word, PRs #132, #136 and this one);
  [write-up](src/docs/learning/16-human-in-the-loop.md)
- 2026-11-04 — **F54 The engine's website**: `src/louis-agent.site`, a public site for .NET developers in the
  Console theme (black, white and metallic grays) with five pages and two films of how the engine works, a short
  one (36 s) and a detailed one (60 s) whose slabs are the chat pipeline's middleware in its real order; static
  server-rendered Razor with no WebAssembly and no script of ours, the films CSS keyframes over CSS 3D, the site
  referencing no engine project and showing only what works today; 21 tests (the pages in memory, a golden per
  film, the stylesheet's rules), `Dockerfile.site`. PR #133; [ADR 0066](demos/louis-agent/docs/adr/0066-the-site-is-static-razor.md);
  [write-up](src/docs/learning/18-testing-and-shipping.md#a-host-that-is-only-content-f54)
- 2026-11-04 — **F52 Observability through OpenTelemetry**: every host sends its logs (Serilog's OTLP sink) and
  traces (the OpenTelemetry SDK) to the collector `OTEL_EXPORTER_OTLP_ENDPOINT` names, on the standard settings
  alone; a turn is an `invoke_agent` span named for its agent and tagged with its session and turn, never its words; a
  sub-session's turn nests under the parent's tool call across the HTTP hop; a crash is one Critical line with its
  stack and the last lines and spans still leave; the Aspire dashboard runs as its own stack on the PC
  (`docker-compose.observability.yml`), and `deploy.ps1 telemetry` sends Azure's to Application Insights; the guide
  has the table of what to log at which level. PRs #121, #122, #124, #126 to #130;
  [ADR 0064](src/docs/adr/0064-observability-through-opentelemetry.md)
- 2026-11-03 — **F53.2 One Evals page**: `/admin` is the Evals page in list/details shape: the list names every eval
  with its last run and per mode the pass@k with its move against the run before; the chosen eval is in the address
  (`?eval=`, the eval that ran last when none is named) and its details hold the Run button with its estimate, the
  manual, the scorecard, the timeline, the trend, what it checks and every stored run as a table; a run starts and is
  followed there, and a thin strip on every admin page says a run is going; the Benchmarks page and the old
  `/admin/benchmarks` and `/admin/maatstawwe` addresses are gone, the run page is at `/admin/runs/{id}`, and the
  sidebar is by job (Measure: Evals; Build: Studio, Prompts) (the second child of F53)
- 2026-11-03 — **F53.1 The run page opens on its answer**: a verdict card under the head (per mode passed of
  graded and the change against the eval's run before, from the trend the page already fetched; the cost, the
  requests, the answers still to grade), the page's blocks as tabs, and the tab and the explorer's place in the
  address, so Back goes up a level and a link opens the same view; the run is fetched only when its id changes
  ([ADR 0065](src/docs/adr/0065-the-view-is-in-the-address.md); the first child of F53, the portal organised by job)
- 2026-11-03 — **The cook's judge recalibrated**: against 60 answers Louis graded in the portal (run
  `cf37574d0f23`), `meal-for-people` agreed on 43 of 45 passes but only 8 of 15 fails, all of them Puls alone
  refusing, asking back or listing ideas; its rubric is now a short ordered decision that calls those `false` and
  keeps "can't tell" for a recipe with no amounts (rubric hash `2ab944be4e29`, was `438a8deb91f4`; eval version 12);
  that judge caught 14 of the 15 fails but failed 7 of the 45 passes (a follow-up whose last turn is only the list or
  the cost, a method written as a paragraph), so the rubric now says an earlier turn's dish counts and a short
  paragraph is a method (hash `51f8ca5a5f84`, eval version 13), and the dataset's 36 labels of run `76667d39dcdb`
  are replaced by those 60
- 2026-11-03 — **FX34 A live run shows each turn's cost**: the Benchmarks page's live table priced its turns only
  once the whole run had ended; now the API answers a run's requests so far from the usage ledger
  (`GET …/runs/{id}/requests`), joined as the run's end joins them, and the table asks after each turn's end, so a
  paid run's spend shows turn by turn and the live and final costs can't disagree
  ([fixes](src/docs/fixes/README.md), [lesson](src/docs/learning/fixes-lessons.md#fx34--a-cost-is-worth-most-while-its-being-spent))
- 2026-11-03 — **FX32 An edit in the studio keeps its eval's folder**: a draft of an eval the workspace has starts
  from a copy of the folder the runner lists it in (the cook's `Cook`), so its references, held-out scenarios, labels,
  manual and manifest are checked and published back into that folder with the draft's changes on top (an edit that
  changes a scenario bumps the manifest's version, for the runner to seal); a refused
  publish no longer ends its quote with `..`
  ([lesson](src/docs/learning/fixes-lessons.md#fx32--an-edit-starts-from-what-is-already-there))
- 2026-11-03 — **FX30 The delegation card's tile**: back in place (`../../brand/` from `css/chat/`), and every relative
  `url(…)` in Puls's stylesheet is tested to point at a file the app serves
  ([fixes](src/docs/fixes/README.md), [lesson](src/docs/learning/fixes-lessons.md#fx30--a-path-written-from-a-file-breaks-when-the-file-moves))
- 2026-11-03 — **FX29 A failed send says why**: Puls's send button seemed dead when the API refused the chat's key;
  the runner set its notice but never redrew the page, which shows it. It does now
  ([fixes](src/docs/fixes/README.md), [lesson](src/docs/learning/fixes-lessons.md#fx29--a-failure-has-to-reach-the-screen-that-shows-it))
- 2026-11-03 — **FX33 The header test reads the same on every checkout**: CL27's `PromptHeadersTests` makes its
  raw-string file LF before splitting it, so a Windows checkout's CRLF no longer fails it
  ([lesson](src/docs/learning/fixes-lessons.md#fx33--a-raw-string-has-its-files-line-endings))
- 2026-11-03 — **CL27 Prompt headers**: every built-in prompt (core's, Puls's and the shops pack's 26, and the
  tests' six) and the cook's judge rubric open with the Agent Skills spec's front matter: `name` (the file's),
  `description`, and under `metadata` the `level` of a mode, role or minion and the `toolsets` a role always loads
  (F33's line, moved there); one strict reader in core, `PromptHeaders` (YamlDotNet, newly in core), refuses an
  unknown key, a wrong name or level, a missing description or an unclosed fence, naming the file; `PromptCatalog`
  reads every header once, so a faulty one stops the host at start; the model and the judge get only the body (the
  prompt goldens unchanged, the rubric's hash still `438a8deb91f4`, no calibration moved); `RoleHeader` deleted,
  `RoleLevel` at the top of `Skills/`, a role's toolsets from `SkillLoader.ToolsetsOf`; `GET /v1/admin/prompts` and
  Puls's admin *Prompts* page list them. Ran unattended; its picks are in the PR for review. Guide
  [Build an identity](src/docs/guides/build-an-identity.md#3-write-its-mode)
  ([0063](src/docs/adr/0063-a-prompt-says-what-it-is.md), [write-up 07](src/docs/learning/07-context-engineering.md#a-prompt-says-what-it-is-cl27))
- 2026-11-03 — **FX31 The trend's change badge stays one pill**: on the Benchmarks page's category cards, `▲ +13%`
  no longer breaks into a two-line blob beside the value, and the small trend panels fill their card instead of
  three fixed columns with one empty ([lesson](src/docs/learning/fixes-lessons.md#fx31--a-small-copy-of-a-layout-needs-its-own-columns))
- 2026-11-03 — **F49.5 Studio: compose, try, publish**, and with it **F49 Evals as content** is done (its five
  children F49.1 to F49.5; what lasts of the parent plan is in [the evals' architecture](src/docs/architecture/evals.md#where-f49-came-from)):
  the admin portal's Studio page (`/admin/studio`) composes an eval as a form over `eval.json` (the head and modes,
  checks picked from the runner's catalogue with their fields drawn from each settings' schema, the rubrics, *View
  JSON*), saves it as an eval draft (the third view of `eval_drafts`), and asks the runner for everything: its
  catalogue (`--checks`, `GET /v1/admin/evals/checks`), its verdict on the draft written out as a folder with the
  eval's sample and transcript drafts (`--check`, which seals the manifest and runs CI's conformance gate;
  `POST …/drafts/{id}/verdict`), a try on a few samples as a replay at no cost (`run --folder --samples --replay`;
  `POST …/{id}/try`), and a publish that commits only a folder the runner passed beside its identity on
  `eval/{identity}-{name}`, through git's plumbing over a temporary index so the checkout is untouched, the push and
  the pull request left to the person, or a zip where there is no git workspace (`POST …/{id}/publish`); promptfoo in
  and out by a mapping table (`--import-promptfoo`, `--export-promptfoo`, YamlDotNet in the runner only;
  `POST /v1/admin/evals/drafts/promptfoo` names what it left out, `GET …/{id}/promptfoo`). The API still never
  references the harness: every call goes through `RunnerProcess`. Steps 1 to 8 ran unattended; their picks are in
  the PR for review. Guide [Add an eval](src/docs/guides/add-an-eval.md#9-or-compose-it-in-the-studio)
  ([0062](src/docs/adr/0062-the-studio-writes-what-git-keeps.md), [write-up 12](src/docs/learning/12-evaluation.md#compose-try-publish-f495),
  [write-up 18](src/docs/learning/18-testing-and-shipping.md#git-stays-the-truth-f495))
- 2026-11-03 — **F49.4 Studio: samples from chats and human review**: on a stored run in the admin portal, a
  conversation becomes a sample draft (*Sample* on its card: a variant of the scenario it ran, read from the eval's
  folder the runner's listing now names; the first question the input, the later ones the follow-ups, the person's
  id, expected values, categories and ideal answer on top, a key the scenario lacks refused) and a graded one a
  reference or known-bad transcript draft (*Keep as reference* on a pass, *Keep as known-bad* with a kebab-case
  reason on a fail; the API keeps it only when every decided verdict is a pass, or one is a fail; the lines are the
  conversation cut from the run as the eval folders hold it); the drafts wait in `IEvalDraftStore` (memory, or
  Mongo's `eval_drafts`) and on the Evals page under their eval's card, counted, listed and deleted, until F49.5
  publishes them; the API's `SampleBody` held equal to the harness's `SampleFile`; five routes under
  `/v1/admin/evals` (`runs/{id}/samples`, `runs/{id}/transcripts`, `runs/{id}/scenarios/{task}`, `{eval}/drafts`).
  Steps 3 to 6 ran unattended; their picks are in the PR for review. Guide [Add an eval](src/docs/guides/add-an-eval.md#6-add-the-reference-transcripts)
  ([0061](src/docs/adr/0061-evals-grow-from-use.md), [write-up 12](src/docs/learning/12-evaluation.md#evals-grow-from-use-f494))
- 2026-10-31 — **F49.3 Checks in packs**: a pack ships the checks that prove its tools, in a registry of its own
  that the composition's `EvalChecks.All` gathers; the shops pack's `ShopsChecks.All` holds `estimates-at-shop` and
  `untooled-shops`, and `PricedAtRetailer` (`priced-at-shop`, no entry) moved beside them, with what only they read
  (`Checks/Reading/`: the shop sites, the estimates and no-branch lines as plain scans, the branch under Where to buy,
  the scenario's `shop` and `town`); plain markdown reading (`Lists`, `Links`) moved down to the check contract
  (`Markdown/`), and the harness's `links-to-host` uses its `IsHost`; whether a price is given as fact comes in through
  a new `IPriceReader` the file names, Puls's `rand-prices`; the shops' site table lives in the pack and in Puls,
  held equal by `ShopTableTests`; Puls keeps no check (`PulsChecks` deleted) and no shop reading; `ToolHosts` deleted;
  the cook's 230 pinned grades unchanged throughout. Guide [Build a pack](src/docs/guides/build-a-pack.md#9-its-checks-when-it-proves-its-tools)
  ([0060](src/docs/adr/0060-a-pack-ships-its-checks.md), [write-up 20](src/docs/learning/20-extensibility.md))
- 2026-10-31 — **F49.2b The generic checks**: every family an eval tool ships has a general check, 24 in
  `GeneralChecks.All` by family (`Facts/`, `Answers/`, `Tools/`, `Compare/`, `Similarity/`, `Language/`): new
  `turns-under`, `tokens-under`, `not-contains`, `starts-with`, `length-under`, `mentions-expected`, `is-json`,
  `tool-not-used`, `tool-used-with`, `tool-calls-under`, `links-to-host`, `near` (Levenshtein) and
  `answers-in-language` (a word count per language, `af` and `en`, no model); **extract, then compare**: a compare
  (`amount-under`, `number-named`, `image-shown`) reads its value through a reader the file names beside the key,
  from a second registry (`ReaderCatalog`: the harness's `currency-amount`, `digits`, `markdown-images`, and every
  identity's through the composition's `EvalReaders`); Puls ships readers, not checks (`rand-total`, `servings`,
  `recipe-photos`, Adapters over its scoring code), its `CookNoError`, `WithinBudget`, `RecipeImage` and
  `ServesAsAsked` deleted and the cook's file naming the general types under the old names (version 11); no grade
  moved: 230 pinned on the 26 transcripts (`CookGradesTests`) and 140 of 140 on a $0 replay of the baseline;
  `similar` (embeddings) left for when an eval needs it. Guide [Add an eval](src/docs/guides/add-an-eval.md#4-choose-the-checks)
  ([write-up 12](src/docs/learning/12-evaluation.md), [write-up 20](src/docs/learning/20-extensibility.md))
- 2026-10-30 — **F49.2 Evals as files**: an eval is a folder with an `eval.json` (its name, the identity it proves,
  a hand-bumped version, its modes and its checks by type with their settings), its manual, dataset, rubric and
  transcripts beside it, read strictly by `FileEval`; Puls's `puls-routing` and `puls-cook` live in
  `identities/louis-agent.identity.puls/Evals/`, the cook's version unchanged and its recorded baseline replayed for
  $0 with 140 of 140 grades the same; the runner finds every eval folder through the composition, and `EVALS=`
  chooses by identity (a name no eval proves is refused); `IEvalSet` and the sets are gone; new general entries
  `routed-to`, `judge` (a rubric file) and `human`; Puls's own fixed rules beside the identity (`Evals/Checks/`),
  which references only core and the check contract, reaching the runner through the composition's `EvalChecks`;
  its judge `meal-for-people` in `identities/louis-agent.identity.puls.judges`, which only the runner references;
  `evals/louis-agent.evals.puls` deleted; the template is a folder to copy (`evals/template/Example/`, zero C#); a
  turn's facts read by the harness for every eval. Guide [Add an eval](src/docs/guides/add-an-eval.md)
  ([0058](src/docs/adr/0058-an-eval-is-a-folder-of-files.md), [write-up 20](src/docs/learning/20-extensibility.md))
- 2026-10-30 — **F49.1 The checks catalogue**: the check contract (`ICheck`, `Grade`, `Trial` and the turn types) out
  of the harness into `evals/louis-agent.evals.checks`, which a pack can reference; grader became check in C# (the
  wire keeps `grader`); a check type is named in kebab case and built from a strict typed settings record
  (`CheckType<TParameters>`), found in `CheckCatalog`, which lists each entry's JSON schema; the harness's entries are
  `GeneralChecks.All`: `no-error`, `within-time`, `cost-under`, and two new checks, `contains` (one of a list of
  texts, case and accents ignored) and `tool-used` (by the agent or a specialist). No regex check
  ([0057](src/docs/adr/0057-regex-is-the-last-option.md)); `routed-to` waits for F49.2
  ([0056](src/docs/adr/0056-a-check-is-a-catalogue-entry.md), [write-up 20](src/docs/learning/20-extensibility.md))
- 2026-10-29 — **FX23 A script that never reads its input keeps its exit code and output**: `ProcessRunner` guards the
  stdin write and close, so a child that exits first ("Broken pipe") no longer reports as a failed run with its output
  lost; `CreatedTool_SupportsPowerShellAndBash` is no longer flaky (ten green runs on the fix, eight of ten red before)
- 2026-10-29 — **FX28 A replay is marked, and can be started, on the Benchmarks page**: a replay's row says *replay
  of …*, the card's *Last run* line gets a *replay* chip, the Run button's estimate leaves replays out as it leaves
  re-scores out (the cook's free replay `c0362ff73151` had become "≈ R0 for 1 repeat"), and a recorded run's row has
  a *Replay* button; `StoredRunRow.Recorded` from the run's `web` setting (`EvalSettings.Recorded`), beside CL26's
  `ReplayOf`
- 2026-10-29 — **CL26 The eval card, reworked, and the stylesheets split**: the Evals page's card leads with its
  latest run as a ring per mode, the runs before it down a timeline with a bar per mode and a tag that says
  recorded, replay or re-score (a run row says what it replayed, `ReplayOf`), and what it checks in a fold
  (`EvalCardView`, a Presentation Model); both web apps' `app.css` became an `@import` list of area files (Puls 18,
  the code-review app 8), byte-identical when joined, and a `.css` file holds at most 200 lines of rules
  ([0053](src/docs/adr/0053-stylesheets-by-concern.md); the byte-checked move is
  [0054](src/docs/adr/0054-a-move-may-copy-line-ranges.md))
- 2026-10-29 — **CL25 Every CLAUDE.md in 200 lines**: `docs/CLAUDE.md` from 558 lines to under 200 and every
  `CLAUDE.md` and `.claude/rules/` file held to 200 by `ClaudeMdSizeTests`; the why of each rule in its ADR (new:
  [0050](src/docs/adr/0050-summaries-like-a-person.md) summaries like a person, [0051](src/docs/adr/0051-flat-conditionals-and-named-endpoints.md)
  flat conditionals and named endpoints, [0052](src/docs/adr/0052-edit-never-script.md) edit, never a script), the how in
  path-scoped rules files (`csharp.md`, `razor.md`, `docs.md`), the how-tos in [guides](src/docs/guides/README.md)
  (get started, finish a step, add a skill, debug and logs, common errors); one rule, *Rider's tools first*, in
  place of the list of Rider's tools
- 2026-10-29 — **FX27 A sub-session's answer carries no usage**: the footer under a specialist's or minion's answer
  names the child, never its tokens or cost, which are zero on a replay and made the parent's next request miss its
  recording; the usage stays in the ledger and the parent's turn, and a fresh cook recording replays with no miss
  ([fix doc as it was](https://github.com/bosbevk/louis-agent/blob/f54d0e7ed2c93bd177dd300f63b5d2df5fd98931/docs/fixes/FX27-sub-session-footer-cost.md))
- 2026-10-29 — **FX26 A replay miss is marked `replay-miss`**: the replay client throws `ModelReplayMissException`, the
  API streams it as `error.type` `replay_miss_error` (`StreamErrorKind` in session-stream), and the runner sets the
  turn's failure, so a replayed trial with no recording counts as infrastructure, not as the agent's failed answer
  ([fix doc as it was](https://github.com/bosbevk/louis-agent/blob/deb14797f4be3e6b8fa0b66eab9900aedb5f87c9/docs/fixes/FX26-replay-miss-kind.md))
- 2026-10-29 — **CL24 Done docs folded**: the docs of built features and done fixes folded into the project design docs
  and the write-ups, then deleted; the plans of built work deleted; every link to them at the tag `pre-cl24`. Each
  fix's row keeps its symptom and cause; the lessons are in [Lessons from the fixes](src/docs/learning/fixes-lessons.md);
  [platform.md](src/docs/plans/platform.md) is the living roadmap with Phases 0 to 3 done; the rule *a feature holds at most
  about ten steps* and its sub-features ([docs/CLAUDE.md](src/docs/CLAUDE.md), rules 2 and 3) are TODO lines
  ([plan](https://github.com/bosbevk/louis-agent/blob/113501480e9df53e94a84a447f4c3abc5c93e081/docs/plans/CL24-done-docs-folded.md))
- 2026-10-29 — **F18 Evaluation**: every check is a C# eval on one harness (`evals/louis-agent.evals/`): `IEval` and
  `IEvalSet`, datasets in Inspect's Sample shape with a held-out split and a hash, code graders first, a model judge
  calibrated against people's labels (90% of each class), human review in the portal, pass@k and pass^k, a roll-up by
  category, re-scores and replays for $0; the runner and the API share one event contract, protocol v2 (`evals/
  louis-agent.evals.protocol`, [ADR 0047](src/docs/adr/0047-protocol-v2-one-event-contract.md)); the store goes through
  typed records ([ADR 0048](src/docs/adr/0048-mongo-through-records.md)) and keeps nothing older than its build ([ADR
  0049](src/docs/adr/0049-nothing-is-compatible-before-a-sale.md)); the portal reads only the contract's records. First
  v2 baseline: `puls-cook` 23 of 24 in its cook mode, `puls-routing` 72 of 72, $0.37 together; the local judge 31 of
  36, below the bar ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F18-evals.md), write-ups
  [12](src/docs/learning/12-evaluation.md) and [13](src/docs/learning/13-cost-and-token-economics.md))
- 2026-10-27 — **FX25 The eval runner sees only what it reads**: a run's process got the API's whole environment; `RunnerEnvironment`
  keeps an allowlist (the runtime's, the runner's settings, and an Anthropic judge's key under the judge's name only), so the Mongo
  login, the admin key and the provider keys never reach an eval or a script ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX25-runner-environment.md))
- 2026-10-25 — **FX22 The recipe's image renders**: an image in an answer renders when its exact URL came back in a tool
  result of the same turn, the cook's sub-session included (`ImageSources`, `Block.Images`, `Markdown.ToHtml(markdown, shown)`),
  not only from a shop's host; a delegation call's own result proves nothing; per turn ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX22-images-from-tool-results.md))
- 2026-10-25 — **CL20 API standards**: every route of the agent API under `/v1` but `/health`, mapped in one place
  (`ApiMap`); resources as nouns (the git routes by resource: `commits`, `commits/{sha}`, `branches/{name}` with its
  slashes, a merged branch deleted with `DELETE`), and both creates answer `201` with `Location`; every `/v1` route
  needs the key unless the code marks it open (the prices and the rates), so `GET /llm-profiles`, open by omission,
  is closed, and an unknown `/v1` path is a JSON 404, not the web app's page; every client names the whole route
  (`v1/sessions`), its setting only the host; CL14's `GET /personalities` and `personality` field gone; the git
  endpoints split by resource (`Endpoints/Git/`); the route table pinned by a golden file, ADR 0039's rule checked on
  it by tests ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL20-api-standards.md), [ADR 0039](src/docs/adr/0039-api-routes-under-v1.md), write-up
  [11](src/docs/learning/11-protocols.md))
- 2026-10-24 — **FX21 The browser says what it is**: `BROWSER_USER_AGENT`, what the headless browser tells every site it
  reads for a user; the Puls stack, its launch profile and the Azure app set `Puls/1.0 (+https://github.com/bosbevk/louis-agent/blob/main/demos/puls/docs/SITE_OWNERS.md;
  reads a page on a user's behalf)`, unset sends Chrome's own, the automation marker is never touched, and never a
  person's name or place ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX21-the-browser-says-what-it-is.md))
- 2026-10-24 — **CL19 Config and Docker by place**: a folder is one kind of thing for one place. `config/` is what the
  app reads at run time (unchanged, so no code moved; `rider/` for the IDE templates); `docker/` is this PC (`images/`
  for every Dockerfile, Ollama's from `services/`; two stacks, one Puls container that peers with itself); `deploy/azure/`
  is the cloud: `puls-app` on a `ts-forwarder` sidecar that carries both tunnel ports from one tailnet node, its yaml
  and SearXNG's tracked with `secretRef`s and no secret value, `deploy.ps1` (build, secrets, update, searxng-settings,
  logs) and the how-to; `tailscale serve` on the PC is the home end. A README in each of the three folders
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL19-config-by-place.md))
- 2026-10-20 — **CL15 Engine, identities and packs** (the platform plan's Phase 2): core is the engine and names no
  product, shop or country; Puls is an identity (`identities/louis-agent.identity.puls`: its voice, modes, roles and
  the packs it names) and the shops a pack (`packs/louis-agent.pack.shops`: the toolset, its guide, settings and
  texts); one `AgentCatalog`, built by the composition root (`src/louis-agent.composition`) from `IDENTITIES` and
  `PACKS`, checked at start-up; `HOST_TIME_ZONE` replaces the South African default; every pack convertible to an MCP
  server, held by `PackConvertibilityTests`; the fakes the test projects share in `tests/louis-agent.testing`. During the
  build Louis added two rules, held by ratchet tests: a `.razor` holds markup only (`RazorHasNoCodeTests`) and a file
  holds at most 200 lines, a test file 400 (`FileSizeTests`), with core, the API, ACP and the web apps sorted and split
  by concern ([ADR 0034](src/docs/adr/0034-code-a-person-can-maintain.md)); the ACP host pinned by a transcript test before
  it moved. Proved
  by three goldens and one paid cook run on the finished branch, `b4a6ea22b178`, read against the two runs on main in
  write-up [07](src/docs/learning/07-context-engineering.md) ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL15-engine-identities-packs.md),
  [ADR 0027](src/docs/adr/0027-engine-identities-and-packs.md), [ADR 0028](src/docs/adr/0028-packs-stay-convertible-to-mcp.md),
  PR #71, write-ups [04](src/docs/learning/04-tool-design.md) and [18](src/docs/learning/18-testing-and-shipping.md))
- 2026-10-19 — **FX20 Skill files wait for approval** (the platform plan's Phase 1, its last door): a skill file the
  agent writes goes to `Skills/agent/functions/pending/`, active only in the engine that wrote it, until the user's
  `/approve <name>` moves it up for every later session; `/reject` deletes it and `/tools` lists it, as agent-built
  tools already did ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX20-skill-files-wait-for-approval.md))
- 2026-10-19 — **FX19 Git arguments as data** (the platform plan's Phase 1): every git tool runs from a list of
  arguments, the model's values after `--end-of-options` and paths after `--`, a value that starts with `-` refused
  before git runs, and `SetConfig` limited to `user.name` and `user.email`; `ArchitectureTests` keeps a command-line
  string out of core ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX19-git-arguments-as-data.md), [ADR 0004](src/docs/adr/0004-arguments-as-data.md) amended)
- 2026-10-19 — **FX18 Auth fails closed** (the platform plan's Phase 1): the API, and the MCP server over HTTP, refuse
  to start without a key unless `AGENT_AUTH=off` says so on purpose; a key that is set is always checked. One pure
  rule in core, `Guardrails/HostAuth`, replaces the two hosts' own checks ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX18-auth-fails-closed.md))
- 2026-10-19 — **F29 Validated specialists**, closed by [the platform plan](src/docs/plans/platform.md) (Phase 0): its
  four steps were in git (`d79dbb1`, `ae257c5`, `d7afa23`, `28c4901`) though its doc said *planned*. `ROLES_ENABLED`
  (`PERSONALITIES_ENABLED` before CL14) gates the roles an instance may use; Puls opens with `specialists` (then
  `agents`) and `web`, answers the web itself and hands only cooking to the cook, until each other role has a report
  shape, a relay rule and a check ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F29-validated-specialists.md),
  [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F29-validated-specialists.md))
- 2026-10-19 — **F26 Specialist follow-up**, built 2026-10-15, closed by the platform plan: a sub-session stays open
  for its chat and Puls continues it by handle (`ContinueSubSession`), so a follow-up on the cook's meal goes to the
  same cook with its context; the firmer follow-up rule measured 4 of 4 continued, and the Werk panel names the role.
  Its open item, a continued specialist's ledger rows, is a fix on Puls's track
  ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F26-specialist-follow-up.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F26-specialist-follow-up.md))
- 2026-10-19 — **F25 Local cook**, built 2026-10-15, closed by the platform plan: Puls's first local specialist, a
  South African meal with its recipe and a shopping list priced in rand at the shops near the user, the routing and
  the sources measured; Tavily searches without refusals. Its open item, the cook's unconfirmed prices (*geskat* or
  *unknown*), is on Puls's track ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F25-local-cook.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F25-local-cook.md))
- 2026-10-19 — **F22 Role tools**, built 2026-10-13, closed by the platform plan: the tools Puls's specialists need,
  `GetToday`, attachments by name, documents (PdfPig, OpenXml, ExcelDataReader), `Blame`, `FileHistory` and
  `SearchCommits` with arguments as a list, `ReadTable`, `ConvertCurrency`, and the user's place. Its open item, binary
  attachments sent as files, is a fix on Puls's track ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F22-role-tools.md),
  [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F22-role-tools.md))
- 2026-10-19 — **F21 Personalities**, built 2026-10-13, closed by the platform plan: each session (and sub-session)
  names its personality, now its role; sub-sessions get no router, and all 24 delegations in the three-run delegation
  check went to the role the router names. Its open item, Louis's corrections to the Afrikaans, is on Puls's track
  ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F21-personalities.md), [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F21-personalities.md))
- 2026-10-18 — **FX17 The cook's labels and counts**: the recipe line and the tally in the task's language (*Dien* /
  *Serves*, *Geprys* / *Priced*), and a count of packs after the name and the price, so the photo stays first; and a
  follow-up's card names the cook while it works, from the role each session's `message_start` now carries
  (`session_role`) ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX17-cook-labels-and-counts.md))
- 2026-10-18 — **F34 Shop deals**: Checkers searched in the store that serves the user's town (its own address call,
  the one approved direct call, ADR 0023), found once per town and kept a day; `SearchSpecials` (multi-buys) and
  `SearchPromotions` with the shop's wording; the cook prices only shops it has a tool for, none: the list with `-` and
  a no-catalogue line, deals first, packs the recipe needs, every product with its photo, and Puls asks how many people.
  A round's calls run at once for read-only toolsets (ADR 0026), two searches at once per shop with a pause after a
  refusal. The cook check compares Puls with the cook's tools against Puls with its cook: run `d94597a51d4d` priced 31 of
  31 with the cook (Prince Albert rightly without prices), 13 of 18 alone ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F34-shop-deals.md))
- 2026-10-17 — **CL14 The public word: role**: what callers see says role: `POST /sessions` takes `role` and the session
  JSON returns it, `GET /roles`, `ROLES_ENABLED`, the model's `StartSubSession(…, role, …)` and the prompts that teach
  it, the voices' headings, the web apps and the benchmarks. Expand, then contract: the old words are still read
  (`personality`, `GET /personalities`, `PERSONALITIES_ENABLED` with a warning) until a TODO line removes them, and
  recorded data (stored runs, old chats, the stream fixture) is read either way for good. Checked by hand (free): the
  language menu, a cook opened with either word, and a run stored before CL14 in the portal
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL14-public-word-role.md))
- 2026-10-17 — **CL13 Skills by role**: `Skills/` in the seams' pattern, the contract at the top, `Providers/`
  (`MarkdownSkillProvider`, `CompositeSkillProvider`) and `Roles/` (`RoleCatalog`, `RoleLevel`, `RoleException`,
  `RoleHeader`); "personality" becomes "role" in the code, `RoleNamingTests` holding it; the unused
  `PersonalitySkillsProvider` deleted, and `MarkdownSkillProvider`'s unused default-routing flag; inside `Skills/` the
  top uses nothing below it (`ArchitectureTests`). The golden files never changed, and a cook session on the rebuilt
  `puls-agent` still opens with `web, shops` (checked by hand, free). The public word (`personality` on the API and
  `StartSubSession`) is CL14's ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL13-skills-by-role.md))
- 2026-10-17 — **F33 Shop catalogues**: a shop's products and exact prices read from its own catalogue through the
  guarded browser (`Tools/Web/Browsing`, extracted from F32's renderer), Checkers first (`Tools/Shops`,
  `Providers/CheckersCatalogue`); a `shops` toolset with `FindProducts`, off unless `SHOP_CATALOGUES` names the shop.
  The cook loads it itself, from its role's header (`toolsets: shops?`), optional toolsets (`?`) loading only where an
  instance has them. Its run priced 19 of 23 ingredients with sources (3 more at their price per kg, which the scorer
  now counts apart), F32's 0 of 32. Plans now carry a namespace map ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F33-shop-catalogues.md))
- 2026-10-17 — **F32 Self-hosted web**: `WebSearch` through SearXNG in the Puls stack (development and benchmarks
  only, ADR 0023: the sold endpoint keeps Tavily) and pages drawn with JavaScript rendered by a headless Chrome
  (PuppeteerSharp), every request a page makes held to the address rule and no WebSockets; both containers pinned by
  digest, off the database's network. Choices are enums (ADR 0024); the web toolset is laid out by role (`Search/`,
  `Rendering/`, each with its `Providers/`). Its run ($0.057) searched and rendered with no key but priced nothing:
  F33 follows ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F32-self-hosted-web.md))
- 2026-10-17 — **F31 Benchmarks in the admin portal**: the checks (`benchmarks/`: a shared Python harness, the cook
  check, the routing check and a free stub) run only from the portal, and the API records each run from the
  script's stdout line protocol into MongoDB (`puls-mongo`, `puls_benchmarks`), costed from the usage ledger; the
  explorer reads a run at every level (nine charts per mode, the conversations side by side, each turn replayed, each
  request) and a trend per check, every chart explaining itself; a price counts only with its source. The first paid
  run (cook check, $0.045, the ledger's figure exactly) found Tavily out of credits. The portal is in English at
  `/admin/benchmarks`. Closes F27 ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F31-benchmarks-portal.md))
- 2026-10-17 — **FX16 A price is checked against its product**: `VerifyQuote` refuses a quote that is only a price
  (`R22.99`) unless it names the product, and then finds it only right after the product's name, no other price
  between; it was *Found* for any product at that price ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX16-short-quote-match.md))
- 2026-10-17 — **CL10 Clear the decks**: `samples/` goes (both checks, 18 recorded runs, the order-service demo and
  its compose file) and `docs/BENCHMARKS.md`; they're kept at the tag `pre-cl10`, where the history docs now link. The
  admin portal stops reading and writing run folders: no checks until they're rewritten, with scripts in `benchmarks/`
  and results in SQL Server's `puls_benchmarks` (the next feature, before F30's minions are measured). Roles are
  named, not derived: `roles.json`, `RoleFor` and `puls-specialist` go; the other specialists stay, off by
  `PERSONALITIES_ENABLED` ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL10-clear-the-decks.md))
- 2026-10-17 — **CL9 Modes per product**: a level-0 session is a mode, one file of voice and routing in a folder per
  product (`agent/louis-agent/voice.md`, the default for the CLI, Rider and code review; `voice-orchestrator.md`), or a
  language (`agent/puls/voice-af.md`, `voice-en.md`) that loads Puls's shared routing (`puls/voice.md`); Puls's *Taal*
  menu picks the language for new chats. Every agent loads the same principles first (never lie, cheat, steal or
  murder). Each product routes only what it does; `AGENT_FUNCTION` and the placeholder code-review skill go; the
  `agents` toolset is `specialists`. Measured from the start-up logs: the coding agent's fixed prefix 29,464 → 28,663
  tokens, the cook's 4,518 → 4,773 (the principles), Puls's chat 6,703 (Afrikaans) and 6,694 (English); the paid
  checks were skipped until they're rewritten after F30 ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL9-modes-per-product.md),
  [ADR 0019](src/docs/adr/0019-modes-one-file-per-product.md), [ADR 0020](src/docs/adr/0020-principles-for-every-agent.md))
- 2026-10-16 — **CL8 Skills by level**: the built-in prompts move into `louis-agent.core/Prompts/`, one folder per
  level a session runs at (`agent/` level 0, `specialists/` level 1, `minions/` level 2 for F30) beside the shared
  `toolsets/`, and are read only from the assembly (`Microsoft.Extensions.FileProviders.Embedded`): a role opens only at
  its own level, a copy on disk no longer overrides one, and a wording change is a rebuild. The repo's `Skills/` keeps
  what the agent writes; the placeholder time-logging skill goes; `SkillsLayoutTests` holds the layout
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL8-skills-by-level.md), [ADR 0017](src/docs/adr/0017-skills-by-level.md),
  [ADR 0018](src/docs/adr/0018-built-in-prompts-embedded.md))
- 2026-10-15 — **Licence: all rights reserved** (was AGPL-3.0): public to read, but no use, copying, changes or hosting
  without written permission; earlier versions keep their licence ([ADR 0016](src/docs/adr/0016-licence-all-rights-reserved.md))
- 2026-10-15 — **Licence: AGPL-3.0** (was MIT): still free to use, change and share; whoever changes it and runs it as a
  service must publish the changes, so a hosted Puls's improvements stay open ([ADR 0015](src/docs/adr/0015-licence-agpl.md))
- 2026-10-15 — **F24 Landing motion**: Puls's landing page as a moving page on the brand's idea (a puls is a disturbance
  that travels from its source): dark water behind it, the app's three panels playing a scripted turn in the page, a
  pinned *In die app* tour, a Kenmerke bento over two marquees, Syfers counting up the measured numbers, an accordion
  FAQ, every link landing its section centred; the app gets the water behind an empty chat, a delegation card that
  opens with a round plus, costs that count up, Blazor's real loading count, and an *Animasies* switch that overrides
  the system's reduced-motion setting in this browser ([feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F24-landing-motion.md),
  [plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F24-landing-motion.md))
- 2026-10-14 — **FX15 A test needed `config/.env`**: the display-currency test names the tracked rates and prices
  files; it had relied on `config/` being found next to a `.env`, which a fresh clone lacks, so the first CI run found
  it ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX15-test-needs-env-file.md))
- 2026-10-14 — **CI**: `.github/workflows/ci.yml` builds and tests the whole solution on Ubuntu on every push and pull
  request (the test results kept as an artifact); a regression is caught before a review, not only locally
- 2026-10-14 — **F5 follow-up, sub-session usage rolled up**: a delegated answer's usage line, the chat total, `/usage`
  and `GET /sessions/{id}` include the specialist's requests, tokens and cost, as one `sub_session` record per reply
  (with `requests`, the model requests it stands for) that is also a row in the parent's ledger; ledger reports list
  those rows without totalling them, since the specialist's own rows are in the ledger it ran on
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F05-usage-rollup.md))
- 2026-10-14 — **FX9 Cancelled requests in the ledger**: a non-streaming request that times out or is cancelled (the
  orchestrator's triage) is written to the ledger as `cancelled` with unknown tokens, as a cancelled stream already
  was; provider errors stay unrecorded ([fix](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX09-cancelled-request-usage.md))
- 2026-10-14 — **CL7 Docs tidy**: the top of `docs/` keeps the eight docs most readers need. The session picture moves
  to `docs/specs/images/` (the 43 KB SVG; the 1.1 MB PNG of it goes), How a turn works joins arc42's runtime view, and
  the runbooks move beside the orchestrator's project doc; every link repointed ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL7-docs-tidy.md))
- 2026-10-14 — **Test first**: a step that changes behaviour starts with its tests, run and seen failing, then the
  code; one commit with both, the red output in the step's explanation ([ADR 0014](src/docs/adr/0014-test-first.md))
- 2026-10-14 — **CL6 arc42, ADRs and C4**: the architecture in arc42's twelve sections in `docs/architecture/`
  (ARCHITECTURE.md's text moved unchanged, plus goals, constraints, C4 context, container and component diagrams,
  quality scenarios and a glossary), and 13 decisions as ADRs in `docs/adr/`, each written from where it was
  recorded and linking its sources; the guide's rules link their ADRs, and a plan that makes a lasting decision adds
  one ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL6-arc42.md))
- 2026-10-14 — **CL5 Guides by folder**: the development guide, which loads into every Claude Code session, keeps what
  every task needs (~11.4k → ~6.2k tokens); core's how-tos move to `louis-agent.core/CLAUDE.md` (~4.8k), the tests' to
  `tests/CLAUDE.md`, the hosts' to `src/CLAUDE.md`, which Claude Code loads only when working there (checked with
  `/context` in a fresh session). None in `Skills/`, which core embeds and reads as personalities
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL5-guides-by-folder.md))
- 2026-10-14 — **CL4 One type per doc**: every doc is one Diátaxis type (tutorial, how-to, reference, explanation;
  plans a fifth kind), named in the docs index and in a quoted opening line. Six mixed docs split, moving facts, not
  dropping them: the README 230 → 152 lines, the TODO's Done to this file and its ticked lines removed, the benchmark
  runs to `docs/BENCHMARKS.md`, Architecture's settings to SETUP and MODELS, How a turn works' how-tos to the guide.
  Five out-of-date facts found on the way were fixed in their own commits (the default model, tool registration since
  F5, F5's status, deleting `personality.md`, "no shell"); a link check ran after every step
  ([plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL4-doc-types.md))
- 2026-10-14 — **F5 Toolset profiles**: a session names its toolsets when it opens and keeps them; an engine per
  toolset list with only those tools and their guides; sub-sessions on a peer (`StartSubSession`), which Puls's
  delegation runs on. Short benchmark: prefix −50% (~48.5k → ~24.4k tokens), run −23%, 4/4 fixes, the cache still hits
  ([numbers](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#f5-benchmark-short-run-2026-10-13)); the full 10-fix run waived by Louis
  ([F5](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F05-toolset-profiles.md))
- 2026-10-14 — **F20 Puls look**: a landing page and a dark three-panel dashboard in Puls's colours, with the Werk panel showing each delegation live (specialist, steps, sources, cost); checked in a real browser with a live delegation ([F20](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F20-puls-look.md)).
- 2026-10-14 — **FX14 Sub-session prompt budget**: `SUB_SESSION_PROMPT_BUDGET` (80k, under the 100k price step) stops a specialist's prompt growing into long-prompt prices; it answers from what it has ([FX14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX14-sub-session-prompt-budget.md)).
- 2026-10-14 — **FX13 Counting to the analyst**: Puls's router sends counting to `files` + a script, and the reader hands counting back instead of reading every file ([FX13](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX13-counting-to-the-analyst.md)).
- 2026-10-14 — **FX12 Env templates readable**: `.env.example`, `.sample` and `.template` files are no longer treated as secrets ([FX12](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX12-env-templates-readable.md)).
- 2026-10-14 — **FX11 Empty sub-session answer**: a specialist that ends with no text (its 10 rounds used) is asked once more in the same session instead of Puls starting over ([FX11](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX11-empty-sub-session-answer.md)).
- 2026-10-14 — **FX10 Optional peer name**: `StartSubSession` without `agent` uses the only peer; Puls's calls without it were refused ([FX10](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX10-optional-peer-name.md)).
- 2026-10-12 — **Ollama on the GPU and local triage**: `ollama-nvidia` and `ollama-amd` services sized for a 4 GB card
  (32k context, flash attention, 8-bit KV cache); Ollama's client waits 10 minutes, not 100 seconds. The short demo's
  triage on `qwen3:4b-instruct-2507` (profile `.env.ollama-triage`): 4 fixed, 1 escalated, as on Claude, triage $0, the
  run $0.042 against $0.049, 2.4× slower; llama3.1 and qwen3's thinking variant failed
  ([Local triage](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#local-triage-2026-10-12),
  `samples/sample-output-short-local-triage/`)
- 2026-10-12 — **CL3 Messages and logging**: every exception, log line, tool error and user text is a message enum
  member with a `[Description]` template (486: 313 in core, the rest in each host's own `Messages/`), proved to render
  as before; `ToolMessageSnapshot.json` pins the 190 texts tools return to the model. Logging is `ILogger` with
  Serilog: stderr only, the run file and the JSON-lines records as sinks, holes kept as fields; `AgentLog` is gone.
  The orchestrator logs to stderr too, and `decisions.jsonl` is snake_case like its other records
- 2026-10-12 — **CL2 Split AgentEngine**: 1,078 lines become the engine (the turn loop, 360) and `ToolRegistry`,
  `ToolCallGuards`, `SkillTools`, `AgentBuiltTools` in `Agent/`; pure checks in `Guardrails/` (`WorkspaceBoundary`,
  `SensitiveFiles`, `SsrfGuard`); the console chat in the CLI. A golden file pins what the model is sent; it found that
  the prompt and two descriptions took the checkout's line endings, now always LF
- 2026-10-11 — **CL1 Project layout**: core in folders by concern (`Agent`, `Llm/{Name}Provider`, `Tools/{Toolset}`,
  `Skills`, `Usage`, `Pricing`, `Protocols/Mcp`, `Hosting`, `Config`, `Messages`, `Logging`), the namespace the folder
  path, one type per file; PascalCase namespaces in every project; the tests mirror the source; `ArchitectureTests`
  fails on a namespace off its folder, two types in a file or a dependency against the layers. Behaviour unchanged:
  the same 779 tests pass. Moved with `git mv`, so history follows
- 2026-10-11 — **F4 Prompt caching**: two cache markers on Anthropic (the system prompt with `LLM_CACHE_TTL`, the
  conversation by automatic caching, 5 minutes), as a provider strategy; tools sorted; writes priced at their lifetime
  (`cache_write_1h`); `usage-probe --cache-gaps` recommends the lifetime from the ledger. Full demo $0.13 against M1's
  $0.66 (−80%), louis-agent's input cost −87%, 10/10 fixes; samples in `samples/sample-output-cached/`
- 2026-10-11 — **F14 Display currency**: costs in the PC's currency in every host (rands from `HOST_CULTURE=en-ZA`,
  written by `usage-probe --host-culture`; `R0.034`, `.` decimals), at a dated rate (Frankfurter mapped into our
  `rates.json`, `config/rates.json` as the fallback, served at `GET /rates`), with the rate and the USD charged beside
  converted totals; a currency picker in the web app. The ledger stays in USD. Both demo samples rerun in rands

- 2026-10-11 — [FX8](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX08-fix-result-quotes.md): a FIX-RESULT whose summary quotes without escaping
  ("Cher") is still read, instead of asking louis-agent to continue three times; found by F14's full demo rerun

- 2026-10-11 — **F3 Show usage** (M1 done): every answer says what it used and cost — a line on the CLI's stderr,
  under each answer in the web app (with the chat's total), in Rider with `USAGE_IN_CHAT=true` — `/usage` in every
  host, `usage` on the API's `message_delta` and `GET /sessions/{id}`, and cost per reply, per error and per run in the
  orchestrator's comms log. The short demo's comms log total equals its ledger's exactly ($0.2526, 57 requests)
- 2026-10-11 — [FX7](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX07-timeout-ends-run.md): a timed-out request fails that error, not the
  orchestrator's whole run; found with the new `run-demo.ps1 -OrchestratorProfile`

- 2026-10-11 — [FX6](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX06-usage-on-failed-turns.md): a failed louis-agent turn still reports its usage (on
  the API's `error` event), so the web app's chat total and the orchestrator's comms log match the ledger; built in F3

- 2026-10-11 — [FX5](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX05-null-conditional-ledger.md): a `?.` in F3's first step skipped the ledger write for
  requests outside a usage scope; F1's tests caught it before the commit

- 2026-10-10 — **F2 Prices and cost**: every ledger record priced from a tracked `config/prices.json` (Haiku 5.5's
  long-prompt tier included), or from the API's `GET /prices` with `PRICES_URL`; `usage-probe -- --ledger` totals a
  ledger; M1 baseline $0.6586 for the full demo ($0.0625 a fix), both sample runs rerun with priced ledgers
- 2026-10-10 — Full demo on Claude Haiku 5.5 with the usage ledger: 10/10 fixes confirmed, 2 ignored, 1 escalated, ≈ $0.66
  (`samples/sample-output/`, now with `usage-2026-10.jsonl`); F2 prices that ledger for the M1 baseline
- 2026-10-10 — [FX1](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX01-round-limit-tools.md): the engine keeps the tools on the request after the
  10-round limit (`tool_choice: none`), which Haiku 5.5 otherwise rejects; the short demo went from 2/4 to 4/4 fixes
- 2026-10-10 — **F1 Usage ledger**: every model request of every host in `{LOG_DIRECTORY}/usage-YYYY-MM.jsonl`, with
  host, session, turn, purpose and the orchestrator's fix and run; `tools/louis-agent.usage-probe`; a short demo
  (`run-demo.ps1 -Short`) saved with its ledger in `samples/sample-output-short/` (4/4 fixes, ≈ $0.27 on Haiku 5.5)
- 2026-10-10 — Default Anthropic model is Claude Haiku 5.5 (about 7.5× cheaper than Haiku 4.5; usage, thinking and
  thinking text checked with `tools/louis-agent.usage-probe`); LLM profiles `config/.env.anthropic` / `.env.ollama`
- 2026-10-07 — Specs and feature plan for usage, budgets and cheaper responses (`docs/specs/`, `docs/features/`); the
  session-architecture and orchestrator designs moved from this list into `docs/specs/`
- 2026-10-07 — Docs: [how the agents and endpoints talk](src/docs/AGENT_COMMUNICATION.md), [runbooks](demos/louis-agent/docs/projects/orchestrator-runbooks.md), a
  docs index in reading order
- 2026-10-07 — Web app Branches tab: merges and deletes are queued instead of silently dropped while another runs
- 2026-10-07 — Comms log keeps louis-agent's text blocks apart
- 2026-10-06 — Orchestrator demo in Docker: ten bugs → ten fix branches, Markdown comms log, sample output with a
  recording of the merges
- 2026-10-06 — Web app Branches tab and history endpoints: all commits, branch and commit diffs, merge, delete merged
- 2026-10-06 — Root `.dockerignore`: builds no longer send `config/.env.secrets` or `.git` into the build context
- 2026-10-06 — Orchestrator POC: runbook per API method, fixes delegated to louis-agent.api and verified; `AgentEngine`
  `toolsets:` option
- 2026-10-06 — MCP server: Streamable HTTP transport (`MCP_TRANSPORT=http`) with its own `MCP_API_KEY`
- Earlier — Skill files standardised (titles, tool tables, Python fences); DevOps blockers query fixed (tag, not type);
  sensitive-file checks added to `DeleteDirectory` and `CopyFile`; all 108 tool descriptions rewritten
