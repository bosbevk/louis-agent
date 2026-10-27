# 12 · Evaluation: a judge is only as good as its last check against a person

> **Skill:** #12 in the [goal](../GOAL.md)'s map, with #16 (human in the loop) and a nod to #15 (least privilege) · **Stages:**
> F18 steps 6.3 to 6a.5, the judge calibrated and then checked from the portal (2026-10-26 to 27) · **Written by** Claude, for Louis
> to read (2026-10-27) · **Next:** the rest of F18's write-up (graders by kind, rates, replay) extends this one; the first
> calibration of the next feature is the button's second real use

What this answers: how we know the model that grades the cook's answers sees what you see, what it took to get it there, and
why the check is now a button in the admin portal instead of four commands in a terminal.

## Why a judge needs checking

Some things code can't grade. "Is this recipe a meal for four?" has no rule. So a model grades it, and that grade is a
measurement like any other. A measurement nobody has checked is just a number that looks sure of itself.

The check is simple. You read answers and say pass or fail. The judge reads the same answers. Then you count how often it
said what you said.

## Before and after

| | Before | After |
|---|---|---|
| Checking the judge | Grade in the portal, download a zip, unzip it into the dataset, run a command in a terminal | Grade in the portal, press *Check the judge now* |
| What the judge saw | The last answer only, and the budget | The whole conversation, and no budget |
| What it counted as a recipe | A dish name with a shopping list | The steps to cook it |
| The result | A paragraph of text in a terminal | Sentences in the card: when, with which judge, how often it agreed, whether it clears the bar, and each answer it got wrong with its reason |
| The history | Gone when you closed the terminal | Every check kept, the latest on top, the earlier ones folded |

## The four rounds on your 36 labels

You graded 36 real answers: 27 passes, 9 fails. The bar is 90% in each class, not 90% overall. The reason: a judge that says
"pass" to everything would agree with all 27 passes and look 75% right without judging anything. So the fails count on their
own.

| Round | What changed | Agreed | What it showed |
|---|---|---|---|
| 1 | Nothing yet | 35 of 36, fails 8 of 9 (89%) | One fail missed, a dish name with a list and no steps. Just under the bar. |
| 2 | The prompt now says a recipe is the steps to cook it | 32 of 36 | Fails now 9 of 9, but four passes flipped. One was a follow-up that swapped the meat: the judge only saw the last answer. |
| 3 | The judge sees the whole conversation | 32 of 36 | The follow-up was right, but all four misses said "over budget". |
| 4 | The judge is not told the budget | 35 of 36 | Pass. 96% and 100%. |

Two of those fixes are worth remembering because they weren't prompt tricks.

- **Show the judge what the answer relies on.** A follow-up like "make it for six" only makes sense with the recipe from the
  turn before. Judging it alone was judging the wrong thing. The labels had to carry every turn too, or the check would
  measure a different question from the one the judge faced in a real run.
- **A model can't misuse what it isn't told.** The prompt said "never judge the price" in bold. The small judge judged the
  price anyway. Taking the budget out of what it is sent worked where the instruction didn't. Another grader checks cost, so
  nothing was lost.

## What the wobble means

The same code and the same 36 labels gave 36 of 36 one night and 35 of 36 the next morning. That isn't a bug. A small local
model still varies a little, even at temperature 0. So a result right at the bar is worth running twice, and one miss in
36 is not a signal. The last miss is the same answer both times: a six-person follow-up where you passed it and the judge
wants cooking steps. That is a question about what counts as a meal, and only you can settle it.

## Why a button, and how it works

The terminal loop was fine for one person tuning one prompt. It stopped being fine on the fourth round. And the portal
already had your grades in its store, so zipping them out to read them back in was a detour.

The interesting part is how the button works, because the obvious way is wrong. The API can't just call the harness: the
two are kept apart on purpose, so one never depends on the other's code. So the button does what a re-score already does.
It starts the eval runner as a separate process, writes your grades to its input, one per line, and reads one line of data
back that holds the whole report. The API keeps that report in its own small store, next to your grades, and never in the
runs list, because a check of the judge isn't a run of the cook.

That is the general pattern for a new harness function reaching the portal: **a process, data in, data out.** It costs a
process start, and it buys a clean wall between the two halves.

## What didn't work, or nearly didn't

- **A test that passed for the wrong reason.** The first version of "the card stops asking when you leave the page" would
  have passed even with the stopping code deleted, because freeing the cancellation token made the loop crash, which also
  ended it. A crash that happens to stop a loop isn't a stop. The fix was to not free the token, since it has no timer.
- **A first allowlist that dropped the proxy settings.** See the next section.
- **Test helpers hold the screen.** The test library's wait-for-assertion helper blocks the very renderer the polling loop
  needs, so the polling test waits plainly instead.

## A side lesson, from skill 15: what a child process can see

Starting the runner copied the API's whole environment, so an eval could read the database login and every provider key,
though it reads about a dozen settings. Fixing it (FX25) is the usual least-privilege move: say what the process may see, not
what it may not. The list is short and doubles as documentation of what the runner depends on.

It cost one thing to learn. A first draft of the list dropped the proxy and certificate settings, so behind a company proxy
the judge couldn't reach Anthropic. And the test for those found a real bug: the selection treated `HTTPS_PROXY` and
`https_proxy` as one variable, when on Linux they are two. A test that scans the runner's code for the settings it reads
now stops a setting going missing quietly.

## The limits, said plainly

- **36 labels, one judge, one person.** Nine fails is "few labels" by the card's own rule. A pass at 100% on nine is
  weaker than it sounds.
- **The prompt was tuned on the same 36 answers it is scored on.** That was your pick, and it is how the four rounds worked,
  but it means 35 or 36 of 36 shows the prompt fits *those* answers. It does not prove it works on new ones. Check it on
  fresh labels: the next grading round, or the new scenarios F48 brings.
- **The labels are yours alone.** A second reviewer's grades would be a stronger check and the store already allows them.
- **A prompt change means a rebuild.** The judge's prompt is baked into the image, so the loop is edit, rebuild, press.
- **The button hasn't been pressed in anger on a new prompt yet.** Its first real use (35 of 36, 2026-10-27) was on the
  prompt we had already tuned.

---
[Learning](README.md) · [Docs index](../README.md) · Related: [F18](../features/F18-evaluation.md) · [FX25](../fixes/FX25-runner-environment.md)
