# Sample output: the cook check, one run from the admin portal

The first run started from Puls's admin portal (F27, *Hardloop* on the Maatstawwe page) on 2026-10-15, one repeat,
model `claude-haiku-5-5`, after three changes to the cook since the [three-run check](../sample-output-puls-cook/README.md)
that morning: Tavily as the search provider (F25 step 6), `ReadRenderedPage` for JavaScript product pages (step 7),
and the firm follow-up rule (F26 step 6). Same three conversations, same two modes. $0.078 in all.

| | Delegated (Puls as built) | Puls with `web` |
|---|---|---|
| Meals routed to `puls-cook` | 2 / 2 | 2 / 2 |
| Control to `puls-researcher` | yes | no: Puls answered it itself, 15 s |
| Follow-up continued the same cook | **2 / 2** (was 3 / 6) | **2 / 2** (was 0 / 6) |
| Unconfirmed-price marks, the two meals | 2 and 0 (was 20 and 20 in run 1) | 4 and 1 |
| Rand amounts in the answers | 26 and 19 | 27 and 10 |
| Cost | $0.0377 | $0.0404 |

- **Search works:** every search went through Tavily; none was refused. The cook read prices from Checkers product
  pages (R119,98 for the chicken thighs, R49,99 for 2 kg potatoes, R34,99 onions) and wrote one honest gap ("Die
  tamatieblik is die enigste item sonder 'n prys").
- **Rendered pages were used, sparingly:** three `ReadRenderedPage` calls in the run, on Pick n Pay and Checkers
  product pages `FetchUrl` had read as empty, as the role says; forty-odd fetches and searches otherwise.
- **Branches from OpenStreetMap:** the list is headed "Checkers, Stellenbosch Sentraal", from the Nominatim lookup.
- **The firm follow-up rule holds:** all four follow-ups went back to the cook with `ContinueSubSession("sub-1")`,
  including both with `web` on Puls, where the morning's check had none. A follow-up cost $0.0010–0.0020.
- **Dearer per run than the morning's checks** ($0.038 against $0.023 delegated): the cook now reads more pages and
  the follow-ups do real work instead of Puls scaling a list, and Tavily's results are longer than DuckDuckGo's. The
  shopping list is what it is for, though: priced, not estimated.

The files are the check's usual: `results.md` and `results.json`, and per mode `answers.json`, `transcript.md` and
`usage.jsonl`. The portal opens them under *Gestoorde lopies*.
