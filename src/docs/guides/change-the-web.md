# Change the framework's web

> **How-to.** Change `src/web/louis-agent.web`, the Razor class library every site is built on: decide what belongs here,
> add an API call, follow a new event in the stream, add a component, give it words, and test it. **Elsewhere:** how
> it works in [louis-agent.web](../projects/louis-agent.web.md); the eval pages in [Change the eval pages](change-the-eval-pages.md); the two apps in
> [Change the code-review web app](../../../demos/louis-agent/docs/guides/change-the-web-app.md) and [Change the Puls web app](../../../demos/puls/docs/guides/change-the-puls-app.md).

The library holds what makes a chat work the same in both apps: the API client (`Api/`), the chat as data and a turn's
life (`Chats/`), the views of a turn and of a specialist's work (`Components/`), safe markdown (`Text/`), usage and
currency (`Usage/`), and what the browser keeps (`Storage/`). Each app keeps its own pages, look, JavaScript and words.

## Does it belong here?

Ask whether it would be the same on every site. If so it belongs here, even if only one site uses it today; what differs comes
in from the site, and the framework names no site ([ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md)). A
component's own rules sit beside it in `X.razor.scss` and travel with it ([ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md));
the globals are the framework's Sass under `Styles/web/`, which a site lists in its `app.scss`.

## Change the chat's frame

`Components/Chat/Shell/ChatShell.razor` is the frame, and each part is a component in the folder of its concern. Something every site
should get is a change here. Something that differs per site is a slot or a parameter on `ChatShell`, which the site fills, never a check
of which site this is. A new word goes in `ChatText` in English. State the frame keeps goes in a small plain class in `Shell/`, with its
own test. A site's page redraws for its own reasons, which sets the frame's parameters again, so `OnParametersSet` there can't assume
the address changed.

## Change the account pages

The sign-in, registration, confirmation, reset and profile pages are the account library's
(`src/web/louis-agent.web.accounts`, [its doc](../projects/louis-agent.web.accounts.md)), the same on every site that
opts in. **Opting in** is a project reference, `@using LouisAgent.Web.Accounts.Components` in `_Imports.razor`,
`AddLouisAgentAccounts()` in `Program.cs`, `AccountLibrary.Assembly` in the router's `FrameworkPages.With(...)` (CL57: a
library a site adds services for is in its router, or its pages answer nothing; `SiteRouterTests` holds it); its styles travel in the site's
bundle, so `app.scss` lists nothing for it (CL58); the starter has all of it. The passkeys script is a module the components import
themselves (CL31), so `index.html` lists nothing for it. A site then changes
three things only: **its words**, a copy of `AccountText` registered before `AddLouisAgentAccounts()` (Puls:
`PulsAccountText.Afrikaans`); **its frame**, a small `AccountLayout` on `AccountShell` with its brand in the slot,
named in `SiteFrames`; and **whether it has accounts at all**, which is the API's `ACCOUNTS_SQL_URL`, not the site's.
A new field or section goes in the library: a component under `Components/` with its bUnit test in
`louis-agent.web.accounts.tests`, its call in `Client/AccountApi`, and its words in `Text/AccountText`. What the chat
needs from accounts comes through `IAccountMenu` in the framework's web, and nothing else does. The server side is
`louis-agent.accounts` ([its doc](../projects/louis-agent.accounts.md)).

## Change the styles or scripts

A rule one component draws goes beside it, in `X.razor.scss` next to `X.razor`: the build compiles it scoped and bundles it, and
nothing is listed anywhere ([ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md)). A rule several components draw goes in their
nearest common ancestor's file under `::deep`. A rule every page shares, one a site's own pages draw too, or one a theme overrides goes
in a partial under `Styles/web/`, with colours from variables only (`FixedColourTests` and `FrameworkStylesTests` hold it). What
repeats becomes one piece under `web/tools/` ([the rules](../../../.claude/rules/css.md)). A component that needs the browser gets an ES module beside it, `X.razor.js` with `export function`s and no global
([ADR 0093](../adr/0093-a-script-is-a-module-beside-its-component.md)); what several components share is a module under
`wwwroot/js/`, its path named once in `Scripts/FrameworkScripts`. The component holds a `JsModule` on that path, calls its
functions and lets it go in `DisposeAsync`; a button calls a method with `@onclick`, never a global inline. A test plans
the module with bUnit's `SetupModule` and sees the calls. `FrameworkModulesTests` holds it: every module a class names is
on disk, every function it invokes is exported there, nothing but `first-paint.js` writes to `window`, and a site's
`index.html` lists only `first-paint.js` of ours. A script holds no words for the person: it names the problem, and
`ChatText` says it.

## Add an API call

`Api/AgentApi.cs` is the only code that calls the API. Add a method there for a call both apps make, using
`GetAsync<T>`, `PostAsync` or `StreamLinesAsync` rather than a new `HttpClient` call. A call one app makes is an
extension method beside what uses it (the git pack's views have `AgentApiGit`). A reply that isn't JSON comes back as the same error, so a page shows it. A failed call throws
`AgentApiException`, which `ChatRunner` turns into the app's words.

## Follow a new event in the stream

1. Map the event to its record in `src/engine/louis-agent.session-stream/SessionEventMapper.cs` (and its record in `SessionEvents.cs`), with a test
   in `src/tests/engine/louis-agent.session-stream.tests`. The reader hands the web apps `SessionEvent`s, so the event exists there first.
2. Write the test first in `src/tests/web/louis-agent.web.tests/Chats/TurnStreamTests.cs`, with the event as the API
   sends it ([the event stream](../AGENT_COMMUNICATION.md#the-event-stream)).
3. `Chats/TurnStream.cs` folds the events into the `Turn` and its `Block`s. A specialist's events arrive wrapped in
   `sub_session` and go to that call's `Delegation`. Add the case there, and keep an unknown event harmless: it is
   ignored, never an error.
4. If a view must show it, add it to the `Turn` or `Block` record and to the view (`TurnView`, `BlocksView` or
   `DelegationCard`).

## Add a component

1. A `.razor` with markup only, and its C# in the `.razor.cs` code-behind, in `Components/` (or `Components/Chat/`
   for a chat piece both apps lay out). Logic that needs no component goes in a plain class with NUnit tests
   (`RazorHasNoCodeTests` fails on an `@code` block).
2. Its words go in `Text/ChatText.cs` as a property whose default is English. Puls sets its
   Afrikaans in `demos/puls/web/louis-agent.puls/Text/PulsText.cs`; add the property there too, or Puls shows English.
3. Leave slots (`RenderFragment` parameters) where an app adds its own parts, as `Composer` and `MessageList` do.

## Drop a chat into a page

`<ChatWidget Identity="the-identity" Title="…" Greeting="…" Placeholder="…" ButtonText="…" />` anywhere in a page
or a layout: a site's frame has a `Corner` slot fixed lower right for it. The identity is one the host's catalogue
runs (`GET /v1/identities`); a visitor with no key or sign-in reaches it only through the host's door
(`OPEN_IDENTITY`, `OPEN_SESSIONS_PER_DAY`, ADR 0099). A site wraps the tag once with its own words as a component
of its own, so a page places that and nothing is configured twice. `ChatWidgetTests` pins it folded and open.

## Markdown and links

`Text/Markdown.cs` is the one Markdig pipeline. It shows raw HTML as text, allows links only to `http(s)` and
`mailto`, and allows images only from the shops' own image hosts, so a model-written image can't send the chat's data
elsewhere. Widen any of these only with a test in `MarkdownTests` that shows the case is safe.

## The link card

A link to a site pasted anywhere (Facebook, LinkedIn, Slack, a chat) shows a card: a title, a line and a picture. The
crawler that builds it runs no script, so the card is read from the site's `wwwroot/index.html`, never from a page:
`<meta name="description">`, the Open Graph tags `og:title`, `og:description`, `og:url` and `og:image`, and
`twitter:card`. The picture and the address are absolute, on HTTPS (a relative path shows no picture); a large card
wants a picture of about 1200 × 630, a small one a square icon. `SiteLinkCardTests` holds every site to it; the
starter's page has the tags with placeholders to fill. A site's picture is its own file under its `wwwroot`, a copy,
never a link into the framework's docs (ADR 0074).

## Test it

```bash
dotnet test src/tests/web/louis-agent.web.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/web/louis-agent.web.tests   # after a deliberate markup change
dotnet test demos/louis-agent/tests/louis-agent.code-review.tests demos/puls/tests/louis-agent.puls.tests  # both apps render through it
```

The tests mirror the folders: `Chats/` (the stream, the runner, delegations, a specialist's status and answer),
`Components/` (bUnit, each pinned to an `.html` golden beside its test, from the recorded Puls turn in
`src/tests/engine/louis-agent.session-stream.tests/Fixtures/session-stream.sse`), `Text/` and `Usage/`. `src/tests/web/louis-agent.web.testing` holds what the three
web test projects share: `FixtureApi`, `SessionStreamFixture`, `Snapshots`, `FakeClock` and `RecordingJSRuntime`.

`Usage/UsageFormat` repeats core's `UsageText` number rules (the app doesn't reference core), so a change to one must
be made in both. Then rebuild the image of each app that uses the library.

## Before you ask for a review

- [ ] Both apps need it; nothing app-specific crept in.
- [ ] New words in `ChatText` and in `PulsText`.
- [ ] Both apps' tests pass, and every golden diff is the change you meant.

---
[Guides](README.md) · [Docs index](../README.md)
