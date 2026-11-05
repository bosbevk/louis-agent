# 0065. The view is in the address

**Status:** Accepted · **Date:** 2026-11-03 (F53.1, built unattended at Louis's word; the picks wait for his review
on the pull request) · **Recorded:** 2026-11-03

Numbered 0065 because 0064 was reserved for F52's (*OpenTelemetry*), written in the same days.

## Context

The run page in the admin portal stacked ten blocks, and its explorer kept where you were (the run, its
conversations, one conversation, a turn, a request) in a private field. The address never changed: pressing Back
left the run instead of going up a level, a view couldn't be linked or shared, and a reload lost the place. The
only thing the address carried was `?task=`, applied once when the page opened.

The review of the portal (2026-11-03) named this as one of its five findings, and Louis picked the option that
reorganises the portal around its jobs (F53). Its first child asked where the explorer's place should live.
Weighed: the explorer keeps its own state and **reports every move** to the page, which writes the address and
hands a changed one back (taken); the explorer **navigates itself** and reads its place only from the address (one
copy, but the explorer can't be rendered without a page, and every component test changes); Blazor's `NavigationLock`
and the history API by hand (more code for the same result).

## Decision

- **What a page shows is in its address.** The run page's tab (`?tab=summary|conversations|judge|notes|settings`)
  and, on the conversations tab, the explorer's place (`?view=run|conversations|conversation|turn|request`, with
  `task`, `turn`, `request` as a UTC round-trip time, `mode`, and `review=true` for the filter) are read from the
  address with `SupplyParameterFromQuery`, parsed once (`RunTabs.Parse`, `ExplorerPlace.Of`), and written by one
  class (`RunAddress.Of`), so a tab, a link in a summary and a move in the explorer can't spell it differently. No
  tab, or one we don't have, is the summary; no view is the run; a request time that isn't one opens its turn.
- **A component that keeps state reports it and takes it back.** The explorer keeps its own level and what is open
  (so it still works alone and its tests render it alone), raises `OnMoved` with an `ExplorerPlace` on every move,
  and applies a `Place` parameter when it differs from what it shows. The page owns the address: on a move it
  navigates to the same run with the new query; when the address comes back different (Back, a link, a reload) the
  parameters flow down and the explorer moves. React's "lifting state up", kept partial.
- **A change of address is not a fetch.** The page fetches the run only when the run's id changes; a tab or a view
  is a link to the same run.
- **Nothing kept for the old link** ([ADR 0049](0049-nothing-is-compatible-before-a-sale.md)): `?task=` became
  `?tab=conversations&view=conversation&task=`, and the links that wrote it changed with it.

## Consequences

Back goes up one level, a view can be shared and comes back on reload, and the browser's history is the explorer's
undo. The place is held twice (the address and the explorer's fields), kept equal by one comparison when parameters
arrive. A new page in the portal follows the same shape: its state in the address, one class that writes it, a fetch
only when the thing shown changes.

## Links

F53 (the admin portal, organised by job) and its first child F53.1 (the run page), both on the TODO; the review
that led there is in the Claude project (`plans/admin-revamp/`).

---
[ADRs](README.md) · Previous: [0063](0063-a-prompt-says-what-it-is.md)
