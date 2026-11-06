# 0083. Packs keep reports behind the admin key

**Status:** Replaced by [ADR 0086](0086-a-packs-screens-are-described.md) for how the numbers reach the page; what it decided about counting stands · **Date:** 2026-11-05 (Louis: "the recipes is not saving?", "so how would we know if our source is
working?"; picked *one small record per day* and *a new plug-in behind the admin key*) · **Recorded:** 2026-11-06

## Context

Nobody could tell whether the recipe library was working without running an eval. The log lines from F35.3 show events one
at a time, Docker's logs don't outlive the container, and nothing kept a total. The numbers had to reach the admin page, and
the API may not name a pack ([ADR 0027](0027-engine-identities-and-packs.md)). Two questions were weighed.

How to count:

- **One record per day, added to as things happen** (MongoDB's Computed Pattern): a little work on each write, and reading
  is a short list.
- **One row per event:** it grows forever, and has to be added up on every read.
- **.NET's metrics counters:** the app doesn't store their values, so they wouldn't outlive a restart here.

How the numbers reach the page:

- **A new plug-in behind the admin key**, the shape [ADR 0071](0071-packs-take-data-uploads.md) gave uploads.
- **Another open feed** ([ADR 0070](0070-packs-publish-data-feeds.md)): anyone may read a feed, and Puls's endpoint is sold,
  so its usage numbers don't belong there.
- **A route only Puls knows about:** it would be the first of its kind, and no other demo could use it.

## Decision

*A pack can list reports: numbers it keeps about itself, as a table, read behind the admin key. What it counts stays the
pack's, and a report never throws.*

- **`IDataReport`** in the composition (`Packs/Reports/`): a `Name`, a `Description` and `ReadAsync`, which answers a
  `ReportTable` of column names and rows of a label and counts. `IAgentPack.Reports` is empty unless a pack lists some, and
  `AgentCatalog` gathers them and refuses a name twice.
- **`GET /v1/admin/reports`** hands back each report's table, read together in the catalogue's order, and names no pack.
- **A general table, not a shape per report,** so a new report needs no change to the API or a page. It can't show more than
  a table.
- **A store that's down is an empty table,** logged. A report that broke that promise would fail the whole list.
- **Counting is a small write beside the work**, awaited inside the pack's own guard: a failed count only logs, and a trial
  that leaves a store alone leaves its count alone. If the database is down, a count can cost its call the timeout.
- **Days are UTC** ([ADR 0046](0046-dates-are-utc.md)), and the report says so.

The recipes pack's report is `recipe-library`: *Kept*, *Offered*, *Served*, *Failed* per day in `library_usage`, a line
adding up every day kept, then the last 30. It's listed only with a database.

## Consequences

- Whether the library is working can be read on the admin page, with no eval and no log search.
- It counts what the pack sees. Whether an answer used a recipe stays with an eval's saved events, and someone reading the
  open feed directly isn't counted.
- *Served* means `ReadRecipe` from our library; `ScaleRecipe`'s look at a recipe already read isn't counted again.
- A run with `RECIPE_LIBRARY_IN_EVALS=true` on the real database adds to the real totals. A run that measures should have
  its own database.
- `ReportCard` is generic, so it lives in `louis-agent.web`. The page is still Puls's, as it uses Puls's `AdminGate`.
  Customising a report's look, a pack's own view that a demo can override, is the next feature in the [TODO](../../../TODO.md).

## Links

[ADR 0027](0027-engine-identities-and-packs.md) · [ADR 0046](0046-dates-are-utc.md) ·
[ADR 0070](0070-packs-publish-data-feeds.md) · [ADR 0071](0071-packs-take-data-uploads.md) ·
[ADR 0082](../../../packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md) ·
[The Computed Pattern](https://www.mongodb.com/blog/post/building-with-patterns-the-computed-pattern) ·
[Plugin (Fowler)](https://martinfowler.com/eaaCatalog/plugin.html) ·
[Write-up 17](../learning/17-observability.md#the-librarys-counter-f357-2026-11-06)
