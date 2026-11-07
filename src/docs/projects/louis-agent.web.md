# louis-agent.web — design

> **Kind:** Razor class library (`net10.0`, runs in the browser) · **Path:** `src/web/louis-agent.web/` ·
> **References:** Markdig, Microsoft.AspNetCore.Components.Web and .WebAssembly · **Used by:** louis-agent.code-review (code
> review) and louis-agent.puls (the general assistant)

## Purpose

The framework's web: what every site shares ([ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md)). So far that's the
admin's frame, the chat's frame and what a chat needs to work the same on each site: the chat as data, the API client, a turn's life from
the first message to the saved answer, the views of a turn and of a specialist's work, markdown, usage and currency. For now each site
keeps its colours and brand; the rules and scripts of the frames are here. The components only emit class names, and their words come from a
`ChatText`, English by default, that a site can replace.

## How it works

- **The admin's frame.** `AdminShell` is the sidebar, the phone's top bar and the content column, with slots a site's small layout
  fills: `Brand`, `Links`, `Top` and the page, plus the way back. An empty slot leaves no gap. A framework page names `AdminFrame`,
  which draws inside the layout the site set in `SiteFrames`. The frame's words are English on every site.
- **The chat's frame.** `ChatShell` is the whole chat: the sidebar, the phone's top bar, the messages, the message box, search, the toast
  and the work panel. A site writes a small page with its addresses and fills the slots: `Logo`, `Views`, `Profile`, `Background`, `Empty`,
  `ViewContent` and `UsageLine`. An empty slot leaves no gap, and with no `Empty` a new chat is just the message box. The site also says how a
  session opens (`OpenSession`), since the toolsets it asks for are its own; the frame hands it the voice, the place and the time zone as
  a `ChatOpening`. A site can reach in for two things through `@ref`: put a draft in the message box, and say something's coming soon.
- **The look.** The rules are Sass under `Styles/web/`, and a site lists them in its own `app.scss`, found through `IncludePaths`, so it
  gets one CSS file with the order it chose. Colours go by Bootstrap's names, or a `--web-` name where it has none, and no rule holds a fixed
  one. `web/settings/_colours.scss` is what a site with none of its own gets: the Console theme, black and white with metallic greys, plus a
  muted red and an amber. A site's colours come after it and win.
- **The scripts.** `wwwroot/js/chat.js` is what the frame calls, and `first-paint.js` applies what the browser remembers before the first
  paint; a site loads both from `_content/louis-agent.web/js/`. A script names a problem, and the site's `ChatText` says it.
- **Small state classes.** What the frame keeps that can be tested without drawing it is a plain class in `Shell/`: renaming a chat,
  this browser's settings, choosing a language, a chat's costs. `ChatShell` holds them and only passes them on.
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
  ([F14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F14-display-currency.md)).
- **Kept in the browser.** `ChatStore` keeps chats in `localStorage`; `ChatSettings` keeps the API key and the admin key
  there too, never on the server.

## Structure

| Path | What's in it |
|---|---|
| `Api/` | `AgentApi`, `AgentApiException` (an error with its status). The events are the session-stream reader's `SessionEvent`s (`louis-agent.session-stream`) |
| `Api/AskedToolset.cs`, `Api/ScriptLanguage.cs` | The browser side's own copies of the names it uses (it cannot reference core); a test in `api.tests` checks them against core's, so they cannot drift (ADR 0043) |
| `Chats/` | The records (`Chat`, `Turn`, `Block`, `BlockKind`, `Attachment`), the stream's folding (`TurnStream`, `Delegation`, `WorkStatus`, `RolePhases`, `SpecialistAnswer`) and `ChatRunner` |
| `Components/` | `TurnView`, `BlocksView`, `DelegationCard`, `CurrencySwitcher`; each component's C# in its `.razor.cs` |
| `Components/Admin/`, `Layout/` | `AdminShell`, `AdminGate` and `SiteFrames`; `AdminFrame`, the layout that hands over to the site's |
| `Components/Packs/`, `Components/Screens/` | `PackViews` and `IScreenView`, which component draws a screen; `PackMenu`, the sidebar's link per screen; in `Table/`, `ScreenTable`, the default, with its cells; in `Acting/`, `ScreenActionButton`; in `Forms/`, `ScreenForm` and `ScreenRowsEditor` ([ADR 0086](../adr/0086-a-packs-screens-are-described.md)) |
| `Pages/` | `FrameworkPages`, the list a site's router takes; in `Admin/`, `Screen`, the one page every pack's screen opens on, and `ScreenRow`, a row's form |
| `Components/Chat/` | `MessageList`, `Composer`, `ComposerButtons`, `ChatUsageBar`, `Notice`, `Toast` |
| `Components/Chat/Shell/` | `ChatShell`, the whole frame, with `ChatOpening` and `ChatShellEmpty`, the two records it hands a site, and `ChatHandoff`, how a screen's button opens the chat with a draft |
| `Components/Chat/Sidebar/`, `Header/`, `Search/` | `ChatSidebar`, `ChatListItem`, `ChatMenu`, `LocationPicker`, `MotionToggle`; `ChatHeader`, `CreditsMenu`, `LanguageMenu`, `ProfileMenu`; `ChatSearch` |
| `Components/Chat/Composing/`, `Turns/`, `Panels/` | `MessageBox`, `ComposerTools`; `UserActions`, `AnswerActions`; `WorkPanel`, `WorkItem`, `WorkStep` |
| `Shell/` | The frame's state as plain classes: `ChatRenaming`, `ChatPreferences` (with `LocatedPlace`), `ChatLanguages`, `ChatCosts` |
| `Search/`, `Content/` | `ChatFinder`, which matches the search; `Source` and `Sources`, a turn's sources |
| `Styles/web/` | `settings/_colours.scss`, `tools/_colours.scss` (both forms of a colour from one value), and the rules by concern in `base/`, `layout/`, `chat/`, `admin/` |
| `wwwroot/js/` | `chat.js` (with `renderCode` for a pack's file viewer), `first-paint.js` |
| `Text/` | `ChatText` (the components' words, English by default), `StepMarks` (a work step's mark), `ToolArgs` (a tool call's input on one line), `Markdown` |
| `Usage/` | `TurnUsage`, `UsageFormat`, `CurrencyState`, `CurrencyChoice`, `RatesTable`, `RatesDisplay` |
| `Storage/` | `ChatStore`, `ChatSettings` |

## Configuration

None. It calls the API on the same origin as the app; the API key, when the API asks for one, is typed into the app
and kept in that browser.

## Extending it

The steps are in [Change the framework's web](../guides/change-the-web.md). In short: a piece belongs here
only when both apps need it; a new API call goes in `AgentApi`; a component's words go in `ChatText` (English
default) and in Puls's `PulsText`; markup only in a `.razor`, logic in a plain class with tests
([ADR 0034](../adr/0034-code-a-person-can-maintain.md)). Then rebuild the image of each app that uses it.

## Tests

`src/tests/web/louis-agent.web.tests`, NUnit with bUnit for the components, mirroring the folders: `ChatRunnerTests`,
`TurnStreamTests`, `DelegationTests`, `WorkStatusTests`, `SpecialistAnswerTests`; `TurnViewTests`, `BlocksViewTests`,
`DelegationCardTests`, `CurrencySwitcherTests`; `AdminShellTests`, `ChatShellTests` and a test for each part of the chat's frame;
`MarkdownTests`, `ToolArgsTests`, `UsageFormatTests`; and
`RazorHasNoCodeTests`, which fails on a `@code` block in a `.razor`. `src/tests/web/louis-agent.web.testing` holds the fakes the
web test projects share.

## Limits and plans

- `Chats/` holds eleven files and `Components/Chat/` ten, over the "about six" of
  [ADR 0034](../adr/0034-code-a-person-can-maintain.md): split them by concern (the records apart from the stream's
  folding) the next time they change.
- `UsageFormat` repeats core's `UsageText` number rules, so a change to one must be made in both (see
  [louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)).

## Related docs

[louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md) · [louis-agent.puls](../../../demos/puls/docs/projects/louis-agent.puls.md) · [louis-agent.api](louis-agent.api.md)

---
[Projects](README.md) · Previous: [louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md) · Next: [louis-agent.mcp-server](louis-agent.mcp-server.md)
