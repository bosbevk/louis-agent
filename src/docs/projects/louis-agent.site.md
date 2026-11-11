# louis-agent.site — design

> **Kind:** Blazor WebAssembly app, and its host `louis-agent.site-api` · **Path:** `src/site/` · **References:**
> `louis-agent.web`, the guide (`louis-agent.identity.guide`), the triage pack's web project; the host also the
> framework's API and the docs, leads and site packs · **Runs:** louis-agent.dev, and the review copy on 127.0.0.1:5080

![The framework's website, and its parts: the pages, the guide, the showroom, its own host and its image](diagrams/louis-agent.site.svg)

## Purpose

The framework's own website (F124, [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md)): what
louis-agent is, how it works, what is built on it and what Louis sells, with the guide in the corner to answer from the
docs. It is the visitor area of a business, built on the framework's web the way a customer's site is (Louis: "the
framework website is visitor area of a business"); a portal comes later (F127). Until F124 it was the landing of the
coding agent's code-review app (F109.1); now it is the framework's, beside it in `src/`, and the one place in `src/`
that uses packs and an identity. Nothing else in `src/` references it (`SiteBoundaryTests`).

## How it runs

![louis-agent.site: How it runs](diagrams/louis-agent.site-how-it-runs.svg)

- **The host is a few lines** (`src/site/louis-agent.site-api/Program.cs`): `SiteCatalog` is the guide alone, so it is
  the default, on exactly the three packs it names; the program serves the site's pages as HTML (F121) and the API the
  guide's chat runs on, behind the key check with the guide's open door, and nothing else: no accounts, no eval
  studio, no coding agent with a shell. `SiteCatalogTests` holds both.
- **Its image** (`src/site/docker/Dockerfile`) is the ASP.NET runtime with the host, the docs the guide reads (laid out
  as the repo, without the films) and the site's own price and rate tables, so in Azure the share keeps only the logs.
  `src/site/docker/docker-compose.yml` runs it on 127.0.0.1:5080; `src/site/config/` holds its settings, and
  `src/site/deploy/azure/louis-agent-app.yaml` the Container App louis-agent.dev runs (`SiteImageTests`).

## The pages and films

Every page is a `Band` per section with an id in words (F117), so every place has its own address (ADR 0107) and the
guide can take a visitor there; the sitemap lists them all, and a test writes it from the pages. A grid that carried a
summary and a story each is a deck of cards that open (F113). Every page that explains a part of the framework shows
one animated diagram of it, drawn by the framework's `GraphView` from data in `Graphs/`, each held to the code by a
test: the identity's parts to the page's list, the architecture's boxes to the solution file's groups, the turn's
pipeline to the middleware's order, the demos to the folders under `demos/` (F111).

The site sells first and the framework is the proof ([ADR 0112](../adr/0112-the-site-sells-first.md), F128.1), and it
still tells the whole architecture ([ADR 0100](../adr/0100-the-site-tells-the-whole-architecture.md)). The menu reads
What we build · Pricing · Demos · Contact · The framework. `/` is one page that moves (F110), offer first: the hero
(a website with an agent, from R500 a month, Louis taking a lead beside it), what it does, how it works for you (three
steps and the facts a buyer trusts), custom agents, the demos, one band of proof (built on a framework of its own,
measured, with links into it) and a strip that asks a business for theirs. The framework's rule moved to
`/framework/measured`, its short film and four concepts to `/framework/architecture`. `/what-we-build` and
`/pricing` (two plans, each a tile with its price) are the selling pages; `/contact`, and the demos: `/demos`, the
showroom's three first, the guesthouse leading (F128.2: `/restaurant-demo`, `/law-practice-demo`, and since F127.1
`/guesthouse-demo` in place of the doctor's practice, whose old address redirects there; one `BusinessDemo` each, a
made-up business's conversation recorded by `src/site/recordings/record-showroom.sh` and played as its customer sees
it, on a full mock of its own website in a look of its own, written in `Components/Showroom/Websites/` as its scene
and its places; when the recording's agent calls `ShowPage` the mock goes to that place,
[louis-agent.identity.showroom](louis-agent.identity.showroom.md); since F127.2 each website has three pages a
visitor clicks through, Wills and estates a fourth opened from its card, each business wears a brand of its own
(mockup 0's marks, faces and headers), and once the recording ends *Try it yourself* puts the visitor in a live chat
with the business's agent in the same corner, the page naming its identity and a first question,
[ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md)),
`/coding-agent` (the coding agent fixing a bug, recorded once and played back,
[ADR 0108](../adr/0108-the-public-watches-the-coding-agent-recorded.md)) and `/triage-demo`, both in the showroom
since F127.1 on the same stage, in their own players. The framework's own pages
sit under `/framework` (F128.1), so the site sells first and they can move whole to `framework.louis-agent.dev` (F130):
`/framework/how-it-works` (the detailed film, one turn through core, the rules every turn keeps),
`/framework/architecture` (every project that ships, by family, `src/site` among them), `/framework/identities`,
`/framework/packs`, `/framework/build-your-own` (a real program and catalogue), `/framework/measured` (the numbers in
dollars and rand), `/framework/built-on-it` and `/framework/whats-coming` (planned features by id, each marked
planned). `/csharp-ai-agent-framework` stays where search found it (F123). The site's host sends each old address on,
section and all, with a permanent redirect (`MovedPages` in `louis-agent.site-api`), and `SiteLinksTests` holds every
link on the site to a place on it. The pages are written from the docs the guide reads, every number from the ledger
or an eval run, each claim held to the repo by a test; the repository is private, so no page links to it. The
landing's `PageHead` carries the site's facts for search engines (`Content/SiteFacts.cs`, F123). Louis in the corner
is the guide's identity ([ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md)): the widget is titled
"Louis · AI" and hides his work (`ShowWork="false"`).

**The two films** are CSS alone. `Styles/site/movie/_stage.scss` is what they share: a 1280×720 stage scaled to its
container, a floor seen from above, each part a slab raised off it, the turn as a light flying above them with its
shadow, a detail card and a caption strip. `_short.scss` (36 s, six scenes) and `_detail.scss` (60 s, ten scenes) add
where each part stands, the camera's keyframes and when each part lights up, every keyframe name ending in the film's
letter. The detailed film's six slabs inside core are the chat pipeline's middleware in its real order
(`DetailedMovieTests`). The films are the content, so a visitor's reduced-motion setting never stills them (FX35).

**The exports for GitHub** (`src/docs/films/`) are rendered from the same CSS frame by frame by
`src/tools/louis-agent.film-export`, which starts this host on a spare port, seeks each animation in a headless Chrome
and encodes the frames in Docker; `films.sha256` records what each was made of, and `ExportedFilmsTests` fails until a
changed film is re-exported ([ADR 0077](../../../demos/louis-agent/docs/adr/0077-films-on-github-are-rendered-webp.md)).

## Structure

| Path | What's in it |
|---|---|
| `Pages/` | The site's pages; under `Framework/`, architecture, identities, packs, build your own and the comparison; under `Agents/`, the demos (`RecordedChat` plays `wwwroot/recordings/coding-session.sse`; [how to record it again](../../../demos/louis-agent/recordings/README.md)) |
| `Errors/NotFound.razor` | The page for an address the site has no page for, outside `Pages/` so the page rules don't count it |
| `Layout/SiteLayout.razor` | The site's frame on the framework's `SiteShell`, the router's default |
| `Components/` | `Architecture/` (the Architecture page's families), `Movies/` (the films and `FilmFrame`), `Guide/` (`ExpertChat`, the framework's `ChatWidget` with the guide), `Landing/` (the hero, the proof tiles, the live panels), `Contact/`, `Identity/` |
| `Content/`, `Graphs/` | `SiteDoors`, `SiteFacts`, the principles shown as the engine holds them; the diagrams' data |
| `Styles/app.scss`, `Styles/site/` | The framework's Sass and the triage pack's demo, then the site's look ([ADR 0085](../adr/0085-styles-are-written-in-sass.md)) |
| `wwwroot/` | `index.html` with its link card, the brand, the icons, the recordings, `sitemap.xml` and `robots.txt`; the CSS is built, and isn't in git |

## The slogan

**100% C#, because you can.** It sits under the brand in the header (the frame's `Tagline` slot, FX79) and in the
footer, and it is the one line the site says about itself.

## The one rule

**First principles.** The startup runs on one rule, and the landing says so under the hero (FX81): every decision
starts from what is certainly true and builds up from there, not from what everyone else does. Measure before you
assume. Write down why. Adopt what is proven, and build only what nobody has. The decision records (`src/docs/adr/`)
are the rule at work: each says what was true, what was chosen, and why.

## What the site sells, and how to reach Louis

The guide answers questions about buying from this section (FX72), so it holds the site's public offer in full:

- **What we sell today: web hosting with an AI agent, for small businesses.** A website for a person or a small local
  company, hosted and kept running by Louis, with an agent in its corner like the site's own guide. It knows the
  business (its hours, prices, services and story, from files the owner can read and change), answers customers day
  and night, and points them to the right place or person. Every answer it gives is measured, so the owner sees what
  it costs. Every agent runs in its own closed Docker container that sees only what it is handed.
- **The price: R500 to R1,000 a month**, by the size of the site and how much the agent has to know, plus a
  once-off fee to build the site. The exact figures, the building fee's too, come from a short call. This is the
  one public price; anything else (a custom agent, the framework, a licence) is not published yet and is asked for
  on the Contact page.
- **Your first files, written with you:** an agent is a few Markdown files (its voice, its roles and what it knows
  about the business), and Louis can write a customer's first Markdown files with them, for a fee, so the agent starts
  out sounding like them. Its price comes from the short call.
- **Custom agents** for a bigger business: an identity that knows it, packs that reach its own systems (a mailbox, a
  spreadsheet, an accounting package), hosted and measured, every change checked by evals.
- **How to reach Louis:** louis.marais@live.com, which every mail link on the Contact page (`/contact`) opens with a
  subject already filled in, or WhatsApp on 066 391 2470 (WhatsApp Business), which the page's WhatsApp buttons open
  as a chat (`https://wa.me/27663912470`), with WhatsApp's own mark, green and words, as its brand guidelines ask
  ([ADR 0102](../adr/0102-third-party-marks-follow-their-owners-guidelines.md)). Say what the agent should do, which
  systems it needs to reach, and where it should run.

## Tests

`src/tests/site/louis-agent.site.tests`, NUnit with bUnit: `Pages/*Tests` render the pages and hold them to the repo
(`SiteMapTests`: every project the solution ships is named on a page; `IdentitiesPageTests` and `PacksPageTests`: every
identity and pack; `BuildYourOwnTests`: the real program; `ProofPageTests`: the rand at the rates file's rate and
date; `StandsOnTests`: every package named is referenced, and no page links to the private repository;
`WhatsComingTests`: every planned feature is an open TODO row); `Components/Guide/GuideKnowsTheSiteTests` holds the
guide's voice to every page; `Movies/*Tests` pin each film; `Styles/StylesheetTests` reads what the build made;
`Host/SiteCatalogTests` and `Host/SiteImageTests` hold the host and its image. The framework's site rules
(`louis-agent.web.tests`, `Rules/`) hold it like any other site.

---
[Projects](README.md) · Related: [louis-agent.identity.guide](louis-agent.identity.guide.md) ·
[louis-agent.web](louis-agent.web.md) · [the coding agent's app](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)
