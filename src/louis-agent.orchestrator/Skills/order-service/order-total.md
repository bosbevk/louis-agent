## Method: order-total

**What it does:** `GET /orders/{id}/total` — the amount to charge: the subtotal minus the order's discount code,
rounded to 2 decimals (`Api/OrderTotal.cs`). The checkout calls it before taking payment, so a failure blocks the sale.

**Discount codes in force:** `WELCOME10` (10%), `VIP20` (20%). Marketing retires codes without telling engineering, so
orders can carry a code that is no longer in the table.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception — for example a `KeyNotFoundException` for a retired discount code.

**Expected behaviour for louis-agent:** an unknown or retired discount code gives no discount (the total is the
subtotal) instead of failing. Totals for orders with `WELCOME10`, `VIP20` or no code must not change.

**Escalate to:** orders — if the fix is not confirmed, or would change the total of an order with a valid code.
