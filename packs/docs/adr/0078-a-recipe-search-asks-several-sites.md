# 0078. A recipe search asks several sites, and the model doesn't pick which

**Status:** Accepted · **Date:** 2026-11-05 (Louis: "almost every recipe comes from, this is not random"; he picked
Scatter-Gather from three options, and the seven findings in the F35.4 plan) · **Recorded:** 2026-11-05

## Context

The cook's prompt said to start with `foodandhome`, because that site publishes clean recipe data, and the tool's
description named it as the example. The model obeyed, and Food & Home has 8 of our 10 dishes, so it rarely needed another:
19 of 21 recipes in the stored runs came from it, and none from the other 44 sites in the table.

A prompt can't fix this for long. Without a name to start from, the model would need the 45 names in every request, and it
tends to settle on a favourite anyway.

## Decision

**With no site named, `FindRecipes` draws the sites in code.** Four at random from the enabled sites with their own search,
asked at once, with their hits merged taking turns and each hit saying whose page it is. The model chooses between hits by
their titles, never between sites by their names.

- **Four**, because the browser opens at most four pages at once, so a search costs about as long as its slowest site. If
  they miss, another search asks another four.
- **Only sites with their own search** are in the draw: a web-searched site uses the web search provider per search, so it
  can still be named but isn't asked by default.
- **A site that refuses is skipped**, and the model only hears about refusals when every site refused.
- **No prompt or guide names a site to start with.** A name there is the one the model would always pick; tests hold this.

The pattern is Scatter-Gather: one request goes to several places and the answers are merged into one.

## Consequences

- Recipes come from many sites. 14 of the 20 sites in the draw give page text rather than clean data (ADR 0067), which
  costs more to read and may lose a clean amount; the measure in the F35 plan says by how much.
- A run varies from one repeat to the next, by design, so a recipes eval needs its repeats to mean anything.
- **The cook and recipes evals run live only** (Louis, 2026-11-05). A replay keys each request on what the model reads,
  and a new draw changes the search's result, so a $0 replay or a studio try of those evals misses at the request after
  the first search. Live pages could already change under a replay; the draw makes it certain. CI grades recorded
  transcripts, so it isn't affected. A seed for eval runs can come later if the studio needs one.
- A rare dish can miss. `pap en sous` and `hoenderpastei` turned up on one site of the 20 in a rough check, about one
  search in five, so the web fallback stays.
- Variety only shows across a run, so it's a number in the measure rather than a check on one conversation.

## Links

- The plan, now deleted: [F35.4](https://github.com/bosbevk/louis-agent/blob/cc45cd85/demos/puls/docs/plans/F35.4-many-recipe-sites.md), with the options not taken.
- [ADR 0067](../../../src/docs/adr/0067-a-pages-own-data-before-its-text.md): a page's own data before its text.
- [Scatter-Gather](https://www.enterpriseintegrationpatterns.com/patterns/messaging/BroadcastAggregate.html).
