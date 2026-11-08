# ADR 0105: The guide acts on the page through a tool the widget hears

**Status:** Accepted (2026-11-08, F117)

## Context

The guide in the site's corner knows the page the visitor is reading (F115, ADR 0104) but could only point: "the
Identities page shows the levels". Louis asked for more (2026-11-08): "I want this agent to take you to a page", "he
should be able to direct you to a certain part of the page also, location on the page, so all pages must have areas",
"can we also give it the capability to highlight areas on the page?" Three ways were weighed: a link in the answer the
visitor clicks (points, doesn't take), a marker in the text the widget parses (a hidden protocol in prose, against
arguments as data, ADR 0004), or a tool the API validates and the widget acts on. Louis picked the tool.

## Decision

- **A client-side effect is a tool call the client hears.** The `site` pack's `ShowPage(path)` runs in the API like
  every tool: it checks the path (a page of this site, an area after the hash in words) and answers that the page is
  shown. It moves nothing. The web's runner raises `ToolCalled` for every tool call as its block arrives, and the
  widget acts on this one: it navigates to the path and asks its module to scroll the area into view and light it up.
  The model asks as data, the server validates, the client performs: nothing in the model's words is parsed.
- **Every area is named.** A page is made of bands, each with an id the framework insists on (`Band`, ADR 0106), and
  the page block a message carries lists them as `#id heading`, so the guide knows where it can send someone before it
  decides. A site's test holds that no section on any page is without an id.
- **The tool is honest about its reach.** An MCP client or a chat page without the widget gets the same text and no
  movement, which is right: the tool says what should be shown, the client decides whether it can. The voice says when
  to call it: once, when the right page or area exists, never a page it only guesses, never when a sentence answers.
- **The highlight is the framework's, restyled by a site.** The module keeps a class on the area for four seconds; the
  web's base styles draw it as a glow that fades, a reduced-motion setting gets a still ring, and a site may restyle it.

## Consequences

- Any effect a site wants the guide to have on the page follows the same shape: a pack's tool for the request, a
  check in the API, the widget hearing the call. The widget names the tool it acts on and never the pack.
- A tool call now has two audiences, the model and the client; the result text speaks to the model, the call's input
  to the client, and both are the same data.
- The web's runner exposes one more event; chat pages without a listener are unchanged.

## Links

- [ADR 0104: a message may carry the page](0104-a-message-may-carry-the-page.md)
- [ADR 0106: the landing is a business website from the framework](0106-the-landing-is-a-business-website-from-the-framework.md)
- [ADR 0080: a tool when it improves the prompt](0080-a-tool-when-it-improves-the-prompt.md)
- [ADR 0004: arguments as data](0004-arguments-as-data.md)
- `packs/tools/louis-agent.pack.site/`, `src/web/louis-agent.web/Components/Widget/ChatWidget.razor.cs`
