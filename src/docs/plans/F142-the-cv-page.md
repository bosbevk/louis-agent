# F142: Louis's CV, a page on louis-agent.dev

**Status:** approved 2026-11-13 (thread "Redo Louis's CV as a webpage", Louis: "merge and commit").
**Branch:** `feature/F142-the-cv-page`. **Mockup:** [F142-mockups/cv-page.html](F142-mockups/cv-page.html), the full
page as Louis approved it; open it in a browser from the repo so the photo loads. **After it:** F143, the guide drives
the CV (its own plan, [F143-the-guide-drives-the-cv.md](F143-the-guide-drives-the-cv.md)).

## Context

Louis, 2026-11-13: "can you redo my CV, do some research first going to be a webpage on louis-agent, links to my
Linked in and all", then "little icon CV there" (the end of the top nav), and two photos, the second one picked. His
CV today is a Word file with a 1-to-5 skills matrix, every school subject and personal details.

**What the research said** (2026 CV guides for software and AI engineers, and South African CV advice):

- For AI roles, proof of work beats a list: a link to working, measured code is what gets read. So louis-agent goes
  right under his name, with numbers from the repo.
- Each job says what was shipped, not what the duties were, newest first.
- Self-ratings (stars, 1 to 5) aren't trusted; skills are grouped instead.
- A public page leaves out the ID number, home address, age, health and salaries: they are identity-theft material
  under POPIA (South Africa's privacy law) and weaken the next pay talk.

**Louis's answers (2026-11-13):** WhatsApp on the CV, the grayscale photo, the Betterview results link, the
International High IQ Society, hobbies, and the Axiz price-rule agent named under Kohde all stay; the page lives at
`/cv` with a CV mark at the end of the nav; "20 years" of experience. **Where the facts live (his pick, card of
2026-11-13): one data file**, which the page draws and, in F143, the guide reads through a tool, so the two never
disagree.

## Design

**The shape, in plain words.**

- **One data file, in the JSON Resume shape.** The CV's facts are `cv.json` in the site project
  (`src/site/louis-agent.site/Content/cv.json`), embedded in its assembly. It follows
  [JSON Resume](https://jsonresume.org/schema) (an open standard, MIT): `basics` with `profiles`, `work` with
  `highlights`, `education`, `skills` with `keywords`, `certificates`, `languages`, `interests`. Reusing a standard
  (ADR 0030) means the file works with other CV tools and themes too, and an agent outside ours already knows the
  shape. We read only the fields the page draws, plus two of our own, the facts row and the "now" project, which
  JSON Resume allows beside its own.
- **Typed records, read once.** A `Resume` record (and its parts) and a `ResumeReader` in the framework's web
  (`louis-agent.web/Resume/`) read the file (ADR 0041). The framework names no person or site (ADR 0084): any site
  could show a CV, so the view is the framework's and the data is the site's.
- **The view is the framework's, the page is the site's.** `ResumeView` and its pieces (`ResumeHero`, `ResumeFacts`,
  `ResumeJobs`, `ResumeSkills`, `ResumeEducation`) draw the mockup's sections from the record, styles beside each
  (ADR 0094). The site's `/cv` page hands it the record and the photo, and adds the "now" band about louis-agent in
  its own words.
- **The CV mark in the nav.** The site's layout adds a small document icon and "CV" at the end of its links. It's the
  site's own link, so nothing in the framework changes for it.
- **Save as PDF.** A print stylesheet lays the same page out on A4 (no nav, no guide, black on white), and a
  "Save as PDF" button opens the browser's print dialog. One page, two outputs; no PDF library.
- **What it never holds.** A test reads `cv.json` and fails on an ID number, a street address, a birth date or a
  salary, so a later edit can't leak them.

**The patterns:** *Single source of truth* (one file, every reader goes through it); *Composite view* (the page is
built from one component per section, each drawing its part of the record); *Template View* (Fowler: markup with
holes the record fills). Each class that carries one names it in its `<remarks>` (ADR 0050).

### Interface design

![The CV as records the framework reads once, and the views that draw them](diagrams/F142-the-cv-page-interface-design.svg)

### Architecture

![The site holds the data and the page, the framework's web reads and draws it, the visitor gets plain HTML](diagrams/F142-the-cv-page-architecture.svg)

### Communication (a visitor opens /cv)

![A visitor opens /cv: the page reads the record once and the view draws each section](diagrams/F142-the-cv-page-communication-a-visitor-opens-cv.svg)

### Objects (one /cv render in memory)

![One render of /cv: the page holds the record, the view hands each section its part](diagrams/F142-the-cv-page-objects-one-render.svg)

### Folder tree (after step 4)

```
src/web/louis-agent.web/
  Resume/                    # new, step 1: the records and the reader
    Resume.cs  ResumeBasics.cs  ResumeWork.cs  ResumeEducation.cs  ResumeSkill.cs  ResumeReader.cs
  Components/Site/Resume/    # new, step 3: the view, one component per section, styles beside each
    ResumeView.razor  ResumeHero.razor  ResumeFacts.razor  ResumeJobs.razor  ResumeSkills.razor  ResumeEducation.razor
src/site/louis-agent.site/
  Content/cv.json            # new, step 2 (embedded)
  Pages/CvPage.razor         # new, step 4
  wwwroot/images/louis-marais.jpg   # new, step 4
```

### Namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `Resume` and its part records | `src/web/louis-agent.web/Resume` | `LouisAgent.Web.Resume` | 1 |
| `ResumeReader` | the same | the same | 1 |
| `ResumeReaderTests` | `src/tests/web/louis-agent.web.tests/Resume` | `LouisAgent.Web.Tests.Resume` | 1 |
| `cv.json` | `src/site/louis-agent.site/Content` | (embedded resource) | 2 |
| `CvContentTests` | `src/tests/site/louis-agent.site.tests/Content` | `LouisAgent.Site.Tests.Content` | 2 |
| `ResumeView` and its sections | `src/web/louis-agent.web/Components/Site/Resume` | `LouisAgent.Web.Components.Site.Resume` | 3 |
| `ResumeViewTests` | `src/tests/web/louis-agent.web.tests/Components/Site/Resume` | `LouisAgent.Web.Tests.Components.Site.Resume` | 3 |
| `CvPage` | `src/site/louis-agent.site/Pages` | `LouisAgent.Site.Pages` | 4 |
| `CvPageTests` | `src/tests/site/louis-agent.site.tests/Pages` | `LouisAgent.Site.Tests.Pages` | 4 |

## Steps

Each step is one commit with its test, written first and seen failing. Stop after each for Louis's review.

1. **The CV as records.** `Resume` and its parts, and `ResumeReader.Read(Stream)` with the one JSON options object
   for the file. *Test:* a small JSON Resume sample reads into the record, with its profiles, a job's highlights and
   a skill's keywords; a misspelt field is refused (strict, ADR 0049).
2. **Louis's CV as data.** `cv.json` with the mockup's content, embedded in the site. *Test:* it reads; it has the
   LinkedIn and GitHub profiles; and it holds no ID number, street address, birth date or salary.
3. **The view.** `ResumeView` and its five sections, drawn from a record, styles beside each, phone width stacked.
   *Test:* bUnit renders a sample record and pins the markup to a golden; each job shows its dates, title, company
   and highlights; a profile link opens in a new tab with `rel="noopener"`.
4. **The page and the nav mark.** `/cv` with its `PageTitle` and description, the photo, the "now" band, the CV mark
   at the end of the nav, and the address in `sitemap.xml`. *Test:* `/cv` renders Louis's name and the LinkedIn link;
   the layout's last link is `/cv`; the sitemap lists it (`SiteLinksTests`).
5. **Save as PDF.** The print stylesheet and the button (a module beside the page, ADR 0093). *Test:* the button is
   there and has a label; the rest is styles, checked by printing to PDF in Chrome.
6. **Docs and done.** The site's design doc and *Change the site* guide get the CV (how to edit `cv.json`), an ADR
   for "a CV is JSON Resume data the framework draws" (next free number), the work log row, the TODO row gone, this
   plan and its mockups deleted. *Test:* none (docs).

## Verification

- `louis-agent.web.tests` and `louis-agent.site.tests` per step; the full suite once after step 5.
- Free manual checks: `/cv` in Chrome on Windows at full width and at 390px; Save as PDF gives one or two clean A4
  pages; the LinkedIn, GitHub, WhatsApp and Betterview links open. No paid run.

## Concepts

Mostly front-end craft (GOAL.md skill 18, *Testing and shipping*). The AI-engineering idea is the data file:
**one source of truth that people and agents both read**. A page an agent has to scrape drifts from what the agent
says; a typed file both read can't. F143 builds on it (skill 4, *Tool design*).

## Approved during the build

(empty)

---
[Docs index](../README.md) · [Plans](README.md) · Next: [F143 the guide drives the CV](F143-the-guide-drives-the-cv.md)
