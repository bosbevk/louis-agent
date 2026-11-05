# Recipes eval (`puls-recipes`): the cook reads a recipe from a real page

## What it measures

Does Puls's cook write the recipe a real page gave? Each conversation asks for a recipe for one South African dish
and some people, in Afrikaans or English, and says any shop will do, so the answer's work is the recipe (the cook still names the supermarkets nearest them). The cook
should find the dish on a recipe site, read the page with `ReadRecipe`, and write its ingredients and method scaled to
the people, with the page's photo and link. What is counted is what the cook *did* (which tool it read the page with,
which site its link is on, whether its photo came from the page) and then, with a model's help, whether its amounts
and steps are the page's.

It is the eval of F35.2 (the cook on the recipes pack). The cook eval (`puls-cook`) asks a bigger question (a meal
for the people, at a shop, in a budget) and stays as it is; this one asks only about the recipe.

## The mode

One mode, `puls-cook`: Puls as built (`puls/voice-af`, toolsets `specialists` and `web`), which hands the meal to its
cook. The cook's own tools are its role's (`recipes`, and `web`), as for the cook eval.

## The conversations

Thirteen scenarios, one session each and one turn: ten in the `dev` split to tune against, three held out (a run
asks for `dev` unless you say `--split held-out` or `--split all`). Five of the ten dev questions are in Afrikaans and
five in English; of the held-out ones, two are Afrikaans and one English.

| Split | Dishes |
|---|---|
| dev | bobotie, melktert, chakalaka, potjiekos, malva pudding, vetkoek, boerewors rolls, pap en sous, hoenderpastei, koeksisters |
| held out | umngqusho, sosaties, bredie |

Each scenario's `expected` holds `routes_to` (always `puls-cook`), `serves` (the people asked for) and `recipe_image`
(always true). A dish whose site has no photo is not a fail: see `recipe-image` below.

## The checks

A trial passes only when every **required** check passes; an Unknown never counts as a pass.

| Check | Kind | Required | Checks |
|---|---|---|---|
| `no-error` | fixed rule | yes | No turn of the conversation failed |
| `routed-to` | fixed rule | yes | The meal went to `puls-cook` |
| `tool-used` | fixed rule | yes | `ReadRecipe` was called, by the cook or by Puls. Only this tool is named: the cook calls it with and without the `find-recipe` minion, so one eval grades both runs |
| `recipe-from-source` | fixed rule | yes | The link under the recipe (`Skakel:` or `Link:`) is on a site of the recipes pack's table; a fail names the host it was on instead |
| `recipe-image` | fixed rule | yes | The answer shows an image a page the cook read gave (`image-shown` over the `recipe-photos` reader); when no page the cook read had an image, the grade is Unknown, never a fail |
| `unpriced-items` | fixed rule | no | Lists the items `EstimatePrices` gave a `?` in the conversation (`tool-rows-marked`: `2 marked ? by EstimatePrices: rotis 5, flaked almonds 100 g`); it only measures, so it always passes and never changes the pass rate |
| `recipe-as-source` | model | no | Are the answer's ingredients, amounts and steps the page's, scaled to `serves`? See below |
| `recipe-as-source.human` | human review | no | The same question put to a person, in the portal; Pending until someone grades it |

## The judge and its review

`recipe-as-source` is the general `judge` with `"sources": ["ReadRecipe"]`: besides the answer and `serves`, it is
sent every result `ReadRecipe` gave in the conversation, each labelled and in the order the cook read them (a result is
cut at 12,000 characters, and the request says so). The rubric tells it to judge against the result whose own `Link:` is
the answer's link, so a cook that read two or three pages is judged against the one it used. It says yes when the
ingredients, amounts and steps are that page's, scaled by one factor, with no ingredient or step added or dropped. The
cook's report has no ingredient list under *Recipe* (the amounts are in its shopping list, and pantry staples are one
line with no amounts), so the rubric reads the list's *For the recipe* column and does not count a named pantry staple
as dropped. A method written in the task's language, step for step with every number, time and temperature unchanged,
counts as the page's. No page read, or one that is no recipe, is a no.

- It is **shown and counted but not required**, and it starts with **no labels**: nobody has yet graded its answers, so
  its score is not yet a measure. Until a person has labelled a sample in the portal through `recipe-as-source.human`
  and *Check the judge* has agreed in each class, read its reasons, not its number.
- A page that came back as its text (32 of the 45 sites) has menu links and comments among its words and may be cut at
  8,000 characters; the rubric reads the recipe out of it and does not call what is past the cut added. That is the
  hardest case for the judge, and the one to look at first in the review.
- Unknown is an answer: no judge configured (`EVAL_JUDGE_MODEL` unset) or a reply it can't read is Unknown with the
  reason. Each grade says what judged and with which rubric (`judge_model`, `judge_prompt_hash`). A change to what the
  judge is sent (the `sources`) bumps the judge's `version` in `eval.json`, since the rubric's hash does not cover it.

## The two runs this eval is for

F35.2's step 5 records it twice, on Louis's go, with 3 repeats each, and the numbers go into the measure table of the
F35 plan (`docs/plans/F35-recipe-sources.md`):

1. **Without the minion:** the stack rebuilt with `RECIPE_SOURCES`, the cook alone searching with `FindRecipes` and
   reading with `ReadRecipe`.
2. **With the minion:** the `find-recipe` minion merged, the stack rebuilt, the same eval run again.

They are compared on passes, whether `ReadRecipe` was used, the cost per answer and the minion's share of the cost
and the time (the ledger's, not a check's).

## No recordings yet

This eval has never run: its `Transcripts/` hold hand-written conversations that prove the checks (a reference that
passes every required check and known-bads that each fail one), not recordings of the cook. The first real run is
step 5's, and records afresh. Its answers are then the ones to label for the judge.

## What to do when it fails

| Check that failed | Usually means |
|---|---|
| `tool-used` | The cook wrote a recipe without reading a page, or read it with another tool. Look at its tool calls |
| `recipe-from-source` | The link is on a site the table doesn't have, or there is none |
| `recipe-image` | The answer shows no photo, or one the page did not give |
| `recipe-as-source` | Open its reason: the ingredient or step it says is added, dropped or changed, and the page it compared with |

## Run it

A run talks to the API and spends money (the cook's, and the judge's), so it waits for Louis's go:

```
EVALS=puls BENCHMARK_RUN=local AGENT_API=http://127.0.0.1:5070/ dotnet run --project demos/puls/hosts/louis-agent.puls-eval-runner -- run puls-recipes --repeat 1
```
