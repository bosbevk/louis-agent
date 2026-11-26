## Method: loyalty-points

**What it does:** `GET /orders/{id}/loyalty-points` — one point per full 10.00 of the subtotal, before discounts
(`Api/LoyaltyPoints.cs`).

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Decisions:**

| The investigation finds | Decision | Who |
|---|---|---|
| a crash in this method's code, its cause certain, e.g. an `OverflowException` for a large business order | auto-fix | the coder |
| a fix that is not confirmed, or a cause that isn't certain | person | orders |

**Expected behaviour for louis-agent:** points are correct for any order size (a 25,000,000.00 order earns
2,500,000 points) and are still a whole number. The method keeps returning `int`; points for normal orders are
unchanged.
