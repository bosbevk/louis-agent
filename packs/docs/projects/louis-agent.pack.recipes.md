# louis-agent.pack.recipes — design

> **Kind:** class library (`net10.0`) · **Path:** `packs/louis-agent.pack.recipes/` · **References:** louis-agent.composition,
> Microsoft.Extensions.FileProviders.Embedded · **Loaded by:** `PACKS=recipes` (an identity names it from F35.2) ·
> **Uses:** core's guarded browser (`BROWSER_URL`) and, for some sites, core's web search provider

## Purpose

What an agent can do with recipes: search a South African recipe site, and read a recipe page into numbered
ingredients and method lines. A model that reads a recipe page as raw text wastes its context on menus and comments
and can slip on an amount; this pack reads the recipe the page publishes for machines (schema.org's `Recipe`) in code
and hands the model a short, exact print. It is a pack ([ADR 0027](../../../src/docs/adr/0027-engine-identities-and-packs.md)), so
any identity can use it, and it could be served as an MCP server unchanged
([ADR 0028](../../../src/docs/adr/0028-packs-stay-convertible-to-mcp.md)).

## How it works

- **One toolset, `recipes`.** `RecipesPack` defines it with its guide (`Prompts/toolsets/recipes.md`) and its two
  tools. It loads only where `RECIPE_SOURCES` names a site and `BROWSER_URL` gives a browser; otherwise
  `GET /v1/toolsets` lists it with the setting it lacks. Its calls only read, so a round's calls run at once.
- **A search.** `RecipeTools.FindRecipes(query, source, maxResults)` with no `source` hands the search to `RecipeScatter`:
  four enabled sites with their own search, at random, asked at once, their hits in a random order and printed
  `1. {title} · {site} · {link}` ([ADR 0078](../adr/0078-a-recipe-search-asks-several-sites.md)). Every hit is shuffled before the list is chosen, so no site or page settles on top, and a title shows without the country (`HitTitle`), since every site is South African; `skip` leaves out sites already asked for this dish. A named `source` looks the
  site up by its short name (any case), asks that site's `IRecipeSource` and prints `1. {title} · {link}`. At most 10, and
  eight by default. The result is framed `--- untrusted recipe data … ---`. A hit can be a page
  that is not a recipe (an article, a round-up), and the description and the guide say so.
- **Two ways to search.** A site's row says how: `WordPressSearch` opens the site's own `/?s=` page through the browser
  and reads the headings that link back to the site, keeping those that share a word with the search (`QueryWords`), since three sites answer no results with suggestions; `SiteWebSearch` asks the web search provider for that site only.
- **A page.** `ReadRecipe(url)` takes any public web address (not only the enabled sites), visits it in the guarded
  browser and reads the `Recipe` in its JSON-LD (`SchemaOrgRecipe`). `RecipePrinter` prints it: the name, one line
  *Makes {yield} · prep 20 min · cook 30 min · total 50 min* with only the parts the page gave, the ingredients as
  written and the method as numbered lines (a section's name as a sub-heading above the ingredients as well as the
  steps, the numbers running straight on), the photo, the author and the link, with at most 60 ingredients and 60
  steps and a line when it cuts. An ingredient is a `RecipeIngredient(Section, Text)`, the same shape as a step: a
  line of the page's list that is only a heading (a whole line in `<h1>` to `<h6>`, or one starting with two or more
  `*` or `#`) is not an ingredient but the section of the lines after it, in the page's own words. A one-star bullet,
  a line with some words in bold (`**2 eggs** (large)`) or a line ending in a colon stays an ingredient. Every other
  HTML tag is taken out of every text, once the entities are read: a tag that breaks a line (`<br>`, `<p>`, `<div>`,
  `<li>`) leaves a space, so two sentences stay two, and a `<` that opens no tag stays.
- **A page without recipe data.** It comes back as the page's text (core's `HtmlText.ToText`), framed, labelled *{host}
  publishes no recipe data; this is the page's text*, cut at 8,000 characters (and said so), then the link.
- **Failures are text.** A site's refusal, the browser's refusal (a private address, a page a site's `robots.txt`
  disallows), a bad address or an unknown site comes back as a result with its reason (`RecipeToolMessage`), never an
  exception. A bad address is refused before the browser is asked for anything.

- **A check that the recipe came from a site.** `recipe-from-source` (`Checks/`, F35.2) passes when the recipe link in
  the first turn's answer is on a site of the table (the host with or without `www.`, under the site's home, so
  Rooi Rose's `novanews.co.za/rooirose` doesn't let the publisher's other sections through), and says which site by
  its short name; another host fails and is named, no link fails, and a first turn that broke is Unknown. How an
  answer writes its link is the identity's, so the file names a link reader beside the check
  (`{"type": "recipe-from-source", "parameters": {"reader": "recipe-link"}}`), as the shops pack's
  `estimates-at-shop` takes a price reader ([ADR 0059](../../../src/docs/adr/0059-an-identity-ships-readers-not-checks.md),
  [ADR 0073](../../../src/docs/adr/0073-packs-reference-no-evals.md)). It lives apart from the pack, in
  `packs-evals/louis-agent.pack.recipes.checks/`, which only the runner references; `RecipesChecks.All` lists it for the
  demo program's `PulsCatalogs`, and the check catalogue's listing shows it with that one setting.

## Design notes

What F35 settled, and why (the [plan](https://github.com/bosbevk/louis-agent/blob/cc45cd85/demos/puls/docs/plans/F35-recipe-sources.md) is deleted; what it taught is in the [write-up](../../../src/docs/learning/05-structured-outputs.md)):

- **Data first, text second** ([ADR 0067](../../../src/docs/adr/0067-a-pages-own-data-before-its-text.md)). A page that publishes a
  whole recipe is printed from that data; one that doesn't, or whose `Recipe` has no ingredients or no steps, is served
  as its text and labelled, so the model never takes a page's words for a recipe the page didn't mark up.
- **It makes, it does not serve.** The record keeps the page's last yield text and the print says *Makes*. The first
  text was wrong on real pages: Foodle Club writes `["1", "1 9\" milk tart"]` (printed *Serves 1* for a whole tart) and
  The Big Tasty Bite `["10", "10 pies"]`.
- **The sites are content, not code** ([ADR 0011](../../../src/docs/adr/0011-prompts-are-content.md)). A site is a row in
  `Sources/Sites/recipe-sites.md`, with the day its `robots.txt` and pages were read, and a site that later says no is a
  change to the table. The model learns the slugs from the `source` parameter's description (two examples and `*`) and
  from the answer to an unknown name, which names the enabled ones; the guide does not list them.
- **Every fetch obeys `robots.txt`** ([ADR 0068](../../../src/docs/adr/0068-every-fetch-obeys-robots-txt.md)) in the browser, and the
  browser says what it is ([ADR 0038](../../../src/docs/adr/0038-the-browser-says-what-it-is.md)); this pack adds nothing there.
- **A search gives 10 pages, with 3 places kept for pages from outside the library** (`RecipeScatter`), and a search for pap en sous says what else the dish is called (`QueryWords.AlsoCalled`), since a site's own search only finds the word it's given.
- **Scaling** (`Scaling/`, [ADR 0080](../../../src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md)). `ScaleRecipe` works from the recipe we hold: `RecipeAmount` reads the amount a line starts with, in the ways sites write it, and writes it back as a cook would measure it; `RecipeScaler` picks the factor from the yield and prints the lines, numbered as the print is. It lists a step's amounts without changing the step, since the cook writes the steps and knows a portion's size stays. A line holding an amount it can't scale is marked `(not scaled)`, and one with a number left after the amount it scaled is marked `(only the first amount scaled)`, never passed over: it can't tell a pack's size from a second ingredient run into the line, so the cook decides.
- **The library** ([ADR 0082](../adr/0082-the-recipe-library-is-an-open-feed.md)). `Library/Store/` keeps each new proper recipe from a table site, field by field, for good, in Mongo (`MongoRecipeStore`, on `RECIPES_MONGO_URL`). A page with no recipe data is kept only if `Reading/PageText/` finds its recipe plainly, under its headings (`PageTextRecipe`) or, failing that, as a run of amounts followed by steps (`PlainRunRecipe`), and the cook's first read of it stays its text. A page with neither is refused and listed in Mongo (`Library/Excluded/`, the `excluded_pages` collection), so it's never opened or offered again. `Library/Feed/RecipesFeed.cs` publishes them at `/v1/feeds/recipes`, each also at `/v1/feeds/recipes/{id}`. `FeedRecipeLibrary` reads them back over HTTP (`RECIPE_LIBRARY_URL`): the list, held five minutes and searched with `QueryWords`, and one recipe at its own address. `Library/RecipeShelf.cs` is what the tools hold: `RecipeScatter` treats it as one more voice and hands out our address for what we hold, with the sites still asked on every search so new recipes keep coming, and `ReadRecipe` reads our address from the API and everything else from the site. The print's link stays the site's. A live text print ends at the recipe (`Reading/PrintedRecipeText.cs`).
- **One photo, never one we know is gone** ([ADR 0080](../../../src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md)). `Reading/Photos/PagePhotos.cs` lists where a page's photo might be, best first: what it declares, then the first real image after its title, with the site's own copy before an image server's. `Reading/Photos/PhotoCheck.cs` takes the first that isn't known to be gone, within 5 seconds each, through the guarded fetcher. A 403, a 404, no answer, no such server or something that isn't an image drops a photo. A server error, or robots.txt saying we may not fetch it, keeps it: Food & Home's photos sit on a host whose robots.txt answers 500 and load fine in the user's browser, so we fetch nothing and the browser does the loading. Dropped photos are logged by host. A page's text keeps no images of its own, since most are related recipes and logos. In a record run each checked photo is saved with the recording, so recordings grow by about one photo per page read.
- **A missing yield comes from the page's words** (`Reading/PageYield.cs`), in the ways pages word it, on one line or over two, in a bracket, or after a dish's name in capitals.
- **A yield of one is doubted** (`Reading/PageYield.cs`): Taste's data says `1 serving` where its page says `Serves 6 to 8`, so when the data says exactly one serving, the page's own words decide; with none, the yield is unknown. It's the one exception to reading a page's data first ([ADR 0067](../../../src/docs/adr/0067-a-pages-own-data-before-its-text.md)).
- **A numbered print, not JSON.** A model quotes a line by its number, and reads a list better than braces. The same
  recipe always prints the same text, so a test can hold it.
- **Off until named** ([ADR 0023](../../../src/docs/adr/0023-copyleft-service-beside-us.md)): nothing is enabled until
  `RECIPE_SOURCES` names it. Puls's stacks name `*`, every site in the table; a site's terms of use are a separate
  reading, not part of its row (Louis's decision on them is in the plan's F35.2 *Approved during the build*, 2026-11-04).

## Structure

| Path | What's in it |
|---|---|
| `RecipesPack.cs`, `RecipesToolset.cs` | The pack: the `recipes` toolset, what it needs, how its tools are built; its own toolset enum (ADR 0043) |
| `RecipesOptions.cs` | The pack's own setting, `RECIPE_SOURCES`, read once from the environment |
| `Tools/RecipeTools.cs` | The two tools, `FindRecipes` and `ReadRecipe`; `RecipeHitPrinter` writes a search over several sites |
| `Sources/Gathering/` | `RecipeScatter` (the draw, the search at once, the merge), `RecipeGather` and `SiteHit` (its answer) |
| `Tools/RecipePrinter.cs` | The markdown print of a `Recipe`, bounded |
| `Recipes/` | `Recipe`, `RecipeIngredient` and `RecipeStep` (the record), `SchemaOrgRecipe` (reads a page's JSON-LD into it), `SchemaOrgIngredients` (the ingredient lines and their headings), `SchemaOrgValues` (the loose shapes) |
| `Sources/` | `IRecipeSource`, `RecipeHit`, `RecipeSourceException`, `RecipeSourceFactory` (the enabled sites and the search each gets) |
| `Sources/Sites/` | `RecipeSites` (reads the table), `RecipeSite`, the choices `RecipeReadBy` and `RecipeSearch`, and `recipe-sites.md`, the table |
| `Providers/` | `WordPressSearch`, `SiteWebSearch`, `SearchResultLinks` (the hits out of a search page) |
| `../louis-agent.pack.recipes.checks/Checks/` | `RecipeFromSource` (`recipe-from-source`) and `RecipesChecks.All`, the list for the check catalogue |
| `Messages/` | `RecipeToolMessage` (what the tools return, pinned by a golden), `RecipeLogMessage`, `RecipeErrorMessage`, `RecipeCheckMessage` (the check's reasons) |
| `Prompts/toolsets/recipes.md` | The guide the model reads with the toolset: its routing line, the tools, how to search and read |

## Configuration

| Setting | What it does |
|---|---|
| `RECIPE_SOURCES` | The sites that may be searched, comma separated slugs (`foodandhome,thelazymakoti`), or `*` for every site in the table. Unset: the toolset is off |
| `BROWSER_URL` | Core's headless browser, which pages and WordPress searches are read through (core's setting) |

A site searched through the web needs a search provider configured (core's `WEB_SEARCH_PROVIDER` settings). On a
machine whose `config/.env` sets `ROLES_ENABLED` for Puls, a server started with only `PACKS=recipes` refuses to start
(the roles it names aren't there), so it needs `IDENTITIES=puls` too. Details in [Setup](../../../src/docs/SETUP.md).

## Extending it

A new site is a row in `Sources/Sites/recipe-sites.md`, added only after its `robots.txt` (the `*` group and the
groups for AI readers), the robots meta and `X-Robots-Tag` of a recipe page (`noai`, `noimageai`) and any
text-and-data-mining reservation were read, with that day in the `checked` column. Set `read by` to what a recipe page
showed (`data` or `text`) and `search` to `wordpress` when the site's own `/?s=` page is allowed and readable, else
`web`. The table's tests then pick it up. A pack like this one from scratch is
[Build a pack](../../../src/docs/guides/build-a-pack.md).

## Tests

`packs-tests/louis-agent.pack.recipes.tests`, mirroring the folders: `RecipesPackTests` (the toolset needs a site and a
browser, the guide and the table are embedded, the guide routes here and says a hit may not be a recipe),
`RecipeToolsTests` (the search and the reading on fake sites and a fake browser: the frame, the numbering, every
refusal, the text path and its cut), `RecipePrinterTests`, `SchemaOrgRecipeTests` (on recorded pages and made-up
blocks), `WordPressSearchTests`, `SiteWebSearchTests`, `SearchResultLinksTests` and `RecipeSitesTests`,
`RecipeSourceFactoryTests`, `RecipeToolMessageSnapshotTests` with its golden of the model-facing texts, and
`Checks/` (`RecipeFromSourceTests` on canned turns with a fake link reader, `RecipesChecksTests` for the list, the
listing and the settings).
`PackConvertibilityTests` (composition tests) checks the MCP shapes.

## Limits and plans

- **Measured and not.** The table comes from step 0's saved pages and `curl`: 13 of the first 45 sites publish a whole recipe
  in their data, 20 are searched through WordPress and 25 through the web search. None of it has yet gone through the
  real Chrome container except the one hand check of step 5; a site that draws its recipe with JavaScript, or answers
  Chrome differently from `curl`, shows only on a live run.
- A page's text starts with the site's navigation (menus, basket), because the text reader keeps the page as a person
  would see it, so the recipe may begin well into the 8,000 characters.
- SearXNG's site-limited results can be weak: The Daley Plate's bobotie search gave its home page and a category.
- A hit may not be a recipe (an article, a category, a home page); the model reads the titles first.
- A recipe whose page marks it up as microdata, not JSON-LD, is read as text (`thekatetin`, in the table's signals).
- The recipe's yield stays text (*4 to 6*, *10 pies*); a number comes later, when something has to scale it.
- Searching through the web costs what the search provider charges, and a result on another host is dropped.
- Puls names the pack from F35.2 step 1, so the cook's session holds its tools; the cook's prompt starts using them in
  step 2, until then it still reads pages the old way.

## Related docs

[Build a pack](../../../src/docs/guides/build-a-pack.md) · [louis-agent.pack.where-to-buy](louis-agent.pack.where-to-buy.md) ·
[ADR 0067](../../../src/docs/adr/0067-a-pages-own-data-before-its-text.md) · [ADR 0068](../../../src/docs/adr/0068-every-fetch-obeys-robots-txt.md) ·
[For site owners](../../../demos/puls/docs/SITE_OWNERS.md)

## Left out, on purpose

- A number out of the yield (*4 to 6* stays words): F48.1's scaler.
- The text sites' recipes as data (an ingredient-line reader over a page's text): F48.1's reader, if it wants it.
- Vision over a page (`ScreenshotPage`): its own feature when a measure asks for it.

---
[Projects](../../../src/docs/projects/README.md) · Previous: [louis-agent.pack.shop-prices](louis-agent.pack.shop-prices.md) · Next: [louis-agent.host-packs](../../../demos/puls/docs/projects/louis-agent.host-packs.md)
