---
name: shop-prices
description: The shop-prices toolset's guide, loaded with it. How to estimate what a shopping list costs with EstimatePrices, from Stats SA's average prices.
---
# Shop prices Skills

How to estimate what a shopping list costs. The prices are Stats SA's average prices (consumer price index, one month,
per province), with a few rows of our own for what Stats SA doesn't price. They are averages over many shops, never
one shop's price. Route here a request for roughly what something costs.

## Tools

| Tool | Use it to |
|------|-----------|
| `EstimatePrices` | Estimate a list (`items`, one line per item in English with its pack: `beef mince 500 g`, `eggs 18`, `onions 1 kg x 2`), in the user's `province` (`Western Cape`; unset is the national average), for `serves` people. Returns a table: each line, what it was priced from, its estimate (`± R62`), the total and the price per person, worked out in code, and the line that credits Stats SA. |

## Workflow

1. **Write the list first,** every item the user buys with its pack size, and only then call `EstimatePrices` once
   with all of it. Leave out what isn't bought (tap water, the pantry).
2. **Give the province** from the user's place, or from `FindBranches` when that toolset is loaded: a province's
   averages are closer than the country's.
3. **Copy the numbers as they come:** each line's estimate, the total and the price per person. Never add, round or
   scale them yourself.
4. **A line with `?`** has no estimate: try it once more with the size the table names (the result says which), else
   keep the `?`, leave it out of the total and say so.
5. **Credit the source** with the line the result ends on.

## Rules

- Every price is an estimate (`± R65`), never a quote and never linked: no shop's prices are read.
- Never invent a price for a line that came back with `?`.
