## Method: customer-initials

**What it does:** `GET /orders/{id}/customer-initials` — the initials printed on the parcel label: the first letters
of the first and last word of the customer's name (`Ada Lovelace` → `AL`) (`Api/CustomerInitials.cs`).

**Data note:** some customers have a single name (e.g. `Cher`).

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Decisions:**

| The investigation finds | Decision | Who |
|---|---|---|
| a crash in this method's code, its cause certain, e.g. an `IndexOutOfRangeException` for a single-word name | auto-fix | the coder |
| a fix that is not confirmed, or a cause that isn't certain | person | orders |

**Expected behaviour for louis-agent:** a single-word name gives one initial (`Cher` → `C`). Every other name keeps
exactly two initials, from its first and last word: `Ada Lovelace` → `AL`, `Mary Ann Smith` → `MS` (not `MAS`).
