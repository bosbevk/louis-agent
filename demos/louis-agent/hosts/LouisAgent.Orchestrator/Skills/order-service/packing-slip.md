## Method: packing-slip

**What it does:** `GET /orders/{id}/packing-slip` — what the warehouse prints: `{qty} x {sku}` per item, then
`| GIFT: {message in capitals}` (`Api/PackingSlip.cs`). Most orders have no gift message.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Decisions:**

| The investigation finds | Decision | Who |
|---|---|---|
| a crash in this method's code, its cause certain, e.g. a `NullReferenceException` for an order without a gift message | auto-fix | the coder |
| a fix that is not confirmed, or a cause that isn't certain | person | orders |

**Expected behaviour for louis-agent:** an order without a gift message prints just the items, with no `| GIFT:`
part (e.g. `2 x BOOK-1; 1 x PEN-3`). Slips with a gift message are unchanged.
