# Sample output: the cook check from the portal, the third run

One repeat from the admin portal on 2026-10-15 at 15:51 (model `claude-haiku-5-5`), the first run after the role
change that lets the cook take a price from a shop's own search result, dated from the index, instead of fetching the
product page, and after the role learned which shops' pages give a price (Checkers and Shoprite; not Pick n Pay).
$0.059 in all: the cheapest run of the day.

| | Delegated | Puls with `web` |
|---|---|---|
| Meals routed to `puls-cook` | 2 / 2 | 2 / 2 |
| Control to `puls-researcher` | yes | no: Puls answered itself |
| Follow-up continued the same cook | 2 / 2 | 1 / 2 (the English follow-up answered by Puls in 5 s) |
| Specialist fetches, both modes | 6 (was 14 the run before) | 4 (was 11) |
| Unconfirmed marks on the meals | 2 and 2 | 1 and 0 |
| Cost | $0.0325 (was $0.0425) | $0.0264 (was $0.0386) |

- **Pricing from the index works as intended:** the Afrikaans meal's list is headed "Die pryse kom uit 'n soekindeks,
  nie uit regte winkelbladsye nie. Dit kan 'n paar dae oud wees", with Checkers prices for tomatoes, carrots, peas,
  potatoes, onions, rice and curry powder, and the two Stellenbosch branches from OpenStreetMap. Fetches fell by more
  than half and the run cost a quarter less than the one before.
- **No `ReadRenderedPage` calls:** the role now says Pick n Pay's pages can't be read, so the cook stopped trying.
- **One follow-up slipped:** with `web` on Puls, "swap the meat for chicken" was answered by Puls itself in five
  seconds. The firm rule holds in 3 of 4 here, 4 of 4 in the two runs before.

Files as the check writes them: `results.md`, `results.json`, and per mode `answers.json` (with the thinking),
`transcript.md` and `usage.jsonl`.
