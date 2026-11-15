# louis-agent.site — design

> **Kind:** web app (Blazor WebAssembly), and its host `louis-agent.site-api` · **Path:** `src/site/` · **References:**
> `louis-agent.web`, the guide (`louis-agent.identity.guide`), the triage pack's web project; the host also the
> framework's API and the docs, leads and site packs · **Runs:** louis-agent.dev, and the review copy on 127.0.0.1:5080
> **Also shows:** `component`, `sequence`

![The framework's website, and its parts: the pages, the guide, the showroom, its own host and its image](diagrams/louis-agent.site.svg)

## Purpose

This is the framework's own website, louis-agent.dev. It tells a visitor what louis-agent is, how it works, what is
built on it and what Louis sells, and the guide stands on every page to answer from the docs. It is the visitor area
of a business, built on the framework's web exactly the way a customer's site would be.

It lives in `src/site/`, beside the framework, and it is the one place in `src/` that uses packs and an identity.
Nothing else in `src/` references it, and `SiteBoundaryTests` holds that.

This doc walks through the site in the order a visitor meets it: the host that serves it, the pages, the showroom of
demos, the films, and then what the site says about itself and what it sells. The CV and its editor come after, and
the lookups (the folders and the tests) close it.

*Decided in:* F124, [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md).

## How it works

### 1. The host, and what it serves

![louis-agent.site: How it runs](diagrams/louis-agent.site-how-it-runs.svg)

The host is a few lines, `src/site/louis-agent.site-api/Program.cs`. Its catalogue, `SiteCatalog`, holds the guide
alone, so the guide is the default agent, on exactly the three packs the catalogue names. The program serves two
things: the site's pages as HTML, and the API the guide's chat runs on, behind the key check with the guide's open
door. Nothing else runs there. There are no accounts, no eval studio and no coding agent with a shell.
`SiteCatalogTests` holds both halves.

A page the host renders asks for a few services, and they are one named list, `SitePageServices`: the chat's words,
an HTTP client, the saved CV, and the site's addresses and areas as the site's own program registers them. With the
areas in that list, the showroom's start has its demos in the HTML a search engine reads. `PagesForACrawlerTests`
draws every page with the list, so a page that can no longer be drawn there fails a test instead of quietly going
out as the plain shell. The host draws a page once, at its own route and without its layout, and serves that for
every place on the page.

The admin is the framework's, named in `LouisAgentDevAreas` with the site's own `Layout/AdminLayout`. Its page
`/admin/overview` shows the guide's talks today and this month, their cost in rand, the leads (the guide's
`SendLead` calls), the parts of the site and the demo businesses those talks were on, and the latest talks. It reads
them from the turn log and the ledger in the log folder. It sits behind `ADMIN_API_KEY`, else `AGENT_API_KEY`, and
nothing public links there (`SiteAdminTests`).

The site ships as one image, `src/site/docker/Dockerfile`: the ASP.NET runtime with the host, the docs the guide
reads (laid out as the repo, without the films), and the site's own price and rate tables, so in Azure the file share
keeps only the logs. `src/site/docker/docker-compose.yml` runs it on 127.0.0.1:5080, `src/site/config/` holds its
settings, and `src/site/deploy/azure/louis-agent-app.yaml` is the Container App louis-agent.dev runs
(`SiteImageTests`).

*Decided in:* F121 (pages as HTML), F127.8 (the admin).

### 2. The pages

The home page is a run of bands the visitor scrolls, inside the site's layout.

![The home page holds its bands in order, from the hero to the contact strip, inside the site's layout](diagrams/louis-agent.site-components-home.svg)

Every page is built from bands, a `Band` per section, each with an id in words. So every place on the site has its
own address, and the guide can take a visitor there. The sitemap lists them all, and a test writes it from the
pages. A grid that once carried a summary and a story for each item is a deck of cards that open.

Every page that explains a part of the framework shows one animated diagram of it, drawn by the framework's
`GraphView` from data in `Graphs/`. Each is held to the code by a test: the identity's parts to the page's list, the
architecture's boxes to the solution file's groups, the turn's pipeline to the middleware's order, and the demos to
the folders under `demos/`.

The site sells first, and the framework is the proof. It still tells the whole architecture, each claim held to the
repo. So the menu reads What we build · Pricing · Demos · Contact · The framework. The home page, `/`, is one page
that moves, offer first. It opens on the hero, a website with an agent from R500 a month with Louis taking a lead beside
it. Then come what it does, how it works for you (three steps and the facts a buyer trusts), custom agents, the
demos, one band of proof (built on a framework of its own, measured, with links into it), and a strip that asks a
business for theirs. `/what-we-build` and `/pricing` (two plans, each a tile with its price) are the selling pages,
and `/contact` is how to reach Louis.

The framework's own pages sit under `/framework`, so the site sells first and they can move whole to
`framework.louis-agent.dev` (F130). `/framework/how-it-works` has the detailed film, one turn through core and the
rules every turn keeps.
`/framework/architecture` has the short film and four concepts, and every project that ships, by family, `src/site`
among them. Then come
`/framework/identities`, `/framework/packs`, `/framework/build-your-own` (a real program and catalogue),
`/framework/measured` (the numbers in dollars and rand, and the framework's rule), `/framework/built-on-it` and
`/framework/whats-coming` (planned features by id, each marked planned). `/csharp-ai-agent-framework` stays where
search found it.

When a page moved, the host sends each old address on, section and all, with a permanent redirect (`MovedPages` in
`louis-agent.site-api`), and `SiteLinksTests` holds every link on the site to a place on it. The pages are written
from the docs the guide reads, and every number comes from the ledger or an eval run. No page links to the
repository. The landing's `PageHead` carries the site's facts for search engines, from `Content/SiteFacts.cs`.

The guide in the corner is Louis's own: the widget is titled "Louis · AI", says it is the AI, and hides its work
(`ShowWork="false"`).

*Decided in:* F110, F111, F113, F117, F123, F128.1, [ADR 0100](../adr/0100-the-site-tells-the-whole-architecture.md),
[ADR 0107](../adr/0107-every-section-has-an-address.md), [ADR 0112](../adr/0112-the-site-sells-first.md),
[ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md).

### 3. The showroom of demos

`/demos` is the showroom's start, and three made-up businesses come first, the guesthouse leading:
`/guesthouse-demo`, `/restaurant-demo` and `/law-practice-demo`. The doctor's practice that stood there before has
gone, and its old address redirects to the guesthouse.

Each business is one `BusinessDemo`: a conversation with its agent, recorded once by
`src/site/recordings/record-showroom.sh` and played as its customer saw it, on a full mock of its own website. Each
website has a look and a brand of its own, its marks, faces and header, and is written in
`Components/Showroom/Websites/` as its scene and its places. Each has three pages a visitor clicks through, and the
law firm's Wills and estates is a fourth, opened from its card. When the recording's agent calls `ShowPage`, the mock
goes to that place ([louis-agent.identity.showroom](louis-agent.identity.showroom.md)). Once the recording ends,
*Try it yourself* puts the visitor in a live chat with the business's agent in the same corner, the page naming its
identity and a first question.

`/coding-agent` shows the coding agent fixing a bug, recorded once and played back, and `/triage-demo` the triage
team at work. Both stand on the same stage as the businesses, in their own players, each in a lit framed window
whose bar says `louis-agent · the coding agent` or `louis-agent · the triage team`. They have no window on the start.
They keep their own pages, and their place on the rail beside a demo's page.

On the start each business is a window: a still look at its website with one moment of its recorded talk, which is
the first question its demo plays, one whole sentence of the agent's recorded answer, and the place the agent
showed. The windows are in `Components/Showroom/Windows/`, one small component a business, and `SiteWindowTalks`
holds the three talks. The talk and the website's words are written there a second time, so `WindowWordsTests`
holds each to its source: the talk to the demo's page and its recording, and the website's name, line, note, photo
and menu, and the name of the place shown, to the demo's own mock website. The chat's title, the words in its box
and the few words under the place's name are the window's own.

In the showroom the guide stands as the bar at the page's foot, with its chips "Walk me through this page", "Which
demo is like my business?" and "What does it cost?". The first is the bright one, and it is true on the start and
on every demo's page, since the same three stand on all of them. On the rest of the site the guide keeps its
corner.

A walk of the start goes from its top to each business's window in turn. When the guide shows a demo, its window
comes forward, and on a wide screen the guide's answer stands beside it. A window on the start is a picture of its
demo, so no business answers there, and the guide doesn't offer to ask one live on the start. At each window it
offers to open the demo instead. Chosen, it takes the visitor to the demo's own page and meets them there with
two questions to put to the business, then a walk of the page, then a stop. A question pressed there is asked of
the business, which answers in its own window on that page, and the guide keeps the demo itself on screen while
it does. The guide's rules for this are in its skills, `focus-demos` for the showroom and `live-demo` for asking a
business, and `GuideSkillsTests` holds them.

While the guide talks, a business's recorded demo holds its play, and goes on once the guide is done, so the
visitor follows one voice at a time. Each answer turns over to show how it was made and what it cost, the bar
carries the talk's total, and each of a demo's "could also" notes is a piece a visitor presses to ask about.

*Decided in:* F120, F122, F127.1, F127.2, F128.2, F134,
[ADR 0108](../adr/0108-the-public-watches-the-coding-agent-recorded.md),
[ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md).

### 4. The films

The two films are CSS alone. `Styles/site/movie/_stage.scss` is what they share: a 1280×720 stage scaled to its
container, a floor seen from above, each part a slab raised off it, the turn as a light flying above them with its
shadow, a detail card and a caption strip. `_short.scss` (36 s, six scenes) and `_detail.scss` (60 s, ten scenes)
add where each part stands, the camera's keyframes and when each part lights up, every keyframe name ending in the
film's letter. The detailed film's six slabs inside core are the chat pipeline's middleware in its real order
(`DetailedMovieTests`). The films are the content, so a visitor's reduced-motion setting never stills them.

The films are also exported for GitHub, into `src/docs/films/`. `src/tools/louis-agent.film-export` renders them
from the same CSS frame by frame: it starts this host on a spare port, seeks each animation in a headless Chrome
and encodes the frames in Docker. `films.sha256` records what each film was made of, and `ExportedFilmsTests` fails
until a changed film is exported again.

*Decided in:* FX35, [ADR 0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md).

### 5. A visitor asks the guide

A visitor opens a page and asks. The page arrives prerendered from the host, and the guide's answer streams back
from the same host.

![A visitor opens a page and asks: the page comes prerendered, and the guide's answer streams from the same host](diagrams/louis-agent.site-sequence.svg)

### 6. What the site says about itself

The site's slogan is **100% C#, because you can.** It sits under the brand in the header (the frame's `Tagline`
slot) and in the footer, and it is the one line the site says about itself.

The startup runs on one rule, **first principles**, and the landing says so under the hero. Every decision starts
from what is certainly true and builds up from there, not from what everyone else does. Measure before you assume.
Write down why. Adopt what is proven, and build only what nobody has. The decision records in `src/docs/adr/` are
the rule at work: each says what was true, what was chosen, and why.

*Decided in:* FX79, FX81.

## What the site sells, and how to reach Louis

The guide answers questions about buying from this section, so it holds the site's public offer in full.

What we sell today is web hosting with an AI agent, for small businesses: a website for a person or a small local
company, hosted and kept running by Louis, with an agent in its corner like the site's own guide. The agent knows the
business, its hours, prices, services and story, from files the owner can read and change. It answers customers day
and night, and points them to the right place or person. Every answer it gives is measured, so the owner sees what it
costs. Every agent runs in its own closed Docker container that sees only what it is handed.

The price is R500 to R1,000 a month, by the size of the site and how much the agent has to know, plus a
once-off fee to build the site. The exact figures, the building fee's too, come from a short call. This is the one
public price. Anything else, a custom agent, the framework or a licence, is not published yet and is asked for on the
Contact page.

An agent is a few Markdown files: its voice, its roles and what it knows about the business. Louis can write a
customer's first Markdown files with them, for a fee, so the agent starts out sounding like them. That price comes
from the short call too.

For a bigger business there are custom agents: an identity that knows it, packs that reach its own systems (a
mailbox, a spreadsheet, an accounting package), hosted and measured, every change checked by evals.

To reach Louis, write to louis.marais@live.com, which every mail link on the Contact page (`/contact`) opens with a
subject already filled in, or use WhatsApp on 066 391 2470 (WhatsApp Business). The page's WhatsApp buttons open it as
a chat (`https://wa.me/27663912470`), with WhatsApp's own mark, green and words, as its brand guidelines ask. Say
what the agent should do, which systems it needs to reach, and where it should run.

*Decided in:* FX72, [ADR 0102](../adr/0102-third-party-marks-follow-their-owners-guidelines.md).

## The CV, and its editor

Louis's CV is a page at `/cv`, and it is data, not markup. Its page is also its own editor, which is this site's
own decision. The rule that keeps the editor out of the public pages is the framework's.

The record is `Content/cv.json`, in the [JSON Resume](https://jsonresume.org/schema) shape with two parts of our own:
`facts`, the row of figures, and `now`, the proof-of-work band. `Resumes/` holds its records and the strict
`ResumeReader`, for which a misspelt field is an error, not a silent gap. `ResumePrivacy` names what a public page
mustn't hold: an ID number, a salary, a birth date, a street address.

The site's host keeps it, in `louis-agent.site-api`'s `Resumes/`. It serves the CV at `GET v1/site/cv` and keeps an
edited copy at `PUT v1/admin/cv`, as `cv.json` in the folder `SITE_DATA_DIRECTORY` names (`/data` on the share in
Azure, `src/site/data` on a PC). The built-in file is the starting copy and the fallback. A save is refused when it
doesn't read strictly or holds something private. The admin reads its copy on its own route too, `GET v1/admin/cv`,
behind the same key as the save, so the editor and its card don't open without it.

The page, `Pages/About/CvPage`, draws `Components/Cv/ResumeView`, a band per part. In the browser it asks the API
(`ApiResumeSource`), and when the host renders it for a search engine it reads the saved file directly. Which of the
two it uses is a Strategy, `IResumeSource`, picked by where the page runs.

The editor is the page itself. `Admin/ResumeEditor` at `/admin/resume` draws the same view with someone listening
for a change. So its words are clicked and retyped where they stand, its lists move, lose and gain items, and what
the page draws as a composed line opens on a small form. Edits go into a `ResumeDraft` and reach the site on Save.
`Admin/Forms/OffPageForm` holds the four things the page never draws. The area, `ResumeArea`, brings its link and
its card to the framework's admin frame.

*Save as PDF* (`ResumePrint`) opens the browser's print dialog. On paper the CV is its desktop layout set for A4, in
near-black ink, with its own footer (`Styles/site/cv/_print.scss` for the sheet, each part's paper rules beside its
component), and every link is a QR code, the framework's `PaperLink`.

*Decided in:* F142, FX153, FX154, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md),
[ADR 0122](../adr/0122-the-sites-cv-is-edited-on-its-own-page.md).

## Structure

This is the place to look things up. The types first: the site holds its words, places, demos and diagrams, on the
framework's shell.

![The types: the site holds its words, places, demos and diagrams, on the framework's shell](diagrams/louis-agent.site-interface.svg)

| Path | What's in it |
|---|---|
| `Pages/` | The site's pages; under `Framework/`, architecture, identities, packs, build your own and the comparison; under `Agents/`, the demos (`RecordedChat` plays `wwwroot/recordings/coding-session.sse`; [how to record it again](../../../demos/louis-agent/recordings/README.md)) |
| `Errors/NotFound.razor` | The page for an address the site has no page for, outside `Pages/` so the page rules don't count it |
| `Layout/SiteLayout.razor` | The site's frame on the framework's `SiteShell`, the router's default |
| `Components/` | `Architecture/` (the Architecture page's families), `Movies/` (the films and `FilmFrame`), `Guide/` (`ExpertChat`, the framework's `ChatWidget` with the guide), `Landing/` (the hero, the proof tiles, the live panels), `Contact/`, `Identity/`, `Showroom/` (a business's demo, each business's mock website in `Websites/`, and the three windows on the start in `Windows/`) |
| `Resumes/` | The CV's records by concern (`Basics/`, `History/`, `Skills/`, `Privacy/`), `ResumeReader`, `ResumeDates`, where a page gets it (`Sources/`) and the editor's draft (`Drafts/`) |
| `Admin/` | The site's own area of the admin: `ResumeArea`, its link and card, `ResumeEditor` at `/admin/resume`, the page inside it (`ResumeSheet`) and `Forms/OffPageForm`; outside `Pages/`, so the page rules don't count it |
| `Content/`, `Graphs/` | `SiteDoors`, `SiteFacts`, the principles shown as the engine holds them; the diagrams' data |
| `Styles/app.scss`, `Styles/site/` | The framework's Sass and the triage pack's demo, then the site's look ([ADR 0085](../adr/0085-styles-are-written-in-sass.md)) |
| `wwwroot/` | `index.html` with its link card, the brand, the icons, the recordings, `sitemap.xml` and `robots.txt`; the CSS is built, and isn't in git |

## Tests

The tests are in `src/tests/site/louis-agent.site.tests`, NUnit with bUnit. `Pages/*Tests` render the pages and hold
them to the repo: `SiteMapTests` says every project the solution ships is named on a page, `IdentitiesPageTests` and
`PacksPageTests` every identity and pack, `BuildYourOwnTests` the real program, `ProofPageTests` the rand at the rates
file's rate and date, `StandsOnTests` that every package named is referenced, and `WhatsComingTests` that every
planned feature is an open TODO row.

`Components/Guide/GuideKnowsTheSiteTests` holds the guide's voice to every page. `Movies/*Tests` pin each film,
`Styles/StylesheetTests` reads what the build made, and `Host/SiteCatalogTests` and `Host/SiteImageTests` hold the
host and its image.

The CV has its own: `Resumes/*Tests` for the strict read, the dates, what is private and the draft;
`Components/Cv/` for the view's golden file, with `Editing/` for every change made on the page; `Admin/*Tests` for
the area and the editor against a stub of the API; and `Host/Resume*Tests` for the store and the two routes. The
framework's site rules, in `louis-agent.web.tests` under `Rules/`, hold this site like any other.

---
[Projects](README.md) · Related: [louis-agent.identity.guide](louis-agent.identity.guide.md) ·
[louis-agent.web](louis-agent.web.md) · [the coding agent's app](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)
