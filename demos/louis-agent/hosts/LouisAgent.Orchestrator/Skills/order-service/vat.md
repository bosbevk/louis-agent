## Method: vat

**What it does:** `GET /orders/{id}/vat` — the VAT included in the order total, at the rate of the order's country:
NL 21%, DE 19%, FR 20% (`Api/Vat.cs`).

**Data note:** country codes come from customers' addresses and are not normalised; `nl` and `NL` are the same country.

**Expected (acknowledge, no action):**
- `OrderService.OrderNotFoundException` — unknown order id (a 404).

**Decisions:**

| The investigation finds | Decision | Who |
|---|---|---|
| a crash caused by the casing of a country code we support (e.g. `KeyNotFoundException` for `nl`), or any other crash on valid data, its cause certain | auto-fix | the coder |
| a country with no rate: a tax decision, not a code fix | person | finance |
| a fix that is not confirmed, or a cause that isn't certain | person | finance |

**Expected behaviour for louis-agent:** country codes are matched case-insensitively. Rates and amounts are
unchanged. Do not invent rates for new countries.
