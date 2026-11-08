# ADR 0104: A message may carry the page the visitor is reading

**Status:** Accepted (2026-11-08, F115)

## Context

The guide sits in the corner of every page of the site, but a visitor's message carried only their words and their
attached files. Asked "can you see the page I'm on?", the guide could not (Louis, 2026-11-08: "the agent should know
which page im on", "it should be able to read what i read and see"). Three ways were weighed: the address and a tool
that reads the page from the site (a new tool, and the site's pages aren't in the guide's docs), what the visitor
sees sent along with the message, or a hand-written line of context per page. Louis picked the second.

## Decision

- **The page travels with the message, as data.** A message request may carry a page: its address, its title and its
  visible text. The web's body leaves the key out when none travels, so the wire is as it was for every caller
  without a page.
- **It becomes one block in the user's message.** The API's prompt builder adds the page after the words and the
  attached files, the way attached files already travel, cut at 6,000 characters with a note. The engine sees one
  user message; nothing edits the system prompt or earlier messages (ADR 0008), and no tool is added for what a
  single block says (ADR 0080's rule runs the other way: the model has what it needs in its hand).
- **Sent once per page, read at send time.** The widget sends the page with the first message written from it and
  again after a move to another page, never on every message. It reads the page when the message is sent, through
  its own module, so the text is the page as rendered and not the one before a move; the runner holds the page for
  the next message and forgets it, and gives it back when the send fails, as it does the draft.
- **The guide knows the shape.** Its voice says what the block is: the page in front of the visitor right now, used
  to answer about what they see, named only when that helps.

## Consequences

- A visitor can ask about "the diagram above" or "this price" and be answered; a few thousand tokens once per page.
- The page's text is the site's own public words, so nothing private travels; a site whose pages hold a person's data
  must decide before it drops the widget in.
- Any client of the API, not only the widget, can send a page with a message; a chat page without one is unchanged.
- The cap is the API's, not the browser's: a browser sends what it reads and the API cuts, so a changed cap is one
  constant in one place.

## Links

- [ADR 0008: history is append-only](0008-history-is-append-only.md)
- [ADR 0080: a tool when it improves the prompt](0080-a-tool-when-it-improves-the-prompt.md)
- `src/hosts/louis-agent.api/Sessions/TurnStream.cs`, `src/web/louis-agent.web/Components/Widget/ChatWidget.razor.cs`
- F115 in the work log
