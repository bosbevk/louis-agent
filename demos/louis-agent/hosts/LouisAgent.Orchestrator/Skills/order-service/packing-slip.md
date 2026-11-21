## Method: packing-slip

**What it does:** `GET /orders/{id}/packing-slip` — what the warehouse prints: `{qty} x {sku}` per item, then
`| GIFT: {message in capitals}` (`Api/PackingSlip.cs`). Most orders have no gift message.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. a `NullReferenceException` for an order without a gift message.

**Expected behaviour for louis-agent:** an order without a gift message prints just the items, with no `| GIFT:`
part (e.g. `2 x BOOK-1; 1 x PEN-3`). Slips with a gift message are unchanged.

**Escalate to:** orders — if the fix is not confirmed.
