# F148: One docs endpoint, and every reader goes through it

**Status:** draft, written 2026-11-13 (thread "Plan a docs API endpoint", Louis: "can we plan a docs api end point
all docs must be served from that endpoint. this is next"). **Branch:** `feature/F148-one-docs-endpoint`.
Louis picked the big one on 2026-11-13: **the words stay markdown, the facts are generated** (*Picked*). Seven
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

**The scope: the framework's docs and the packs', not the implementations'** (Louis, 2026-11-13, after the plan
merged: "we are only caring about the framework docs at this point no implementation docs, and offcourse packs").
In scope: the three docs at the root, `src/docs/` and `packs/docs/`, 252 docs. Out of scope for now: the demos' own
docs (`demos/puls/`, `demos/louis-agent/`, 38 docs), which keep their place, their names and their shape; no
header, no pattern, no move and no test reaches them in F148, F150 or F149. The library still *serves* whatever is
under the docs root, as the tools do today, so the guide loses nothing it can read now; a doc with no header is
listed with no type. The demos' docs get the same treatment later, as their own row, when Louis asks.

**What the ten feature docs become: A, they fold into six draft plans** (card of 2026-11-13, after "we should also
go through all features, and look at where they stand"; where each stands is in *The feature docs* below). One kind
for work not yet built, the plan: F7 takes F6, F11 and today's draft; F9 takes F16; F15 takes F13; F12 becomes
F44's; F8 and F10 each become their own. `src/docs/features/` goes as `specs/` does, and "feature doc" is not a
kind. This is the *design doc* or *RFC* way, one proposal per piece of work, kept until it is built
([Design docs at Google](https://www.industrialempathy.com/posts/design-docs-at-google/), Rust's RFCs). Not picked:
the feature doc kept as a kind beside the plan (two docs to keep in step for one feature, as F7 has today), and
removing all ten (October's stories would leave the working folder).

### Open choices (asked one at a time)

1. **Which standards the docs follow.**
   - **A. The known standard for each question, kept light:** Diátaxis for the kinds, C4 for the layers, MADR for
     ADRs, arc42's sections under plain names for a project doc (ten, not twelve), Good Docs-style templates,
     lowercase-hyphen file names, a front-matter header. Nothing invented, nothing heavier than what we write
     today. *Recommended.*
   - **B. arc42 in full for every project doc:** all twelve sections, its names. The most recognisable to an
     architect; but arc42 describes one system, and 34 project docs would each carry five sections with nothing to
     say.
   - **C. Our own pattern, written down:** today's most common sections become the rule, no outside names. The
     least change; a new developer learns our words instead of ones they may know.
2. **What the header holds.**
   - **A. Three fields: `type`, `title`, `about`.** Enough for the list and for finding a project's docs.
     *Recommended.*
   - **B. Those plus `adrs`, `feature` and `updated`.** A richer index; more to keep right by hand on 388 docs.
   - **C. No header: read the type from the doc's first line and the title from its heading**, as the docs are
     written today. Nothing to add; but it parses prose, and "about" has nowhere to live.
3. **How strictly a kind's pattern holds** (F150's rule, asked here because step 3's test starts it).
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

6. **Where the header's reader comes from** (asked when step 2 starts; the options are in that step): the pack's
   own reader with YamlDotNet (recommended), a shared reader below core, or plain string methods.
7. **How the 252 headers get written** (asked when step 3 starts; the options are in that step): the docs' tool
   writes them once (recommended), by hand a kind at a time, or as each doc is next touched.
8. **Where ADR 0108 and ADR 0111 live** (F150; the two the owner rule does not settle).
   - **A. Both stay with the framework:** each holds a rule the framework's code or tests keep (the recording
     player; the one place in `src/` that may reference a pack). The site's ADR index links them.
   - **B. Both move to `src/site/docs/adr/`:** each is named for the site. The framework's guide then cites a rule
     kept in the site's folder.
   - **C. 0111 stays, 0108 moves:** 0111 is the repo's layout, which the development guide cites; 0108 opens with
     the site's own door, and the player it names is already in `louis-agent.web`'s design doc. *Recommended.*

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
solution strategy, crosscutting concepts, glossary) are said once for the whole system on layer 3, not 34 times.

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
  plans/  fixes/                               5  being built: gone when done         (plan, fix)
  features/  specs/                               going (F150): folded into plans, or rewritten or removed
  templates/                                      new: one template per kind (F150)
src/site/docs/         new (F150)                 the same layers 4 and 5, for the framework's own website
packs/docs/            projects/ 9, adr/ 3        the same layers 4 and 5, for the packs
demos/*/docs/                                     out of scope for now: left as they are
```

**What moves** (Louis: "we will move them if needed, and you must show it"). Six of the framework's docs sit outside their layer
today. F150 moves them, or removes them, each move its own small commit with its links fixed; nothing else changes place.

| Today | After | Why |
|---|---|---|
| `src/docs/MODELS.md` (a how-to) | `src/docs/guides/choose-a-model.md` | A how-to belongs with the how-tos, layer 4 |
| `src/docs/AGENT_COMMUNICATION.md` (how the hosts talk) | `src/docs/architecture/agent-communication.md` | It explains the whole, layer 3 |
| `src/docs/specs/RESPONSE_OPTIMISATION.md`, `USAGE_AND_BUDGETS.md` (in progress: F1 and F2 built) | `src/docs/architecture/`, rewritten to hold only what is built | A spec whose work is partly built is finished as a proper explanation, layer 3; what was never built is dropped from it |
| `src/docs/specs/SESSION_ARCHITECTURE.md`, `PDF_SEARCH.md` (ideas, nothing built) | removed | An idea nobody is building is not a doc; its TODO row keeps the one line |

The specs (Louis, 2026-11-13: "specs which is not built can be removed, in-progress ones must be fnished as proper
docs with only what is built"): so `src/docs/specs/` goes, and "spec" is not a kind (the demos' two specs are out of scope and stay). A removed spec stays in git at
the tag `pre-f150`, where the TODO's links to one then point (the row F94 and the known limitation that names the
session architecture), as the rows of CL24's folded docs point at `pre-cl24`.

What links to a spec today, so what F150 touches when the spec goes or moves (Louis: "which are affetected"):

| Spec | What happens | What links to it |
|---|---|---|
| `SESSION_ARCHITECTURE.md` | removed | The feature doc F09, the design docs of `louis-agent.acp` and `louis-agent.api`, a known limitation in the TODO, the docs index |
| `PDF_SEARCH.md` | removed | The TODO row F94, the docs index |
| `RESPONSE_OPTIMISATION.md` | rewritten in `architecture/` | The feature docs F08, F09, F10, F12, F13, F15 and F16 and their index, write-up 13, the usage probe's design doc and how-to, the docs index |
| `USAGE_AND_BUDGETS.md` | rewritten in `architecture/` | The feature docs F06, F07, F08, F11 and F12 and their index, the docs index |

No plan is touched; the solution file lists the four and loses or renames those lines.

**What stays:** the demos' docs, all of them, as they are (out of scope); the plans (Louis: "plans stay"): every plan and draft in a `plans/` folder keeps its place, built
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

### Whose doc it is: one docs folder per owner

Louis, 2026-11-13: "all frameworks docs under src, and refs to relevant projects. So specific ADR's must move out, in
the case of .site, it will get its own docs folder, of it's stuff and ADR's", and "feature docs for the framework
under framemwork and .site gets its own, same for puls etc." ADR 0074 already says a doc lives with what it is
about; this makes it hold for the site, which has no docs folder today, and for the ADRs that are not the
framework's.

| Owner | Its docs folder | Holds |
|---|---|---|
| The framework (engine, hosts, web, evals, tools) | `src/docs/` | Everything in the layers above |
| The framework's own website (`louis-agent.site`, its guide, its showroom, its host) | `src/site/docs/`, **new** | `adr/`, `projects/`, `guides/`, `plans/`, and a `README.md` index |
| The packs | `packs/docs/` | `adr/`, `projects/` |
| Puls | `demos/puls/docs/` | As today |
| Our own demo (the coding agent, the triage team) | `demos/louis-agent/docs/` | As today |

**Every kind of doc works this way** (Louis, 2026-11-13: "all docs must work like this"): an owner's folder has the
same subfolders as the framework's, and a doc of any kind goes in its owner's.

| Kind | Folder, under each owner's docs folder | Sorted by |
|---|---|---|
| ADR | `adr/` | The code its *Decision* changes |
| Project doc | `projects/` | The project it describes |
| How-to | `guides/` | The thing it changes: a how-to for one site or one pack is that owner's; a how-to for extending the framework (*Build a pack*, *Spawn a site*) is the framework's |
| Plan | `plans/` | Where most of its steps land |
| Fix doc, and a fix's lesson | `fixes/`, `learning/fixes/` | The code the fix changed, settled as each lesson file is written (F150) |

**Three things stay one for the whole repo**, because each is a single list or a single reader's: the front door
and the lists (`TODO.md`, `ROADMAP.md`, the work log, the fixes table, the ADR index), which name every owner's
items and link to them; the write-ups by skill in `src/docs/learning/`, which are what Louis learned and follow
GOAL.md's skills, not a project; and the templates in `src/docs/templates/`, one set every owner copies.

**The rule that sorts an ADR:** it lives with the code its *Decision* changes. A decision the site asked for but
that added a part to the framework (the open door, the showroom area, a message that carries the page) is the
framework's, and stays. A decision about what one site says, sells or looks like is that site's, and moves.
**Numbers stay shared**, and `src/docs/adr/README.md` keeps listing every ADR with a link to wherever it lives (the
rule today). **References, not copies:** the framework's indexes (`src/docs/README.md`, `projects/README.md`,
`guides/README.md`) link to an owner's docs folder and hold none of its pages.

**What moves out of `src/docs`** (F150, each move its own small commit with its links fixed):

| Today | After | Why |
|---|---|---|
| `src/docs/adr/0100` the site tells the whole architecture, `0112` the site sells first, `0113` the guide is Louis and says it is the AI, `0122` the site's CV is edited on its own page | `src/site/docs/adr/` | What louis-agent.dev says and sells; no other site inherits it |
| `demos/louis-agent/docs/adr/0066` the site is static Razor, `0077` films on GitHub are rendered WebP | `src/site/docs/adr/` | The site left that demo for `src/site/` in F124; its ADRs stayed behind |
| `src/docs/projects/louis-agent.site.md`, `louis-agent.identity.guide.md`, `louis-agent.identity.showroom.md`, with their diagrams | `src/site/docs/projects/` | The site's own projects |
| `src/docs/guides/change-the-site.md` | `src/site/docs/guides/` | A how-to for the site only |
| `src/docs/plans/F143-the-guide-drives-the-cv.md` | `src/site/docs/plans/` | A feature of the site and its guide (the resume pack it adds is named in the packs' docs when built) |
| `src/docs/adr/0101` a lead is a tool sent once on the visitor's yes | `packs/docs/adr/` | The leads pack's own decision |
| `src/docs/adr/0095` Puls runs from Azure, `0097` Puls deploys from GitHub without a password, `0098` the demos' app runs beside Puls | `demos/puls/docs/adr/` | Puls's hosting; the files that hold it are `demos/puls/config/azure` and `deploy/azure` |
| `src/docs/plans/puls-track.md` | `demos/puls/docs/plans/` | Puls's own track |

**Stays in `src/docs/adr` although it names the site or Puls**, since its decision is a part of the framework: 0019
(modes, one file per product), 0079 (a specialist's report shown whole), 0099 (the open door and its cap), 0102
(third-party marks), 0104 and 0105 (a message carries the page; a client-side effect is a tool call the client
hears), 0106 (every generic part of a business website is the framework's), 0107, 0109, 0110, 0114, 0115 and 0116
(the showroom area and its live demos), 0117, 0120, 0121. The plans F125, F131, F133 and F134 stay too: most of
their steps are in the framework's web, the API or a pack. **Two are not clear and are Louis's pick** (open
choice 8): 0108 (the public watches the coding agent recorded: the door is the site's, the player is the
framework's) and 0111 (the framework's website lives in `src/site/`: about the site, but it is the rule that lets
one place in `src/` reference packs).

**The site's image reads its own docs too:** step 4's Dockerfile line copies `src/site/docs` beside the others, so
the guide on louis-agent.dev can still answer about the site it stands on.

### The feature docs: where each stands

Louis, 2026-11-13: "we should also go through all features, and look at where they stand. should they be reemoved
merged or marked done." Checked against the code on 2026-11-13, by looking for each doc's own implementation steps
(the classes and settings it names):

| Doc | Says | Built | Verdict |
|---|---|---|---|
| F6 Cost estimates | merged into F7 | nothing (`UsageHistory`, `Estimator`: not in the code) | merge into F7's one doc |
| F7 Budgets | planned | nothing (`BudgetGuard`, `BudgetLedger`) | stays, as F7's one doc; `plans/F07-budgets.md` is a second doc for the same row and folds in |
| F8 Reliability | planned | nothing (`RetryingChatClient`, `FallbackChatClient`, `AccountLimitException`) | stays |
| F9 Bounded history | planned | nothing (`ClearStaleToolResults`, `CompactAsync`) | stays, takes F16 in |
| F10 Settings per route | planned | nothing (`AGENT_MAX_TOOL_ROUNDS`, `ORCHESTRATOR_THINKING`) | stays, but its design predates roles declaring their own settings (ADR 0025) and is rewritten when planned |
| F11 Usage tab and the bill | merged into F7 | nothing (`UsageQuery`, the reconciliation) | merge into F7's one doc |
| F12 Response speed | merged into F44 | nothing (no `ttft_ms` on a ledger record) | becomes F44's doc: F44 has none of its own |
| F13 Local models | merged into F15 | nothing (`num_ctx`, `ContextBudget`, a compact prompt) | merge into F15's one doc |
| F15 Model limits | planned | nothing (`ModelLimits`, `LLM_CONTEXT_TOKENS`) | stays, takes F13 in |
| F16 Summariser | merged into F9 | only the oversized-results log it starts from, which is older | merge into F9's one doc; the row F16-S1 under *Cleanups* stays |

**So none is done, none is half built, and none is dead:** every one still has an open row in the TODO. Five say
"merged into" another but still sit beside it as a doc of their own, which is the one thing wrong: ten docs for
five pieces of work (six with F44). All ten are October's designs, from before the engine, identity and pack split,
so the class and file names in them are partly gone (`run-demo.ps1`, `LouisAgentClient`, `Skills/local.md`); each
is rewritten into its plan when its work starts, as the drafts in `plans/` already say of themselves.

**All ten are the framework's**, so by the owner rule they stay under `src/docs/`. The two that are not are already
with their owner and out of scope: F17 (our demo's orchestrator, merged into F39) and F28 (Puls's team talk, step 1
built).

**What F150 does with them** (Louis's pick A), each its own small commit with its links and its TODO row's *Detail*
fixed:

| Draft plan in `src/docs/plans/` | Folded from | TODO row |
|---|---|---|
| `F07-budgets.md` (there today, as a draft) | `features/F06`, `F07`, `F11` | F7 *Cost you can see and stop* |
| `F08-reliability.md` | `features/F08` | F8 *Reliability* |
| `F09-long-chats.md` | `features/F09`, `F16` | F9 *Long chats*; F16-S1 under *Cleanups* keeps its row |
| `F10-settings-per-role.md` | `features/F10` | F10 *Settings per role and per session* |
| `F15-model-limits.md` | `features/F15`, `F13` | F15 *Model limits and local models* |
| `F44-the-engine-as-a-product.md` | `features/F12` | F44 *The engine as a product* |

Each opens with the drafts' line ("Plan draft, not yet approved ... rewritten into the plan Louis approves when the
work starts") and keeps the stories, the acceptance criteria and the done-when; where two docs say the same thing
it is said once. `features/README.md`'s *Finishing a feature* list moves to the plans' README, since the
development guide links it. The old docs stay in git at the tag `pre-f150`.

### The doc design: one pattern per kind of doc

Louis, 2026-11-13: "Project docs should all follow same pattern", "all docs". A doc's *kind* is the `type` in its
header, and each kind has one pattern: the same sections, in the same order, under the same names. What the docs do
today (counted on `main`; the counts are the framework's and the packs', 252 docs in scope):

| Kind | Docs | Today | The pattern (proposed: what most of them already do) |
|---|---|---|---|
| Every doc | 252 | Six different opening lines (`How-to.`, `Kind:`, `Explanation.`, `Status:`, `Skill:`, ...) | The header; one `#` title; one opening line that says what it answers; the index line last |
| Project design doc | 34 | Most hold *Purpose*, *How it works*, *Structure* and *Tests* (36, 36, 30 and 39 of the 43 with the demos'); two opening styles; 20 one-off sections | *Purpose*, *How it works*, *Diagrams*, *Structure*, *Configuration*, *Extending it*, *Tests*, *Design notes*, *Limits and plans*, *Related docs* |
| ADR | 114 | All hold *Context*, *Decision*, *Consequences*; 112 *Links* | *Context*, *Decision*, *Consequences*, *Links* |
| How-to guide | 23 | 15 end with *Before you ask for a review*, 10 hold *Test it*; the rest differ | The steps, each named for what you do; *Test it*; *Before you ask for a review* |
| Write-up | 14 | 12 hold *Before and after*; 4 or fewer hold each other section | *Before and after*, *What was learned*, *What didn't work*, *Trade-offs accepted*, *What I can now explain*, *What's left* |
| Plan | 19 | Ruled by the plans' README | *Context*, *Design*, *Steps*, *Verification*, *Concepts*, *Approved during the build* |
| Fix doc | open ones | Ruled by the fixes' README | *Symptom*, *Cause*, *Fix*, *Tests*, *Confirmed*, *Lesson* |
| Explanation (architecture) | 13 | Free | The opening line and the index line; the sections are the doc's own |

A section a doc has nothing for is left out, never left empty; a project doc's one free place is *Design notes*,
which takes today's one-off sections ("What the build decided"). The pattern is held the way the repo holds a rule
it grows into: a test that reads each doc's `type` and checks its sections, with the docs that don't fit yet in a
list that can only shrink (as `WriteUpsTests` does).

**Where it is built.** The header and the test for "every doc" are F148's steps 2 and 3. Bringing each kind to its pattern
is **F150**, its own feature between F148 and F149, the 34 project docs first: F149 can only generate a project
doc's *Structure*, *Configuration* and *Tests* once every project doc has them in the same place.

### Where each pattern is used

Louis, 2026-11-13: "also identity where we will use which pattern". Every doc in scope (the framework's and the packs'), by the pattern it
follows after F150; the count is today's, on `main`. The demos' docs are out of scope and not counted.

| Pattern (its template) | The standard behind it | Where it is used | Docs | Layer |
|---|---|---|---|---|
| Front door (no template: one of each) | GitHub's README convention | `README.md`, `ROADMAP.md`, `TODO.md` at the root | 3 | 1 |
| Index (`index.md`) | Diátaxis reference; generated by F149 | The `README.md` of every docs folder: `src/docs/`, `adr/`, `projects/`, `guides/`, `learning/`, `plans/`, `fixes/`, `architecture/`, and the packs' | 11 | 2 |
| Standing page (`reference.md`) | Diátaxis reference | `goal.md`, `setup.md`, `work-log.md` | 3 | 2 |
| Explanation (`explanation.md`) | Diátaxis explanation; C4's containers | `src/docs/architecture/`; joined by the protocols pages (from `AGENT_COMMUNICATION.md`), and the two rewritten specs | 13, then about 19 | 3 |
| Project design doc (`project.md`) | arc42's sections under plain names; C4's components | `src/docs/projects/` (25), `packs/docs/projects/` (9) | 34 | 4 |
| How-to (`how-to.md`) | Diátaxis how-to, and tutorial for a first run | `src/docs/guides/` (23); joined by `choose-a-model.md` | 23, then 24 | 4 |
| ADR (`adr.md`) | Nygard's ADR as MADR | `src/docs/adr/` (111), `packs/docs/adr/` (3) | 114 | 5 |
| Lesson (`lesson.md`), new | The blameless postmortem (Google's SRE book): what happened, why, what it taught | `learning/fixes/` in its owner's docs folder, one file a fix | 45 today in one doc, about 150 when the owed ones are written | 5 |
| Write-up (`write-up.md`) | Ours, from GOAL.md's "a skill counts as learned when": measured, trade-offs, what didn't work | `src/docs/learning/`, one page a skill | 13 | 5 |
| Plan (`plan.md`) | Ours (the plans' README); the nearest outside name is a design doc or RFC | `src/docs/plans/` | 19 | 5 |
| Fix doc (`fix.md`) | Ours: symptom, cause, fix, tests, confirmed, lesson | `src/docs/fixes/` (1 open) | 1 | 5 |
| Folder README (`folder-readme.md`) | GitHub's README convention | `src/site/config/README.md`, `packs/tests/README.md`: what is in the folder and how to use it | 2 | with its folder |

**Markdown that is not a doc**, so it gets no doc pattern and the endpoint doesn't list it: the prompts and skills
(71; their own header and rules, ADR 0063), the evals' manuals and rubrics (12; an eval's content, F126), Claude's
own instructions (12: every `CLAUDE.md` and `.claude/rules/`, held to 200 lines), `THIRD-PARTY-NOTICES.md`, a pack's
data (`recipe-sites.md`) and a test fixture.

### Interface design

The pictures show the recommended options and are redrawn if a pick changes them.

![One library, three doors: the tools, the API's routes and the MCP resources read IDocLibrary, and only the docs pack knows the files](diagrams/F148-one-docs-endpoint-interface-design.svg)

```csharp
public interface IDocLibrary                       // louis-agent.composition, step 5
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

### The software patterns, and where each is used in the build

Louis, 2026-11-13: "now also add what software patterns will be used where during the build", "with examples".
Looked up in Fowler's [catalogue](https://martinfowler.com/eaaCatalog/) (PoEAA), the Gang of Four as
[Refactoring.Guru](https://refactoring.guru/design-patterns/catalog) tells them, and *Building Evolutionary
Architectures* for the test. Each names a shape the step already needs (ADR 0055): none is added for its own sake.
The examples are sketches for the recommended options, not the code to copy; a pattern that hangs on an open choice
says so, and any design choice the build turns up still goes to Louis with three options (rule 11).

| Step | Where | Pattern | What it does for us |
|---|---|---|---|
| 1 | `DocLibrary` | **Repository** (Fowler) | One object that stands for "all the docs". Nobody else walks the folder |
| 1, 8 | `DocsTools` | **Adapter** (GoF) | Turns the library's answers into the text a model reads; the tools hold no file code any more |
| 2 to 7 | `DocInfo`, `DocSource`, `DocPage`, `DocHit` | **Value Object** (Fowler; our records, ADR 0040) | Small values that are equal when their fields are; nothing can change one after it is made |
| 3 | `EveryDocHasAHeaderTests` | **Fitness function** as a ratchet (Ford, Parsons, Kua) | A test that holds the rule, with a list of docs still owed that can only shrink |
| 4 | the stamp the Dockerfile writes | **Build once** (Humble and Farley, *Continuous Delivery*) | The image says which commit it is; nothing works it out later |
| 5 | `IDocLibrary` in composition, `DocLibrary` in the pack | **Separated Interface** with **Plugin** (Fowler), as `IDataFeed` | The API reads docs without referencing the docs pack (ADR 0027). *Hangs on choice 5* |
| 5 | `AgentCatalog.Docs` | **Registry** (Fowler), the catalogue we have | The one place a host asks "which pack serves docs?" |
| 6, 7 | `DocsEndpoints`, `DocsHandlers`, `DocsReply` | **Remote Facade** with a **Data Transfer Object** (Fowler) | A few coarse routes over the library, answering with a record shaped for the wire (ADR 0045) |
| 9 | `DocResources` | **Adapter** (GoF), as `FeedResources` | Each doc becomes the MCP SDK's own resource type |
| 9 | `McpPrimitives` | **Facade** (GoF), the one we have | Both transports get the docs through its one call |
| not used | a Remote Proxy for the tools | (GoF) | Only if choice 4 goes to "the tools call `/v1/docs`"; F125.8 brings it for every tool at once |
| not yet | a Strategy for search | (GoF) | When F125.1 adds ranked search there are two ways to search; with one, a method is enough |

**Repository** (step 1): the library is the only code that knows the docs are files.

```csharp
public sealed class DocLibrary(string root) : IDocLibrary
{
    public IReadOnlyList<DocInfo> List() => _docs;                       // read once, at start
    public DocPage? Read(string path) =>
        _byPath.TryGetValue(path, out var info) ? new DocPage(info, File.ReadAllText(Full(path))) : null;
}
```

**Adapter** (steps 1 and 8): the tool only turns the library's answer into what the model reads.

```csharp
public string ReadDoc(string path) =>
    library.Read(path) is { } page
        ? page.Markdown + MessageText.Format(DocsToolMessage.Source, SourceLine.Of(page.Info, library.Source))
        : MessageText.Format(DocsToolMessage.DocNotFound, new DocPath(path));
```

**Value Object** (steps 2 and 4): a record, with the one thing it can work out.

```csharp
public sealed record DocSource(string? Commit, string? Repository)
{
    public string? UrlOf(string path) =>
        Commit is null || Repository is null ? null : $"{Repository}/blob/{Commit}/{path}";
}
```

**Separated Interface with Plugin** (step 5): the contract sits where both sides can see it, and the pack plugs in.

```csharp
// louis-agent.composition: all the API ever sees
public interface IAgentPack { IDocLibrary? Docs => null; /* ...as today */ }

// louis-agent.pack.docs: the one implementation
public IDocLibrary? Docs => options.Root is null ? null : _library;
```

**Remote Facade with a Data Transfer Object** (step 6): a named handler, a typed answer, a record for the wire.

```csharp
public static Results<JsonHttpResult<DocReply>, NotFound<ApiError>> GetDoc(string path, AgentEngineFactory engines) =>
    engines.Catalog.Docs?.Read(path) is { } page
        ? TypedResults.Json(DocReply.Of(page), InfoJson.SnakeCase)
        : ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownDoc, new DocPath(path)));
```

**Fitness function as a ratchet** (step 3): the rule fails for a new doc, and the old ones are a shrinking list.

```csharp
[Test]
public void EveryDoc_SaysWhatItIs()
{
    var without = Docs.All().Where(doc => DocHeader.Read(doc) is null).Select(Docs.Relative);
    Assert.That(without, Is.EquivalentTo(HeadersOwed));   // a doc may leave HeadersOwed, never join it
}
```

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `DocLibrary` | `packs/tools/louis-agent.pack.docs/Library/` | `LouisAgent.Packs.Docs.Library` | 1 |
| `DocHeaderReader` | `packs/tools/louis-agent.pack.docs/Library/` | `LouisAgent.Packs.Docs.Library` | 2 |
| `DocType`, `DocInfo` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 2 |
| `DocSource` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 4 |
| `IDocLibrary`, `DocPage` | `src/engine/louis-agent.composition/Packs/Docs/` | `LouisAgent.Composition.Packs.Docs` | 5 |
| `IAgentPack.Docs`, `AgentCatalog.Docs` (new members) | where they are | unchanged | 5 |
| `DocsEndpoints`, `DocsHandlers`, `DocsReply`, `DocReply` | `src/hosts/louis-agent.api/Endpoints/Docs/` | `LouisAgent.Api.Endpoints.Docs` | 6 |
| `DocHit`, `DocHitsReply` | the contract's folder; the API's `Endpoints/Docs/` | as their folders | 7 |
| `DocResources` | `src/hosts/louis-agent.mcp/Resources/` | `LouisAgent.Mcp.Resources` | 9 |
| `DocLibraryTests`, `DocHeaderReaderTests`, `DocSourceTests` | `packs/tests/louis-agent.pack.docs.tests/Library/` | `LouisAgent.Packs.Docs.Tests.Library` | 1, 2, 4 |
| `EveryDocHasAHeaderTests` | `src/tests/engine/louis-agent.core.tests/Docs/` | `LouisAgent.Core.Tests.Docs` | 3 |
| `DocsEndpointsTests` | the API's tests, `Endpoints/Docs/` | mirrors the API's | 6, 7 |
| `DocResourcesTests` | `src/tests/hosts/louis-agent.mcp.tests/Resources/` | mirrors the host's | 9 |

### Folder tree (after step 9)

```
packs/tools/louis-agent.pack.docs/
  DocsPack.cs  DocsOptions.cs  DocsToolset.cs
  Library/                                   # new
    DocLibrary.cs                            # step 1
    DocHeaderReader.cs                       # step 2
  Tools/DocsTools.cs                         # reads the library (1), names its source (8)
src/engine/louis-agent.composition/Packs/
  Docs/                                      # new, the contract
    IDocLibrary.cs  DocInfo.cs  DocType.cs  DocSource.cs  DocPage.cs  DocHit.cs
src/hosts/louis-agent.api/Endpoints/
  Docs/                                      # new, steps 6 and 7
    DocsEndpoints.cs  DocsHandlers.cs  DocsReply.cs
src/hosts/louis-agent.mcp/Resources/
  DocResources.cs                            # step 9, beside FeedResources.cs
src/site/docker/Dockerfile                   # step 4: writes the stamp beside the docs
```

### Steps (proposed, for the recommended options; settled after the picks)

Ten steps, each one commit with its test, small enough to read in about ten minutes, and a stop after each for
Louis to review (rules 2 and 5). Every step is test first: the test is written, seen failing for the reason given,
then the code makes it pass.

| Step | What | The pictures and docs it changes |
|---|---|---|
| 1 | The library: one object knows the docs | The docs pack's design doc |
| 2 | The header's reader | The same |
| 3 | The header on every doc, and the test that holds it | Every doc; the docs rules |
| 4 | The stamp: the image says which commit it is | The site's Dockerfile, its deploy doc |
| 5 | The contract: the API can ask any pack for its docs | The composition's and core's design docs |
| 6 | The endpoint: the list and one doc | The API's design doc, `agent-communication` |
| 7 | Search on the endpoint | The same |
| 8 | The guide names its source | The guide's design doc, the golden files |
| 9 | `doc://` resources | The MCP host's design doc |
| 10 | Docs and done | ADR 0123, the write-up, F125's plan |

#### Step 1: the library

- **In plain words.** Today each of the three tools walks the docs folder itself. This step puts that in one class,
  `DocLibrary`, and the tools ask it. Nothing a visitor or the model sees changes; it is the ground the rest
  stands on.
- **Test first.** `DocLibraryTests`: over a temp folder with three docs, `List()` gives their paths in order,
  `Read("adr/x.md")` gives the text, `Read("../secret.md")` gives nothing, a `.png` is not listed. It fails
  because `DocLibrary` doesn't exist.
- **Then.** `Library/DocLibrary.cs` takes the folder walk, the "is it a doc" check, the size limit and the boundary
  guard out of `DocsTools`; `DocsTools` takes a `DocLibrary` and keeps only the wording of its answers; `DocsPack`
  makes the one library and hands it to the tools.
- **Look closely at.** The existing `DocsToolsTests` and the pack's golden file pass **unchanged**: that is the
  proof the move changed no behaviour. `DocsTools` should hold no `File.` or `Directory.` call afterwards.
- **Concept.** Repository (Fowler). GOAL skill 4, tool design: a tool is a thin face over something testable.
- **Commit.** `refactor(f148): step 1 - the docs tools read one DocLibrary`

#### Step 2: the header's reader

- **In plain words.** Teach the library to read the three lines at the top of a doc (`type`, `title`, `about`) and
  to carry them on each doc it lists. No doc has a header yet, so every doc reads as "no header" for now.
- **Test first.** `DocHeaderReaderTests`: a doc with a full header gives its type, title and about; a doc with none
  gives nothing; an unknown `type` is refused with the doc's path and the word it didn't know; a header that never
  closes is refused. `DocLibraryTests` gains "a doc's `DocInfo` carries its header". They fail because there is no
  reader and `DocInfo` has only a path.
- **Then.** `DocType` (an enum with wire names, parsed once with `SettingChoice.TryParse`, ADR 0024) and `DocInfo`
  in the composition contract; `Library/DocHeaderReader.cs` in the pack; the library reads each header once when it
  is made.
- **Look closely at.** Where the reader comes from, which is an **open choice asked when this step starts**: the
  prompts' reader (`PromptHeader`) lives in core, and a pack may reference only the composition contract. Three
  ways: the pack reads its own three fields with YamlDotNet (MIT, the triage pack already uses it; recommended,
  the least code), a shared front-matter reader moved below core that the prompts and the docs both use, or the
  header read with plain string methods and no package.
- **Concept.** Value Object; choices as enums. Skill 7, context engineering: metadata the model can filter on
  before it reads anything.
- **Commit.** `feat(f148): step 2 - a doc's header is read into its DocInfo`

#### Step 3: the header on every doc, and the test that holds it

- **In plain words.** Now the 252 docs in scope (the root's three, `src/docs/` and `packs/docs/`) get their three lines, and a test makes sure a new doc can't be added
  without them.
- **Test first.** `EveryDocHasAHeaderTests.EveryDoc_SaysWhatItIs`: every doc at the root, under `src/docs/` and under `packs/docs/` has a
  header the reader accepts, except the paths in `HeadersOwed`. Written with `HeadersOwed` empty, it fails naming all
  252 docs: that output is kept as the "before". The demos' docs are not in the test.
- **Then.** The headers are written, and how is the second **open choice asked when this step starts**: the docs'
  tool writes them once (type from the folder, title from the `#` heading, about from the file's name for a project
  doc), reviewed as one diff (recommended: an hour, not a week); or by hand, a kind at a time over several
  commits; or none now, with all 252 in `HeadersOwed` and each written when its doc is next touched.
  `.claude/rules/docs.md` gains the rule in one line.
- **Look closely at.** The diff adds lines only, at the top of each file, and GitHub still renders each doc well (it
  shows front matter as a small table). Spot-check ten docs of different kinds for a right `type`. The films, the
  prompts and the evals' manuals are not docs and get nothing.
- **Concept.** A fitness function as a ratchet: a rule the build checks, not a rule people remember. Skill 18,
  testing and shipping.
- **Commit.** `docs(f148): step 3 - every doc says what it is, and a test holds it`

#### Step 4: the stamp

- **In plain words.** The image writes down which commit its docs came from, and the library can turn any doc's
  path into its address on GitHub at that commit.
- **Test first.** `DocSourceTests`: a root with a stamp of commit `527ab4e` gives
  `https://github.com/bosbevk/louis-agent/blob/527ab4e/README.md` for `README.md`; a root with no stamp gives no
  address and a source that says "local". `SiteImageTests` gains the Dockerfile line that writes the stamp. They
  fail because nothing reads or writes one.
- **Then.** `DocSource` in the contract; the library reads the stamp file once; `DocInfo` gains `GitHubUrl`; the
  site's Dockerfile takes the commit as a build argument and writes it beside the docs; the deploy script passes
  `git rev-parse HEAD`. The demo's image is an implementation and is left as it is, so its guide says "local".
- **Look closely at.** The repository's address is a setting or part of the stamp, never text in core or the pack
  (core names no product, ADR 0027). A local run must say "local" and not guess a commit: unknown is unknown,
  as with prices (ADR 0007).
- **Concept.** Build once (*Continuous Delivery*): the artifact carries its own identity. Skill 7: provenance,
  where a piece of context came from.
- **Commit.** `feat(f148): step 4 - the image stamps its docs with their commit`

#### Step 5: the contract

- **In plain words.** The API may not know the docs pack (ADR 0027). So the contract every pack follows gains one
  optional member, "here is my docs library", and the catalogue offers it to whoever asks. This is how feeds
  already work.
- **Test first.** A catalogue built with the docs pack gives its library through `AgentCatalog.Docs`; one built
  without it gives nothing; two packs that both offer docs are refused when the catalogue is built, with both
  names. `PackConvertibilityTests` still passes. They fail because the member doesn't exist.
- **Then.** `IDocLibrary` and `DocPage` in `louis-agent.composition/Packs/Docs/`; `IAgentPack.Docs => null` by
  default, so no other pack changes; `DocLibrary` implements it; `AgentCatalog.Docs`.
- **Look closely at.** No new project reference anywhere: `ArchitectureTests` proves the API still doesn't see the
  pack. This step hangs on open choice 5 (if Louis picks "a docs feed", this step becomes "plug in an `IDataFeed`"
  and is smaller).
- **Concept.** Separated Interface with Plugin (Fowler), dependency inversion. Skill 20, extensibility.
- **Commit.** `feat(f148): step 5 - a pack offers its docs through the contract`

#### Step 6: the endpoint, the list and one doc

- **In plain words.** The two routes that started all this: `GET /v1/docs` lists every doc with its header and
  GitHub address, `GET /v1/docs/{path}` returns one doc.
- **Test first.** `DocsEndpointsTests`: the list holds every doc of a test library and the commit; one doc answers
  200 with its path, type, title, commit, GitHub address and markdown; an unknown path is 404 with the standard
  error body; `..` in a path is 404, never a file; an instance without a docs pack answers 200 with an empty list;
  both routes answer without the API key. They fail with 404: no route.
- **Then.** `Endpoints/Docs/DocsEndpoints.cs` (the routes, `RouteAccess.Open`, under the `/v1` group),
  `DocsHandlers.cs` (named methods returning `TypedResults`, ADR 0051), `DocsReply` and `DocReply` (records for
  the wire, snake case, ADR 0045); `ApiUserText.UnknownDoc`.
- **Look closely at.** The path is a catch-all (`{*path}`), since a doc's path has slashes. The answer when the
  client asks for `text/markdown` is the raw markdown; otherwise JSON. An `ETag` from the commit, so a client that
  has the doc gets a 304.
- **Concept.** Remote Facade and Data Transfer Object. Skill 11, protocols: one source behind an HTTP door.
- **Commit.** `feat(f148): step 6 - GET /v1/docs and one doc by its path`

#### Step 7: search on the endpoint

- **In plain words.** The same search the guide's `SearchDocs` does, for anyone: `GET /v1/docs/search?q=robots`.
- **Test first.** `DocsEndpointsTests`: a word in two docs gives both hits with path, line and text; no query is
  400 with the reason; `most` is clamped to 1 to 100; the hits equal what `SearchDocs` returns for the same word.
  It fails with 404.
- **Then.** `IDocLibrary.Search` and `DocHit` in the contract; the line-by-line search moves from `DocsTools` into
  the library, so the tool and the route share it; `DocHitsReply`.
- **Look closely at.** The route is registered before the catch-all, or "search" is read as a doc's path. This is
  still the plain substring search; F125.1 replaces what is behind `Search` with ranked search, and neither door
  changes.
- **Concept.** One behaviour, two doors. Skill 8's first stone: retrieval has an interface before it has a ranker.
- **Commit.** `feat(f148): step 7 - GET /v1/docs/search`

#### Step 8: the guide names its source

- **In plain words.** The fix for the screenshot. Each docs tool result ends with one line saying where the doc is
  from, and the toolset's guide tells the model to give it when asked.
- **Test first.** `DocsToolsTests`: `ReadDoc("README.md")` over a stamped library ends with
  "Source: README.md at commit 527ab4e, {its GitHub address}"; over a local library it ends with "Source: README.md,
  a local copy, not a commit". The pack's golden file changes. They fail because the result is only the text.
- **Then.** `DocsToolMessage.Source` and `SourceLocal` (texts as enum templates, their holes a record, ADR 0009
  and 0119); the line added in `ReadDoc`, and the paths in `ListDocs` and `SearchDocs` left as they are;
  `prompts/toolsets/docs.md` gains two sentences. Golden files regenerated and their diff reviewed.
- **Look closely at.** The golden diff: only the source line and the two sentences. Then the free check by hand:
  ask the local guide "is this the github readme".
- **Concept.** Grounding with provenance: the tool result answers "from where?" before it is asked, so the model
  cites instead of guessing. Skills 4 and 7. **Paid:** the guide's eval before and after, on Louis's go.
- **Commit.** `feat(f148): step 8 - a doc the guide reads says where it is from`

#### Step 9: `doc://` resources

- **In plain words.** The MCP host offers each doc as a resource an MCP client can list and read, the way it
  offers the feeds.
- **Test first.** `DocResourcesTests`, beside `FeedResourcesTests`: a library of three docs gives three resources
  with `doc://{path}`, the doc's title as the name and `text/markdown`; reading one gives its markdown; a catalogue
  with no docs gives none. They fail because `DocResources` doesn't exist.
- **Then.** `Resources/DocResources.cs`; one line in `McpPrimitives.Add`, so stdio and HTTP both get them.
- **Look closely at.** About 290 resources in one list: check the SDK pages `resources/list` or that a client copes; if
  not, that is a finding with three options, not a quiet fix.
- **Concept.** Adapter (GoF). Skill 11: MCP resources are context the application picks, tools are actions the
  model picks.
- **Commit.** `feat(f148): step 9 - every doc is a doc:// resource`

#### Step 10: docs and done

- **In plain words.** Bring everything written about this in line with what was built, and write up what it
  taught.
- **Then.** ADR 0123 (*One docs library, and every door reads it*); the design docs of the docs pack, the API, the
  MCP host, the composition contract and the guide, with their pictures redrawn from their JSON; `SETUP.md` if a
  setting was added; the write-up in `07-context-engineering.md` (provenance: what the guide said before and
  after); F125's plan amended under *Approved during the build* (its `doc://` half is done here); the work log's
  row; the TODO's row gone; this plan deleted. The full suite, once.
- **Look closely at.** The write-up answers "why a library behind three doors, and not GitHub live or everything
  as JSON" without the code open.
- **Commit.** `docs(f148): step 10 - ADR 0123, the design docs and the write-up`

## Verification

- The docs pack's, the API's and the MCP host's tests per step; the golden files at steps 1 and 8; the full suite
  once after step 9.
- Free manual check: locally, `curl /v1/docs/README.md`, then ask the guide "is this the github readme" and get the
  commit and the link.
- **Paid:** the guide's eval before and after step 8 (a few cents), only on Louis's go.

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
