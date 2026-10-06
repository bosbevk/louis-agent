## Method: customer-initials

**What it does:** `GET /orders/{id}/customer-initials` — the initials printed on the parcel label: the first letters
of the first and last word of the customer's name (`Ada Lovelace` → `AL`) (`Api/CustomerInitials.cs`).

**Data note:** some customers have a single name (e.g. `Cher`).

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Auto-fixable:** any other exception, e.g. an `IndexOutOfRangeException` for a single-word name.

**Expected behaviour for louis-agent:** a single-word name gives one initial (`Cher` → `C`). Two-or-more-word names
are unchanged.

**Escalate to:** orders — if the fix is not confirmed.
