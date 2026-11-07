# 20 · Extensibility: a check as a catalogue entry, an eval as a folder

> **Skill:** #20 in the [goal](../GOAL.md)'s map, with #12 (evaluation) for what a check is · **Stages:** Phase 3b,
> F49.1 *The checks catalogue* and F49.2 *Evals as files*, the first two children of F49 *Evals as content*
> (2026-10-30), F49.2b *The generic checks* and F49.3 *Checks in packs* (2026-10-31) · **Written by** Claude, for
> Louis to read (2026-10-30, extended the same day for F49.2 and on 2026-10-31 for F49.2b and F49.3) · **Next:** the
> studio: samples and known-bads from conversations (F49.4), then a check's settings as a form (F49.5)

What this answers: how a check went from "a constructor call in an eval's C#" to "a name a file can use", why the
contract left the harness, and what was left out on purpose (F49.1); then how an eval went from a C# class in its own
project to a folder of files beside its identity, and what that move taught (F49.2, from
[An eval as a folder of files](#an-eval-as-a-folder-of-files-f492) on).

## Before and after

| | Before F49.1 | After F49.1 |
|---|---|---|
| Where the contract lives | Inside the harness (`evals/louis-agent.evals`): `IGrader`, `Grade`, `Trial` | Its own project, `evals/louis-agent.evals.checks`, which references only what a pack references |
| Who can write a check | The harness and an eval set | Also a pack (F49.3), without reaching the harness that runs Puls's evals |
| How an eval gets a check | `new WithinTimeGrader(TimeSpan.FromSeconds(120))` in C# | That, or `within-time` with `{"seconds": 120}` from a file, through the catalogue |
| The words | "Grader" everywhere | `ICheck` in C#; the wire still says `grader`, so stored runs read the same (ADR 0049) |
| The general checks | four, all C# only | six, five of them named from a file: `no-error`, `within-time`, `cost-under`, `contains`, `tool-used`; `routed-to` C# only |

The cook's and routing's evals didn't change: they still build their checks in C#, every check kept its name and
version, and their reference and known-bad transcripts grade the same in the test suite before and after.

## Three patterns, each for one job

**Separated Interface** (Fowler, PoEAA) for where the contract lives. A pack may not reference the harness (ADR 0027),
so a pack couldn't ship a check while `ICheck` lived there. The contract moved to a small project its clients can
reference. It isn't a "references nothing" project: a check's reason is a message enum formatted by core's
`MessageText` (ADR 0009), a trial carries the session stream's events, and a grade carries the protocol's kinds. So it
references exactly those three, which a pack references anyway; `ArchitectureTests` holds the list.

**Factory Method** (Gang of Four) for how a file's settings become a check. `CheckType<TParameters>` does the part every
entry shares: read the JSON into a typed record, strictly (an unknown field, a missing one and a null are refused,
naming the type and the field, so a typo never becomes a default). The entry only says how to build its check from the
record. The record is also the schema: .NET's `JsonSchemaExporter` turns it into the JSON schema the studio will make
its form from, so the form and the reader can't disagree.

**Registry** (Fowler) for how a name finds its entry. Each owner lists its entries in one place
(`GeneralChecks.All` for the harness; a pack's list in F49.3) and the composition gathers the lists, as it gathers
`PACKS` and `IDENTITIES`. Weighed against it: an attribute scanned by reflection (magic a reader can't follow) and an
enum with a switch in the harness (the harness would have to reference every pack).

## What was left out, and why

ADR 0055 (*keep it simple*) did most of the work in this feature:

- **No `regex` check.** The plan had one because "the field uses it" (promptfoo has both). No eval of ours needs a
  pattern, and a pattern in a file is a language to learn, a slow-match risk (ReDoS) and hard to read back.
  `contains` takes a list of texts instead (one is enough, case and accents ignored), which covers "one of these words".
  The question became a rule for the code too: [ADR 0057](../adr/0057-regex-is-the-last-option.md).
- **No `routed-to` entry.** Its expected role is a function that reads a scenario key and the mode's toolsets. A file
  can't hold a function, so it waits for F49.2's eval file to settle how a route is stated.
- **The runner doesn't build the catalogue yet.** Nothing reads one until F49.2's file reader; `GeneralChecksTests`
  proves the catalogue on every test run in the meantime.
- **No `judge` or `human` entry.** They need a rubric as a file, which is F49.2's.

## What was learned

- **A rename stops at the wire.** `IGrader` became `ICheck` in C# (through Rider, one commit with nothing else in it),
  but the JSON keys stayed `graders`/`grader`: a stored run is data someone else may read, and ADR 0049 forbids an
  alias. The protocol tests and the golden files proved the wire didn't move.
- **A pure move is provable.** Steps 2 and 3 moved thirteen files with `git mv` and a SHA-256 check of each, and kept
  their namespaces (a namespace is logical, an assembly physical), so none of the 118 files using them changed.
- **A folder name is a namespace, and it can hide a name.** The plan's `Grading/Common/Text/` made a namespace `Text`,
  and the tests' `TrialBuilder.Text(…)` stopped compiling. The folder became `Answers/`, which also says better what
  the check reads.
- **Who did the work matters less than whether it was done.** `tool-used` counts a specialist's call too: Puls hands
  the meal to its cook, and an eval that asks "was the shop searched?" would otherwise fail because the work was
  delegated. The reason says who called it, so nothing is hidden.
- **One check, one question.** `tool-used` ignores a streamed error; `no-error` reports it. An eval that wants both
  lists both, and each grade says one thing.

## An eval as a folder of files (F49.2)

| | Before F49.2 | After F49.2 |
|---|---|---|
| What an eval is | A C# class (`CookEval`, `RoutingEval`) in `evals/louis-agent.evals.puls` | A folder with an `eval.json`, its manual, dataset, rubric and transcripts, under the identity's `Evals/` |
| How the runner knows it | An `IEvalSet` per identity, chosen by an enum, `EVALS=puls` | Every `eval.json` copied beside the runner; `EVALS=` chooses by the identity the file names, and a typo is refused |
| Where its checks come from | `new …()` in the eval's C# | By type from the catalogue: the general checks, each identity's own through the composition (`EvalChecks`), and Puls's judge from its own project |
| A new eval that uses existing checks | A C# class, a set entry and a registration | A copy of `evals/template/Example/`, edited; zero C# |
| A turn's facts | Each eval's own (`PulsTurnFacts`) | The harness reads them from the engine's events, for every eval |

The stored runs, the trend and the portal didn't move: every check kept its name and version, and the cook's eval
kept its version (10), since moving it changed no scenario, mode or check.

**Three more patterns.** *Registry* again, this time in the composition: `EvalChecks.All` is where every identity's
check list meets, because the composition is the one project that knows every identity, so the runner never names
one. The *Stable Dependencies Principle* (Robert Martin) for where the cook's judge lives: it is built on the
harness's judge, and the identity, which every host loads, must never carry the harness, so the judge got a small
project of its own beside the identity that only the runner references. *Prototype* (Gang of Four) for the template:
a working eval you copy and change, proven to load and conform with no C#, instead of a scaffold with empty members
that throw.

**What F49.2 taught:**

- **Prove a move with the thing it must not change.** The cook moved from C# to files in step 8b, and a free replay
  of the recorded baseline (`--replay`, every model answer from the recording) graded 140 of 140 the same: outcome,
  score, version, reason and the judge's prompt hash. A test suite proves the code; a replay proves the stored meaning.
- **Same name, same meaning, or a new name.** The cook's `cook-no-error` looked like the general `no-error`, but the
  cook's leaves a broken turn Unknown before counting errors and the general one fails on an error first, with other
  reasons. Swapping one for the other would have changed what stored grades mean under the same name, so the cook's
  stayed its own entry.
- **A rubric is content, and its words are its identity.** The judge's prompt left its embedded resource for a file
  in the eval's folder word for word, and the review found its hash was of the bytes: `161e5cebcc15` on a Windows
  checkout (CRLF), another on Linux (CI went red). `RubricHash` now makes the line endings LF before hashing, so one
  rubric is one hash (`438a8deb91f4`) everywhere; the 36 labels the judge was calibrated against still describe it,
  and the runs recorded before carry the old hash as another measure.
- **A dependency rule finds the design for you.** "The identity never references the harness" is what put the judge
  in a project of its own; a check the identity's code could hold (the six fixed rules) went beside it, one that
  needed the harness couldn't. Writing the rule as a test (`ArchitectureTests`) keeps it.
- **Keep a move reviewable.** Step 9b moved 60 files with `git mv` and a byte check, and only their namespaces
  changed, so git showed 60 renames; their missing summaries and long comments were tidied in a step of their own
  (9c), checked by comparing each file's code with comments and whitespace stripped. Mixed in one commit, the moves
  would have read as rewrites.
- **A plan is cut where the work turns out to be.** Nine steps became fourteen: the cook needed its checks named from
  a file before it could become one (7, 8a), the sets had to go after both evals were files (8c), the judge needed a
  home before the rest could move (9a), and the template and the docs were each a review of their own (10a to 10c).

**Left out on purpose:** the cook's shop hosts are no longer a turn's facts (the harness reads the URLs a tool was
given, and a passing cook run calls no shop tool), and a check of an identity's own is still C#: a rule is code, and
F49.3 moves the shops' checks to the shops pack.

## Readers: the smallest thing a domain ships (F49.2b)

F49.2b made the extension point smaller. An identity used to ship whole checks; now a check that compares a value in
the answer with the scenario's is a general compare plus a **reader**, a one-method Strategy the file names beside the
key (`"reader": "rand-total"`). Readers sit in their own registry (`ReaderCatalog`), built like the check catalogue:
the harness lists its plain ones, Puls lists its three (`PulsReaders.All`), the composition gathers them. Puls's
readers are Adapters over the scoring code its old checks used, unchanged, so the knowledge didn't move, only the
seam did. It's the same shape as a pack shipping tools: the engine owns the loop, the domain owns the small piece only
it knows. How we proved no grade moved is in [write-up 12](12-evaluation.md#generic-checks-and-readers-instead-of-pulss-own-f492b).

## A pack ships its checks (F49.3)

The platform's rule is *identity = who, pack = what it can do, eval = how we prove it*. After F49.2b, two checks
still sat in the wrong place: the shop rules (`untooled-shops`, `estimates-at-shop`, and the kept `priced-at-shop`)
were Puls's, though what they prove is the shops pack's tools. F49.3 moved them beside the tools, with a registry of
the pack's own (`ShopsChecks.All`), so a pack is complete on its own and the identity only composes.

**Dependencies one way is the whole job.** A pack may not reference an identity, an identity may not reference a
pack, and neither may reference the harness (every host loads them). So every move asked the same question: *who
else needs this?*
- **Shared by everyone, and plain:** reading a markdown table, a link, a site's host. It moved *down* to the check
  contract (`Markdown/`), where all three can reach it (Fowler's Move Method).
- **What only the identity knows:** how Puls writes a rand price, or what counts as one given as fact. It came in
  *sideways*, as a reader the eval file names (`IPriceReader`, Puls's `rand-prices`): the same seam as F49.2b's.
- **What both sides need, and only seven rows long:** the table of shops' names and sites. It lives twice, and a test
  in the one project that sees both (`ShopTableTests`) fails if the copies ever differ. That's cheaper than a third
  project, and honest about the cost.

**The plan was cut where the work turned out to be.** Its step 2 was "move all the shop helpers", but their only users
stayed in Puls until steps 3 and 4, so each helper moved *with* the check that used it. Step 2 became the one change
of behaviour worth reviewing alone: two regexes rewritten as plain scans, pinned first by 21 characterisation tests
that passed on the regexes before the rewrite.

**The tests split by what they prove.** The pack's tests prove its rules with a fake price reader. Puls's tests
prove `rand-prices` on the same answers. And the cook's `CookGradesTests` proves the two together: its 230 pinned
grades held through every step.

## Sources

Fowler, *Patterns of Enterprise Application Architecture* (Separated Interface, Registry, Plugin) · Gamma et al.,
*Design Patterns* (Factory Method, Prototype) · Robert C. Martin, *Agile Software Development* (the Stable
Dependencies Principle) · Microsoft, *Framework Design Guidelines* (namespaces) and the System.Text.Json schema
exporter · promptfoo, *assertions* (`icontains-any`) · Inspect AI, *scorers* (`includes()`) and *datasets* (`Sample`)
· Anthropic, *Develop tests* (tool-call checks) · [ADR 0056](../adr/0056-a-check-is-a-catalogue-entry.md) ·
[ADR 0058](../adr/0058-an-eval-is-a-folder-of-files.md) · [ADR 0055](../adr/0055-keep-it-simple.md) ·
[ADR 0057](../adr/0057-regex-is-the-last-option.md) · [ADR 0060](../adr/0060-a-pack-ships-its-checks.md) · Fowler,
*Refactoring* (Move Method, Extract Class) · Feathers, *Working Effectively with Legacy Code* (characterisation tests)

## Core ships no agent

The test of an extensible engine is whether its own first product could be anyone's. Ours couldn't: the coding agent lived in
core. Moving it out showed three kinds of code that all looked like "tools":

- **What the model asks for** is a plugin. It needs only a contract, so it can ship alone: a console app ran the git pack from
  a NuGet package with no engine in its dependency graph.
- **What every plugin relies on** is a platform service. The guards around a fetch stay in the engine, and a plugin asks for a
  guarded fetcher. That is safer than a shared helper, because the plugin has no way to skip the check.
- **What needs the engine's insides** stays. Delegation opens sessions and rolls up their usage; making it a plugin would have
  pushed those insides into the contract, which is the coupling the split exists to remove.

The golden files were the safety net. A move this size should change nothing the model reads, and when one did change, the
order of a Puls session's guides, the test caught it. The order looks cosmetic, but it is part of the prefix the prompt cache
keys on, so it became a rule in the code. Concept: *20 Extensibility* and *18 Testing and shipping* in the [goal](../GOAL.md);
pattern: Separated Interface (Fowler, *PoEAA*), Composition Root (Seemann).

## The framework's web (F59.1, 2026-11-06)

The engine has been a framework for a while, but the web wasn't. A new site would have started as a copy of Puls. F59.1 makes the
split official without moving much: the shared library takes the framework's name, the rule is written, and tests hold it. The moves
come in the children after it. Doing the name first means nothing later moves twice.

**A rule you can run beats a rule you can read.** The framework named a site on 24 lines and its project file, all of them comments,
and nobody had noticed, because nothing checked. Now a test reads every file, and it finds the sites on disk, so a new site comes
under the rule with nobody listing it. The second test is a *ratchet*: what's still too long sits on a list, and the list can only
shrink. It's how you adopt a rule the code doesn't meet yet without pretending it does.

**A tool that says it worked may not have.** Rider's namespace tool did the rename, and the build and most tests stayed green while:

- it dropped `@using` lines from `_Imports.razor`. Razor treats a component it can't find as a plain HTML tag, so that's a warning, not
  an error, and only the page tests noticed;
- it rewrote markup such as `Chat` into `Chats.Chat`, in 16 files;
- it removed `using` lines that were still needed, and left old ones behind.

What caught it was reading the diff with a question in mind: *which lines changed that aren't a namespace or a `using`?* In a rename
that should be almost nothing, so anything there is the bug. No golden file moved at the end, which is the evidence that the pages
still draw what they did.

**MCP Apps, which you asked about.** MCP lets an AI app use tools from a server, and a tool normally sends back text or data. MCP Apps
adds screens: a tool's description names a `ui://` address, the chat app fetches the HTML page held there and runs it in a sandboxed
frame, and the page can only act by asking the chat app to call the server's tools. It's the one formal standard in this area, and it's
built for screens inside a chat, so it doesn't fit our admin. The idea we kept is that a screen can do only what the server named.

Concept: *20 Extensibility* and *18 Testing and shipping* in the [goal](../GOAL.md); pattern: Composition Root (Seemann).

## Described screens (F59.5, 2026-11-07)

A pack can now show whoever runs it a screen without writing any web code. It says what the screen is, as plain data: a title, a sentence,
its columns, whether it searches. One generic page in the framework draws that. So a new screen appears on every site, sidebar link
included, with no change to any of them. This is called *metadata-driven UI*, and the Django admin is the famous example.

**The catch is the vocabulary.** A description can only say what its words allow. Give it enough words and you've slowly written a worse
programming language. So there's a rule for growing it: a new kind of column or field enters only when a *second* screen needs it, and until
then that screen uses a real component.

**The way out is a list with an order.** `PackViews` maps a screen's name to a component, and the plain table is what you get if nobody
registered one. The last to register wins, so the order of the lines in `Program.cs` *is* the order of precedence: the framework's
default, then a pack's view, then a demo's. Fowler calls a shared lookup like this a Registry. A component found by name also means the
generic page names no pack, which is the Plugin pattern, and it's why a pack's view can live in a project the framework has never heard of.

**Two projects that can't see each other still share facts.** The recipes pack runs on the server with its database. Its view runs in the
browser, and must never carry a database driver. So neither references the other, and `PackWebBoundaryTests` holds that. But the view has to
know the screen's name and two column headings. Those are written twice, and the only thing stopping them drifting is one test project
that's allowed to see both. When you split two things apart, ask what facts they still share, and give those facts a test.

**A value from outside is still outside.** The table turns a link column into a link, and those values can come from pages we read. A
`javascript:` address in an `href` runs in the admin's tab, which holds the admin key. So only `http` and `https` become links, and the rest
stays text. It's the same habit as treating a tool's result as data: where a value came from decides how far you trust it.

**A wrong proof looks like a right one.** The boundary test first "passed on a throwaway bad reference". It hadn't: the edit adding that
reference never applied, so the test had only passed on the unchanged project. A test seen passing tells you little until you've seen it
fail, and "seen it fail" needs checking too.

**A button that asks a model is a place to slow down (F59.6).** The git and file controls moved out of one site and into their packs' web
projects, and became admin screens. On the old page their Review button started the turn at once. A screen is on a different page from the
chat, so it had to change anyway, and it now opens the chat with the prompt waiting in the box. Nothing costs money or runs a tool until
the person reads it and presses send. The small `ChatHandoff` object holds the words for the moment between the two pages, in memory, because
a long prompt in a web address would end up in the browser's history.

**One seam still has to fit what doesn't look like a table.** The git panels have no rows, so a described screen seems wrong for them. They
became `OwnViewScreen`s, which are a name and a heading and nothing else. That keeps one list of screens rather than a second list of panels,
but it raised a real question: Puls's host loads the git pack too, for its model, so its sidebar would have offered an empty *Changes*. The
rule that came out is that a screen with no columns can only be drawn by its own view, so a site that doesn't carry the view doesn't list it.

**Sharing a frame means saying what it isn't (F59.7).** The code-review app moved onto the same chat frame as Puls, and lost about 300 lines of
page code and all its copies of the frame's styles. But a coding agent has no use for a place, a language menu or buying credits. Those are
hooks now, all on by default, in one record a site can pass: Template Method's other half. A site names only what doesn't suit it.

**Look at it, even when the tests are green.** Every test passed and the first look still found two bugs. The message box sat below the window,
since the frame's empty chat takes the full height and expects the box inside it. And an admin page crashed the whole app: with no API behind
the dev server, the reply was a web page, reading it as JSON threw, and nothing caught that kind of error. The menu on every admin page made it
worse. The fix went in the one place replies are read, so every page gets it. Tests answer the questions you thought to ask.

**A form in the vocabulary is a line we crossed on purpose (F59.8).** Recipes can now be found, fixed and removed on screen, with no script. A list of columns is cheap to
describe, but a form has to say what each field is, whether it can change, and what a refusal means, and that's how a description turns into a programming language. So it
stops at four kinds: text, number, link and rows. The rules stay in the pack: `RecipeForm` says a name is required and where a recipe came from can't change, and the page only
draws what it's told. A value for a read-only field has nowhere to land, because the save starts from the kept recipe rather than from what was sent.

**Destroying something should cost a second thought, in code.** Removing a recipe does two writes, and their order matters. The page goes on the excluded list first, so if the
second write fails the recipe is still there. The other way round, a failure would leave it gone from the library and free to be kept again on the next read. Being able to say
which half-finished state you'd rather be in is most of what makes a two-step write safe when you have no transaction.

Concept: *20 Extensibility* and *15 Safety* in the [goal](../GOAL.md); patterns: Plugin and Registry (Fowler), Decorator (Gang of Four).

## The admin's frame (F59.2, 2026-11-06)

The admin's frame moved to the framework, and Puls's layout shrank to what's its own. Two patterns carry it, and both solve the same
problem from different sides: the framework needs a site's things, but can't name them.

- **Template Method, for the frame.** `AdminShell` keeps the sidebar and the column, and a site fills the parts that differ as slots. The
  other option was a settings object listing the logo and links, which gives a site nothing to write, but checks its types only when the
  page runs. Slots cost a demo fifteen lines and the compiler checks them.
- **Bridge, for the layout.** A Blazor page's layout is an attribute, fixed when it's compiled, so a page in the framework can't name a
  layout in a site. And a page naming none gets the router's default, which on Puls is the chat's. So a framework page names `AdminFrame`,
  whose only job is to draw inside the layout the site chose. The first test runs through the real `RouteView`, because the trap is in
  the router: a test of `AdminFrame` alone would pass and the page would still sit inside the chat's layout.

**Pin before you move.** The golden file for Puls's layout was written from the old layout, before anyone touched it, and the new one had
to match. That's a stronger claim than "it looks right", and it's cheap. It proves the markup is the same; it doesn't cover states the
test didn't draw, such as the live strip showing a run, so those lean on their own tests.

Concept: *20 Extensibility* in the [goal](../GOAL.md); patterns: Template Method and Bridge (Gang of Four).

## The chat's frame (F59.3, 2026-11-06)

Puls's chat page was the whole chat, so a new site would have started by copying about 300 lines. Now the frame is `ChatShell` in the
framework, and Puls's page is 28 lines of markup that fill its slots. The same two golden files pinned that page all the way through. They
moved once, when an `<img>` built from values started closing with `>` instead of `/>`, and that was the only difference; the last step,
the frame itself, moved neither.

**Move the parts first, and the frame last.** Each small move was easy to check: the sidebar, the header, the message box, the work panel,
each taking its words and brand from outside. By the last step the frame was only wiring, and the step that sounds biggest was one of the
quieter ones. Doing it the other way round means one huge change that nobody can review.

**Pull the state out before you move the page.** Renaming a chat, this browser's settings, choosing a language and a chat's costs each became
a small plain class. That's a *Presentation Model* (Fowler): the rules live in a class you can test without drawing anything, and the
screen only shows them. Without that step `ChatShell` would have been over the size limit, with its rules only testable through a page.

**A move can change when your code runs.** Puls's page used to read the chat from the address in `OnParametersSet`, which Blazor called
only when the address changed. Inside `ChatShell` the same code runs far more often, because a parent that redraws sets its child's parameters
again whenever one of them is a slot. Puls's page redraws on every address change, so a redraw that still carried the old empty address made
the frame look that address up and drop the open chat. Nothing about the code looks wrong; only its caller changed. So the frame now remembers
which chat it opened. Taking that line out fails `ARedrawFromTheSite_KeepsTheOpenChat` and Puls's page test with the turn, which is how we know
the line does its job: a test written alongside a fix hasn't proved anything until you've seen it fail without the fix.

**What the tests can't see.** bUnit answers every script call with nothing, so a test can't tell whether the sidebar's edge really drags. The
look in a browser covered that: the views, a template's draft, renaming, a favourite, search, the toast, the old address and the phone's
width. Sending a message needs the API key and costs money, so the turn itself leans on the recorded turn in the golden file.

Concept: *20 Extensibility* in the [goal](../GOAL.md); patterns: Template Method (Gang of Four), Presentation Model (Fowler).

## Styles anyone can reuse (F59.4, 2026-11-07)

Moving stylesheets into a framework sounds like moving files. It wasn't, for one reason you spotted when you asked "can't we use color vairables?": 89 rules had Puls's orange written straight in, so any new site would have come out orange. A framework's rules can't hold one site's values, so the first job was getting the values out.

**Use names someone else already defined.** The colour names became Bootstrap's own, so a site's author learns no list of ours, and only what Bootstrap has no word for gets a `--web-` name. It had a cost: Bootstrap's own parts read those names too, so text in its dropdowns took Puls's colour. Look for that kind of side effect whenever two things share a name.

**Sass doesn't replace CSS variables.** A Sass variable is gone once the build has run, and a theme that swaps while the page runs needs a variable the browser can read. So Sass writes the variables. The real win is one mixin writing both forms of a colour from one value, so `--bs-primary` and `--bs-primary-rgb` can't disagree.

**Prove a move by comparing what the browser gets.** Each file was split by whose classes its rules use, and the checks compared the whole compiled CSS, rule by rule: 1,591 before and after. Then the browser compared every element on every page with the old stylesheet swapped in. One run gave nonsense because the old sheet hadn't loaded, which is worth remembering: check that your comparison compared something.

Concept: *20 Extensibility* in the [goal](../GOAL.md); patterns: Template Method (Gang of Four), Mixin.

---
[Docs index](../README.md) · Related: [Learning](README.md) · [louis-agent.evals.checks](../projects/louis-agent.evals.checks.md)
