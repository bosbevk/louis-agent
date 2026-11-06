# Lessons from the fixes

> **Answers:** what each fix taught, in one place, now that the fix docs are folded (CL24, 2026-10-29) · **Written by**
> Claude, for Louis to read

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

---
[Learning](README.md) · [Fixes](../fixes/README.md) · [Docs index](../README.md)

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

**FX57: an error can be somebody else's, and still be yours to name.** The dashboard filled with *A task failed and
nothing awaited it*, in clumps of four, long after the page that caused them. PuppeteerSharp fails a private task whenever a
page or an iframe goes before its script context exists, and the runtime only reports it when the garbage collector gets
there, which is why the errors never lined up with anything. FX36 could await the failing task for redirects; this task is
private to the library, so the only choice was to say what's expected. The filter holds the exception's type and its exact
words, so a timeout from the same library is still an Error. An error log you've learned to ignore is the expensive part:
Louis asked about it, and nothing had been lost. Skill 17 (observability: a log line has to mean something).
