# LouisAgent.Site — design

> **Kind:** web app (Blazor WebAssembly), and its host `LouisAgent.SiteApi` · **Path:** `src/site/` · **References:**
> `LouisAgent.Web`, `LouisAgent.Web.Themes`, `LouisAgent.Web.Places`, the guide (`LouisAgent.Identities.Guide`); the
> host also the framework's API, core's web, telemetry and Anthropic, the showroom's identities
> (`LouisAgent.Identities.Showroom`), and the docs, leads and site packs · **Runs:** louis-agent.dev, the review copy
> on 127.0.0.1:5080, and Rider's run profile on 127.0.0.1:5083
> **Also shows:** `component`, `sequence`

![louis-agent.dev, and its parts: the pages, the guide, the showroom, the CV, its own host and its image](diagrams/LouisAgent.Site.svg)

## Purpose

This is louis-agent.dev, the website that sells websites with agents. It says what Louis sells, lets a visitor watch
three made-up businesses' agents at work and try them, and carries his CV. The guide, Orbi, stands as a bar at the
foot of every page and answers questions from the docs. The framework's own pages aren't here: they're the
framework's site, [LouisAgent.SiteFramework](LouisAgent.SiteFramework.md), meant for framework.louis-agent.dev.

It's the visitor area of a business, built on the framework's web just the way a customer's site would be. A portal
comes later. It lives beside the framework, in `src/`, and it's the one place in `src/` that uses packs and an
identity. Nothing else in `src/` references it, and `SiteBoundaryTests` holds that.

The rest of this doc follows a visitor. First the host that serves the site, how it draws a page for a search engine,
its admin and its image. Then what the site is made of, the landing page, the offer and the pages about trust. Then
the showroom, the guide in it, and how every claim is held to the repo. It ends with the CV, then the files, the
settings and the tests, to look things up.

*Decided in:* F124, [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md), F127.

## How it works

![LouisAgent.Site: How it runs](diagrams/LouisAgent.Site-how-it-runs.svg)

### 1. A host of a few lines

The host is `src/site/LouisAgent.SiteApi/Program.cs`, and it's short. Its catalogue, `SiteCatalog`, holds the guide
first, so the guide is the default, and then the showroom's three businesses: the guesthouse, the restaurant and the
law practice. All four run on exactly the three packs the catalogue names: the docs, the leads and the site.

The program serves three things. It serves the site's pages as HTML. It serves the API the guide's chat runs on,
behind the key check. And it serves the CV's two routes, which part 18 tells. Nothing else: no accounts, no eval
studio, and no coding agent with a shell. `SiteCatalogTests` holds both sides of that.

The key check has an open door. `OPEN_IDENTITIES` names the guide and the three businesses, so a visitor talks to any
of them without a key. The door counts sessions per address per day across all four: 50 on a PC and 20 in Azure
(`OPEN_SESSIONS_PER_DAY`). The guide asks a business on the same container, through `AGENT_PEERS`, which names the
host itself.

Two old addresses are sent on, for good, before any other route: `/doctors-practice-demo` goes to the guesthouse's
demo, and `/pricing` to the contact page. A section or a page after the old address comes along, and so does the
question. `MovedPages` holds the list, and `MovedPagesTests` holds it to that.

*Decided in:* F121.

### 2. Drawing a page for a search engine

When the host draws a page itself, the page asks for a few services, and they're one named list:
`SitePageServices`. It holds the chat's words, an HTTP client, the saved CV, and the site's addresses and areas, the
way the site's own program registers them. Because the areas are there, the showroom's start has its demos in the HTML
a search engine reads.

`PagesForACrawlerTests` draws every page with that list. So a page that can no longer be drawn there fails a test,
instead of quietly going out as the plain shell. The host draws each page once, at its own route and without its
layout, and serves that for every place on the page.

![A visitor opens a page and asks: the page comes prerendered, and the guide's answer streams from the same host](diagrams/LouisAgent.Site-sequence.svg)

### 3. Its admin

The site's admin is the framework's, named in `LouisAgentDevAreas`, with the site's own `Layout/AdminLayout`.

`/admin/overview` shows the guide's talks today and this month, and their cost in rand. It shows the leads, which are
the guide's `SendLead` calls. It shows the parts of the site and the demo businesses the talks were on. And it shows
the latest talks, each marked as yours when your browser held the admin key, or as a visitor's. It reads all of that
from the turn log and the ledger in the log folder. It sits behind `ADMIN_API_KEY`, or `AGENT_API_KEY` when that's
not set, and nothing public links there (`SiteAdminTests`).

The site adds one area of its own to the admin, where Louis edits his CV (part 20).

*Decided in:* F127.8.

### 4. Its image

The image, `src/site/docker/Dockerfile`, is the ASP.NET runtime with the host, the docs the guide reads (laid out like
the repo, without the films), and the site's own price and rate tables. So in Azure, the file share only has to keep
the logs and the saved CV.

`src/site/docker/docker-compose.yml` runs it as the service `site`, on 127.0.0.1:5080 by default (`SITE_PORT`). It
keeps the logs in `src/site/logs` and the CV in `src/site/data` on the PC. Its chats are kept in the MongoDB of the
service `sessions-mongo` beside it, so restarting the site leaves a conversation where it was. `src/site/config/`
holds its settings, and
`src/site/deploy/azure/louis-agent-app.yaml` is the Container App louis-agent.dev runs. `SiteImageTests` holds the
image.

The script that releases it, `deploy.ps1`, sits beside that yaml and releases the framework's own site too, one app
at a time. A merge runs it through `.github/workflows/deploy-sites.yml`, and the tests in the site's `Deploy/` folder
hold the script, both apps' yaml and the workflow. The how-to is [Deploy the sites](../guides/deploy-the-sites.md).

### 5. What the site is made of

The whole site is named in one place, `LouisAgentDevAreas.Name`. It turns on the showroom with the site's three
demos. It names the part of the site each page is, so the guide knows how to lead: it sells on every page, and
guides on `/demos` and on each demo's page. It gives the guide's bar its four chips, and opens the bar when the
pointer is on it. It turns on the admin with the overview counting leads, and adds the CV's area. It gives every
place on the pages its own address. And it offers the two themes, glass first, so glass is what a first visit gets.

That one method is the site's Composition Root, the one place where it says which parts it uses. The browser's
program and the host both hand it to `AddSiteAreas`, and the tests hand it there too, so all of them draw what the
site draws.

The site has no styles of its own. `Styles/app.scss` lists the framework's Sass, glass's and flat's sheets, and the
framework's paper sheets for the CV's print, and nothing else.

### 6. Every place has an address

Every page is built from `Band`s, one per section, each with an id in words. So every place on the site has its own
address, and the guide can take a visitor straight there. The site names its places among its areas, with
`.Places(Home, typeof(App).Assembly)`, the landing's under the word `home`, as in `/home/welcome`. The router draws a
place opened afresh in its own page's layout ([LouisAgent.Web.Places](LouisAgent.Web.Places.md)). The sitemap lists
the pages, one address each, and a test writes it from the pages. A section's address answers its page, and names the
page as the original, so a search engine isn't handed a copy of the page at every section (ADR 0128).

A grid where each item had a summary and a story is a deck of cards that open.

![The home page holds its bands in order, from the hero to the contact strip, inside the site's layout](diagrams/LouisAgent.Site-components-home.svg)

*Decided in:* F117, [ADR 0107](../adr/0107-every-section-has-an-address.md), F113, F164.1.

### 7. The framework's pages are on their own site

The framework's pages are no longer on louis-agent.dev. They're written new as the framework's pages package and
served by the framework's own site ([LouisAgent.SiteFramework](LouisAgent.SiteFramework.md)), so this site sells
and that one teaches. What stays here is `/framework/theme`, the theme's component guide, which the landing and the
themes link to.

The old framework addresses answer with nothing, and no page of this site links to them: `SiteLinksTests` holds every
link on the site to a place on it. The CV's four "now" cards, which prove its claims, lead to their places on
framework.louis-agent.dev. `CvContentTests` reads each of those places by the address rule over the pages in that
site's sitemap, so a page renamed there fails a test here.

The films aren't on the site any more either, which leaves their export tool stuck (see *Limits and plans*).

*Decided in:* F164.1, F164.3.

### 8. The landing: selling first, the framework as proof

The site sells first, and the framework is the proof, told on the framework's own site. The menu reads Home, Demos,
Framework, Theme and Contact. Home is there so every page has a plain way back to the landing, since the name in the
corner doesn't look like a link. Demos opens a short menu: all demos, then each of the three businesses. Framework
leads to framework.louis-agent.dev. Theme leads to `/framework/theme`, where every button, switch and card the site is
built from stands working, to press and try, in the look the visitor picked. At the header's end a three-line button
opens the settings, a thin glass bar under the header that holds the look switches alone. The CV is reached from the
landing's band about who builds it.

`/` is one page that moves, with the offer first. The hero stands centred and opens with the agent's mark, drawn
large. The mark plays four of its states in turn, resting, listening, thinking and answering, with a line under it
that says what each one means. A visitor who presses a state's button holds it there.

Under the mark comes the headline "Your doors close at five. Your website doesn't.", one sentence to a line. It opens
on the owner's problem rather than on what we sell. A line under it says what changes for them: the question asked at
11 at night is answered, and the person's details wait for them in the morning. Two buttons follow. "Ask for yours"
goes to the contact page, and "Watch one work" goes to the demos, so they see it before they ask.

Under the hero runs the tape, one line of what the agents did that never stops. Then comes what
it does, as four glass cards, "It does more than chat.", each card something the owner can watch happen. Where it
runs comes next, "Your data stays in South Africa.": we host it in Azure's South African region, a bigger business may
keep it on its own servers, and the data stays where it's put.

In the middle stands the brand, "Meet Orbi.": the violet ball at the foot of the page is the agent, Orbi, and the
owner's agent gets a name and a look of its own. The mark stands large with the name on black, white, violet and
glass, and the looks beside it lead to the theme's page. None of its panels can be picked to ask the guide about.

Then come the demos, three photographs fanned out, each in a glass window with its business's made-up address in the
bar. The one a visitor picks, or the guide shows, comes to the front. Custom agents come next. A product window plays
the guesthouse, the restaurant and the law practice in turn. Each is its own page, with its agent answering a visitor
from its recorded talk, drawing the request and sending it on a yes.

Then comes who builds it, the one place on the landing that names Louis. It's the framework's profile hero in a band,
with his photo, "Built by Louis Marais in Malmesbury.", a line on what he does for the owner, and a button to his CV.
A small owner buys from a person, which is why it's there. The last strip asks Orbi to take the visitor's details, or
"Write to us" leads to the contact page, and says each business is quoted after a short call. The framework itself
has no band on the landing: its pages are on its own site.

*Decided in:* F165, F145.1, [ADR 0112](../adr/0112-the-site-sells-first.md), F128.1,
[ADR 0100](../adr/0100-the-site-tells-the-whole-architecture.md), F110.

### 9. What the site sells, and how to reach Louis

The guide answers questions about buying from this part, so it holds the site's public offer in full.

What we sell today is web hosting with an AI agent, for small businesses. That's a website for a person or a small
local company, hosted and kept running by Louis, with an agent in its corner like the site's own guide. The agent
knows the business: its hours, prices, services and story, from files the owner can read and change. It answers
customers day and night, and points them to the right place or person. Every answer it gives is measured, so the owner
sees what it costs. Every agent runs in its own closed Docker container, which sees only what it's handed.

Every quote is made on request. What a website with an agent costs depends on the size of the site and how much the
agent has to know, so no price is published. Each one is quoted after a short call, and asked for on the Contact page
or by leaving details with the guide. The same holds for a custom agent, the framework or a licence. No page names a
price, which is why the old `/pricing` address goes to `/contact`.

Your first files can be written with you. An agent is a few Markdown files: its voice, its roles, and what it knows
about the business. For a fee, Louis can write a customer's first Markdown files with them, so the agent starts out
sounding like them. That price comes from the short call too.

A bigger business can get a custom agent. That's an identity that knows it, and packs that reach its own systems, such
as a mailbox, a spreadsheet or an accounting package, hosted and measured, with every change checked by evals.

To reach Louis, mail louis.marais@live.com, or use WhatsApp on 066 391 2470 (WhatsApp Business). The Contact page
(`/contact`) has two ways in side by side, a website with an agent and a custom agent. Each one's mail button opens
with a subject already filled in, and each one's WhatsApp button opens a chat with a first line written. Under them
are a plain mail button, a WhatsApp button and a LinkedIn button. The WhatsApp buttons open a chat at
`https://wa.me/27663912470`, with WhatsApp's own mark, green and words, as its brand guidelines ask. Say what the agent
should do, which systems it needs to reach, and where it should run.

How a job with a client goes has a page of its own, *Work with us* at `/work-with-us`, in the menu just before
Contact. It is the slideshow Louis hands a client, told in the site's "we", and it is the one page of the site laid
out as slides: the page asks for `PageLayout.Slides` with one line, and the frame makes every section a screen and
settles on one at a time. On that page the guide's bar stands as the corner's round button, so a slide has the whole
screen. Its nine sections go in the order of a first meeting: what we build, who builds it, where to see it working,
the seven steps from the discovery call to the handover, how the price is paid in two halves of 50%, what the client
gets at the end, what keeps costing after go-live, and the next step, which leaves details with the guide or goes to
Contact. Like the rest of the site it names no price. Its words are lists keyed by an enum of their parts in
`Content/WorkWithUs`, and the site's small `WorkTiles` draws one list as a row of tiles.

*Decided in:* FX72, [ADR 0102](../adr/0102-third-party-marks-follow-their-owners-guidelines.md); the Work with us
page in F177, FX246 and FX247.

### 10. Privacy and trust

`/privacy` tells a visitor what happens to what they type, in the order they meet it. Their messages go to Anthropic,
whose model writes the answers, in the United States, and the site holds the chat in memory only. Their details reach
Louis by mail, through Resend, and only when they press Send it. Their address counts their chats for a day, also in
memory. What's written down is the cost, the page and the tools of each answer, never their words, and the timing of
each step goes to Azure's monitoring, also without their words. The look they pick stays in their own browser.

The page then sets out their rights under POPIA, says who answers for the site, and links the Information Regulator.
Every page's footer links to it, the landing's data tile does too, and the guide points to it before it sends a
visitor's details.

`/trust` sits beside it, for a business owner who wants the technical facts before trusting the site with their
business. It has five parts, each at its own address. They're where it runs, their data, what the agent may do, and
what happens if something breaks. The last is a reviewer's checklist of what a business should ask for before it pays. Every fact is a tile
that says In place or Not yet, so a gap is named rather than hidden. The companies that handle data for the site,
Azure, Anthropic, Resend, Cloudflare and GitHub, are in a table with what each sees and where.

The facts are data in `Content/Trust/`, and every one in place names the file of the repo that shows it, which a test
opens. So the page can only say what the code does. One `FactBand` draws each part from its list. The footer links
Trust right after Privacy, and the guide sends someone who asks about security or backups there.

### 11. The showroom's three businesses

`/demos` shows the showroom's three businesses first, with the guesthouse leading. Their pages are
`/guesthouse-demo`, `/restaurant-demo` and `/law-practice-demo`. The guesthouse took the doctor's practice's place,
and the old address redirects there.

Each is one `BusinessDemo`: a made-up business's conversation, recorded by `src/site/recordings/record-showroom.sh`,
and played the way its customer would see it. It plays on a full mock of the business's own website, in a look of its
own, written in `Components/Showroom/Websites/` as its scene and its places. When the recording's agent calls
`ShowPage`, the mock goes to that place ([LouisAgent.Identities.Showroom](LouisAgent.Identities.Showroom.md)).

Each website has three pages a visitor can click through; Wills and estates is a fourth, opened from its card. Each
business wears a brand of its own, with marks, faces and headers from mockup 0.

A business's website keeps that one look whatever theme and style the page around it wears, and its agent's corner is
the business's too. Its colours are its demo page's data, a `MockColours`. They're its brand, the words on the brand, its page
and its ink, and a colour for its small capitals when the brand has one of its own. Each must be six hex digits. The
framework's mock tool sets them on the mock. It also points the theme's names that the mock's card, bar and main
button read at those colours, and lays no drawing of a theme's on them. So the site still has no styles of its own.

Once the recording ends, *Try it yourself* puts the visitor in a live chat with the business's agent, in the same
place. The page names its identity and a first question.

A website's places are the ones its recording was sent with its first question, kept in `src/site/recordings/pages/`,
and a test holds the two lists equal. Its home page's words were sent too. So a website can gain a photo without
anything else changing, and new words, a new section or a new card mean recording the conversation again.

The photos are in `wwwroot/images/showroom/`, each a WebP the site serves itself. They're third-party, from Pexels
under its licence, chosen with no recognisable person or brand in them. The three hero photos are recorded here with
where they came from. Two of them are cut from the bottom of the original, which takes a real business's sign and a
flag out of the stoep, and a painting of people out of the study. Where the other photos came from isn't recorded in
the repo.

| Photo | What it shows | Its page on Pexels |
|---|---|---|
| `seemeeu-lounge.webp` | A lounge that opens onto the sea | [14746032](https://www.pexels.com/photo/hotel-room-with-view-on-the-sea-14746032/) |
| `wheatfield-stoep.webp` | A stoep's tables in the late sun | [19648312](https://www.pexels.com/photo/patio-of-a-restaurant-19648312/) |
| `koringland-desk.webp` | A study, a desk under a lamp | [12900435](https://www.pexels.com/photo/stack-of-books-on-wooden-table-12900435/) |

*Decided in:* F128.2, F127.1, F127.2,
[ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md).

### 12. A demo's page: nothing beside the agent

A demo's page holds its heading, a few words on the scene, and the business's website with its agent at work. There
is nothing else on the stage: no list of what to watch, no notes on what a custom agent could add, and no offer bar,
because those took the eye off the agent. The framework's stage still has those slots, and `BusinessDemo` leaves them
empty. The demo pages wear the site's showroom frame, `Layout/ShowroomLayout`, whose one offer, "Ask for yours", is the
framework's `LinkButton`.

The conversation doesn't play on its own. Its first question waits in the business's window over a Play the
conversation button, so the guide in the bar, or the visitor, starts it. The window is a place of its own on the page,
`the-conversation`. So `/guesthouse-demo/the-conversation` is its first turn, and `/the-conversation/2` to `/4` the
turns after it, and each demo's conversation is in the sitemap. When the guide shows one of those turns,
`BusinessDemo` hears it through the framework's `AgentWindows` and plays that turn, which stops after it with Play on.
A visitor already talking to the business live keeps their talk: a turn the guide shows then plays nothing.

When the recording ends, the chat's own ending holds two buttons. *Try it yourself* is the theme's main button, drawn
by the framework's `RecordedChat` from the words the page hands it, and *Play it again* plays the talk from its start.
The guide in the bar is the one that explains what the visitor is seeing.

*Decided in:* F127.1, FX194, FX195, F160.

### 13. The windows on the demos' start

On `/demos`, the start only shows the three businesses, each as a window. A window is a still look at the business's
website, with one moment of its recorded talk: the first question its demo plays, one whole sentence of the agent's
recorded answer, and the place the agent showed.

The windows are in `Components/Showroom/Windows/`, one small component per business, and `SiteWindowTalks` holds the
three talks. Because the talk and the website's words are written a second time there, `WindowWordsTests` holds each
to its source. The talk is held to the demo's page and its recording. The website's name, line, note, photo and menu,
and the name of the place shown, are held to the demo's own mock website. The chat's title, the words in its box and
the few words under the place's name belong to the window alone.

A window holds no HTML of its own. It's the framework's `SiteBody` given its words, and the place shown is a
`ShownCard` the window fills in its code-behind and the framework draws. The showroom's frame is the same.
`ShowroomLayoutTests` holds the frame and the three windows to framework controls only.

### 14. The guide in the showroom

The guide stands as the bar at the page's foot everywhere, the showroom included. Its chips are "Walk me through this
page", "Which demo is like my business?", "How do I get one?" and "Tell me about the framework". The first is the
bright one, and it's true on the start and on every demo's page, since the same four stand on all of them.

A walk of the start goes from its top to each business's window in turn. When the guide shows a demo, its window comes
forward, and on a wide screen the guide's answer stands beside it. A window on the start is a picture of its demo, so
no business answers there, and the guide doesn't offer to ask one live on the start. At each window it offers to open
the demo instead.

Chosen, it takes the visitor to the demo's own page and meets them there with two questions to put to the business,
then a walk of the page, then a stop. The walk explains the screen at the demo, then plays the conversation a turn at
a time, one stop an answer. At each turn it says what to watch for, from the line for that turn in
`demos/louis-agent/docs/demos.md`, and shows the turn, which waits for the guide to finish and then plays. After the
last turn it asks whether the visitor wants to try it, and on a yes asks the business for them.

A question pressed there is asked of the business, which answers in its own window on that page, and the guide keeps
the demo itself on screen while it does. The guide's rules for this are in its skills, `focus-demos` for the showroom
and `live-demo` for asking a business, and `GuideSkillsTests` holds them.

While the guide talks, a business's recorded demo holds its play, and goes on once the guide is done, so the visitor
follows one voice at a time. Each answer turns over to show how it was made and what it cost, and the bar carries the
talk's total. Each of a demo's "could also" notes is a piece a visitor presses to ask about.

*Decided in:* F134.

### 15. Every claim held to the repo, and the guide on every page

The pages are written from the docs the guide reads. Every number comes from the ledger or an eval run, and each claim
is held to the repo by a test. No page links to the repository. The landing's `PageHead` carries the site's facts for
search engines (`Content/SiteFacts.cs`).

Beside the site's name in the header stands its stage, a small "beta-1" flag. It's a quiet grey tag with a slow violet
dot before the words, which reads as a status: built, and still being polished. The dot holds still for a visitor who
asked for less motion. A press on it opens a short note, that this is version beta-1 and it works while it's still
being polished, with a way to the contact page. A press anywhere else closes it.

Orbi at the foot of the page is the guide's identity. Every layout places the same tag, `ExpertChat`, which is the
framework's widget as its bar. The bar is the mark, a box that opens out of it when the visitor or the guide needs
the other, and the site's four chips over it. So the visitor meets one guide on the site's pages and in the showroom alike. No page
has a button of its own to start a tour: the first chip asks for a walk through the page in view. The widget is titled
"Orbi · AI", and hides its work (`ShowWork="false"`). Its bar opens under the pointer on its orb, and its fourth chip
leads into the framework.

On a screen wide enough, Louis's LinkedIn stands in the screen's bottom-right corner as a small link, the framework's
`CornerLink`. The address comes from `SiteFacts`, which also tells search engines about it. The footer names the
site, links LinkedIn, Privacy and Trust, and closes with the slogan.

*Decided in:* F123, [ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md).

### 16. The slogan, and the one rule

The slogan is *The website that answers.* It sits under the brand in the header (the frame's `Tagline` slot) and in
the footer. It's the one line the site says about itself, and it says what the site is when the name doesn't. The home
page is for a small business owner, so the line is written for them, not for a developer.

The tab and the link preview speak to the same owner. The page's title is "louis-agent · the website that answers".
Its description says what it does for their business in two short sentences. A shared link shows the hero's own line,
"Your doors close at five. Your website doesn't." Developers find the framework from the framework's pages.

The startup runs on one rule, first principles, and the landing says so under the hero. Every decision starts from
what's certainly true and builds up from there, not from what everyone else does. Measure before you assume. Write
down why. Adopt what's proven, and build only what nobody has. The decision records (`src/docs/adr/`) are the rule at
work: each one says what was true, what was chosen, and why.

*Decided in:* FX79, the brand research of 2026-11-18, FX81.

### 17. The CV is data

Louis's CV is a page at `/cv`, and it's data, not markup. The page being its own editor is this site's decision. The
rule that keeps the editor out of the public pages is the framework's.

`Content/cv.json` is in the [JSON Resume](https://jsonresume.org/schema) shape, with two parts of our own: `facts`
(the row of figures) and `now` (the proof-of-work band). `Resumes/` holds its records and the strict `ResumeReader`,
so a misspelt field is an error, not a silent gap. `ResumePrivacy` names what a public page mustn't hold: an ID
number, a salary, a birth date or a street address.

*Decided in:* [ADR 0122](../adr/0122-the-sites-cv-is-edited-on-its-own-page.md),
[ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md).

### 18. Where the CV is kept

The site's host (`LouisAgent.SiteApi`, `Resumes/`) serves it at `GET v1/site/cv`, open to anyone, and keeps an
edited copy through `PUT v1/admin/cv`. That copy is `cv.json`, in the folder `SITE_DATA_DIRECTORY` names: `/data` on
the share in Azure, `src/site/data` on a PC. The built-in file is the starting copy and the fallback. A save is
refused when it doesn't read strictly, or when it holds something private.

The admin reads its copy on its own route too (`GET v1/admin/cv`), behind the same key as the save, so the editor and
its card don't open without it.

*Decided in:* FX153.

### 19. The CV's page

`Pages/About/CvPage` draws `Components/Cv/ResumeView`, with a band per part. The CV has no styles of its own, as
nothing on this site does: each part is the framework's controls given the CV's words. Who he is stands in a
`ProfileHero` beside his photo, and the few numbers in a `FactStrip`. What he builds now and his skills are in
`Panel`s, a job's stack and a skill group in a `TagList`, and the jobs and the schooling in a `Timeline`, the dates
in a column of their own.

In the browser, the page asks the API (`ApiResumeSource`). When the host draws it for a search engine, it reads the
saved file directly. That's `IResumeSource`, a Strategy picked by where the page runs.

### 20. The editor is the page

`Admin/ResumeEditor`, at `/admin/resume`, draws the very same view, with someone listening for changes. So you click
its words and retype them where they stand. Its lists move, lose and gain items, and anything the page draws as a
composed line opens a small form.

Edits go into a `ResumeDraft`, and reach the site when you Save. `Admin/Forms/OffPageForm` holds the four things the
page never draws. The area, `ResumeArea`, brings its link and its card to the framework's admin frame.

### 21. Save as PDF

`ResumePrint` opens the browser's print dialog. On paper, the CV is its desktop layout set for A4, in near-black ink,
with the page's number at each sheet's foot. The view stands in the framework's `PaperSheet`, which sets the sheet,
and each control keeps its own layout on paper beside it.

Every link becomes a QR code, the framework's `PaperLink`. The ways to reach him are a row of codes under the hero,
and a panel's link is a `PrintableLink`: the link on a screen, and its code on paper.

*Decided in:* FX154, FX191.

These are the types involved:

![The types: the site holds its words, demos and CV, on the framework's shell, its places through the places package](diagrams/LouisAgent.Site-interface.svg)

## Structure

The place to look a file up. The site is `src/site/LouisAgent.Site/`, and its host `src/site/LouisAgent.SiteApi/`.

| Path | What's in it |
|---|---|
| `Pages/` | The site's pages: the home page, contact and Work with us; under `About/`, the CV, the privacy page and the trust page; under `Framework/`, the theme's component guide; under `Agents/`, the showroom's start and, under `Showroom/`, the three businesses' demos, whose recordings are in `wwwroot/recordings/showroom/` |
| `Errors/NotFound.razor` | The page for an address the site has no page for, outside `Pages/` so the page rules don't count it |
| `Layout/` | `SiteLayout`, the site's frame on the framework's `SiteShell` and the router's default; `ShowroomLayout`, the demos' frame on `ShowroomShell`; `AdminLayout`, the admin's |
| `Components/` | `Cv/` (the CV's view, its parts and its print), `Guide/` (`ExpertChat`, the framework's `ChatWidget` with the guide, and the corner's LinkedIn), `Landing/` (the hero, who builds it, the contact strip, and under `Offer/` the bands of the offer), `Showroom/` (`SiteDemos`, a business's demo, each business's mock website in `Websites/`, and the three windows on the start in `Windows/`), `Trust/` (`FactBand`, one part of the trust page), `WorkWithUs/` (`WorkTiles`, one list of the Work with us page as tiles) |
| `Resumes/` | The CV's records by concern (`Basics/`, `History/`, `Skills/`, `Privacy/`), `ResumeReader`, `ResumeDates`, where a page gets it (`Sources/`) and the editor's draft (`Drafts/`) |
| `Admin/` | The site's own area of the admin: `ResumeArea`, its link and card, `ResumeEditor` at `/admin/resume`, the page inside it (`ResumeSheet`) and `Forms/OffPageForm`; outside `Pages/`, so the page rules don't count it |
| `Content/` | `SiteFacts`, the site's facts for search engines; `Cv` and `cv.json`, the CV the site is built with; `Trust/` (`TrustFact`, `TrustFacts`, `DataHandlers`), what the trust page says; `WorkWithUs/` (`WorkWithUsWords`, `WorkTile`, `PaymentShare`, and their parts' enums in `Parts/`), what the Work with us page says |
| `LouisAgentDevAreas.cs` | The site's areas: the showroom, the focuses, the guide's chips, the bar opening under the pointer, the admin and the CV's area, its places, and glass and flat |
| `Styles/app.scss` | The framework's Sass, the two themes and the paper sheets ([ADR 0085](../adr/0085-styles-are-written-in-sass.md)); the build makes `wwwroot/css/app.css` |
| `wwwroot/` | `index.html` with its link card, the brand, the icons, the showroom's photos, the recordings, `sitemap.xml` and `robots.txt`; the CSS is built, and isn't in git |
| `LouisAgent.SiteApi/Program.cs` | The host |
| `LouisAgent.SiteApi/SiteCatalog.cs` | What the host runs: the guide and the three businesses, on the docs, leads and site packs |
| `LouisAgent.SiteApi/SitePageServices.cs` | What a page the host draws asks for |
| `LouisAgent.SiteApi/MovedPages.cs` | The two old addresses and where they're sent |
| `LouisAgent.SiteApi/Resumes/` | The CV's store and its routes, and the source a page drawn on the host reads |
| `src/site/recordings/` | `record-showroom.sh`, which records the three demos, and `pages/`, the places each was sent |
| `docker/Dockerfile`, `docker/docker-compose.yml` | The image, the `site` service, and the `sessions-mongo` service its chats are kept in |
| `deploy/azure/louis-agent-app.yaml` | The Container App louis-agent.dev runs |
| `deploy/azure/deploy.ps1` | The script that builds, releases and watches both sites' apps in Azure |

## Configuration

The host reads the API's settings ([Setup](../SETUP.md)) from the site's `config/` folder: `AGENT_API_KEY`, and
`ANTHROPIC_API_KEY` so the guide can answer, from `.env.secrets`, and anything optional from `.env`. In the compose
file the model comes from `.env.anthropic`. These are the site's own:

| Setting | What it does |
|---|---|
| `OPEN_IDENTITIES` | Who answers a visitor with no key: the guide and the three businesses |
| `OPEN_SESSIONS_PER_DAY` | How many sessions one address opens a day across them: 50 in the compose file and the run profile, 20 in Azure |
| `AGENT_PEERS` | Where the guide asks a business: the host itself, `self=` its own address |
| `SITE_DATA_DIRECTORY` | Where the saved CV is kept |
| `SESSION_STORE`, `SESSION_STORE_URL` | Where the chats are kept: `mongo` on the compose's `sessions-mongo` in the compose file, memory in the run profile |
| `LEADS_TO`, `LEADS_FROM` | Where the guide's lead mail goes and who it's from; with `RESEND_API_KEY` it's sent, without it the lead goes to the log |
| `HOST_TIME_ZONE`, `HOST_CULTURE` | Africa/Johannesburg and en-ZA in the compose file, so costs show in rand |
| `DOCS_ROOT` | Where the docs the guide reads are: the repo in the run profile, `/app/docs` in the image |
| `SITE_PORT` | The review copy's port, 5080 by default |

## Extending it

A new page goes under `Pages/`, built from the framework's controls given the site's words, with its own heading and
its sections as `Band`s, so its places get addresses. A part the site needs is named among its areas in
`LouisAgentDevAreas`. A look of the site's own doesn't go in `Styles/`: the site keeps to the themes. The how-to is
[Change the site](../guides/change-the-site.md).

## Tests

The tests are in `src/tests/site/LouisAgent.Site.Tests`, on NUnit with bUnit.

`Pages/*Tests` render the pages and hold them to their addresses, headings and named sections. `SiteLinksTests` holds
every link on the site to a place on it, `Pages/Meta/` each page's title, description, heading and the sitemap, and
`Pages/Claims/WhatIsTrueTests` what the pages claim. `Showroom/*Tests` hold the three businesses' demos, their
recordings and their identities, and `Layout/*Tests` the frames and the router.

`Components/Guide/GuideKnowsTheSiteTests` holds the guide's voice to every page, and holds this doc to the offer in
part 9. `Guide/*Tests` hold the guide's identity, skills, focuses and bar. `Styles/StylesheetTests` reads what the
build made. `Host/SiteCatalogTests` and `Host/SiteImageTests` hold the host and its image, `Host/MovedPagesTests` the
two old addresses sent on, and `Host/PagesForACrawlerTests` every page drawn for a search engine.

The CV has its own: `Resumes/*Tests` (the strict read, the dates, what's private, the draft), `Components/Cv/` (the
view's golden file, and `Editing/` for every change made on the page), `Admin/*Tests` (the area, and the editor
against a stub of the API), and `Host/Resume*Tests` (the store and the two routes).

The framework's site rules, in `LouisAgent.Web.Tests` under `Rules/`, hold this site like any other, and
`SiteBoundaryTests` in core's tests holds that nothing else in `src/` references it.

## Limits and plans

The films are no longer on the site. `src/tools/LouisAgent.FilmExport` and its exports in `src/docs/films/` still
name the old pages, `/framework/architecture` and `/framework/how-it-works`, and the films' files, which are gone. So
the tool can't make an export until it's fixed.

## Related docs

[LouisAgent.Identities.Guide](LouisAgent.Identities.Guide.md) · [LouisAgent.Identities.Showroom](LouisAgent.Identities.Showroom.md) ·
[LouisAgent.Web](LouisAgent.Web.md) · [LouisAgent.SiteFramework](LouisAgent.SiteFramework.md) ·
[Change the site](../guides/change-the-site.md) ·
[the coding agent's app](../../../demos/louis-agent/docs/projects/LouisAgent.CodeReview.md)

---
[Projects](README.md) · [Docs index](../README.md)
