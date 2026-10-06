## Method: delivery-estimate

**What it does:** `GET /orders/{id}/delivery-estimate` — the expected delivery date: the created date plus the days
until dispatch (Monday–Thursday 1, Friday 3, Saturday 2) plus 2 days in transit (`Api/DeliveryEstimate.cs`).

**Data note:** the web shop takes orders seven days a week.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. an `IndexOutOfRangeException` for an order placed on a Sunday.

**Expected behaviour for louis-agent:** a Sunday order is dispatched on Monday (1 day), so it arrives 3 days after it
was placed (Sunday 2026-10-04 gives 2026-10-07). Estimates for Monday–Saturday are unchanged.

**Escalate to:** orders — if the fix is not confirmed.
