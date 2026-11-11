# louis-agent.identity.showroom — design

> **Explanation.** Who the site's showroom agents are: three made-up businesses' agents for the demo pages, and how
> each answers and passes a booking on. **Elsewhere:** the tool they pass it on with in
> [louis-agent.pack.leads](../../../packs/docs/projects/louis-agent.pack.leads.md); the site they show on in
> [louis-agent.site](louis-agent.site.md); the decisions in [ADR 0112](../adr/0112-the-site-sells-first.md) and
> [ADR 0113](../adr/0113-the-guide-is-louis-and-says-it-is-the-ai.md).

> **Kind:** Class library (`net10.0`) · **Path:** `src/site/louis-agent.identity.showroom/` · **References:**
> `louis-agent.composition` only · **Used by:** `louis-agent.site-api` (the site's host), behind its key

## Purpose

A business owner understands a website with an agent best by watching one answer a business like theirs (F128.2,
Louis's pick 4A: "not a bakery, say a laywers practice, restaurant, and a doctors practice"; since F127.1 a
guesthouse in the doctor's place: "we should just change doctor to something else", since the law practice already
shows an agent that never advises). These are three made-up businesses' agents, recorded once each answering a
customer and passing a booking on, and played in the site's showroom on a mock of each business's own website.
Nothing runs for a visitor who watches; since F127.2 a visitor can try each agent live once its recording ends.

## How it works

- `ShowroomIdentity`: the base the three share, its name from `ShowroomIdentityName`, its voice
  `{name}/voice`, and two packs, `leads` to pass a booking on and `site` to show a place on the business's website
  (F127.1, Louis: "the agent can navigate the website basically"), the same `ShowPage` the guide uses.
  `RestaurantIdentity` (Wheatfield Kitchen), `LawPracticeIdentity` (Koringland Attorneys) and `GuesthouseIdentity`
  (Seemeeu Guesthouse, in Yzerfontein) each say only which they are.
- `prompts/agent/{name}/voice.md`: the business's made-up facts (hours, services, prices), how it answers (three
  short sentences, proper English or Afrikaans, a greeting once), and how it passes a booking on: name, contact and
  what they want, repeated back, and `SendLead` only after the customer's yes. Each says it is an AI and a made-up
  business when asked. The law practice never gives advice; the guesthouse never promises a room it can't see, and
  knows dogs stay in the garden room. *Showing your website* says to open the place that answers a question first,
  then answer about what the customer sees ("first show them the room and then they book it"), one place an answer;
  since F127.2 it knows its website's three pages: the exact place rather than its whole page, a place on another
  page when the talk moves there, and the booking place in the answer that asks for a booking. The law practice's
  knows what its Wills and estates page says, and may open a part of it, never apply it to anyone's case.
- Each is a demonstration (`IsDemonstration`, F127.2), so a booking tried on one reaches Louis marked `[demo]`, and
  the agent hears it was a demonstration and says so
  ([ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md)).
- `src/site/recordings/record-showroom.sh` records the three, its first question carrying the business's website
  as the guide's widget sends the page in view: `src/site/recordings/pages/{name}.json`, its words and its places by
  their addresses on the mock's site (`/rooms/garden`), each page marked `(page)` before its places. The agent learns
  its places from that page, and the recording's `ShowPage` calls take the mock there when it plays. `ONLY=business`
  records one business, and `SEND_MAIL=1` lets its booking mail Louis; the recordings were made again in F127.2, so
  each moves between pages and sections.
- The site's host lists them after Louis, so he stays the default. Since F127.2 the door opens them to visitors too
  (`OPEN_IDENTITIES`), so a visitor can try a business's agent live, under the same daily cap as the guide. Since
  F127.3.1 the guide can ask one for a visitor (`AskAgent("seemeeu-guesthouse", …)`): the business opens one level
  down in its own voice, with its own packs and nothing to ask further with, since a role's level comes from its
  file's folder, not from who asks. The
  leads pack's texts name no owner, so the same tool that sends Louis a lead sends a practice its booking request.

## Tests

`ShowroomIdentitiesTests` in `src/tests/site/louis-agent.site.tests/Showroom/` holds each business's name, voice
and packs, the honesty, yes-first and show-first rules in every voice, the practices' never-advise rules, and the
site's catalogue order; `ShowroomPagesTests` holds each recorded page's places to the mock's, and every price and time
on a mock to its voice; `SiteCatalogTests` holds that no identity on the site reaches a pack the guide doesn't load;
`ShowroomRecordingsTests` holds each recording's places to its website's, on two pages or more with a section or a
card; `BusinessDemoTests` holds Try it yourself, the swap, the cues waiting and the way back.

---
[Docs index](../README.md) · Related: [louis-agent.site](louis-agent.site.md) ·
[louis-agent.identity.guide](louis-agent.identity.guide.md)
