# louis-agent.web.shared — design

> **Kind:** Razor class library (`net10.0`, runs in the browser) · **Path:** `src/louis-agent.web.shared/` ·
> **References:** Markdig, Microsoft.AspNetCore.Components.Web and .WebAssembly · **Used by:** louis-agent.web (code
> review) and louis-agent.puls (the general assistant)

## Purpose

What both web apps share, so a chat works the same in each: the chat as data, the API client, a turn's life from the
first message to the saved answer, the views of a turn and of a specialist's work, markdown, usage and currency. Each
app keeps its own pages, words, look (CSS) and JavaScript; the shared components only emit class names, and their words
come from a `ChatText` each app fills in.

## How it works

- **One client.** `AgentApi` is the only code that calls the API: it opens a session (its toolsets, role, place and
  time zone), streams a message's events over SSE, stops and deletes sessions, and reads the toolsets, rates and other
  GETs. Components never touch `HttpClient`.
- **A turn's life.** `ChatRunner` holds what every chat page does with a message: the draft and its attachments, the
  session opened on the first message, the stream with a redraw raised at most every 50 ms, stop, the save, and errors
  in the app's own words.
- **The stream becomes a turn.** `TurnStream` folds the API's events (thinking, text, tool calls, usage) into the
  `Turn` and its `Block`s the page shows. A sub-session's events go to its `Delegation`, so a specialist's work appears
  inside the turn that called it; `WorkStatus` and `RolePhases` say what the specialist is doing, and `SpecialistAnswer`
  splits its turn into its work and its answer.
- **The views.** `TurnView` and `BlocksView` show one turn (the message, collapsible thinking, text, tool cards);
  `DelegationCard` a specialist's work, live, with its cost; `MessageList`, `Composer`, `ComposerButtons`,
  `ChatUsageBar` and `Notice` are the chat pieces both apps lay out, with slots for an app's own parts.
- **Safe markdown.** `Markdown` is the one Markdig pipeline: raw HTML is shown as text, links only `http(s)` and
  `mailto`, images only from the shops' own image hosts, so a model-written image can't send the chat's data elsewhere.
- **Usage and currency.** `TurnUsage` and `UsageFormat` show a turn's tokens and cost from the API's usage event;
  `CurrencyState` and `CurrencySwitcher` show it in the browser's chosen currency from the API's rates
  ([F14](../features/F14-display-currency.md)).
- **Kept in the browser.** `ChatStore` keeps chats in `localStorage`; `ChatSettings` keeps the API key and the admin key
  there too, never on the server.

## Structure

| Path | What's in it |
|---|---|
| `Api/` | `AgentApi`, `AgentApiException` (an error with its status), `AgentStreamEvent` |
| `Chats/` | The records (`Chat`, `Turn`, `Block`, `BlockKind`, `Attachment`), the stream's folding (`TurnStream`, `Delegation`, `WorkStatus`, `RolePhases`, `SpecialistAnswer`) and `ChatRunner` |
| `Components/` | `TurnView`, `BlocksView`, `DelegationCard`, `CurrencySwitcher`; each component's C# in its `.razor.cs` |
| `Components/Chat/` | `MessageList`, `Composer`, `ComposerButtons`, `ChatUsageBar`, `Notice` |
| `Text/` | `ChatText` (the components' words, the code-review app's by default), `ToolArgs` (a tool call's input on one line), `Markdown` |
| `Usage/` | `TurnUsage`, `UsageFormat`, `CurrencyState`, `CurrencyChoice`, `RatesTable`, `RatesDisplay` |
| `Storage/` | `ChatStore`, `ChatSettings` |

## Configuration

None. It calls the API on the same origin as the app; the API key, when the API asks for one, is typed into the app
and kept in that browser.

## Extending it

Put a piece here only when both apps need it; anything one app needs stays in that app. A new API call goes in
`AgentApi`. A component's words go in `ChatText` with the code-review app's text as the default, and Puls sets its
Afrikaans in `src/louis-agent.puls/Text/PulsText.cs`. A component holds markup only, with its C# in a `.razor.cs` and
its logic in a plain class with NUnit tests ([ADR 0034](../adr/0034-code-a-person-can-maintain.md)). Then rebuild the
image of each app that uses it.

## Tests

`tests/louis-agent.web.shared.tests`, NUnit with bUnit for the components, mirroring the folders: `ChatRunnerTests`,
`TurnStreamTests`, `DelegationTests`, `WorkStatusTests`, `SpecialistAnswerTests`; `TurnViewTests`, `BlocksViewTests`,
`DelegationCardTests`, `CurrencySwitcherTests`; `MarkdownTests`, `ToolArgsTests`, `UsageFormatTests`; and
`RazorHasNoCodeTests`, which fails on a `@code` block in a `.razor`. `tests/louis-agent.web.testing` holds the fakes the
web test projects share.

## Limits and plans

- `Chats/` holds eleven files and `Components/Chat/` ten, over the "about six" of
  [ADR 0034](../adr/0034-code-a-person-can-maintain.md): split them by concern (the records apart from the stream's
  folding) the next time they change.
- `UsageFormat` repeats core's `UsageText` number rules, so a change to one must be made in both (see
  [louis-agent.web](louis-agent.web.md)).

## Related docs

[louis-agent.web](louis-agent.web.md) · [louis-agent.puls](louis-agent.puls.md) · [louis-agent.api](louis-agent.api.md)

---
[Projects](README.md) · Previous: [louis-agent.web](louis-agent.web.md) · Next: [louis-agent.mcp-server](louis-agent.mcp-server.md)
