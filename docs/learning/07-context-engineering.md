# 7 · Context engineering: what F32 to F34 taught

> **Skill:** #7 in the [goal](../GOAL.md)'s map, with #12 (evaluation), which measured it · **Stages:** F32
> self-hosted web, F33 shop catalogues, F34 shop deals (2026-10-17 to 2026-10-18) · **Written by** Claude, for Louis to
> read (2026-10-19) · **Next:** a JSON-LD reader for any page's data (F35), and the context-building step per turn (F9)

What this answers: why the cook went from pricing nothing with a source to pricing every row it could, and what that
says about what to put in front of a model.

## Before and after

**Before,** the cook was handed the web: search results and whole pages, read by the model, which was asked to find a
price in them. **After,** it is handed the shop's own data: one tool call per ingredient returns a short table (product,
pack, pack price, price per kg, deal, photo, link), read from the shop's catalogue in code.

The cook check (F31) scored each run from its transcript: a price counts only with its source.

| Run | What the cook had | Priced with a source | Cost | Requests |
|---|---|---|---|---|
| F32 `663d065ccca8` | `WebSearch` (SearXNG, Bing only), pages through a headless Chrome | **0 of 32** (0/14, 0/11, 0/7) | $0.057 | 66 |
| F33 `fc7f8ecfdc87` | + `FindProducts("checkers", …)` | **19 of 23**, 3 more at a price per kg (re-scored) | $0.068 | 61 |
| F34 `d94597a51d4d` | + the town's store, deals, the shop tools only | **31 of 31 rows right**: 26 priced, Prince Albert's 5 rightly unpriced | $0.118 | 71 |
| `main` before CL15 `aeb645d53300` (2026-10-19) | the same, the baseline for the cut | **32 of 32 rows right**: 25 priced, Prince Albert's 7 rightly unpriced | $0.112 | 75 |
| `main` before CL15 `14c0907fdff5` (2026-10-19) | the same, a second baseline | **29 of 29 rows right**: 23 priced, Prince Albert's 6 rightly unpriced | $0.108 | 68 |
| CL15 branch `b4a6ea22b178` (2026-10-20, after the engine, identities and packs cut and the layout reviews) | the same, through the shops pack | by the stored scores, 55 ingredient rows: 40 priced, Prince Albert's 7 rightly unpriced, 0 unsourced; the baseline `aeb645d53300` by the same sums is 48 rows: 38 priced, 7 rightly unpriced, 3 unsourced. Nothing new in the cook: in all three runs it looked Prince Albert up on Nominatim (nothing came back), searched the default store twice and priced nothing (`no_shop_rows` 7). The no-shop town's `wrong_store` 6 and `store_default` 7 are Puls alone (`puls-tools`), which looks no town up and prices at the default store in every run (`wrong_store` 5 in `14c0907fdff5`; `store_default` 6 scored as `unsourced` 3 in `aeb645d53300`), and the English cook priced two of four rows per kilo. Read against the stored events, 2026-10-20 | $0.124 | 80 |

The same check ran Puls without the cook, as the control. With only `web` it priced 0 of 3 with a source after F33,
so the gain was the tool, not the model. Given the cook's own tools (`puls-tools`, F34) it priced 13 of 18: the tools
are most of the gain, and the cook's rules (no store, no prices; deals first) are the rest.

## What the numbers say

- **The data was there all along.** Checkers' search page fetched its products from its own catalogue
  (`/api/catalogue/get-products-filter`) as JSON with exact prices. The model never saw that JSON: it saw what a page
  became as text, or nothing. On Pick n Pay's search page `FetchUrl` read 0 characters; the Chrome renderer read 4,345
  with 37 rand prices, and the model still had to guess which price was which product's.
- **A small, exact context beats a large, fuzzy one.** A table of ten products is a few hundred tokens and every
  number in it is right. A rendered page is thousands of tokens of menus, banners and prices that belong to other
  products. The model is good at choosing from a table and bad at pulling the one right number out of a page.
- **Better context cost more, and that was the right trade.** F34's run cost about twice F32's ($0.118 against $0.057)
  because the cook did more: it looked up the town's store, searched deals first, and priced every row. $0.057 for no
  usable answer is the expensive run.
- **The prompt can be the bottleneck.** F32's zero wasn't the renderer's fault. The cook's prompt forbade the shops'
  search pages, a rule written when Tavily Extract couldn't read them. A rule about one tool's failure outlived the
  tool. Context is the prompt as well as the data, and a stale rule in it costs as much as missing data.

## What changed, and why this way

- **F32: search and pages without a key.** SearXNG behind `IWebSearchProvider` and a headless Chrome behind
  `IPageRenderer`, each one more implementation of a seam we had, so the model's tools didn't change. The browser
  checks every request a page makes with the same address rule as `FetchUrl`, not only the first URL. This made the
  run possible; it didn't make it good. Its value is the baseline: 0 of 32, with the reasons found.
- **F33: read the shop's data in code.** `CheckersCatalogue` visits the search page through the guarded browser, like
  any shopper, and keeps the catalogue answer the page itself fetched. Code turns it into rows: in stock only, the price
  now (a special's when lower), the pack's weight, a link to the product. The model gets a table framed as untrusted
  shop data. **Why not call the catalogue's API directly:** it isn't published, so visiting the page as a shopper is
  the honest line (F33's design notes); F34 kept that line when it chose not to use the site's own `dealsOnly` filter.
- **F33: give the model the right number, not a number it must convert.** The table first showed a weighed pack at its
  price per kg, and the cook added R99.99/kg to its totals as if it were a pack's price. Now `ShopProduct` carries
  `Price` and `PriceTo` (the lightest and heaviest pack, computed in code) and the column is *Pack price*. Arithmetic
  the model would get wrong is done before the text reaches it.
- **F34: the context depends on where the user is.** Checkers keeps a town's store in cookies. `CheckersStore` finds
  the stores serving the town (Nominatim gives its centre), keeps their cookies in a browser context of their own, and
  every search runs in them. Each table's header names the store that answered, so the model, the user and the scorer
  all see which store a price came from.
- **F34: fewer, better tools in the cook's hands.** The cook lost `WebSearch` for prices and the two shops picked at
  random; it prices a shop only through a shop tool, and says *No shop near you has a catalogue here yet* when there is
  none. Taking a tool away was the change that stopped the guessing.
- **The tool comes with its role** (F33 step 4, [ADR 0025](../adr/0025-a-role-declares-its-toolsets.md)). The cook's
  file declares `toolsets: shops?`, so its shop tools load wherever the instance has them, without Puls having to
  remember to pass them.

## The measuring (skill 12)

- **A score is only as good as what it counts.** F33's first score was 22 of 23. PR #58's review read the answers and
  found two of those were prices per kg, not pack prices. The scorer gained a `per_kg` count, the stored answers were
  re-scored for free, and the honest number was 19 of 23. A check that counts "a price with a link" rewards a wrong
  price with a link.
- **Compare like with like.** Until F34 the control (Puls alone) had only `web`, so the check compared tools, not the
  cook. With `puls-tools` both arms have the same tools, and the difference left is the cook's rules.
- **A right answer can be "no price".** In Prince Albert, where no shop has a catalogue, the right answer is the rows
  unpriced with the no-catalogue line. The accuracy ring first counted those as unknown; once it knew there's no shop
  there, the run scored 31 of 31. A grader has to know what right looks like in every case, including the empty one.
- **Cost comes from the ledger,** never from a grader: every run above is priced from its own records.

## Trade-offs accepted

- **One shop.** Only Checkers has a provider. Pick n Pay, Shoprite and Woolworths wait for *Sources as data* (Puls's
  track). A user in a town without Checkers gets a recipe and no prices, and a polite line saying why.
- **A site's terms.** Reading a shop's site by automation is governed by its terms
  ([ADR 0023](../adr/0023-copyleft-service-beside-us.md) for SearXNG; F33's design notes for the shops). The shops
  toolset is off unless `SHOP_CATALOGUES` lists a shop, so the sold endpoint doesn't read shops until that's checked.
- **Coupled to a page we don't own.** If Checkers changes its catalogue's JSON, the provider breaks. The tests run
  against a recorded answer, and the error says what is missing, but the fix is ours to make each time.
- **Slower answers.** The cook took 80 to 155 s a meal in the last run. Parallel tool calls for read-only toolsets
  ([ADR 0026](../adr/0026-parallel-tool-calls-for-read-only-toolsets.md)) and a day's cache of the town's store
  brought it down from up to 350 s.

## What didn't work (and what it taught)

- **More text didn't help.** Rendering pages fully (F32) gave the model more characters and no more sourced prices.
- **Search engines that aren't ours.** Through SearXNG only Bing answered from Louis's IP; Google and Qwant denied,
  Brave limited, DuckDuckGo asked for a CAPTCHA. Bing ignores `site:`, and a freshness filter left no engine at all.
  Search is a dependency, so a run says which engine it used.
- **The shop's own limit.** About 50 searches in seven minutes and Checkers refused every search for four minutes, which
  sank one run's cook-af to 1 of 8. Two searches at once per shop and a 20 s pause after a refusal fixed it; the next
  run saw 8 refusals and no block. Parallel calls made the cook faster and the shop angrier until it was throttled.
- **OpenStreetMap's town boundary.** Malmesbury's Checkers is mapped just outside the town, so "supermarkets in
  Malmesbury" never listed it and the cook never priced there. The cook now looks a covered shop up by name.
- **Deals came last.** In Louis's own chats the cook searched specials once, after the list was priced, so a deal could
  never change the meal. Order matters in a prompt as in code: deals are searched first now.

## Where skill 7 stands

GOAL.md's row lists the catalogue table under *Have today* (F33). Still to come: a JSON-LD reader so any page's structured
data is read in code (F35, the same lesson for every site that publishes it), each model's window and price step as the
limits (F15, Phase 9), and long chats: clearing stale tool results, summarising, and a context-building step per turn
that retrieves, ranks and budgets (F9).

## What I can now explain

- Why handing a model the data, shaped by code, beats handing it the page and asking it to find the data.
- Why a number the model must convert (per kg to per pack) is a bug in the context, not in the model.
- Why a stale rule in a prompt can zero a run, and how a free experiment found it.
- Why taking a tool away can raise accuracy.
- Why a check needs a control with the same tools, a count for each way of being wrong, and a right answer for the
  empty case, and why re-scoring stored answers is free.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md)
