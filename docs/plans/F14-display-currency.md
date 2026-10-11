# Plan: F14 Display currency

> **For:** [F14 Display currency](../features/F14-display-currency.md) · **Approved:** 2026-10-11

## Context
F2 prices every request in the price table's currency (USD, what Anthropic bills) and F3 shows those amounts in every
host. Louis thinks in rands, so every figure needs converting in his head. F14 shows costs in the PC's own currency,
**taken from the Windows region** (asked for when planning), wherever F3 shows them. The ledger is never converted:
records keep the charged currency with the table's date, so history and F11's reconciliation stay in USD. Feature doc:
`docs/features/F14-display-currency.md` (stories S1–S4, its open rate decision now decided).

**Measured while planning:**
- Windows: culture `en-ZA`, `RegionInfo.CurrentRegion` = `ZA`, ISO currency **ZAR**, symbol **R** (.NET maps region →
  currency itself: no table of ours).
- The containers (API + web, Rider's ACP, orchestrator) run on Linux with the invariant culture: they **can't see** the
  Windows region.
- The browser reports `en-US` (it follows the Windows *display* language, not the region): browser locale would wrongly
  pick USD, so the web app does **not** use it.
- `https://api.frankfurter.dev/v1/latest?from=USD` (ECB reference rates, free, no key; `.app` now redirects there):
  `{"amount":1.0,"base":"USD","date":"2026-10-09","rates":{…,"ZAR":16.5331}}`, ~30 currencies.
- en-ZA formats `35.00` as `35,00` (and dates as `2026/10/11`): running the containers *in* en-ZA (`LANG`) would change
  every tool, `dotnet test` and the agent's own fixes inside them.

**Decided:** the containers get the **Windows culture as a value**, `HOST_CULTURE=en-ZA` in `config/.env` (every
container already reads it), written once from the PC; their own culture stays invariant. Amounts are written
**`R0.11`**: the culture's currency symbol with `.` decimals, like every other number in the logs. Rates are fetched
live at start-up with a dated `config/rates.json` fallback (the `PRICES_URL` pattern).

## Design
**Which currency** (`DisplayCurrency.Resolve`, core `usage/`): `DISPLAY_CURRENCY` (an optional override, e.g. EUR) →
else `HOST_CULTURE`'s region → else the process's own culture's region when it isn't invariant (native runs on the
PC) → else the price table's currency (no conversion, as today). Currency code **and symbol** come from .NET's
`RegionInfo` (`en-ZA` → `ZA`, `ZAR`, `R`); no symbol table of ours. For a `DISPLAY_CURRENCY` override, the symbol is
looked up from the cultures that use that currency, else the code (`0.04 CHF`).

**Getting the PC's culture into the containers:** `dotnet run --project tools/louis-agent.usage-probe --
--host-culture` (run on the PC) prints the detected culture, region, currency and today's rate, and writes
`HOST_CULTURE=en-ZA` into `config/.env` (replacing the line if present, appending with a comment if not; nothing else
in the file changes). Re-run after moving; set `DISPLAY_CURRENCY` to override.

**Rates** (core `usage/`, the price table's pattern):
- **One format of ours, the only one read:** `rates.json`, like `prices.json`:
  `{"as_of":"2026-10-09","base":"USD","source":"frankfurter (ECB reference rates)","rates":{"ZAR":16.5331,…}}`.
  `ExchangeRates.Parse` reads only this. The tracked fallback `config/rates.json` and our API's `GET /rates` are in it.
- **A small strategy turns a provider's reply into it** (`IRatesMapper`, like F1's `IUsageMapper`):
  `FrankfurterRatesMapper` maps `{"amount":1.0,"base","date","rates"}` → ours (`date` → `as_of`, rates scaled by
  `amount`, `source` set); `RatesJsonMapper` passes our own format through (for a host reading another louis-agent's
  `/rates`). Chosen by **`RATES_FORMAT`** (`frankfurter`, the default, or `rates-json`); a new provider is one new
  mapper and nothing else changes.
- **`RATES_URL`** (`AgentOptions.RatesUrl`, a setting like `PRICES_URL`): default
  `https://api.frankfurter.dev/v1/latest?from=USD`; `off` skips the fetch. **`RATES_FILE`**: the fallback, default the
  tracked `config/rates.json` (found like `config/.env`; compose sets the path, as for prices).
- `AgentHost.LoadRates(agent, handler?)` mirrors `LoadPrices`: fetch `RATES_URL` (5-second timeout) → map with
  `RATES_FORMAT` → parse ours; on any failure one `[WARN]` and the file → neither: costs shown in the table's currency.
- **Exposed on the API like the price table:** louis-agent.api serves the rates it loaded, in our format, at public
  `GET /rates` (404 when it has none). Other hosts can read it: the demo's orchestrator gets
  `RATES_URL=http://demo-api:8080/rates` and `RATES_FORMAT=rates-json`, so both agents use one rate. Rider's ACP and
  the CLI keep the default (frankfurter), with the same file fallback.
- The rates' `base` must be the price table's currency, else start-up fails naming the source (like a bad price
  table).
- Loaded once per host start (Rider starts one per chat, the CLI per run), so one rate per run: converting a total
  equals converting each record. The rate's date is always shown with a converted figure.
- A wrong base (rates not from the table's currency) or a malformed file stops start-up, naming it (like prices).

**Formatting** (`MoneyDisplay` = currency + rate + date, built in `AgentHost.Build` → `AgentHostContext.Display`,
`AgentEngine.Display`): `UsageText.Money` converts, then F3's significant-digit rule, with the display's symbol and
**`.` as the decimal mark, always** (`35.00`, never en-ZA's `35,00`): formatted with the invariant culture, whatever the
host's or thread's culture is (`R35.00`, `R0.11`, `$0.006`; one format in every host and the logs). `UsageText.Line` and
`Summary` take the display; `/usage` adds a line: `rate: 16.53 ZAR/USD on 2026-10-09 · $0.04 charged`. Unknown price
stays "price unknown"; $0 stays `R0`.

**Per host**
| Host | Change |
|---|---|
| CLI, Rider (ACP) | `UsageSession` / `/usage` use `engine.Display`: `turn: … · R0.11 · session R0.73` |
| API | `message_delta.usage` and `GET /sessions/{id}` keep `cost`/`currency` (charged) and add `display_cost`, `display_currency`, `display_symbol`, `rate_as_of`; new public `GET /rates` (the loaded rates, in our `rates.json` format); `GET /rates/display` for the web app (`{display_currency, symbols}`, symbols worked out on the server, since the browser's .NET has little culture data) |
| Web app | Default currency = the server's `display_currency` (not the browser's locale, which says en-US here); a currency select in the sidebar (from `GET /rates`), kept in `localStorage` like the theme; `UsageFormat` converts `cost` with the chosen rate and symbol (same rules as `UsageText`) |
| Orchestrator | Comms log in the display currency, with the rate in the run summary; `fixes.jsonl` / `decisions.jsonl` unchanged (charged currency) |

## Steps (each one commit with its test; stop for review after each)
Branch `feature/f14-display-currency` off `main` (after PR #12).
0. **Plan and doc:** this plan to `docs/plans/F14-display-currency.md`; F14's doc records the region decision, the
   rate source and frankfurter's URL; F14 moves to *now* in the TODO (out of M5's order, by choice).
1. **`ExchangeRates` (our format) + `IRatesMapper` (`FrankfurterRatesMapper`, `RatesJsonMapper`) + tracked
   `config/rates.json` + `RATES_URL` / `RATES_FORMAT` / `RATES_FILE` + `AgentHost.LoadRates`.** *Tests:* the
   frankfurter mapper on a saved real reply (and `amount` ≠ 1); the pass-through mapper; `Parse` rejects anything not in
   our format; fetch via `StubHandler`; unreachable, non-200 or unmappable → file with a warning; `off` → file; wrong
   base and malformed fail naming the source.
1b. **API `GET /rates`** in our format (and the compose settings: `RATES_FILE` for the containers; `RATES_URL` +
   `RATES_FORMAT=rates-json` to demo-api's `/rates` for the demo's orchestrator). *Check by hand:* curl shows our
   format with `source: frankfurter…`; with `RATES_URL=off` it serves `config/rates.json`.
2. **`DisplayCurrency.Resolve`.** *Tests:* `DISPLAY_CURRENCY` wins; `HOST_CULTURE=en-ZA` → ZAR and `R`; the process
   culture used only when not invariant (via `CultureInfo` scopes); a bad culture name warns and falls through; no
   rate for the currency → table currency with a warning.
3. **`MoneyDisplay` in `UsageText`.** *Tests:* exact strings in USD (unchanged), ZAR (`R0.11`, `.` decimals even
   under an en-ZA thread culture), EUR, an unknown code; "price unknown"; `R0`; partial totals; the `/usage` rate
   line.
4. **Hosts:** `AgentHost` builds the display; CLI and ACP lines; API display fields and `GET /rates/display`. *Tests:*
   core end-to-end through `AgentHost.Build`; API by hand with curl.
5. **`usage-probe -- --host-culture`** writes `HOST_CULTURE` to `config/.env`. *Tests:* the `.env` line updater
   (replace, append, keeps comments and other lines, CRLF kept).
6. **Web app:** server default, sidebar select, conversion. *Check by hand* in Chrome.
7. **Orchestrator comms log** in the display currency with the rate line. *Tests:* `CommsLog` strings in ZAR.
8. **Docs and finish:** SETUP (`HOST_CULTURE`, `DISPLAY_CURRENCY`, `RATES_URL`, `RATES_FORMAT`, `RATES_FILE`), README,
   the probe's doc, F14 doc *What the build changed*, finishing checklist, GOAL (#13 / #17).

## Approved during the build (2026-10-11)

- **Rerun both demos and update both samples** in step 8, after the docs (about $0.91 on Haiku 5.5: full about $0.66,
  `-Short` about $0.25): their comms logs are then in rands with the rate line, while the ledger and the jsonl files
  stay in USD; each run also re-checks F3 (comms log total against the ledger).

## Verification
- `dotnet test louis-agent-solution.sln` after every step.
- Free, by hand on Ollama with a test price table that prices `qwen2.5:0.5b` above 0 (so conversion shows): the CLI
  line in R, `/usage` with the rate line, the API's display fields and `GET /rates` via curl, the web app's select and
  reload in Chrome, `RATES_URL=off` falling back to `config/rates.json`.
- `--host-culture` on this PC writes `HOST_CULTURE=en-ZA`, and a restarted container then shows R, while its own
  culture stays invariant (a tool's number output unchanged).
- No paid run needed; optional later: a `-Short` demo to see the comms log in rands (≈ $0.25, with approval).

## Concepts

Locale vs region vs display language (why the browser said en-US); configuration that has to cross a container boundary;
an adapter (strategy) at a third-party boundary, so one internal format is all the code reads; stored vs displayed
values (convert at the edge, never in the record); a third-party dependency with a fallback and a dated value. Skill map
#13 (cost) and #17 (observability).

---
[Plans](README.md) · Previous: [F3](F03-usage-display.md)
