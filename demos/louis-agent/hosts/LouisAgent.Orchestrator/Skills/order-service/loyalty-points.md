## Method: loyalty-points

**What it does:** `GET /orders/{id}/loyalty-points` — one point per full 10.00 of the subtotal, before discounts
(`Api/LoyaltyPoints.cs`).

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. an `OverflowException` for a large business order.

**Expected behaviour for louis-agent:** points are correct for any order size (a 25,000,000.00 order earns
2,500,000 points) and are still a whole number. The method keeps returning `int`; points for normal orders are
unchanged.

**Escalate to:** orders — if the fix is not confirmed.
