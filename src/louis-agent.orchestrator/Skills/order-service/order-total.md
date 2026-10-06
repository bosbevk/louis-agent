## Method: order-total

**What it does:** `GET /orders/{id}/total` — the amount to charge for an order: the sum of quantity × unit price,
minus the order's discount code, rounded to 2 decimals (`OrderApi.GetOrderTotal`). The checkout calls it before
taking payment, so a failure here blocks the sale.

**Discount codes in force:** `WELCOME10` (10%), `VIP20` (20%). Marketing retires codes without telling engineering, so
orders can carry a code that is no longer in the table.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception — for example a `KeyNotFoundException` for a discount code that is not in the
table, or bad item data.

**Expected behaviour for louis-agent:** an unknown or retired discount code gives no discount (the total is the
subtotal) instead of failing. Totals for orders with `WELCOME10`, `VIP20` or no code must not change.

**Escalate to:** orders — if the fix is not confirmed, or if a fix would change the total of an order that has a
valid code.
