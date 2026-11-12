# 16 · Human in the loop: grading is a queue, not a hunt

> **Skill:** #16 in the [goal](../GOAL.md)'s map, with #12 (the eval workflow's portal) and a side lesson for #18
> (what an unattended build may decide) · **Stage:** Phase 3, F53 *The admin portal, organised by job*, three
> children in three pull requests (#132, #136 and F53.3's, 2026-11-03 to 2026-11-04), built unattended at Louis's word
> · **Written by** Claude, for Louis to read (2026-11-04) · **Next:** the studio's numbered steps (the TODO's
> leftover); approval policies per action and the approvals screen (F41, Phase 7)

What this answers: why grading answers is now one page with three keys instead of a walk through the runs, what the
build decided on its own and why, and what a person in the loop is for when the judge is already a model.

## What a person is for

A model judge grades every answer. It is checked against people's labels (write-up 12), and that check only means
something while people keep labelling. So the person's job is not to grade everything: it is to grade enough, often
enough, that the judge stays honest. A job like that has to be cheap, or it stops happening.

Before F53, grading one answer meant: open the portal, open Benchmarks, open the run, open Conversations, open the
task, open the mode, find the panel, press Pass. About seven clicks, once per answer, and nothing anywhere said how
many answers were waiting. The review that led here counted it as finding 4 of 8.

## Before and after

| | Before | After |
|---|---|---|
| Where the waiting answers are | In each run's explorer, filtered with *to review* | One page, `/admin/review`, across every stored run |
| Getting to the next answer | Back out, open the next conversation | It opens by itself after a grade; *Skip* moves on without one |
| Grading | A click on Pass, Fail or Unsure | The same buttons, or the keys 1, 2 and 3 |
| Knowing how many wait | Nothing said it | The count beside *Review* in the sidebar, falling as you grade |
| Whose queue it is | — | This reviewer's: an answer leaves once *you* graded it, not once anyone did |

Labelling tools call this shape an **annotation queue** (Label Studio, Prodigy, LangSmith's queues all have it): a list
you never see whole, one item at a time, number keys, a done count. It is the standard shape because the person's
attention is the scarce thing, and every click spends it.

## What the build decided, and why

The build was unattended, so each design finding took the recommended option and said so, for Louis to reverse in
review. Four findings mattered.

**Whose queue (A).** An answer waits for *this reviewer* until they grade it, without a name until anyone does. The
alternative, one queue for everyone, is simpler but a second reviewer would never see what the first graded, and the
judge check wants more than one person's labels on the same answers. The rule is the explorer's *to review* rule,
unchanged, now held in the API (`ReviewQueue`) so that one place says what waits.

**Where the reading happens (B).** The API answers the whole item: the run, the eval, the run's graders, the
conversation's row with its grades and the people's, the questions asked and the last answer. The page reads one
route and hands the item to the explorer's review panel as the explorer does. The cost is that the API reads each
item's turns, so the route takes a `limit` (twenty) and reports the `total` separately, reading turns only for the
items it hands back. The other options (the page joining runs itself, or reading every run) moved that work into the
browser, where it is slower and untested.

**The keys (C).** The page listens for 1, 2 and 3 on its own box and grades through the panel, which stays the
explorer's panel with one addition: it reports a stored grade. Keys inside the panel would have made every panel in
the explorer answer keys too, where nobody expects it. One thing the test caught: a number typed in the note is a key
on the page, so the note and the name field stop their keys from reaching it.

**The count (D).** A scoped state the badge and the page share, the pattern the reviewer's name already uses
(`ReviewerState`): the badge reads the queue once when the portal opens, the page sets the count as it loads and
after each grade. Polling, as the live-run strip does, would spend reads on a number that changes only when this
person grades.

## Blind by construction, still

The review panel hides the judge's verdict until the person has graded (write-up 12: a person who sees the judge's
answer first agrees with it). The queue item carries the judge's grade, because the panel needs it afterwards, and the
panel draws it only after the person's own, as before. Reusing the panel unchanged is what kept the rule: a new panel
for the queue would have had to get it right a second time.

## What the numbers say

The build spent nothing: no run was started, no model was asked. The measure is clicks: seven per answer to one key,
and the next answer open before the hand moves. The count is the other measure. Before, "are there answers to grade?"
had no answer short of opening every run; now it is a number in the sidebar, and zero means done.

## What didn't work, or was left out

- bUnit reports a stopped key (the note's `stopPropagation`) as a missing handler rather than as nothing happening, so
  the test for "typing in the note never grades" asserts that report. It is honest, but it reads oddly.
- The route walks the newest fifty runs, as the runs list does. A store with more waiting answers further back shows
  them only once the newer ones are graded. Good enough for one reviewer; a cursor can come when it isn't.
- The studio's numbered steps (the review's finding 5) were in F53's plan "if the budget allows". They stay on the TODO
  as F53's one leftover.

## Side lesson for #18: an unattended build names its picks

Three pull requests were built, reviewed by their own tests and merged without a person. What made that safe to try:
every finding took the *recommended* option and the pull request named it, so the review Louis does afterwards is a
review of decisions, not an archaeology of them; every step was test-first with the failing run kept; and nothing
paid ran. The pattern is the same as a person's: decide out loud, leave the trail.

## F55: the preview is the check (2026-11-04)

The same idea, used on data instead of grades. Stats SA's average prices are what the cook's estimates stand on, and
Stats SA doesn't release every month; each release also repeats every month back to 2017, sometimes with an earlier
month corrected. Louis: "I need to upload it via admin", and "we need to pick up the corrections".

- **A person, not a schedule, decides a release is in.** The admin portal's Uploads page (`/admin/imports`) takes the
  two spreadsheets. Preview reads them and says what saving would do: the new months, each corrected month with the
  prices that changed, the unchanged ones, and any product with no names yet. Save writes exactly that. Choosing a
  file again hides Save until the next preview, so nothing is written that the person didn't see.
- **No tick.** The first plan replaced a stored month only when a box was ticked. Once the preview listed the
  corrections, the tick added nothing: the preview *is* the check, and a second upload of the same files changes
  nothing. One fewer thing to get wrong.
- **Data as content, not code** (#7, #18). A new release used to be a JSON file made by hand and a build. Now it is
  an operator's upload, and the code that turns sheets into price files is tested against the shipped August table,
  price for price.
- **A plugin seam again.** The API may not know a pack (ADR 0027), so core got a fourth thing a pack can give,
  `IDataImport`, beside toolsets, prompts and feeds; the portal shows every upload without knowing whose it is
  ([ADR 0071](../adr/0071-packs-take-data-uploads.md)).
- **An old price beats no price, if it says so.** A product the newest month lacks is priced from the newest month
  that has it, and the line names that month. Nothing is copied forward, so a stored month stays what Stats SA
  published.
- **Left out:** the files travel twice (preview, then save), fine at a few megabytes; an upload isn't served over MCP,
  since it is a portal job.

## F127.4.1: the visitor's click is the yes (2026-11-12)

The demo businesses passed a booking on after the visitor typed "yes". A typed yes is easy to give to the wrong
question, and a booking read back as one long sentence is easy to miss a wrong date in. Louis: "when the guide must
give options to the user, like you do for me", and the booking summary "the agents must draw this themselves with
our diagram tool".

- **Show it, then ask with cards.** The agent draws what it gathered as a ticket (`ShowCard`), every fact on its own
  row, marked *Not sent yet*, then offers *Book it*, *Change something* and *Not now* (`Offer`). The click comes back
  as the visitor's own message, and the lead goes only then. After it went, the ticket shows again as *Sent*. The
  person checks a picture, not a paragraph, and their yes is one button with one meaning.
- **A tool, not a rule** (#4, ADR 0080). "Ask before you send" was a prompt rule the model could skip. Now a decision
  is a tool call the page draws, checked in code (two to four choices, one suggested) and refused with the reason when
  it is wrong, so the model fixes it (#5, structured outputs).
- **The prompt asks; the page makes it true.** The prompt said "words first, then the card". A small model put the
  card first anyway, so the widget draws an answer's cards under its words, whatever the order. Where the order
  matters to a person, the code holds it, and the prompt only explains it.
- **A refusal is an error.** The tools' refusals start with "Error", so the stream marks the call failed and the
  widget draws nothing. Otherwise the visitor would see the cards the model was told never showed, and then its
  second try as well.
- **Look at it where it is used.** The tests passed with the ticket drawn black: inside `@scope` a bare `svg` rule
  misses the drawing's own root. Only the live check showed it. The 390 px check showed the words at 8 px, and
  Louis's look at a walkthrough showed a stop asking in words where three cards belonged. Three findings that no
  test of ours could see, each now held by one.
- **Left out:** the guided live demo, where the guide runs a business's demo and the visitor carries on in its window,
  is F127.4.2; letting the guide draw any docs diagram is F138.

## F127.4.3: what a person must write, they write in a form (2026-11-12)

Choices were cards, but what only the visitor knows was still asked in words, one thing at a time: a name, then a
contact, then the dates. Louis typed his name into the guide's chat while the guesthouse waited for it in its own
window, and the guide, made to end every answer with choices (FX139), offered a "Describe the job" card that only
said to type. Louis: "can the agent offer a card where I can put my name in?", "and then only that card".

- **Ask with the shape of the answer.** `AskFor` draws a box per field, each suited to what it asks (a phone's number
  pad, an email's keyboard, a box that grows for a job told in their own words), and Send waits until every box is
  filled. The person sees at once what is needed, and answers it in the window that asked. The agent asks only what it
  wasn't told: Seemeeu, told the room and the guests, asked for the name, a contact and the dates.
- **The answer stays a message.** The filled form goes back as the visitor's own words, each field by its label, so the
  model reads it like anything the visitor types, and the history stays plain text (ADR 0008). The form folds to what
  was sent. Nothing new crosses the wire: the web reads the call it already streams.
- **A turn that asks must stop.** Asking ends the turn (`[EndsTurn]`), as offering does, and the engine counts it as the
  answer's close, so the guide's closing choices don't come under a form (#2, the loop: the engine, not the prompt,
  decides when a turn is over). A refused form ended nothing, and then the choices are still owed.
- **The page makes it true again.** The pack's guide says a form is the only card in its answer; the widget holds it,
  drawing no choices beside a form whatever the model sends, as it holds the cards under the words.
- **Left out:** the visitor's message repeats what the folded form shows, as a clicked card's label does today; whether
  either should hide is Louis's call. The 390 px look wasn't checked: the browser window wouldn't resize.

---
[Write-ups](README.md) · [Goal](../GOAL.md) · [12 · Evaluation](12-evaluation.md)
