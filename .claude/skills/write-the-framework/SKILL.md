---
name: write-the-framework
description: Works as the framework writer, who holds the docs, the framework's own pages and the code to each other. Use when a doc about the framework is written, rewritten or reviewed, when a framework page (Overview, Getting started, How it works, Features, Cost and evals, Examples, Comparison) changes, or when Louis asks whether a doc is true, explained on the framework's pages, or proved in code. Holds how a doc is read claim by claim, where each claim is checked, the verdicts, and what each verdict sends where.
---

# Write the framework

The framework writer reads a doc one claim at a time and asks three things of each: does the code do it, does a
test hold it, and do the framework's own pages (`louis-agent.web.framework`) explain it, linking back to the doc?
How a doc reads is `/write-the-docs`; how a page's words read is below, under *How a page reads*. Five stops
come first.

1. **The code is the truth.** A claim is checked by reading the code that does it, never from memory, a summary
   or another doc. A doc or a page that disagrees with the code is what changes.
2. **Never change code to match a doc.** Code that looks wrong is a finding for Louis (`/put-a-finding`), or a
   fix through `/fix-a-bug` on his word.
3. **Say what could not be checked.** A claim you could not find in the code, or could not run, is marked so,
   never passed (the docs rules, "True to the code").
4. **A page says only what a test holds.** A new claim on a framework page comes with the test that ties it to
   the repo, written first (`/do-a-step`, ADR 0100).
5. **What goes on the pages is Louis's pick.** A held claim missing from the pages is reported; adding it is a
   finding, not a step taken on your own.

## The steps

1. **Read the doc** top to bottom, and the framework page or pages that link to it (`DocLinks`, a feature's `Doc`).
2. **List its claims.** One row for each sentence that says the framework does, has or needs something: a
   setting, a type, a call, a number, a limit, an order, what a failure does. Leave out the doc's own framing.
3. **Find each in the code.** Open the type or setting it names and read what it does. Write the file and line.
4. **Find the test that holds it.** Search the tests for the type, the setting or the behaviour. Name the test.
5. **Find it on the pages.** Look in the records in `src/web/louis-agent.web.framework/Content/` (the table below).
   Does a page say it, the same way, and link this doc?
6. **Give each row its verdict** (the table below), and write the claims table.
7. **Send each row on**: a wrong doc to `/write-the-docs`, a wrong or missing page line to a step with its test
   first, untested or doubtful code to Louis as a finding.

## The verdicts

| Verdict | It means | What follows |
|---|---|---|
| Held | The code does it, a test holds it, and a page tells it and links the doc (or it has no place on the pages) | Nothing |
| Doc wrong | The code does something else, or no longer exists | Rewrite the sentence with `/write-the-docs`, keeping every other fact |
| Not on the pages | Code and test hold it, but no page tells it | A finding: add a record to the page it belongs on, or leave it to the doc |
| Page wrong | A page's record says it differently from the code | A step: the page's test changed first, then the record |
| Not proved | The code does it, but no test holds it | A finding: a test is code, so it is Louis's step |
| Not found | Nothing in the code does it | Leave it out of the doc, or say it is planned in the plan; never on a page |

## Where the pages' words live

Every framework page says what its records in `Content/` say, and a test holds each to the repo
([louis-agent.web.framework](../../../src/docs/projects/louis-agent.web.framework.md#4-what-a-page-says-is-held-to-the-repo)).

| Page | Its records | Held by |
|---|---|---|
| Overview, Getting started | `ProgramListings`, `FrameworkFamily`, `FrameworkProject`, `GettingStarted` | `OverviewPageTests`, `GettingStartedPageTests` |
| How it works | `HowItWorks` | `HowItWorksPageTests` (every identity and pack on the disk) |
| Features | `Features/FrameworkFeatures` (`FrameworkFeature`: area, name, code, what, doc) | `FeaturesPageTests` (the code is in `src/`, the doc is a file) |
| Cost and evals | `Cost/` (`CostExample`) | `CostAndEvalsPageTests` (the numbers from the price table) |
| Examples | `Examples/` (`FrameworkExample`) | `ExamplesPageTests` (the identity's folder exists) |
| Comparison | `Comparison/` (`ComparisonClaim`) | `ComparisonPageTests` (each of our claims names its proof) |
| Every page | Its doc links | `DocPathsTests`, `NoMarkupTests` |

The tests run with `dotnet test src/tests/web/louis-agent.web.framework.tests`.

## How a page reads

A framework page is an overview for a developer, not a sales page, so `/write-the-copy`'s message-first and
awareness rules don't apply here. Its sentence rules do (short, active, plain, front-loaded), and so do
`/write-the-docs`' voice and its "speak to the reader". Docs sites call this kind of page an *overview*: what the
thing is, how its parts fit, and where to go next (Cloudflare, Elastic, Tom Johnson).

- **A band's title says the idea**, in sentence case, ending with the site's full stop ("A few lines of your own.").
  It makes sense read alone in the menu's *On this page*.
- **A summary is two to four short sentences**: what this part is, then what it gives you. Never a run of nouns,
  and no name of code a reader can't yet place.
- **A term's line is one or two sentences** that start with what the thing does for you, then how.
- **Every page ends by handing on**: its doc links, and on a page that teaches a task, the guides that come next.
- **Say what is true today.** Planned work is named as planned ("packaging for NuGet is planned"), never as built.
- **Show the real thing.** A program, a number or a list comes from the repo and is held there by a test, as the
  pages' tests do now.

Each page's words are checked as a whole: read the page aloud top to bottom, every sentence under 25 words, and
each fact of the old text ticked off in the new, as `/write-the-docs` does for a rewrite.

## The claims table

The report is one table, one row a claim, in the doc's order:

| Claim (the doc's words) | Code | Test | Page | Verdict |
|---|---|---|---|---|

A claim is quoted short. Code is `file:line`. A cell with nothing found says "none", never left blank.

## Checks before it is shown

- Every claim in the doc has a row, and the count of rows is said.
- Every `file:line` was opened in this session, and every test named exists.
- Each "Not found" was searched for by its name and by what it does, not only once.
- Nothing was changed in the code. Doc and page fixes are listed, not yet made, unless Louis asked for them.

## Handing it over

Show Louis the claims table, the count of each verdict, and the findings, each on its own with three options
(`/put-a-finding`). On his picks, the doc fixes go through `/write-the-docs` and the page fixes as steps, one doc
area or one page to a pull request, through `/ship-it`.

## An example

`src/docs/guides/debug-and-logs.md` says: "`LOG_CONTENT=full` shows the values, and is for your own machine only."

| Claim | Code | Test | Page | Verdict |
|---|---|---|---|---|
| `LOG_CONTENT=full` shows the values | `louis-agent.core/Config/AgentOptions.cs:57`, `Logging/ToolCallRecords.cs:12` | `LogContentTests` | none: no feature names it, no page links the guide | Not on the pages |

The code and the test hold it, so the doc is right. The Features page's guards have no line for logs that hide what
the user typed, so this goes to Louis as a finding: add a `FrameworkFeature` under the guards, linked to the guide,
or leave it to the doc.

## References

- Daniele Procida, [Diátaxis: reference](https://diataxis.fr/reference/): reference follows the shape of the
  machinery it describes
- Jared Bhatti and others, *Docs for Developers* (Apress), and [their interview](https://stackoverflow.blog/2022/06/21/an-engineers-field-guide-to-great-technical-writing-ep-455/):
  "good documentation tells the truth and builds trust"
- Cloudflare's [overview content type](https://developers.cloudflare.com/style-guide/documentation-content-strategy/content-types/overview),
  Elastic's [overviews](https://www.elastic.co/docs/contribute-docs/content-types/overviews) and Tom Johnson,
  [API product overviews](https://idratherbewriting.com/learnapidoc/docapis_doc_overview.html): what an overview page holds
- [ADR 0100](../../../src/docs/adr/0100-the-site-tells-the-whole-architecture.md), the site tells the whole architecture
- [louis-agent.web.framework](../../../src/docs/projects/louis-agent.web.framework.md), the pages and their tests
- [The docs rules](../../rules/docs.md#what-every-doc-is), true to the code
