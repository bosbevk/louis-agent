## Method: refund

**What it does:** `POST /orders/{id}/refund` — refunds the order's total to the card it was paid with
(`payment_ref`) and marks it refunded (`OrderApi.RefundOrder`). This moves money.

**Expected (acknowledge, no action):**
- `System.InvalidOperationException` with a message containing "already been refunded" — a duplicate refund
  request was correctly rejected.
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** nothing. Payments code is never changed by an agent.

**Escalate to:** payments — every other exception, with the exception type, message and order id, so they can
check whether the customer was refunded.
