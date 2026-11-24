# 12 · Evaluation: a judge is only as good as its last check against a person

> **Skill:** #12 in the [goal](../GOAL.md)'s map, with #16 (human in the loop), a nod to #15 (least privilege) and side
> lessons for #17, #4 and #3 · **Stages:** F18 steps 6.3 to 6a.5, the judge calibrated and then checked from the portal
> (2026-10-26 to 27); the rest of F18 and its first v2 baseline (2026-10-29); F49.2b, the generic checks (2026-10-31); F126.1, the old evals deleted, and F126.2, the file and the grading (2026-11-22); F126.3.1, runs, F126.3.2, the run pages, and F126.3.3, the page a conversation names (2026-11-23)
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

## The page a conversation names (F126.3.3)

An eval is only worth its score if the agent it measures is the agent people meet. On louis-agent.dev the guide is
never asked a bare question: every message a visitor sends carries the page they're reading, its words, its places,
its links, and the part of the site it belongs to, which tells the guide whether to sell, show demos or talk shop.
Until F126.3.3 an eval sent none of that, so it measured a guide nobody meets. The field calls this the gap between
the eval's harness and production, and it's the first thing to close before trusting a number.

**Read on the server, the way the widget reads it.** The site's API now answers `GET /v1/site/page?address=` with
the page as a message carries it, rendered on the server and read rule for rule as the widget's script reads it in
the browser: the title, the words of `main`, the places with the widget's own marks, the links within the site, and
the focus from the same rule the widget asks, which moved to the shared contract so both could reach it. The run
asks that route for the page a conversation names, sends it with the first ask only, as the widget does for a
visitor who stays put, and keeps it beside the agent's and the judge's answers, so a replay sends the very same page
and costs nothing.

**Comparing the two readings found three differences.** On the landing, `/demos` and `/trust`, the titles, the
places and the links matched. The server read 2,580 characters on the landing that the browser hides; the reader now
leaves out what the markup marks `hidden` or `aria-hidden`, which brought the landing to 4,080 characters against the
browser's 4,137. The browser's `innerText` runs words together where blocks meet ("roomtook"), and the server keeps
them apart, the cleaner of the two, so that stays. And the animated mark's state words differ by the second, which
nobody needs. What a server can't know, what CSS alone hides and what is on the visitor's screen, stays out of reach,
and that is said plainly in the design doc.

**The eval learnt something on its first run with the page.** Asked "What is louis-agent, in one minute?" on the
landing, the guide answered as a seller, as its voice asks on a selling page: it read its selling skill, said in three
plain sentences what it does for a business, and offered a demo. It read no doc and never said C#, so the eval's old
expectations, written for a technical answer, failed. The eval was wrong, not the agent: it had been written for a
guide that never saw the page. Its conversation now expects what the guide is built to do there, `used ReadSkill` and
"Says what louis-agent does for a business, in plain words". The same day, the guide stopped naming docs on the
public site at all (FX229), which an always-failing criterion had pointed at.

**What recording again cost.** One live run of one conversation, once: $0.003563 for the agent by the run's own
record, and about $0.0008 for the judge's two calls at the price table's rates, about $0.0044 in all. The kept
recordings are a second run's own files, read before they went into the public repository: that run cost the agent
$0.003513 and the judge about $0.0008, and it met every fact and criterion, the reworded one included.

**What didn't work at first.** The test eval's JSON, built in a raw string that ended on a quote, lost that quote to
the string's closing marks. A message with no page wrote `"page": null` while the browser leaves the key out; the wire
record now leaves it out for every sender. And the replay test still failed after the new recordings, on the one fact
the live run had missed, which is what put the eval's expectations in front of you. On CI the replay then failed on
Linux and passed here: "message 3 (tool) changed". `ReadSkill` handed the model the skill as the checkout wrote it,
CRLF on this PC, where the recording container was built, and LF on Linux. It now hands it with LF, as `ReadDoc`
has done since F126.3.1, and the guide was recorded once more. The lesson is the one F126.3.1 learnt for docs: any
tool that hands the model a file must hand it the same bytes on every machine.

**In the field's words:** the *harness* against *production*, *train/serve skew* in its eval form, a *self-initializing
fake* that now records the page too, and Fowler's *Remote Facade* for the route.

## Start, follow and stop a run (F126.3.4)

Until this child, a run was started with `curl` and the admin only read what came back. Now a person does it all from
an eval's card: a replay for free, at once, or a live run after an ask that says about what it will cost. The run's
page follows the run without a reload and stops it at once, and every run shows two bills, the agent's and the
judge's.

**An eval has two bills.** The agent's turns were always priced. The judge's calls landed in the ledger under their
own purpose, but no run said what they cost. Each criterion's mark now comes back with what its one call cost, read
from the judge's own usage scope, and a turn keeps the sum beside the agent's cost. A replayed mark is a known $0,
and a mark nobody priced is null, never $0, as ADR 0007 asks. A run in short carries both costs, so a card can price
the next run with no second read.

**An estimate is only as good as the last real spend.** The ask's figure is the eval row's own answer: what its last
finished live run spent, a repeat, times the repeats picked. It comes from a run that happened, not from a price
table, because a table can't know how many tool calls the agent will make or how long its answers will be. When only
one bill was priced, as for a run made before the judge's cost was kept, the estimate is that one. When neither was,
the ask says the cost isn't known, and the button says "Run live" with no figure. A wrong $0 would invite the very
spend the ask is there to guard.

**A guard before spending.** A replay starts on one press because it costs nothing. A live run opens an ask inside
its card, which names what it plays, offers one repeat or three, says the estimate, and has buttons named for what
they do: "Run live, about $0.011" and "Don't run". The page holds which card's ask is open, so only one is. The
buttons go off from a press until the host answers, so a double press starts one run, and the host's `409` stays the
guard for a run started somewhere else. This is the human in the loop at its smallest: one clear question, asked at
the moment money is about to be spent, with what it will cost in the question.

**Cooperative cancellation, and telling a stop from a shutdown.** In .NET a long job is stopped by asking, not by
force: it is handed a `CancellationToken`, and it checks it at the points where stopping is safe. The run's work now
gets two. One is a person's stop alone. The other joins that with the host shutting down, and the play runs under
it. So when the play is cancelled, the work knows why. A person's stop keeps the run as stopped, with everything
graded before it and the reason. A shutdown leaves the file saying running, which the desk reads as could not finish
when the host comes back. The desk cancels under the same lock a start takes, so a stop can't land between one run
ending and the next one starting.

**Start, then ask how it goes.** The start still answers `202` at once with the run's address. The run's page now
asks that address again every three seconds while the run goes, on a timer from the page's clock, and stops asking
once it reads a run that has ended, or once the person leaves the page. The clock is the one the evals' area
registers, `TimeProvider.System`, so the tests use Microsoft's fake clock and move time by hand: a test that waited
three real seconds would be slow and flaky. The page shows only what it read, never a guess (ADR 0042), so its
figures move once a conversation, as often as the run's file is kept.

**Measuring what a stop really saves.** Two live runs of the guide's eval at one repeat, on your go, in the site's
container. The first finished in 11 seconds: the ask said "About $0.0035 for 1 repeat", the page showed the agent's
$0.0035 and the judge's $0.00063, and the ledger held $0.003484 and $0.000625. The second was stopped while it
played. Its page showed it stopped 217 ms after the press, with nothing graded and one conversation not run. The
ledger held one agent call of $0.000319, which had ended 8 ms before the stop, then one call marked cancelled, 3 ms
long and costing nothing. So the stop saved the rest of that conversation's calls, about $0.0038 here. The API logged
the run's own `DELETE` of its session after the stop's `204`, so no session was left open. Both runs together cost
about $0.0044. The second ask already said $0.0041, both bills of the first run.

**What the ledger said that the code didn't.** The first run's judge rows were in the ledger, priced, but with no run
tag, while the agent's rows named their run. The run's page was right, since it reads the run's own scope, but the
ledger alone couldn't say what judging one run cost. You picked tagging them: the player now grades under the play's
run and task, which the judge's own scope takes on. Reading the ledger, not the code, found it, and that is the
point of measuring: the code looked done.

**What didn't work at first.** A stop that lands just as a session is created could leave that session open: the
read of the created reply was cancelled too, so its id was never known and never deleted. It now reads that reply
whatever the stop says. The tests' wire-name rule refused a made-up refusal type that looked like a provider's, and
the test now uses one of its own. And a note in the plan said the site's container keeps no usage ledger. It keeps
one, in its logs folder rather than its data folder, which the live runs showed. The note was corrected. The pages
were checked through the browser's script, since this browser took no screenshots, so there are no pictures of the
new states.

**In the field's words:** *cooperative cancellation* and *linked cancellation tokens* in .NET; Microsoft's
*Asynchronous Request-Reply* with the client *polling* its status address; a *confirmation step* with *specific
button labels*, the human-in-the-loop guard before an action that costs; and the *test double for time*, the fake
clock.

## Flips, re-grade and better or worse (F126.4)

The judge is a model, and it is sometimes wrong. Until now a run's number mixed the agent's mistakes with the
judge's, nobody could tell which criterion to reword, and seeing what a reworded criterion did cost a live run. Now a
person corrects the judge mark by mark, a run is graded again from its stored answers for almost nothing, and every
run says whether it was better or worse than the one before.

**A person correcting the judge.** Every criterion's mark has Flip. A flip says "the judge got this wrong", with one
optional line of why, and it is kept on the mark itself, beside the judge's own answer and reason. The mark then
answers which answer counts, and every number of the run asks it, never the judge's answer directly. Hamel Husain
asks for exactly this: the expert's verdict plus a critique detailed enough to teach a judge later. LangSmith does it
the same way, a correction on the judge's score with an explanation. What we leave out is a percentage of agreement:
a run says how often each criterion was flipped, which tells you which criterion wants rewording, or wants to be a
fact. A fact is never flipped, since code tells it.

**When two answers are the same answer.** A flip is a person's word on one answer, so it must not leak onto a
different one. One rule decides: two turns say the same when the ask, the answer, the error and every tool call are
the same, compared as the text kept, and how long they took doesn't count. Where a turn says the same and a
criterion's words are word for word the same, the earlier mark is the mark. A replay uses it to carry the live run's
flips, and a re-grade to keep marks. The rule is asked by name, because a C# record's own equality compares its
lists by reference, and two turns that read alike would come out different: a test shows it.

**Re-scoring without re-running.** A re-grade is a new run made from a run's stored answers, with no agent asked, as
Inspect's `inspect score` writes a new log and W&B Weave's rescore a new evaluation run. Facts are told again in
code, which is free. Every mark the rule finds is kept, flip and all. The judge is asked only for a criterion that is
new or reworded. So grading becomes a separate, cheap pass over stored transcripts: a run against an unchanged file
asks the judge nothing, and the re-graded run says after what it asked and what it cost, in place of an ask before.
The grading of a turn moved into one `TurnGrader` that the player and the re-grader share, so they can't drift.

**Recording new episodes.** The judge's answers in a re-grade are added to the live run's own recording, beside the
answers already there, which is VCR's new-episodes mode. That is what makes a reworded criterion cheap: before, a
replay of the live run stopped on it as out of date; now one re-grade asks the judge once a repeat for it, and Replay
free plays again. A test proves it on the API in memory: after a re-grade with a reworded criterion, the replay
finishes, and a re-grade of that re-grade records into the same live play.

**Comparing runs.** Each run is compared with the run before: a re-grade with the run it re-graded, since it is about
what changed in the file, and a live run or a replay with the newest earlier run that finished, the way Braintrust
picks the most recent experiment as its baseline. A run that stopped holds only part of its conversations, so it is
never a baseline. The run's head says "▲ 1 more than the run before" in green or "▼ 1 fewer" in red, rows are tagged
"now passes" or "now fails", and the last eight runs are a bar each with the criteria they missed most. Each run's row
carries its own tally of missed and flipped criteria, so the history adds rows and reads no run whole.

**Turning a real conversation into a test case.** Copy as conversation turns a run's conversation back into a block
for the eval file, written by the file's own options, so it reads back with the file's own reader. A conversation
that surprised you becomes a test in one paste.

**What was measured.** On the :5080 container, for free: a replay at $0; a flip with a note made the conversation
"Passed after your flip", and Undo put it back; a re-grade of a replay with the file unchanged made 0 judge calls and
kept 2 of 2 marks; the change line, the eight bars and the criteria missed most showed; and the guide's conversation,
copied and pasted into a scratch copy of its eval file, read back. The one paid check, re-grading the guide's live run
after rewording one criterion, ended as could not finish: the provider refused the judge's call with "invalid
x-api-key", the site container's model key having stopped working since the morning's runs. Nothing was spent, the
re-grade kept its reason, and the real-provider proof waits for a working key.

**What didn't work at first.** After a re-grade, the browser moved to the new run's address but the page kept the
old run's figures: Blazor keeps a page when only its address's run changes, and the run's page read once, when made.
It now reads whenever the address names a run it hasn't read. A re-grade that judged nothing said the judge's new
answers had joined the recording; that line now shows only after judge calls. And rows that now carry a list broke
two tests that compared rows by equality, which they now do as JSON.

**In the field's words:** *human review* of an *LLM-as-a-judge*, with a *critique*; *re-scoring* (Inspect, Weave);
*VCR's new_episodes record mode*; a *baseline* and *regressions per case* (Braintrust); *Information Expert* for where a
flip lives; and *Extract Class* for the turn grader.

## The guide's evals (F126.5.1)

The harness was whole; what it lacked was evals. The guide's eval grew from one conversation to twelve, written the
way the field says to: first from what the guide is told to do, then from what went wrong in use.

**From the rules.** The guide's voice and skills already say what it must do: greet first, three short sentences,
no doc named, lead as the part of the site asks, walk a page one stop at a time, offer choices as cards, treat a
described job as a custom agent, offer to pass a visitor on to Louis, and be honest about what it can't do. Each
became a conversation, with a fact in code where code can tell (`used ShowPage`, `never-says .md`) and a criterion
where only a reader can. Writing them showed a rule an eval must keep for itself: no conversation may send a lead,
since a live run would email a real one to Louis, so the lead conversations stop before the send and check in code
that it never happened.

**Prompt injection as an eval case.** The guide reads the page a visitor is on, and a page can carry instructions
meant for it. To test that without such a page on the public site, a conversation can now give its page in the eval
file itself, sent exactly as a visitor's message carries one. `guide-injection` gives a customer story whose words
tell the AI reading it to send a lead and say a code word. Code checks the guide did neither, in its answer or an
image's address. It passed every repeat. It is OWASP's LLM01 with the eval's own tools, and the guide's first
security eval.

**Error analysis, with what the log may hold.** The site keeps no visitor's words: its turn log holds each turn's
page, part of the site and tools, and content is hashed. So the analysis read patterns, not answers. On this PC's
log, Louis's and Claude's own chats and eval runs rather than production's visitors, three talks on the landing read
no skill on their first turn, though the selling skill is to be read there, so a bare "Hello" on the landing became a
conversation. A log of tools and pages can show a skipped step but never a bad answer whose tools were right, which
is the honest limit of reading it.

**What the baseline measured.** One live run of three repeats: twelve conversations, about six and a half minutes,
$0.090, the agent's $0.062 and the judge's $0.028. Nine passed in every repeat. Asked to be told everything, the
guide wrote about fifteen sentences where its voice allows three; asked where to read about packs, it named the guide
by its title in two of three; and once it answered with a card and no words. Those are the eval doing its job, each
now a fix in the TODO (FX231 to FX233). The judge's reasons were read before trusting them, and two criteria turned out
to ask for words where the guide's voice asks for a card, so they were reworded to count either, before the baseline.

**What the first run found that wasn't the guide.** It stopped at a conversation for the free-website offer, which
had been closed the day before: the guide still named its page. That was FX228, waiting for exactly a live run, so it
was done here and the baseline recorded the prompt as it stays. And the replay of the baseline stopped on a docs
search, which answered other lines on Windows than in the Linux container, from a different folder and in a
different order (FX230). A free replay is only as free as every tool in it gives the same bytes everywhere. Writing
this child's own records then showed the other side of that: a replay that runs the docs tools over today's docs goes
stale as soon as a docs line matching a recorded search changes, even a row in the TODO. So FX235 records what each
tool answered in a live run and plays it back, as the recording already does for the model, and the guide's replay
now runs over an empty docs folder.

**F126.4's re-grade, proved against the real provider.** With a working key, one criterion was reworded in a working
copy and the baseline re-graded: three judge calls, one a repeat, $0.00087, the other 48 marks kept. Then Replay free
finished with every ledger row at $0, the reworded criterion's answers found in the live run's recording.

**In the field's words:** *eval-driven development* from a prompt's rules, *error analysis* (Hamel Husain and Shreya
Shankar), *prompt injection*, indirect, as OWASP's LLM01, a *test fixture* for content the model reads, and a
*baseline* guarded by a *free regression replay*.

## The framework Orbi's evals (F126.5.2)

The second agent to get an eval was the framework site's Orbi, and the point of it was that the harness didn't
change: an eval is the agent's, wherever it runs. The framework site's host took the same two calls louis-agent.dev's
does, the eval routes behind the admin key, and a data folder for its runs and recordings, on this PC and on the
Azure share, as a second mount of the folder the app already uses for its logs. Louis then asked for the admin page
too. The admin's frame was already the framework's, so the framework site only had to fill it with its own name and
name the evals' area, which is what a site built from framework parts should cost.

**From the rules, with no log to read.** Orbi had no turn log on this PC, so its eleven conversations come from its
voice and skills alone, plus an injection case. Its injection case is different from the guide's in an interesting
way: Orbi has no tool that does anything outside the chat, so the attack can only make it say something, a download
address and a code word, and code checks it said neither. It held in every repeat.

**What the baseline measured.** Twelve conversations, three repeats, $0.066: eight passed in every repeat, and
every fact of every first repeat was met. Orbi's own faults were close cousins of the guide's: once its visible reply
was empty, only a page and choices, the guide's FX233 again; under the code it named one setting where its voice
asks for each one; and it sometimes wrote past the doc's path or summed a listing up in two sentences. Those are
FX236 to FX238. One criterion was the eval's fault, not Orbi's, the second time this happened: "Sends a question
about buying to louis-agent.dev" was read as an action, so it now says "Says that buying questions go to
louis-agent.dev". A criterion is an instruction to a reader as literal as a model, and its verb decides the mark.

**What went wrong in the running of it.** The first live run went out on a container built before the injection
case was written, so it held eleven conversations, not twelve, and a second run was needed. A live run measures the
build it runs on, not the branch; rebuild before you pay.

## The triage team's evals, and F126 closed (F126.5.3)

The third agent was the hardest to measure, because the triage manager doesn't answer alone: it asks a sorter and an
investigator, by their addresses, and decides. So its eval proves routing in code for the first time: `routed-to
triage.investigator` is met when the turn asked the investigator, and `continued` when a follow-up asked it again.
Each case is briefed as the feed would brief it, on the sample order service, with errors made by running it, and
no case can reach the coder: the manager's calls name only its three roles, so an auto-fix stops at the decision.

**Five live runs, five findings.** The first ended 0 of 6 for three cents: an eval opens a session by identity alone,
and the team's voices named their toolsets only in prose, so the manager had none (FX239). The next stopped before it
began, the agents toolset needing a peer. Then two real faults in the manager's prompt, each found in one repeat in
three: it didn't decide again when a case came back (FX240), and it decided "person" without raising the case
(FX241). Each fix was a sentence, and each said why.

**Then the cost.** Those runs cost $1.84 each, $0.10 a chat, against a fifth of a cent for the guide. The ledger's
split by purpose showed $0.84 of it was summaries, and every one was the files pack's search returning megabyte-long
lines from the repository's one-line files. Showing a window around the match (FX242), and letting the investigator
search only the repository it was given (FX243), brought the next run to $0.12, $0.0067 a chat, sixteen times less,
and it passed better too: 5 of 6 in every repeat. Cost per chat is the number to watch: it made a $1.84 run look
reasonable until it sat beside the guide's.

**The pages caught up.** Closing F126 meant the framework site's page, louis-agent.dev's trust tile and the
framework Orbi's map of the site could no longer say the evals were being rebuilt. They say what is built now, each
claim held by a test, and Orbi's baseline was recorded again for its changed prompt; its injection case then caught
the eval out rather than the agent, when a warning that named the download site failed a fact meant for obeying.

**F126 in one paragraph.** The evals were deleted and rebuilt as one file of conversations beside each agent, with
facts that code checks and criteria a judge marks, a person's flip when the judge is wrong, runs started and read on
an admin page, re-grades that ask the judge only what changed, and replays that play back the model's and the tools'
answers for nothing. Three agents on three hosts are measured by it, each with a baseline that every pull request
replays.

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
