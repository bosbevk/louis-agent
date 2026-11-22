# LouisAgent.Identities.Showroom — design

> **Explanation.** Who the site's showroom agents are: three made-up businesses' agents for the demo pages, and how
> each answers and passes a booking on. **Elsewhere:** the tool they pass it on with in
> [LouisAgent.Packs.Leads](../../../packs/docs/projects/LouisAgent.Packs.Leads.md); the site they show on in
> [LouisAgent.Site](LouisAgent.Site.md); the decisions in [ADR 0112](../adr/0112-the-site-sells-first.md) and
> [ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md).
> **Also shows:** `activity`, `sequence`

> **Kind:** contracts library (`net10.0`, an identity's prompts) · **Path:** `src/site/LouisAgent.Identities.Showroom/` · **References:**
> `LouisAgent.Composition` only, and the embedded file provider package · **Used by:** `LouisAgent.SiteApi` (the site's host), behind its key

![The showroom's agents: a guesthouse, a restaurant and a law practice, on the leads and the site packs](diagrams/LouisAgent.Identities.Showroom.svg)

## Purpose

A business owner gets what a website with an agent is best by watching one answer a business like theirs. So the
showroom has three made-up businesses: a restaurant, a law practice and a guesthouse. The guesthouse is there
because the law practice already shows an agent that never gives advice.

The rest of this doc follows one of those agents: the base all three share, what each voice knows, how it shows its
website while it talks, how a demonstration says so, how the recordings are made, and how a visitor tries one live.
It ends with the files, to look things up, and the tests.

Each agent was recorded once, answering a customer and passing a booking on. The site's showroom plays those
recordings on a mock of each business's own website. Nothing runs while a visitor watches. Once a recording ends,
the visitor can try that agent live.

*Decided in:* F128.2, F127.1, F127.2.

## How it works

### 1. Three businesses on one base

All three share one base, `ShowroomIdentity`. It takes its name from `ShowroomIdentityName`, its voice from
`{name}/voice`, and it loads two packs. The `leads` pack passes a booking on. The `site` pack lets the agent show a
place on the business's website, with the same `ShowPage` the guide uses. In other words, the agent can move around
the website.

On top of that base, each business only says which one it is. `RestaurantIdentity` is Wheatfield Kitchen, in
Malmesbury. `LawPracticeIdentity` is Koringland Attorneys, also in Malmesbury. `GuesthouseIdentity` is Seemeeu
Guesthouse, in Yzerfontein.

The three share one project, and one provider over it: each business's voice sits at `agents/{name}/voice.md`, named
once, and all three identities hand the catalogue the same provider. A prompt's path has one home in a catalogue, and
the catalogue reads a provider it is given twice only once, so the three voices never clash.

*Decided in:* F127.1, CL68.1.

### 2. What each voice knows

Each business's voice lives in `agents/{name}/voice.md`. It holds the business's made-up facts: its hours,
services and prices. It says how to answer: three short sentences, in proper English or Afrikaans, with a greeting
only once.

It also says how to pass a booking on. The agent gets the customer's name, how to reach them and what they want. It
shows that back as a card, marked *Not sent yet*, and offers *Book it*, *Change something* and *Not now*. Only when
*Book it* comes back does it call `SendLead`, once, and then it shows the card again, marked *Sent*. When asked, each
one says it's an AI and that the business is a demonstration on louis-agent.dev.

Each has its own lines it won't cross. The law practice never gives advice: not what the law says, not what someone
should do, not their chances. When something is urgent, it offers to have an attorney call back. The guesthouse never promises a room it
can't see, and it knows dogs stay in the garden room.

### 3. Showing the website while it talks

The voice's *Showing your website* part says: first open the place that answers the question, then answer about
what the customer is looking at. Think "first show them the room, and then they book it". It's one place per
answer.

Each agent knows its website's three pages: the restaurant's Home, Menu and Visit, the guesthouse's Home, Rooms and
Stay, and the law practice's Home, What we do and Consultations, plus its Wills and estates page. It opens the exact place rather than a whole page, moves to a place on
another page when the talk moves there, and opens the booking place in the answer that asks for a booking. The law
practice's agent knows what its Wills and estates page says, and may open a part of it, but never applies it to
anyone's own case.

### 4. A demonstration says so

Each agent is a demonstration (`IsDemonstration`). So a booking someone tries reaches Louis marked `[demo]`. The
agent hears it was a demonstration, and tells the visitor so.

*Decided in:* F127.2, [ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md).

### 5. How the recordings are made

`src/site/recordings/record-showroom.sh` records the three. Its first question carries the business's website,
just like the guide's widget sends the page that's in view. That website is
`src/site/recordings/pages/{name}.json`: its words, and its places by their addresses on the mock's site (such as
`/rooms/garden`), with each page marked `(page)` before its places.

The agent learns its places from that page. When a recording plays, its `ShowPage` calls take the mock to those
places. Each recording moves between pages and sections. `ONLY=business` records just one business, and
`SEND_MAIL=1` lets its booking really mail the address in `LEADS_TO`, once.

### 6. Trying them live, and the guide asking them

The site's host lists the three after the guide, Louis's agent, so the guide stays the default
(`SiteCatalog`). The door also opens them to visitors (`OPEN_IDENTITIES`), so a visitor can try a business's agent live, under the same daily cap as the guide.

The guide can ask one of them on a visitor's behalf, such as `AskAgent("seemeeu-guesthouse", …)`. The business then
opens one level down, in its own voice, with its own packs, and with nothing it could ask further. That's because a
role's level comes from the folder its file is in, not from who's asking.

The leads pack's texts don't name an owner. So the very same tool that sends Louis a lead sends a practice its
booking request.

*Decided in:* F127.2, F127.3.1.

![How it works: recorded once and tried live after, each business on the leads and site packs](diagrams/LouisAgent.Identities.Showroom-flow.svg)

![A booking at the guesthouse: the agent shows the room, shows the booking back on a card, and sends it only after Book it](diagrams/LouisAgent.Identities.Showroom-sequence.svg)

These are the types involved:

![The types: one abstract base and three businesses, each saying only which it is](diagrams/LouisAgent.Identities.Showroom-interface.svg)

## Structure

The place to look a file up. Paths are from `src/site/LouisAgent.Identities.Showroom/`.

| Path | What's in it |
|---|---|
| `ShowroomIdentity.cs` | The base: the name, the voice, its own prompts, the two packs, and `IsDemonstration` |
| `Businesses.cs` | `RestaurantIdentity`, `LawPracticeIdentity` and `GuesthouseIdentity`, one line each |
| `ShowroomNames.cs` | The two enums: `ShowroomIdentityName` (the three businesses) and `ShowroomPack` (leads, site) |
| `agents/{name}/voice.md` | Each business's voice: its facts, how it answers, how it shows its website and passes a booking on |
| `src/site/recordings/` | `record-showroom.sh`, and each business's website in `pages/{name}.json` |

## Tests

The tests are in `src/tests/site/LouisAgent.Site.Tests/`.

`ShowroomIdentitiesTests`, in `Showroom/`, holds each business's name, voice and packs. It checks that every voice
keeps the honesty rule, the yes-first rule and the show-first rule, that the practices never advise, and the order
of the site's catalogue.

`ShowroomPagesTests` holds each recorded page's places to the mock's, and every price and time on a mock to its
voice. `SiteCatalogTests` checks that no identity on the site reaches a pack the guide doesn't load.
`ShowroomRecordingsTests` holds each recording's places to its website's, across two pages or more, with a section
or a card. And `BusinessDemoTests` covers *Try it yourself*, the swap, the cues waiting and the way back.

---
[Docs index](../README.md) · Related: [LouisAgent.Site](LouisAgent.Site.md) ·
[LouisAgent.Identities.Guide](LouisAgent.Identities.Guide.md)
