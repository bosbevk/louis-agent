# Sample output: the cook check (three runs)

`samples/puls-cook-check/run_check.py --repeat 3` on 2026-10-15 (model `claude-haiku-5-5`, the Puls containers on
`claude/project-thread-381vmw` after F25 step 2 and F26 step 4). Three conversations, each in its own Puls session:

- **cook-af:** "Wat kan ek vanaand vir vier mense kook vir onder R250?" with the town on the session
  (Stellenbosch), then "Maak dit eerder vir ses mense. Wat kos dit nou?"
- **cook-en:** "What should I cook tonight in Pretoria for two? I have about an hour…", then "Swap the meat for chicken."
- **control-web:** the prime lending rate, with sources (the delegation check's web-2), which must go to the researcher.

Two modes: **delegated** (Puls as built: `agents` only) and **puls-web** (`agents` and `web`: Puls can search itself).
The scores come from what the agents called (the role Puls passed, the hosts the specialist fetched, the follow-up's
`ContinueSubSession`), not from reading the prose; only the rand amounts and the unconfirmed-price marks are read from
the answer. Costs are the ledger's (USD), specialists included. The three runs cost $0.114 together.

## The questions this run was asked

| | Delegated (3 runs) | Puls with web (3 runs) |
|---|---|---|
| **Does Puls route a meal to the cook?** | **6 of 6** meals to `puls-cook`, 3 of 3 controls to `puls-researcher` | 6 of 6 meals to `puls-cook`; the control answered by Puls itself, 3 of 3 |
| **Does the cook use local sources?** | Every meal priced at Checkers and Pick n Pay, often Woolworths, Shoprite and Spar too; the only non-shop hosts were recipe pages (Rhodes, Food24, Wikipedia) | the same |
| **Does a follow-up go to the same cook?** | **3 of 6** continued the cook (`sub-1`); Puls answered the other 3 itself | **0 of 6**: Puls answered every follow-up itself |
| **Does the cook invent a price?** | No: 1 or 2 prices confirmed from a product page per meal, the rest marked *geskat* / *skatting* with the total called an estimate | the same |

## The totals

| Mode | Cost per run, mean (lowest–highest) | Tokens per run | Requests per run |
|---|---:|---:|---:|
| delegated | $0.0229 ($0.0199–0.0263) | 260k–408k | 29–36 |
| puls-web | $0.0152 ($0.0133–0.0173) | 194k–284k | 20–25 |

| Conversation | Mode | Routed right | Priced at a shop | Follow-up continued | Cost, mean (lowest–highest) |
|---|---|---:|---:|---:|---:|
| cook-af | delegated | 3/3 | 3/3 | 1/3 | $0.0079 (0.0073–0.0082) |
| cook-af | puls-web | 3/3 | 3/3 | 0/3 | $0.0058 (0.0053–0.0061) |
| cook-en | delegated | 3/3 | 3/3 | 2/3 | $0.0072 (0.0043–0.0106) |
| cook-en | puls-web | 3/3 | 3/3 | 0/3 | $0.0044 (0.0038–0.0050) |
| control-web | delegated | 3/3 | — | — | $0.0078 (0.0049–0.0110) |
| control-web | puls-web | 0/3 | — | — | $0.0051 (0.0030–0.0074) |

A meal's first turn cost $0.0030–0.0095 and took 31–84 s; its follow-up cost $0.0006–0.0015 and took 4–26 s whether
the cook continued it (cache reads on its kept prefix) or Puls answered it itself.

## What it says

- **Routing holds.** Two roles share the `web` toolset and Puls picked the right one every time from the request alone:
  the two table rows (F25 step 1) are enough. With `web` of its own Puls still hands the meal to the cook (the router's
  "delegate when you lack a toolset" rule didn't win), but it answers the plain web question itself.
- **The cook is honest about prices, and search was broken for most of the run.** DuckDuckGo refused 41 of the
  specialist's 48 searches ("refused the automated search"; the same refusal hit the roles run). The cook then fetched
  the shops' own product and search pages: a Checkers product page gives a price (R129,99/kg mince, R64,99 for 500 g),
  the shops' search pages render with JavaScript and give none. So each meal has one or two confirmed prices and a list
  of estimates, each marked, and a total called an estimate; one answer opened with *"Eerlik eerste: ek kon nie die
  winkelpryse bevestig nie."* It never presented an estimate as a shop price. But it estimates rather than writing
  *unknown* as its role says, and an estimate of ±R20 is a price the user didn't ask it to guess: the role's wording
  is for Louis to decide.
- **The follow-up is where the prompt is weakest.** Puls continued the cook in half the delegated follow-ups; in the
  others it scaled the list by 1.5 itself (correctly, with the estimates still marked) in under ten seconds. With `web`
  it never continued. The result was fine both ways for "make it for six"; for "swap the meat for chicken" the cook's
  own answer was better (it fetched the chicken mince page). The rule in `default.md` and *Following up* is one line
  each; Puls reads it as optional when it can do the arithmetic itself.
- **Puls with `web` is cheaper** (−34% per run) because the control question never starts a specialist and the follow-ups
  never continue one. On the control it read 11 pages itself and gave the same rate with the same date as the
  researcher, 23–57 s faster.
- **Scoring gaps found in this run:** the *Only SA sources* score counts a recipe page on rhodesquality.com or food24.com
  against the cook, which is wrong (every shop it priced at was South African); and the first regex for "unknown"
  missed the Afrikaans *geskat* / *skatting* (fixed before `--report`, so the saved tables are right).

## What to change next

1. **Search:** a provider that isn't refused (Google's Custom Search works but is discontinued on 2027-01-01; Brave,
   Tavily or Google's Agent Search are the candidates: a package decision for Louis). Without it the cook can only
   confirm the prices whose product pages it guesses the URL of.
2. **The follow-up rule:** say in `default.md` that a follow-up on a specialist's answer goes to that specialist even
   when Puls could answer it, or accept that Puls does the arithmetic. Measure again.
3. **The cook's estimates:** *unknown* as the role says, or estimates marked as now. Louis's call.
4. **The Werk panel** names the peer (`puls`), not the role, and shows a continue as a bare tool line (Louis, from the
   screenshot during this run): F26's next step.

## Files

| Path | What it is |
|---|---|
| `summary.md`, `summary.json` | The three runs together: routing, sources, follow-ups and cost per conversation and mode |
| `run-N/results.md`, `results.json` | One run's table: role, hosts fetched, rand amounts, unconfirmed marks, follow-up, cost per turn |
| `run-N/{delegated,puls-web}/transcript.md` | Each conversation's turns: Puls's calls, the specialist's calls, the answers |
| `run-N/{delegated,puls-web}/answers.json`, `usage.jsonl` | The raw answers with every call recorded, and the run's priced ledger records |

Scores and tables again from the saved records (no model calls):
`python samples/puls-cook-check/run_check.py --report samples/sample-output-puls-cook`.
