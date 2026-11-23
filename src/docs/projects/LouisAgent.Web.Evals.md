# LouisAgent.Web.Evals — design

> **Kind:** Razor library (`net10.0`, runs in the browser) · **Path:** `src/web/LouisAgent.Web.Evals/` ·
> **References:** LouisAgent.Web, LouisAgent.Web.Docs, LouisAgent.Evals.Contracts · **Used by:** LouisAgent.Site, naming `.Evals()` among its areas

![The admin's eval pages: the routes read and every address in one place, the evals' page, and the words its code picks](diagrams/LouisAgent.Web.Evals.svg)

## Purpose

This project is where a person reads an eval's runs, in the admin of a site that runs evals. Its first page lists
the host's evals, one card each, and says how each one's last run went. From a card, a run's page gives that run's
number first and its conversations, the failed ones first. It reads what
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)'s routes send, a run's records as
[LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) holds them, and runs in the browser like the rest of the
admin.

A site takes it with one word among its areas, `.Evals()`, and louis-agent.dev does.

The doc follows a page from its data to what it draws: how a site takes the pages, how the routes are read, the
evals' page and its cards, a run's page, and the words.

## How it works

### 1. A site takes the pages

`EvalsArea` gives a site the eval pages with one word among its areas, `.Evals()`, as the accounts and the docs are
taken. It hands the site's router the library's pages, registers `EvalsApi`, and gives the admin two parts: an
Evals link in the sidebar, `EvalsLinks`, with the frame's tick for its icon, and a card on the admin's home,
`EvalsCard`, that says how many evals the host has and how many of their last runs passed every conversation. The
card reads the same list the evals' page reads. A site that names no evals ships none of it, and the area needs the
site's admin frame, so a site names `.Admin(layout)` before it.

### 2. The routes are read, and every address has one place

`EvalsApi` is a small client over the site's own API client. It reads the host's evals, one run whole, and an
eval's runs, each with `RunJson`'s options, the ones the store wrote a run with, so it is as strict as every reader
of ours. A run the host doesn't have is none, since a page says that in its own words. A refused key or a host that
can't be reached stays the API client's own `AgentApiException`, which every admin page turns into the key box or a
plain line.

`EvalAddress` holds every address the pages use and every route they read, so no page joins one by hand. Each part
is escaped, so a name can't change an address's shape. A repeat is part of a conversation's address, never a `#`, so
a repeat can be linked and the browser's Back works.

![Its classes: the page reads the evals through the client, which reads the routes the addresses name; each eval is a card, and the words come from one place](diagrams/LouisAgent.Web.Evals-classes.svg)

### 3. The evals' page

`EvalsPage`, at `/admin/evals` in the admin's frame, reads the host's evals when it opens and again on Reload, and
nothing polls. Each eval is an `EvalCard`: its name, its agent's address, how many conversations it holds, and its
last run with the number first, "5 of 7 passed", then when it started in the browser's own time, its kind and its
repeats. A last run that could not finish says so, with how many conversations it graded. An eval never run says
"Not run yet." and has no link; every other card links its last run's page.

A host with no eval shows one card, `NoEvalsCard`, that says where an eval file goes. It links the guide on running
an eval through the site's own docs address when the site names one, and names it in words when it doesn't. A
refused key shows the frame's key box.

The cards are Bootstrap's grid, four across on a wide screen and one on a phone. The library's own styles hold only
what Bootstrap doesn't, and every colour is a theme's name, so glass and flat, light and dark all draw it.

![The evals' page: it reads the host's evals and hands each its card, shows the one card that says where a file goes when there is none, and the frame's key box on a refused key](diagrams/LouisAgent.Web.Evals-components-evals.svg)

### 4. A run's page

`RunPage`, at `/admin/evals/{eval}/runs/{run}`, reads one run whole when it opens and again on Reload. A way back to
the evals sits above its heading, which names the eval and when the run started. The number comes first, "0 of 1
passed", in the monospace face. Beside it, `RunFigures` lays out the run's other figures, each a `RunFigure` with
its name over its value: the kind, the repeats, how long it took, the agent's cost, and the facts and criteria met.
A line under them says which agent was asked, and whether its message carried the page a visitor would be on.

Every one of those figures is the run's own answer, asked of its records: the counts are `CountFacts` and
`CountCriteria`, the time is `MeasureTime`, and the cost is `SumCost` in the currency `FindCurrency` names. A cost
with no price, or a time with no end, reads "unknown", never 0.

The conversations follow, in the run's order with the failed ones first. Each is a `ConversationRow`, and the whole
row links to the conversation's own address. It shows a tick or a cross, the conversation's id, its first ask, and
how many of its repeats passed, "2 of 3 repeats". A failed one adds a line in the danger colour saying which repeat
failed first and on what, in that repeat's first miss's own words: "Repeat 2, not met:" and the criterion, or
"fact not met:" and the fact.

### 5. The words

The words the code picks are `EvalsWords`, one entry for each part of `EvalsPart`, as every page's words are
(ADR 0132): a count says "1 repeat" or "3 repeats", a date is said in the browser's own time, and a length of
time in its two largest parts, "33 s", "6 min 12 s" or "1 h 2 min". `MarkDot` is a mark
of a fact or a criterion, a tick or a cross with its words for a screen reader, so a mark is never told by colour
alone.

*Decided in:* F126.3.2, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

```
src/web/LouisAgent.Web.Evals/
├── EvalsArea.cs   # .Evals(), and EvalsLibrary, the assembly the router is handed
├── Admin/         # EvalsLinks and EvalsCard: the sidebar's link and the home's card
├── Client/        # EvalsApi, EvalAddress: the routes read, and every address
├── Pages/         # EvalsPage: /admin/evals; RunPage: /admin/evals/{eval}/runs/{run}
├── Components/    # each part with its styles, in a folder for the page it draws
│   ├── Evals/     # EvalCard, NoEvalsCard: the evals' page
│   ├── Runs/      # RunFigures, RunFigure, ConversationRow: a run's page
│   └── Marks/     # MarkDot: a mark, on every page
└── Words/         # EvalsWords, and Parts/EvalsPart
src/tests/web/LouisAgent.Web.Evals.Tests/   # the client, the addresses, the page and its parts, the words
```

## Configuration

None of its own. It reads with the site's API client and the admin's key, and links the guide through the site's
`DocsAddress` when the site names one.

## Tests

`LouisAgent.Web.Evals.Tests` holds the client over a stub of the API (`EvalsApiTests`), the addresses
(`EvalAddressTests`), the evals' page and a run's page with bUnit (`EvalsPageTests`, `RunPageTests`), a mark (`MarkDotTests`), the words
(`EvalsWordsTests`), the area (`EvalsAreaTests`) and the admin's link and card (`EvalsLinksTests`,
`EvalsCardTests`). louis-agent.dev's own tests hold that its admin draws the evals' link and card.

## Limits and plans

A conversation's page is not built yet, so a row's link opens nothing yet. A run's page draws a finished run; a
run that stopped, one still running and one the host doesn't have have no words of their own yet, and the eval's
other runs are not listed.
It is not a package ([ADR 0130](../adr/0130-the-framework-is-published-as-packages-one-tag-each.md)).

## Related docs

[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md) · [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) ·
[Run an eval](../guides/run-an-eval.md) · [Test projects](tests.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
