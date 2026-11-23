# 12 · Evaluation: a judge is only as good as its last check against a person

> **Skill:** #12 in the [goal](../GOAL.md)'s map, with #16 (human in the loop), a nod to #15 (least privilege) and side
> lessons for #17, #4 and #3 · **Stages:** F18 steps 6.3 to 6a.5, the judge calibrated and then checked from the portal
> (2026-10-26 to 27); the rest of F18 and its first v2 baseline (2026-10-29); F49.2b, the generic checks (2026-10-31); F126.1, the old evals deleted, and F126.2, the file and the grading (2026-11-22); F126.3.1, runs, and F126.3.2, the run pages (2026-11-23)
> · **Written by** Claude, for Louis to read (2026-10-27, extended 2026-10-29, 2026-10-31, 2026-11-22 and 2026-11-23)

What this answers: how we know the model that grades the cook's answers sees what you see, what it took to get it there, and
why the check is now a button in the admin portal instead of four commands in a terminal.

![Evaluation: a judge checked against a person, the field's words, generic checks with readers, evals grown from use, and the studio](diagrams/12-evaluation.svg)

Some of what follows moved on after it was written. The judge got its next round: Louis graded 60 answers in the
portal, and the check closed on 2026-11-03 (run `cf37574d0f23`). The harness's general checks are 25 today, not 24.
And the gap a replay couldn't cross, a hand-off to a sub-session, was closed by FX27: a sub-session's answer no
longer carries its usage, so it reads the same in a replay. The cook and recipes evals run live only now for another
reason: the recipe search draws its sites at random
([ADR 0078](https://github.com/bosbevk/puls/blob/main/docs/adr/0078-a-recipe-search-asks-several-sites.md)).

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

Show the judge what the answer relies on. A follow-up like "make it for six" only makes sense with the recipe from the
turn before. Judging it alone was judging the wrong thing. The labels had to carry every turn too, or the check would
measure a different question from the one the judge faced in a real run.

A model can't misuse what it isn't told. The prompt said "never judge the price" in bold. The small judge judged the
price anyway. Taking the budget out of what it is sent worked where the instruction didn't. Another grader checks
cost, so nothing was lost.

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

A test that passed for the wrong reason. The first version of "the card stops asking when you leave the page" would
have passed even with the stopping code deleted, because freeing the cancellation token made the loop crash, which
also ended it. A crash that happens to stop a loop isn't a stop. The fix was to not free the token, since it has no
timer.

A first allowlist that dropped the proxy settings. See the next section.

Test helpers hold the screen. The test library's wait-for-assertion helper blocks the very renderer the polling loop
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

A task is one thing you want the agent to do, a scenario in a dataset. **A trial** is one go at it: one conversation.
**A transcript** is everything that happened in that trial. **A grader** looks at a transcript and says pass, fail or
unknown, with a reason. An **eval** is a task set plus its graders.

Code graders first. Most of what matters has a rule: did it route to the cook, is the total under the budget, did a
turn fail. Code is free, fast and says the same thing twice. Only what has no rule ("is this a meal?") goes to a
model, and every eval must have at least one code grader that's required, so no eval rests on a judge alone.

A judge is checked, then a person stays in the loop. The judge is calibrated against your labels (above), and the
human review sits beside it in the portal, blind: you grade before you see what the judge said.

Grades and metrics are different things. A grade decides a pass. A metric (calls, hosts, tokens, seconds, cost) is
measured and never fails a run. Mixing them was the old "scores" bag; splitting them is what made the v2 results
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

Traces, and logs that don't leak (skill 17). Every turn and tool call is traced, so a slow or failing step can be
found. And the logs no longer hold what you typed: the arguments, results and a failure's quoted reason show as keyed
hashes and lengths (`area=#3f9c0a1b2d4e (10)`), so a log says what ran and how it ended. The same text always gets the
same hash, so you can still match two lines. `LOG_CONTENT=full` shows the values, on your own machine only.

The stream format is a seam (skill 4). The agent's stream is read by one small library (`LouisAgent.SessionStream`)
that turns each server-sent event into a record. Every host, the harness and the portal read those records, never the
raw text. When the format changes, one reader changes. Protocol v2 is the same idea for evals: one contract the runner
writes and the API reads (ADR 0047).

The prompt's hash (skill 3). Each run keeps the hash of every prompt each agent used, and the trend chart puts a
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
refactor's test: pin the outputs first, then change the code, and the pins must hold. Before anything was touched,
`CookGradesTests` wrote every grade's outcome and score on all 26 reference and known-bad transcripts to a golden file:
230 grades from the old checks. After the swap, the test passed without the file being rewritten. Then a replay of the
baseline run (the recorded model answers, for $0) graded the same as the run it replayed: 140 of 140 grades equal.

The reasons changed wording, and the version numbers moved, so the eval went to version 11 and its trend starts a
new line; the outcomes didn't move. That's the difference between "it looks the same" and "it measures the same".

**Two families left as heuristics or out.** Language is told by counting common small words per language (*die*,
*en*, *nie* against *the*, *and*, *not*), with no model: free, instant, and Unknown when an answer is too short to
tell. Similarity by meaning needs embeddings, which no eval asks for yet and the provider we use doesn't offer, so it
waits (TODO); `near` covers closeness to a short fixed text with the edit distance.

## Evals grow from use (F49.4)

The advice every eval guide gives, Anthropic's *Develop test cases* first among them, is: don't invent your test
cases in a quiet room, grow them from what real use turns up. A wrong answer someone noticed is worth ten made-up
questions, because it's a mistake the agent actually makes. Until F49.4 that advice cost a trip to Rider: you saw a
bad conversation in the portal, then wrote its scenario file or its transcript by hand.

Now the portal does it. Two moves, both from a stored run:

A conversation becomes a sample. Press *Sample* on its card. The dialog starts from the scenario the conversation ran
(its place, time zone, expected values and categories), you give the new sample an id and change what you want (cook
for six instead of four, a smaller budget), and it's kept as a draft. Its first question becomes the sample's input
and the later ones its follow-ups. It's a *variant* of a scenario that already works, so the checks know its keys; a
key the scenario doesn't have is refused, because nothing would read it.

A reviewed conversation becomes a transcript. After you grade it, a pass offers *Keep as reference* and a fail *Keep
as known-bad*, with a reason in kebab-case (`no-shop`) that becomes part of its file name. These are the transcripts
the pinned-grades test reads (`CookGradesTests`): a known-bad is a regression test for a grader, an answer it must
keep failing.

**Why drafts, not files.** A draft waits in a store until F49.5 publishes it as files in a pull request. Writing the
file straight into the checkout would be simpler on this PC, but the hosted Puls has no checkout, and a half-made
sample in your working tree is easy to commit by accident. Git stays the truth; the store is a waiting room
([ADR 0061](../adr/0061-evals-grow-from-use.md)).

**The person is the gate.** Nothing becomes part of an eval because a model said so: a transcript is kept only when
people's verdicts back it (every decided verdict a pass for a reference, a fail for a known-bad), and an unsure keeps
nothing. That's the field's "human in the loop" for datasets: the model may suggest, a person decides.

**In the field's words:** this is *dataset curation from production traces* (LangSmith calls it "add to dataset",
Braintrust "log to dataset"); the known-bads are *negative examples* or *regression cases*; and keeping the person
as the gate is how you stop a dataset drifting toward what the judge already likes.

## Compose, try, publish (F49.5)

This is the last piece of skill #12's "evals as data": a new eval is made in the browser, not in Rider. The Studio
page (`/admin/studio`) is a form over `eval.json`. You pick checks from the catalogue, each check's fields drawn from
its settings' schema, and type the rubric. Then you **check** it, **try** it and **publish** it
([ADR 0062](../adr/0062-the-studio-writes-what-git-keeps.md)).

**One reader of eval files, behind a process.** The tempting shortcut was to let the API read and validate an eval
itself. But then two programs would read eval files, and two readers drift: one would accept a file the other refuses.
So the runner, the program that runs evals anyway, is the only one that reads them. The API starts it as a child
process and reads its lines: `--checks` for the catalogue, `--check {folder}` for the verdict. A check is the same
strict load and the same conformance gate CI runs, so "the studio says it's fine" and "CI says it's fine" can't
disagree. The cost is a process start per click, about a second. In the field's words, the runner is a *Facade* over
the harness, and the process boundary is the seam every host goes through.

**One folder for everything.** A draft is written out as one folder (`DraftFolder`) every time the runner must read
it, with the samples and transcripts you kept from runs (F49.4). Check, try, publish and export all read that folder,
so what you checked is what you tried and what you published.

**A try is a replay.** "Try on three samples" replays a stored run on just those samples (`--folder`, `--samples`),
so the model's answers come from the recording and it costs $0. If a question isn't in the recording, it ends as a
replay miss. It never quietly calls the model. This is the *frozen input* idea from F18 again: you're testing the
eval's checks, not the agent, so the agent's answers should hold still.

**promptfoo in and out: an Adapter with a table.** promptfoo is the eval tool people most often already have.
Importing its file is a translation: its tests become our scenarios, its assert types map to our check types by one
table (`PromptfooAssertion`, an enum of promptfoo's names), and an `llm-rubric` becomes a rubric file our `judge`
reads. Anything with no counterpart, such as a JavaScript assert, is **named as left out**, never dropped without a
word. A silent drop would leave you with an eval that looks complete and grades less than you think. Export runs the
same table backwards. The pattern is Gang of Four's *Adapter*: two interfaces that mean nearly the same thing, and
one small class in between that knows the difference.

**In the field's words:** evals as code in a repository (*evals-as-code*), a *pre-flight validation* that is the same
gate CI runs, and *interoperability* with a common format instead of a format of our own that only we can read.

## Deleting the old evals (F126.1)

Everything above built something, and this part takes it all away. You looked at the old evals on 2026-11-09 and
said "we cannot have 23000 lines of code for something simple". On 2026-11-22 you picked to delete them first,
before a line of the new design is written. So F126.1 deleted the harness, its protocol, its check contract, its
runner, the studio's API, the eval pages, their stores and their tests, and ported nothing.

Here is the cut, counted one way and said once: the lines of `.cs`, `.razor` and `.scss` that git tracks, from
`f2fbd098a` (the commit before the first deleting step) to the last step. The code went down by 32,892 lines and
the tests by 35,466, against 435 lines added. With the docs, the pictures and the settings, 89,201 lines left. The
plan had guessed about 32,100 of code and 33,200 of tests, so the guess was close and a little low.

The order was the lesson. It ran from the outside in: first the sites that named the eval area, then the demo,
then the pages, then the studio's own tests, then the API's test library, then the studio, and only then the
harness. Each step deleted something nothing else still used, so every commit built and every touched test
passed. In a deletion, the tests that go red first are the rule tests that hold the repo's layout. A list such as
`PackagesTests`' packable projects changes first and fails on the folder that's still there, then the folder goes.

Some things only showed up on the way. A removed test case can't fail, it just stops running, so a layout list
isn't always a test first. Two sanity floors (`>= 30` projects, `> 40` docs) and a package count in an integration
test still held the old numbers, and only a search for the numbers found them. And this PC's copies of some files
had Windows line endings that git didn't, which made one page test fail here and never on CI.

What a later child may need is still in git. The session client the new runs will use was
`src/evals/LouisAgent.Evals/Api/HttpSessionClient.cs`, and the judge's helpers were in
`src/evals/LouisAgent.Evals/Grading/Models/`. Copy them from `f2fbd098a`, with a test first, as you picked ("1.
Delete everything"). The decision itself is [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md), which
supersedes the nine ADRs this write-up's earlier parts rest on.

**In the field's words:** a *preparatory refactoring* comes before the change it makes easy (Fowler), and an
*Architecture Decision Record* that is replaced stays, marked *superseded* (Nygard).

## The file and the grading (F126.2)

With the old evals gone, F126.2 is the first new code, and it builds the two things every later eval stands on:
what an eval file may hold, and how one turn gets its marks. You wanted it kept simple, and it came out as four
parts and nothing else: a reader, a schema, the facts, and a judge.

The file is one `{name}.eval.json` of conversations, each turn with its own facts and criteria. The reader is
strict and stops at the first fault, with the fault's JSON path at the front of the message, such as
`$.conversations[0].turns[0].facts[1] (line 5)`. That place costs no code: the serializer knows where it is, even
when our own converter is what refused the words. The schema an editor reads is made from the same records by
.NET's exporter, so the two can't drift apart, and it's a golden file, so a change to a record shows in review.

Grading splits in two, the way Anthropic's evals guidance splits it. What code can tell for certain is a fact, seven
of them, read from a turn's transcript: free, instant and the same every time. `never-says` reads the answer as
sent, markdown addresses included, which is the check a prompt-injection eval needs. Everything else is a criterion,
which one judge marks with one model call, the reason first and then yes or no, as a typed record. A judge that
can't mark says so and never answers "not met", since that would blame the agent for the judge's trouble.

The one thing reading couldn't confirm was whether the model would answer in a schema through our Anthropic
adapter, so a live proof came first: two calls on Haiku 5.5, one plainly met and one plainly not, for about a
hundredth of a US cent. It held, and the judge then marked two of the guide's criteria live, rightly both times.

Three names changed on the way, each on your word after research. A refused value is in single quotes, as the
diagrams' texts quote one, so no template needs an escape. The reader's exception builder is `Refused`, Microsoft's
*exception builder method* named as the repo's others are. And the schema's method is `Write()`, a verb, since ADR
0126 keeps `Of` for a record built from another of our types. One old fault turned up too: a backspace character in
the integration-marking rule's pattern, there since it was written, had kept half the rule from ever matching.

**In the field's words:** *code-based graders* and *model-based graders* (Anthropic), *structured outputs* with a
schema on the request, *fail fast*, and a *golden file* for what a model reads.

## Runs, and a replay that costs nothing (F126.3.1)

F126.2 could grade a turn somebody wrote down by hand. F126.3.1 is the part that writes it down for real: an eval
harness, the piece that runs a task end to end and hands what happened to the graders. It asks the agent the way a
person would, over the same sessions API the chat uses, so the agent measured is the one visitors get. A turn's
transcript is built from the stream the agent's own clients read: the answer as sent, each tool call with its result,
the error, the cost. That is the complete record of a trial, its transcript in the field's word, and the graders
read nothing else.

A trial starts clean. Each repeat gets a session of its own, and a follow-up goes into the same session, since a
follow-up means something only when the agent remembers. A run asks for one repeat or three, because a model doesn't
answer the same way twice, and a conversation passes only when all three pass. That is pass^k, your pick over pass@k:
one lucky pass out of three says little about what a visitor gets, and three out of three says the agent does it
reliably. The record holds how many repeats were asked, so a run cut off part way never counts a conversation as
passed.

The run tells the agent failing from the harness failing, and grades only the first. An agent's error in a turn is a
result: `no-error` isn't met, and the next turn is still asked. A judge that can't mark, the API refusing, or a
recording that no longer answers is the harness: the run stops as could not finish, says why, and keeps what it
graded. Nothing is marked "not met" for trouble that isn't the agent's.

A run is one JSON file a person can open, the way Inspect keeps its logs, named by when it started and the eval, so a
folder listing is already in order. The eval files travel as content with the agent they prove, embedded beside its
prompts and found at start-up. Starting a run is long work behind an API, so the start answers `202` with the run's
address and the run is asked how it goes: Microsoft's Asynchronous Request-Reply, with the run in the background of
the host on a hosted service that takes one run at a time.

The replay is the part that will pay for itself. A live run records every model answer, the agent's and the judge's,
under each repeat's trial. A replay plays those answers back: nothing calls a model, and it costs nothing. It is a
response cache for exactly repeated requests, which is also its limit, and that limit is the point. Change a word of
the guide's voice and the request no longer matches, so the replay stops, saying the recording is out of date. So the
guide's eval is now one unit test with no key, and a pull request that changes the guide turns it red until a live run
records again. That is a regression gate that sees the agent changed.

**What it measured.** Three live runs of the guide's one conversation, three repeats each, on Claude Haiku 5.5 for
the agent and the judge. The one that counts, `20261123-063633-guide`, took 33 seconds, wrote a 55,578-byte file and
cost $0.0077 for the agent's 7 calls and $0.0074 for the judge's 9: about a cent and a half for nine marks a repeat.
All three runs together cost $0.038, priced by the site's table, which matched Anthropic's page that day. Its replay
took about a second, gave the same 18 marks, and its 16 calls are on the ledger at a known $0. The guide met every
fact every time, and passed none: it never named the doc it read, and it said "AI agent framework in C#" in one repeat
of three.

**What didn't work at first.** The first live run finished but recorded nothing of the agent. The open door, which
lets visitors reach the guide, admitted the run's own sessions as a visitor's and dropped their run tag and their
recording. Your pick was that a keyed request isn't a visitor. The second run then recorded, but the guide opened with
the instance's tools instead of its own and never read a doc, so it wasn't the guide visitors get. The door now lets
an operator's run, a keyed request that names a run, through as a visitor's agent that keeps its run, and the third run
was right. The judge also split on the guide's `Offer` cards as its nudge, met once and not met twice for the same
behaviour: a criterion to word more plainly when F126.5 writes the guide's evals.

**In the field's words:** an *eval harness* and a *transcript* (Anthropic), *pass^k* against *pass@k*, a *trial* that
starts clean, *Asynchronous Request-Reply* (Microsoft), a *hosted service*, a *response cache* as a free regression
run, and Fowler's *self-initializing fake*.

## Reading a run (F126.3.2)

A run kept as a file is complete, and nobody reads it. Anthropic's advice on evals keeps coming back to one habit:
read the transcripts. A score tells you that something got worse; only the transcripts tell you what. F126.3.2 is
the place where that reading happens, three pages in the admin of a site that names `.Evals()`: the evals, a run,
and a conversation.

**The number first, the failures first.** A run's page opens on how many conversations passed, of how many, in
large type, because that is the one thing everyone wants first. Then come the conversations, the failed ones at the
top, each with one line saying which repeat failed and on which mark, in that mark's own words. Seven rows with two
failures needs no filter when the failures are already on top. This is the start of an error analysis: you don't
read every transcript, you read the failed ones, one after another. Previous and Next failed on a conversation's
page are that habit made into two links.

**What a pass rate hides.** With pass^k, a conversation passes only when all three repeats do, so "5 of 7 passed"
can hide a conversation that passed two of three. The run page keeps the counts beside the rate: "2 of 3 repeats" on
every row, and the facts and criteria met over the whole run. A conversation's page opens at its first failed
repeat, the one worth reading, and a switch shows the others with their tick or cross.

**A code grader and a model grader look different.** A fact is checked in code, so it is drawn in code type and
says so. A criterion is the judge's, so it carries the judge's line of why. That matters when a mark looks wrong:
a fact that is wrong is a bug in code; a criterion that is wrong is either the judge or the criterion's words, and
the line of why tells you which. In the first runs the guide's "Names the doc it read" failed every time, and the
judge's lines showed why plainly: the answer never named a doc. Reading them is what showed the criterion asks for
something the public guide shouldn't do at all.

**Ask the record, not the page.** Every figure on these pages is a method on the run's records: the counts, the
cost, the time, the first miss, the order failed first, the next failed one. The page only draws. So the run page,
the conversation page and the admin's home card can't disagree about what passed, and the run's file gained no
field: a test holds its JSON's property names exactly. A cost with no price reads "unknown", never 0, the same rule
as the usage ledger everywhere else.

**A short run is never shown as a whole one.** A run that could not finish says so first, in its own words, and
says how many of the eval's conversations it didn't reach. Its score reads "of 2 graded passed", not "of 2 passed".
That is the same line F126.3.1 drew between a failed agent and a failed harness, carried onto the page.

**An area a site opts into.** The pages are a library, `LouisAgent.Web.Evals`, that a site takes with one word
among its areas, as the routes are a library its API maps. A site that runs no evals ships neither.

**What didn't work at first.** The pages' folder grew past six files at the run's states and split by page, your
pick. A hand-made run left "running" read as "could not finish": the API marks a running file that isn't the
current run as cut off, so "still going" only shows for a run really playing, which is right. And the browser
window here wouldn't narrow to 390, so the phone width was looked at only through Bootstrap's grid, not by eye.

**In the field's words:** *read the transcripts* and *error analysis* (Anthropic, Hamel Husain), *pass^k*, a *code
grader* against a *model grader*, and Fowler's *Repository* behind the client.

## The limits, said plainly

36 labels, one judge, one person. Nine fails is "few labels" by the card's own rule. A pass at 100% on nine is weaker
than it sounds.

The prompt was tuned on the same 36 answers it is scored on. That was your pick, and it is how the four rounds worked,
but it means 35 or 36 of 36 shows the prompt fits *those* answers. It does not prove it works on new ones. Check it on
fresh labels: the next grading round, or the new scenarios F48 brings.

The labels are yours alone. A second reviewer's grades would be a stronger check and the store already allows them.

A prompt change means a rebuild. The judge's prompt is baked into the image, so the loop is edit, rebuild, press.

The button hasn't been pressed in anger on a new prompt yet. Its first real use (35 of 36, 2026-10-27) was on the
prompt we had already tuned.

---
[Learning](README.md) · [Docs index](../README.md) · Related: [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md) · [FX25](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/fixes/FX25-runner-environment.md)
