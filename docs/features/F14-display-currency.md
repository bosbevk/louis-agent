# F14 · Display currency: show costs in your own currency

> **Status:** planned · **Milestone:** M5 · **Depends on:** F2, F3
>
> **Spec:** [Usage §4.2 (prices), §7 (showing usage)](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F14 Display currency** under *Now* ([TODO](../../TODO.md))

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
- *Given* no `DISPLAY_CURRENCY` *then* everything is shown in the price table's currency, as in F3.
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

### Open decision: where rates come from (decide when planning F14)

| Option | How | For | Against |
|---|---|---|---|
| **A. Fixed rates in config** | A dated `config/rates.json` (or a `rates` section in `prices.json`), updated by hand like the prices | Free, offline, reproducible: a run shows the same figures tomorrow; no third party | Goes stale; someone has to update it |
| **B. Live rate at start-up** | Fetch the day's rates from a public exchange-rate API (e.g. the ECB's daily reference rates through a free endpoint), cached for the day | Always current; no upkeep | A network dependency and a third party at start-up; figures differ day to day; needs a fallback |
| **A + B** | B when reachable, A as the fallback (the same pattern as F2's `PRICES_URL` and the price file) | Current when online, still works offline | Two paths to test |

Whatever is chosen, the rate's date is always shown with a converted figure (S3), and a missing rate falls back to the
table's currency, never to a guess.

## Implementation steps

1. **Decide the rate source** (above) and write the plan in [plans/](../plans/README.md).
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

## Not in this feature

Pricing in another currency (the price table stays in the bill's currency); budgets in another currency (F7 compares
in the table's currency; showing a budget converted comes for free from S1).

---
[Features](README.md) · Previous: [F13 Local models](F13-local-models.md)
