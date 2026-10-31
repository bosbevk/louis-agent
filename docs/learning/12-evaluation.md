# 12 · Evaluation: a judge is only as good as its last check against a person

> **Skill:** #12 in the [goal](../GOAL.md)'s map, with #16 (human in the loop), a nod to #15 (least privilege) and side
> lessons for #17, #4 and #3 · **Stages:** F18 steps 6.3 to 6a.5, the judge calibrated and then checked from the portal
> (2026-10-26 to 27); the rest of F18 and its first v2 baseline (2026-10-29); F49.2b, the generic checks (2026-10-31)
> · **Written by** Claude, for Louis to read (2026-10-27, extended 2026-10-29 and 2026-10-31)

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

## The rest of F18, in the field's words

The field has settled on a few words, and F18 uses them as they are, so a paper or a library reads the same way.

- **A task** is one thing you want the agent to do, a scenario in a dataset. **A trial** is one go at it: one
  conversation. **A transcript** is everything that happened in that trial. **A grader** looks at a transcript and
  says pass, fail or unknown, with a reason. An **eval** is a task set plus its graders.
- **Code graders first.** Most of what matters has a rule: did it route to the cook, is the total under the budget,
  did a turn fail. Code is free, fast and says the same thing twice. Only what has no rule ("is this a meal?") goes to
  a model, and every eval must have at least one code grader that's required, so no eval rests on a judge alone.
- **A judge is checked, then a person stays in the loop.** The judge is calibrated against your labels (above), and
  the human review sits beside it in the portal, blind: you grade before you see what the judge said.
- **Grades and metrics are different things.** A grade decides a pass. A metric (calls, hosts, tokens, seconds, cost)
  is measured and never fails a run. Mixing them was the old "scores" bag; splitting them is what made the v2 results
  readable.

## pass@k against pass^k

Run each task k times. **pass@k** asks: did it work at least once? **pass^k** asks: did it work every time? For a tool
someone tries once and walks away from, pass^k is the honest one. The first v2 cook run shows why both matter:
Puls with its cook passed 23 of 24 trials, and the categories with six trials (budget, follow-up) passed all six, so
both numbers agree there. Puls answering on its own with web tools passed 6 of 24: sometimes it gets there, mostly it
doesn't, and pass^k says so loudly where pass@k would flatter it.

## Datasets with a held-out split

A dataset is files, versioned, with a hash, so a run says exactly what it asked. The scenarios sit in two folders,
`dev` and `held-out`. You tune on `dev`. You never look at `held-out` while tuning; it's for the release check. If you
tune on everything, a high score only proves the prompt fits those questions. Step 7d moved the files to Inspect's
`Sample` shape (`id`, `input`, `target`, `metadata`), so another tool could read them.

## Frozen input, and what a replay is for

The web changes under you. So a run can record every page, search and fetch, and the model's answers too, and a replay
plays them back. The first v2 replays cost $0, every request a cache hit. A replay doesn't test a prompt (a changed
prompt is a new request, so it misses). It tests everything around the model: the graders, the harness, the engine.
It also found a real problem: a mode that hands work to a sub-session doesn't replay, because the hand-off's tool
result changes between runs. That's the kind of thing you only learn by running it.

## Tonight's judge check, a real example of "check it on fresh labels"

The limits below said to check the judge on fresh labels. The first v2 cook run did: 36 new answers, labelled blind by
a separate reviewer who never saw the judge's verdicts. The judge agreed on 31 of 36: 87% on passes, 83% on fails. That
is **below the bar**. Four of the five misses are the same old story: the judge fails a real meal because it wants
cooking steps, when the answer gave a scaled list or a swap. So the 35 of 36 from the tuning rounds was the prompt
fitting those answers, exactly as warned. The judge needs another round, on these labels and then on fresh ones again.

## The limit that stays

CI grades recorded transcripts. That proves the graders and the harness still work. It can't see the agent: a prompt
or tool change only shows up in a real run, and a real run costs money and waits for your go. A gate that sees the
agent is still missing (GOAL, skill 12).

## Side lessons from F18

- **Traces, and logs that don't leak (skill 17).** Every turn and tool call is traced, so a slow or failing step can be
  found. And the logs no longer hold what you typed: the arguments, results and a failure's quoted reason show as keyed
  hashes and lengths (`area=#3f9c0a1b2d4e (10)`), so a log says what ran and how it ended. The same text always gets
  the same hash, so you can still match two lines. `LOG_CONTENT=full` shows the values, on your own machine only.
- **The stream format is a seam (skill 4).** The agent's stream is read by one small library (`louis-agent.session-
  stream`) that turns each server-sent event into a record. Every host, the harness and the portal read those records,
  never the raw text. When the format changes, one reader changes. Protocol v2 is the same idea for evals: one
  contract the runner writes and the API reads (ADR 0047).
- **The prompt's hash (skill 3).** Each run keeps the hash of every prompt each agent used, and the trend chart puts a
  marker where a hash changed. So when a pass rate jumps, you can see whether the prompt changed that night. Comparing
  two prompt versions side by side, as a habit before a merge, is the next step and still missing.

## Generic checks, and readers instead of Puls's own (F49.2b)

Every eval tool (promptfoo, Inspect, OpenAI's evals) ships the same families of check: facts about the run (no error,
time, cost, turns, tokens), rules about the text (contains, starts with, short enough, valid JSON), tool calls, a
compare against an expected value, closeness to a text, and a model judge. Before F49.2b the harness had six of them,
and the cook had six of its own. Now the harness has 24, by family, and any eval names them from its file.

The interesting part is the cook's rules. "The basket is within the budget" looks like Puls knowledge, but most of it
is general: read a number from the answer, compare it with the scenario's. Only the *reading* is Puls's: the rand
total is on an Afrikaans *Totaal* line or the bold row of a list, and a price without a link is a guess. So the check
splits in two. The harness's `amount-under` does the compare, and a **reader** Puls ships (`rand-total`) does the
reading, named in the file beside the key. The field calls this a feature extractor in front of a generic matcher
(Hamcrest's `FeatureMatcher`, promptfoo's `transform`). The same split gave `number-named` with Puls's `servings`
and `image-shown` with Puls's `recipe-photos`. Puls now ships readers, not checks; its file keeps the old names
(`"name": "within-budget"`), so a person reading a run sees what they always saw.

**How we knew the swap changed nothing.** A grader swap is a refactor of a measuring instrument, so it gets the
refactor's test: pin the outputs first, then change the code, and the pins must hold.
1. Before touching anything, `CookGradesTests` wrote every grade's outcome and score on all 26 reference and
   known-bad transcripts to a golden file: 230 grades from the old checks.
2. After the swap the test passed without rewriting the file.
3. Then a replay of the baseline run (the recorded model answers, $0) graded the same as the run it replayed: 140 of
   140 grades equal.

The reasons changed wording, and the version numbers moved, so the eval went to version 11 and its trend starts a
new line; the outcomes didn't move. That's the difference between "it looks the same" and "it measures the same".

**Two families left as heuristics or out.** Language is told by counting common small words per language (*die*,
*en*, *nie* against *the*, *and*, *not*), with no model: free, instant, and Unknown when an answer is too short to
tell. Similarity by meaning needs embeddings, which no eval asks for yet and the provider we use doesn't offer, so it
waits (TODO); `near` covers closeness to a short fixed text with the edit distance.

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
[Learning](README.md) · [Docs index](../README.md) · Related: [the evals design](../projects/louis-agent.evals.md#design-notes) · [FX25](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX25-runner-environment.md)
