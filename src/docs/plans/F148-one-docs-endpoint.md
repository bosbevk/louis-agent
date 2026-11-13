# F148: One docs endpoint, and every reader goes through it

**Status:** draft, written 2026-11-13 (thread "Plan a docs API endpoint", Louis: "can we plan a docs api end point
all docs must be served from that endpoint. this is next"). **Branch:** `feature/F148-one-docs-endpoint`.
Louis picked the big one on 2026-11-13: **the words stay markdown, the facts are generated** (*Picked*). Five
smaller choices are open and go to him one at a time, three options each (*Open choices*); the steps, the four
diagrams and the namespace map are settled after his picks, before step 1.

## Context

**What happened.** On louis-agent.dev a visitor asked the guide for the README, then "is this the github readme".
The guide answered: "I can't say for certain that it's the one on GitHub. It's the README.md at the root of the docs
I can read."

**Why it couldn't say.** It *was* the GitHub README: the site's image copies `README.md`, `ROADMAP.md`, `TODO.md`,
`src/docs/`, both demos' docs and `packs/docs/` to `/app/docs` when it is built (`src/site/docker/Dockerfile`), and
the docs pack reads that folder (`DOCS_ROOT`). But nothing tells the guide where the folder came from or which commit
it is, so the honest answer was "I don't know". The copy is also only as fresh as the last deploy.

**What exists.**

- `louis-agent.pack.docs` (F109.3): `ListDocs`, `ReadDoc`, `SearchDocs`, straight on the file system under
  `DOCS_ROOT`. A doc is a bare path and its text: no address, no version.
- No HTTP route serves a doc. The API serves what packs publish as *feeds* (`IDataFeed`, `/v1/feeds/{name}`, open,
  ADR 0070) and the MCP host serves those as `feed://` resources (F56, ADR 0072).
- F125.1 (approved, not built) replaces the three tools with `docs_search` and `docs_fetch` and adds `doc://`
  resources; F125.2 gives the docs a public MCP address. Neither says where the docs come from.

**What F148 adds.** One place that holds the docs (a *library*), one HTTP endpoint that serves all of them, and
every other reader (the guide's tools, the MCP resources, later the website's own pages) reading the same library.
Every doc it hands out carries its **path, its commit and its address on GitHub**, so "is this the GitHub README"
has an answer: "yes, `README.md` at commit `527ab4e`, here is the link".

## Design

**The shape, in plain words** (written for the recommended options; it changes with Louis's picks).

- **Every doc says what it is, in a small header.** A few lines at the top of each markdown doc, as our prompts
  already have (ADR 0063): its type (how-to, reference, explanation, plan, ADR), its title and what it is about (a
  project, a pack, a feature). The words below stay markdown, written by hand. The library reads the headers, so
  the endpoint's list of docs is built on the fly and is never a table someone forgot.

- **A docs library.** One class that knows every doc: its path, title, text, the commit it was read at and its
  GitHub address. The docs pack owns it; today's three tools read through it instead of the file system.
- **The endpoint.** `GET /v1/docs` lists every doc; `GET /v1/docs/{path}` returns one (its text plus where it is
  from); `GET /v1/docs/search?q=` finds lines. Open, like the feeds: the docs are public on GitHub already.
  Under `/v1`, nouns, typed answers (ADR 0039, 0045).
- **The docs are the image's copy.** Every merge to `main` deploys the site, so the copy is GitHub's within
  minutes; what was missing was the label.
- **The version stamp.** The image's build writes the commit beside the docs it copies; the library reads it once.
  On the PC, where the docs are the working folder, the stamp says "local, not committed".
- **The guide says where a doc is from.** Each tool result ends with the doc's source line, and the docs toolset's
  guide tells the model to name it when asked.
- **MCP.** Each doc is also a `doc://{path}` resource from the same library, the way F56 serves feeds. This is the
  resource half of F125.1, moved here; F125.1 keeps the search (`docs_search`, BM25) and builds on the library.

**The patterns:** *Single source of truth*; *Repository* (Fowler, PoEAA: one object that stands for the collection
of docs, whatever holds them); *Plugin* (Fowler: the contract names the shape, the pack plugs it in, so the API never
references the pack, ADR 0027), as `IDataFeed` already does.

### Picked (Louis, 2026-11-13)

**How the docs are kept and served: C, words in markdown, facts generated.** He asked "why can't an AI serve the
docs via a tool from github?" and "what if we generate all our docs from Json ... the api can build them on the fly
via json?", and picked from three:

- **A. GitHub live, through a tool** (*Gateway* with *Cache-Aside*; our own call to GitHub's API, or
  [GitMCP](https://mcpservers.org/servers/idosal/git-mcp)). Always `main`; but 60 calls an hour without a token and
  5,000 with one ([GitHub's limits](https://docs.github.com/en/rest/using-the-rest-api/rate-limits-for-the-rest-api)),
  a cache, slow and limited search, and no docs when GitHub is down. Not picked: a merge already deploys the docs.
- **B. Every doc from JSON.** One model for people and machines; but 388 docs and 430,000 words moved, prose
  written and reviewed inside JSON, and JSON someone forgets is as stale as markdown someone forgets. Not picked.
- **C. Words in markdown with a header, facts generated from what is already true** (the usual advice: generate the
  reference, hand-write the guides and the explanations,
  [Fern's comparison](https://buildwithfern.com/post/generated-vs-manual-documentation-which-approach), Diátaxis).
  *Picked.* It is two features: **F148** (this plan: the header, the library, the endpoint, the stamp) and **F149**
  (the lists a generator can own, the project tables, the settings table and the indexes, written from the code by
  the docs' tool before each PR and checked in CI, as the diagrams are).

**Where a fix's lesson lives: A, one lesson, one file** (card of 2026-11-13, after "we currently have one learning
doc for fixes, should that not be another layer?"). Today `learning/fixes-lessons.md` holds 45 lessons in 413 lines
and 98 more are owed, about 1,300 lines in one doc. A lesson is not a new layer: it is as deep as an ADR (one small
thing, layer 5). It is a new *kind*, filed as the ADRs are: `learning/fixes/fx153-a-page-is-behind-the-key.md`,
kind `lesson`, its template (*Symptom*, *Cause*, *Lesson*) and its skill in the header, so each skill's write-up
lists its lessons by itself (F149) and the guide fetches one lesson, not 1,300 lines. Not picked: the lessons inside
each skill's write-up (long write-ups, six skills have none yet), and one doc as now (a lesson can't be linked,
listed by skill or fetched alone). Built in F150.

**The same shape in other places** (Louis: "can you see if there are other places also?"; counted on `main`).
A doc that gains a section for every feature or fix is a folder of small docs written as one file. The same rule
(*one item, one file, and the list is generated*) is proposed for these in F150; Louis's pick was asked for the
lessons, so these four wait for his word:

| Where | Today | As one item, one file |
|---|---|---|
| The skills' write-ups (`learning/`) | A section per feature in the skill's one doc: `20-extensibility.md` is 638 lines with 20 features in it, `18-testing-and-shipping.md` 396 with 11 | A write-up per feature, its skill in the header; the skill's page keeps the summary and lists its write-ups |
| The fixes table (`fixes/README.md`) | 143 rows, a paragraph of symptom and cause in each cell (630 characters a row, up to 1,557) | The symptom and cause move into the fix's lesson file; the table is generated from the headers (F149) |
| "What the build decided" in the project docs | 15 dated sections in 10 docs (six in `louis-agent.diagrams.md`), history inside a design doc | What still holds goes under *Design notes* or into an ADR; the date and the id are the work log's |
| `AGENT_COMMUNICATION.md` | 414 lines: the HTTP API, the event stream, ACP and MCP in one doc | One page per protocol on layer 3 |

**Alike, but right as they are:** the work log and the TODO (a log and a list are one table by nature), and the
indexes (`adr/README.md` 119 rows, `projects/README.md` 63, `SETUP.md`'s 73 settings), which stay one page each
and are generated by F149.

### Open choices (asked one at a time)

1. **Which standards the docs follow.**
   - **A. The known standard for each question, kept light:** Diátaxis for the kinds, C4 for the layers, MADR for
     ADRs, arc42's sections under plain names for a project doc (ten, not twelve), Good Docs-style templates,
     lowercase-hyphen file names, a front-matter header. Nothing invented, nothing heavier than what we write
     today. *Recommended.*
   - **B. arc42 in full for every project doc:** all twelve sections, its names. The most recognisable to an
     architect; but arc42 describes one system, and 43 project docs would each carry five sections with nothing to
     say.
   - **C. Our own pattern, written down:** today's most common sections become the rule, no outside names. The
     least change; a new developer learns our words instead of ones they may know.
2. **What the header holds.**
   - **A. Three fields: `type`, `title`, `about`.** Enough for the list and for finding a project's docs.
     *Recommended.*
   - **B. Those plus `adrs`, `feature` and `updated`.** A richer index; more to keep right by hand on 388 docs.
   - **C. No header: read the type from the doc's first line and the title from its heading**, as the docs are
     written today. Nothing to add; but it parses prose, and "about" has nowhere to live.
3. **How strictly a kind's pattern holds** (F150's rule, asked here because step 2's test starts it).
   - **A. The same sections in the same order, one may be left out, none added;** a project doc's extras go in
     *Design notes*. Every doc of a kind reads alike, and a generator knows where things are. *Recommended.*
   - **B. A required core, and any extra section anywhere.** Easier to reach; the docs drift apart again.
   - **C. A template to copy, checked in review only.** No test to write; nothing holds it, which is how the
     project docs came to differ.
4. **How the guide's own tools reach the docs.** "All docs served from that endpoint", read three ways:
   - **A. One library, many doors** (*Repository*): the endpoint, the tools and the MCP resources all read the same
     library in process. One source, no network hop inside one program. *Recommended.*
   - **B. The tools call `/v1/docs` over HTTP** (*Remote Proxy*): literally everything through the endpoint; the
     guide could then read another instance's docs. A hop, a timeout and a failure mode for a call to itself;
     F125.8's `TOOL_TRANSPORT` gives this later for every tool at once.
   - **C. The tools read `doc://` over MCP.** Waits for F125.8's client.
5. **The endpoint's shape.**
   - **A. A `docs` feed**: plug an `IDataFeed` into the pack; `/v1/feeds/docs` and `feed://docs` come for free.
     Smallest. But a feed is one JSON blob with flat ids: no path with slashes without bending the contract, no
     search, no plain markdown.
   - **B. Its own routes, `/v1/docs`, behind a small contract** (`IDocLibrary` beside `IDataFeed`): list, one doc by
     path, search; JSON by default, the raw markdown on `Accept: text/markdown`; an `ETag` from the commit. A new
     contract and a route file. *Recommended.*
   - **C. Its own docs host** (a separate small program at its own address, the one F125.2 would make public). The
     cleanest outside door; a second container to deploy and pay for now, before anything needs it.

### The standards the docs follow

Louis, 2026-11-13: "we must rename then if needed, is there not a standard to follow?" There is no one standard for
a repo's docs, but there is a known one for each question, and we already follow two of them:

| Question | The standard | Where we stand |
|---|---|---|
| What kinds of doc are there? | [Diátaxis](https://diataxis.fr/): tutorial, how-to, reference, explanation | Followed since ADR 0013; six opening-line styles to make one |
| How is a decision written? | [Nygard's ADR, as MADR](https://adr.github.io/madr/): context, decision, consequences | Followed: all 118 ADRs |
| In which layers is a system described? | [The C4 model](https://c4model.com/): context, containers, components, code | Not named; our layers below are C4's, one to one |
| What does a design doc hold? | [arc42](https://arc42.org/overview): twelve fixed sections, from goals to risks | Not followed; our project docs hold seven of its twelve under other names |
| What does a template per kind look like? | [The Good Docs Project](https://www.thegooddocsproject.dev/template): a template per kind of doc | No templates yet |
| How is a file named? | [Google's developer style guide](https://developers.google.com/style/filenames): lowercase words joined by hyphens | 112 ADRs, the guides and the write-ups follow; 17 files are in capitals |
| How does a doc say what it is? | YAML front matter, the header every docs tool reads (Jekyll, Hugo, Docusaurus) | Our prompts have it (ADR 0063); no doc does |

**C4 onto our layers:** layer 1 (the front door) is C4's *context*: what the system is and who uses it. Layer 3
(the whole) is *containers*: the hosts, the engine, the packs and how they talk. Layer 4 (one project) is
*components*. C4's *code* level is the summaries in the code itself, which the docs never copy.

**arc42 onto a project doc** (the sections a project doc keeps, under arc42's names where ours differ only in
wording): *Purpose* (arc42 1, goals), *How it works* (6, runtime view), *Diagrams*, *Structure* (5, building blocks),
*Configuration* (7, deployment), *Extending it*, *Tests* (10, quality), *Design notes* (9, decisions, linking the
ADRs), *Limits and plans* (11, risks and debt), *Related docs*. The five we leave out (constraints, context,
solution strategy, crosscutting concepts, glossary) are said once for the whole system on layer 3, not 43 times.

**The names that change** under the file-name standard (if Louis picks it): capitals stay only where GitHub or a
tool looks for them (`README.md`, `CLAUDE.md`, `TODO.md`, `ROADMAP.md`, `THIRD-PARTY-NOTICES.md`). `GOAL.md` becomes
`goal.md` and the three `SETUP.md` become `setup.md` (68 files link to them, fixed in the same commit by a script
check, as the docs review's link check did); `SITE_OWNERS.md` keeps its name, since site owners are sent its
address; the rest are renamed by their move in the table below.

### The docs in layers

Louis, 2026-11-13: "all our docs must be layered, also give me the hierarchy of the doc structure", "each with their
template". The docs already sit in folders by kind; the layers say the order to read them in, and which way the
links go: **down** to the next layer for more detail, **up** to the index on every doc's last line.

![Five layers, top down: the front door, the index, the whole, one project and its how-tos, then the why, the lessons and the work in progress](diagrams/F148-one-docs-endpoint-the-layers.svg)

```
README.md  ROADMAP.md  TODO.md                 1  the front door: what, where to, what's next
src/docs/
  README.md                                    2  the index: every doc by kind
  GOAL.md  SETUP.md  MODELS.md  work-log.md       the standing pages
  CLAUDE.md  AGENT_COMMUNICATION.md
  architecture/        14                      3  the whole: how the parts fit        (explanation)
  projects/            26                      4  one project: its design doc         (project)
  guides/              24                      4  one job: its how-to                 (how-to)
  adr/                112                      5  why: one decision each              (ADR)
  learning/            15                      5  what it taught: a write-up a skill  (write-up)
  plans/  features/  fixes/  specs/            5  being built: gone when done         (plan, feature, fix)
  templates/                                      new: one template per kind (F150)
packs/docs/            projects/ 9, adr/ 3        the same layers 4 and 5, for the packs
demos/louis-agent/docs/  projects/ 4, guides/ 2, adr/ 3     and for each demo
demos/puls/docs/         projects/ 4, guides/ 2, adr/ 1
```

**What moves** (Louis: "we will move them if needed, and you must show it"). Ten docs sit outside their layer
today. F150 moves them, or removes them, each move its own small commit with its links fixed; nothing else changes place.

| Today | After | Why |
|---|---|---|
| `src/docs/MODELS.md` (a how-to) | `src/docs/guides/choose-a-model.md` | A how-to belongs with the how-tos, layer 4 |
| `src/docs/AGENT_COMMUNICATION.md` (how the hosts talk) | `src/docs/architecture/agent-communication.md` | It explains the whole, layer 3 |
| `demos/louis-agent/docs/QUICK_START_AGENT.md` (a tutorial) | `demos/louis-agent/docs/guides/quick-start.md` | The demo's guides, layer 4 |
| `demos/louis-agent/docs/demos.md` (an explanation) | `demos/louis-agent/docs/projects/demos.md` | What the demo's parts are, layer 4 |
| `src/docs/specs/RESPONSE_OPTIMISATION.md`, `USAGE_AND_BUDGETS.md` (in progress: F1 and F2 built) | `src/docs/architecture/`, rewritten to hold only what is built | A spec whose work is partly built is finished as a proper explanation, layer 3; what was never built is dropped from it |
| `demos/louis-agent/docs/specs/ORCHESTRATOR_DESIGN.md` (partly proven by the POC) | `demos/louis-agent/docs/projects/`, only what is built | The same, as the demo's design doc, layer 4 |
| `src/docs/specs/SESSION_ARCHITECTURE.md`, `PDF_SEARCH.md` (ideas, nothing built) | removed | An idea nobody is building is not a doc; its TODO row keeps the one line |
| `demos/puls/docs/specs/PULS_COOK.md` (ideas after F48, nothing built) | removed | The same; F76 and F88 keep their rows |

The specs (Louis, 2026-11-13: "specs which is not built can be removed, in-progress ones must be fnished as proper
docs with only what is built"): so the `specs/` folders go, and "spec" is not a kind. A removed spec stays in git at
the tag `pre-f150`, where the four TODO rows that link one (F76, F88, the session architecture's and the PDF
search's) then point, as the rows of CL24's folded docs point at `pre-cl24`.

**What stays:** the plans (Louis: "plans stay"): every plan and draft in a `plans/` folder keeps its place, built
or not, and is deleted only when its work is done, as now; `SETUP.md` and `SITE_OWNERS.md` (standing pages; the second is an address Puls's browser sends to
site owners), and a `README.md` beside a `config/`, `docker/` or `deploy/` folder, which lives with what it is
about (ADR 0074) and only gains its header.

What the layers add to today's folders:

- **A doc knows its layer.** It follows from the `type` in its header, so the endpoint can answer "give me the
  layer above this" and the guide can start wide and go deeper instead of grepping (*Progressive disclosure*, the
  way the skills load, ADR 0114).
- **Links go one layer at a time.** A project doc links down to its ADRs and how-tos and up to the architecture
  page that shows it; an ADR never has to explain the project again.
- **Each kind has a template**, a file in `src/docs/templates/` (`project.md`, `adr.md`, `how-to.md`, `write-up.md`,
  `plan.md`, `fix.md`, `explanation.md`): the header filled in, the sections in order, one line under each saying
  what goes there. A new doc starts as a copy, and the pattern test reads the same files, so the template and the
  rule can't differ.

### The doc design: one pattern per kind of doc

Louis, 2026-11-13: "Project docs should all follow same pattern", "all docs". A doc's *kind* is the `type` in its
header, and each kind has one pattern: the same sections, in the same order, under the same names. What the docs do
today (counted on `main`, 317 docs without the prompts):

| Kind | Docs | Today | The pattern (proposed: what most of them already do) |
|---|---|---|---|
| Every doc | 317 | Six different opening lines (`How-to.`, `Kind:`, `Explanation.`, `Status:`, `Skill:`, ...) | The header; one `#` title; one opening line that says what it answers; the index line last |
| Project design doc | 43 | 36 hold *Purpose* and *How it works*, 30 *Structure*, 24 *Configuration*, 19 *Extending it*; two opening styles; 20 one-off sections | *Purpose*, *How it works*, *Diagrams*, *Structure*, *Configuration*, *Extending it*, *Tests*, *Design notes*, *Limits and plans*, *Related docs* |
| ADR | 118 | All 118 hold *Context*, *Decision*, *Consequences*; 112 *Links* | *Context*, *Decision*, *Consequences*, *Links* |
| How-to guide | 27 | 15 end with *Before you ask for a review*, 10 hold *Test it*; the rest differ | The steps, each named for what you do; *Test it*; *Before you ask for a review* |
| Write-up | 14 | 12 hold *Before and after*; 4 or fewer hold each other section | *Before and after*, *What was learned*, *What didn't work*, *Trade-offs accepted*, *What I can now explain*, *What's left* |
| Plan | 20 | Ruled by the plans' README | *Context*, *Design*, *Steps*, *Verification*, *Concepts*, *Approved during the build* |
| Fix doc | open ones | Ruled by the fixes' README | *Symptom*, *Cause*, *Fix*, *Tests*, *Confirmed*, *Lesson* |
| Explanation (architecture) | 13 | Free | The opening line and the index line; the sections are the doc's own |

A section a doc has nothing for is left out, never left empty; a project doc's one free place is *Design notes*,
which takes today's one-off sections ("What the build decided"). The pattern is held the way the repo holds a rule
it grows into: a test that reads each doc's `type` and checks its sections, with the docs that don't fit yet in a
list that can only shrink (as `WriteUpsTests` does).

**Where it is built.** The header and the test for "every doc" are F148's step 2. Bringing each kind to its pattern
is **F150**, its own feature between F148 and F149, the 43 project docs first: F149 can only generate a project
doc's *Structure*, *Configuration* and *Tests* once every project doc has them in the same place.

### Interface design

The pictures show the recommended options and are redrawn if a pick changes them.

![One library, three doors: the tools, the API's routes and the MCP resources read IDocLibrary, and only the docs pack knows the files](diagrams/F148-one-docs-endpoint-interface-design.svg)

```csharp
public interface IDocLibrary                       // louis-agent.composition, step 4
{
    DocSource Source { get; }
    IReadOnlyList<DocInfo> List();
    DocPage? Read(string path);
    IReadOnlyList<DocHit> Search(string query, int most);
}

public sealed record DocInfo(string Path, DocType Type, string Title, string? About, string? GitHubUrl);
public sealed record DocSource(string? Commit, string? Repository);   // both null on the PC: "local"
public sealed record DocPage(DocInfo Info, string Markdown);
public sealed record DocHit(string Path, int Line, string Text);
```

### Architecture

![A merge is the only way in: the image carries the docs and their commit, and every reader gets them from the one library](diagrams/F148-one-docs-endpoint-architecture.svg)

### Communication: "is this the GitHub README?"

![The tool's result ends with the commit and the link, so the guide says yes and shows where; another program gets the same doc from /v1/docs](diagrams/F148-one-docs-endpoint-communication-is-this-the-github-readme.svg)

### Objects in memory: one running site

![One DocLibrary per process: the pack makes it, the catalogue hands it to the API, and the tools hold the same one](diagrams/F148-one-docs-endpoint-objects-one-running-site.svg)

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `DocLibrary` | `packs/tools/louis-agent.pack.docs/Library/` | `LouisAgent.Packs.Docs.Library` | 1 |
| `DocHeaderReader` | `packs/tools/louis-agent.pack.docs/Library/` | `LouisAgent.Packs.Docs.Library` | 2 |
| `DocType`, `DocInfo` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 2 |
| `DocSource` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 3 |
| `IDocLibrary`, `DocPage` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 4 |
| `IAgentPack.Docs`, `AgentCatalog.Docs` (new members) | where they are | unchanged | 4 |
| `DocsEndpoints`, `DocsHandlers`, `DocsReply` | `src/hosts/louis-agent.api/Endpoints/Docs/` | `LouisAgent.Api.Endpoints.Docs` | 4 |
| `DocHit` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 5 |
| `DocResources` | `src/hosts/louis-agent.mcp/Resources/` | `LouisAgent.Mcp.Resources` | 7 |
| `DocLibraryTests`, `DocHeaderTests` | `packs/tests/louis-agent.pack.docs.tests/Library/` | `LouisAgent.Packs.Docs.Tests.Library` | 1, 2 |
| `EveryDocHasAHeaderTests` | `src/tests/engine/louis-agent.core.tests/Docs/` | `LouisAgent.Core.Tests.Docs` | 2 |
| `DocsEndpointsTests` | the API's tests, `Endpoints/Docs/` | mirrors the API's | 4, 5 |
| `DocResourcesTests` | the MCP host's tests, `Resources/` | mirrors the host's | 7 |

### Folder tree (after step 7)

```
packs/tools/louis-agent.pack.docs/
  DocsPack.cs  DocsOptions.cs  DocsToolset.cs
  Library/                                   # new
    DocLibrary.cs                            # step 1
    DocHeaderReader.cs                       # step 2
  Tools/DocsTools.cs                         # reads the library (1), names its source (6)
src/engine/louis-agent.composition/Packs/
  Docs/                                      # new, the contract
    IDocLibrary.cs  DocInfo.cs  DocType.cs  DocSource.cs  DocPage.cs  DocHit.cs
src/hosts/louis-agent.api/Endpoints/
  Docs/                                      # new, steps 4 and 5
    DocsEndpoints.cs  DocsHandlers.cs  DocsReply.cs
src/hosts/louis-agent.mcp/Resources/
  DocResources.cs                            # step 7, beside FeedResources.cs
src/site/docker/Dockerfile                   # step 3: writes the stamp beside the docs
```

### Steps (proposed, for the recommended options; settled after the picks)

1. **The library.** `DocLibrary` in the docs pack: every doc with path and title; the three tools read through it.
   *Test:* the tools' answers are unchanged (golden files hold).
2. **The header.** The reader for it (the prompts' reader, reused), the header written on every doc, a test that no
   doc is without one. *Test:* a doc with no header fails by name; the library gives each doc its type and about.
3. **The stamp.** `DocSource` (commit, repository address); the Dockerfile writes it; a doc's GitHub address is built
   from it. *Test:* a stamped root gives `…/blob/{commit}/README.md`; no stamp gives "local".
4. **The contract and the endpoint.** `IDocLibrary` in composition, the pack plugs it in, `/v1/docs` and
   `/v1/docs/{path}` open. *Test:* the list holds every doc; one doc carries its source; a path outside the root is
   404; an instance without the pack answers an empty list.
5. **Search on the endpoint.** `/v1/docs/search?q=`. *Test:* the same hits as `SearchDocs`.
6. **The guide names its source.** The source line on each tool result, the toolset guide's rule. *Test:* the result
   ends with the line; golden diff reviewed.
7. **`doc://` resources.** `DocResources` in the MCP host, beside `FeedResources`. *Test:* a resource per doc, read
   through both transports' one registration (`McpPrimitives`).
8. **Docs and done.** ADR 0123 (one library, every door reads it), the pack's and the API's design docs, the
   write-up, F125's plan amended (its `doc://` half is here), the TODO row gone, this plan deleted.

## Verification

- The docs pack's, the API's and the MCP host's tests per step; the golden files at steps 1 and 6; the full suite
  once after step 7.
- Free manual check: locally, `curl /v1/docs/README.md`, then ask the guide "is this the github readme" and get the
  commit and the link.
- **Paid:** the guide's eval before and after step 6 (a few cents), only on Louis's go.

## Concepts

- **Grounding and provenance** (GOAL.md skill 7, Context engineering): what goes into the context carries where it
  is from, so the model can cite it instead of guessing. The first stone of retrieval (skill 8).
- **Tool design** (skill 4): a tool result that answers the follow-up question ("from where?") before it is asked.
- **Docs as data where it pays** (skill 18, Testing and shipping): a header a machine reads and a test holds, so
  the index can't drift; F149 takes it further.
- **Protocols** (skill 11): one source behind three doors, HTTP, tools and MCP resources.

## Approved during the build

(empty)

---
[Docs index](../README.md) · [Plans](README.md) · Related: [F125](F125-an-agentic-site.md) · [ADR 0070](../adr/0070-packs-publish-data-feeds.md) · [ADR 0072](../adr/0072-feeds-are-mcp-resources.md) · [ADR 0039](../adr/0039-api-routes-under-v1.md)
