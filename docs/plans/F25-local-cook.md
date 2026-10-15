# F25 · Local cook: plan

> Approved 2026-10-15 · Feature: [F25 Local cook](../features/F25-local-cook.md) · Branch: `claude/project-thread-381vmw`
> (the Claude project's branch, where the role was written; keeps its name)

## Context

Louis wrote the cook's role in the Claude project (commit 1acc861: `puls-cook.md` and its tests) and asked for a
benchmark that shows two things: Puls routes a meal question to it, and the cook uses local sources. Neither holds yet:
nothing names the cook in Puls's tables, and the delegation check records Puls's tool calls but not the specialist's.

## Design

What changes from the feature doc: nothing; the doc was written with this plan.

- **Reuse before building:** the check is the delegation check's functions (`ask`, `ledger`, `summarise`) with a
  different question list and one more thing read from the stream. Considered: a mode flag in `run_check.py` (would
  mix two question lists and two reports in one script) and a new harness (F18 isn't built; too much for three
  questions). Chosen: a sibling script, `samples/puls-cook-check/run_check.py`, importing the delegation check's helpers.
- **The specialist's tool calls** come from the API's `sub_session` events, which carry the specialist's stream
  (`content_block_start` with `tool_use`), so no API change is needed.
- **Scoring is code, not a judge:** role equals `puls-cook`; every fetched host ends in a known shop domain
  (`checkers.co.za`, `pnp.co.za`, `woolworths.co.za`, `spar.co.za`, `shoprite.co.za`); the answer has `R` amounts; an
  "unknown" count. An LLM judge is F18's.
- **Questions:** two meals (Afrikaans with a town, English with a town and a time limit) and one control web question
  (the prime rate, from the delegation check), so the researcher's routing is checked in the same run.
- **Modes:** `delegated` (Puls as built: `agents`) and `puls-web` (`agents` and `web`, Louis: "we can also include web in
  puls cause it will save on cost"). The second may answer the meal itself instead of delegating; the run says.

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; TODO line; index rows | — |
| 1 | The two routing rows; `AMealToCook_GoesToTheCook_WithWeb` | unit tests; golden file regenerated |
| 2 | `samples/puls-cook-check/run_check.py`: the questions, the specialist's calls from the stream, the score per answer, two modes, `--report` | `--report` on a saved run (no model calls) |
| 3 | The run (3 questions × 2 modes × 3 repeats, ~$0.50), its README under `samples/sample-output-puls-cook/`, a section in BENCHMARKS.md, the feature's *Done when* | **paid: Louis's go first** |
| 4 | What the run says: fix the role or the rows if routing or sourcing fails; else *Finishing a feature* | unit tests; golden file |

## Verification

`dotnet test tests/louis-agent.core.tests` per step and the golden diff reviewed; the check's `--report` on a saved run
is free; step 3 spends money and is run only on Louis's go, with the Puls containers up on this branch (the Skills
folder is mounted, so the role and the rows need no rebuild).

## Concepts

- **Routing by prompt** (GOAL #10, multi-agent orchestration): a specialist exists for the orchestrator only once its
  instructions name it; two roles can share a toolset and differ by the kind of request.
- **Grounding on live local data** (GOAL #8): a role that must fetch the shop's page rather than answer from training,
  and say "unknown" where it can't.
- **Trace-based evaluation** (GOAL #12): judging an agent by what it called (the role, the hosts) and not only by its
  answer, with code as the scorer before any LLM judge.

## Approved during the build

- **2026-10-15, the follow-up and the Werk card** came through F26 (its plan has them).
- **2026-10-15, every cooking question is the cook's, and it says where to buy** (Louis: "any cooking stuff must
  always use cook, because cook knows to also look for prices, also update the cook to also show places where to
  buy"): step 4, the prompt only: the router's opening and its cook row, the agents guide's row, and the role's *Where
  to buy* (the shops' branches in the user's town, never invented). Not measured again: Louis tests by hand.
- **2026-10-15, branches from OpenStreetMap, and no fetching of JavaScript pages** (Louis's second test: "it did not
  listen to include prices and where i can buy it, actual shops"; the log showed the cook following the role and the
  shops' search pages and store finders returning nothing): step 5, the role only. Branches come from Nominatim's
  JSON search through `FetchUrl` (free, no key; F22 already uses Nominatim), checked from the container; the role
  says which shop pages not to fetch, and to stop searching after two refusals. Prices still need a search engine that
  isn't refused: Louis's decision (the TODO).
- **2026-10-15, Tavily for search** (Louis: "use tavily", from the three options: Tavily, Serper.dev, and not Google or
  Brave): step 6. A `TavilySearchProvider` beside Google's and DuckDuckGo's (`POST https://api.tavily.com/search`, the
  key as a bearer header, `include_domains` for `site`, `time_range` for `freshness`), chosen in
  `WebSearchProviderFactory`: an explicit `WEB_SEARCH_PROVIDER` wins, else a `TAVILY_API_KEY` picks Tavily, else
  Google's keys, else DuckDuckGo. No package: one HTTP endpoint. Tests on a stubbed client; the key never in a body.
- **2026-10-15, Tavily Extract as a tool of its own** (Louis: "use tavily extract, but call the tool something else,
  keep the web search", from three options: Tavily Extract, Jina Reader, Playwright in the container): step 7.
  `ReadRenderedPage(url, maxChars, startIndex)` in the web toolset, beside `FetchUrl`, which stays as it is: an
  `IPageRenderer` with `TavilyPageRenderer` (`POST https://api.tavily.com/extract`, `extract_depth: advanced`, the
  same key) and an unavailable one that says what to set, picked in `PageRendererFactory`. The SSRF guard runs before
  the URL leaves for Tavily. The guide and the cook's role say: only for a product page `FetchUrl` read as empty.
- **2026-10-15, the report goes through as the cook wrote it, by prompt** (Louis: "force Puls to output the data as
  the cook said it, and conform the cook's output"; chosen over an engine change that would stream the sub-session's
  answer as the turn's): three steps. (1) The cook's report in one fixed shape: six headings in a fixed order, the
  shopping list and the branches as tables with fixed columns, a price only from a shop's page or its search result
  with the source beside it, the `Geprys:` line, and the role's closing line says Puls hands the report on as it is.
  (2) Puls relays a report instead of rewriting it: a *Relaying a report* rule in `agents-skills.md` and a line in
  `default.md`. (3) The check scores both: `relayed` (the cook's tally line and every list row are in Puls's answer,
  from the specialist's text the check now captures) and the rating read from the table's source column. Every step
  has its prompt test first; the golden request snapshot is regenerated with each.
- **2026-10-15, a benchmark of what the cook adds** (Louis: "generate me a benchmark for puls which between only
  -puls and -puls -puls-cook"; the id and build-only his choices): the cook check gets a `--personalities` switch
  that runs the same conversations in two modes, `puls-only` (`web` only: Puls can't delegate, so it cooks itself)
  and `puls-cook` (`agents` and `web`). The score counts Puls's own fetches too (in `puls-only` it fetches itself),
  the expected role follows the toolsets, and Louis's Stellenbosch question (two shops, links, branches, the cheapest
  basket) is a fourth conversation. In the portal too (Louis: "must be here mos"): a check carries arguments of its
  own, and *Puls alleen teen Puls met die kok* (`puls-alone`) runs the cook check's script with `--personalities`.
  First built as a restart with `PERSONALITIES_ENABLED=puls`; dropped because the portal runs checks inside the
  container that would restart (Louis chose toolsets over a restart or a runtime switch). Checked with `--report` on
  saved runs; the paid run is Louis's to start.
