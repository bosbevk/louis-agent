# LouisAgent.Web.Evals — design

> **Kind:** Razor library (`net10.0`, runs in the browser) · **Path:** `src/web/LouisAgent.Web.Evals/` ·
> **References:** LouisAgent.Web, LouisAgent.Web.Docs, LouisAgent.Evals.Contracts · **Used by:** its tests

![The admin's eval pages: the routes read and every address in one place, the evals' page, and the words its code picks](diagrams/LouisAgent.Web.Evals.svg)

## Purpose

This project is where a person reads an eval's runs, in the admin of a site that runs evals. Its first page lists
the host's evals, one card each, and says how each one's last run went. It reads what
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)'s routes send, a run's records as
[LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) holds them, and runs in the browser like the rest of the
admin.

No site names the library among its areas yet, so the page is not yet reachable in a site.

The doc follows a page from its data to what it draws: how the routes are read, the evals' page and its cards, and
the words.

## How it works

### 1. The routes are read, and every address has one place

`EvalsApi` is a small client over the site's own API client. It reads the host's evals, one run whole, and an
eval's runs, each with `RunJson`'s options, the ones the store wrote a run with, so it is as strict as every reader
of ours. A run the host doesn't have is none, since a page says that in its own words. A refused key or a host that
can't be reached stays the API client's own `AgentApiException`, which every admin page turns into the key box or a
plain line.

`EvalAddress` holds every address the pages use and every route they read, so no page joins one by hand. Each part
is escaped, so a name can't change an address's shape. A repeat is part of a conversation's address, never a `#`, so
a repeat can be linked and the browser's Back works.

![Its classes: the page reads the evals through the client, which reads the routes the addresses name; each eval is a card, and the words come from one place](diagrams/LouisAgent.Web.Evals-classes.svg)

### 2. The evals' page

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

### 3. The words

The words the code picks are `EvalsWords`, one entry for each part of `EvalsPart`, as every page's words are
(ADR 0132): a count says "1 repeat" or "3 repeats", and a time is said in the browser's own time. `MarkDot` is a mark
of a fact or a criterion, a tick or a cross with its words for a screen reader, so a mark is never told by colour
alone.

*Decided in:* F126.3.2, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

```
src/web/LouisAgent.Web.Evals/
├── Client/        # EvalsApi, EvalAddress: the routes read, and every address
├── Pages/         # EvalsPage: /admin/evals
├── Components/    # EvalCard, NoEvalsCard, MarkDot, each with its styles
└── Words/         # EvalsWords, and Parts/EvalsPart
src/tests/web/LouisAgent.Web.Evals.Tests/   # the client, the addresses, the page and its parts, the words
```

## Configuration

None of its own. It reads with the site's API client and the admin's key, and links the guide through the site's
`DocsAddress` when the site names one.

## Tests

`LouisAgent.Web.Evals.Tests` holds the client over a stub of the API (`EvalsApiTests`), the addresses
(`EvalAddressTests`), the evals' page with bUnit (`EvalsPageTests`), a mark (`MarkDotTests`) and the words
(`EvalsWordsTests`).

## Limits and plans

A run's page and a conversation's page are not built yet, and no site names the library, so nothing links here.
It is not a package ([ADR 0130](../adr/0130-the-framework-is-published-as-packages-one-tag-each.md)).

## Related docs

[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md) · [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) ·
[Run an eval](../guides/run-an-eval.md) · [Test projects](tests.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
