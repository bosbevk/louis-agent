# 5 · Structured outputs: what F35 taught

> **Skill:** #5 in the [goal](../GOAL.md)'s map · **Stage:** F35 Recipe sources, with its children F35.1, F35.2, F35.4
> and F35.5 (2026-11-03 to 2026-11-05) · **Next:** results between agents typed from a schema (F39), and drafts a person
> approves (F41)

This skill is usually about what a model *writes*: ask for a shape, check it in code. F35 taught the other half first:
what a model *reads*. If a recipe reaches the model as checked data instead of a page of prose, its amounts can be
trusted, and a program can test the answer against them.

## Before and after

**Before F35** the cook wrote recipes from search snippets and page text, and nothing could say whether they were right.
There was no check on it, because there was nothing to compare with.

**After F35** the cook searches recipe sites and reads a page with `ReadRecipe`, which prints the page's own data as
numbered lines. The eval then asks two things it couldn't ask before: did the recipe come from a page the cook read, and
is it that page's recipe?

All runs are the recipes eval, 3 repeats of 10 dishes, 30 answers each:

| Run | What it was | From a page it read | Judge: it's that page's recipe | Time | Cost an answer |
|---|---|---|---|---|---|
| `0722c7408705` | First measure, no minion | 25 | 28 | 60 s | $0.0070 |
| `8d9c642016ac` | With a minion on Haiku | 24 | 24 | 70 s | $0.0075 |
| `79ea99b3ec08` | Report shown as it is, calculator, no minion | 30 | 27 | 52 s | $0.0059 |
| `713d617be18b` | The same commit, with a local minion | 30 | 27 | 60 s | $0.0063 |

**The final code was not measured.** The two runs started after those fixes were both stopped, and F35 was closed on
Louis's call. So nothing here proves what the last fixes did to accuracy, time or cost. Those fixes are: more sums for the
calculator, whole eggs, servings as people, no shop question, the shop lookup first, the photo check and map links.

## What changed

- **Data first, text second** ([ADR 0067](../adr/0067-a-pages-own-data-before-its-text.md)). Many recipe pages hide a
  schema.org `Recipe` in their HTML for search engines. `SchemaOrgRecipe` is a Data Mapper: sites write the same recipe in
  many shapes, and this is the one class that knows them, so the rest of the pack only sees a `Recipe` record.
- **Whole or not at all.** A `Recipe` with no ingredients or no steps isn't returned. The page is read as text instead,
  and the print says so, so the model never takes a page's prose for a recipe the page marked up.
- **A numbered print, not JSON.** A model quotes a line by its number and reads a list better than braces.
- **The check can read the same thing.** Because the print is exact, a judge can be shown the page and the answer, and a
  fixed rule can test the link. Structure is what made this measurable at all.

## What didn't work

- **Structured data can be wrong.** Taste's data says `1 serving` on a bobotie whose page says *Serves 6 to 8*. The judge
  believed it, and wanted 4 kg of mince for four people. Reading data first is a default, not a promise, so a yield of
  exactly one now gives way to the page's own words. The Food Fox's data names a photo on a server that no longer answers.
- **Broken data stays broken.** We tried reading Boerekos's JSON with a stray comma allowed. The real problem was a double
  comma, and the data had no steps anyway, so we took the change back out. The fix would have helped no real page.
- **A minion that saves nothing.** A free local model doing the search sounded like an easy saving. The search was only
  about 7% of an answer, and the whole answer came out 7% dearer and 8 s slower, at the same accuracy
  ([ADR 0081](../../../demos/puls/docs/adr/0081-the-cook-searches-for-itself.md)). Measure the whole answer, not the part
  you changed.
- **A check that passes broken things.** `recipe-image` asked where an image came from, never whether it loads, and passed
  8 of 55 images that wouldn't load from this PC. Some of those may load in a browser, which is why it's only a sign.
- **The photo check broke working photos** (FX46). The first version counted any failure as *gone*. Food & Home's photos
  sit on a host whose `robots.txt` answers 500, so our rules forbid the fetch, and good photos vanished: 3 of 5 answers had
  one, against 27 of 30 before. The unit tests passed, because their fake fetcher had no site rules, and Louis caught it by
  looking at the answers. "We may not check" isn't "broken".

## What it taught, in one line each

- **Put the rule where it can hold.** Arithmetic in the prompt kept slipping (1 teaspoon × ⅔ as 2⅔), and code doesn't
  ([ADR 0080](../adr/0080-a-tool-when-it-improves-the-prompt.md)). A tool still needs using: the cook called the calculator
  in 13 of 30 answers.
- **Don't pay a model to copy.** Puls's answer was about 96% the cook's report, at the output rate. Showing the report as
  it is removed a whole request ([ADR 0079](../adr/0079-show-a-specialists-report.md)).
- **A judge has to be able to tell.** The local judge caught 5 of 12 answers we broke on purpose, and the paid one all 12.
  Try a judge on known-bad answers before trusting its passes.
- **Read a number with its noise.** The paid judge agrees with itself on about 25 of 30, so 27 against 28 is no difference.

## What's left

Typed results between agents and validated drafts are F39 and F41, which is the half of this skill most people mean. The
recipe library, with our own copies as one more source, is F35.3. A number out of a yield (*4 to 6* stays words) is
F48.1's scaler.

---
[Docs index](../README.md) · [Goal](../GOAL.md) · [All write-ups](README.md)
