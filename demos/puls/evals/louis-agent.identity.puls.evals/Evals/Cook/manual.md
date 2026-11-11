# Cook eval (`puls-cook`): Puls with the cook's tools against Puls with the cook

## What it measures

What does Puls's cook add? The same conversations are held twice: first by Puls alone with all the cook's tools
(`web` and `shops`), which cooks itself, then by Puls as built, which hands a meal to the cook (`puls-cook`). Both have
the same tools, so the difference is the cook's role and its own session. What is counted is what the agents *did* (who
handed over, which sites were read, whether the answer gives estimates and says where to buy), not only what they
wrote. The cost comes from the ledger, not from the eval.

Since F18 step 5.7 the cook looks up no shop's prices (ADR 0038: Checkers refuses our browser, and PnP's and
Woolworths' robots.txt disallow their search pages). It gives estimates, marked as estimates, and names the shop's
branch under *Where to buy*.

Since F35.2 the cook no longer reads recipe pages with `FetchUrl` or `ReadRenderedPage`: it searches a recipe site
with `FindRecipes` and reads the hit with `ReadRecipe`, which prints the recipe (ingredients, method, the photo and the
link) from the page's own data, or the page's text when the site publishes none. The cook's prompt names no site that
refuses readers any more: the browser refuses them in code (ADR 0068). `FetchUrl` stays for the map. The recorded
trials below were made before this change and are graded unchanged; the re-recorded ones are step 5's.

## The modes

| Mode | Puls's tools | The cook's tools | What it is |
|---|---|---|---|
| `puls-tools` | `web`, `shops` | — | Puls alone with the cook's tools: can't hand over, cooks itself |
| `puls-cook` | `specialists`, `web` | `web`, `recipes`, `minions` | Puls as built: the web, cooking to the cook |

`specialists` is not something the cook uses: it is Puls's ability to hand over. Without it Puls can only cook
itself, which is `puls-tools`. The cook gets `web` from Puls (the recorded turn shows `toolsets: ["web"]`), and its
role adds `recipes` and `calculator` (F35.2, ADR 0025; it was `shops` from F33 until F18 step 5.7 and F35.2 took them out
of its header). The cook searches with `FindRecipes` and reads the hit with `ReadRecipe`; it uses no minion (ADR 0081).

**What the checks see of a minion** (read in `EngineEvents` and the checks, 2026-11-04, not run on a recording). A
trial's checks read the first-level turn's events and the specialist's, which Puls's stream carries as `sub_session`
events. A minion's own events are one level deeper (the cook's stream carries them, and Puls's stream carries the
cook's), and `tool-used`, `tool-not-used` and `tool-calls-under` unwrap one level only. So the cook's `StartMinion` call
(with `FindRecipes` as its tool) and its `ReadRecipe` call are seen as specialist calls, and `routed-to` reads only
Puls's own first `AskAgent`; but the `FindRecipes` call the minion makes is not seen by `tool-used`, nor are its
results or the sites it searched by the counts that read `ToolResultsOf` and `UrlsGiven`. **A check on the search
names `StartMinion`, never `FindRecipes`:** a `tool-used` on `FindRecipes` would fail even when the search ran.
The identity's own `TurnView` unwraps every level, so a check built on it would see the minion's calls. What the
minion cost is on the run's ledger as a `sub_session` request under the cook's turn.

## The conversations

Each conversation is one session; a second message is the follow-up. Eleven scenarios: eight in the `dev` split and
three held out (a run asks for `dev` unless you say `--split held-out` or `--split all`).

| Conversation | Split | Where | What is asked | Right is |
|---|---|---|---|---|
| `cook-af` | dev | Stellenbosch | Dinner for four under R250 at PnP, recipe and shopping list; then: make it for six | the cook gets it (in `puls-cook`), the basket is within R250, and the follow-up goes back to the same cook |
| `cook-en` | dev | Pretoria (in the message) | An hour's cooking for two at PnP; then: chicken instead of meat | as above, in English |
| `cook-shops` | dev | Stellenbosch | Dinner for four, shopping at PnP and Woolworths | the cook gets it, and says where to buy at both |
| `cook-malmesbury` | dev | Malmesbury | R150 for dinner for four at PnP in Malmesbury | the cook gets it, and the basket is within R150 |
| `cook-no-branch` | dev | Prince Albert | Dinner for two, shopping at PnP in a town where it has no branch | the cook gets it, and the answer says there is no branch there |
| `cook-recipe-only` | dev | Stellenbosch | Just a recipe for bobotie for four; the shop doesn't matter | the cook gets it |
| `cook-asks-first` | dev | — | "What can I cook tonight?", nothing else | Puls itself: it asks first (a control) |
| `control-web` | dev | — | The current prime lending rate, with sources | Puls answers itself (it isn't food: a control) |
| `cook-follow-up` | held out | Somerset-Wes | Dinner for four with chicken at PnP; then: vegetarian, for six | the cook gets it, and the follow-up goes back to the same cook |
| `cook-paarl` | held out | Paarl | Dinner for five, about an hour, PnP | the cook gets it |
| `cook-worcester-budget` | held out | Worcester | R180 for dinner for three at PnP | the cook gets it, and the basket is within R180 |

## The graders

A trial passes only when every **required** grader passes; an Unknown never counts as a pass.

| Grader | Kind | Required | Checks |
|---|---|---|---|
| `cook-no-error` | fixed rule | yes | No turn of the conversation failed |
| `routed-to` | fixed rule | yes | The first turn went where the scenario expects (`routes_to`): a meal to `puls-cook`, a control to Puls itself |
| `within-budget` | fixed rule | yes | The first turn's basket costs no more than the scenario's `budget`; a scenario that sets none passes; an answer with no amount it can read is Unknown |
| `estimates-at-shop` | fixed rule | yes | The conversation gives estimates and says where to buy, and looks up no shop's prices: no shop tool called, no shop page read, no price given as fact, and the answer names the shop's branch under *Where to buy* (or says there is none) |
| `untooled-shops` | fixed rule | yes | No turn's list has a price column for a shop the cook can't read (it reads Checkers through its tools, and PnP and Woolworths as a rendered page); a fail names the shops' sites |
| `recipe-image` | fixed rule | yes | The answer shows an image the recipe page gave, never one the model made up; the `recipe-photos` reader reads the print's `![name](url)` line from `ReadRecipe` (a text print's logos and share icons are left out); when no page the cook read had an image the grade is Unknown, never a Fail |
| `serves-as-asked` | fixed rule | no | Each turn's answer names the number of people the scenario asks for |
| `unpriced-items` | fixed rule | no | Lists the items `EstimatePrices` gave a `?` in the conversation (`tool-rows-marked`: `2 marked ? by EstimatePrices: rotis 5, flaked almonds 100 g`); it only measures, so it always passes and never changes the pass rate |
| `meal-for-people` | model | no | Is the last answer a meal for the number of people asked for? See below |
| `meal-for-people.human` | human review | no | The same question put to a person, in the portal; Pending until someone grades it (F18 step 6a) |

## The model grader and its human review

Besides the fixed rules, one grader is a model, `meal-for-people` (version 2): *is the last answer a meal for the number
of people asked for?* A judge model reads the ask (how many people, the town), the conversation so far when there is one
(a follow-up relies on the recipe before it), and the answer, and says yes, no, or "can't tell". A recipe, for it, is the
steps to cook the dish. It is **not told the budget**: it judges the meal, and `within-budget` judges the cost.

- It is **shown and counted but not required**: the trial's outcome does not wait on it until its answers agree with a
  person's, in each class, at 90% (F18 step 6.3). The first rubric passed that check against the dataset's 36
  labels, but against 60 answers graded in the portal (run `cf37574d0f23`) it failed only 8 of the 15 that were no
  meal: Puls alone refusing, asking a question back, or listing ideas. Since eval version 12 the rubric is an ordered
  decision (a chosen dish with a way to cook it, then a main dish, then enough for the people) that says such an
  answer is `false`, never "can't tell". That caught 14 of the 15 but failed 7 of the 45 meals: follow-ups whose last
  turn is only the list or the cost, and methods written as a paragraph. Version 13 says a dish from an earlier turn
  counts and a short paragraph is a method. The dataset's labels are those 60.
- **Unknown is an answer.** With no judge configured (`EVAL_JUDGE_MODEL` unset), a reply it can't read, or a judge that
  can't tell, the grade is Unknown with the reason, never a guessed pass. A scenario that asks for no number of people
  has no meal to judge and passes without a request.
- Each grade says what judged (`judge_model`, `judge_prompt_hash`), and the judge's requests are in the ledger as
  `grading` on the conversation, so the run's cost includes them.
- `meal-for-people.human` is how you grade the same answers yourself, in the portal, before you see the judge's verdict;
  the run's page then shows how often the judge agreed with you, and *Check the judge now* asks it again with today's
  prompt (see the guide, *Checking the judge*).
- Set the judge with `EVAL_JUDGE_PROVIDER`, `EVAL_JUDGE_MODEL` and `EVAL_JUDGE_ENDPOINT` (see `config/.env.example`);
  the local Ollama model is free.

## What it measures

Each conversation also records what it did. These are measured, never graded, so none of them fails a run; the
grades above are what a trial passes on. (The scores the Python check wrote went in F18 step 7c.)

| Metric | Means |
|---|---|
| `agent_calls` | Puls's own tool calls, over every turn |
| `specialist_calls` | The cook's calls, over every turn |
| `hosts` | The sites pages were read from (Puls's and the cook's), each once, in order |
| `turns` | How many turns the conversation had |
| `seconds` | How long the conversation took |
| `tokens_in`, `tokens_out`, `cost` | Empty for now: the ledger has them, per request |

## What it costs

It spends money: every turn asks Claude, and the judge's requests count too (free on a local model). On Claude
Haiku 5.5 a trial costs about a cent; a `--repeat 3` run of the `dev` split was about $0.26 (F18's plan). The run's own
cost is in the ledger and on its page.

## How to read it

- **On the page:** the run's results by category, then each conversation's grades; the person's review and the judge's
  verdict sit under the turns.
- **In the explorer** (click the run in the list): the cost per mode and per conversation side by side, then a
  conversation's turns on a timeline of model requests, then the turn itself, replayed as it was typed, and one
  request's record.
- Compare `puls-tools` with `puls-cook` on the same conversation: does the cook cost more, and does it say where to
  buy at the right branch?

## By hand

With the Puls containers up, from the repo's root:

```
EVALS=puls BENCHMARK_RUN=local AGENT_API=http://127.0.0.1:5070/ dotnet run --project evals/louis-agent.evals.runner -- run puls-cook --repeat 1
```

Without the portal the run isn't stored; the ledger still has its cost under the tag `local`.
