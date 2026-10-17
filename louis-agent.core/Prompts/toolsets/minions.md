# Minions Skills

How to hand one small task to a minion with `StartMinion`. A minion is a worker on a cheap model (the instance's
`MINION_LLMS` says which) that uses one of **your own** tools on one task and answers in its role's fixed shape. What
it reads never enters your context: you get its lines back, and a usage line.

## Tools

| Tool | Use it to |
|------|-----------|
| `StartMinion(role, tool, task, stronger)` | Open a minion with `role`, hand it one of your tools (`tool`) and the `task`; wait for its answer. It answers once and closes. `stronger: true` runs the same task on the next, stronger model in the role's list, once, only when the instance lists one. |

## When a minion is worth it

- **Reading you'd otherwise do yourself.** Search results and pages stay in your context and are paid for again on
  every later request of this turn; a minion reads them on the cheap model and hands you a few lines.
- **One task, one tool, one answer.** You decide what to look for and you use the answers; the minion only finds and
  reports. A minion that needs two tools is two minions, or your own work.
- **Not for what you already know.** The tool you hand must be one you hold.

## The roles

| Role | Hand it | Its answer |
|---|---|---|
| `puls-minion-web-price-lookup` | `WebSearch` and a task with one ingredient and the amount to buy, the user's town, and the shops to look in with their websites (it searches each shop twice: the product, then its price; Spar and Usave once, they have no online prices) | `ingredient: …`, then one line per shop: `shop`, `price`, `size`, `per_kg`, `location`, `url`, `quote` (the exact words the price came from), each `unknown` when nothing says, and a `note` when something's off |

## Writing the task

In English, self-contained: the ingredient as you want it priced (name and size), the user's town, and the shops with
their websites. For example: `beef mince, 500 g; Malmesbury; shops: Checkers (checkers.co.za), Pick n Pay (pnp.co.za),
Spar (spar.co.za)`. The minion sees nothing of this conversation and nothing you found elsewhere.

## Using the answer

A minion's answer is a claim on a cheap model. Use only what its lines say: a price with its `url` and `quote`, or
`unknown`. Never fill a gap from memory. A price-lookup minion's prices come from the shops' search results, so they
may be a few days old: say they're from the search index.

## Limits and results

- At most 16 minions per user turn, counted across every specialist working on that turn; each stops after 2 minutes.
  A refused, failed or timed-out minion returns an error: what it was looking for is unknown; don't look for it
  yourself and don't repeat the call.
- The usage line is what the minion cost; it is already in the usage ledger, linked to this turn.
