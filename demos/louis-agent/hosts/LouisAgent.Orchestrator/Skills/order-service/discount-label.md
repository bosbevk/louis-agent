## Method: discount-label

**What it does:** `GET /orders/{id}/discount-label` — how the receipt shows the discount code: the name and the
percentage from its last two digits, e.g. `WELCOME10` → `WELCOME (10% off)` (`Api/DiscountLabel.cs`).

**Data note:** marketing also issues codes without a percentage, such as `FREESHIP`.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. a `FormatException` for a code that doesn't end in a number.

**Expected behaviour for louis-agent:** a code that doesn't end in two digits is shown as-is (`FREESHIP`). Labels
for percentage codes and "no discount" are unchanged.

**Escalate to:** orders — if the fix is not confirmed.
