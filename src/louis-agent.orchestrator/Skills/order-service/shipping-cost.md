## Method: shipping-cost

**What it does:** `GET /orders/{id}/shipping` — shipping is free from a 100.00 subtotal; below that a parcel costs
4.95, or 9.95 when the average item price is above 50.00 (`Api/ShippingCost.cs`).

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. a `DivideByZeroException` for an order without items.

**Expected behaviour for louis-agent:** an order without items has nothing to ship and costs 0.00. The rules above
are unchanged for every other order.

**Escalate to:** orders — if the fix is not confirmed.
