# ADR 0116: A showroom's demos can be tried live, through the open door

**Status:** Accepted (2026-11-10, F127.2)

## Context

F127.1 put the site's demos in a showroom: each business's agent played a recorded conversation on a mock of the
business's own website (ADR 0115). Louis asked for more: "the demos must be interactive, 3 pagers with working pages
and sections, agents must demonstrate navigation between pages and sections. when the demo finishes there should be a
button try it yourself." A recording shows what an agent does. A visitor who types their own question learns whether it
would answer theirs.

Only the guide could be reached without a key or a sign-in: one identity, a few sessions a day per address (ADR 0099).
A business's agent can pass a booking on (`SendLead`), and on a made-up business that booking reaches Louis, so it has
to say it was a try.

## Decision

- **A mock website has pages.** A site writes each as a `MockPage` inside its `SiteMock`; each registers with its
  mock, as Microsoft's TabSet example has its tabs do, and the mock draws its menu from them. The page an address is on
  is the longest page path it starts with (`MockPaths`). A page change loads like a browser: the address glows, a
  loading line runs, the page slides up, still for less motion.
- **Try it yourself swaps the recording for a live chat in the same corner** (`MockLiveChat`, on the framework's
  `ChatRunner`). It opens a session as the business's identity, sends the mock's page with a question (its words,
  every page's places, its menu), and hands each `ShowPage` call to the demo, as the recording does, so the website
  moves the same way for both. While it's live the cues wait, and the chat shows no usage line: a customer never sees
  tokens on a business's website.
- **The open door takes a list, under one cap.** `OPEN_IDENTITIES` names the identities a visitor may open with no key
  and no sign-in, the guide and the showroom's businesses. One address opens a few sessions a day across them all, and
  the session is narrowed in code to the identity the door admitted and its own toolsets.
- **A demonstration says so.** An identity is a demonstration when it says it is (`IAgentIdentity.IsDemonstration`);
  the turn carries who it runs as (`TurnIdentity`, ambient, as `UserPlace`), and the leads pack marks a
  demonstration's lead: the mail's subject is `[demo] {identity}: {name}`, and the model hears that it was a
  demonstration, so it tells the visitor. One leads pack and one `SendLead`: a second registration would have named
  the tool twice, and tool names are unique so packs stay convertible to MCP (ADR 0028).
- **The brands are the framework's to serve.** A mock's faces are a class of three roles (display, text, small
  capitals), its header one of three treatments, and the faces it wears are the framework's own files under their open
  licence, fetched only by a page that wears them, so no third party sees a visitor.

## Consequences

- A visitor can try every business's agent on its own website, and the website it moves is the one the recording
  moved.
- The worst day costs what the guide's did: the cap is one, across every open identity.
- A showroom business must stay safe to open to the public: its toolsets are only its own (the leads and site packs),
  a test holds that no identity on the site reaches a pack the guide doesn't load, and its voice says it's an AI and
  made up.
- A recording must be recorded again when its website's pages change, since its `ShowPage` calls name their places; a
  test holds each recording to the places its website has.

## Links

[ADR 0099](0099-the-guide-answers-without-a-sign-in-within-a-cap.md) (the door this widens) ·
[ADR 0115](0115-a-sites-showroom-is-an-area-of-the-framework.md) (the showroom) ·
[ADR 0101](0101-a-lead-is-a-tool-sent-once-on-the-visitors-yes.md) (the lead) ·
[ADR 0028](0028-packs-stay-convertible-to-mcp.md) · [ADR 0117](0117-one-address-for-every-agent-call.md) (the guide
asks a business next, F127.3 to F127.5) · F127.2 in the [work log](../work-log.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
