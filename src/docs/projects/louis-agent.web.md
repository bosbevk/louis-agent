# louis-agent.web — design

> **Kind:** Razor class library (`net10.0`, runs in the browser) · **Path:** `src/web/louis-agent.web/` ·
> **References:** Markdig, Net.Codecrete.QrCodeGenerator, Microsoft.AspNetCore.Components.Web and .WebAssembly · **Used by:** every site with a chat:
> louis-agent.code-review, louis-agent.puls and louis-agent.starter

![The framework's web, and its parts: the chat's frame, the admin's frame, the site's parts, styles and scripts, and the seams](diagrams/louis-agent.web.svg)

## What it is for

This library is the framework's web: what every site built on the framework shares. That is the chat's frame, the
admin's frame, the screens a pack brings, the frame of a site's public pages and its showroom of demos. Under them
it is everything a chat needs to work the same on each site: the chat as data, the client that calls the API, a
turn's life from the first message to the saved answer, the views of a turn and of a specialist's work, markdown,
usage and currency.

A site holds what makes it itself. For now that is its colours and its brand, with its own words and pages. The
rules and the scripts of the frames are here.

Two rules let the same parts fit any site. A component only gives its elements class names, so how it looks comes
from the site's colours. And a component's words come from one `ChatText`, English by default, which a site can
replace.

**How this doc is laid out.** Part 1 is how a site says what it has. Parts 2 to 5 each follow one person: someone
who chats, someone who reads the site, someone who watches a demo, and the owner who opens the admin. Part 6 is
what a site can opt into, part 7 how it looks and moves, and part 8 the general parts any page can use. After the
pictures, *Where things are* goes folder by folder, for looking a file up.

*Decided in:* [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md).

## 1. A site says what it has

A site never builds or registers the framework's parts itself. It names its **areas**, in one call, and the
framework does the rest. This is the framework's own site:

```csharp
builder.Services.AddSiteAreas(areas => areas
    .Showroom(SiteDemos.All)
    .Focuses(Focuses)
    .Admin(typeof(AdminLayout)).Overview(count => count.Tool("SendLead", "leads")));
```

`AddSiteAreas` does three things, in this order.

**First it registers the chat's plumbing, for every site.** The settings kept in the browser (`ChatSettings`), the
store of chats (`ChatStore`), the client that calls the API (`AgentApi`), the currency (`CurrencyState`) and an
English `ChatText`. Each is registered only where the site set none of its own.

**Then it registers each area the site named.**

- `Chat()` adds the chat portal's handoff. That is how a Review or Ask button on an admin screen reaches the chat:
  it opens the site's chat with the words as a draft. It can't send them, so the person always reads the prompt and
  sends it themselves.
- `Admin(layout)` adds the admin's frame, its home and its pages. Part 5 explains them.
- `Showroom(demos)` adds the demos, the guide's way in, the demos' windows and the kept guide. Part 4 explains
  them.
- A library adds an area of its own through `Add`. The eval studio's `Evals()` and the accounts' `Accounts(layout)`
  do that.

An area named twice counts once.

**Last, it keeps one record of what the site has.** `SiteAreas` is registered once. It gives the router its page
assemblies, and it gives the admin's shell each area's admin links.

`Focuses(f => …)` names the part of the site each page is. A page's part is the longest address that matches it,
compared by whole segments, and the default when none matches. The guide's widget sends that part with every
message.

`SitesNameTheirAreasTests` holds the rule that a site names its areas this way. The code is in `Components/Areas/`,
and the focuses in `Components/Areas/Focuses/`.

*Decided in:* F127.6, F127.7, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md).

## 2. Someone chats: one message, from start to finish

### The frame around the chat

`ChatShell` is the whole chat: the sidebar, the phone's top bar, the messages, the message box, search, the toast
and the work panel. A site writes a small page with its addresses and fills the frame's **slots**: `Logo`, `Views`,
`Profile`, `Background`, `Empty`, `ViewContent` and `UsageLine`. An empty slot leaves no gap. With no `Empty`, a
new chat is just the message box.

The site also says how a session opens (`OpenSession`), since the toolsets it asks for are its own. The frame hands
it what it knows, the voice, the place and the time zone, as a `ChatOpening`.

A site can reach into the frame for two things: to put a draft in the message box, and to say something is coming
soon.

What the frame keeps that can be tested without drawing it is a plain class in `Shell/`: renaming a chat, this
browser's settings, choosing a language, a chat's costs. `ChatShell` holds these and only passes them on.

### What happens when a message is sent

![Sending a message: ChatRunner opens the session the first time, applies each event to the turn, redraws the page and saves the chat](diagrams/louis-agent.web-diagrams-2.svg)

Follow one message through four steps.

1. **One runner holds the turn's life.** `ChatRunner` holds what every chat page does with a message. It keeps the
   draft and its attachments, and the page the visitor is reading when one is set, which is sent once and then
   forgotten. It opens the session on the first message, streams the answer, can stop it, saves the chat, and puts
   errors in the app's own words.

   While the answer streams, it raises a redraw at most every 50 ms and always draws the last update. The last
   update matters. Without it, an update that comes just after a redraw would wait for the next event, which can
   be half a minute away while a tool runs. The page would sit behind and then seem to print everything at once.
2. **One client calls the API.** `AgentApi` is the only code that calls it. It opens a session with its toolsets,
   role, place and time zone. It streams a message's events over SSE. It stops and deletes sessions, and it reads
   the toolsets, the rates and the other GETs. Components never touch `HttpClient`.
3. **The stream becomes a turn.** `TurnStream` folds the API's events (thinking, text, tool calls, usage) into the
   `Turn` and its `Block`s, which is what the page shows. A sub-session's events go to its `Delegation`, so a
   specialist's work appears inside the turn that called it. `WorkStatus` and `RolePhases` say what the specialist
   is doing, and `SpecialistAnswer` splits its turn into its work and its answer.
4. **The views draw the turn.** `TurnView` and `BlocksView` show one turn: the message, thinking that folds away,
   text and tool cards. `DelegationCard` shows a specialist's work, live, with its cost. `MessageList`, `Composer`,
   `ComposerButtons`, `ChatUsageBar` and `Notice` are the chat pieces both apps lay out, with slots for an app's
   own parts.

### An answer can hold more than words

**A card, or a set of choices.** The site pack has two tools for this, `ShowCard` and `Offer`. They draw nothing
themselves. The chat reads each call and draws it, also when the agent's work is hidden. A card is drawn as the
diagrams library's ticket, held inline with its styles kept to itself. An offer's choices are drawn as buttons. An
answer's cards go under its words, whatever order they were called in.

A click on a choice goes up to the chat that owns the runner, which sends the choice's label as the visitor's
message. Each turn is handed the next one's words as its answer, and a finished turn draws again when it gets one.
That is how a turn's cards fold to the one chosen. A failed call draws nothing: the pack's refusals start with
"Error".

**Another agent's answer.** The guide can ask another agent with `AskAgent`. The stream then gives the call that
agent's session, taken from the nested `message_start`. A window on the page can say which agent it shows. The
guide's widget hands such a window an ask to its agent. That marks the call as shown in a window, so the guide's
chat draws one line instead of the talk, and the widget tells the window each time the stream moves on. On a narrow
screen the window is brought into view and the guide folds to its head.

The showroom's live chat is such a window. It watches the ask as one turn: the visitor's words and the answer, its
blocks shared with the guide's stream, and the agent's tool calls handed on once each. Once the ask ends, it holds
a chat on that agent's own session, so the visitor carries on there.

### Around every chat: its cost, its safety and where it is kept

**The cost is shown.** `TurnUsage` and `UsageFormat` show a turn's tokens and cost from the API's usage event.
`CurrencyState` and `CurrencySwitcher` show the cost in the browser's chosen currency, from the API's rates.

**What a model writes is made safe.** `Markdown` is the one Markdig pipeline. Raw HTML is shown as text. Links are
only `http(s)` and `mailto`. Images load only from the shops' own image hosts, or when a tool gave that exact image
in the same turn. So a model-written image can't send the chat's data elsewhere.

**Nothing is kept on the server.** `ChatStore` keeps chats in the browser's `localStorage`. `ChatSettings` keeps
the API key and the admin key there too.

### The same chat, in a corner of any page

`ChatWidget` is a chat any page or layout drops in with one tag, `<ChatWidget Identity="…" />`. It is a button
folded in a corner and a panel with the chat's own message list and composer. It has a runner of its own, opened
as the identity named. Its chats are kept under a key of their own, so a site's chat page never lists them. Its
words (the title, the greeting, the placeholder, the button's text and labels) are parameters. A site's frame has
a `Corner` slot made for it.

The widget knows the page. A script module beside it reads the page's address, title and main text when a message
is sent, and the runner sends them with the first message written from a page. So the guide answers about what
the visitor sees.

*Decided in:* F109.3 (the widget), F115 and ADR 0104 (it knows the page), F127.4.1 (cards), F127.4.2 and ADR 0117
(one agent asking another), FX22 (a tool's own images),
[F14](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F14-display-currency.md) (currency).

## 3. Someone reads the site: public pages

The engine and an agent run without any of this: the command line, Rider, the API and MCP need no web. This part
is what turns an agent into a website.

**The frame.** `SiteShell` is a header, the page, a footer and the guide in its corner. It adds a scroll progress
line, a way back up, and a section index on the left. The index is built from the page's bands: each band puts
itself on the frame's list as it draws.

**What a page is built of.** A page is built of `Band`s, each a named section, and the framework's `SiteHero`,
`TileRow` and `Tile`, `CardDeck` and `SiteStrip`, each with the site's words. A `GuideButton` anywhere asks the
guide a question.

**Slides, or a flowing page.** A site lays its pages out as one or the other, and a page may choose the other with
one line. On slides every section is a screen tall with its content in the middle, and the page settles on one
section at a time. A section with more than a screen of content pages sideways, with a pager. A diagram scales to
fit.

**Every place has its own address.** A section is `/proof/rand`. A card is `/identities/puls`. A page of a paged
section is `/how-it-works/parts/2`. The landing's places sit under `/home`. `SiteAddresses` is the rule, and
`SectionPage` is the catch-all route that opens any place. The address bar follows the visitor. A place's address
opened afresh draws in its own page's layout.

**The guide knows the page, and can go to a place on it.** The guide's widget sends the page it reads with every
place by its address, and the place on the visitor's screen. The guide's page tool takes an address. The widget
then scrolls there, opens a card or turns a page, and lights the place with a spotlight that dims the rest.

**Every part is ready for an agent.** A tile with a title or a number, and a deck's card, are **pieces**. Each has
an id, so `/home/100-adrs` opens it. Three things select a piece: a click on it, which asks the guide about it. A
card the visitor opens. And a piece the guide shows. The last two select it with nothing sent. A selected piece is
drawn pressed, a chip in the guide's box names it, and every message carries it until the chip is closed.

**Found by search.** A site lists every page and place in its `sitemap.xml`, which a site test writes from the
rendered pages and holds to them. A page meant for search declares a `PageHead` beside its title: its description
and any structured data, in one head, since Blazor keeps only the last one a page writes. The facts are schema.org
records (an organisation, a website, a piece of software, a breadcrumb), written as JSON-LD. The API serves each
page as HTML with its own head and words, for crawlers that run no script.

*Decided in:* F118, F119, F121, F123, F127.1, F127.7, ADR 0106,
[ADR 0107](../adr/0107-every-section-has-an-address.md), ADR 0109,
[ADR 0110](../adr/0110-a-page-says-what-it-is-in-one-head.md),
[ADR 0121](../adr/0121-every-part-of-the-web-is-ready-for-an-agent.md).

## 4. Someone watches a demo: the showroom

The showroom is the area a site shows its demos in. Its code is under `Components/Site/Showroom/`, in four
folders, and this part takes them in the order a visitor meets them.

**The frame** (`Shell/`). `ShowroomShell` is a slim bar, a rail of the demos grouped by what a visitor comes for,
and the corner beside the frame. On a phone the rail becomes a row of chips. The whole frame is in the colour of
the demo on screen. An open guide takes the rail's place. Each demo names its icon, one of the framework's line
icons, which the rail, the chips and the start draw.

**The start** (`Start/`). `ShowroomStart` is the showroom's front page: a big start for the main demo, then a
cover for every demo.

**A demo's page** (`Stage/`). `DemoStage` lays one out: a kicker, the page's one h1, a lede, the player, the cues,
the notes and the offer. The cues are lit in step with the play, and each is a button that plays from its turn.
The notes say what a custom agent could add. Each part is a section with its own address.

**A business's website, as a mock** (`Mock/`). `SiteMock` is a browser bar, the business's sample brand, a page
that scrolls inside the mock, and its agent's widget in the corner. The page comes in one of three looks, with a
hero photo worn with the brand, facts and its places. A site writes each page of the mock, and each registers with
it. The page an address is on is the longest page path it starts with. A visitor clicks through the menu, and a
page change loads like a browser. A card with a page of its own opens it.

The mock's header wears its brand. Its font (`MockFont`) sets three faces, `--mock-face`, `--mock-text` and
`--mock-caps`. Its header (`MockHeader`) is one of three treatments, and `MockColours.Label` is the colour of its
small capitals. The faces are the framework's own woff2 files under `wwwroot/fonts/showroom/`, each beside its
`OFL.txt`. A site lists them, so a page fetches only the faces it wears.

**The play moves the mock.** A recorded chat tells its page each tool call as it plays. So a mock goes to the
place its agent showed: a section under the guide's spotlight, or a card pressed.

**Tried live.** The business's agent can also be live in the mock's widget, opened as its identity. It sends the
page's words, every page's places by address and the menu as links with each new page. The widget then says Live,
the hint is a question to tap, the cues wait, and no usage line shows. The mock's chat and the guide's minimise
and maximise with the same buttons.

*Decided in:* F127.1, F127.2, F134.1 (the icons),
[ADR 0115](../adr/0115-a-sites-showroom-is-an-area-of-the-framework.md),
[ADR 0116](../adr/0116-a-showrooms-demos-can-be-tried-live-through-the-open-door.md).

## 5. The owner looks in: the admin

**The admin is a frame its areas plug into.** `Admin(layout)` is the frame alone: `AdminShell` around the site's
layout, and the home at `/admin`. Everything inside is an area with its pages, its sidebar links and its card on
the home. The home draws each area's card in the order the site named them, in two columns, one on a phone.
"Home" is first in the sidebar.

Three areas come with the framework.

- **The overview**, `Overview(count => count.Tool("SendLead", "leads"))`, names the tools it counts. The API
  counts every tool and names no pack. Its page at `/admin/overview` draws the API's overview in the reader's
  currency: today's and the month's talks, their cost and the counted tools, the month's talks by part of the
  site and by who answered, in one colour, and the latest talks, in a table that scrolls sideways on a phone.
- **The packs**, `Packs(views => …)`, are the packs' screens and uploads, with their menu and the packs page at
  `/admin/packs`.
- **The eval studio**, `Evals()`, is at `/admin/evals`.

An admin area named without the frame is refused at start-up.

**Every card reads the same way.** Each card loads the key and reads. When the API refuses, it tells the home, so
the home asks for the key once, and a saved key makes every card read again. The cards share a base class for
that (`AdminCardBase`, a Template Method), and the refusal goes through `AdminAccess` (an Observer).

**The frame has slots.** `AdminShell` is the sidebar, the phone's top bar and the content column. A site's small
layout fills its slots: `Brand`, `Links`, `Top` and the page, plus the way back. An empty slot leaves no gap. A
framework page names `AdminFrame`, which draws inside the layout the site set. The frame's words are English on
every site.

*Decided in:* F127.8, F144, [ADR 0120](../adr/0120-nothing-is-hand-wired-from-the-framework.md).

## 6. What a site can opt into

**Accounts, through one seam.** The account pages and their client are
[louis-agent.web.accounts](louis-agent.web.accounts.md), a library on top of this one that a site opts into. All
this library knows of accounts is `IAccountMenu`, declared beside the chat's profile menu: are there accounts, who
is signed in, the two labels, load, changed, sign out. The menu asks for it once. When a site registered nothing
it holds `NoAccounts`, a Null Object, so the guest's menu draws with no branch for that case.

Every call to the API still says it is the app's own, with an `X-Requested-With` header. That is what lets a
signed-in person's cookie count in place of a key.

**The eval studio** is [louis-agent.web.evals](louis-agent.web.evals.md), a library on this one that a site opts
into the same way. The router lists what a site opted into.

*Decided in:* CL30, CL57, [ADR 0089](../adr/0089-the-account-client-is-a-library-a-site-opts-into.md).

## 7. How it looks and moves

**A component's styles sit beside it.** A rule that one component draws is in its `X.razor.scss`. The build scopes
it and bundles it into the site's `{site}.styles.css`, which the site links once. The globals are Sass under
`Styles/web/`: what every page shares, what a site's own pages draw too, and what a theme overrides. A site lists
them in its own `app.scss`, so it gets one CSS file in the order it chose.

**Colours go by name.** They use Bootstrap's names, or a `--web-` name where Bootstrap has none, and no rule holds
a fixed colour. `web/settings/_colours.scss` is what a site with no colours of its own gets: the Console theme,
black and white with metallic greys, plus a muted red and an amber. A site's colours come after it and win.

**A script is a module beside its component**, or shared under `wwwroot/js/`. Each is imported the first time its
component draws and let go when the component is disposed. The paths are named once, in `FrameworkScripts`. A
script names a problem, and the site's `ChatText` says it. `FrameworkModulesTests` holds that rule.

One script is listed by the site itself, `first-paint.js`. It applies what the browser remembers before the first
paint, so nothing jumps: the sidebar's width, whether it or the work panel is hidden, and whether the page may
move although the system asks for less.

*Decided in:* CL31, CL58, [ADR 0093](../adr/0093-a-script-is-a-module-beside-its-component.md),
[ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md).

## 8. General parts for any page

Some parts know nothing of chats, demos or admins, so any page of any site can use them.

**Line icons** (`Components/Icons/`). `LineIconView` draws one icon as inline strokes, in the colour and at the
size of the words around it. A screen reader passes over it. The icons are the diagrams library's one set, so a
page and a diagram never hold two copies of a drawing.

**A stage, and what stands on it** (`Components/Staging/`). Four parts, each usable alone.

- `LitStage` puts a light from above, a pool of it on the floor and the floor's lines behind whatever it holds.
  Its colour is an accent. With none, it is the colour of the place it stands in. The colour changes slowly when
  it changes, and at once for a visitor who asked for less motion.
- `Deck` draws any items through the caller's template: one forward in the middle, the rest behind it in depth.
  An arrow at each side, the keyboard's arrows, or a press on an item behind asks for another to come forward.
  What an item behind holds can't be reached until it is forward. `DeckPlace` is the deck's one rule: how far from
  the middle each item stands, counted the short way round.
- `FramedWindow` is a bar like a browser's, with an address word, then a body. The body can't outgrow the frame or
  be scrolled inside it. The framework offers two bodies, which hold no words of their own. `SiteBody` is a
  website with its chat: a photo with a name and a line over it, and one question, its answer and one thing the
  agent showed. `WorkBody` is panes of work, each a heading over plain lines.
- `Dock` is a row of icon buttons drawn from `DockItem`s, with a divider where the group changes. The current
  button shows its name, and the rest keep theirs for a screen reader. Each button carries an id and is marked
  as a card of the page.

The deck and the dock share one idea: **the page owns the choice.** Which item is forward or current comes in from
the page, and the part only says what the visitor asked for.

**Paper** (`Components/Paper/`, `Paper/`). `PaperLink` is a link as it is printed: its QR code, its words and its
address written short. A link on paper can't be clicked, so this is drawn in its place when the page is printed,
and not on a screen. The code is the link as well, so a PDF read on a screen can still be clicked.

**Editing** (`Components/Editing/`). Pieces for a page a person edits. The framework decides nothing about how a
site edits its pages. There is a labelled box and a group of them under a heading, words as chips, and list edits
that each give a new list. `Inline/` is for a page edited where it stands: click the words, a box with a tick and a
cross, Enter keeps and Escape drops.

*Decided in:* F134.1 (icons and the stage), FX154 (paper), F142 (editing).

## The pictures

The [eval pages](louis-agent.web.evals.md) have pictures of their own.

![The framework's web: a site's pages in the chat's and the admin's frames, and the API they call](diagrams/louis-agent.web-diagrams.svg)

![The types: shells for a site's frames, one runner for a chat's turn over the agent API, the chats kept in the browser and an address for every place](diagrams/louis-agent.web-interface.svg)

## Where things are

This part is for looking a file up, not for reading through. Each row is a folder and what is in it. The notes
under the table say more about the larger components.

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
| `Components/Icons/` | `LineIconView`, a line icon from the framework's one set (part 8) |
| `Components/Staging/` | `LitStage`. `Decks/`: `Deck`, `DeckPlace`. `Windows/`: `FramedWindow`, `SiteBody`, `WorkBody`, `WorkPane`. `Docks/`: `Dock`, `DockItem` (part 8) |
| `Components/Paper/`, `Paper/` | What a page needs when it is printed (FX154, Louis: "in a pdf hyperlinks is qr codes always"): `PaperLink`, a link as it is printed, its QR code with its words and its address written short, drawn on paper only, in the ink of the page that prints it, and still a link in a PDF read on a screen; `QrImage`, a code as a picture; `QrDrawing`, a code as data (the one place that names Net.Codecrete.QrCodeGenerator), and `PaperAddress`, an address written short |
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
