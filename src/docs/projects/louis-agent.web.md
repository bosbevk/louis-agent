# louis-agent.web — design

> **Kind:** Razor class library (`net10.0`, runs in the browser) · **Path:** `src/web/louis-agent.web/` ·
> **References:** Markdig, Microsoft.AspNetCore.Components.Web and .WebAssembly · **Used by:** every site with a chat:
> louis-agent.code-review, louis-agent.puls and louis-agent.starter

![The framework's web, and its parts: the chat's frame, the admin's frame, the site's parts, styles and scripts, and the seams](diagrams/louis-agent.web.svg)

## Purpose

The framework's web: what every site shares ([ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md)). That's the
admin's frame, a pack's screens, the chat's frame and what a chat needs to work the same on each site: the chat as data, the API client, a turn's life from
the first message to the saved answer, the views of a turn and of a specialist's work, markdown, usage and currency. For now each site
keeps its colours and brand; the rules and scripts of the frames are here. The components only emit class names, and their words come from a
`ChatText`, English by default, that a site can replace.

## How it works

- **A site names its areas** (F127.6, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md)).
  `AddSiteAreas(areas => …)` (`Components/Areas/`) registers the chat's plumbing for every site (`ChatSettings`,
  `ChatStore`, `AgentApi`, `CurrencyState`, an English `ChatText`, each only where the site set none), then each area
  the site names: `Chat()` the chat portal's handoff (`ChatHandoff`); `Admin(layout)` the admin's frame
  (`SiteFrames`), its home and its pages; `Showroom(demos)` the demos, the guide's way in
  (`GuideDoor`), the demos' windows (`AgentWindows`) and the kept guide (`GuideKeeper`); and a library's own area
  through `Add`, as the eval studio's `Evals()` and the accounts' `Accounts(layout)` do. An area named twice counts
  once. `SiteAreas`, registered once, gives the router its page assemblies and `AdminShell` each area's admin links.
  A site never builds or registers any of these itself; `SitesNameTheirAreasTests` holds it. `Focuses(f => …)`
  (F127.7, `Components/Areas/Focuses/`) names the part of the site each page is (`SiteFocuses.Of(path)`: the longest
  matching address by whole segments, else the default), which the guide's widget sends with every message.
- **The admin is a frame its areas plug into** (F144, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md)).
  `Admin(layout)` is the frame alone: `AdminShell` around the site's layout, and the home at `/admin`
  (`Pages/Admin/AdminHome`), which draws each admin area's card (`SiteArea.AdminCard`, in `SiteAreas.AdminCards`) in
  the order the site named them, two columns, one on a phone, with "Home" first in the sidebar. Everything inside is
  an area with its pages, its sidebar links (`AdminLinks`) and its card: `Overview(count => count.Tool("SendLead",
  "leads"))` the overview, naming the tools it counts (the API counts every tool and names no pack); `Packs(views =>
  …)` the packs' screens and uploads, with `PackMenu` and the packs page at `/admin/packs`, all reading one
  `PackOffer`; and the eval studio's `Evals()`, at `/admin/evals`. An admin area named without the frame is refused at
  start-up (`WebErrorMessage.AdminAreaWithoutFrame`). Every card is an `AdminCardBase` (Template Method): it loads
  the key, reads, and when the API refuses tells `AdminAccess` (Observer), so the home asks for the key once, and a
  saved key makes every card read again. A card draws its frame with `AdminCard` and its figures with the
  `web/tools/figures` Sass tool.
- **The admin's overview** (F127.8, an area since F144): `Pages/Admin/Overview` at `/admin/overview` draws the API's
  overview in the reader's currency: `OverviewFigures` (today's and the month's talks, cost and the counted tools,
  `OverviewCounts.In`), `OverviewBars` (the month's talks by part of the site and by who answered, one colour) and
  `LatestTalks` (a table that scrolls sideways on a phone), all in `Components/Admin/Overview/` with `OverviewCard`
  and `OverviewLinks`.
- **Every part is ready for an agent** (F127.7, [ADR 0121](../adr/0121-every-part-of-the-web-is-ready-for-an-agent.md)).
  A tile with a title or a number and a deck's card are **pieces** (`data-piece`, an id from `Site/Pieces/PieceId`,
  so `/home/100-adrs` opens it). `Widget/Pieces/PieceWatch` and its module hear a click on a piece (it asks the guide
  about it), a card the visitor opens and a piece the guide shows (both selected with nothing sent): the piece is drawn
  pressed (`web/tools/pressed`), a chip in the guide's box names it, and every message carries it
  (`Page.Selected`) until the chip is closed. The page block lists the pieces as `(piece, selects)`.
- **The admin's frame.** `AdminShell` is the sidebar, the phone's top bar and the content column, with slots a site's small layout
  fills: `Brand`, `Links`, `Top` and the page, plus the way back. An empty slot leaves no gap. A framework page names `AdminFrame`,
  which draws inside the layout the site set in `SiteFrames`. The frame's words are English on every site.
- **Accounts, through one seam** (CL30, [ADR 0089](../adr/0089-the-account-client-is-a-library-a-site-opts-into.md)).
  The account pages and their client are [louis-agent.web.accounts](louis-agent.web.accounts.md), a library on top of
  this one that a site opts into. All this library knows of accounts is `IAccountMenu`, declared beside the chat's
  profile menu: are there accounts, who is signed in, the two labels, load, changed, sign out. The menu asks the
  provider for it once and holds `NoAccounts`, a Null Object, when a site registered nothing, so the guest's menu
  draws with no branch for the case. Every call to the API still says it is the app's own (`X-Requested-With`), which
  is what lets a signed-in person's cookie count in place of a key; `SiteFrames.Account` names the layout the account
  pages draw inside.
- **The eval studio** is [louis-agent.web.evals](louis-agent.web.evals.md), a library on this one a site opts into (CL57); the
  router lists what a site opted into through `FrameworkPages.With(AccountLibrary.Assembly, EvalLibrary.Assembly)`.
- **The chat's frame.** `ChatShell` is the whole chat: the sidebar, the phone's top bar, the messages, the message box, search, the toast
  and the work panel. A site writes a small page with its addresses and fills the slots: `Logo`, `Views`, `Profile`, `Background`, `Empty`,
  `ViewContent` and `UsageLine`. An empty slot leaves no gap, and with no `Empty` a new chat is just the message box. The site also says how a
  session opens (`OpenSession`), since the toolsets it asks for are its own; the frame hands it the voice, the place and the time zone as
  a `ChatOpening`. A site can reach in for two things through `@ref`: put a draft in the message box, and say something's coming soon.
- **The look** ([ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md), CL58). A rule one component draws sits beside it in its
  `X.razor.scss`, scoped and bundled by the build into the site's `{site}.styles.css`, which the site links once. The globals are Sass under
  `Styles/web/`: what every page shares, what a site's own pages draw too and what a theme overrides; a site lists them in its own `app.scss`,
  found through `IncludePaths`, so it gets one CSS file with the order it chose. Colours go by Bootstrap's names, or a `--web-` name where it
  has none, and no rule holds a fixed one. `web/settings/_colours.scss` is what a site with none of its own gets: the Console theme, black and white with metallic greys, plus a
  muted red and an amber. A site's colours come after it and win.
- **The scripts** ([ADR 0093](../adr/0093-a-script-is-a-module-beside-its-component.md), CL31) are ES modules beside their components
  (`ChatShell.razor.js`, `MessageList.razor.js`, `TurnView.razor.js`) or shared under `wwwroot/js/` (`preferences.js`, `dialogs.js`,
  `download.js`), each imported through `Scripts/JsModule` the first time its component draws and let go when it's disposed; the paths are
  named once in `FrameworkScripts`. `first-paint.js` applies what the browser remembers before the first paint, and is the one script a
  site lists of ours. A script names a problem, and the site's `ChatText` says it; `FrameworkModulesTests` holds the rule.
- **Small state classes.** What the frame keeps that can be tested without drawing it is a plain class in `Shell/`: renaming a chat,
  this browser's settings, choosing a language, a chat's costs. `ChatShell` holds them and only passes them on.
- **One client.** `AgentApi` is the only code that calls the API: it opens a session (its toolsets, role, place and
  time zone), streams a message's events over SSE, stops and deletes sessions, and reads the toolsets, rates and other
  GETs. Components never touch `HttpClient`.
- **A turn's life.** `ChatRunner` holds what every chat page does with a message: the draft, its attachments and the page the visitor is reading when one is set (F115, sent once and forgotten), the
  session opened on the first message, the stream with a redraw raised at most every 50 ms and the last update always drawn (`RedrawThrottle`), stop, the save, and errors
  in the app's own words.
- **The stream becomes a turn.** `TurnStream` folds the API's events (thinking, text, tool calls, usage) into the
  `Turn` and its `Block`s the page shows. A sub-session's events go to its `Delegation`, so a specialist's work appears
  inside the turn that called it; `WorkStatus` and `RolePhases` say what the specialist is doing, and `SpecialistAnswer`
  splits its turn into its work and its answer.
- **The views.** `TurnView` and `BlocksView` show one turn (the message, collapsible thinking, text, tool cards);
  `DelegationCard` a specialist's work, live, with its cost; `MessageList`, `Composer`, `ComposerButtons`,
  `ChatUsageBar` and `Notice` are the chat pieces both apps lay out, with slots for an app's own parts.
- **Cards an agent draws** (F127.4.1). The site pack's `ShowCard` and `Offer` draw nothing themselves: the chat reads
  each call (`Chats/Calls/`: `CardCall` through the diagrams library's `TicketCard`, `OfferCall` into `Offered`) and
  draws it with the work hidden too: `DrawnCard` holds the library's SVG inline (`RenderForPage`, its styles kept to
  itself) and `OfferCards` the choices as buttons. `CardsLast` puts an answer's cards under its words, whatever order
  they were called in. A click goes up as `Chosen` (`BlocksView`, `TurnView`, `MessageList`) to the chat that owns a
  runner, which sends the label as the visitor's message; `MessageList` hands each turn the next one's words as its
  `Answer`, and a finished turn draws again when it gets one, so its cards fold to the one chosen. A failed call (the
  pack's refusals start with "Error") draws nothing.
- **An ask shown in the asked agent's own window** (F127.4.2, ADR 0117). The stream gives an `AskAgent` call its
  agent's session from the nested `message_start` (`Block.SubSessionId`). `Components/Widget/Door/AgentWindows`, an
  Observer like `GuideDoor`, lets a page's window say which agent it shows; the guide's widget hands it an ask to that
  agent (`Take`, which marks the call `InWindow`, so the guide's chat draws one line instead of the talk) and tells it
  each time its stream moves on (`Moved`); on a narrow screen the widget's module brings the window into view and the
  guide folds to its head. The showroom's `MockLiveChat` watches the ask (`Chats/Calls/WatchedAsk`: the visitor's
  words and the answer as one turn, its blocks shared with the guide's stream, the agent's tool calls handed on once
  each) and, once it ends, holds a chat on that agent's own session, so the visitor carries on there.
- **Safe markdown.** `Markdown` is the one Markdig pipeline: raw HTML is shown as text, links only `http(s)` and
  `mailto`, images only from the shops' own image hosts, so a model-written image can't send the chat's data elsewhere.
- **Usage and currency.** `TurnUsage` and `UsageFormat` show a turn's tokens and cost from the API's usage event;
  `CurrencyState` and `CurrencySwitcher` show it in the browser's chosen currency from the API's rates
  ([F14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F14-display-currency.md)).
- **A site's public pages** (F118, ADR 0106, 0107). The engine and an agent run without any of this: the CLI, Rider,
  the API and MCP need no web. This part is what turns an agent into a website. `SiteShell` is the frame: a header, the
  page, a footer, the guide in its corner, a scroll progress line, a way back up, and a section index on the left
  built from the page's bands (each band puts itself on the frame's `PageAreas` as it draws). A page is built of
  `Band`s and the framework's `SiteHero`, `TileRow` and `Tile`, `CardDeck` and `SiteStrip`, each with the site's words;
  a `GuideButton` anywhere asks the guide a question through `GuideDoor`. A site lays its pages out
  as **slides** or a **flowing page** (`SiteShell Layout`, a page may choose the other with one `PageLayoutChoice`
  line): on slides every section is a screen tall, its content in the middle, the page settles on one section at a
  time, a section with more than a screen of content pages sideways with a pager, and a diagram scales to fit. Every
  place has its own address (ADR 0107): a section `/proof/rand`, a card `/identities/puls`, a page of a paged section
  `/how-it-works/parts/2`, the landing's under `/home`; `SiteAddresses` is the rule and `SectionPage` the catch-all
  route that opens any place, and the address bar follows the visitor. The guide's widget sends the page it reads
  with every place by its address and the place on the visitor's screen, and its page tool takes an address: it
  scrolls there, opens a card or turns a page, and lights it with a spotlight that dims the rest. A site lists every
  page and place in its `sitemap.xml`, which a site test writes from the rendered pages and holds to them (F119). A
  page meant for search declares a `PageHead` beside its `PageTitle` (`Components/Site/Meta/`): its description and
  any structured data, in one head since Blazor keeps only the last one a page writes. The facts are schema.org
  records under `Meta/Schema/` (an organisation, a website, a piece of software, a breadcrumb), written as JSON-LD by
  `SchemaJson` (F123, [ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md)). The API's
  `MapLouisAgentSite` serves it as HTML with its own head and words, for crawlers that run no script (F121, ADR 0109).
  A place's address opened afresh draws in its page's own layout: `PlaceLayout` gives the router's `RouteView` the
  layout of the page the address names (F127.1).
- **A site's showroom** (F127.1, [ADR 0115](../adr/0115-a-sites-showroom-is-an-area-of-the-framework.md)), under
  `Components/Site/Showroom/`: the area a site shows its demos in. `Shell/` is the frame (`ShowroomShell`: a slim
  bar, `DemoRail` grouped by what a visitor comes for, `DemoChips` on a phone, the corner beside the frame, the whole
  frame in the colour of the demo on screen; an open guide takes the rail's place). `Stage/` is a demo's page
  (`DemoStage`: kicker, the page's one h1, lede, player, `DemoCues` lit in step with the play and each a button that
  plays from its turn, `DemoNotes` of what a custom agent could add, the offer; each part a section with its own
  address). `Start/` is the showroom's start (`ShowroomStart`: a big start for the main demo, a cover per demo).
  `Mock/` is a business's website (`SiteMock`: a browser bar, its sample brand, a page that scrolls inside the mock
  in one of three `MockLook`s with a hero photo worn with its brand, facts and its places, `MockSection` and
  `MockCard` with photos, `Menu/` for a real menu, and its agent's widget in the corner). A site registers its
  `ShowroomDemos` and may register `ShowroomText`. The recorded chat tells its page each tool call as it plays
  (`RecordedChat.ToolCalled`, read with `ShowPageCall`, shared with the guide's widget), so a mock goes to the place
  its agent showed, a section under the guide's spotlight and a card pressed.
- **A mock website of pages, tried live** (F127.2, [ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md)).
  `Mock/Website/`: a site writes each page as a `MockPage` inside its mock, which registers with it (TabSet) and keeps
  its list in `MockSiteMap`; the page an address is on is the longest page path it starts with (`MockPaths`), and a
  card with a page of its own opens it (`MockCard.Opens`). A visitor clicks through the menu, and a page change loads
  like a browser. `Mock/Head/MockHead` is the header, wearing its brand: `MockFont`'s class gives three faces
  (`--mock-face`, `--mock-text`, `--mock-caps`, from `web/tools/faces`), `MockHeader` one of three treatments,
  `MockColours.Label` its small capitals' colour; the pairings' faces are the framework's own woff2 files under
  `wwwroot/fonts/showroom/`, each beside its `OFL.txt`, declared in `web/showroom/fonts`, which a site lists, so a
  page fetches only the faces it wears. `Mock/Live/MockLiveChat` is the business's agent live in the widget, on
  `ChatRunner`, opened as its identity, sending `SiteMock.ReadPageAsync` (the page's words, every page's places by
  address, the menu as links) with each new page; the widget then says Live, the hint is a question to tap, the cues
  wait (`DemoCues.Waiting`) and no usage line shows (`MessageList.ShowUsage`, passed to `TurnView`). The mock's chat
  and the guide's minimise and maximise with the same `WindowButtons` (`ChatWindowSize`).
- **Kept in the browser.** `ChatStore` keeps chats in `localStorage`; `ChatSettings` keeps the API key and the admin key
  there too, never on the server.

## Diagrams

The [eval pages](louis-agent.web.evals.md) have diagrams of their own.

![The framework's web: a site's pages in the chat's and the admin's frames, and the API they call](diagrams/louis-agent.web-diagrams.svg)

![Sending a message: ChatRunner opens the session the first time, applies each event to the turn, redraws the page and saves the chat](diagrams/louis-agent.web-diagrams-2.svg)

![The types: shells for a site's frames, one runner for a chat's turn over the agent API, the chats kept in the browser and an address for every place](diagrams/louis-agent.web-interface.svg)

## Structure

| Path | What's in it |
|---|---|
| `Api/` | `AgentApi`, `AgentApiException` (an error with its status). The events are the session-stream reader's `SessionEvent`s (`louis-agent.session-stream`) |
| `Api/AskedToolset.cs`, `Api/ScriptLanguage.cs` | The browser side's own copies of the names it uses (it cannot reference core); a test in `api.tests` checks them against core's, so they cannot drift (ADR 0043) |
| `Chats/` | The records (`Chat`, `Turn`, `Block`, `BlockKind`, `Attachment`), the stream's folding (`TurnStream`, `Delegation`, `WorkStatus`, `RolePhases`, `SpecialistAnswer`) and `ChatRunner` |
| `Chats/Calls/` | `CardCall`, `OfferCall`, `Offered`, `OfferChoice`, `CardsLast`: the tool calls the chat draws (F127.4.1); `WatchedAsk`, an ask shown in its agent's window (F127.4.2) |
| `Components/` | `TurnView`, `BlocksView`, `DelegationCard`, `CurrencySwitcher`; each component's C# in its `.razor.cs`; in `Blocks/`, `DrawnCard` and `OfferCards` |
| `Components/Areas/` | `SiteAreasServices` (`AddSiteAreas`), `SiteAreasBuilder`, `SiteArea` and `SiteAreas`: how a site names its areas (F127.6) |
| `Components/Admin/`, `Components/Frames/`, `Layout/` | `AdminShell`, `AdminGate`; `AdminCard`, `AdminCardBase` and `AdminAccess`, every area's card on the home and the one ask for a refused key (F144); `SiteFrames`, the site's two layouts; `AdminFrame`, the layout that hands over to the site's |
| `Components/Chat/Header/Menu/` | `IAccountMenu`, what the profile menu asks of accounts, and `NoAccounts`, the answer when a site has none (CL30); the real side is in [louis-agent.web.accounts](louis-agent.web.accounts.md) |
| `Components/Packs/`, `Components/Screens/` | `PackViews` and `IScreenView`, which component draws a screen; `PackMenu`, the packs area's sidebar links (the packs page, then a link per screen); `PackOffer`, what the packs offer, which the menu, the packs page and `PacksCard` read (F144); in `Table/`, `ScreenTable`, the default, with its cells; in `Acting/`, `ScreenActionButton`; in `Forms/`, `ScreenForm` and `ScreenRowsEditor` ([ADR 0086](../adr/0086-a-packs-screens-are-described.md)) |
| `Pages/` | `FrameworkPages`, the list a site's router takes; in `Admin/`, `Screen`, the one page every pack's screen opens on, `ScreenRow`, a row's form, and `Imports`, the Uploads page; in `Admin/Running/`, `Reviewing/` and `Building/`, the eval pages |
| `Components/Imports/` | `ImportCard`, an upload by default, and `ImportForm`, the rules of when a preview or a save may run |
| `Components/Chat/` | `MessageList`, `Composer`, `ComposerButtons`, `ChatUsageBar`, `Notice`, `Toast` |
| `Components/Chat/Shell/` | `ChatShell`, the whole frame, with `ChatOpening` and `ChatShellEmpty`, the two records it hands a site, and `ChatHandoff`, how a screen's button opens the chat with a draft |
| `Components/Chat/Sidebar/`, `Header/`, `Search/` | `ChatSidebar`, `ChatListItem`, `ChatMenu`, `LocationPicker`, `MotionToggle`; `ChatHeader`, `CreditsMenu`, `LanguageMenu`, `ProfileMenu`; `ChatSearch` |
| `Components/Chat/Composing/`, `Turns/`, `Panels/` | `MessageBox`, `ComposerTools`; `UserActions`, `AnswerActions`; `WorkPanel`, `WorkItem`, `WorkStep` |
| `Shell/` | The frame's state as plain classes: `ChatRenaming`, `ChatPreferences` (with `Location/`: `LocatedPlace`, `LocationProblem`), `ChatLanguages`, `ChatCosts`, `ModelNames` |
| `Search/`, `Content/` | `ChatFinder`, which matches the search; `Source` and `Sources`, a turn's sources |
| `Styles/web/` | The globals: `settings/_colours.scss`, `tools/` (`_colours.scss`, both forms of a colour from one value; `_screen-answer.scss`, a mixin two screen components include), and the shared rules by concern in `base/`, `layout/`, `chat/`; a component's own rules sit beside it (`Components/Admin/AdminShell.razor.scss`, `Components/Chat/Sidebar/ChatSidebar.razor.scss`, CL58) |
| `Components/Editing/` | Pieces for a page a person edits (F142). The framework decides nothing about how a site edits its pages. louis-agent.dev builds its CV's editor from them ([ADR 0122](../adr/0122-the-sites-cv-is-edited-on-its-own-page.md), the site's own): `TextField` and `FieldGroup` (a labelled box, a few under a heading), `TagInput` (words as chips), `ListEdits` (add, take out, replace, move, each a new list) and `EditedItem` (an item with the way to hand back a changed copy). `Inline/` is for a page edited where it stands: `InlineEdit` (click the words, a box with a tick and a cross, Enter keeps, Escape drops, and plain words when nobody listens), `InlineForm` (the same for an item of several fields), `ItemTools` and `ItemAdd` (move, remove, open and add for a list on the page), and `PartChanges`, which gives a part a listener exactly when the whole has one |
| `Scripts/` | `JsModule`, the proxy that imports a module once and lets it go; `FrameworkScripts`, the modules' paths named once; `ChatModules`, the frame's three modules on one runtime (CL31) |
| `wwwroot/js/` | `first-paint.js` (the one script a site lists), `preferences.js`, `dialogs.js`, `download.js` (shared modules); a module beside its component sits with it (`Components/Chat/Shell/ChatShell.razor.js`, `Components/Chat/MessageList.razor.js`, `Components/TurnView.razor.js`) |
| `Text/` | `ChatText` (the components' words, English by default), `StepMarks` (a work step's mark), `ToolArgs` (a tool call's input on one line), `Markdown` |
| `Usage/` | `TurnUsage`, `UsageFormat`, `CurrencyState`, `CurrencyChoice`, `RatesTable`, `RatesDisplay` |
| `Storage/` | `ChatStore`, `ChatSettings` |

**`Components/Widget/ChatWidget`** (F109.3): a chat any page or layout drops in with one tag, `<ChatWidget
Identity="…" />`: a button folded in a corner and a panel with the chat's own `MessageList` and `Composer` on a
`ChatRunner` of its own, opened as the identity named, its chats kept under a key of their own so a site's chat
page never lists them; the words (title, greeting, placeholder, the button's text and labels) are parameters.
A site's frame (`SiteShell`) has a `Corner` slot made for it. It knows the page (F115, ADR 0104): its module beside
it, `ChatWidget.razor.js`, reads the page's address, title and main text when a message is sent, and the runner
sends them with the first message written from a page, so the guide answers about what the visitor sees. The block lists the page's areas, and the widget acts on the guide's `ShowPage` call (F117, ADR 0105): `ChatRunner.ToolCalled` tells it of each tool call as it arrives, it navigates to the path and the module's `showArea` scrolls the area into view and lights it up with `.area-shown`, a glow in the web's base styles a site may restyle.

**`Components/Site/Bands/Band`** is one named area of a site's page (F117, ADR 0106): a section with a required id, an optional kicker and the site's classes, drawing no style of its own, so every area on every site is named because the framework insists and the guide can send a visitor to it. A band whose content is one piece, such as a demo that plays, sets `Paged="false"` and the frame's module never pages it sideways (F122).

**`Components/Site/Cards/CardDeck`** is a row of short cards that open (F113): each `Card` inside gives a title, a
one-line summary, an optional mono tag and, as its content, the story. The cards register with the deck through a
cascading value (the tab-set pattern) and draw nothing; the deck draws the summaries as buttons in a row and, under
the row, the open card's story, which the styles beside it slide in once. One open at a time, a click on the open one
closes it, a phone stacks the row. `CardDeckTests` holds the row, the click, the swap and the golden.

**`Components/Site/Menu/SiteMenu`** is a group in a site's menu (FX73): a label in the bar that opens the links it holds
on hover or on focus, CSS only; the one at the end of the bar opens its list right-aligned, so it stays on the screen
(F122).

**`Components/Graphs/GraphView`** draws a diagram from data (F111, the rules in [ADR 0103](../adr/0103-a-diagram-is-data-on-a-row-grid.md)):
a page declares a `GraphSpec` (in `louis-agent.diagrams`' `Flow/` since CL60.1, plain C# the docs' tool draws with
too: nodes with an id, a label, a note and a column; edges between ids, with a word when one helps), `GraphLayout.Place` puts the columns across the width on one row grid, orders each by its
neighbours and hands the arrows to `EdgeRouter`, which draws each as a connector (right-angle bends, one track per
gap, back arrows under the drawing, loops to the right, words on flat pieces with room, each gap as wide as the words
that cross it and the bend just after a word, F119), and the view writes one SVG
that scales to its box. Its root carries `reveal`, so the frame's module adds `.in` as it enters and the styles beside it make each arrow
draw itself and each box rise, a column at a time, once; fully drawn at rest. The SVG's text goes out as markup from
the code-behind, since Razor keeps the `text` tag for itself. `GraphLayoutTests` holds the numbers, `GraphViewTests`
the markup.

**`Components/Recorded/RecordedChat`** plays a recorded session in the chat's own view (F120,
[ADR 0108](../adr/0108-the-public-watches-the-coding-agent-recorded.md)): a site gives it a `.sse` file of the stream
the API sent, the question it answered, a title, the currency for the bill and an ending, and a visitor watches the
agent think, call its tools and answer, then sees the ending and a play-again button. It reads the file with
`SessionStreamReader`, waits by each event's kind (`Chats/Replay/RecordingPace`: thinking fast, the answer at reading
speed, a beat per tool) and folds the events with `Chats/Replay/TurnReplay`, the loop the eval studio's replay shares,
into a `MessageList`. Its window is the framework's `web/tools/_window` mixin, which a site's own animated panels use
too, and keeps one height: the list gives up room for the ending, whose guide button a site can make plain
(`GuideButton Class="is-plain"`, F122). `RecordedChatTests`, `RecordingPaceTests` and `TurnReplayTests` hold it.

**`Components/Site/SiteShell`** puts the brand on the left and the menu with the actions on the right, and carries its own
motion as a module beside it (F110, `SiteShell.razor.js`, attached after every render): the progress line's width
(`#site-progress`), the header compact past the top, the section index's active link (`.site-index a`), an in-page link
that lands its section centred, and a reveal each `.reveal` block plays once when a quarter of it enters, all honouring
the system's less-motion setting unless the viewer switched motion on. A site marks what moves and holds the look
(the keyframes, the index, the line) in its own partials, so a site chooses its motion; `SiteShellTests` holds the
attach and `FrameworkModulesTests` the module's path and exports.

## Configuration

None. It calls the API on the same origin as the app; the API key, when the API asks for one, is typed into the app
and kept in that browser.

## Extending it

The steps are in [Change the framework's web](../guides/change-the-web.md). In short: a piece belongs here
when it would be the same on every site; a new API call goes in `AgentApi`; a component's words go in `ChatText` (English
default) and in Puls's `PulsText`; markup only in a `.razor`, logic in a plain class with tests
([ADR 0034](../adr/0034-code-a-person-can-maintain.md)). Then rebuild the image of each app that uses it.

## Tests

`src/tests/web/louis-agent.web.tests`, NUnit with bUnit for the components, mirroring the folders: `ChatRunnerTests`,
`TurnStreamTests`, `DelegationTests`, `WorkStatusTests`, `SpecialistAnswerTests`; `TurnViewTests`, `BlocksViewTests`,
`DelegationCardTests`, `CurrencySwitcherTests`; `DrawnCardTests` and `OfferCardsTests` in `Components/Blocks/`; `AdminShellTests`, `ChatShellTests` and a test for each part of the chat's frame; the studio's in `louis-agent.web.evals.tests` (CL57); `Pages/Admin/`;
`MarkdownTests`, `ToolArgsTests`, `UsageFormatTests`; and
`RazorHasNoCodeTests`, which fails on a `@code` block in a `.razor`. `src/tests/web/louis-agent.web.testing` holds the fakes the
web test projects share.

## Limits and plans

- **Left out on purpose:** a landing page, as it's what makes a site itself; a NuGet package for the framework or the template, as the template works
  inside this repo until packages exist; a screen served as an MCP App, as no plan names it; bulk selection, and field kinds such as dropdowns or dates,
  until a second screen needs them; swapping a view without a rebuild, since a custom component is built in, though a description isn't.
- `Chats/` holds eleven files and `Components/Chat/` ten, over the "about six" of
  [ADR 0034](../adr/0034-code-a-person-can-maintain.md): split them by concern (the records apart from the stream's
  folding) the next time they change.
- `UsageFormat` repeats core's `UsageText` number rules, so a change to one must be made in both (see
  [louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)).

## Related docs

[louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md) · [louis-agent.puls](../../../demos/puls/docs/projects/louis-agent.puls.md) · [louis-agent.api](louis-agent.api.md) · [The eval pages](louis-agent.web.evals.md)

---
[Projects](README.md) · Previous: [louis-agent.code-review](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md) · Next: [louis-agent.mcp](louis-agent.mcp.md)
