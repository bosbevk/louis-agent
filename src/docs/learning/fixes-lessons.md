# Lessons from the fixes

> **Answers:** what each fix taught, in one place, now that the fix docs are folded (CL24, 2026-10-29) · **Written by**
> Claude, for Louis to read

![Lessons from the fixes: what each fix taught, by its skill, most of them about testing and shipping](diagrams/fixes-lessons.svg)

Each fix's symptom and cause stay in its row of the [fixes table](../fixes/README.md); the whole doc as it was is at
the tag [`pre-cl24`](https://github.com/bosbevk/louis-agent/tree/pre-cl24/docs/fixes). Here is what each one taught,
with its skill in the [goal](../GOAL.md)'s map. The ones with a write-up of their own skill are told at more length
there: FX1 and FX5 to FX7 in [13](13-cost-and-token-economics.md), FX10 to FX14 in
[10](10-multi-agent-orchestration.md), FX18 to FX20 in [15](15-safety-and-security.md), FX25 in
[12](12-evaluation.md).

## FX1 · Keep the tools on the request after the 10-round limit

**Preserved thinking**: newer Claude models tie their thinking blocks to the exact conversation (system prompt, tools,
earlier messages), so any layer that edits a request in flight, even a library's, must keep it append-only. Skill map #2
(reasoning and extended thinking) and #14 (reliability). The same rule is behind [FX4](../fixes/FX04-append-only-history.md).

## FX2 · The usage probe says which model to pull on an Ollama 404

An error a person will meet should say what to do next, not only what failed (skill map #14, reliability).

## FX3 · The Ollama container starts and pulls the profile's model

A shell script run inside a Linux container must have LF endings however it is checked out on Windows; `.gitattributes`
protects new checkouts, not old ones (skill map #18, testing and shipping).

## FX5 · Requests outside a usage scope are written to the ledger again

`?.` short-circuits the rest of the expression, arguments included, so a side effect inside the argument (writing,
counting, logging) disappears when the target is null. Keep side effects out of a null-conditional call. And
recording must never depend on optional context: the ledger is the source of truth, and the scope is only a view of it.
The tests that caught it were written for F1, which is why every step keeps its tests (skill map #17, observability;
#18, testing and shipping).

## FX6 · A turn that fails still reports what it used

A total is only as good as its worst path: failure paths must report what they used, or every view built on the
happy path drifts from the ledger, quietly, exactly when something went wrong (skill map #17, observability; #13,
cost).

## FX7 · A timed-out request no longer ends the orchestrator's run

Cancellation is not one thing: "the user stopped" and "something timed out" share an exception type in .NET, so tell
them apart by the token that was cancelled, never by the type alone (skill map #14, reliability: timeouts in long
tasks; #18, testing and shipping).

## FX8 · A FIX-RESULT with quotes in its summary is still read

A machine-readable line written by a model is still model output: parse it tolerantly, and never answer a parse
failure with a request the model can't act on differently, since every retry is paid for (skill map #5, structured
outputs, which F8 addresses with results as tool calls; #13, cost).

## FX9 · A request cancelled without streaming is still written to the ledger

A ledger that only records successes undercounts exactly the requests that went wrong, which are the ones worth
seeing. Skill 17 (observability) in the [goal](../GOAL.md).

## FX10 · A sub-session call without `agent` uses the only peer

A tool's schema is the contract, and the description is only advice. When the description says a value can be left
out, the schema has to agree, or the model gets refused for following the advice. Skill 4 (tool design) in the
[goal](../GOAL.md).

## FX11 · A sub-session that ends without an answer is asked once more, in the same session

Continue the context you already paid for; don't start over. A retry in a fresh session re-buys the prefix and redoes
the work. A follow-up in the same session reads both from the cache. The same idea is behind prompt caching (F4) and
the orchestrator's continue loop. Skill 10 (multi-agent orchestration) and skill 13 (cost and token economics) in the
[goal](../GOAL.md).

## FX12 · `.env` templates (`.env.example`) can be read

A guardrail that blocks too much has a cost too: the agent works around it, guesses, or gives up. Block by what a
file holds, not by what its name looks like, and test both the blocked and the allowed side. Skill 15 (safety and
security) in the [goal](../GOAL.md).

## FX13 · Counting goes to the analyst, and the reader hands it back

Routing is a prompt, so it can fail, and the cost of a wrong route depends on what the wrong specialist does next. Tell
each role what is *not* its job and what to do instead, so a wrong route fails cheaply. Skill 10 (multi-agent
orchestration) and skill 3 (prompt design) in the [goal](../GOAL.md).

## FX14 · A sub-session's prompt has a budget

Limit the tail in code. A prompt can make a failure less likely, but only a limit decides what a failure costs. Context
grows with every tool call, and long-context pricing turns that growth into a cliff, so the limit belongs on the
context. Skill 13 (cost and token economics) and skill 14 (reliability) in the [goal](../GOAL.md).

## FX15 · A test passed only on a machine with `config/.env`

A test that reads from the machine instead of from what it was given passes where it was written and fails where it is
first run elsewhere. CI's first job was to find the hidden local state, which is exactly why it exists. Skill 12
(evaluation) in the [goal](../GOAL.md): a check is only a check if it runs the same everywhere.

## FX16 · A quote that is only a price was "Found" for any product at that price

**A check is only as strong as what it binds** (skill 12, evaluation; skill 15, guardrails in code): "the words are on
the page" proves the words, and a price alone is a word every product at that price shares. A verifier has to tie the
claim to its subject (the price to its product) or say it can't, else a *pass* overstates what was checked, and every
score built on it does too.

## FX17 · The cook wrote "Serves" in Afrikaans, "Geprys" in English, a count before a photo, and a follow-up's card said "sub-1"

A label the model is meant to translate has to be shown translated: one literal form is copied as it stands, whatever
the task's language (skill 3, *Prompt design*). And a card that names who it's waiting on needs that name from the one
who's working, not only from the call that sent the work: a call can be a follow-up that never names it (skill 10,
*Multi-agent orchestration*).

## FX18 · Without a key, the API and the MCP server over HTTP serve anyone who can reach them

Deny by default (OWASP Top 10:2021, A01): fail closed at start-up rather than at the first request, with one pure rule
tested without a host. A warning that a door is open is not a guard: the operator who never reads the log never
closes it, so the host has to refuse, and the one who wants it open says so in a setting that is never set in a
compose file. And unset and set-but-empty are different values in the environment: a fail-closed rule has to treat
them the same everywhere a key is read, or a blank line in an env file opens the door (the review's catch). Skill map
#15 (safety and security).

## FX19 · A value from the model can reach git as an option, and some options run commands

Argument injection (OWASP LLM05:2025; CWE-88): a list stops splitting, not options; `--end-of-options`, from git's
`gitcli` docs, and an allow-list rather than a deny-list. And a test for a guard has to fail for the hole, not for a
side effect: the branch name `x --upload-pack=…` is refused by git whatever we do, so it proves nothing on its own; the
test that proved this fix was the file `git show --output` wrote. Skill map #15 (safety and security).

## FX20 · A skill file the agent writes is loaded at once, for every later session, with no person in between

Memory poisoning (OWASP Top 10 for Agentic Applications, ASI06): what the agent writes for later sessions is untrusted
until a person approves it, however reasonable it reads, because the model that wrote it may have been reading an
attacker's page. The gate costs nothing new: agent-built tools already had it, and the engine's "files I wrote" list
already kept a file alive for one session; the fix was to stop the file reaching every other session on its own.
Skill map #15 (safety and security) and #16 (human in the loop).

## FX21 · The browser reads shops' pages for a user without saying what it is

A disguise is a decision about the product, not a rendering fix: whether the agent tells sites what it is decides
what a retailer finds in their logs and whether the sold endpoint can say it never pretended. The principles that open
every prompt bind the code around the model too.

## FX22 · The recipe's image showed as a link: an image renders when a tool gave it

A guard that names hosts guards against yesterday's hosts. What a page should be allowed to load is what the turn's own
tools returned, which a model can repeat but not invent: the same shape as ADR 0004's arguments as data, applied to
output.

## FX24 · The cook's recipe photo and its estimates: the grader, the chip, the box and the prompt

When a grader fails a good answer, check the grader before the model: open the page yourself. `recipe-image` v1
failed answers for icons on pages the cook only passed through, and the chip counted estimates as unpriced, so both
measured the wrong thing (skill 12, evaluation).

## FX25 · The eval runner gets only the settings it reads, not the API's secrets

**Least privilege for a child process** (the goal's *Security and guardrails*): what a process can read is part of its interface. A child inherits
the parent's environment unless told otherwise, so "it only reads four settings" is not the same as "it can only see four settings". The allowlist
makes the runner's inputs explicit, which is also documentation: the list *is* what the runner depends on. The paste's other three points (the
runner as its own job with its own identity, sign-in with roles and an audit log, and how long transcripts are kept) are Phase 9's.

## FX26 · A replay miss is marked `replay-miss`, not taken for the agent's failure

An error that crosses a process boundary as text loses its kind. The replay client knew exactly what had gone wrong,
but the API flattened every failure to `api_error` and the runner only classified its own exceptions, so the label
the contract already had (`TurnFailure.ReplayMiss`) was never reachable. Name a failure where it happens, carry the
name on the wire as a typed value (an enum with its wire name, parsed once), and read it once at the other end. And a
label is not a fix: the turn is counted fairly, but it has no answer until FX27 removes the cause. Skill 12
(evaluation) and 4 (the stream's seam).

## FX27 · A sub-session's answer carries no usage, so a replay matches its recording

What the model reads is the key a replay (and a prompt cache) looks things up by, so anything in it that changes from
run to run, a count, a cost, a time, breaks the lookup. Keep measurements out of the prompt: they belong to the ledger
and the screens ([ADR 0007](../adr/0007-measure-every-request.md)), and the model gains nothing from them. Of the
three options (take the usage out, mask it in the replay's key, replay the usage too), taking it out fixed the cause
instead of hiding it from the key and was the smallest change; the price was a fresh cook recording
([ADR 0049](../adr/0049-nothing-is-compatible-before-a-sale.md)). When a replay misses, read *which* message changed
first: here it pointed straight at the one line that carried numbers. Skill 12 (evaluation) and 13 (cost).

## FX36 · A library's failed task is yours to see

A `Task` that ends in an exception nobody ever awaits is a lost task: when the garbage collector finds it, the runtime
raises `TaskScheduler.UnobservedTaskException`, and F52's last-chance handler logs it as an error. PuppeteerSharp
makes one of those on purpose for every redirect a page follows: a redirect has no body, so the library sets the
body's task to "Response body is unavailable for redirect responses" and leaves it, where the JavaScript Puppeteer
keeps the error as a value and only makes a rejecting promise when someone asks for the body. Nothing was wrong with
any visit; the error showed up at an unrelated moment,
whenever the collector ran. Three lessons. An observability feature finds old faults, not only new ones: the line
appeared the day after F52 shipped, and the bug is as old as F32. A library's fault is observed where it's made,
with the smallest thing that awaits it (asking for the body), not silenced in the handler that reports every lost
task, which would hide the next real one. And a workaround for a library says so, names the version it was checked
against and the place in the library's code, so the day the library fixes it the class goes (TODO, small cleanups).
Skill 17 (observability) and 14 (reliability).

## FX34 · A cost is worth most while it's being spent

A live run's table showed every turn's role, calls and seconds, and "—" for its cost, until the whole run had ended:
the costs came from the stored run's requests, and those are written at the end. Nothing was wrong with the numbers;
they just arrived after the money was gone, and "—" meant both "not priced yet" and "no price known" (the second is
ADR 0007's meaning). Three lessons. A measure belongs where the decision is made: a paid run is watched while it
runs, so that's when its cost must show. Find where the data lives before choosing who should send it: the first
proposal had the runner price its turns, but the agent's requests are written to the ledger by the API's sessions,
so the runner never had them; checking that premise changed the design before any code. And when a live view and a
final view show the same number, give them one join and one sum (`EvalLedger.Join`, `LiveTurns.CostsFrom`), so they
can't drift; a sum that is asked again and again starts from empty each time, or it counts twice. A detail on the
way: a map that a background fold writes and a request reads is a `ConcurrentDictionary`, and a reader that can meet
a half-written file line says so instead of answering "nothing", which would have blanked the costs already shown.
Skill 13 (cost and token economics) and 17 (observability).

## FX32 · An edit starts from what is already there

The studio wrote a draft out as a folder made only of what it keeps: the eval's file, its rubrics, its scenarios and
the transcripts kept from runs. For a new eval that is everything. For an edit of an eval git already holds it was a
stripped copy: no references, no held-out scenarios, no labels, no manual. Every unit test passed, because each
one checked a new eval; it took an end-to-end run on the real cook eval to see it. Two lessons. An editor writes over
what exists, it doesn't rebuild it from its own fields, so the folder now starts as a copy of the eval's folder and
the draft goes on top (the runner stays the one that reads it, finding R). And a feature that edits real content is
tested on real content, not only on a made-up one: the first fix passed every unit test too, and the second run on
the cook showed its folder is `Cook`, not the `PulsCook` the studio worked out from the eval's name. Where a thing
lives is asked of the one that knows (the runner's listing), never derived from its name. Skill 18 (shipping:
end-to-end checks) and 12 (evaluation).

## FX33 · A raw string has its file's line endings

A C# raw string literal spanning lines holds the newlines of the source file it is written in, and git writes that
file with CRLF on a Windows checkout and LF on Linux. A test that compares a raw string's text to a `\n` literal
passes or fails by where it was checked out: here it passed in the worktree that wrote the file and failed on a fresh
checkout of `main`. The lesson for platform-neutral tests (ADR 0014): a test that cares about line endings sets them
itself (`ReplaceLineEndings`), and a test is proved on a fresh checkout, not only where it was written. Skill 18.

## FX31 · A small copy of a layout needs its own columns

The category cards draw the same trend chart as the big panel, only smaller, and they took its grid with it: three
fixed columns. A card has two panels, so each got a third of the card and the last third stayed empty; in that narrow
panel the change badge's text wrapped inside its round pill and turned into a green blob. Two lessons. A pill (a badge
with fully rounded ends) must never wrap, so `white-space: nowrap` belongs on the pill itself, wherever it is drawn.
And a compact version of a component should size its columns from what it holds (`auto-fit`), not inherit the big
version's count. Skill 18 (shipping: what a reviewer sees).

## FX28 · A free run is marked as one, wherever a number is read as a measure

A replay and a re-score both cost R0 for a reason (no model was asked), and a screen that shows the number without
the reason turns it into a wrong measure: the cook's replay became the check's last run and its estimate. The rule
is the one ADR 0007 already holds for an unknown price (`null`, never 0): a known 0 must say *why* it is 0 wherever
it is shown or summed, and every reader of the runs (the estimate, the trend, the card) leaves such runs out by the
same rule. The second lesson is where a fact like "this run was recorded" comes from: the class that writes the
setting reads it back (Information Expert, as CL26 chose for `replay_of`), so the web app never spells the setting's
name or the mode's word, and a test on that one class covers every page. Skill 12 (evaluation) and 13 (cost).

## FX30 · A path written from a file breaks when the file moves

CL26 split one stylesheet into folders and proved the move byte for byte: the same rules, in the same order. The one
thing a byte check can't see is a rule whose meaning depends on where it sits, and a relative `url(…)` is exactly that:
`../brand/` was right from `css/` and wrong from `css/chat/`. The browser said nothing; the tile just wasn't there. The
lesson: when a move keeps the text, test what the text *points at*, not only the text. `PulsStylesheetTests` now
resolves every relative `url(…)` from its own file's folder and asks whether the file is there, so the next split
fails in the test instead of on the screen. Skill 18 (testing and shipping).

## FX29 · A failure has to reach the screen that shows it

The send button looked dead, but it worked: the click opened the session, the API refused the key, and the runner
did the right thing (the draft back in the box, a notice saying the key is missing). Nobody saw it, because in Blazor
an event redraws only the component it was raised in, here the message box, while the notice belongs to the page.
The happy path told the page (`Changed`), the failure path didn't. The lesson: every path out of a state change,
the early `return` in a `catch` above all, raises the same "I changed" signal, and a test pins the signal, not only
the state (`ChatRunnerTests`: the notice *and* one `Changed`). A failure that is caught well but shown nowhere is
worse than one that throws: it reads as a dead button. Skill 18 (testing and shipping).

## FX23 · A pipe the child never reads is not a failure

When a parent writes to a child's standard input and the child exits first, the operating system answers the write
with "Broken pipe". It means "nobody is listening", not "something went wrong", and a runner whose one catch-all
turns every exception into a failed run reports a script that succeeded as a failure and throws its output away. The
lesson is to know which exceptions a call can raise and what each one means before catching them: here the only
exception that write can raise is the closed pipe, so a guard around that one call (Louis's pick over an async write
or a declaration on each tool) fixes the cause in three lines. The second lesson is about flaky tests: "flaky" is a
symptom with a cause, and the way to find it is to make it certain (a megabyte of input makes the child close the
pipe every time), then write that as the red test. Skill 15 (safety: the tool loop's guards) and 11 (test first).

## FX35 · A setting honoured too hard hides the content

A visitor's "animations off" setting reaches a website as `prefers-reduced-motion`, and a site is right to honour
it. The mistake was the blanket rule: every animation cut to a millisecond, which for a thirty-six-second film means
its end frame, with no card, caption or light, and a visitor asking where everything is. The setting is about
decoration that moves without being asked (a typing effect, a bounce); a film the visitor came to see is content,
and a video player doesn't stop playing because of it. So the rule now leaves the stage out, and a test holds that
every selector in it does. Two lessons: a check the browser can switch on is a case to render, not to assume (one
screenshot with `reducedMotion: 'reduce'` reproduced the PC exactly); and an accessibility rule needs the same
"what does this mean for the reader" question as any other. Skill 18 (shipping: what the visitor's machine changes).

## FX37 · A test that races is not a test

A flaky test is a test whose answer depends on timing, not on the code: here the publisher's timeout test set a
timeout of zero and expected the first git call, `rev-parse`, to be the one refused. But .NET's `WaitForExitAsync`
looks at the cancellation token only while the process is still running, so a `rev-parse` that finished in the
millisecond before the check slipped through, and the next call was refused instead. Run once it passed; run a
hundred times it failed. Three lessons. Reproduce a flake by repetition before you touch it (`[Repeat(100)]` failed in
seconds, and that output is the "red" of test-first). Fix the cause, not the assertion: loosening the test to accept
any git call would have hidden that the code's idea of "in time" was wrong at the edge; the code now asks the deadline
itself. And keep the repetition in the test, because a race that a single run can't show is only caught by many runs.

## FX39 · A cap on an unsorted answer loses what you were looking for

Asked for Pick n Pay in Malmesbury, the tool said the town had none and named one 42.9 km away. One is 0.5 km from the
centre. The code sorted by distance and kept the nearest, which looks right, but it sorted what the map had already
cut: the map answers at most 20 hits, in no distance order, and a box half a degree wide reaches Cape Town, whose
branches filled all 20. Two lessons. When a service caps its answer, sorting afterwards can't bring back what it
dropped, so narrow the question first (the smallest box first, then wider only if it finds nothing). And the tests
missed it because the fake answered every box the same: a fake that ignores what it was asked can't show a bug in the
asking, so this test's map answers each box differently, the way the real one did.

## FX40 · A list from outside is not a list of one kind

A recipe page's ingredient list is meant to hold ingredients, and one site also put its headings in it, as a line of
markdown (`***Filling`) or of HTML (`<h3>CURRIED MINCE</h3>`). We had saved that page as a test and the test even
expected the heading to be the first ingredient: the test described what the code did, and the code had never been
asked what an ingredient is. Three lessons. When data from outside mixes two kinds of thing in one list, give the
second kind its own place in the record (steps already had a `Section`; ingredients now have the same), instead of
cleaning the text at the end where nobody can tell it was a heading. Take HTML out where every text passes, after
the entities are read, because a page can write `&lt;h3&gt;`; and take out only a real tag, so that "cook for < 5 min"
keeps its sign. And a count in a saved-page test (21 lines) is a claim about the page: read the page's lines yourself
before you change the number. Skill 4 (tool design: shape what a tool hands the model, so it is clean before the model
reads it).

## FX43 · A check that always passes hides a design nobody wanted

Someone who said "any shop" got *Any supermarket near you.* under *Where to buy*, in every chat. Nothing had broken: the
prompt asked for that line, and the check `estimates-at-shop` passed `any` whatever the answer said, so no eval could
ever object. Louis saw it by using the app. Two lessons. A branch of a check that passes without reading the answer is a
hole, not a rule: ask of each arm what answer would fail it. And ask what the user can do with an output: a line that
sends them off to look for themselves is no answer. The fix is a tool that does the looking (`FindShopsNear`), and it was
tried on the real map first, where Sea Point's answer held a fabric shop and lost the nearest supermarket in a wide box,
which set its filter and its small first box. Skill 4 (tool design) and skill 12 (evaluation: a check has to be able to fail).

## FX44 · A limit copied from another question answers the wrong one

`FindShopsNear` kept three shops because `FindBranches` keeps three, and for one shop's branches three is plenty. "Which
supermarkets are near me" is a different question: Malmesbury's Checkers was fifth on the map, 2.2 km away, and the
answer never named it. Louis saw it in the app, so give each question its own number, and test it against the real
answer that showed the gap. The same fix took a made-up branch name, *Pick n Pay Malmesbury*, out of the cook's format
example: a model copies the example's shape, names included, so an example has to be true too. Skill 4 (tool design:
how much a result holds) and skill 3 (prompt design: an example teaches more than the rule around it).

## FX45 · A judge shown only good answers hasn't been tested

The local judge agreed with Louis on 26 of 30 recipes, which looked like a pass, but all 30 were good answers. Twelve
answers broken on purpose (an amount changed, a step dropped) showed what it really did: it caught 5, and no changed
amount. Agreement on one class says nothing about the other, so a judge's check needs bad answers too, made by hand
if no run has produced them. The paid judge that replaced it had never run at all: every judge sent temperature 0,
Anthropic refuses that for this model, and the grade came back Unknown, which an eval counts as nothing rather than
as a failure. An Unknown that never goes away is a failure nobody sees. Then the strict judge failed good recipes
over water, salt and a serving line, so the rubric said those don't change a recipe. Skill 12 (evaluation: calibrate
on both classes) and skill 1 (models and providers: a provider's limits are the factory's business).

**FX58: a limit on how often you act has to finish the job.** The chat redraws at most every 50 ms, and an update inside that window was simply dropped, trusting the next event to carry it. That holds while tokens pour in and fails exactly when it matters: the last update before a long tool call stayed hidden for the whole wait, so the page looked stuck and then seemed to dump everything at once. A throttle needs a trailing call. The test had to move time by hand, because the bug is what doesn't happen when nothing else arrives. It's only part of the stall, though: a search that takes 20 seconds still takes 20 seconds, and the fix only makes the screen honest about it. Skill 6 (the agent loop: what the reader sees is part of the stream).

**FX57: an error can be somebody else's, and still be yours to name.** The dashboard filled with *A task failed and
nothing awaited it*, in clumps of four, long after the page that caused them. PuppeteerSharp fails a private task whenever a
page or an iframe goes before its script context exists, and the runtime only reports it when the garbage collector gets
there, which is why the errors never lined up with anything. FX36 could await the failing task for redirects; this task is
private to the library, so the only choice was to say what's expected. The filter holds the exception's type and its exact
words, so a timeout from the same library is still an Error. An error log you've learned to ignore is the expensive part:
Louis asked about it, and nothing had been lost. Skill 17 (observability: a log line has to mean something).

## FX150 · Work that runs later must hold what it needs from the start

The chat redraws at most every 50 ms, and a catch-up redraw fires up to 50 ms after the last event (FX58). In PR
#327's CI run a test had already let its page's services go by then. The widget looked its business windows up in
those services on every redraw, so the late one threw, and it threw on the timer's thread, where nothing catches
it. That took the whole test host down: every test in the run failed and none was named. The fix takes the windows
once, when the widget starts. Anything that runs later, on a timer or after a wait, should hold what it needs from
the start, and not look it up in something whose life it doesn't control. And a test run that dies without naming a
test is a thread nobody was watching, so look for timers before looking at the tests. Skill 18 (testing and
shipping: a failure with no name).

## FX151 · A rule that is right for one kind of page can be wrong for the next

The section list marked a section once its top passed a line 140 px under the top of the screen. On the slide
pages every section is a screen tall, so the rule had never been wrong. Louis's CV was the first scrolling page
with short sections at its end: the page ran out of scroll 20 px before Education's top reached the line, so
Education was never marked, and Skills stayed marked while Education filled the screen. Louis's pick of three: mark
the section you see most of, and the last one at the page's end. A rule proved on the pages you have is proved for
their shape, so a page of a new shape is the moment to read the old rules again. There is no test runner for the
scripts, so it was checked by a scroll sweep in Chrome, and the first sweep looked wrong only because the smooth
scroll hadn't settled: measure after the page stops moving. Skill 18 (testing and shipping: checking by eye, with
care).

## FX152 · The model copies your example, so the example is the rule

The guide's choice card read "Tell you my business" over "I'll point you to the demo closest to it": two speakers
on one card. The words are the model's, but it had copied the title from the `Offer` tool's own description, which
gave exactly that as its example of a choice a visitor writes in, and nothing said whose voice the line under it
is in. Changing the example, and one line of the toolset's guide, changed what the model wrote: in seven guide
calls (about R0.22) it offered a box twice, and both times asked "Tell me about your business". An example in a
tool's description is read on every request and outweighs the rules around it, so it has to be the best version of
what you want, in the right voice. The same look found the answer drawn twice, on the folded card and as the
visitor's bubble: the rule that leaves a card's answer out of the bubbles (FX142) was written before a choice could
hold a box (FX147) and never learnt it. When a new kind of thing is added, find the rules that list the old kinds.
Skill 4 (tool design: the description is the prompt) and skill 7 (context engineering: an example beats a rule).

## FX153 · A page is behind the key only if what it reads is

On louis-agent.dev the CV's editor at `/admin/resume` opened for anyone. Nothing could be changed, since the save
route refused without the key and a test held that. But the page was there, with every edit button on it. The
admin's frame checks nothing itself: each admin page is closed because its own read is refused. The editor read
the CV on the route the public page uses, which was already there and is open to anyone, so its read was never
refused and it drew. Its test held its address and its frame, and its summary said it "sits behind the admin's
key", a sentence nothing checked. The fix gives the admin its own read, behind the same key as the save, and a
test in which the API refuses and the editor must not open. Access is decided where the data is read, not by the
address a page lives at or the frame drawn around it, so an open route reused in a closed place opens the place.
And "it is protected" is a claim like any other: the test that proves it is the one that refuses, not the one that
lets you in. Skill 15 (safety and security: closed by default, and proved by a refusal).

## FX154 · Check the thing itself, not something like it

Save as PDF shipped having been checked on a wide screen with the print rules switched on. That is not a printed
page. A sheet of A4 is narrower than the site's phone breakpoint, so the real PDF came out in the phone's layout,
seven pages long. It had black margins and a grey cast, from two things no screen check shows: a grain the
framework paints as a layer fixed to the window, which print repeats on every sheet and turns into a picture, and
the browser's own dark canvas, which paints a sheet whose page says it is dark, under every background rule. Louis
found all of it by printing once. Chrome prints a page to a PDF file from the command line, and a PDF's fills can be
read out of the file, so the real thing could be checked in seconds: six variants of the page, one rule each,
showed the dark fill was Chrome's and not ours. When a check is a stand-in, say so, and look for the way to make
the real thing. He also chose the layout from three examples he could open and print, after picking differently
from three sketches: a choice about how something looks is made by looking at it. And on paper a link is its QR
code, always, since paper can't be clicked.

Its pull request then failed two checks nobody had run. To give the printed link black ink, two names went into
the framework's list of colours, and that list is not one project's file: every site has to set each name on
it, and the films are recorded with its fingerprint. The site's and the web's tests passed, Puls's and the film
tool's did not. "Test what you touch" counts what a file reaches, not the folder it sits in, so a change to a
shared list runs the tests of everything that reads it. The link now takes the ink of the page it is printed on
(Louis's pick), which is what the CV's own print sheet was already making it do. Skill 18 (testing and
shipping: the check has to be of what ships).

## FX155 · A list written twice is two lists

The site's own program registers what its pages ask for, for the browser. The site's host registers what its pages
ask for, for the page it draws for a search engine. They were two lists written by hand, and when the showroom
came, its demos went into one. So a crawler's showroom had a heading and no demo, on the live site, and no test
said so: each test that drew a page "as the host serves it" wrote a third list of its own. The fix gives the host
one named list a test can call, and every such test now draws with it. A test that builds its own copy of the
thing under test proves the copy.

Three things were said about this before they were checked, and all three were wrong: that the old start gave a
crawler a link per demo, that a demo's page gave it an empty rail, and that each demo's address would be its own
page. One request to the running site, read as a crawler reads it, settled each one. The host draws a page once, at
its own route and without its layout, and serves that for every place on the page. So what a crawler gets is asked
of the server, never worked out from the components. Testing and shipping (skill 18).

## FX156 · A shared look is only shared by what names it

The pictures got one look in steps, a kind at a time: the component diagram, the cards, the sequence, the class
chart, the flow. Each step moved the kind it was about. The card an agent draws in the chat was built
before any of them, with a style of its own, and no step was about it, so it stayed a navy band over a torn edge
beside a page of glass. Nothing was broken and no test could fail: a rule held that every drawing goes through the
one builder, and it named the ticket as the exception. An exception written into a rule is a place nobody looks
again. When a look is meant for everything, the check to write is the list of what does not wear it yet, and it
should be empty or say why.

It was also a look decided by looking. Three drawn options were put, and the pick was made sharper once it was
on screen: glass, and nothing of a ticket. The words "our theme" would not have settled that. Skill 18 (testing
and shipping: what a visitor sees is checked by seeing it).

## FX157 · A refusal should say what is really wrong

The site's container was rebuilt while Louis had the guide open. His next message was refused with "sign in, or
enter AGENT_API_KEY", a builder's words, on a page where visitors never need a key. Nothing was locked. The
conversation was simply gone, because the server keeps its sessions in memory and the restart forgot them. The key
gate stands in front of every handler, and it only knew that it hadn't let this session in, so it said the one thing
it knows how to say. The handler behind it would have told the truth, a 404, and the page already had kind words
for that.

The fix lets the door ask before it refuses: does the host have this session at all? If not, the call goes on to the
part that can answer it. A gate that answers for something it can't see turns one problem into another. What stays
closed is what exists and isn't yours, which is the same as before. Skill 15 (safety and security: closed by
default, and a refusal that names its real cause).

## FX158 · A scoped style is scoped only at its last step

Hovering over Contact, a plain link, opened both menus in the header. The menu group's styles sit beside it and are
scoped, so it looked safe to call its root `site-menu`. But Blazor marks only the last part of a selector with the
component's scope. `.site-menu:hover .site-menu-list` meant "a list of mine, under anything at all called
site-menu", and the frame round the whole menu was called that too. The fix gives the group names no one else uses.
The test reads the class the list opens under from the stylesheet itself and counts how many elements round each
list carry it. It found two before the fix and finds one after. A scope protects what a rule lands on, never what
the rule looks up to. Skill 18 (testing and shipping: what a visitor sees is checked by seeing it).

## FX159 · A rule about one piece outlives the reason for it

F151.6 kept the header out of glass, so its switch and its CV were drawn by hand in plain colours, and the notes
said "a bar is never glass". When Louis asked for a glass header, the change was not new drawing work at all. The
surface tool already had the group, the main button and the quiet one, each glass where the theme draws it and
plain where it doesn't. The hand-drawn pieces only had to ask for them. Drawing a piece through the theme's own
kinds, even when today's look is plain, is what lets the look change later in one place. Skill 18 (testing and
shipping: what a visitor sees is checked by seeing it).

## FX160 · An address is only a way there if something takes you

The hero's new button pointed at `/home/does`, the very address the section index uses, and it did nothing. The
index's links work because each also names its section with `data-section`, and the frame's script moves the page
for those. A bare link to the same address went to Blazor's router instead, which saw the same page and stayed
put. The test passed on the address and missed the move. Clicking it in the browser found it, and the test now asks
for the section's name too. Skill 18 (testing and shipping: what a visitor sees is checked by seeing it).

## FX162 · A page made of data can't lose its look

When the site's own stylesheets came out so it would wear the theme alone, nearly every section lost its look at once.
They were written as headings, paragraphs and lists, and their look lived in the sheets that went. Nothing in them was
the theme's. The cure was a rule (ADR 0125): a site's pages hold no HTML, only the framework's controls given words and
data. A heading with its summary is one control, named entries another, the design page's cards, films and fan more.
Once a section is data handed to a control, the theme reaches it, and changing the look is one change in one place.
The design page was the truth throughout, its own rules taken as they stood, and a name that needed a dictionary
(`lede`, `kicker`) became one that didn't. Skill 18 (testing and shipping: what a visitor sees is checked by seeing it).

## FX220 · Count what overlaps by holding it open, not by hoping

The test proves that a parallel engine runs a round's three tool calls at once, by counting how many run together.
Each call waited 150 ms and ended, so three were counted only if the third had started before the first was done.
On a quiet machine it always had. With every core busy the third came late, and the test said 2, seven times in
twenty. The cure is the one FX219 found: hold the slow thing open on purpose. Each call now waits until the round's
last call has started, with a cap so a broken engine fails instead of hanging, and the count no longer depends on
timing. The lesson is that a test of concurrency must make the overlap certain, never likely: a gate that opens
when all the parties have arrived, not a delay long enough on the machines it was tried on. Skill 18 (testing and
shipping: a test passes for its reason, not its timing).

## FX83 · A rule that nothing runs holds nothing

The DevOps and Paymo packs broke ADR 0028's rule that a waiting tool takes a cancellation token, and the check that
says so had seen it once, when the coding agent still loaded them. From the day it stopped, no catalogue loaded them,
so no test reached them, and the breach sat in the TODO as a note. It showed again only when a project held every pack
at once, which is the lesson: a check that runs per catalogue covers only what some catalogue happens to load, so a
rule about every pack needs a test that lists every pack. The fix was the other half of the same thought. Two packs
nothing loaded and nothing published were costing a rule exception, so they went, and the list of known breaches went
with them, empty. Skill 18 (testing and shipping: a guard covers what it runs on, nothing more).

## FX219 · A test that races the machine loses on a faster one

The test for "a re-score while another run runs is a 409" started a real stub run to keep the runner busy, and said
so in its own comment: the runner "takes longer to start and finish than this request takes to arrive". On this PC it
always did. On a CI runner the stub sometimes finished first, the endpoint saw nothing running, and answered 201. The
cure was to stop racing: the busy run is now the stand-in runner's `sleeper`, which holds four seconds on purpose,
and the run to re-score is written to the store directly. The lesson is that a test which depends on one thing being
slower than another holds only on the machines it was tried on; hold the slow thing open on purpose, or wait for the
state you need, never for a speed. Skill 18 (testing and shipping: a test passes for its reason, not its timing).

## FX218 · A default someone relied on is a contract nobody wrote down

F166.2 gave every package a version from its tags. One older test packed a pack with no version at all and asked for
1.0.0, the number .NET gives when nothing says otherwise. Once MinVer said otherwise, the pack came out as
`0.0.0-alpha.0` and the restore failed on CI, after the merge, since the integration tests run only there. The cure
was for the test to say the version it means when it packs. The lesson has two halves: a default that code leans on is
a promise no file records, so changing who decides it means searching for whoever leaned; and a change to how
everything builds runs the integration tests before it merges, not only the unit suite. Skill 18 (testing and
shipping: the tests that leave their process are part of the change).

## FX217 · What a browser shows is not what a search engine reads

The framework's site went live with the brand's mark as an SVG, and every browser tab showed it, so it looked done.
Google Search still put a grey globe beside each of its results. Google reads only bitmap icons, PNG, ICO and the
like, and never an SVG, and it takes one icon per host name, so the framework's subdomain could not borrow
louis-agent.dev's. The cure was to name the same PNGs louis-agent.dev already serves, with a test that the files stay
the same on both sites. The lesson is that a crawler has its own list of what it accepts, so checking a page in a
browser doesn't prove what a search engine will show; the search engine's own guide is the thing to check against.
Skill 18 (testing and shipping: what a crawler is told is part of what ships).

## FX216 · A copy that says it's the original is still a copy

Search Console left 160 of louis-agent.dev's pages out, 95 of them as "Duplicate, Google chose different canonical
than user". Every section had its own address, each answering its whole page word for word with a canonical link that
named itself, and the sitemap listed them all. On top of that, www served a second copy of the site, because the
canonical's host came from each request. Google saw dozens of copies that each claimed to be the original, believed
none of them, and picked its own. The cure was to give every page one original and say so three ways: the canonical
names the page's route, the sitemap lists pages only, and a page on any other host answers a 301 to the address in
`SITE_ADDRESS` (ADR 0128). Two things were learned. A feature for people, an address for every section, can be a
problem for a search engine unless the page says which address is the original. And "no setting to keep right" stops
being simpler the day a second host name reaches the app. Skill 18 (testing and shipping: what a crawler is told is
part of what ships).

## FX215 · A trust page is true first, and reassuring after

The trust page held four Azure facts to Puls's Bicep file. Three were still true and only needed a source the
framework keeps. The fourth, *Rebuilt from code*, said in place that everything in Azure is described in one Bicep
file, a file that still described the VM and the SQL server FX122 had removed and that leaves with Puls. A page
meant to earn a reviewer's trust can't claim more than is there, so that tile now says Not yet and what is true: one
script releases both sites, and the environment they run in was built by code being retired. A test holds that no
fact rests on a demo's file, since a demo can leave and take its files with it. Skill 18 (testing and shipping: a
claim held to a file is only as good as the file staying).

## FX214 · A fix that removes a thing removes what says it's there

Puls left Azure with FX122, and its VM with it, but four of its own files kept saying "the Azure app has its own
browser and Mongo on a private VM". One even named the `deploy.ps1 vm` verb that F166.1 took out. Each sentence was
true when written, and nothing checked it afterwards, because a comment in a settings file is read only by the next
person to open it. The cure was to search for every place that says the removed thing exists, at the time of the
removal, not to wait for someone to be misled. Skill 18 (testing and shipping: a removal is finished when nothing
says the thing is still there).

## FX213 · The framework's words name no product

The eval runner's stub, which every demo's portal lists, described itself as proving "the portal, not Puls". It was
written when Puls's evals were the only ones beside it. The runner is the framework's, so its words name no product,
and a test now reads the demo identities' names and holds the stub's description to none of them, so a new demo or
Puls leaving changes nothing. Skill 20 (extensibility: what ships to everyone speaks to everyone).

## FX212 · A test owns what runs in the folder it made

The git tests make a small repo each, commit to it, and delete it after. On CI one delete failed on a lock file that
had vanished: git had started its own housekeeping in the background after the test's commit, and it finished while
the teardown was clearing the files' read-only flags. The teardown could have learned to forgive a missing file, but
the housekeeping would still be working in the folder while it was deleted. So each test's repo now turns it off,
`maintenance.auto false` and `gc.auto 0`, beside its other settings, and nothing runs in it but the test. Skill 18
(testing and shipping: a test that makes its world decides what runs in it).

## FX211 · A test's data is the smallest that shows what it checks

The recorded chat's hold test passed every time on the PC and timed out on CI. bUnit's own count said why: 5,107
renders in ten seconds, and still going. The test played the whole recorded session, 1,503 events a turn, and the
chat draws once per event even at no pace, so two turns took one or two seconds here and more than ten on CI's
runner. The test is about the hold, not about a long answer, so its turns now play a whole answer in four events,
and it runs in the 400 ms it holds for. A bigger timeout would only have made the race rarer. Skill 18 (testing and
shipping: a minimal fixture is fast on every machine).

## FX210 · Two sides of one wire agree on a name only when it's written down

Every file uploaded to a pack reached it with no name. The admin page wrote the body in the admin wire's snake case,
so the name went as `file_name`. The API read the body with ASP.NET's defaults, camelCase that ignores case, so it
looked for `fileName` and found nothing. The slot and the bytes have one-word names, which match either way, and
shop-prices reads its files by slot, so no one saw it. A test on a fake upload that reports each file's name showed it
at once. The repo had already solved this twice: a request record names a two-word property's wire name with
`[JsonPropertyName]`, as `time_zone` and `row_id` do, so neither side's naming policy decides it. Then every other
body the API reads was checked for the same gap, and none had it. Skill 18 (testing and shipping: a fake that echoes
what it got finds what a real pack never looked at).

## FX209 · A warning that names a CVE is an error

Every build had warned NU1903 for a while, and Rider had it as an error: the Blazor WebAssembly server package at
10.0.13 carried CVE-2026-102870, a remote code execution in the debug proxy that the advisory rates high. The build
still succeeded, so nothing stopped, and the warning sat beside a harmless one about a comparison. The fix was a
version, 10.0.15, in the five hosts that pin it, and `dotnet list package --vulnerable` came back empty. A known
vulnerability isn't noise to read past: the advisory says what is hit, and the patched version is usually one line
away. Skill 15 (safety and security: what the build already knows is cheaper than an audit).

## FX208 · Two buttons that look the same are one too many

On a phone, louis-agent.dev's header had two round buttons, both drawing the same three lines, and the page slid
sideways. The flag that had just changed, beta-1, was the first suspect, but the old alpha-1 flag was the same width.
The picture settled it: the brand, the flag and the two buttons, with their gaps, needed about 406 pixels on a 390
screen, and the two buttons had stood side by side since FX163 put the settings behind a three-line button of their
own. The fix took one button away rather than squeezing the row: on a phone the menu's sheet holds the settings at its
foot, which the frame already did for a site with no settings. A layout fault on a phone is often a duplicate, not a
lack of room, and the last change is a suspect to check, not the cause. Skill 18 (testing and shipping: a look is
proved by looking, and measured from the picture when the browser can't be scripted).

## FX207 · Mark it where you already know it

Louis asked whether the overview could tell his own talks from his clients' by their internet address. It could, but
the privacy page promises no address is ever written down, and one address is shared by a whole office or changes on
a phone. The browser already knew the answer: only Louis's browser holds the admin key. So when it opens a session it
says so once, behind that key, and the session carries the mark into every turn it logs. A visitor's browser holds no
key and sends nothing, so nothing new is kept about them. The best place to record who someone is, is the place that
already knows it, not a guess from what the network shows. Skill 18 (testing and shipping: each part of the path has
its own test, from the browser's call to the column on the page).

## FX206 · Borrow the shape, check the colour it brings

The header's flag moved from alpha-1 to beta-1, and its look from a solid violet pill to a build status: a quiet tag
with a small violet dot that breathes. The framework already had a status pill with a live dot, so it was the first
thing reused. In the browser it came out green, because a status pill means "all is well" and wears that colour. A
violet dot on a green pill is two accents on one element, which the brand rules out. The plain tag carried the same
shape with no colour of its own, and the dot became the one accent. The tests could not see that clash, only the
browser could. Reuse the shape, then look at what colour comes with it. Skill 18 (testing and shipping: a look is
checked by seeing it).

## FX205 · A second way in counts the first press again

On a desktop the bar opens when the pointer comes onto the orb. That was added later, beside the press that opens it.
The press still believed it was the only way in, so when it found the box open it closed it. A visitor who hovered and
then clicked saw the bar open and shut at once. The test played that order, a hover and then a click, and it failed.
The fix asks who opened the box: if only the hover did, the click is the visitor's first press and holds it open, and
the next click closes it as before. The same change took three small things a visitor met on the way. A chip asked
about a price the site no longer names, the slogan broke over two lines on a phone, and the tape spoke in tool names.
Each got a test that pins the words or the rule. When a second way into the same state is added, every action that
read that state has to learn which way it came. Skill 18 (testing and shipping: play the order a person really uses).

## FX204 · A thing that changes while it leaves must be asked again

Entering the free-website entry, the page threw an error and the red bar came up. Orbi's answer had begun as a short
caption over the bar. Then, a moment later, the same answer grew a form, and a form needs the whole talk, not a
caption. The bar had already put that answer on its way out, fading, and it kept asking it for its caption. There was
none any more, so it fell over. The test wrote that exact order of events down: an answer captioned, then given a
form, then let go. It failed, and one line made it pass: an answer that now needs the whole talk leaves at once. It
only showed with a model that streams at a person's pace, so a fake model that waits between its words found it. A
stream changes its mind as it goes, so whatever holds a piece of it must ask again before it uses it. Skill 18
(testing and shipping: reproduce it at the speed it really happens).

## FX203 · An agent that passes things on must read them first

Someone typed keyboard mash into the free-website entry, and Orbi laid it on a neat card with Send it suggested.
Nothing had asked Orbi to read the answers. The recipe said ask, show and send, so it did exactly that. Whether
"sadfsadga" is a business is judgement, and judgement belongs in the prompt, so the fix is one rule there. Orbi now
reads what comes back, says kindly which answer it couldn't use, and asks for only those boxes again. Louis reads
every entry, so nonsense never reaches the card. A step that hands work on to a person needs a moment where the
agent looks at what it is handing on. Skill 7 (context engineering: the prompt says what to judge, not only what to
do).

## FX202 · A fix for one state is a fix for every state

FX200 took the capsule off a long answer's box, but only while it sat still. Click into it and the box put the
theme's focus drawing back on, and that drawing is a pill made for a one-line field. So the capsule came straight
back, now with a violet edge. A control has several looks, at rest, in focus, pressed and turned off, and each one
can come from a different place. Now a long box has its own focus look: the same corner and wash, with an edge and a
soft ring in the brand's colour. When a fix changes how something looks, look at it in every state, not only the
one in the screenshot. Skill 18 (testing and shipping: what a visitor sees is checked by seeing it).

## FX201 · When the next step is a form, make it the only step

Pressing "Enter your business" sent Orbi a question, and Orbi did several things at once. It greeted the visitor,
then asked with the form, all in one answer. The form never showed, and on the next turn Orbi pointed at a form
that wasn't there. A model given a friendly voice will warm up first, and every extra step in the same answer is
one more place for the form to get lost. So the entry recipe now says the form is the first and only call, with
one short line over it. If the visitor can't see it, Orbi asks again instead of pointing at it. A button that
promises a form should lead straight to one.

## FX200 · A prompt can ask for what its tool can't do

The free-websites entry told Orbi to fill in what the visitor had already said, so they would only add what was
missing. It sounds helpful, but the form tool has no way to hold an answer. A field is a label and a kind, and the
label is at most 30 characters. So the model had nowhere to put those answers, and the form came back refused or
never showed. The quote recipe beside it already said the right thing, ask only what they haven't told you, and it
works. Now the entry says that too, and names its four short labels. The lesson is to read a recipe against the
tool's own parameters. When a prompt promises a step the tool has no slot for, the model improvises, and the
visitor sees the seam.

## FX199 · A showcase is only as good as its weakest control

The theme page shows every control a site is built from, and a visitor tries them. Three of its buttons were links
back to the page itself, so a press did nothing, and the off controls had no words, so they looked broken. A
showcase that has one dead control makes the whole kit look unfinished. Now each button counts its presses where you
can see it, an off control says it is off, and the page holds no link at all. That also fixed a quieter fault, since
the framework's guide had named one site's address. Every control was then pressed in a real browser before it was
shown. Skill 18 (testing and shipping: what a visitor sees is checked by seeing it).

## FX198 · A way home has to look like one

On every inner page the only way back to the landing was the site's name in the top corner. It did lead home, but
nothing about it said so, and a visitor on the privacy page saw no way back. The fix put a plain Home link first in
the menu, where people look for it. The same change took the CV mark out of the settings, since the landing now
shows who builds the site and leads to the CV from there, and the privacy page now calls the agent by its name,
Orbi. The lesson is that a link a visitor can't recognise as one is no link at all. Skill 18 (testing and shipping:
what a visitor sees is checked by seeing it).

## FX197 · A fixed layer covers whatever has no layer of its own

The footer had been at the very bottom since FX163, but nobody could see it. Under the guide's bar the page fades
into a strip of the page itself, so nothing passes sharp behind the bar. That strip is fixed to the screen's foot,
sits at layer 9 and is solid in its lower half. The footer sat in the page's normal flow with no layer of its own,
and a positioned layer always paints over plain flow, so the strip covered it. A click still reached the footer,
because the strip lets the pointer through. That is why a check by what is under the pointer said the footer was
on top while the eye saw nothing. Raising the footer to layer 10 brought it back. It is still under the bar, the
header and the panel. Two small things showed up once it could be seen: the corner's LinkedIn link sat on its
right-hand words, and on a phone the orb sat on its middle. So the footer keeps room for each. The lesson: anything
fixed over the page that hides content must say what it lies under as well as what it lies over, and the check for
"can people see it" is a look, not a hit test. Skill 18 (testing and shipping: what a visitor sees is checked by
seeing it).

## FX196 · Paint through letters, and their edges go soft

The big headings wore a gradient clipped into their letters, a violet band on glass and the words' own colour on flat.
On Louis's screen the edges read as a soft rim, though a local screenshot looked nearly the same as plain text, so
the exact cause on his screen was not pinned down. What is known is that a background clipped to the letters is
painted as an image in their shape, not drawn as text. So the heading now takes its strongest colour as ordinary
text, and the theme value that held the band is gone, since nothing reads it. The lesson: when a look can't be
reproduced but the plain way does the same job, the plain way wins, and a heading that must read crisp everywhere is
plain ink. Skill 18.

## FX195 · What a guide may offer is what the site still has

Two demos left the site, and with them their pages, their rail entries, their recordings and the triage pack's web
screens that only those pages used. The pages were the easy part. The guide's prompts named them too, in its index of
pages, its site map and the doc it reads to explain the demos, and a guide reads its prompts as the truth: left alone
it would go on offering a page that answers with nothing. So a removal is not done until every text the model reads
is searched for the address, the same way the code is. The lesson: the prompt is part of the site, and a page that
goes takes its lines in the prompt with it. Skill 18.

## FX194 · A button with no rule falls back to the browser's grey

When the site's own styles came out, the demo's *Try it yourself* stayed the site's own `button class="demo-try"`,
and nothing styled it any more, so it showed as the browser's plain grey box. The cure was to stop the site drawing a
button at all: the framework's recorded chat already ends with a button of its own, so it now draws this one too, as
the theme's main button, from the words the page hands it. The same look emptied the stage round the agent. The cues,
the notes and the offer bar each had a reason, but together they pulled the eye off the agent, which is the whole
point of the page. The lesson: a site that hands its look to the theme must hand over every element, and a page
that shows an agent should show little else. Skill 18.

## FX193 · A colour that works on black can vanish on white

The two themes' dark looks set a pale red, amber, blue and green for the demos, and the light looks simply kept them.
On black a pale colour glows. On glass's violet page the same blue stood at 1.4 to 1 against it, so a demo's button
was white words on a pale wash and two of the dock's icons all but disappeared. The purple had always had a deeper
value for the light, which is why the brand's own buttons looked fine and hid the problem. Each light look now has
its own deeper shade of every accent, and a test measures each one against white with the same contrast sum the
web's guidelines use. The lesson: a colour isn't readable on its own, only against what it stands on, so each look
sets and checks its colours for its own page. Skill 18.

## FX192 · A measure written for one layout breaks when the layout moves

The guide's bar tells the page how much room it takes at the foot of the screen, so nothing ends under it. It worked
that out by adding its chips' height on top of its row, because the chips used to stand over the row. Then the chips
moved to stand round the orb while the bar is closed, and nobody told the measure. It went on adding their height
on top, so the bar claimed 384 pixels where it stands in 234, and the showroom's deck, which takes what the bar
leaves, shrank to its least. Nothing looked broken in a test, since each part was right on its own. The measure now
asks where the chips really are. The same look at the page found two colours that only failed in one look: the
question in each window took the page's ink as its fill, white on white in the dark, and the website's words took
the theme's brightest, black on a photo in the light. Both now use what the chat and an action already use. The
lesson: when a part changes where it stands, look for whatever measures it, and look at a page in every look, not
one. Skill 18.

## FX191 · Taking the styles out takes the page with them

FX162 took every stylesheet out of the site so that its look would be the theme's alone. That was the right rule, but
the CV's parts still drew their own HTML, a hero grid, a column of dates, a row of tags, and those classes now
pointed at nothing. Every test still passed, because the tests found the parts by their classes and the classes were
all still there. The page itself was a heap: the summary ran into the name, the jobs had no date column, and it no
longer printed as a sheet. The fix finishes what FX162 started. The shapes a CV needs became framework controls, a
person's hero, a strip of facts, a timeline, a panel and a row of tags, each with its own layout on a screen, on a
phone and on paper, and the CV now only gives them its words. A page that prints stands in a paper sheet, which
sets the A4 page for any site. The lesson: when a rule removes what draws a page, a test that finds markup can't see
the damage, so the page is looked at, on a screen and on paper, before the rule is called done. Skill 18.

## FX190 · A browser drops a line it can't read, silently

`font: 500 13px/1.4 inherit` looks like it says "this size, this weight, the rest from the parent". But `inherit` can
only stand alone in a shorthand, so the browser throws the whole line away without a word, and a button falls back
to the browser's own button font: centred, and on a phone larger. The guide's answer card had four such lines, so on
a phone its choices, its links and its way into the box all grew, and the way into the box broke over two centred
lines. Each now inherits the face alone and sets its size, weight and height apart, and a rule test refuses the
shape anywhere in the styles. The lesson: CSS doesn't report a line it can't read, so a test has to look for the
shapes that fail quietly. Skill 18.

## FX189 · A file that changes needs an address that changes

The server lets a browser keep a theme's drawing for an hour by its address. Flat's mark showed what that costs: a
drawing changed, its address did not, and Louis's Chrome went on drawing the old one even after a hard refresh,
since a drawing reached through the stylesheet was not fetched again. Glass's drawings and its silk had the same
risk for every returning visitor. They now carry their version in their address, raised whenever one changes, so a
new drawing is a new address the browser has never kept. The lesson: caching is keyed by the address, so anything
that can change must change its address when it does. Skill 18 (testing and shipping: a test holds every drawing's
address to its version).

## FX188 · Things that do the same job should say the same thing

The contact page had three WhatsApp buttons. One said the number and two said "Chat on WhatsApp", because each was
written where it stood. A visitor reads three different buttons as three different things. They now all say the
number beside the mark, and a test counts them and holds them to one label. The lesson: when one of a kind changes,
look for its siblings. Skill 18 (testing and shipping: a test holds every one of a kind, not one).

## FX187 · Letting words wrap means letting the box grow

FX180 let a button's words wrap in its column so its mark stayed in view, but the button kept the height of one
line, so the second line spilled out under it. A box that lets its words wrap has to let itself grow too. In a
column the button now has a least height instead of a fixed one, and its row's buttons stretch to the same height.
The lesson: one change to how text flows is two changes, the text's and its box's. Skill 18.

## FX186 · A component that borrows a colour borrows its meaning

A tile's icon stood on the panel colour, which in glass is white and in flat is the card's own grey, so in flat the
plate vanished into its card. The plate borrowed a name that meant "a panel", not "a plate that stands out from a
card". It now has a name of its own, and each theme says what a plate is: white glass with a soft shadow, or the page's
own colour with none. The lesson: a value is chosen for what it means, not for the colour it happens to have in one
theme. Skill 18.

## FX185 · A hover that only changes a line's colour

The flat design's card answers the pointer by turning its one line a darker grey. Ours drew a second ring outside the
line, so the edge looked two pixels thick. A border can't take a new colour from a theme value, but a shadow can be
replaced. So flat draws its line as a one pixel line just inside the edge, and the hover puts its own line in that
same place. The buttons answered nothing at all in flat, because their hover lived only in glass's drawings; the
hover colours are now values every theme sets. The lesson: when a look lives in one theme's pictures, the other
theme inherits nothing, so the behaviour has to be a value too. Skill 18.

## FX184 · Measure against the design before changing it

Louis found the wordmark's o too big. Measured, it matched the flat design to half a pixel, so the design itself was
too big there. That made it a choice, not a bug, and it went to him with pictures at three sizes. His answer was the
reason the design had missed: glass's o is a ball inside a glass bubble, and flat has no bubble, so flat's o is the
ball alone, glass's ball's size, half the letters' height. Its size is now a
theme value, and the o's drop is worked out from its size, so it stands on the line whatever the size. The lesson:
measure what is there before fixing it, so the fix is the right kind. Skill 18.

## FX183 · A value hidden in a recipe can't be themed

The band through a heading's words was a gradient with the violet written into it. No theme could switch it off, so
flat's headline could not be solid as its design asks. Naming the band's colour as a value, violet by default, lets
flat set it to the words' own colour, and the band disappears. Glass is unchanged. The lesson: anything a theme might
want to differ on has to be a named value, not a literal inside a style. Skill 18.

## FX182 · A theme starts from the defaults, so check what it kept

Flat had kept the framework's three soft lights behind the page and the framework's glow round the bar's box. Neither
is in its design, which has no gradient and no shadow anywhere. They were never chosen, only inherited. The lesson:
when a theme copies a starting sheet, every value it keeps is a decision it made without looking. Skill 18.

## FX181 · A brand drawn only in one theme vanishes in the other

The brand's l on the orb existed only as glass's drawing. Flat drew nothing, so its orb showed whatever letter the
page handed it: nothing on the landing, a capital L in the bar. Flat now has its own drawings of the mark, the l with
a hard violet step under it, and gives the handed letter no ink, as glass does. The lesson: a brand mark belongs to
every theme, so check it in each one. Skill 18.

## FX180 · A shared part changes everywhere it is used, so let each use ask

The contact page's cards put their buttons in a row that wraps, so a long button dropped to a line of its own and left
a gap beside it. The first fix made every row of buttons two even columns, and the photograph of the landing showed
what that cost: its closing band uses the same row, and its two buttons stretched across the whole page. So the row now
takes a setting, how many columns, the way a row of tiles already does, and only a tile asks for two. A second slip
showed in the same photograph: centred in a column too narrow for its words, a button pushed its WhatsApp mark out of
sight, so the words now wrap inside the column. The lesson: before a shared part changes, find every place that uses it
and look at each one. Skill 18 (testing and shipping: a test holds both kinds of row).

## FX179 · A hyphen is a place the browser may break a line

The alpha flag in the header reads "alpha-1". On a wide screen it has room, but on a phone it sits between the name
and the header's two buttons, and the browser looked for a place to break it. A hyphen is one such place, so the flag
stood as "alpha-" over "1", which reads as a mistake. A short label that must read as one piece says so with
`white-space: nowrap`, and the browser keeps it whole. The lesson: a label that looks fine at desktop width needs its
look at phone width too, and the check after a feature is where it turned up. Skill 18 (testing and shipping: a
test holds the flag to one line).

## FX178 · A filter only paints inside its region, and the default region moves with what it blurs

Inside the glass orb, a few coloured shapes drift across the ball under one soft blur, which turns them into clouds
of light. An SVG filter paints only inside its region, and by default that region is measured from the box of what
it blurs. The shapes drift, so a shape that left the box was cut along a straight edge. At the bar's size the cut was
a fraction of a pixel; drawn large for the share card it was a plain line through the ball. Measuring the region in
the drawing's own units, the whole ball, means nothing inside it can be cut, however far a light drifts. The lesson:
a flaw too small to see at one size is still a flaw, and drawing something larger is how it shows. Skill 18
(testing and shipping: a test holds every mark drawing to the whole-ball region).

## FX174 · Draw a brand picture from the real thing, not a copy of it

Louis asked where his glass was: the card a shared link shows still had the flat disc. The glass ball lives in the
site's styles and the theme together, and no hand-drawn copy would stay true to it as the look changes. So the card is
drawn from the site itself. A tool starts the site, photographs the ball alone on nothing, and lays the photograph
into the card's page, drawn at twice its size so it stays sharp. Two slips on the way taught their own lessons. A
clip measured on screen after a scroll photographed an empty page, which a photograph of the element itself avoids.
And a faint square came along with the ball, which a round clip on the card took away. Skill 18 (testing and
shipping: a test holds the card's real size to what the page tells a link preview).

## FX177 · A second switch costs one line when the first was built to be switched

Louis wanted the way back up out of sight too. Because FX175 had already made the frame's extras switches a site
sets, this was one more switch beside the first, and the frame's script, which places the way up under the index,
already did nothing when either was missing. Building the first removal as a switch, not a deletion, is what made
the second one small. Skill 20 (extensibility).

## FX176 · Hidden is the browser's word for "not now"

Louis wanted the Demos and The framework menus out of the way until their pages are fixed, but not deleted. The
menu group gained a switch that sets the browser's own `hidden` attribute, which takes the group out of sight, out
of the keyboard's reach and away from screen readers, while its links stay in the page as they were. Turning the
switch off brings the group back exactly, and the tests that hold its links still read them. Skill 20
(extensibility: a framework part a site can hide for now and keep).

## FX170 · The browser's own popover before a script of ours

Louis wanted a flag in the header saying the site is at alpha-1, with a short note behind it. A note that opens on
hover fails on a phone and for a keyboard, and one that opens on a press usually needs a script to open it, close it
on a press elsewhere and on Escape, and stand it over the page. The browser does all of that itself now: a button
names the note it opens with `popovertarget`, and the note says it is one with `popover`. So the flag is markup and
styles, nothing more, and it works the same everywhere the browser does. The flag sits beside the site's name and
not inside it, since the name is a link home and a button can't stand inside a link. Skill 18 (testing and shipping:
the test holds the button and its note named to each other).

## FX169 · A part any site could draw goes to the framework, even when one site asks for it

Louis wanted a section on the landing for the brand, with the theme beside it, and nothing in it the guide could be
asked about. The landing is the site's, but the mark shown large, with the name on four grounds, is a part any site
could use to show its own brand. So it went to the framework as `MarkShowcase`, given the site's name and letters,
and the site's section is only the framework's band, tiles and that part, configured. The tiles' own switch, `Pick`,
kept them out of the guide's reach without anything new. Skill 20 (extensibility: a site configured from framework
parts).

## FX175 · Off is not gone: a switch keeps a part for later

Louis wanted the section index out of sight on his site, but not deleted. The frame drew it on every site, with no
way to say no. A switch on the frame, on by default, lets a site turn it off while every other site keeps it, and the
part itself stays tested and ready. Deleting it would have meant writing it again the day it was wanted back. Skill
20 (extensibility: a framework part a site can switch off without losing it).

## FX173 · A measure written in three places drifts; name it once

Louis wanted the agent bar larger, its chips round the closed orb, and the agent's own suggestions first. The size
was the lesson. The ball's 76 and its room of 14 were written in the row, the orb and, since FX172, the closed dock.
A phone's rules set a smaller ball, but the closed dock still used the desktop's numbers, so on a phone it would
have closed to the wrong width. Named once on the bar, as `--agent-ball` and `--agent-air`, every rule reads the
same pair, and a phone sets the pair instead of each rule. The same went for the gap round the chips. And when the
bar's styles outgrew their file, the chips became a part of their own, since they were a concern of their own all
along. Skill 18 (testing and shipping: a test holds that the old number is gone).

## FX172 · When a thing shows is a rule of its own, worth a type

Louis wanted the agent bar to start closed and open only when the visitor or the guide needs the other. Every fact
that decides it was already in the bar: whether the guide is answering, whether the visitor is writing or picked a
piece, whether an answer stands. What was missing was the rule that joins them, and one fact of the visitor's own,
that they pressed the orb or are in the box. Put on a small record of its own, `BarFacts`, the rule reads as one
line and is tested case by case without drawing anything, while the bar only asks it. The same holds for the
orb's state, which the bar also asks of a type. Skill 18 (testing and shipping: a rule tested apart from its
drawing).

## FX171 · A page is more than its file

Louis asked for the "What we build" page to go, with everything of it. Deleting the page's file was the smallest
part. The menu linked to it, the sitemap listed it and two of its sections for search engines, a title test and a
page test held it, and the guide's own skills told it to send people there. Left behind, any of those would have
pointed a visitor or a crawler at a page that answers "not found". The tests found two of them, the sitemap and the
menu's count, and a search for the address found the guide's lines, which no test reads. The lesson is to remove a
page by its address, not by its file, and to search for that address everywhere the site speaks. Skill 18 (testing
and shipping: a test now holds that no page answers there).

## FX168 · A cascade carries what only the far end needs, past what lies between

Louis wanted the brand in the empty room on each side of the agent bar. The bar sits inside the chat widget, and
the site gives the widget its words. The plain way would pass two new slots through the widget, but the widget
was already at its size limit and knows nothing of brands. A cascading value fits better. The site wraps its widget
in a small component that holds the two sides, Blazor hands them down the tree, and the bar picks them up and draws
them beside its own row. The widget in between never sees them. So the sides line up with the bar exactly, with no
copy of its measures, and a site that gives none gets the bar as it always was. Skill 20 (extensibility: a framework
part a site configures without the parts in between changing).

## FX167 · A state the page already marks is one a component's styles can answer

Louis wanted the landing's three demos in glass windows, and the one picked brought to the front. Neither needed
anything new. The framework already had the window, the glass with three dots and an address word, which another
section fills with a website and its chat. Here it holds only the photograph and its caption. And a piece the
visitor picks already carries a mark the page's watcher sets, as does one the guide shows. The fan had simply never
answered it. So bringing the picked one forward is one rule in the fan's own styles, which also sends the middle one
back. The lesson is to look for what the page already says before adding script: a state that is marked can be
drawn. Skill 20 (extensibility: a framework part reused with a new body, configured from the site's records).

## FX166 · A slow page is measured before it is fixed, and a filter that repaints costs every frame

The site lagged even in the flat theme, which has none of glass's bending, so the theme's effects were not the
cause. Measured in the browser, the page drew about three frames a second while nothing happened. The browser's
own timing of each slow frame showed no script, no style and no layout work in it, so the time went to painting.
Two things were painting for good: every big heading swept a band of colour through its letters, and every
heading and summary carried nine blurred drop shadows. A moving fill under a filter makes the browser work the
whole filter out again on every frame, nine blurs over each heading. Taking the layers and the sweep out put the
page back to rest. The lesson is the order: measure what a frame spends its time on, then take out what the
measure points at, not what looks fanciest. Skill 18 (testing and shipping: a test now holds that no rule stacks
shadows or sweeps a heading).

## FX165 · A control takes its look from the design's drawing, not from the generic rule it inherits

The bar's send button looked broken: a flat violet circle that turned to a pale ghost whenever the box was empty. It
had never been given the design's look. It inherited the chat's generic send, and every disabled button on the site
fades to 40%. The design draws its send with the same press art as its main buttons and never greys it, because a
send that waits is still the way to ask. So the bar draws it with the framework's press, and a small tool keeps a
waiting press at rest. Skill 18 (testing and shipping: what a visitor sees is checked by seeing it).

## FX164 · A page taken down is taken out of what the agent reads too

The prices left the landing and the menu, yet the pricing page still answered at its address, and the guide still
quoted R500 to R1,000 a month and sent people to it. A page lives in more places than its file: the sitemap, the
guide's map of the site, its voice, the doc it answers from, and the tests that pin each. Taking it out meant every
one of them, and moving the address to the contact page so an old link still lands somewhere useful. An agent that
reads the site's own docs says what they say, so a stale line there is a wrong answer to a visitor. Skill 7 (context
engineering: what the agent reads is part of the product).

## FX161 · What a reload must keep belongs with what the reload brings back

A visitor closed the guide's answer, pressed Ctrl+R, and the answer stood over the bar again. The chat itself was saved
in the browser and came back on the reload, as it should. But that the answer was closed lived only in the bar's memory,
and a reload starts the bar empty, so it saw a last answer it had never been told was closed. The fix keeps the fact on
the turn it is about, saved with the chat, so every way the chat comes back agrees on it. A second note kept beside the
chat would have been two saved things that can disagree. Skill 9 (sessions and state: one source of truth for what must
survive).

## FX163 · An animation held after its end hides the page from the glass

The glass bent nothing behind it, though its filter ran and its numbers were right. The page's column played a
short entrance and then held it, and a held animation makes its element a layer of its own. A backdrop filter sees
only up to the nearest such layer, so a card's lens saw the column's empty inside, never the ribbons behind the
page. Letting the entrance go once it ends gave the lens the page back. The rest of the fix came from reading the
design's own code instead of guessing from pictures: the pointer as a lamp, a press that flattens the glass, a
lens on every pane rather than the hovered one. And a panel's glass became one value, colour, amount and type, so a
site says it in one place and the theme and the lens do the rest. Skill 18 (testing and shipping: what a visitor
sees is checked by seeing it).

---
[Learning](README.md) · [Fixes](../fixes/README.md) · [Docs index](../README.md)
