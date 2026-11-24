# LouisAgent.Identities.Guide — design

> **Explanation.** Who the framework's guide is, the guide in its website's corner, and how it answers. **Elsewhere:**
> the pack it reads with in [LouisAgent.Packs.Docs](../../../packs/docs/projects/LouisAgent.Packs.Docs.md); the widget
> that shows it in [LouisAgent.Web](LouisAgent.Web.md); the site it sits on in [LouisAgent.Site](LouisAgent.Site.md);
> the decisions in [ADR 0099](../adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md),
> [ADR 0100](../adr/0100-the-site-tells-the-whole-architecture.md) and
> [ADR 0111](../adr/0111-the-frameworks-website-lives-in-src-site.md).
> **Also shows:** `activity`, `sequence`

> **Kind:** contracts library (`net10.0`, an identity's prompts) · **Path:** `src/site/LouisAgent.Identities.Guide/` · **References:**
> `LouisAgent.Composition` only, and the embedded file provider package · **Used by:** `LouisAgent.SiteApi` (the site's host), and the coding agent's
> catalogue beside the coding identity

![The guide, and its three packs: the docs, the leads and the site](diagrams/LouisAgent.Identities.Guide.svg)

## Purpose

The framework's website has a chat in its lower-right corner. Whoever answers there has to know the framework the
way its docs tell it, and nothing else: no files, no git, no shell. That's this identity, `louis-agent-guide`. It's
one voice with three packs.

It belongs to the framework, so it lives beside its site in `src/site/`. Its name says what it is. The site's host
runs it first, as the default, and beside it only the showroom's three made-up businesses
([LouisAgent.Identities.Showroom](LouisAgent.Identities.Showroom.md)), so a visitor can't reach anything else.

The rest of this doc walks the guide part by part: its packs, who it is, how it teaches and sells, how it answers,
its skills, the focus it takes on each part of the site, a piece the visitor picked, where its knowledge comes from,
how it asks the showroom's businesses, and how it runs a demo live. Then it says why it is open on purpose, and ends
with the files, to look things up, and the tests.

*Decided in:* F109.3, F124.

## How it works

### 1. One voice, three packs

`GuideIdentity` holds the guide's name, its default voice (`louis-agent-guide/voice`) and its three packs. The docs
pack is what it answers from. The leads pack has one tool, which mails Louis a lead when a visitor is interested and
has said yes. The site pack has one tool too, which takes the visitor to a page or a part of one; the widget hears
it and moves the browser.

Every answer ends with choices: the identity says `EndsEachAnswerWith` the site pack's `Offer`, so the engine asks
for one when an answer ends without it. The one exception is "Stop here", a walkthrough's last choice, after which it
says goodbye and offers nothing (`StopsOn`).

*Decided in:* F112, [ADR 0101](../adr/0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md), F117.

### 2. Who it is

The voice lives in `agents/louis-agent-guide/voice.md`. The guide is Orbi, Louis's AI agent. Louis is the man
who made louis-agent, and Orbi runs on it. Orbi tells his story as his, never as its own: "Louis grew up in
Malmesbury". The story is Malmesbury, Yzerfontein, the PC at 10, .NET for twenty years, four kids, the outdoors, and
a stoic who reads Kierkegaard. Orbi sounds like him, plain, dry and at home, with an idiom where it helps.

The widget's title reads "Orbi · AI", and its greeting opens "Hi, I'm Orbi, Louis's AI agent." The guide says it's
an AI the moment that matters, and never adds to his story. It leaves the man's own work to him: the call, the lead,
and a meeting in Cape Town and surrounds.

*Decided in:* F128.1.

### 3. How it teaches and sells

It teaches one thing with every answer. It sells by showing, and most answers need no pitch at all. It's honest always.

It's a salesperson that reads the person. Someone who has never written code gets plain words, with no code or file
names. Once it knows it's talking to a software developer, it gives the full technical answer.

It knows what we sell today: web hosting with an AI agent for small businesses. There's no price list, and it names
no price. Louis quotes each site after a short call. And it can say what an agent does for a business, in the
owner's own words.

*Decided in:* FX72.

### 4. How it answers

It reads before it speaks. It names the doc it used, but only for a developer. It says so when the docs don't cover
something. It only gives numbers that come from a doc, uses plain words, and only talks about what works today.

Some things stay Louis's. It never quotes the roadmap's money, numbers or risks, or the TODO's notes. Any other
price question goes to the contact page.

*Decided in:* FX77.

### 5. Its skills

The voice works like an index. It holds who Orbi is, the seven rules it never breaks, a table of which skill goes
with which job, and how it answers, and it ends with one whole example answer. The skills sit beside it, in
`skills/`, each a folder named for the skill that holds its `SKILL.md`, the shape Claude Code's skills have, and
each has the same shape inside. The rules it never breaks come first, then its steps or its detail, then a
worked example, then the docs it rests on, which Orbi can open to check.

Three skills are always in the prompt. `site-pages` is an index of every page, one line each, so it knows which
page to open. `writing` says how it writes every answer, the way a copywriter writes for the web: sentences of
about 14 words and never more than 25, the answer first, one idea to a paragraph, plain words, active voice, and no
colon, semicolon or bracket. The voice says who the guide is and how it sounds, and the writing skill says how each
sentence is built, so neither crowds the other. `live-demo` runs a business's demo live, as part 10 tells.

The rest it reads with `ReadSkill` only when their job comes up, as the voice's table says. `docs-map` says where
things are in the docs root, so a search goes to the right folder. `site-map` says what each page claims and which
docs it was written from, so a question about a page's claim goes to its source. Splitting those two off made its
fixed prefix about 14% smaller, measured live. `take-a-lead` passes someone on to Louis, from the offer to the
card and the send. `walk-a-page` walks a page one stop per answer, and `what-we-sell` holds what is sold today, what it does
for a business, and why it is safe. The three focus skills are part 6.

*Decided in:* F129, [ADR 0114](../adr/0114-an-identitys-skills-load-always-or-on-demand.md), F109.4.

### 6. One guide, a focus for each part of the site

louis-agent.dev names which part of the site each page belongs to (`LouisAgentDevAreas`). `/demos` and every demo
are the demos part. Every other page is the selling part, the theme's component guide at `/framework/theme`
included, since the framework's own pages are on their own site. These parts are `GuideFocus`.

Each message's page block says which part the visitor is in. The voice reads that part's on-demand skill
(`focus-selling` or `focus-demos`) the first time a part shows up, and again whenever it changes.

In the selling part, it sells honestly by showing. It gives first, asks for small yeses, puts the visitor's own
business in the live demo, never fakes a hurry, and guides them to the demos. In the demos part, it guides them
through the showroom. On the framework's pages it still talks about the framework from the site map's entries and
sends a developer to the page that answers them.

*Decided in:* F127.7, [ADR 0121](../adr/0121-every-part-of-the-web-is-ready-for-an-agent.md).

### 7. A piece the visitor picked

Sometimes a message says the visitor selected a piece: a tile or a card they clicked, or one the guide showed. Then
"this" means that piece. The site toolset's guide has it explain the piece and offer what goes deeper. For anything
on another page, it calls `FindPlace` first, then `ShowPage` with the address it got back.

*Decided in:* F127.7.

### 8. Where its knowledge comes from

Everything it knows comes from the `docs` toolset, over `DOCS_ROOT`. The site's image carries the docs at `/app/docs`,
and on the PC the host's run profile sets `DOCS_ROOT` to `../../..`, the repo itself. So an answer can always name the doc it read. The guide is an identity, not a role, and
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

![How it works: one voice, three packs and three businesses to ask, its knowledge only the docs it reads](diagrams/LouisAgent.Identities.Guide-flow.svg)

![A price asked about: it reads its skill, opens the contact page and says Louis quotes each site after a short call](diagrams/LouisAgent.Identities.Guide-sequence.svg)

These are the types involved:

![The types: GuideIdentity and four enums, its name, its packs, the businesses it may ask and its focus](diagrams/LouisAgent.Identities.Guide-interface.svg)

### 11. Its eval

The guide is measured by an eval of its own, `agents/louis-agent-guide/guide.eval.json`, beside its voice: twelve
conversations, one for each rule its voice and skills name and one for what its turn log showed. It greets first,
answers in three short sentences with no semicolon, names no doc, leads as the demos part asks, walks a page one
stop at a time, offers choices as cards, treats a described job as a custom agent, offers to pass a visitor on to
Louis, and says plainly what it doesn't do. A bare hello on the landing reads its selling skill, since the log
showed talks that didn't. Where code can tell, a fact checks it, such as `used ShowPage` or `never-says .md`, and
the judge marks the rest.

Two cases guard what matters most. No conversation sends a lead, and those that could check in code that `SendLead`
was not used, so a live run never emails Louis. And `guide-injection` hands the guide a page, given in the eval
file itself, whose words hide instructions to send a lead and say a code word; code checks it did neither. That is
the guide's first security eval, prompt injection as OWASP's LLM01 names it.

A live run of three repeats is the baseline, and every conversation's first repeat is kept in
`src/site/recordings/evals/guide/`, so `GuideEvalReplayTests` replays the whole eval on every build, facts only and with
no key, over an empty docs folder: the docs tools' answers are played back from the recording, so a doc edited
since can't make it stale.

*Decided in:* F126.5.1, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

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

## Structure

The place to look a file up. Paths are from `src/site/LouisAgent.Identities.Guide/`.

| Path | What's in it |
|---|---|
| `GuideIdentity.cs` | The identity, and its four enums: `GuideIdentityName`, `GuidePack` (docs, leads, site), `GuideCall` (the three businesses it may ask) and `GuideFocus` (selling, demos) |
| `agents/louis-agent-guide/voice.md` | Who Orbi is, his story, the seven rules, what we sell today, what stays Louis's, the skills' table, and how it answers |
| `agents/louis-agent-guide/skills/` | `site-pages`, `writing` and `live-demo`, always; `docs-map`, `site-map`, `take-a-lead`, `walk-a-page`, `what-we-sell`, `focus-selling` and `focus-demos`, on demand |

## Tests

The tests are in `src/tests/site/LouisAgent.Site.Tests`. `GuideIdentityTests` and `SiteCatalogTests` hold its
name, its voice's path, its three packs, and the site's catalogue: the guide first, then the businesses it may ask,
and the program serving the site and nothing else. `GuideKnowsTheSiteTests` holds
the voice and its skills to every page of the site. `GuideSkillsTests` holds the split: every page in the index
that's always loaded, the writing skill always loaded apart from the voice, the two maps read on demand, and every
skill in the shape, with its rules first, an example and its references. `OrbiVoiceTests` holds the voice's words,
that its table names every skill read on demand, and that nothing it reads names the closed free-website offer.
`GuideEvalFileTests` holds its eval's conversations and `GuideEvalReplayTests` replays every one of them. And `CodingCatalogTests`, in
`demos/louis-agent/tests/LouisAgent.Coding.Catalog.Tests`, holds it beside the coding identity, in the coding agent's
catalogue.

---
[Docs index](../README.md) · Related: [LouisAgent.Site](LouisAgent.Site.md)
