# F25 · Local cook: Puls's first local specialist

> **Status:** done (built 2026-10-15, on `claude/project-thread-381vmw`; closed 2026-10-19 by [the platform plan](../plans/platform.md)); the search provider was measured with Tavily (2026-10-15) · **Milestone:** — · **Depends on:** F21 (roles), F22 (`GetToday`, the user's place, `ConvertCurrency`), F5 (sub-sessions)
>
> **In the TODO:** done, so its line is gone; its open item, **The cook's unconfirmed prices** (*geskat* or
> *unknown*), is under *Puls* ([TODO](../../TODO.md)) · **Plan:** [plans/F25](../plans/F25-local-cook.md)

The end product is a general agent that knows where its users are and looks up what changes instead of relying on
training ([TODO](../../TODO.md), *South African general AI*). Puls's specialists so far are generic: a researcher, a
reader, a historian, an analyst. The cook is the first **local** one, Louis's idea: one question ("what should I cook?")
gets an answer for where the user is: a South African dish, a shopping list priced in rand at the shops near them, and
a total, so they can drive to the shop and buy it. It is also the first test of whether a specialist's role can make it
use local sources, and whether Puls routes a kind of question to a specialist that shares a toolset with another.

## User stories

**F25-S1 — A meal for today, where I am.** As a *user*, I want to ask Puls what to cook tonight and get a dish that
fits (people, time, budget, season), the recipe, and a shopping list with the shop and the price of each ingredient,
so that I can go and buy it without asking again.
- *Given* "Wat kan ek vanaand kook?" with my town set *then* the answer names a dish, the recipe, each ingredient's
  shop and price in rand, and the total.
- *Given* an ingredient the cook couldn't price *then* it is marked unknown and the total says how many it leaves out;
  no price is invented.
- *Given* no town *then* the answer is for South Africa in general and says so.

**F25-S2 — Puls hands it to the cook.** As *Puls*, I want my router to name the cook for a meal question, so that it
goes to the role that knows the shops, not to the researcher.
- *Given* a meal question *when* Puls delegates *then* `StartSubSession` carries `web` and `puls-cook`, and the task
  names the user's town.
- *Given* a plain web question (the prime rate) *then* it still goes to `puls-researcher`.

**F25-S3 — Measured, not assumed.** As a *developer*, I want a check that shows which role Puls chose and which sites
the cook read, so that "routes properly" and "uses local sources" are numbers from a run, not a reading of the prompt.
- *Given* the cook check *then* each question records the role Puls passed, the hosts the specialist fetched, whether
  the prices are in rand and whether any ingredient is marked unknown, with the cost from the ledger.

## Design notes

- **The role** is `Skills/personalities/puls-cook.md`, on the shared `puls-*` base (built in the Claude project): South
  African first, buyable today at Checkers, Pick n Pay, Woolworths, Spar or Shoprite, every price from the shop's own
  page, never invented. Its tools are the web toolset's (`WebSearch`, `FetchUrl`) and `GetToday`, which every engine
  has (F22).
- **Routing** is two table rows: `default.md` (the request → `web` + `puls-cook`) and `agents-skills.md` (the role's
  job, and "put their town in the task"). Nothing in code changes: a role exists for Puls once its tables name it (F21).
  `roles.json` is unchanged: a web sub-session with no role is still the researcher; the cook is always passed.
- **The check** reuses the delegation check (`samples/puls-check/run_check.py`): its sessions, streaming and ledger
  reading. New: the specialist's own tool calls, read from the `sub_session` events the API already streams, so the
  cook's `FetchUrl` hosts are recorded; and a score per answer (role, local hosts, rand, unknown rather than invented).
  Two modes: Puls as built (`agents` only) and, at Louis's request, Puls with `web` itself, to see what it saves on
  plain web questions and whether it still hands a meal to the cook.

## Done when

- **F25-S2:** both routers name the cook with `web`. *Met:* `AMealToCook_GoesToTheCook_WithWeb` (both files), the
  golden file showing only the new row.
- **F25-S1 and F25-S3:** in the cook check, every meal question goes to `puls-cook`, the control question to the
  researcher, every price the cook reports comes from a South African shop's page it fetched, and unpriced ingredients
  are marked unknown. *Met* in the [three-run cook check](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-cook/README.md): 6 of 6 meals
  to the cook, 3 of 3 controls to the researcher, every meal priced at Checkers and Pick n Pay (and others), confirmed
  prices from product pages only. *Open:* the cook marks an unconfirmed price *geskat* (an estimate) rather than
  *unknown*, and with DuckDuckGo refusing 41 of 48 searches most prices were estimates; a search provider that isn't
  refused is needed before the shopping list is one the user can drive to the shop with.
- Suites pass; the run's README is in `samples/` and its numbers in [BENCHMARKS.md](https://github.com/bosbevk/louis-agent/blob/pre-cl10/docs/BENCHMARKS.md#the-cook-check-2026-10-15). *Met.*

## What the build changed

- **F26 came first.** Louis asked (2026-10-15) that a follow-up on the meal go to the same cook; checked, it couldn't
  (every sub-session was deleted after its answer), so [F26 Specialist follow-up](F26-specialist-follow-up.md) was
  built on this branch before the check, and the check asks a follow-up in each meal conversation.
- **"Local sources" is scored in two parts:** every fetched host South African (`.za`, so a recipe site counts), and
  at least one known shop among them (where the prices must come from). The plan's single "every host a shop" would have
  failed a cook that read a recipe page.
- **The check scores calls, not prose:** the role from Puls's `StartSubSession` arguments, the hosts from the
  specialist's `FetchUrl` calls in the API's `sub_session` events, the follow-up from a `ContinueSubSession` call with
  the first answer's handle. Rand amounts and "unknown" marks are the only things read from the answer.
- **Every cooking question is the cook's, and it says where to buy** (Louis, 2026-10-15, from a manual test: "ek wil
  bebotie maak, wat het ek nodig" was answered by Puls from memory). The router's opening said knowledge questions need
  no tool, and the cook's row spoke of "what to cook today"; both now say cooking is never answered from memory. The
  role takes a named dish through the whole report and adds *Where to buy*: the shops' branches in the user's town.
- **Branches come from OpenStreetMap, not the shops.** The shops' store finders, search and category pages draw their
  content with JavaScript and gave the cook nothing (Checkers' locator is a 404, Pick n Pay's and Woolworths' search
  endpoints return their app shell); Nominatim's search returns the branches with their streets as JSON, which
  `FetchUrl` reads, so the role names it. Prices still depend on a search engine finding the product page, which is
  the only shop page that gives one.
- **Tavily is the search provider** (Louis, 2026-10-15, from three options): `TavilySearchProvider`, chosen by the
  factory when `TAVILY_API_KEY` is set and no provider is named; 1,000 free searches a month, each result carrying the
  page's relevant text. The cook's refused searches were DuckDuckGo's doing; Google's Custom Search API, the other
  configured option, ends on 2027-01-01.
- **A rendered page is its own tool** (Louis, 2026-10-15): with Tavily searching, the cook read prices from Checkers'
  product pages, but Pick n Pay's came back "(no readable text)", a JavaScript app. `ReadRenderedPage` renders a page
  through Tavily Extract (advanced depth, two credits per five pages); `FetchUrl` is unchanged, and the role says to
  render only a product page that read as empty, since each render costs a credit.
- **The town travels two ways:** on the session (`location`, as the web app sends it) for the Afrikaans conversation,
  in the message for the English one; the cook's role covers both.
- **What the cook adds is measured by turning it off** (Louis, 2026-10-15): `--personalities` runs the conversations
  with Puls on `web` only (it can't delegate) and on `agents` and `web`; the portal runs it as *Puls alleen teen Puls
  met die kok*. Not with `PERSONALITIES_ENABLED`: that list is read at start-up, so switching it restarts the
  container the portal runs checks in. Puls's own `FetchUrl` calls are now scored as hosts too: without the cook it
  reads the shops itself, and before, those fetches went uncounted.

---
[Features](README.md) · [Plan](../plans/F25-local-cook.md)
