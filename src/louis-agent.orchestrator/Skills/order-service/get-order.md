## Method: get-order

**What it does:** `GET /orders/{id}` — returns a one-line summary of an order: id, customer and number of items
(`OrderApi.GetOrder`). Read-only; changes nothing.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — the client asked for an order id that doesn't exist (a 404).

**Auto-fixable:** any other exception. A lookup must never crash on the data of an order that exists.

**Expected behaviour for louis-agent:** for any order in `data/orders.csv`, `get-order` returns the summary line;
unknown ids still throw `OrderNotFoundException`.

**Escalate to:** orders — if the fix is not confirmed.
