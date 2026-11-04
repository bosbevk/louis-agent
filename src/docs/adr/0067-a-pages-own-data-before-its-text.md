# 0067. A page's own data before its text: a tool reads the structured data in code

**Status:** Accepted · **Date:** 2026-11-03 (decided, F35 plan approved on the recommended picks) · **Recorded:** 2026-11-04

## Context

Twice now, reading a site's data in code beat letting the model read its pages. The shops measure
([the shops pack](../../../packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md), scrapped in F50.2) got 0 of 32 prices right when the cook read the
pages, and 31 of 31 with the user's store. Recipe pages are the
same story: a page can publish its recipe for machines as a schema.org block, a JSON-LD script hidden in its HTML,
beside the text people read. When the model reads prose instead, it spends more tokens, gets amounts wrong, and leaves
nothing that code can check. The F35 plan ([plan](../../../demos/puls/docs/plans/F35-recipe-sources.md)) needs the cook to get a whole,
exact recipe from the site that published it, so the amounts are the page's and not the model's memory.

## Decision

We read a page's own structured data in code whenever the page publishes what we need that way (JSON-LD, a feed, a
catalogue answer). A tool turns that data into our own record and hands the model a short print of it, framed as
untrusted data ([ADR 0005](0005-guard-the-context.md)). The model reads the page's text only where the page has no such
data, and the tool's answer then says so. The reader that finds the data is infrastructure in core, shared by every
pack. The record belongs to the pack that uses it.

## Consequences

- Fewer tokens per answer, and exact amounts: the cook quotes the page's lines, it doesn't rebuild them.
- A record that a check can grade against its source, and that a later feature (F48) can compute on.
- One more reader to keep up when a site changes how it publishes its data.
- A site without the data falls back to its text, and the answer says that the text is all there is.
- The shape of each site's data is checked once by hand and written down in the plan, not assumed.

## Links

The F35 plan, [F35-recipe-sources.md](../../../demos/puls/docs/plans/F35-recipe-sources.md), and its steps; the TODO's F35 line
([TODO](../../../TODO.md)). The robots policy that every fetch obeys is ADR 0068, written in F35.1's step 2. Also
[ADR 0003](0003-provider-strategies.md) (one interface, one implementation per provider) and
[ADR 0055](0055-keep-it-simple.md) (nothing for a future nobody planned).

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
