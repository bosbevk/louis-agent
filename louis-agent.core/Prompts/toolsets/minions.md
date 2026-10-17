# Minions Skills

How to hand one small task to a minion with `StartMinion`. A minion is a worker on a cheap model (the instance's
`MINION_LLMS` says which) that runs one of **your own** tools once, on one task, and answers in its role's fixed shape.
The page it reads never enters your context: you get its few lines back, and a usage line.

## Tools

| Tool | Use it to |
|------|-----------|
| `StartMinion(role, tool, task, stronger)` | Open a minion with `role`, hand it one of your tools (`tool`) and the `task`; wait for its answer. The minion stays open for this chat: the answer's footer names its handle. `stronger: true` runs the same task on the next, stronger model in the role's list, once, after the first answer failed your check. |
| `ContinueMinion(handle, message)` | A follow-up to a minion this chat keeps open, by its handle; it still has the page it read. |

## When a minion is worth it

- **A page you'd otherwise read yourself.** Reading a shop's page into your context costs its tokens on every later
  request of this turn; a minion reads it on the cheap model and hands you five lines. One minion per page.
- **One task, one tool, one answer.** Never a search, a comparison or a decision: you search, you choose the pages,
  you check the answers. A minion that needs two tools is two minions, or your own work.
- **Not for what you already know,** and not for a page the minion's tool can't open: a JavaScript shop page needs
  `ReadRenderedPage`, a plain one `FetchUrl`; the tool you hand must be one you hold.

## The roles

| Role | Hand it | Its answer |
|---|---|---|
| `puls-minion-web-price-lookup` | `FetchUrl` (or `ReadRenderedPage` for a JavaScript shop page) and a task with the URL, the product with its size or unit, and the user's town; or `WebSearch` and the shop's domain instead of a URL, for a shop whose pages no reader opens | `shop`, `location`, `price`, `per_kg`, `date`, `url`, `quote` (the exact words the price came from), `source` (`page` or `search`), each `unknown` when nothing says; a `note` line when something's off |

## Writing the task

In English, self-contained, three things: the URL, the product as you want it priced (name, size or unit), the user's
town. For example: `https://www.checkers.co.za/…/beef-mince-500g; beef mince, 500 g pack; Stellenbosch`. The minion
sees nothing of this conversation and nothing you found elsewhere.

## Checking the answer

A minion's answer is a claim on a cheap model. Check what matters before you use it: a `source: page` price's `quote`
is for `VerifyQuote(url, quote)`, which looks those words up on the page itself; a `source: search` price can't be
checked that way, so say it comes from the search index. A quote that isn't there means the price
isn't either: ask once more with `stronger: true` (the same task on the next model), and if that fails too, treat the
price as `unconfirmed` and say why. Never pass a minion's number on as yours without its check.

## Limits and results

- At most 16 minions per user turn, counted across every specialist working on that turn; each stops after 2 minutes.
  A refused, failed or timed-out minion returns an error: that page's value is unknown; don't read it yourself and
  don't repeat the call.
- The usage line is what the minion cost (0 on the local model); it is already in the usage ledger, linked to this
  turn.
