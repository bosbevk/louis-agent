## Method: get-order

**What it does:** `GET /orders/{id}` — a one-line summary of an order: id, customer, number of items and the first
item's SKU (`Api/GetOrder.cs`). Read-only.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — the client asked for an order id that doesn't exist (a 404).

**Auto-fixable:** any other exception. A lookup must never crash on an order that exists, including one with no items.

**Expected behaviour for louis-agent:** every order in the data returns its summary; an order without items says it
has 0 items and no first SKU (e.g. "Order 1006 for Barbara Liskov: 0 item(s)"). The summary of orders with items is
unchanged; unknown ids still throw `OrderNotFoundException`.

**Escalate to:** orders — if the fix is not confirmed.
