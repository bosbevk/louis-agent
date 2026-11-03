# F35 Recipe sources: the cook reads a recipe from the page's own data

> **What this answers:** how the cook gets a real South African recipe, whole and exact, from the site that
> published it, in code, so that the amounts and steps are the page's and not the model's memory, the answer can link
> its source, and an eval can replay the same pages every run. This is **F35.1**, the first steps of F35, 0 to 5 (the
> JSON-LD reader, the robots policy, the `Recipe` record, the sources and `RecipeTools`); F35.2 (the cook uses them, the recipes eval,
> the write-up) is listed at the end and planned when it starts. Branch `feature/F35.1-recipe-sources`, one PR per
> step to that branch, the branch to `main` at the end. ADRs **0067** and **0068** (0065 is F53.1's and 0066 the website's, claimed in open PRs on 2026-11-03; steps 0 and 2
> take the next free numbers on `main` the day they are written, and nothing else changes).

Status: **draft for Louis, 2026-11-03.** Six findings below, each on its own card. Picked 2026-11-03: A1, B1, **C2** (the
guarded browser, not the HTTP fetcher; the plan below is rewritten for it), D2, F1, **E3** (a robots policy on every fetch, in core: a new step 2 and ADR 0068, written in below) and **G1** (an unreachable `robots.txt` refuses the page for an hour). **Approved on every finding, 2026-11-03 23:32.**

## The goal, in Louis's words

"It has to be free" (2026-10-18, on recipe APIs), "recipe providers like shops providers, so it has all the details",
and the pages kept, "because benchmarks need them". The platform plan settled the shape: a `recipes` pack with
`FindRecipes(source, query)` over the sites turned on and `ReadRecipe(url)` over any public page's schema.org
`Recipe` block, the JSON-LD reader as infrastructure in core, each site turned on only after its terms are read
([ADR 0023](https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0023-copyleft-beside-us-permissive-linked.md)).
F48 (the cook as a home cook and teacher) builds on F35's `Recipe` record: its ingredient-line reader, scaler and
shopping list work on that record, so what this plan gives the record is what F48 gets.

GOAL skills **4** (tool design: a whole recipe in a small result), **5** (structured outputs: a page's own data over
its text), **7** (context: fewer tokens in), **12** (evals on replayed pages) and **15** (outside data framed as data;
what we read, and may: `robots.txt` obeyed in code).

## Context: what we have today, checked in the code on 2026-11-03

- **The cook reads no recipe as data.** `puls-cook.md` searches the web, `FetchUrl`s a page and reads the recipe from
  the page's text. The amounts are read by the model from prose, the photo is picked from `![alt](url)` lines by
  rules in the prompt, and the prompt carries a list of nine sites that refuse automated readers (`food24.com` among
  them). The eval's `recipe-image` check (`Evals/Cook/eval.json`) grades the photo; nothing grades the recipe against
  its source, because there is no source as data.
- **Core already touches JSON-LD, with a regular expression.** `LeadImage` (FX24, `Tools/Web/Fetching/`) finds the
  page's photo in its JSON-LD with a `[GeneratedRegex]` over `<script type="application/ld+json">`, parses each block
  with `System.Text.Json` and walks `@graph`. ADR 0057 says a regex goes as the code is touched, and step 1 touches
  it.
- **The page fetcher is reusable.** `PageFetcher` (core) is the guarded HTTP client the web tools fetch with: the
  SSRF guard on every hop, an honest user agent (`… louis-agent/1.0`, ADR 0038), a 5 MB cap, 20 s timeout, and
  **record and replay built in** (`RecordedFetchHandler` over the session's `ReplayStore`, F18 step 4b). A pack gets
  the store from `ToolsetContext.Replay`. So the old draft's step 4 (a cassette of our own) is already built, and
  this plan has no such step.
- **The guarded browser** (`IWebBrowser`, Chrome) is there too, with its own record and replay (`RecordedWebBrowser`),
  for a site that renders with script. The three sites below don't (checked: their search results are in the HTML).
- **A pack is a known shape.** `packs/louis-agent.pack.shops` is the template: `ShopsPack : IAgentPack`, one
  `ToolsetDefinition`, `ShopsOptions.FromEnvironment()`, `IShopCatalogue` per provider, `ShopCatalogueFactory.Enabled`
  parsing the setting into an enum, `ShopToolMessage` for every text, the guide in `Prompts/toolsets/shops.md`, and
  `PackKind` in the composition root, whose summary already says "F35 `Recipes`". `ArchitectureTests` already names
  `louis-agent.pack.recipes` in its fixture. [Build a pack](https://github.com/bosbevk/louis-agent/blob/main/docs/guides/build-a-pack.md) is the how-to.
- **No HTML parser in core.** Pages are read with hand-written readers and generated regexes (`HtmlText`); there is no
  AngleSharp or HtmlAgilityPack. Finding A decides whether that changes.

### Facts checked for this plan

| Site | Checked 2026-11-03 | What it means |
|---|---|---|
| **drizzleanddip.com** | `robots.txt`: everything allowed for all readers but MJ12bot, no `Crawl-delay`, four sitemaps. WordPress with WooCommerce. The search page `/?s=bobotie` is server-rendered and gave 8 results (a classic bobotie, bobotie phyllo pies, a Cape tomato bredie, the *South African recipes* collection). Its recipe cards show *Servings*, *Prep time* and a *Print Recipe* link, the shape WP Recipe Maker gives | The first source: SA classics, a working search, nothing in its robots against us |
| **thelazymakoti.com** | `robots.txt`: everything allowed, no `Crawl-delay`, one sitemap. WordPress (Site Kit). Recipes with ingredient lists (chakalaka, umngqhosho, soft rolls, tomato gravy). `/?s=chakalaka` answered with the chakalaka post itself, not a results list (a WordPress theme that jumps to a single match) | The second source, SA home food. Its search reader must take a results page **or** a single post |
| **recipesfromsouthafrica.com** | `robots.txt`: everything allowed. WordPress. The posts seen (`/soetkoekies/`, `/soet-cookies/`) are stories with no ingredient list, and `/?s=melktert` found one category page, not a recipe | Thin. Enabled only if step 0's check finds `Recipe` data on its pages; otherwise left out, as the cook's prompt already leaves it |
| **food24.com** | Refuses automated readers (`robots_disallowed` on `/robots.txt` and `/`; the cook's prompt lists it too) | **Out.** A site that says no is not read (ADR 0038); a licence would be a business deal (`research/sa-recipes-api.md`) |
| The JSON-LD `Recipe` block itself | **Checked 2026-11-04 from the PC** with `curl` and the brief's user agent (the raw HTML, scripts included): three recipe pages per site and one search page; the trimmed copies are the fixtures. **drizzleanddip.com**, 3 of 3 recipe pages: a `Recipe` block in its own `application/ld+json` script, not inside the page's `@graph` block (which holds Article, BreadcrumbList, Person, WebPage and WebSite). The plugin is WP Recipe Maker (`wprm-recipe-container` on the page). `recipeYield` is a list with one string (`["6"]`, `["8"]`) on two pages and absent on the third. `recipeInstructions` is a list of `HowToStep` objects only (5, 10 and 11 steps; no `HowToSection`). `image` is a list of strings (1, 1 and 4 URLs). `prepTime` and `cookTime` are ISO durations (`PT10M`, `PT45M`); the milk tart has no `cookTime`; no page has `totalTime`. The search page `/?s=bobotie` lists 12 entries, each an `<article>` with its heading link. **thelazymakoti.com**, 0 of 3 recipe pages: no `application/ld+json` script and no recipe-plugin markup; the ingredients are a plain list (`<ul><li>`) under an *Ingredients:* heading. **recipesfromsouthafrica.com**, 0 of 3 recipe pages and its search page: no `application/ld+json` script, only microdata breadcrumbs. **Then the wider check of 74 sites (2026-11-04, *Approved during the build*) found drizzleanddip's pages carry `<meta name="robots" content="noai, noimageai">`**, which the first trim dropped. The recipe fixtures are now five pages of sites that allow us, one per shape: a `Recipe` alone from the site itself (`foodandhome-pork-bobotie`, `bibbyskitchenat36-bobotie-filo-parcels`), from Penci Recipe in Afrikaans (`rooirose-hoenderpasteitjies`), and WP Recipe Maker's inside a Yoast `@graph` (`wandercapetown-malva-pudding`) and a Rank Math `@graph` (`tantalisemytastebuds-pickled-fish`); the search fixtures are Food & Home's and Tantalise's `/?s=bobotie`. | **drizzleanddip.com is out: it refuses AI readers.** thelazymakoti.com and recipesfromsouthafrica.com allow us but publish no `Recipe` data: they are text sources under Louis's pick of 2026-11-04 (*Approved during the build*). The fixtures show `recipeYield` as a string (`"4 to 6"`), a list (`["6"]`) and empty, `HowToSection`s (Tantalise, Wander Cape Town) and `image` as a string and as an object; step 3's tests build the one shape none shows, `recipeYield` as a number, inline. |
| schema.org `Recipe` | `name`, `image`, `author`, `datePublished`, `description`, `prepTime`, `cookTime`, `totalTime` (ISO 8601 durations, `PT1H15M`), `recipeYield` (a string, a number or a list), `recipeCategory`, `recipeCuisine`, `keywords`, `recipeIngredient` (a list of lines), `recipeInstructions` (text, a list of `HowToStep`, or `HowToSection`s of steps), `nutrition` | The record's fields and the reader's cases, from Google's *Recipe structured data* page and schema.org |

## The findings: seven choices, one card each

Each is put to Louis on its own (rules 10 and 11), with three options, the pattern each one is, and the
recommendation. The plan is written with the recommended option.

### A. Where does the JSON-LD reader live, and how does it find the blocks?

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| **A1 (recommended)** | **One reader in core**, `Tools/Web/StructuredData/JsonLdBlocks`: a hand-written walk of the HTML (`IndexOf` of `<script`, its `type` attribute, the closing tag), each block decoded and parsed with `System.Text.Json`; `LeadImage` is moved onto it and its regex goes | Infrastructure the model never calls, shared (the platform plan's own placing); a small reader before a regex (ADR 0057); one reader for every caller (DRY) | Core gains one small class; `LeadImage` loses its regex; the pack gets the blocks for free | About 60 lines in core, with tests for the odd cases (an escaped `</script>` inside a string, HTML entities, a malformed block) |
| A2 | **AngleSharp** (MIT) in core, the reader a query over its DOM | Adopt a library (ADR 0030) | A real HTML parser for this and every later reader (`HtmlText`'s regexes could go too) | A 1 MB dependency and a new way of reading pages beside the existing one; nothing else needs it yet (ADR 0055) |
| A3 | The reader **inside the recipes pack** only; `LeadImage` untouched | Keep it local | Core doesn't change in F35 | Two readers of the same thing in two projects; the next pack (places, `LocalBusiness` blocks) copies it again |

Sources: ADR 0057 (a reader before a regex); the platform plan's *Inside a pack: four kinds of thing* (the JSON-LD
reader is infrastructure, in core); JSON-LD 1.1 §*Embedding JSON-LD in HTML* (w3.org/TR/json-ld11); ADR 0030, 0055.

### B. What is a recipe in code?

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| **B1 (recommended)** | **Our own `Recipe` record**, read from the JSON-LD by `SchemaOrgRecipe.Read(JsonElement, Uri)`: name, yield, the three times, the ingredient lines as they are, the steps (with their section), the photo, the author, the source link | Data Mapper (Fowler, PoEAA): one class that knows the page's shape and builds our record; the record a plain value (ADR 0040) | F48 gets exactly the fields it named (name, serves, lines, steps, link); the reader holds every odd case the sites show (`recipeYield` as `"6"`, `6` or `["6", "6 servings"]`; steps as text, `HowToStep` or `HowToSection`) | We write the reader (about 120 lines, tests from the recorded pages) and extend it when a site shows a new shape |
| B2 | **Schema.NET** (MIT): deserialise the block into its `Recipe` type, map to our record | Adopt a library | Every schema.org type at once, maintained by others | Its values are `OneOrMany<T>` and `Values<T1, T2, …>` unions, so the mapping to a plain record is the same work as B1, on top of a 10 MB package; mixed `@graph` documents need our walk anyway |
| B3 | **No record**: `ReadRecipe` hands the model the block's JSON, trimmed | None (pass-through) | Least code | The model reads JSON (more tokens than a table), nothing in C# can check or scale it, and F48's maths has nothing to work on. It fails F48's premise |

Sources: Fowler, *Data Mapper* (PoEAA); ADR 0040 (records for values); Schema.NET's README (its `Values` unions);
the F48 plan's decision *no Cooklang: the maths works on F35's record*.

### C. How does the pack read a site?

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| C1 (recommended, not picked) | **Over HTTP with core's `PageFetcher`**: the site's own search page (`/?s=bobotie`) and the recipe page, read as any reader's browser would, no Chrome | Gateway (Fowler): one object wraps the outside system; the fetcher we have, with its guard and its record and replay | The toolset needs no `BROWSER_URL`; a recorded eval replays through the fetcher's store; one page is one request | A site that renders its search with script can't be read this way (none of the three does, checked) |
| **C2 (Louis's pick, 2026-11-03)** | **Through the guarded browser** (`IWebBrowser`, Chrome), as the shops pack does | The same gateway, heavier | Script-rendered sites work | Chrome for a page that doesn't need it: slower, `BROWSER_URL` required, the Puls stack's browser container on every eval |
| C3 | **WordPress's REST API** (`/wp-json/wp/v2/posts?search=`), JSON in, no HTML reading | API client | The cleanest search answer | The precedent is to read a site as its visitors do, not its APIs (ADR 0023's note on F33/F34); a site can switch the API off; and the recipe page still has to be read for its block |

Sources: Fowler, *Gateway* (PoEAA); ADR 0023 (how Checkers is read); ADR 0038 (the fetcher says what it is);
`PageFetcher`'s summary (the guard, the record and replay).

### D. One class per site, or one class for every WordPress site?

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| D1 | **One class per site** (`DrizzleAndDipSource`, `LazyMakotiSource`), each behind `IRecipeSource`, as shops has `CheckersCatalogue` | Strategy (GoF), one concrete strategy per site | Each site's quirks in its own file | Three files that differ in a URL and a name; the next WordPress site is a fourth copy |
| **D2 (recommended)** | **One `WordPressSource`** behind `IRecipeSource`, built with the site's row (`RecipeSourceKind`, home URL, display name) from a small table, `RecipeSites`; a site that isn't WordPress gets its own class when it comes | Strategy whose concrete class is parameterised by data (Refactoring.Guru: *Replace Subclass with Fields*); the table a Registry | Three sites are three rows; the interface stays for a different kind of site | The one class must take both search shapes (a results list, or a theme that jumps to the single post) |
| D3 | **No interface**: one static `RecipeSearch` with the table | Simplest | Fewest types | The first non-WordPress site (a site with its own search API) forces the interface back in, and tests can't fake a source |

Sources: GoF, *Strategy*; Refactoring.Guru, *Replace Subclass with Fields*; Fowler, *Registry* (PoEAA); ADR 0003
(provider strategies: one interface, one implementation per provider, picked in one factory).

### E. Who checks that a site may be read?

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| E1 (recommended, not picked) | **A person, once, at enable time**: a site is read only where `RECIPE_SOURCES` names it, and it is named only after its `robots.txt` and terms were read (the table above); the source's summary says what was checked, as `CheckersCatalogue` does | Configuration over code; the ADR 0023 precedent (`SHOP_CATALOGUES`) | No code; the check is written down where the next reader looks | A site that changes its `robots.txt` later isn't noticed until someone looks |
| E2 | **`robots.txt` checked in code** when the pack starts: a source whose home disallows `/` or `/?s=` is left unavailable | Guard at start-up | Catches a change | A reader of `robots.txt` (its groups, `Allow` over `Disallow`, wildcards) for three sites that allow everything; the platform plan puts that reader with the collecting job (*Sources as data*), where every page goes through it |
| **E3 (Louis's pick, 2026-11-03)** | **A robots policy on every fetch** in core's `PageFetcher`, and on every page the browser opens | Policy on the gateway | Every tool obeys it, `FetchUrl` too | A design of its own (caching per host, the user agent's group, what to tell the model); out of F35's size |

Sources: ADR 0023; ADR 0038; the platform plan's *Collecting is a job, not a tool*; RFC 9309 (robots.txt) for E2/E3.

### F. What does `ReadRecipe` hand the model?

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| **F1 (recommended)** | **Short markdown sections**, framed as untrusted data: the name, *Serves* and the times on one line, the ingredient lines numbered, the steps numbered under their sections, the photo, the author, the link; nothing the page didn't say | The shops pack's table precedent (ADR 0005: bounded, framed output) | The model reads about 400 tokens instead of a 3,000-token page; the cook can quote lines by number | Our own print of the record (one small printer class) |
| F2 | **JSON** of the record | Pass-through | No printer | More tokens than F1, and the model is worse at quoting from JSON than from a list |
| F3 | **Both, by a `format` argument** | Parameterised output | The caller chooses | Two shapes to keep in step and golden; nobody asked for JSON (ADR 0055) |

Sources: ADR 0005 (guard the context); the shops pack's `ShopToolMessage.UntrustedFrame` precedent; Anthropic's tool
writing guidance (small, readable results).

### G. An unreachable `robots.txt` (a 5xx answer or a network error): read the page, or not?

Came with E3. The file *missing* (404) is settled by the RFC: everything is allowed. *Unreachable* is the open case.

| | Option | Pattern | What it changes | What it costs |
|---|---|---|---|---|
| **G1 (recommended)** | **Refuse the page for an hour**, then try the file again; the model is told why | RFC 9309 §2.3.1.4: a crawler MAY assume complete disallow while the file is unreachable | Honest: when a site can't tell us its rules, we don't guess | A site whose `robots.txt` is down (rare; it's a static file) is unreadable for that hour |
| G2 | **Read the page** as if the file allowed it | Fail open | Nothing stops a read | We read where we couldn't check: the opposite of why E3 was picked |
| G3 | **Read, but log a warning** and keep trying the file | Fail open, with a trace | The dashboard shows it | Same as G2 for the site; one more thing to look at |

Sources: RFC 9309 §2.3.1 (unavailable: allowed; unreachable: may disallow); Google's robots.txt specification (the
same table).

## Design: what changes (with the recommended picks)

In plain words: a page that publishes a recipe also publishes it for machines, as a JSON block hidden in its HTML
(JSON-LD, the format Google's recipe cards read). We read that block in code, turn it into a `Recipe` record, and
hand the cook a short, exact print of it. The pack has two tools: `FindRecipes` searches the sites a deployment turned
on, `ReadRecipe` reads one page. Both read through the guarded browser core already has (Chrome behind `IWebBrowser`,
as the shops pack reads Checkers), which already records and replays, so an eval replays them with nothing new.

1. **Core gets one reader** (step 1): `JsonLdBlocks.In(html)` returns every `application/ld+json` block as a parsed
   `JsonDocument`. `LeadImage` uses it; its regex goes.
2. **Core gets a robots policy** (step 2, Louis's pick E3): `RobotsPolicy` reads a host's `robots.txt` once a day
   through the recorded fetcher, keeps the group for `louis-agent` (else `*`), and says whether a path may be read
   (RFC 9309: the longest matching rule wins, `Allow` on a tie, `*` and `$` wildcards). `PageFetcher` asks it before
   every hop (`FetchUrl`, `VerifyQuote`, the pack's reads all go through it), and a `RobotsWebBrowser` decorator asks
   it before the browser opens a page (the page's own loads are the browser's, as any visitor's). A disallowed page is
   refused with the reason, as a private address is. `robots.txt` itself is always fetched; a missing one (4xx)
   allows all; an unreachable one is finding G.
3. **The pack** `packs/louis-agent.pack.recipes` (steps 3 to 5): `Recipe` and `SchemaOrgRecipe` (step 3), the sources
   (step 4), the tools, the guide, the pack class and its registration (step 5).
4. **Settings:** `RECIPE_SOURCES=drizzleanddip,lazymakoti` (comma-separated, case ignored, unset means none, as
   `SHOP_CATALOGUES`), and `BROWSER_URL` for the browser, as shops needs it (the Puls stack sets it).
5. **What's not here:** the cook doesn't use the pack yet (F35.2 changes `puls-cook.md`, adds `recipes` to Puls's
   packs, writes the recipes eval and the `local-sources` check); F48 adds the cache of read recipes its
   `WriteRecipe(link)` needs; the collecting job, `Crawl-delay` and the sitemaps stay with *Sources as data*.

### Interface design

```csharp
// core, Tools/Web/StructuredData (step 1): the blocks a page publishes for machines
public static class JsonLdBlocks
{
    /// Every <script type="application/ld+json"> block of the page, decoded and parsed; a malformed block is skipped.
    public static IReadOnlyList<JsonDocument> In(string html);
}

// core, Tools/Web/Robots (step 2): may this page be read?
public sealed record RobotsRule(bool Allow, string Pattern);                 // "Disallow: /wp-admin/", "Allow: /*.css$"
public sealed record RobotsRules(IReadOnlyList<RobotsRule> Rules)             // the group that applies to us
{
    public bool Allows(string pathAndQuery);                                  // longest match wins; Allow on a tie; nothing matches: allowed
}
public static class RobotsTxt
{
    public static RobotsRules Parse(string text, string userAgentToken);      // our group ("louis-agent"), else "*", else allow all
}
public enum RobotsVerdict { Allowed, Disallowed, Unreachable }
public sealed record RobotsFile(int? Status, string? Text);                   // 200 and the text; 404; 503; null status for a network error
public sealed class RobotsPolicy(Func<Uri, CancellationToken, Task<RobotsFile>> fetchRobots, TimeProvider? clock = null)
{
    public Task<RobotsVerdict> CheckAsync(Uri url, CancellationToken cancellationToken = default);   // one robots.txt per host, kept a day
}
public sealed class RobotsWebBrowser(IWebBrowser inner, RobotsPolicy robots) : IWebBrowser;   // refuses a disallowed page before Chrome opens it
// PageFetcher gains `RobotsPolicy? robots = null` and asks it on every hop; WebBrowserFactory wraps the recorded browser in RobotsWebBrowser.

// pack, Recipes (step 3): what a recipe is, as the page published it
public sealed record Recipe(string Name, string? Yield, TimeSpan? PrepTime, TimeSpan? CookTime, TimeSpan? TotalTime,
    IReadOnlyList<string> Ingredients, IReadOnlyList<RecipeStep> Steps, Uri? Image, string? Author, Uri Source);
public sealed record RecipeStep(string? Section, string Text);
public static class SchemaOrgRecipe
{
    /// The first Recipe entity in the page's JSON-LD (the document, an array or a @graph), or null when there is none.
    public static Recipe? Read(string html, Uri source);
}

// pack, Sources (step 4): a site that can be searched
public enum RecipeSourceKind { DrizzleAndDip, LazyMakoti }             // RecipesFromSouthAfrica only if step 0 finds its data
public sealed record RecipeSite(RecipeSourceKind Kind, Uri Home, string DisplayName);
public static class RecipeSites { public static RecipeSite Of(RecipeSourceKind kind); }
public sealed record RecipeHit(string Name, Uri Url);
public interface IRecipeSource
{
    RecipeSourceKind Kind { get; }
    Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults, CancellationToken cancellationToken = default);
}
public sealed class WordPressSource(RecipeSite site, IWebBrowser browser) : IRecipeSource;   // /?s={query}; a results list or one post
public static class RecipeSourceFactory
{
    public static IReadOnlyList<RecipeSourceKind> Enabled(string? setting);                  // RECIPE_SOURCES
    public static IReadOnlyDictionary<RecipeSourceKind, IRecipeSource> Create(IReadOnlyList<RecipeSourceKind> enabled, IWebBrowser browser);
}
public sealed class RecipeSourceException(string message) : Exception(message);               // a RecipeToolMessage, never a crash

// pack, Tools (step 5): what the model calls
public sealed class RecipeTools(IReadOnlyDictionary<RecipeSourceKind, IRecipeSource> sources, IWebBrowser browser)
{
    [Description("…")] public Task<string> FindRecipes(string source, string query, int maxResults = 5, CancellationToken ct = default);
    [Description("…")] public Task<string> ReadRecipe(string url, CancellationToken ct = default);   // any public page
}
public sealed class RecipesPack(RecipesOptions options) : IAgentPack;   // Name "recipes", one toolset, Requires []
```

```mermaid
classDiagram
    direction LR
    class JsonLdBlocks { <<core, step 1>> +In(html)$ IReadOnlyList~JsonDocument~ }
    class LeadImage { <<core, changed step 1>> }
    class IWebBrowser { <<core, as is>> +VisitAsync(url, capture, session, script, ready, ct) }
    class RecordedWebBrowser { <<core, as is>> records or replays }
    class Recipe { <<record, step 3>> Name Yield PrepTime CookTime TotalTime Ingredients Steps Image Author Source }
    class RecipeStep { <<record, step 3>> Section Text }
    class SchemaOrgRecipe { <<step 3>> +Read(html, source)$ Recipe? }
    class IRecipeSource { <<interface, step 4>> +Kind +FindAsync(query, max, ct) }
    class WordPressSource { <<step 4>> }
    class RecipeSites { <<step 4>> +Of(kind)$ RecipeSite }
    class RecipeSourceFactory { <<step 4>> +Enabled(setting)$ +Create(enabled, fetcher)$ }
    class RecipeTools { <<step 5>> +FindRecipes() +ReadRecipe() }
    class RecipePrinter { <<step 5>> +Print(recipe)$ }
    class RecipesPack { <<step 5>> }
    class IAgentPack { <<core>> }
    class RobotsPolicy { <<core, step 2>> +CheckAsync(url, ct) RobotsVerdict }
    class RobotsTxt { <<core, step 2>> +Parse(text, token)$ RobotsRules }
    class RobotsWebBrowser { <<core, step 2>> }
    class PageFetcher { <<core, changed step 2>> asks the policy per hop }
    IWebBrowser <|.. RobotsWebBrowser
    RobotsWebBrowser --> IWebBrowser : wraps the recorded one
    RobotsWebBrowser --> RobotsPolicy
    PageFetcher --> RobotsPolicy
    RobotsPolicy ..> RobotsTxt
    RobotsPolicy --> PageFetcher : reads robots.txt, recorded
    LeadImage ..> JsonLdBlocks : uses
    SchemaOrgRecipe ..> JsonLdBlocks : uses
    SchemaOrgRecipe ..> Recipe : builds
    Recipe *-- RecipeStep
    IRecipeSource <|.. WordPressSource
    WordPressSource --> IWebBrowser
    WordPressSource --> RecipeSites : its row
    RecipeSourceFactory ..> WordPressSource : creates
    RecipeTools --> IRecipeSource : one per enabled site
    RecipeTools --> IWebBrowser : ReadRecipe
    RecipeTools ..> SchemaOrgRecipe
    RecipeTools ..> RecipePrinter
    RecipesPack ..> RecipeTools : CreateTools
    RecipesPack ..> RecipeSourceFactory
    IAgentPack <|.. RecipesPack
    IWebBrowser <|.. RecordedWebBrowser
```

### Architecture

```mermaid
flowchart LR
    subgraph host["Any host (API, CLI, MCP server)"]
        comp["louis-agent.composition<br/>PackKind.Recipes (step 4)"]
        engine["louis-agent.core<br/>the loop, IWebBrowser + RecordedWebBrowser,<br/>JsonLdBlocks (step 1), RobotsPolicy (step 2)"]
        pack["packs/louis-agent.pack.recipes<br/>Recipe + SchemaOrgRecipe (3)<br/>sources (4), RecipeTools + guide (5)"]
        comp --> pack
        pack --> engine
    end
    dd["drizzleanddip.com"]
    lm["thelazymakoti.com"]
    any["any public recipe page"]
    store[("/data/replay<br/>recorded answers")]
    chrome["puls-browser (Chrome, BROWSER_URL)<br/>RenderPolicy on every request"]
    pack -- "visit /?s=… and the recipe page" --> chrome
    chrome --> dd
    chrome --> lm
    chrome -- "ReadRecipe(url)" --> any
    engine -- "WEB_REPLAY=record / replay" --> store
```

### Communication: the cook asks for a bobotie recipe (F35.2 will do this; here it is the shape the tools serve)

```mermaid
sequenceDiagram
    autonumber
    participant M as Model (the cook)
    participant T as RecipeTools
    participant S as WordPressSource (drizzleanddip)
    participant F as IWebBrowser (Chrome, recorded or replayed)
    participant R as SchemaOrgRecipe
    M->>T: FindRecipes("drizzleanddip", "bobotie")
    T->>S: FindAsync("bobotie", 5)
    S->>F: VisitAsync https://drizzleanddip.com/?s=bobotie
    F->>F: robots.txt of drizzleanddip.com, once a day (RobotsPolicy): allowed
    F-->>S: the results page (HTML)
    S-->>T: 5 hits: name · link
    T-->>M: a numbered list, framed as untrusted data
    M->>T: ReadRecipe("https://drizzleanddip.com/2018/09/26/traditional-south-african-bobotie…")
    T->>F: VisitAsync the page
    F-->>T: HTML
    T->>R: Read(html, url)
    R-->>T: Recipe (serves 6, 1 h 15, 14 lines, 9 steps, photo, author)
    T-->>M: the recipe printed: Serves · times · ingredients · steps · photo · link
```

### Objects in one process

```mermaid
flowchart TB
    cat["AgentCatalog"] --> rp["RecipesPack (one per process)"]
    rp --> opt["RecipesOptions<br/>RECIPE_SOURCES"]
    rp --> fetcher["IWebBrowser (the host's one,<br/>ToolsetContext.Browser, recorded or replayed)"]
    rp -- "CreateTools per engine" --> tools["RecipeTools"]
    tools --> dict["Dictionary‹RecipeSourceKind, IRecipeSource›"]
    dict --> s1["WordPressSource<br/>RecipeSite(DrizzleAndDip, https://drizzleanddip.com, 'Drizzle and Dip')"]
    dict --> s2["WordPressSource<br/>RecipeSite(LazyMakoti, https://thelazymakoti.com, 'The Lazy Makoti')"]
    s1 --> fetcher
    s2 --> fetcher
    tools --> fetcher
    fetcher --> robots["RobotsPolicy (one per process,<br/>a host's robots.txt kept a day)"]
```

### Folder trees

Core, after step 1 (one new folder, one changed file):

```
louis-agent.core/Tools/Web/
├── Browsing/            …, RobotsWebBrowser (the decorator)             ← step 2
├── Fetching/            HtmlText, LeadImage (now on JsonLdBlocks), PageFetcher (asks the policy), PageText
├── Robots/              RobotsRule, RobotsRules, RobotsTxt, RobotsVerdict, RobotsFile, RobotsPolicy   ← step 2
├── StructuredData/      JsonLdBlocks                                   ← step 1
└── …
```

The pack, after step 4 (mirrored in `tests/louis-agent.pack.recipes.tests/`):

```
packs/louis-agent.pack.recipes/
├── louis-agent.pack.recipes.csproj
├── RecipesPack.cs             the pack: one toolset, recipes                  ← step 5
├── RecipesToolset.cs          enum { Recipes }                                ← step 5
├── RecipesOptions.cs          RECIPE_SOURCES                                  ← step 4
├── Recipes/                   Recipe, RecipeStep, SchemaOrgRecipe             ← step 3
├── Sources/                   IRecipeSource, RecipeSourceKind, RecipeSite, RecipeSites,
│                              RecipeHit, RecipeSourceException, RecipeSourceFactory   ← step 4
├── Providers/                 WordPressSource, SearchResultLinks              ← step 4
├── Tools/                     RecipeTools, RecipePrinter                      ← step 5
├── Messages/                  RecipeToolMessage, RecipeLogMessage             ← steps 4, 5
└── Prompts/toolsets/recipes.md                                                ← step 5

tests/louis-agent.pack.recipes.tests/
├── Recipes/SchemaOrgRecipeTests.cs, Fixtures/{site}-{dish}.html (the ld+json block, trimmed)   ← step 3
├── Providers/WordPressSourceTests.cs, Fixtures/{site}-search-{query}.html                    ← step 4
├── Sources/RecipeSourceFactoryTests.cs                                                        ← step 4
├── Tools/RecipeToolsTests.cs, Messages/RecipeToolMessageSnapshot.json (+Tests)               ← step 5
└── RecipesPackTests.cs, RecipesPrompts.cs                                                     ← step 5
```

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `JsonLdBlocks` | `louis-agent.core/Tools/Web/StructuredData/` | `LouisAgent.Core.Tools.Web.StructuredData` | 1, new |
| `LeadImage` (uses it; regex removed) | `louis-agent.core/Tools/Web/Fetching/` | `LouisAgent.Core.Tools.Web.Fetching` | 1, changed |
| `JsonLdBlocksTests` | `tests/louis-agent.core.tests/Tools/Web/StructuredData/` | `LouisAgent.Core.Tests.Tools.Web.StructuredData` | 1, new |
| `RobotsRule`, `RobotsRules`, `RobotsTxt`, `RobotsVerdict`, `RobotsFile`, `RobotsPolicy` | `louis-agent.core/Tools/Web/Robots/` | `LouisAgent.Core.Tools.Web.Robots` | 2, new |
| `RobotsWebBrowser` | `louis-agent.core/Tools/Web/Browsing/` | `LouisAgent.Core.Tools.Web.Browsing` | 2, new |
| `PageFetcher` (asks the policy per hop), `WebBrowserFactory` (wraps the browser), `WebToolMessage.RobotsDisallows` and `.RobotsUnreachable` | `Fetching/`, `Browsing/`, `Messages/` in core | as they are | 2, changed |
| `RobotsTxtTests`, `RobotsPolicyTests`, `RobotsWebBrowserTests`, a case in `PageFetcherTests` | `tests/louis-agent.core.tests/Tools/Web/Robots/`, `…/Browsing/`, `…/Fetching/` | `LouisAgent.Core.Tests.Tools.Web.…` | 2 |
| `Recipe`, `RecipeStep`, `SchemaOrgRecipe` | `packs/louis-agent.pack.recipes/Recipes/` | `LouisAgent.Packs.Recipes.Recipes` | 3, new |
| `IRecipeSource`, `RecipeSourceKind`, `RecipeSite`, `RecipeSites`, `RecipeHit`, `RecipeSourceException`, `RecipeSourceFactory` | `Sources/` | `LouisAgent.Packs.Recipes.Sources` | 4, new |
| `WordPressSource`, `SearchResultLinks` | `Providers/` | `LouisAgent.Packs.Recipes.Providers` | 4, new |
| `RecipesOptions` | the pack's root | `LouisAgent.Packs.Recipes` | 4, new |
| `RecipeToolMessage`, `RecipeLogMessage` | `Messages/` | `LouisAgent.Packs.Recipes.Messages` | 4 (the source's texts), 5 |
| `RecipeTools`, `RecipePrinter` | `Tools/` | `LouisAgent.Packs.Recipes.Tools` | 5, new |
| `RecipesPack`, `RecipesToolset` | the pack's root | `LouisAgent.Packs.Recipes` | 5, new |
| `PackKind.Recipes`, `Plugins.Create` arm | `src/louis-agent.composition/` | `LouisAgent.Composition` | 5, changed |
| Tests mirror each; `PackConvertibilityTests`, `PluginsTests` get the pack | `tests/louis-agent.pack.recipes.tests/`, `tests/louis-agent.composition.tests/` | `LouisAgent.Packs.Recipes.Tests.…` | 3 to 5 |

## Steps

One commit each, test first, the review stop after each (rule 5). The commit's scope is `f35.1`.

| # | Step and commit | Test first (seen failing, output kept) | Concept (GOAL skill) |
|---|---|---|---|
| 0 | **The plan on its branch, and the free check.** `feature/F35.1-recipe-sources` off `main`; this plan at `docs/plans/F35-recipe-sources.md` with its README row; the TODO's F35 line gets the plan link; ADR 0067 *A page's own data before its text* (below). The check, from Rider on the PC: three recipe pages per site (bobotie, melktert, chakalaka and the like) and one search page per site fetched with `curl`, their `application/ld+json` blocks read by eye; the result goes in this plan's *Facts checked* (which site has the block, which plugin, how `recipeYield` and `recipeInstructions` are written) and the trimmed pages, and each site's `robots.txt`, become the fixtures of steps 2 to 4. `docs(f35.1): step 0 - the F35 plan, ADR 0067 and the sites' data checked` | Docs only; the check's table | Measure before building (12) |
| 1 | **`JsonLdBlocks` in core; `LeadImage` onto it.** `feat(f35.1): step 1 - JsonLdBlocks reads a page's ld+json; LeadImage drops its regex` | `JsonLdBlocksTests`: two blocks on one page; a block with `&quot;` entities; `</script>` escaped inside a JSON string (`<\/script>`); a malformed block skipped, the next read; a `type` written `application/ld+json; charset=utf-8`; no block, an empty list. `LeadImageTests` unchanged and green | A typed reader over a regex (ADR 0057); structured data in the page (5) |
| 2 | **The robots policy in core.** `RobotsTxt`, `RobotsRules`, `RobotsPolicy` (a day's cache per host, on `TimeProvider`), `RobotsWebBrowser`, `PageFetcher` asking the policy on every hop, `WebBrowserFactory` wrapping the browser, `WebToolMessage.RobotsDisallows` and `.RobotsUnreachable`, ADR 0068. `feat(f35.1): step 2 - every fetch obeys robots.txt` | `RobotsTxtTests` on real files (the three sites' from step 0, and the RFC's examples): the `louis-agent` group over `*`; the longest match wins; `Allow` on a tie; `*` and `$`; a `Disallow:` with no path allows all; comments and blank lines; no group for us and no `*` allows all. `RobotsPolicyTests` with a fake fetch and a fake clock: one fetch per host a day (a second check doesn't fetch; a day later it does); 404 allows all; a disallowed path is `Disallowed`; the unreachable case as finding G settles it. `RobotsWebBrowserTests`: a disallowed page throws `WebBrowserException` with `RobotsDisallows` and the inner browser is never called; an allowed one passes through. `PageFetcherTests`: a disallowed hop is refused with the reason, as a private address is; `robots.txt` itself is never checked. The golden file changes (two new `WebToolMessage` texts): its diff read | A policy where every request passes (GoF *Decorator*, the `RecordedWebBrowser` precedent; the per-hop check beside the SSRF guard); the browser says what it is and obeys what it's told (ADR 0038; 15) |
| 3 | **The pack project, `Recipe` and `SchemaOrgRecipe`.** The csproj and the tests project (added to the solution), `Recipe`, `RecipeStep`, `SchemaOrgRecipe.Read`. `feat(f35.1): step 3 - the recipes pack: Recipe read from a page's schema.org data` | `SchemaOrgRecipeTests` on the step-0 fixtures: a WP Recipe Maker page in a `@graph`; `recipeYield` as `"6"`, `6` and `["6", "6 servings"]`; `recipeInstructions` as `HowToStep`s, as `HowToSection`s and as text; `PT1H15M` to 1 h 15; `image` as a string, an object or a list; a page with no `Recipe` gives null; a relative image resolved against the page | A page's data as a record (5, 7); Data Mapper |
| 4 | **The sources.** `RecipeSourceKind`, `RecipeSite`/`RecipeSites`, `IRecipeSource`, `RecipeHit`, `WordPressSource` with `SearchResultLinks`, `RecipeSourceFactory`, `RecipesOptions`, `RecipeSourceException`, the source's `RecipeToolMessage` texts. `feat(f35.1): step 4 - a WordPress site searched as a source` | `WordPressSourceTests` with a fake `IWebBrowser` that returns the recorded search page (as `CheckersCatalogueTests` does): Drizzle and Dip's list gives hits in page order, capped at `maxResults`, links to its own site only, no duplicates; The Lazy Makoti's single-post answer gives one hit (the visit's own URL and the page's title); an empty search gives none; a `WebBrowserException` from the browser (the site refused, a timeout) becomes `RecipeSourceException` with the site and the browser's reason. `RecipeSourceFactoryTests`: `RECIPE_SOURCES` parsed as `SHOP_CATALOGUES` is (case, repeats, an unknown name ignored, unset none) | One contract, one implementation per provider, picked in one factory (ADR 0003; 4) |
| 5 | **The tools, the guide, the pack, registered.** `RecipeTools` (`FindRecipes`, `ReadRecipe`), `RecipePrinter`, the rest of `RecipeToolMessage`, `Prompts/toolsets/recipes.md`, `RecipesPack`, `RecipesToolset`, `PackKind.Recipes` and the `Plugins` arm, the composition's csproj reference, `config/.env.example`, `docs/projects/louis-agent.pack.recipes.md`, the golden files regenerated and their diffs read, the free hand check through the MCP server. `feat(f35.1): step 5 - FindRecipes and ReadRecipe, the recipes toolset` | `RecipeToolsTests` with fake sources and a fake `IWebBrowser`: `FindRecipes` lists hits numbered with their links under the untrusted frame; an unknown source names the enabled ones; `ReadRecipe` prints the record (*Serves 6 · 20 min + 55 min*, the lines numbered, the steps under their sections, the photo, the author, the link); a page with no block says so and suggests `FetchUrl`; an invalid address is refused before any visit, and a private one by the browser's own policy (its `WebBrowserException` comes back as text); `maxResults` capped at 10. `RecipesPackTests`: unavailable without `RECIPE_SOURCES` or `BROWSER_URL` (names the missing one), `ParallelSafe` (the browser opens at most four pages at once), the guide embedded with its header. Composition: `PackConvertibilityTests` and `PluginsTests` pass with the new pack; `ArchitectureTests` green | Tool design: a whole recipe in a small, framed result (4, 15); the pack shape (ADR 0027, 0028) |

**The hand check in step 5, free:** `PACKS=recipes RECIPE_SOURCES=drizzleanddip,lazymakoti` with `BROWSER_URL` pointing at the Puls stack's browser container, on the MCP server, which
exposes tools with no model (ADR 0028), then `FindRecipes("drizzleanddip", "bobotie")` and `ReadRecipe` of a hit from
Rider's MCP client. The result goes in the PR's description: the done-when of F35.1 is *`FindRecipes` and `ReadRecipe`
answer from recorded pages*, which the tests show, and this shows the live site once.

### What to look at closely

- **Step 1:** the reader must not stop at a `</script>` inside a JSON string: JSON-LD written by WordPress escapes it
  as `<\/script>`, but the test holds the case. `LeadImage`'s behaviour must not change: its tests are the proof.
- **Step 2:** the policy must never check `robots.txt` itself (a loop), and must read it through the recorded
  fetcher, so a replay never opens the network. `RobotsWebBrowser` sits **outside** `RecordedWebBrowser`, so a replay
  of an old trial that has no `robots.txt` recording fails as a miss, loudly, never quietly as allowed (ADR 0049):
  the cook eval's recorded trials are re-recorded in F35.2's paid run. The matcher is a plain walk over the pattern
  (`*` and `$` only; ADR 0057). The policy checks the page the browser is asked to open, not the images, scripts and
  frames that page loads: those are what any visitor's browser loads for it.
- **Step 3:** `recipeYield` is the field every plugin writes differently; the record keeps it as the page's text
  (`"6 servings"`), and F48's reader turns it into a number. The ingredient lines are kept **as they are**, for F48.
- **Step 4:** The Lazy Makoti's search answers with the post itself when there is one match; the reader tells the
  two shapes apart by the page's own markup (a results list has many article links; a single post has a `Recipe`
  block). A link that leaves the site is never a hit.
- **Step 5:** every text in `RecipeToolMessage`, nothing inline (ADR 0009); the frame line is the pack's own copy of
  the shops' wording; every visit goes through the browser's `RenderPolicy` (F33), which refuses private and
  link-local addresses on every request the page makes, so the pack can't be used to reach an internal service
  through a recipe link. The browser's cap of four pages at once holds for recipe reads too.

## ADR 0067: a page's own data before its text

**Context.** Twice now, reading a site's data in code beat letting the model read its pages: the shops (0 of 32 prices
right from pages, 31 of 31 from the catalogue, `docs/GOAL.md:33`) and the recipe pages, where a page publishes its
recipe for machines as a schema.org block. The model reading prose costs more tokens, gets amounts wrong, and leaves
nothing C# can check.

**Decision.** *When a page publishes what we need as structured data (JSON-LD, a feed, a catalogue answer), a tool
reads that data in code and hands the model a short, framed print of the record; the model reads the page's text only
where the page has no such data, and then says so.* The reader is infrastructure in core, shared by every pack; the
record is the pack's own; the print is bounded and framed as untrusted (ADR 0005).

**Consequences.** Fewer tokens per answer and exact amounts; a record a check can grade against its source and a
later feature (F48) can compute on; one more reader to keep when a site changes its block; a site without the block
falls back to text, which the tool's answer names.

## ADR 0068: every fetch obeys robots.txt

**Context.** The browser says what it is (ADR 0038), but nothing in core reads what a site says back: `robots.txt`,
the file where a site tells automated readers which paths to leave alone. The cook's prompt carries a hand-kept list
of nine sites that refuse readers, which code can't check and a prompt can forget. Louis's pick (F35, finding E3):
the check belongs on every fetch, not on a person at enable time.

**Decision.** *Every page core fetches or opens for a tool obeys the site's `robots.txt`*: the group for
`louis-agent`, else `*`; the longest matching rule wins, `Allow` on a tie; a disallowed page is refused with the
reason, as a private address is, and the model is told. `robots.txt` is read once a day per host through the
recorded fetcher, so a replay never opens the network; `robots.txt` itself is always read; a missing file (4xx)
allows all; an unreachable one (5xx, a network error) refuses the host's pages for an hour, then the file is tried again (G1). `Crawl-delay` and sitemaps stay with the
collecting job (*Sources as data*).

**Consequences.** A site that says no is never read, by any tool, whatever a prompt says; the cook's hand-kept list
goes in F35.2. One more request per host per day. A recorded trial from before this needs re-recording, since its
`robots.txt` was never saved (ADR 0049: no lenient path). A site that blocks by status (403) or a captcha isn't
`robots.txt`'s business: that stays a refused page, as today.

## Verification

- **Each step:** `dotnet build louis-agent-solution.sln`, the pack's tests and core's (`dotnet test tests/louis-agent.core.tests`,
  `dotnet test tests/louis-agent.pack.recipes.tests`), the composition's in step 4; the whole solution before the PR.
- **Step 2:** core's web tests; the golden file's diff is two new texts. The cook eval's old recordings won't replay
  until F35.2 re-records them (a paid run, on Louis's go).
- **Golden files** (step 5): the pack's `RecipeToolMessageSnapshot.json` is new; Puls's session golden doesn't change
  (Puls doesn't name the pack until F35.2).
- **Free hand check** (step 5): the MCP server call above, on the live sites, once.
- **No paid run** in F35.1. F35.2's eval run (the cook on the recipes pack, and the three-way measure of page text
  against the block against a screenshot) waits for Louis's go, as the platform plan says.

## Concepts this teaches (GOAL skills 4, 5, 7, 12, 15)

| Step | Concept | Where it sits |
|---|---|---|
| 1 | **Structured data in the page.** Sites publish their facts for machines beside the text people read; reading that is cheaper and more exact than reading prose. | Skill 5 (structured outputs), 7 (context: fewer tokens in) |
| 2 | **Obeying a site's rules in code.** `robots.txt` is the web's contract with automated readers; a policy object checks it where every request passes, so no prompt has to remember which sites say no. | Skill 15 (safety: what we read, and may), 4 |
| 3 | **A data mapper.** One class that knows the outside shape (schema.org, in all its variants) and builds our plain record, so nothing else in the code knows the outside shape. | Skill 4 (tool design) |
| 4 | **A provider behind a contract.** The strategy pattern with data where subclasses would only differ in a URL; the setting that turns a provider on is the one place a site is allowed. | Skill 4, 15 (what we read, and may) |
| 5 | **A tool's result is for the model.** Small, numbered, framed as data; the model quotes, it doesn't transcribe. And a pack that stays an MCP server in shape. | Skill 4, 11 (protocols), 15 |
| all | **Record and replay for free.** Reading through the browser we already record makes the eval repeatable with nothing new. | Skill 12 (evaluation) |

## Approved during the build

- **2026-11-04, step 0, at least 25 South African sites, only ones that allow us** (Louis: "we need at least 25 SA
  recipe sites", "it can be blogs, food blogs", "we need ones which allows us"). A site *allows us* when nothing it
  publishes says no: its `robots.txt` (the `*` group and the groups for AI readers: GPTBot, ClaudeBot, anthropic-ai,
  CCBot, Google-Extended, Applebot-Extended and the like), the recipe page's robots meta and `X-Robots-Tag`
  (`noai`, `noimageai`), and a TDM reservation (`tdmrep.json`, `tdm-reservation`). Step 2's robots policy and ADR
  0068 take this in; its design (which reader names count, what a `noai` page answers) is step 2's to put to Louis.
  74 sites were checked from the PC on 2026-11-04, `robots.txt` first and one recipe page each: 45 allow us, 12
  refuse AI readers (News24 and Netwerk24, with Food24, Sarie, Huisgenoot and YOU; Eat Out; Maroela Media; Women's
  Health SA; Food For Mzansi; halaal.recipes; Micaela's Kitchen; Le Creuset; The Pretty Blog; avocado.co.za's
  `noindex`; and **drizzleanddip.com's `noai` meta, so it is out**), 11 answered 403, a challenge or an error, and 11
  were gone or not recipe sites.
- **2026-11-04, step 0, read the 45 by data where they have it, else by their text** (Louis's pick 2 of three; the
  others were the 14 data sites only, and searching on or asking sites). 25 sites with `Recipe` data that allow us
  don't exist among the 74: most magazines, TV cooks and brands publish only Yoast's Article graph. So 14 sites are
  read from their `Recipe` data, as ADR 0067 says, and 31 from their page text, labelled as text, which ADR 0067
  allows where a page has no data. How the text path reads and prints a recipe, how a site without a search we may use
  is found from (12 disallow their search or have none, 4 weren't tried, and 11 aren't WordPress), and whether 45 sites are an
  enum or a table are findings for steps 3 to 5, put to Louis there.

| Site | Kind | Read by | Its own search | Notes |
|---|---|---|---|---|
| taste.co.za (Woolworths TASTE) | magazine | data, the site's own | `/?s=` | |
| foodandhome.co.za | magazine | data, the site's own | `/?s=` | fixture |
| bibbyskitchenat36.com | blog | data, the theme's own | `/?s=` | fixture |
| tantalisemytastebuds.com | blog, Cape Malay | data, WP Recipe Maker in Rank Math's `@graph` | `/?s=` | fixture |
| wandercapetown.com | blog | data, WP Recipe Maker in Yoast's `@graph` | `/?s=` | fixture |
| thecinnamonjar.com | blog, an SA-born cook in Sheffield | data, WP Recipe Maker | `/?s=` | |
| saltyginger.com | blog, SA recipes (the author seems to be in NZ) | data, WP Recipe Maker | `/?s=` | |
| thebigtastybite.com | blog, many SA recipes | data, WP Recipe Maker | `/?s=` | |
| bitsofcarey.com | blog (Carey Erasmus) | data, WP Recipe Maker | `/?s=` | |
| simply-delicious-food.com (Alida Ryder) | blog | data, WP Recipe Maker | disallowed | |
| feastandwest.com | blog | data, WP Recipe Maker | disallowed | |
| foodleclub.com | blog | data, WP Recipe Maker | disallowed | |
| woolworths.co.za | retailer | data, the site's own | disallowed | its `robots.txt` allows AI readers by name |
| novanews.co.za/rooirose (Rooi Rose) | magazine, Afrikaans | data, Penci Recipe | disallowed | fixture; `Content-Signal: ai-input=yes, ai-train=no` |
| thelazymakoti.com | blog | text | `/?s=` | |
| recipesfromsouthafrica.com | blog | text | `/?s=` | |
| thekatetin.com | baking blog | text | `/?s=` | WP Recipe Maker loaded, no card on the post seen |
| cooksister.com | blog, an SA author in London | text | `/?s=` | WP Recipe Maker loaded, none on the 2010 post seen |
| vrouekeur.co.za | magazine, Afrikaans | text | `/?s=` | |
| thefoodfox.com (Ilse van der Merwe) | blog | text | `/?s=` | |
| mykitchen.co.za | recipe site | text | `/?s=` | |
| cupcakesandcouscous.com | blog | text | `/?s=` | |
| sarahgraham.co.za | TV cook | text | `/?s=` | |
| zolanene.com | TV cook | text | `/?s=` | |
| sibamtongana.com | TV cook | text | `/?s=` | |
| braai.com (Jan Braai) | TV cook | text | `/?s=` | |
| realmealrevolution.com | brand | text | `/?s=` | |
| rhodesquality.com | brand | text | `/?s=` | |
| foodloversmarket.co.za | retailer | text | `/?s=` | |
| thesouthafrican.com | news site, food section | text | `/?s=` | |
| finglobal.com | emigration firm with SA recipes | text | `/?s=` | |
| thestarlitpath.com | lifestyle blog | text | `/?s=` | |
| lookatwhatimade.net | mostly crochet, a few recipes | text | `/?s=` | |
| inspiredlivingsa.co.za | blog | text | disallowed | |
| clover.co.za | brand | text | not tried | |
| luckystar.co.za | brand | text | none (it answers the home page) | not WordPress |
| spar.co.za | retailer | text | not tried | not WordPress |
| pnp.co.za | retailer | text | disallowed | an app drawn in the browser; `Crawl-delay: 10` |
| paarman.co.za (Ina Paarman) | brand | text (its `Recipe` has no ingredients or steps) | `/search?q=` | Shopify |
| thedaleyplate.com | blog | text | disallowed | Squarespace |
| jackiecameron.co.za | cooking school | text | disallowed | Squarespace |
| foodiesofsa.com | recipe site | text | not tried | Wix |
| koesister.com | heritage food site | text | not tried | Wix |
| shirleybanting.blogspot.com | blog | text | disallowed | Blogger |
| thesouthafricancookbook.blogspot.com | blog | text | disallowed | Blogger |

## F35.2 (planned when it starts)

The cook: `recipes` in Puls's packs and the cook's toolsets, `puls-cook.md` finds and reads a recipe with the tools
and links it (its nine-site block list and its photo rules shrink); the `local-sources` check in the pack (the recipe
link is on an enabled source); the recipes eval, the three-way measure (page text, the block, a screenshot) on
recorded pages; the write-up `docs/learning/18-structured-data.md`. Vision (`ScreenshotPage`) is its own feature if
the measure wants it.
