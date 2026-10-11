# F14 · Display currency: show costs in your own currency

> **Status:** done (2026-10-11) · **Milestone:** M5 (built early, by choice) · **Depends on:** F2, F3
>
> **Spec:** [Usage §4.2 (prices), §7 (showing usage)](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F14 Display currency** under *Now* ([TODO](../../TODO.md)) · **Plan:**
> [plans/F14](../plans/F14-display-currency.md)

F2 prices every request in the price table's currency (USD: what Anthropic bills in), and F3 shows those amounts
everywhere: `turn: 3 requests · 28k tokens · $0.006`. For someone who thinks in another currency, every figure needs a
conversion in their head. F14 shows costs in a chosen currency (`R0.11`, `€0.0055`) wherever F3 shows them.

**What doesn't change: the ledger.** Records keep their cost in the price table's currency, stored with the table's
date (F2). Converting is for display only, so history is never rewritten by a rate change, and F11 still compares the
ledger with the bill in the bill's currency.

## User stories

**F14-S1 — One currency for every host.** As an *operator*, I want to set the currency costs are shown in, so that the
CLI, Rider, `/usage` and the orchestrator's comms log all show the same one.
- *Given* `DISPLAY_CURRENCY=ZAR` and a rate for ZAR *when* a turn ends *then* the usage line shows
  `turn: 3 requests · 28k tokens · R0.11 · session R0.73`.
- *Given* no `DISPLAY_CURRENCY` *then* the PC's region decides (Windows `en-ZA` → ZAR, passed to the containers as
  `HOST_CULTURE`), and with no region either, the price table's currency, as in F3.
- *Given* `DISPLAY_CURRENCY` names a currency with no rate *then* start-up logs one `[WARN]` and costs are shown in the
  table's currency (never a guessed rate).

**F14-S2 — Each web user picks their own.** As a *user* of the web app, I want to choose the currency in the settings,
so that I see costs in mine whatever the server's default is.
- *Given* a currency chosen in the settings *then* every answer's line and the chat's total use it, and the choice is
  kept for that browser (like the theme).
- *Given* nothing chosen *then* the web app uses the server's `DISPLAY_CURRENCY`.

**F14-S3 — Honest about the rate.** As a *user*, I want to know which rate a converted figure used, so that I don't take
an estimate for the bill.
- *Given* a converted cost *then* `/usage` names the rate and its date (`at 18.40 ZAR/USD, 2026-10-11`) and the amount
  in the table's currency.
- *Given* the API's `message_delta.usage` *then* `cost` and `currency` stay in the table's currency (what was charged)
  and `display_cost`, `display_currency` and `rate_as_of` are added; the orchestrator's `fixes.jsonl` keeps the charged
  currency.

**F14-S4 — Rates kept up to date.** As an *operator*, I want rates that are current enough without maintaining them by
hand every day, so that converted figures stay close to the truth. *How*: the open decision below.

## Design

- **`ExchangeRates`** (core, `usage/`): rates from the price table's currency to others, each with an `as_of` date;
  `Convert(UsageCost, currency)` returns the converted amount and the rate used, or null when there is no rate.
- **Two formatters to change:** core's `UsageText` and the web app's `UsageFormat` (which can't reference core) share
  the number rules, so both get the currency, the symbols and the conversion, with the same test strings.
- **`UsageText`** takes a display currency and the rates: `Money` converts, then formats with the currency's symbol
  where it's unambiguous (`$`, `€`, `£`, `R`; otherwise the code, `0.04 CHF`) and F3's significant-digit rule.
- **Hosts** pass `DISPLAY_CURRENCY` (`AgentOptions`) and the loaded rates; the web app gets them from the API (with the
  prices, or `GET /rates`) and converts on its side, so a user's choice doesn't touch the server.
- **Totals convert per record, not per total**: rates change over time, so a session that crosses a rate update adds up
  each record at the rate shown with it. (With one rate per run this is the same; it matters for F11's longer reports.)

### Decided when planning (2026-10-11)

- **Which currency:** the Windows region, read on the PC and written once to `config/.env` as `HOST_CULTURE=en-ZA`
  (containers can't see it, and running them *in* en-ZA would change every tool's number format); `DISPLAY_CURRENCY`
  overrides it. The browser's locale isn't used: it reports the display language (`en-US`), not the region.
- **Format:** the culture's symbol with `.` decimals (`R35.00`, `R0.11`), never `35,00`.
- **Rates: A + B below.** Live from `RATES_URL` (default frankfurter, ECB reference rates), mapped by a small
  strategy (`IRatesMapper`) into our own `rates.json` format, the only one the code reads; the tracked
  `config/rates.json` as the fallback; and served by the API at `GET /rates`, like the price table.

The options as they were weighed:

| Option | How | For | Against |
|---|---|---|---|
| **A. Fixed rates in config** | A dated `config/rates.json` (or a `rates` section in `prices.json`), updated by hand like the prices | Free, offline, reproducible: a run shows the same figures tomorrow; no third party | Goes stale; someone has to update it |
| **B. Live rate at start-up** | Fetch the day's rates from a public exchange-rate API (e.g. the ECB's daily reference rates through a free endpoint), cached for the day | Always current; no upkeep | A network dependency and a third party at start-up; figures differ day to day; needs a fallback |
| **A + B** | B when reachable, A as the fallback (the same pattern as F2's `PRICES_URL` and the price file) | Current when online, still works offline | Two paths to test |

Whatever is chosen, the rate's date is always shown with a converted figure (S3), and a missing rate falls back to the
table's currency, never to a guess.

## Implementation steps

The approved [plan](../plans/F14-display-currency.md) has the steps as they will be built; these were the first
sketch.

1. **Decide the rate source** (above) and write the plan in [plans/](../plans/README.md). *Done.*
2. **`ExchangeRates` and its loading.** *Test:* convert with a dated rate; unknown currency → null; malformed rates file
   fails start-up naming the file (like prices).
3. **`UsageText` and the web app's `UsageFormat` in a display currency.** *Test:* exact strings for USD, ZAR, EUR and an unknown code; partial-priced
   totals still say how many are unpriced; `/usage` names the rate and its date.
4. **`DISPLAY_CURRENCY` in the CLI and Rider.** *Test:* the setting parses; manual check on Ollama (costs 0 convert to
   0, so check with a priced model's saved ledger or a test price table).
5. **API: `display_cost`, `display_currency`, `rate_as_of`** on `message_delta.usage` and `GET /sessions/{id}`; rates
   served to the web app. *Check by hand* with curl.
6. **Web app: currency in the settings**, kept per browser. *Check by hand* in the browser.
7. **Orchestrator comms log** in the display currency (with the rate in the run summary); `fixes.jsonl` unchanged.
8. **Docs and finishing.**

## Done when

- With `DISPLAY_CURRENCY` set, the CLI line, `/usage`, Rider, the web app and the comms log all show converted costs
  with the rate's date available, and the ledger is byte-for-byte what it would have been without F14.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)).

## What the build changed from this plan

- **The currency comes from the PC's Windows culture** (`HOST_CULTURE`, written by `usage-probe --host-culture`), with
  `DISPLAY_CURRENCY` as the override, rather than `DISPLAY_CURRENCY` alone. The containers can't see the region, and
  running them in en-ZA would have changed every tool's number format (`35,00`), so the culture is passed as a value.
- **Code and symbol from .NET's `RegionInfo`** instead of a symbol table of ours; a symbol that clashes with the price
  table's own (`$` for AUD, CAD, NZD, MXN) is written as the code (`0.1 AUD`).
- **Rates: our own `rates.json` format, the only one read**, with a mapper per provider (`IRatesMapper`; Frankfurter's
  reply is mapped), `RATES_URL` / `RATES_FORMAT` / `RATES_FILE`, a tracked `config/rates.json`, and `GET /rates` on the
  API like the price table; the demo's orchestrator reads demo-api's, so both agents use one rate. An outage or an
  unmappable reply falls back to the file, and so does a reply in the wrong currency (the provider's change, found in
  the PR #13 review); only a `RATES_FILE` in the wrong currency, our own configuration, stops start-up. The default URL
  is `?base=USD`, v1's documented parameter (`?from=` means a date in Frankfurter v2).
- **Totals are converted at the one rate the host loaded,** not record by record as the design above says. A host loads
  its rates once at start-up, so the numbers are the same; F11, whose reports cover months, must convert each record
  at the rate of its own date instead of relying on this.
- **The API's usage object always carries `display_*`** (the charged figures when not converting), and
  `GET /rates/display` gives the web app the server's currency and a symbol per currency. The web app converts the
  stored charged cost when drawing, so changing its currency re-labels every saved chat.
- **Amounts always use `.`** (`R35.00`), whatever the culture; converted totals carry the rate and the USD charged
  (`/usage`, the comms log's run total).
- **Both demos were rerun for the samples** (comms logs in rands; both matched their ledgers exactly in USD). The full
  run found [FX8](../fixes/FX08-fix-result-quotes.md), fixed in F14; the full sample is kept as it was, with a note.
- **Checked by hand:** the CLI, Rider (over stdio), the API and the web app (Chrome) on Ollama with a priced test
  table, and the orchestrator in both demos.

## Not in this feature

Pricing in another currency (the price table stays in the bill's currency); budgets in another currency (F7 compares
in the table's currency; showing a budget converted comes for free from S1).

---
[Features](README.md) · Previous: [F13 Local models](F13-local-models.md) · Next: [F15 Model limits](F15-model-limits.md)
