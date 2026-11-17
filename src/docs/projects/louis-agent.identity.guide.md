# louis-agent.identity.guide — design

> **Explanation.** Who the framework's guide is, the guide in its website's corner, and how it answers. **Elsewhere:**
> the pack it reads with in [louis-agent.pack.docs](../../../packs/docs/projects/louis-agent.pack.docs.md); the widget
> that shows it in [louis-agent.web](louis-agent.web.md); the site it sits on in [louis-agent.site](louis-agent.site.md);
> the decisions in [ADR 0099](../adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md),
> [ADR 0100](../adr/0100-the-site-tells-the-whole-architecture.md) and
> [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md).
> **Also shows:** `activity`, `sequence`

> **Kind:** contracts library (`net10.0`, an identity's prompts) · **Path:** `src/site/louis-agent.identity.guide/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.site-api` (the site's host), and the coding agent's
> catalogue beside the coding identity

![The guide, and its three packs: the docs, the leads and the site](diagrams/louis-agent.identity.guide.svg)

## Purpose

The framework's website has a chat in its lower-right corner. Whoever answers there has to know the framework the
way its docs tell it, and nothing else: no files, no git, no shell. That's this identity, `louis-agent-guide`. It's
one voice with three packs.

It belongs to the framework, so it lives beside its site in `src/site/`. Its name says what it is. The site's host
runs it alone, so a visitor can't reach anything else.

*Decided in:* F109.3, F124.

## How it works

### 1. One voice, three packs

`GuideIdentity` holds the guide's name, its default voice (`louis-agent-guide/voice`) and its three packs. The docs
pack is what it answers from. The leads pack has one tool, which mails Louis a lead when a visitor is interested and
has said yes. The site pack has one tool too, which takes the visitor to a page or a part of one; the widget hears
it and moves the browser.

*Decided in:* F112, [ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md), F117.

### 2. Who it is

The voice lives in `prompts/agent/louis-agent-guide/voice.md`. The guide is Louis: the AI version of the man who made
louis-agent. It tells his story as "I": Malmesbury, Yzerfontein, the PC at 10, .NET for twenty years, four kids, the
outdoors, a stoic who reads Kierkegaard. It talks like him, plain, dry and at home, with an idiom where it helps.

The widget's title reads "Louis · AI". The guide says it's the AI the moment that matters, and never adds to his
story. It leaves the man's own work to the real one: the call, the lead, and a meeting in Cape Town and surrounds.

*Decided in:* F128.1.

### 3. How it teaches and sells

It teaches one thing with every answer. It sells by showing, with one nudge at the end, and it's honest always.

It's a salesperson that reads the person. Someone who has never written code gets plain words, with no code or file
names. Once it knows it's talking to a software developer, it gives the full technical answer.

It knows what we sell today: web hosting with an AI agent for small businesses, at R500 to R1,000 a month. That's the
one public price. And it can say what an agent does for a business, in the owner's own words.

*Decided in:* FX72.

### 4. How it answers

It reads before it speaks. It names the doc it used, but only for a developer. It says so when the docs don't cover
something. It only gives numbers that come from a doc, uses plain words, and only talks about what works today.

Some things stay Louis's. It never quotes the roadmap's money, numbers or risks, or the TODO's notes. Any other
price question goes to the contact page.

*Decided in:* FX77.

### 5. Its skills

Its skills sit beside the voice, in `skills/`. One is always in the prompt: `site-pages.md`, an index of every page,
one line each, so it knows which page to open.

Two more it only reads with `ReadSkill` when a question needs them. `docs-map.md` says where things are in the docs
root, so a search goes to the right folder. `site-map.md` says what each page claims and which docs it was written
from, so a question about a page's claim goes to its source. Splitting those two off made its fixed prefix about 14%
smaller, measured live.

*Decided in:* F129, [ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md), F109.4.

### 6. One guide, a focus for each part of the site

louis-agent.dev names which part of the site each page belongs to (`LouisAgentDevAreas`). Selling is the default.
`/demos` and every demo are the demos part. Everything under `/framework`, and `/csharp-ai-agent-framework`, is the
framework part. These parts are `GuideFocus`.

Each message's page block says which part the visitor is in. The voice reads that part's on-demand skill
(`focus-selling`, `focus-demos` or `focus-framework`) the first time a part shows up, and again whenever it changes.

In the selling part, it sells honestly by showing. It gives first, asks for small yeses, puts the visitor's own
business in the live demo, never fakes a hurry, and guides them to the demos. In the demos part, it guides them
through the showroom. In the framework part, it answers technically, names its docs, and says a diagram drawn in
the chat is planned.

*Decided in:* F127.7, [ADR 0121](../adr/0121-every-part-of-the-web-is-ready-for-an-agent.md).

### 7. A piece the visitor picked

Sometimes a message says the visitor selected a piece: a tile or a card they clicked, or one the guide showed. Then
"this" means that piece. The site toolset's guide has it explain the piece and offer what goes deeper. For anything
on another page, it calls `FindPlace` first, then `ShowPage` with the address it got back.

*Decided in:* F127.7.

### 8. Where its knowledge comes from

Everything it knows comes from the `docs` toolset, over `DOCS_ROOT`. The site's image carries the docs, and on the PC
the repo's docs are mounted. So an answer can always name the doc it read. The guide is an identity, not a role, and
it reads the docs with a tool, not from an index or a digest.

### 9. Asking the showroom's businesses

It may ask the three showroom businesses. Its `Calls` are `GuideCall`'s: `seemeeu-guesthouse`,
`wheatfield-kitchen` and `koringland-attorneys`. The door gives its session core's `agents` toolset for this, and its
prompt lists them after the agents guide.

It asks with `AskAgent`, on this same container (`AGENT_PEERS: self=http://localhost:8080`). It passes the answer on
as the business's, says what the business did from the line under it, and reads its steps with `ReadAgentWork` when a
visitor asks. It keeps the last word, so it doesn't relay answers whole (`RelaysAnswersWhole` stays off).

*Decided in:* F127.3.1, [ADR 0117](../adr/0117-one-address-for-every-agent-call.md).

### 10. Running a demo live

Its `live-demo` skill is always loaded. On a demo page, it offers *Ask it live* with `Offer`, and asks the business
in the visitor's own words. The page shows that talk in the business's own window, where the visitor carries on in
the same session. The guide's own chat says so in one line, and on a phone it folds away so the window shows.

If the visitor asks what happened, it retells it from the line under the answer, or from `ReadAgentWork`. That reads
the business's whole session, including the turns in the window.

A business has that window only on its own demo page. The showroom's start shows each demo as a window on a stage,
and such a window is a picture of the demo, so no business answers there. So on the start the guide never offers
*Ask it live*. Walking the start, it stops at each business's window in turn and offers *Open this demo* there,
which is the suggested choice at the last window. Its `focus-demos` skill has this.

Chosen, it takes the visitor to the demo's own page with `ShowPage` and meets them at the door with exactly four
choices: two questions to put to the business, a walk of the page, and a stop. It never calls `AskAgent` on any
page but the business's own demo page, and in an answer where it does ask, the only place it shows is the demo
itself, so the business's window stays on the visitor's screen while it answers.

On a demo page the business's recorded conversation waits to be started, and the guide is the one who starts it, a
turn at a time. Its walk goes in the page's order. At the demo itself it says in two short sentences what is on the
screen: the business's made-up website, and its agent's window, which plays a real conversation recorded once.
Then each turn is a stop of its own. The guide first says in two short sentences what to watch for, from that
turn's line in `demos/louis-agent/docs/demos.md`, and then shows the turn with `ShowPage`. Showing a turn plays it,
once the guide has finished talking, and the play stops after it. The stop's choices are the next turn, Ask it
live, another section or Stop here. After the last turn the guide asks whether the visitor wants to try it, and on
a yes offers questions and asks the business, so the same window becomes a live chat. The `live-demo` skill has
these rules, and `GuideSkillsTests` holds them.

*Decided in:* F127.4.2, F134, F160.

![How it works: one voice, three packs and three businesses to ask, its knowledge only the docs it reads](diagrams/louis-agent.identity.guide-flow.svg)

![One question answered: it searches the docs, reads the best section, opens the page and answers](diagrams/louis-agent.identity.guide-sequence.svg)

These are the types involved:

![The types: GuideIdentity and three enums, its name, its packs and the businesses it may ask](diagrams/louis-agent.identity.guide-interface.svg)

## Open on purpose

The guide is more open than most agents on business sites (Louis, 2026-11-09: "quite open if compared to how other
online agents is configured, but that is on purpose"). Many site agents follow a script or a short list of topics,
turn away anything off it, and never say what they read. This one needs no sign-in (ADR 0099), follows the visitor's
own questions across the whole framework, reads the docs and says where an answer came from, gives a developer the
full technical answer, and says plainly when the docs don't cover something. That is the point: the site sells agents,
and an agent that answers freely and honestly is the demo.

What stays closed is held in code, not by narrowing what it talks about. Its host loads only its three packs, so it has
no files, git or shell. It sends a lead only on the visitor's yes. It never quotes Louis's own money, numbers, risks or
notes. A cap limits what a visitor can spend (ADR 0099).

## Tests

The tests are in `src/tests/site/louis-agent.site.tests`. `GuideIdentityTests` and `SiteCatalogTests` hold its
name, its voice's path, its three packs, and the site's catalogue to the guide alone. `GuideKnowsTheSiteTests` holds
the voice and its skills to every page of the site. `GuideSkillsTests` holds the split: every page in the index
that's always loaded, and the two maps read on demand. And `CodingCatalogTests` holds it beside the coding identity,
in the coding agent's catalogue.

---
[Docs index](../README.md) · Related: [louis-agent.site](louis-agent.site.md)
