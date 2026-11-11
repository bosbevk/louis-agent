# Draft: Puls's track, between the phases

> **Plan draft, not yet approved.** Moved whole out of the old platform plan on 2026-11-07 (CL56), lines 3529 to 3739 of `platform.md` at the tag `pre-roadmap`; the body below is as it was. When the work starts, this is rewritten into the plan Louis approves (the development guide, rule 3) and its TODO row names it. The sub-feature split of 2026-10-28 is the TODO's rows.

### Puls's track, between the phases

Puls sells itself (Louis: "puls can sell itself, but the agent should also"). It is a local guide on cloud models, in
Louis's words "puls the product is a local AI when it knows your [location] it [becomes] a know it all in that place":
"local" means local knowledge, never a model on the phone (Part 1). Every source that knows the place answers through a
few tools (places, shops, recipes, later bookings), and the sites are rows of data, never one tool per site, so a
session's tool list stays small and fixed (ADR 0006, FX4; Part 2, question 11). Its items take their turns between the
phases, one feature at a time as GOAL.md asks (`docs/GOAL.md:61`), never alongside one, and the TODO (Part 5) says which
comes next. They come in this order, each item its own feature or fix with its own plan when it starts (`docs/CLAUDE.md`
rule 3). On 2026-10-28 F35 and the specialists became phases of their own (3c and 3e), right after F18; the rest of the
track keeps its turns between the phases. GOAL skills: 3, 4, 7, 8, 13, 19. Paid runs: per feature (CL16's cook run, F35's recipe simulation, and each
later simulation), each on Louis's go. Selling: listings and credits are sold only with Phase 9's accounts.

![Puls's track: each item waits for what it needs, and a local lookup becomes a pack only when a simulation shows demand](diagrams/puls-track-puls-s-track-between-the-phases.svg)

A dashed arrow means "comes when measured". Puls's open items, F28 and more shops have no prerequisite beyond what
they name.

**1. Puls local spec** (`docs/specs/puls-local.md`, new; `docs/specs/` holds four specs today, checked): the place
registry, the business registry with its two tiers, the municipality theme, and POPIA for location. Docs only, and
first, because places, businesses and the theme all build on it (the thread put it before F35). The spec also says how
the landing page's pitch is rewritten around place: today it sells reading files, the repository's history, search, cost
and counting, and never mentions place (`src/louis-agent.puls/Pages/LandingPage.razor:275-292`; *Where this plan
challenges you*, row 10). The page itself changes in its own small step after the spec.

**2. F35 Recipe sources**, in full (`packs/tools/louis-agent.pack.recipes`, namespace `LouisAgent.Packs.Recipes`, green: small
sites read in our process). After Phase 3, whose record and replay its simulation runs on.

- **Why.** Today the cook never opens a recipe page: its role allows `WebSearch` for a recipe and `FetchUrl` only for
  OpenStreetMap (`louis-agent.core/Prompts/specialists/puls-cook.md:65`), so its recipe is search snippets plus the
  model's memory, unsourced and unchecked.
- **Free only.** Louis: "it has to be free". No paid recipe API: pages are found with SearXNG (F32) and read through our
  own Chrome, with no key and no quota.
- **Research** (`research/sa-recipes-api.md`, checked 2026-10-18): no hosted recipe API covers South African dishes.
  TheMealDB returns 0 South African meals and its free key is for development only; Spoonacular and Edamam are paid,
  forbid keeping results (Spoonacular for longer than an hour) and are mostly US-written. The SA recipes are on the SA
  sites (Food24, Woolworths TASTE, Drizzle and Dip, and recipesfromsouthafrica.com with its Afrikaans *resepte*), and
  none has a public API, so F35 reads each page's own schema.org `Recipe` block, with `WebSearch` kept as the fallback.
- **Shaped like shops.** Louis: "yes I want recipe providers like shops providers, so it has all the details". Two
  tools: `FindRecipes(source, query)` searches only the sites turned on, and `ReadRecipe(url)` reads any public page's
  `Recipe` block through the guarded browser and returns it as a short table. Behind them, an `IRecipeSource` per site,
  chosen by `RecipeSourceKind` (an enum) and turned on by `RECIPE_SOURCES` (like `SHOP_CATALOGUES`; a site is turned on
  only after its terms are read, ADR 0023). A `Recipe` record carries every detail: name, serves, prep, cook and total
  times, every ingredient line, steps by section, photo, author and link.
- **The draft.** `plans/F35-recipe-sources.md` (the Claude project's draft, not approved) was written for the old CL15
  shape, in `louis-agent.puls.core`. Before its approval it is rebased onto the recipes pack: its record and replay
  (step 4: `WEB_CASSETTE`, `RecordingWebBrowser`, `ReplayWebBrowser`) was built in Phase 3 instead (steps 4a and 4b:
  `WEB_REPLAY`, `RecordedWebBrowser`, `WebReplayStore`), and its screenshot and three-way benchmark (steps 6 and 7) go
  to *Vision*. Its first sources (question 14, answered 2026-10-19): Drizzle and Dip, Food24, The Lazy Makoti and recipesfromsouthafrica.com; Drizzle and Dip
  first, because its pages carry the block (WP Recipe Maker; the draft's reading, not re-checked). F35's first step
  confirms each site's `Recipe` block and reads its terms; one without the block is read through `WebSearch` or dropped,
  and more are added later if needed.
- **Proved by** a recipes simulation: the same 10 SA dishes three ways (today's search snippets, `FetchUrl` of the page,
  `ReadRecipe` of its block), the cost per answer from the ledger and the share of answers that match their source, on
  pages recorded with Phase 3's record and replay, so a difference between runs comes only from our change. The thread
  guessed 5,000 to 15,000 tokens for a whole page against about 500 for the block (Claude's estimate there, not
  measured); the simulation is what proves it.

| # | Part | Done when |
|---|---|---|
| 0 | F35's feature doc, the rebased plan, the TODO line and indexes; the free check: 10 SA recipe pages from question 14's sites, through the guarded browser, for a `Recipe` block | docs only; the check's table in the feature doc |
| 1 | **The JSON-LD reader, in core** (Part 3, row 30): `application/ld+json` read from the raw HTML before `HtmlToText` strips every script (`louis-agent.core/Tools/Web/WebTools.cs:413-417`, the script pattern at `:463-464`), measured on the 10 pages and on about 50 Swartland business sites from OpenStreetMap's `website` tags | recorded pages, trimmed: one block, a block in `@graph`, two blocks, broken JSON, none; the measured share in the feature doc |
| 2 | `Recipe`, `RecipeStep` and the schema.org reading, in the pack (Schema.NET, MIT, tried first; Python's recipe-scrapers considered, but it is Python) | `@graph`, `HowToSection`, `PT1H15M` and the `recipeYield` forms read right; a page without a block gives none |
| 3 | `IRecipeSource`, `RecipeSourceKind`, the factory and the first provider | the site's search page, recorded, gives hits; `RECIPE_SOURCES` parsed with `SettingChoice`; a site that doesn't answer gives its reason |
| 4 | `RecipeTools` (`FindRecipes`, `ReadRecipe`), the `recipes` pack (`PackKind.Recipes`; `PulsIdentity` names `recipes`), its guide and its message texts | the table framed as untrusted, as the shop tools are (`ToolMessage.cs:285-286`) until Phase 7's frame takes both; a page with no block says so; an unknown source lists the ones turned on; the toolset unavailable without `BROWSER_URL`; the convertibility test green; the pack's golden reviewed |
| 5 | The cook reads its recipe: its header gains `recipes?` (`toolsets: shops?, recipes?`, in Puls's identity project after Phase 2), and its role finds and reads a recipe, links it and scales it to *Serves* | the role's tests on the prompt; Puls's golden reviewed |
| 6 | **The recipes simulation**, on recorded pages | the three ways' cost per answer and match share in the feature doc; **paid, on Louis's go** |
| 7 | Docs and the write-up, by Louis himself without the code open: `docs/learning/07-context-engineering.md` gains "data read in code, not pages" for recipes, after F32 to F34's prices | Louis's write-up written; F35 closed as *Finishing a feature* says |

**3. CL16 Reusable roles**, after F35. A pack carries roles: the cook moves into `packs/tools/louis-agent.pack.cooking`, which
requires `recipes` (from F35) and uses `shops` where the instance has it. The identity's frame is loaded before a pack's
role, a mechanism of CL16's own: the cook's file mixes the craft with Puls's protocol today (*How every Puls specialist
works* at `puls-cook.md:69`, *Your report to Puls* at `:85`, Afrikaans lines at `:38-40`), so the protocol stays with
Puls's identity and the craft goes to the pack (Part 3, row 34). Proved by the cook simulation under Puls (paid, on
Louis's go), and its reuse by a free golden: a test identity in the cooking pack's tests names `cooking`, loads the cook
under its own frame, and its request is pinned, so the craft is shown without Puls's protocol (the engine research
measured CL16 under Puls and under a second identity; Louis: "we should be able to reuse those items").

**4. Sources as data** (ADR 0032 *Sites are data*, an explicit exception to ADR 0024: a site directory's keys are rows,
not enum members). `ShopKind` becomes the platform: `Checkers`, `Shopify` (`/products.json`), `WooCommerce` (its Store
API, `/wp-json/wc/store/v1/products`), `SchemaOrg` and `PriceList`; today its one member is `Checkers`
(`louis-agent.core/Tools/Shops/ShopKind.cs:7-10`). Shops and recipe sites become rows: name, town, URL, platform, a
terms flag that defaults to off, and `as_of`. Checkers becomes the first descriptor-driven provider: a descriptor is a
data file holding a search URL template, which response to capture, a field map and a ready selector. A visit already
takes a URL, a rule for which responses to capture and a ready selector (`IWebBrowser.VisitAsync`,
`louis-agent.core/Tools/Web/Browsing/IWebBrowser.cs:13-16`), so the next shop with a JSON catalogue is a reviewed data
file, not a feature (the challenge's design). A site registry and its indexing are shared by shops and recipes: the
collecting job reads `robots.txt` first (obeys it and its `Crawl-delay`, names itself with a user agent), then the
sitemap (filtered by path, `lastmod` saying what changed), then the pages through the JSON-LD reader (Part 2,
*Collecting is a job*; Part 3, row 31). Once the registry exists, it also picks the pages later simulations record;
F35's own simulation records the pages its trials read. The registry is seeded from open place data we may store, the
second of Puls's sources (Part 2, *Inside a pack*): OpenStreetMap's shops and businesses in a town with their `website`
tags (the same list Part 3, row 30 measures the JSON-LD reader on), or Overture's places. Each row keeps its source and
`as_of`, and OpenStreetMap's licence (ODbL: attribution, and share-alike for a database made from it; not re-checked
here) goes with the rows. Overture's South African coverage is unmeasured (the challenge's). `SourcedFact` (source, URL,
`as_of`, terms) comes here, when a second source needs it (Part 3, row 32). Why, in numbers: Checkers took 45 commits,
16 of them fixes, and covers 1 of the 4 supermarket chains in Malmesbury (Part 2), so a provider per platform replaces a
provider per site, and the Shoprite, Pick n Pay and Woolworths providers (`TODO.md:259-261`) wait for this. Shop and
recipe rows follow Phase 6's recovery contract (B5a): a row older than its policy's `MaxAge` by `as_of` is stale,
conflicting prices for one product are settled by a fixed precedence rule, a malformed row is quarantined and never
coerced into a price, and the cook reports its coverage, as F34 already leaves an item unpriced rather than guess.

**5. Places** (`packs/tools/louis-agent.pack.places`), after the area-guide simulation proves it (Phase 4). Louis: "what would
be [places] here?", and of his town: "we can even load the municipality css when [a] person changed or drives. Like for
instance Malmesbury is swartland municipality give it that municipality colours".

- **The area guide moves into Puls.** Louis: "where puls will take your location and become a tour guide, a know it all
  for an area". When the places pack lands, the guide that Phase 4's `demo-area-guide` proved moves into Puls's identity
  with it, and its simulation's scenarios are rerun on Puls (one paid run, on Louis's go). The demo's pitch has then
  done its job.
- **The place specialist.** A local question goes to a specialist opened for the town, with only the place and shop
  tools its sources need and the town's source rows in its brief (Part 2, *Inside a pack*; question 11).
- **Sources and tools.** OpenStreetMap's Nominatim reverse lookup, Wikipedia's nearby search in English and Afrikaans,
  and the municipality's own site; tools such as `FindPlaces` and `Municipality`; every fact with its source.
- **Municipalities as data.** The 257 municipalities are rows (code, name, colours; the thread's count, not re-checked
  here), and Phase 4's town-to-municipality table (the one its `MunicipalityRight` grader reads) holds their first rows.
  A step first confirms that Nominatim's reverse lookup returns the municipality for SA towns; Puls's web app already
  asks Nominatim at `zoom=10`, with the position rounded to 2 decimals, about 1 km, and falls back to
  `address.municipality` for the town (`src/louis-agent.puls/wwwroot/js/chat.js:213-218`).
- **The theme.** Puls's web app applies the municipality's colours as its theme: Puls has one theme today, its colours
  as CSS variables (`src/louis-agent.puls/wwwroot/css/settings/tokens.css:1-4,8`). Colours only: a crest or a logo is official
  insignia and needs the municipality's permission. While the user drives, the theme changes when they stop or open the
  app, never mid-chat.
- **Location under POPIA.** Location is personal information: the user is asked once (as today: the browser asks
  once, and the town is kept in that browser only, `src/louis-agent.puls/wwwroot/js/chat.js:195-198,205`), only the
  town is used, as today (F22), never a track of movements, and no movements are stored. Coarse coordinates (about 1
  km) only when a measured question needs "how far", recorded in an ADR when it comes.
- **Places never changes the identity.** Louis asked "to find pulse identity?": no. Puls is the same identity in every
  town. The town is an input to the places tools; it changes the facts and the web app's look, never who Puls is or a
  session's tools (a chat's tool list is fixed when it opens, ADR 0006 and 0008).
- **The geocoder.** Places takes the town geocoder from the shops pack (where Phase 2 moved it) behind an `IGeocoder`
  seam in core, a shared service through `ToolsetContext`, so shops keeps naming places by name only (Part 3, row 33).

The picture Louis confirmed ("this is correct"):

![The places pack reads the town's own sources, and Puls's answer and the web app's colours follow the town](diagrams/puls-track-puls-s-track-between-the-phases-2.svg)

**6. Local businesses.** Louis: "A shop can then [advertise] or become a provider like Shops/hairdressers". Two tiers in
the business registry. **Listed:** name, category (shop, hairdresser and so on), municipality, hours and a link, shown
beside an answer and always marked as an ad, never inside it; a Puls revenue line, sold from Phase 9's accounts.
**Provider:** their prices or open times reach a pack through their platform, a price list or their MCP server (an
orange pack, Phase 8), with a future bookings pack for hairdressers; categories map to packs. One rule, from the
principles (`louis-agent.core/Prompts/shared/principles.md`: never cheat): paying never changes what Puls calls cheapest
or nearest. Businesses joining is the best of Puls's sources (Part 2's ranking).

**7. Vision** (GOAL skill 19, `docs/GOAL.md:40`; its own feature after F35, question 15, answered 2026-10-19). Louis: "okay so when
I as a person look at the computer, I can see and take a pic with my eyes, how can we do it". `ScreenshotPage(url)`
asks our Chrome for the page's screenshot (the DevTools protocol's `Page.captureScreenshot`), cut into pieces about one
screen high, and returns them as image `DataContent`. It is the fallback when a page has no recipe block; later it reads
a shop's specials leaflet that exists only as an image, or a photo the user sends. Its parts, from the F35 draft's steps
6 and 7: first a check that our Anthropic client carries an image in a tool result; then the tool, its screenshots
recorded by Phase 3's record and replay (which records "screenshots once the vision feature adds them"); then a
three-way simulation on recorded pages (page text, recipe block, picture: tokens, cost and correct ingredient lines per
way), paid, on Louis's go; then the write-up, `docs/learning/19-multimodal-and-computer-use.md`. The thread estimated
about 1,500 tokens a screen ("width × height ÷ 750", Anthropic's rule of thumb as Claude quoted it) and 0.5 to 2 MB a
screenshot; neither is measured.

**8. Local lookups.** What training can't know (`TODO.md:8-9,138-139`): sports results, SARS dates and load-shedding
schedules, each a pack when a simulation shows people want it; and the South African context Puls assumes ("the game"
means the Springboks or Bafana, `TODO.md:136-137`), measured on Puls's simulations. Municipal services go to *Places*,
and rand prices are the shops pack's.

**9. F23 Credits** (`TODO.md:237-239`): credits bought in rand, spent per question at cost plus a margin, from the
ledger and prices; Louis: "'n layer wat geld na ons interne krediete verander. Wil so bietjie geld maak as ek kan." It
takes in rand pricing and billing (`TODO.md:147`), and comes with Phase 9's accounts.

**10. Puls's open items:** the cook's *geskat* or *unknown* (decide, then measure; `TODO.md:231`); a shorter cook report
(half the model time; `TODO.md:218`); binary attachments sent as files, not text (F22's open item, a fix;
`TODO.md:196-198`); a continued specialist's ledger rows named by the turn that continued it (F26's open item, a fix;
`TODO.md:236`); Louis's corrections to the Afrikaans (F21's, `TODO.md:199`). F21, F22, F25, F26 and F29 themselves are
closed in Phase 0 step 3.

**11. F28 Team talk** (step 1 built: commit `89c46ba`, Puls briefs its specialist in a fixed note, mood included; the
mood rule and the `[stemming: af]` marker in Puls's own routing, `prompts/agent/puls/voice.md`, since F127.3.1 deleted
the specialists guide). The mood read
is live with no off switch; question 4 was answered *switch* (2026-10-19): add it, off by default. The rest of its plan
stays here (`docs/plans/F28-team-talk.md:57-59`): `read_mood` on the API and the chat's *Lees my stemming* toggle (off
by default, with an instance setting), the delegation card as a thread and the Werk panel's chip, and the briefing and the mood graded by
Puls's simulations (Phase 3 step 5), which score `briefed` and `mood`.

**12. More shops** only with a retailer's permission or feed (F33's follow-up; ADR 0023), after *Sources as data*.

**13. Later for Puls:**

- **A Puls-owned set of a few hundred SA recipes** (skills 8 and 12): searched before the web, so the cook's answers are
  repeatable and an eval can say whether they were right; F45's first retrieval case. The content is our own writing,
  open sources (Wikibooks' SA dishes are CC BY-SA: credited and shared alike) or a content deal with an SA publisher
  (Food24, a cookbook author); copying blog text is not an option. Mealie (AGPL-3.0), run beside us as a separate
  service as ADR 0023 allows, is the candidate store; its licence is decided in an ADR before it is used.
- **WhatsApp as a host** (`TODO.md:143`), **the eleven official languages measured** (`TODO.md:140`), and **voice**
  (`TODO.md:142`).

