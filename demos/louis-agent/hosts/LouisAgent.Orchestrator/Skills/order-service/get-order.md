## Method: get-order

**What it does:** `GET /orders/{id}` — a one-line summary of an order: id, customer, number of items and the first
item's SKU (`Api/GetOrder.cs`). Read-only. It reads the service's data file, `data/orders.csv`, which the deploy puts
beside the service.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — the client asked for an order id that doesn't exist (a 404).

**Decisions:**

| The investigation finds | Decision | Who |
|---|---|---|
| a crash in this method's code, its cause certain: a lookup must never crash on an order that exists, including one with no items | auto-fix | the coder |
| the data file, `data/orders.csv`, missing or unreadable, or where it is deployed | hand-off | ops |
| a fix that is not confirmed, or a cause that isn't certain | person | orders |

**Expected behaviour for louis-agent:** every order in the data returns its summary; an order without items says it
has 0 items and no first SKU (e.g. "Order 1006 for Barbara Liskov: 0 item(s)"). The summary of orders with items is
unchanged; unknown ids still throw `OrderNotFoundException`.
