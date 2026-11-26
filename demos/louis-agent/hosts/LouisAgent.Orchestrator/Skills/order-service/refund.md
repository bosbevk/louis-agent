## Method: refund

**What it does:** `POST /orders/{id}/refund` — refunds the order's total to the card it was paid with
(`payment_ref`) and marks it refunded (`Api/Refund.cs`). This moves money.

**Expected (acknowledge, no action):**
- `System.InvalidOperationException` with a message containing "already been refunded" — a duplicate refund
  request was correctly rejected.
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Decisions:** Payments code is never changed by an agent, so nothing here is auto-fixed.

| The investigation finds | Decision | Who |
|---|---|---|
| any exception not listed as expected, whatever its cause: send the exception type, message and order id, so they can check whether the customer was refunded | person | payments |
