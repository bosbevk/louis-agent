# Long recipes eval (`puls-recipes-long`): thirty more dishes, baking among them

## What it measures

The same question as the recipes eval (`puls-recipes`), on dishes it doesn't ask: does Puls's cook write the recipe a real page gave? Ten dishes there run out, so the library's search and the reader only ever see those ten. Here fifteen are baking and sweet things and fifteen are everyday dishes, half in Afrikaans, half in English.

Ten of the baking questions name no people, because a cake is made whole: Puls should hand those on without asking how many, and the cook should keep the recipe as it's written. Their `serves` is `as the page makes it`, so the judge is still asked, and the rubric takes the factor as 1.

The reference transcripts start as short written ones and are meant to be replaced with real ones from a run. It is dev only, with nothing held out.

## The mode

One mode, `puls-cook`: Puls as built (`puls/voice-af`, toolsets `specialists` and `web`), which hands the meal to its
cook. The cook's own tools are its role's (`recipes`, and `web`), as for the cook eval.

## The conversations

Thirty scenarios, one session each and one turn, all in the `dev` split. Fifteen questions are in Afrikaans and fifteen in English.

| Kind | Dishes |
|---|---|
| Baking, made whole, no number | chocolate cake, carrot cake, lemon meringue tart, peppermint crisp tart, banana bread, rusks, brownies, scones, hertzoggies, crunchies |
| Baking and sweet, with a number | apple tart, cupcakes, pumpkin fritters, souskluitjies, roosterkoek |
| Everyday | bolognese, lasagne, butter chicken, Cape Malay curry, samoosas, frikkadelle, bunny chow, oxtail, pickled fish, butternut soup, cottage pie, roast chicken, macaroni cheese, fish cakes, braaibroodjies |

Each scenario's `expected` holds `routes_to` (always `puls-cook`), `serves` (the people asked for, or `as the page makes it`) and `recipe_image`
(always true). A dish whose site has no photo is not a fail: see `recipe-image` below.

## The checks

A trial passes only when every **required** check passes; an Unknown never counts as a pass.

| Check | Kind | Required | Checks |
|---|---|---|---|
| `no-error` | fixed rule | yes | No turn of the conversation failed |
| `routed-to` | fixed rule | yes | The meal went to `puls-cook` |
| `tool-used` | fixed rule | yes | `ReadRecipe` was called, by the cook or by Puls. Only this tool is named: the cook calls it with and without the `find-recipe` minion, so one eval grades both runs |
| `recipe-from-source` | fixed rule | yes | The link under the recipe (`Skakel:` or `Link:`) is on a site of the recipes pack's table, or elsewhere when the answer says under *Unsure* that it came from a web search; a fail names the host it was on instead |
| `recipe-image` | fixed rule | yes | The answer shows an image a page the cook read gave (`image-shown` over the `recipe-photos` reader); when no page the cook read had an image, the grade is Unknown, never a fail |
| `unpriced-items` | fixed rule | no | Lists the items `EstimatePrices` gave a `?` in the conversation (`tool-rows-marked`: `2 marked ? by EstimatePrices: rotis 5, flaked almonds 100 g`); it only measures, so it always passes and never changes the pass rate |
| `recipe-as-source` | model | no | Are the answer's ingredients, amounts and steps the page's, scaled to `serves`? See below |
| `recipe-as-source.human` | human review | no | The same question put to a person, in the portal; Pending until someone grades it |

## The judge and its review

**It needs a paid judge.** On 2026-11-05 the local `qwen3:4b` caught 5 of 12 answers broken on purpose and no changed amount; Haiku 5.5 caught all 12 and agrees with a person's labels on 27 of 30, at about a tenth of a cent an answer (FX45). **Check it with a re-score, not *Check the judge now*:** that button sends the answer without the pages it read, and this judge fails an answer with no page, so it reports nonsense here. Include answers you know are wrong: a judge only ever shown good answers can't show that it catches bad ones.

`recipe-as-source` is the general `judge` with `"sources": ["ReadRecipe"]`: besides the answer and `serves`, it is
sent every result `ReadRecipe` gave in the conversation, each labelled and in the order the cook read them (a result is
cut at 12,000 characters, and the request says so). The rubric tells it to judge against the result whose own `Link:` is
the answer's link, so a cook that read two or three pages is judged against the one it used. It says yes when the
ingredients, amounts and steps are that page's, scaled by one factor, with no ingredient or step added or dropped. The
cook's report has no ingredient list under *Recipe* (the amounts are in its shopping list), so the rubric reads the
list's *For the recipe* column. A method written in the task's language, step for step with every number, time and temperature unchanged,
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

F35.2's step 5 recorded it twice, on Louis's go, with 3 repeats each, and the numbers are in the
[write-up](../../../../../../src/docs/learning/05-structured-outputs.md):

1. **Without the minion:** the stack rebuilt with `RECIPE_SOURCES`, the cook alone searching several sites at once with `FindRecipes` and
   reading with `ReadRecipe`.
2. **With the minion:** the `find-recipe` minion merged, the stack rebuilt, the same eval run again. The minion lost that
   comparison and went (ADR 0081), so the eval now runs the cook alone.

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
| `recipe-from-source` | The link is on a site the table doesn't have and the answer doesn't say it came from the web, or there is none |
| `recipe-image` | The answer shows no photo though the print had one, or one the print did not give. `ReadRecipe` drops a photo it knows is gone, so a pass means it wasn't known to be broken, not that it certainly loads (version 3) |
| `recipe-as-source` | Open its reason: the ingredient or step it says is added, dropped or changed, and the page it compared with |

## Run it

A run talks to the API and spends money (the cook's, and the judge's), so it waits for Louis's go:

```
EVALS=puls BENCHMARK_RUN=local AGENT_API=http://127.0.0.1:5070/ dotnet run --project demos/puls/hosts/louis-agent.puls-eval-runner -- run puls-recipes --repeat 1
```
