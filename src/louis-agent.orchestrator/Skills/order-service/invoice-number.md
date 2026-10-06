## Method: invoice-number

**What it does:** `GET /orders/{id}/invoice-number` — `INV-{yyyyMM}-{id}`, from the order's `created` date
(`Api/InvoiceNumber.cs`). Accounting files invoices by this number.

**Data note:** `created` is an ISO 8601 date (`2026-10-01`); orders from the new checkout also carry a time and zone
(`2026-10-02T14:05:00Z`).

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. a `FormatException` on a `created` value with a time.

**Expected behaviour for louis-agent:** both forms of `created` are accepted, and the month comes from the date part
(`2026-10-02T14:05:00Z` gives `INV-202610-1007`). Numbers for date-only orders are unchanged.

**Escalate to:** orders — if the fix is not confirmed.
