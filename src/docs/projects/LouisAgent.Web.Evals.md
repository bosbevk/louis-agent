# LouisAgent.Web.Evals — design

> **Kind:** Razor library (`net10.0`, runs in the browser) · **Path:** `src/web/LouisAgent.Web.Evals/` ·
> **References:** LouisAgent.Web, LouisAgent.Web.Docs, LouisAgent.Evals.Contracts · **Used by:** LouisAgent.Site, naming `.Evals()` among its areas

![The admin's eval pages: the routes read and every address in one place, the evals' page that starts a run, and the words its code picks](diagrams/LouisAgent.Web.Evals.svg)

## Purpose

This project is where a person starts, follows and reads an eval's runs, in the admin of a site that runs evals.
Its first page lists the host's evals, one card each, says how each one's last run went, and starts the next: a
replay for free, or a live run after an ask that says about what it will cost. A run's page gives that run's number
first and its conversations, the failed ones first, follows a run while it goes and can stop it, re-grades one that has ended and says how it went against
the run before, and a conversation's page reads one repeat turn by turn, where a person can flip the judge's mark
and copy the conversation back to the eval file. It reads and calls what
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)'s routes send, a run's records as
[LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) holds them, and runs in the browser like the rest of the
admin.

A site takes it with one word among its areas, `.Evals()`, and louis-agent.dev does.

The doc follows a page from its data to what it draws: how a site takes the pages, how the routes are read, the
evals' page and its cards, how a card starts a run, a run's page, better or worse, a re-grade, a conversation's
page, a flip, and the words.

## How it works

### 1. A site takes the pages

`EvalsArea` gives a site the eval pages with one word among its areas, `.Evals()`, as the accounts and the docs are
taken. It hands the site's router the library's pages, registers `EvalsApi` and the clock a run's page follows a
run by, `TimeProvider.System`, unless the site registered its own, and gives the admin two parts: an
Evals link in the sidebar, `EvalsLinks`, with the frame's tick for its icon, and a card on the admin's home,
`EvalsCard`, that says how many evals the host has and how many of their last runs passed every conversation. The
card reads the same list the evals' page reads. A site that names no evals ships none of it, and the area needs the
site's admin frame, so a site names `.Admin(layout)` before it.

### 2. The routes are read, and every address has one place

`EvalsApi` is a small client over the site's own API client. It reads the host's evals, one run whole, and an
eval's runs, each with `RunJson`'s options, the ones the store wrote a run with, so it is as strict as every reader
of ours. A run the host doesn't have is none, since a page says that in its own words. It also starts a run, from
the eval's name, the kind and the repeats, and answers a `RunStart`: the new run's id, read from the address the
host answers with (`EvalAddress.FindRunId`), or the host's reason for refusing it, a `400`, `404` or `409` in the
host's own words. And it stops a run, where a run that has already ended, or is gone, is no failure, since the page
reads the run again after and shows how it ended. A refused key or a host that
can't be reached stays the API client's own `AgentApiException`, which every admin page turns into the key box or a
plain line.

`EvalAddress` holds every address the pages use and every route they read, so no page joins one by hand. Each part
is escaped, so a name can't change an address's shape. A repeat is part of a conversation's address, never a `#`, so
a repeat can be linked and the browser's Back works.

![Its classes: the page reads the evals through the client, which reads the routes the addresses name; each eval is a card, and the words come from one place](diagrams/LouisAgent.Web.Evals-classes.svg)

### 3. The evals' page

`EvalsPage`, at `/admin/evals` in the admin's frame, reads the host's evals when it opens and again on Reload, and
nothing polls. Each eval is an `EvalCard`: its name, its agent's address, how many conversations it holds, and its
last run with the number first, "5 of 7 passed", then when it started in the browser's own time, its kind and its
repeats. A last run that could not finish says so, with how many conversations it graded, and one a person stopped
says "Stopped". A line says how the last run went against the run before, "▲ 1 more than the run before", from the
eval's row (`EvalRow.CountChange`). An eval never run says "Not run yet." and has no link; every other card links its last run's page.

A host with no eval shows one card, `NoEvalsCard`, that says where an eval file goes. It links the guide on running
an eval through the site's own docs address when the site names one, and names it in words when it doesn't. A
refused key shows the frame's key box.

The cards are Bootstrap's grid, four across on a wide screen and one on a phone. The library's own styles hold only
what Bootstrap doesn't, and every colour is a theme's name, so glass and flat, light and dark all draw it.

### 4. A card starts a run

Under its last run, each card has two buttons. "Replay free" plays the eval's last finished live run back from its
recordings, with that run's repeats, and costs nothing, so it starts at once. It is there only when the eval has such
a run (`EvalRow.CanReplay`); until then the card says "Nothing to replay yet. A replay plays a live run back."

"Run live…" spends money, so it opens an ask inside the card, `RunAsk`, with no dialog and no page of its own. The
ask names the eval and how many conversations it plays, "against the real model and the real site", lets the person
pick one repeat or three, and says about what that will cost: "About $0.011 for 3 repeats, from what the last live
run cost." The estimate is the eval row's own answer, `EstimateLive`: what the last finished live run spent on the
agent and the judge, a repeat, times the repeats picked. When only one of the two was priced, it is that one. When
neither was, or there is no such run, the ask says the cost isn't known yet and the button says "Run live" with no
figure. The buttons say what they do, "Run live, about $0.011" and "Don't run".

Which card's ask is open is the page's to hold, so one is open at a time: the card asks the page to open or close it,
and the page tells each card whether its ask is the open one. A start takes the person to the new run's page. A
refusal stays on the card, in the host's own words. A card's buttons are off from a click until the host answers,
so a double click starts one run.

One run goes at a time. While one does, a line above the cards names its eval and says the others wait, every other
card's buttons are off, and the going run's own card says since when it goes and links it, "Follow the run →".

![The evals' page: it reads the host's evals and hands each its card, which holds the ask before a live run; the one card that says where a file goes when there is none, and the frame's key box on a refused key](diagrams/LouisAgent.Web.Evals-components-evals.svg)

### 5. A run's page

`RunPage`, at `/admin/evals/{eval}/runs/{run}`, reads one run whole when it opens and again on Reload, with the
eval's line in the host's list and the eval's runs beside it. A run that is going is read again on its own, as the
next part tells. A way back to
the evals sits above its heading, which names the eval and when the run started. The number comes first, "0 of 1
passed", in the monospace face. Beside it, `RunFigures` lays out the run's other figures, each a `RunFigure` with
its name over its value: the kind, the repeats, how long it took, the agent's cost, the judge's cost, and the facts
and criteria met.
A line under them says which agent was asked, and whether its message carried the page a visitor would be on.

Every one of those figures is the run's own answer, asked of its records: the counts are `CountFacts` and
`CountCriteria`, the time is `MeasureTimeSoFar`, and the costs are `SumCost` and `SumJudgeCost` in the currency
`FindCurrency` names. A cost with no price reads "unknown", never 0.

The conversations follow, in `RunConversations`, in the run's order: the failed ones first, then those that passed
only after a person's flip, then the rest. Each is a `ConversationRow`, and the whole row links to the
conversation's own address. It shows a tick or a cross, the conversation's id, its first ask, and
how many of its repeats passed, "2 of 3 repeats". A failed one adds a line in the danger colour saying which repeat
failed first and on what, in that repeat's first miss's own words: "Repeat 2, not met:" and the criterion, or
"fact not met:" and the fact. A row is tagged "passed after your flip" in the theme's blue, and "now passes" or
"now fails" against the run before.

Under the conversations, `RunRows` lists the eval's runs as the API sends them, newest first. Each is a link with
when it started, its kind, its repeats and how many conversations passed, and the open run is marked for the eye
and, with `aria-current`, for a screen reader. That list is where the run before is found.

A page says plainly what it could not show. A run that didn't finish puts a card first, `RunEnding`. One that
could not finish is the harness failing, so its card is in the danger colour, with the run's own reason. One a
person stopped is no failure, so its card is in the page's own colours: "The run was stopped", "A person stopped it.
What was graded before is kept." Either then says how many of the eval's conversations it didn't reach: a run keeps
only the conversations it graded, so the eval's row (`EvalRow.CountNotRunIn`) says how many, not which, and the
score reads "of 2 graded passed". A run the host doesn't have is one line that names the id.

### 6. A run's page follows a going run

A going run's page reads the run again every three seconds, so the person watches the conversations being graded
without pressing anything. The line under the heading says "The run is going. 3 of 7 conversations are graded so
far, and this page reads it again every few seconds." The time figure is named "So far" and counts on the page's
clock, and the costs are what was spent so far. The run's file is kept after each conversation, so that is how often
the page's figures move.

The timer runs on the clock the evals' area registers, so a test moves time by hand instead of waiting. It starts only
for a run that is going, stops once a read answers a run that has ended, and goes with the page when the person
leaves it. Each tick comes off the page's render thread, so its read and its redraw go through the component's
`InvokeAsync`. A read that fails keeps the run last shown, the frame's line says why, as on a first load, and the
timer carries on.

While the run goes, "Stop the run" stands where Reload does. It asks the host to stop the run and reads it again, so
the page shows at once that it was stopped and what it kept. Once the run has ended, Reload is back, and the time
figure is named "Took".

### 7. Better or worse

Every run that finished is compared with the run before it. The page reads that run whole, the one
`RunHistory.FindBefore` names from the eval's rows: the run a re-grade re-graded, else the newest earlier run that
finished. The run answers the comparison itself (`EvalRun.CompareWith`), and `RunChangeLine` says it under the
heading: "▲ 1 more than the run before" in the theme's green (`--web-ok`), "▼ 1 fewer" in its red (`--bs-danger`),
or "the same", then the run before's time, linked, and which conversations now pass and now fail. A run with no
run before says nothing of one.

Under the conversations, `RunTrend` draws the last eight finished runs, a bar each as tall as its share of passed
conversations, this run's filled and every bar a link to its run, then the criteria those runs missed most, with
how often each was missed and how often a person flipped it, and a line that a criterion you keep flipping wants
rewording, or to be a fact. It is plain markup over Bootstrap's flex classes and the theme's names, with no chart
package, and it adds the tallies the rows already carry, so no run is read for it. A flip is said as a count, never
as a rate of agreement.

### 8. A re-grade

A run that has ended has Re-grade beside Reload (`RegradeButton`). It starts at once, with no ask, since a re-grade
asks the judge only for what changed and the run says after what it spent; the browser goes to the new run's page,
and a refusal is a line in the API's words. Blazor keeps the page when only the run in its address changes, so the
page reads the run whenever the address names one it hasn't read.

A re-graded run's page says what it did, in `RegradeChanges`: no agent was asked, these are the answers of the run
it re-graded, linked, graded with the eval file as it is now. Its judge calls and the marks it kept are figures,
then a line each for every criterion added with how it was judged over the repeats (`ConversationRun.CountCriterion`),
every one taken out, the marks kept with the flips that came with them, the facts checked again, and every
conversation left out. When it asked the judge anything, a last line says Replay free plays it again, since the new
answers joined the live run's recording.

### 9. A conversation's page

`ConversationPage` is at the conversation's own address in its run, with the repeat as the address's last part when
one is asked for: `/admin/evals/{eval}/runs/{run}/{conversation}/2`. It reads the run whole, and picks the
conversation and the repeat from the address. Which repeat opens is the conversation's own answer, `FindRepeat`:
the one asked for, or with none asked for its first failed one, the one worth reading first, or its first. Moving to
another repeat changes only the address, so the page reads again only when the run changes, or on Reload.

Under a way back to the evals and to the run, the heading names the conversation and says whether it passed, how
many repeats passed and which one is on show. `RepeatSwitch` is a pill of links, one a repeat with its tick or
cross, the open one filled and marked with `aria-current`; a conversation of one repeat has none. Each turn of the
repeat is a `TurnCard`: what was asked, a fold that says how many tool calls and names the tools, holding each
call's input and result, the answer as the agent sent it, a quiet line with what the turn cost the agent and what
judging it cost, the agent's error when the turn ended in one, and the turn's marks two to a row on a wide screen. A mark is a `MarkLine`: a fact in code type, "checked in code", or a
criterion in words with the judge's line of why. The repeat's own figures close the page: how long its turns took,
their cost, and its facts and criteria met. A run, a conversation or a repeat the host doesn't have is one line
that names it.

Two links beside Reload walk the run in its page's order, failed first: Previous opens the conversation before, and
Next failed the next one that failed, each asked of the run (`FindPrevious`, `FindNextFailed`) and each left out
where there is none. Under the turns, one link opens the same conversation in the run before, by the comparison's
rule (`RunHistory.FindBefore`). A row of the runs list doesn't say which conversations a run
holds, so the link isn't checked ahead; a run that lacks the conversation says so in its one line. On the eval's
first run there is no such link.

Copy as conversation, beside Previous (`CopyAsConversation`), reads the conversation as a block for its eval file and
copies it with the framework's own code listing module, then says "Copied: this conversation as a block for
guide.eval.json, ready to paste and change.", or says plainly that the browser didn't allow it.

### 10. A flip

The judge is a model, and a person can say it got a criterion wrong. Each criterion's mark has Flip; a fact has none,
since code tells it. Flip opens `FlipBox` under the mark, a part of the page and not a dialog, since a flip is put
back with one press: "Flip to met?" (or "to not met?") with what a flip does, a one-line note, and "Flip to met" and
"Don't flip". The mark's line hands the flip up through `TurnCard`, which gives each criterion its place
(`MarkPlace`); the page sends it to the mark's address and reads the run again, so the page shows what the run keeps
(ADR 0042). A refusal is a line in the API's words.

A flipped mark is set off by a rule in the theme's blue (`--bs-info`): what it was flipped to, what the judge said
and why, the note, how often its criterion was flipped in the last runs, and Undo the flip, which puts the judge's
answer back. Its tick is the answer that counts. The head says "Passed after your flip" when that is so, and the
repeat's figures count the flips.

### 11. The words

The words the code picks are `EvalsWords`, one entry for each part of `EvalsPart`, as every page's words are
(ADR 0132): a count says "1 repeat" or "3 repeats", a date is said in the browser's own time, and a length of
time in its two largest parts, "33 s", "6 min 12 s" or "1 h 2 min". `MarkDot` is a mark
of a fact or a criterion, a tick or a cross with its words for a screen reader, so a mark is never told by colour
alone.

*Decided in:* F126.3.2, F126.3.4, F126.4, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md),
[ADR 0042](../adr/0042-the-web-shows-the-live-results.md).

## Structure

```
src/web/LouisAgent.Web.Evals/
├── EvalsArea.cs   # .Evals(), the clock, and EvalsLibrary, the assembly the router is handed
├── Admin/         # EvalsLinks and EvalsCard: the sidebar's link and the home's card
├── Client/        # EvalsApi, RunStart, EvalAddress, MarkPlace, FlipBody: the routes read and called, and every
│                  # address
├── Pages/         # EvalsPage, RunPage, ConversationPage
├── Components/    # each part with its styles, in a folder for the page it draws
│   ├── Evals/     # EvalCard, RunAsk, NoEvalsCard: the evals' page and the ask before a live run
│   ├── Runs/      # RunFigures, RunFigure, RunEnding, RunConversations, ConversationRow, RunRows: a run's page
│   ├── Changes/   # RunChangeLine, RunTrend, RegradeButton, RegradeChanges: better or worse, and a re-grade
│   ├── Conversations/ # RepeatSwitch, TurnCard, CopyAsConversation: a conversation's page
│   └── Marks/     # MarkDot, MarkLine, FlipBox, FlipAsk: a mark, and its flip
└── Words/         # EvalsWords, and Parts/EvalsPart
src/tests/web/LouisAgent.Web.Evals.Tests/   # the client, the addresses, the page and its parts, the words
```

## Configuration

None of its own. It reads with the site's API client and the admin's key, and links the guide through the site's
`DocsAddress` when the site names one.

## Tests

`LouisAgent.Web.Evals.Tests` holds the client over a stub of the API (`EvalsApiTests`), its starts and stops
among them, and the addresses (`EvalAddressTests`). With bUnit it holds the evals' page (`EvalsPageTests`), a card
that starts a run and its ask (`EvalCardStartTests`), a run's page (`RunPageTests`) and its states
(`RunPageStatesTests`), where a going run is followed and stopped on a clock the test moves, a run's ending
(`RunEndingTests`), and a conversation's page and its walk (`ConversationPageTests`, `ConversationWalkTests`). The
pages run over `EvalPagesHost`, a stub of every route they read and call, with a fake clock from
`Microsoft.Extensions.TimeProvider.Testing`, which also answers flips and re-grades and keeps more than one run.
`Pages/Flips` holds a flip on a conversation's page and a run's row (`ConversationFlipTests`, `RunPageFlipTests`),
`Components/Changes` a re-grade and better or worse (`RegradeTests`, `BetterOrWorseTests`), and
`Components/Conversations` the copy (`CopyAsConversationTests`). It holds the runs list (`RunRowsTests`), a mark (`MarkDotTests`), the
words (`EvalsWordsTests`), the area (`EvalsAreaTests`) and the admin's link and card (`EvalsLinksTests`,
`EvalsCardTests`). louis-agent.dev's own tests hold that its admin draws the evals' link and card.

## Limits and plans

A run's page follows a run by the conversations graded so far, so a long conversation shows nothing new until it
ends, and no line says which turn is being asked. It is not a package ([ADR 0130](../adr/0130-the-framework-is-published-as-packages-one-tag-each.md)).

## Related docs

[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md) · [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) ·
[Run an eval](../guides/run-an-eval.md) · [Test projects](tests.md)

---
[Docs index](../README.md) · Related: [Projects](README.md) · [Plans](../plans/README.md)
