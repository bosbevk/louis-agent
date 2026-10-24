# Cook check (`cook`): Puls with the cook's tools against Puls with the cook

## What it measures

What does Puls's cook add? The same conversations are held twice: first by Puls alone with all the cook's tools
(`web` and `shops`), which cooks and looks up prices itself, then by Puls as built, which hands a meal to the cook
(`puls-cook`). Both have the same tools, so the difference is the cook's role and its own session. What is counted is
what the agents *did* (who handed over, which sites were read, how many ingredients were priced), not only what they
wrote. The cost comes from the ledger, not from the script.

## The modes

| Mode | Puls's tools | The cook's tools | What it is |
|---|---|---|---|
| `puls-tools` | `web`, `shops` | — | Puls alone with the cook's tools: can't hand over, cooks and prices itself |
| `puls-cook` | `specialists`, `web` | `web`, `shops` | Puls as built: the web itself, cooking to the cook |

`specialists` is not something the cook uses: it is Puls's ability to hand over. Without it Puls can only cook
itself, which is `puls-tools`. The cook gets `web` from Puls (the recorded turn shows `toolsets: ["web"]`), and its
role adds `shops` (F33, ADR 0025).

Until F34 the first mode was `puls-only`, with only `web`: Puls alone couldn't reach a shop's catalogue, so the
comparison measured the tools, not the cook (Louis: "puls loaded with everything the cook has vs puls + puls-cook").
The new name keeps the portal's trend honest: old runs' `puls-only` stands apart.

## The conversations

Each conversation is one session; a second message is the follow-up.

| Conversation | Where | What is asked | Right is |
|---|---|---|---|
| `cook-af` | Stellenbosch | Dinner for four under R250, recipe and shopping list with prices; then: make it for six | the cook gets it (in `puls-cook`), and the follow-up goes back to the same cook |
| `cook-en` | Pretoria (in the message) | An hour's cooking for two, what it costs at the shops; then: chicken instead of meat | as above, in English |
| `cook-shops` | Stellenbosch | Recipe, each ingredient's price at two shops with links, one shop for everything, the branches, the cheapest basket | the cook gets it, and prices at South African shops |
| `cook-malmesbury` | Malmesbury | R150 for dinner for four: what to make and what it costs at the shop, using specials where there are any (F34) | the cook gets it, prices at the store that serves Malmesbury, and looks for specials |
| `cook-no-shop` | Prince Albert | What to cook tonight for two, the recipe and what it costs at the shop: a town with no shop the cook has a tool for (F34) | the cook gets it, and the list comes without prices, saying no shop near the user has a catalogue here |
| `control-web` | Stellenbosch | The current prime lending rate, with sources | Puls answers itself (it isn't food) |

## The scores

Each conversation gets these scores, under its turns in the run's table and in the explorer:

| Score | Means | ✓ when |
|---|---|---|
| `role` | The role Puls handed the first turn to, or empty if Puls answered itself | — |
| `routed_right` | Whether the first turn was routed right | a meal to `puls-cook` when the cook is on; otherwise, and for the control question, Puls itself; never when the turn failed |
| `answered_itself` | Puls answered itself, without an error | — |
| `hosts` | The sites pages were read from (Puls's and the cook's), in order | — |
| `local_hosts` | Of those, the South African ones (`.za`) | — |
| `shop_hosts` | Of those, the shops (Checkers, Pick n Pay, Woolworths, Spar, Shoprite, Usave) | — |
| `sources_local` | Every site read is South African; a shop search counts as its shop's site, and a map (OpenStreetMap, where the cook looks up a branch) is no source (F34) | yes |
| `priced_at_shops` | At least one shop's page was read, or its catalogue asked with a shop tool (F34) | yes, where there is a Checkers |
| `shop_tool_calls` | How many times the cook used a shop tool (`FindProducts`, `SearchSpecials`, `SearchPromotions`, F34) | more than 0 where there is a Checkers |
| `deal_tool_calls` | Of those, `SearchSpecials` and `SearchPromotions`: searches for specials | more than 0 for every meal with a shop tool: the cook looks for specials first |
| `deals_named` | How many rows on the list name a special beside the price (`· BUY 2 FOR R45`, `SAVE`) | — |
| `untooled_shops` | How many shops the list prices that the cook has no tool for (Louis, F34: only shops with a tool). The portal shows it red | 0 |
| `store_for_area` | How many of the cook's shop searches were answered by the store that serves the user's town: the tool's first line names the store, checked against Checkers's own store id (Louis: "how do we know the address was used?") | more than 0 in Malmesbury |
| `store_default` | How many were answered by the shop's default store: no town, no store there, or the town's store wasn't used | 0 in Malmesbury |
| `no_catalogue_line` | The answer says no shop near the user has a catalogue here yet (the list without prices) | yes in Prince Albert (`cook-no-shop`), no elsewhere |
| `ingredients` | How many ingredients are on the list: the table's rows (the cook's own *Priced: n of m* line, *Geprys: n van m* in Afrikaans, is its claim, not the score) | — |
| `priced` | How many of them have a price **with the link it came from**: only a price with its source counts (F31 step 11b: a price from memory is not an answer). The link must go to that column's shop (Checkers's price to `checkers.co.za`); a recipe's link or another shop's doesn't count, and a reference link (`[R12,99][1]`) is its URL. The row's link to the product at that shop counts too (F34: Puls alone writes `[Onions 1kg](checkers…) \| R34.99`) | — |
| `no_shop_rows` | In a town with no shop the cook has a tool for (`cook-no-shop`, Prince Albert): how many rows are rightly without a price (`-`). They count as right (Louis: "accuracy is 100%, no shop there mos?") | all the rows there |
| `wrong_store` | In that town: how many rows have a price anyway, with or without a link (from a store that isn't the town's, such as Checkers's default store, or a guess). It counts as wrong, never as priced or as unsourced. From the C# cook eval (F18 step 5) on; Python runs counted an unlinked price there as `unsourced` | 0 |
| `priced_ratio` | The right rows over the list's: `priced` plus `no_shop_rows`, divided by `ingredients` | green from 0.8, red below 0.5 |
| `unsourced` | In a town with a shop: how many prices were given without a link, guessed from memory. It never counts as priced; the portal shows it orange (*estimated*). In a town with no shop such a price is `wrong_store`, and `unsourced` is 0 | 0 |
| `per_kg` | How many prices with their link are a price per kg instead of a pack's: the cell says *per kg* (`R99,99/kg`), or it is one amount for something sold by weight (Checkers's link ends in `KG`), where a pack costs a range (`R87,99–R103,99`). It counts as neither priced nor guessed; the portal shows it orange (PR #58's review: the run priced a 1–1.2 kg pack at its per-kg price) | 0 |
| `rand_amounts` | How many rand amounts the answers give | — |
| `unconfirmed_marks` | How often the answers say *unknown*, *estimated* or *search index* (*onbekend*, *geskat*, *soekindeks*): honesty about what isn't confirmed | — |
| `errors` | How many turns failed | 0 |
| `follow_up` | What happened with the follow-up: *continued* (the same cook), *new sub-session* (a new cook) or *answered itself* | *continued* |
| `follow_up_handle` | The cook's handle that continued the follow-up | — |

## What it costs

It spends money: every turn asks Claude. The first paid run (F31 step 11b, `474faef30ff0`, 1 repeat, 12 turns) cost
**$0.0446** (R0.73), 49 requests on Claude Haiku 5.5: `puls-only` $0.013, `puls-cook` $0.032. But that run ran without
web searches (Tavily's credits had run out), so a run with searches will cost more.

## How to read it

- **On the page:** the run's table shows each turn as it finishes; the scores sit under each conversation.
- **In the explorer** (click the run in the list): the cost per mode and per conversation side by side, then a
  conversation's turns on a timeline of model requests, then the turn itself, replayed as it was typed, and one
  request's record.
- Compare `puls-tools` with `puls-cook` on the same conversation: does the cook cost more, and does it price more
  ingredients at local shops?

## By hand

With the Puls containers up, from the repo's root:

```
BENCHMARK_RUN=local PULS_API=http://127.0.0.1:5070/ python3 benchmarks/cook/run_check.py --repeat 1
```

Without the portal the run isn't stored; the ledger still has its cost under the tag `local`.
