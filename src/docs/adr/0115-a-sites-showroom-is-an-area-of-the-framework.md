# ADR 0115: A site's showroom is an area of the framework

**Status:** Accepted (2026-11-10, F127.1)

## Context

The site sells websites with agents (ADR 0112), and its demos sold them in the site's own pages, one layout each: a
restaurant, a law practice and a doctor's practice played a recorded conversation in a plain window, and the coding
agent and the triage team each had a page of their own. Louis asked for "a layout specifically for demos", not the
landing's, "colourful and fancy", and of the research's layouts (a rail of every demo, a demo shown on the business's
own website, a big start screen) picked the rail and the mock website together. During the build he asked for more:
sample branding, the play at a person's pace, cues a visitor can click to go back, the agent showing its website and
not only talking ("the agent can navigate the website basically"), real photos and a real menu ("this is our selling
point"), and notes of what a custom agent could add ("we could integrate with the booking system if they have one").

## Decision

- **The showroom is an area of the framework, in `louis-agent.web`,** as the site's frame is (ADR 0084): the frame
  (`ShowroomShell`: a slim bar, the rail of demos grouped by what a visitor comes for, chips on a phone, the corner),
  a demo's stage (`DemoStage`: the kicker, the page's one h1, the lede, the player, the cues, the notes and the
  offer, each a section with its own address, ADR 0107), its start (`ShowroomStart`: a big start for the main demo,
  a cover per demo) and the mock website (`SiteMock` with its places, `MockSection`, `MockCard`, its menu, its looks).
  A site gives data and words only: its demos as `ShowroomDemos`, registered once, its words as `ShowroomText`, and a
  page per demo that fills the stage's slots. The framework names no site.
- **A business's demo plays on a mock of its own website,** in its sample brand (a mark, its colours, a typeface, a
  look), with real photos dressed in that brand, and every price and time on it is one its agent knows: a test holds
  the website to the agent's voice, so the two never tell a visitor different things.
- **The agents show the website with the guide's own tool.** A business's agent has the `site` pack's `ShowPage`
  beside `leads`, learns its website's places from the page its first message carries (as the guide's widget sends
  the page in view), and its recording's tool calls are told as they play (`TurnReplay`, `RecordedChat.ToolCalled`),
  so the mock goes to the place, lit with the guide's spotlight (a section) or pressed (a card). What the demo shows
  is what a customer's website would get.
- **A cue is a way back in:** each plays the conversation from its turn, the turns before it at once, so the cues and
  the website catch up.
- **A place's own address opened afresh draws in its page's layout** (`PlaceLayout`), so a refreshed
  `/restaurant-demo/the-demo` is still in the showroom.

## Consequences

- A new demo is one line in the site's `ShowroomDemos` and a page that fills the stage; the rail, the chips, the start
  and the sitemap pick it up.
- The demo websites' facts grow with their voices, and a recording sends the page it shows, so changing a website
  means recording its conversation again (a dry run first, then one lead mail each, with Louis's yes).
- Photos are third-party: from Pexels under its licence, chosen with no recognisable person, brand or real private
  property presented as a made-up business, and kept in the site's own `images/showroom`.
- The demos become working three-page websites a visitor can try live in F127.2, on this area.

## Links

- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- [ADR 0105: the guide acts on the page through a tool the widget hears](0105-the-guide-acts-on-the-page-through-a-tool-the-widget-hears.md)
- [ADR 0107: every section has an address](0107-every-section-has-an-address.md)
- [ADR 0108: the public watches the coding agent, recorded](0108-the-public-watches-the-coding-agent-recorded.md)
- [ADR 0112: the site sells first](0112-the-site-sells-first.md)
- F127.1 in the work log
