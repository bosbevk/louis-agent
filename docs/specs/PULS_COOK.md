# Puls's cook: what to add next

> **What this answers:** after the F48 plan ([F48-cook-and-teacher.md](../plans/F48-cook-and-teacher.md)),
> what else would make `puls-cook` the best home cook and the best teacher in South Africa: what the competition
> does, what cooking-teaching research says works, what SA households need, and which AI techniques help. Each idea
> says why, how it fits our engine, and what it costs, then the whole list is put in an order. Research 2026-10-26,
> for Louis. Nothing here is approved. The page with the diagrams drawn:
> <https://claude.ai/artifact/RJutZeb2sak75HB2M2X1Vw>.

## The short answer

- **The competition is tied to one shop.** Woolworths' *My Woolies Chef* suggests meals from its own *Taste* archive
  and fills a Woolies Dash basket (a beta from September 2026, wider launch early 2027). Pick n Pay's *Penny* builds
  baskets from voice notes and photos. Checkers' *Pixie* recommends products from your past orders and does no
  recipes. None of them teaches, and none works across shops. That's Puls's gap, and the window is until early 2027.
- **The best cooking apps win in the kitchen, not in the chat.** Their standout feature is a guided *cook mode*: one
  step at a time, timers, hands free, and a helper that knows which step you're on. A reviewer's main complaint about
  general assistants like ChatGPT is that they "don't track your recipe or step". Puls's sessions already keep the
  recipe, so we can do this.
- **SA households are squeezed.** A low-income household's food basket was R5,328 in March 2026, 17% under what
  basic nutrition needs, and food is bought only after transport and electricity are paid. About a third of SA's food
  is wasted. A cook that plans the week for a budget, uses up leftovers and says which pack size is cheaper per kg is
  doing real good.
- **Safety has to be in code.** SA's 2018 listeriosis outbreak (from polony) made over 1,000 people sick and killed
  over 200. Studies find users don't notice flaws in AI recipes. So the cook gets a **food-safety guard in C#**, not
  only a line in the prompt: poultry to 74 °C, mince to 71 °C, leftovers reheated to 74 °C.
- **"Best teacher" can be measured.** A published, free questionnaire (Lavelle et al., 14 cooking skills rated 1 to 7,
  CC BY) measures cooking confidence. Three of its questions set the user's level, and the same three asked later
  show whether Puls taught them anything.
- **18 ideas, in three waves.** Six are cheap and belong with F48. Six come next as one feature. Six wait for
  retrieval, vision, voice or the WhatsApp front door.

## What the competition does

| Who | What it does | What it doesn't |
|---|---|---|
| Woolworths *My Woolies Chef* | Meals from what you have, your diet or household size; recipes from Woolworths' *Taste* archive; ingredients straight into a Woolies Dash basket | One shop, one recipe archive; beta only (September 2026), wide launch early 2027 |
| Pick n Pay *Penny* | Builds a basket from voice notes, texts and photos; meal plans by household size, budget and diet | Demonstrated, not launched; one shop |
| Checkers *Pixie* | Product suggestions from past orders, specials, a swipe-to-add basket | No recipes, no cooking |
| Glutt, SideChef, Samsung Food, Pestle, Cookie | Guided cook mode: one step at a time, timers, voice ("next", questions mid-cook), a camera check of the pan, pantry recipes | Mostly US/EU recipes and prices, paid tiers, phone-only apps |
| ChatGPT and other general assistants | Ideas, conversions, swaps | "Doesn't track your recipe or step" (reviewer); no local prices or shops |

What Puls can own: **any shop, real SA recipes from many sites, prices from official data, and a cook that teaches.**
Sources: [Business Day](https://www.businessday.co.za/companies/2026-07-15-news-analysis-grocers-bet-you-will-ask-the-algorithm/),
[Pixie for everyone](https://newcastillian.com/2026/07/26/checkers-sixty60-opens-tinder-for-groceries-ai-assistant-to-all-customers/),
[AI cooking apps compared](https://glutt.org/best-ai-cooking-apps) (written by one app's founder, so read it as a
feature list, not a verdict).

## What SA home cooks need

- **Money first.** The PMBEJD Household Affordability Index: the food basket low-income households actually buy cost
  R5,328.53 in March 2026, against R6,410.74 for basic nutrition, a gap of 17%. "Food is bought after monies for
  transport and electricity have been paid for." A worker's family has about R595 a person a month for food.
  ([TimesLIVE](https://www.timeslive.co.za/news/south-africa/2026-04-08-households-forced-to-cut-back-on-nutrition-as-costs-outpace-income/))
- **Waste.** About 10 million tonnes of food a year, roughly a third of what's available, is wasted in SA.
  ([Daily Maverick](https://www.dailymaverick.co.za/article/2024-04-12-how-to-reduce-food-waste-and-boost-nutrition-security))
- **Safety.** The 2018 listeriosis outbreak, traced to polony, made over 1,000 people sick and killed over 200.
  ([Food Safety News](https://www.foodsafetynews.com/publishers-platform-it-has-nearly-been-six-years-since-over-1000-people-were-sickened-and-over-200-died-from-listeria-tainted-polony-in-south-africa/))
  Ready-to-eat meats are a real risk for pregnant women and small children.
- **Power is less of a worry now.** No load shedding for 408 days by June 2026, though Gauteng still has local "load
  reduction" cuts. So "cook without power" is an option on request, not a default (this corrects the F48 page).
  ([EnergyBee](https://energybee.co.za/news/eskom-408-days-no-load-shedding-june-2026))
- **Healthy, in SA words.** The national food-based dietary guidelines are 11 plain messages, for example "Make starchy
  foods part of most meals", "Eat dry beans, split peas, lentils and soya regularly", "Have milk, maas or yoghurt every
  day", "Use salt and food high in salt sparingly".
  ([FAO](https://www.fao.org/nutrition/education/food-dietary-guidelines/regions/south-africa/zh/))
  They're official and free to quote, and they let the cook nudge without stating a single nutrition number.
- **Channel.** WhatsApp is SA's most-used messaging app, and most people are on prepaid data they buy weekly. The data
  I found is old (a 2020 web survey), so treat it as direction, not a number.
  ([GeoPoll](https://www.geopoll.com/resources/south-africa-smartphone-internet-usage/)) Keep answers light: the photo is
  optional, and the list is text you can copy.

## What the teaching research adds

The F48 page already has the core (process over recipe, cues not just timers, one why per pitfall, levels, mistake to
cause, one lesson to keep). Three more things:

1. **Confidence is measurable.** Lavelle et al. (2017) built and validated two short questionnaires: 14 cooking
   skills (boiling, steaming, frying, chopping vegetables, making a sauce from scratch...) and 19 food skills
   (planning, shopping, budgeting, using leftovers, reading labels), each rated 1 ("very poor") to 7 ("very good").
   Published CC BY 4.0. ([IJBNPA](https://ijbnpa.biomedcentral.com/articles/10.1186/s12966-017-0575-y)) We'd use three
   of the questions, in our own words, to set the level, and ask again after a few dishes.
2. **Confidence grows from small wins.** Self-efficacy (believing you can do it) grows most from doing it and
   succeeding. So the first dishes for a beginner are ones that can't easily fail, and the cook says what went right.
   (Bandura's theory, the basis of most cooking-skills programmes.)
3. **Feedback closes the loop.** Courses that only show and never ask how it went teach less. "How did it go?" after a
   dish gives the cook something to teach from, and gives us real failures for the evals.

## AI techniques that help

- **Grounding in our own recipes (retrieval, skill 8).** F35 records every recipe page it reads. Those pages become
  our own vetted SA recipe library: search it first, the web second. It's faster, cheaper and gives the same answer
  every run. Embeddings come later, once keyword search falls short.
- **Swaps the right way (hybrid).** The research survey on ingredient substitution says: hard rules in code for what
  must never be wrong (allergens, halaal, vegetarian), a curated table of good swaps, and the model only ranking them
  and saying why. The ingredient's job in the dish decides the swap (an egg that binds is not an egg that glazes), and
  there's no standard benchmark, so we build our own labelled set.
  ([survey](https://arxiv.org/html/2501.01958v1))
- **Guards in code.** The safety guard and the allergy rules are the "guards in code enforce" principle from ADR
  0020, applied to food.
- **Vision (skill 19)** for a till slip (fills the pantry with dates and real prices) and "does this look right?". Never
  for safety: a photo can't tell you chicken is at 74 °C.
- **Voice.** The browser's own speech recognition can do "next step" and short questions in cook mode; Afrikaans
  support differs by browser, so English first. Voice notes in are what Penny does.
- **Evals from real use (skill 12, 16).** Every "how did it go: not well" becomes a candidate scenario, the 6b pattern
  ("drawn from real failures").

## The 18 ideas

Each: what it is, why, how it fits, and what it costs. *Steps* are commits of about ten minutes' review.

### Wave 1: with F48 (cheap, high value)

| # | Idea | Why | How it fits | Cost |
|---|---|---|---|---|
| 1 | **Food-safety guard** | Users miss AI recipe flaws; listeriosis | C# rules on the recipe record: poultry → "74 °C in the thickest part, juices clear", mince → 71 °C, egg dishes → 71 °C, leftovers → reheat to 74 °C and fridge within 2 hours; ready-to-eat meats → a note for pregnancy and small children. The rule adds the line; a grader checks it. | 2 steps, no model cost |
| 2 | **Cheaper pack size** | Budget | From Stats SA's table: maize meal 5 kg R65.26 is R13.05/kg, 2.5 kg R38.53 is R15.41/kg. Code says "the 5 kg is 15% cheaper per kg" when the user will use it. | 1 step |
| 3 | **Healthy nudges in SA words** | Health without numbers | One guideline message where it fits ("add a veg: *eat plenty of vegetables and fruit every day*"), from a small table of the 11 messages, credited. | 1 step (role + table) |
| 4 | **Cook mode in the Puls app** | The competition's best feature | A page for one recipe: mise en place checklist first, one step on screen, a timer per step, the screen stays on (the browser's wake lock). Reads the `WriteRecipe` result, so no new model call. | 3 to 4 steps (Razor, bUnit tests) |
| 5 | **List to share or copy** | WhatsApp is how SA shares | A "copy list" button that gives plain text ready to paste; no new data. | 1 step |
| 6 | **"How did it go?"** | Feedback, and evals from real use | After a recipe, one tap (good / so-so / not good) and an optional line; stored with the browser id; "not good" opens troubleshooting and goes to the eval inbox. | 2 steps |

### Wave 2: one feature next ("cook for the week")

| # | Idea | Why | How it fits | Cost |
|---|---|---|---|---|
| 7 | **A week on a budget** | "R900 for 4 for a week" is the SA ask | Five dinners, one shopping list, shared ingredients (half a cabbage on Monday, the rest on Wednesday), perishables early, leftovers on purpose. The list and total from code. | 4 steps |
| 8 | **Use up leftovers** | A third of food is wasted | "I have leftover pap and chakalaka": the pantry plus a dish that uses it, with the safety rule (reheat to 74 °C). | 1 to 2 steps |
| 9 | **Cheap this month** | Seasons and prices | Stats SA's monthly series shows which vegetables dropped in price; the cook prefers them. Code, from data we have. | 2 steps |
| 10 | **Diet and allergy rules** | Must never be wrong | A profile (halaal, no pork, vegetarian, nut allergy) with hard rules in code on the recipe record; the model can't override them. | 2 to 3 steps |
| 11 | **Swaps that fit the dish** | Missing or pricey items | The hybrid: our SA swap table plus rules, the model ranks and says why; a labelled test set. | 3 steps |
| 12 | **Fix what went wrong, and a confidence check** | The teacher half | Troubleshooting scenarios ("my pap is lumpy") with cause and fix; three confidence questions set the level and are asked again after five dishes. | 3 steps |

### Wave 3: waits for a building block

| # | Idea | Waits for | Note |
|---|---|---|---|
| 13 | **Our own recipe library** | F35's recorded pages; keyword search first, embeddings later (skill 8) | Same answer every run; cheaper |
| 14 | **Ask about this step** | Cook mode (4) | The chat knows the step you're on; a cheap model call |
| 15 | **Hands-free voice** | Cook mode (4); browser speech | English first |
| 16 | **Photo: does it look right?** | Vision (after F35) | Never for safety |
| 17 | **Voice notes in, WhatsApp out** | Phase 9's front door (`louis-agent.puls.gateway`) | Where SA actually is |
| 18 | **Dishes you've mastered** | Accounts (Phase 9) | A progress view; the browser id can start it |

## Value and cost, side by side

## What it costs to run

- **Wave 1 adds almost nothing per answer.** The guard, pack size and nudges are code and small tables. The
  nudge line is about 20 output tokens. Cook mode reads the answer we already have. On Haiku 5.5 ($0.50 per million
  output tokens, `config/prices.json` as of 2026-10-10) that's well under a hundredth of a cent.
- **"How did it go?"** is a tap, stored; a "not good" that opens troubleshooting is one normal cook turn, about a cent.
- **The week planner** is one longer answer (five dishes): a few cents a plan, measured before it ships.
- **Evals:** each wave adds scenarios (safety should-nots, a budget week, allergy refusals). Their paid runs wait for
  Louis's go, as always.

## Where it sits in the skill map

| GOAL skill | What this adds |
|---|---|
| 4 · Tool design | Pack-size and safety as code the cook calls through `WriteRecipe` |
| 8 · Memory and retrieval | Our own recipe library from F35's recorded pages; the browser id's history |
| 12 · Evaluation | Safety should-nots, budget weeks, allergy refusals; real failures from "how did it go" |
| 15 · Safety | A food-safety guard and diet rules in code, checked by a grader |
| 16 · Human in the loop | The user's rating feeds the eval inbox; Louis labels |
| 19 · Multimodal | Till slip and "does it look right" photos; voice in cook mode |

## Sources

- [Business Day: grocers bet you will ask the algorithm](https://www.businessday.co.za/companies/2026-07-15-news-analysis-grocers-bet-you-will-ask-the-algorithm/) (2026-07-15)
- [Checkers Sixty60 opens Pixie to all](https://newcastillian.com/2026/07/26/checkers-sixty60-opens-tinder-for-groceries-ai-assistant-to-all-customers/) (2026-07-26)
- [The best AI cooking apps in 2026](https://glutt.org/best-ai-cooking-apps) (by one app's founder)
- [Households cut back on nutrition (PMBEJD, March 2026)](https://www.timeslive.co.za/news/south-africa/2026-04-08-households-forced-to-cut-back-on-nutrition-as-costs-outpace-income/)
- [How to reduce food waste (Daily Maverick)](https://www.dailymaverick.co.za/article/2024-04-12-how-to-reduce-food-waste-and-boost-nutrition-security)
- [Listeria-tainted polony, six years on (Food Safety News)](https://www.foodsafetynews.com/publishers-platform-it-has-nearly-been-six-years-since-over-1000-people-were-sickened-and-over-200-died-from-listeria-tainted-polony-in-south-africa/)
- [Safe minimum internal temperatures (FoodSafety.gov)](https://www.foodsafety.gov/print/pdf/node/10)
- [SA food-based dietary guidelines (FAO)](https://www.fao.org/nutrition/education/food-dietary-guidelines/regions/south-africa/zh/)
- [408 days without load shedding (EnergyBee)](https://energybee.co.za/news/eskom-408-days-no-load-shedding-june-2026)
- [Smartphone usage and data costs in SA (GeoPoll, 2020 data)](https://www.geopoll.com/resources/south-africa-smartphone-internet-usage/)
- [Lavelle et al. 2017, cooking and food skills confidence measures (IJBNPA, CC BY)](https://ijbnpa.biomedcentral.com/articles/10.1186/s12966-017-0575-y)
- [A survey on food ingredient substitutions (arXiv 2501.01958)](https://arxiv.org/html/2501.01958v1)
- Stats SA, P0141 *CPI Average Prices*, August 2026 (`data/statssa-average-prices/` in the Claude project)

---
[Docs index](../README.md) · Related: [F48 plan](../plans/F48-cook-and-teacher.md) · [TODO](../../TODO.md)
