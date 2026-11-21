# Change the framework's web

> **How-to.** Change `src/web/LouisAgent.Web`, the Razor class library every site is built on: decide what belongs
> here, change the chat's frame or the account pages, add a style or a script, add an API call, follow a new event in
> the stream, add a component, and test it. **Elsewhere:** how it works in
> [LouisAgent.Web](../projects/LouisAgent.Web.md); the eval pages in [Change the eval pages](change-the-eval-pages.md);
> the apps in [Change the code-review web app](../../../demos/louis-agent/docs/guides/change-the-web-app.md) and
> [Change the Puls web app](../../../demos/puls/docs/guides/change-the-puls-app.md).

![The framework's web at a glance: LouisAgent.Web holds the API client, the chat and a turn's life, the components, the text rules and the styles, and every site fills it with its own words and data](diagrams/change-the-web.svg)

This library is what makes a site on the framework work the same everywhere. It holds the API client (`Api/`), the
chat as data and a turn's life (`Chats/`), the components, from the chat's frame to a site's bands (`Components/`),
safe markdown and the chat's words (`Text/`), usage and currency (`Usage/`), what the browser keeps (`Storage/`), and
the shared Sass (`Styles/web/`). A site keeps only its own pages, words, brand and data.

## Does it belong here?

Ask whether it would be the same on every site. If it would, it belongs here, even if only one site uses it today.
What differs comes in from the site, as a parameter, a slot or an area the site names, and the framework never names a
site.

*Decided in:* [ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md),
[ADR 0125](../adr/0125-a-site-is-configured-framework-components.md).

## Change the chat's frame

The frame is `Components/Chat/Shell/ChatShell.razor`, and each part of it is a component in the folder of its
concern. Something every site should get is a change here. Something that differs per site is a slot or a parameter
on `ChatShell`, which the site fills, and never a check of which site this is. A new word goes in `ChatText`, in
English. State the frame keeps goes in a small plain class in `Shell/`, with its own test.

One trap: a site's page redraws for its own reasons, and that sets the frame's parameters again. So
`OnParametersSet` there can't assume the address changed.

## Change the account pages

The sign-in, registration, confirmation, reset and profile pages belong to the account library,
`src/web/LouisAgent.Web.Accounts` ([its doc](../projects/LouisAgent.Web.Accounts.md)), and they're the same on every
site that has accounts.

A site opts in by naming the area, `.Accounts(typeof(AccountLayout))`, among its areas, beside a project reference and
two `@using` lines in `_Imports.razor` (`LouisAgent.Web.Accounts` and `LouisAgent.Web.Accounts.Components`). That one
call registers the services, brings the pages to the site's router, and names the layout they draw inside. The pages'
styles travel in the site's bundle, and the passkeys script is a module the components import themselves, so neither
`app.scss` nor `index.html` lists anything for them. The starter has all of it.

After that, a site changes three things only. Its words are a copy of `AccountText`, registered in its `Program.cs`
before `AddSiteAreas`, as Puls registers `PulsAccountText.Afrikaans`. Its frame is a small `AccountLayout` on
`AccountShell` with its brand in the slot. And whether it has accounts at all is the API's `ACCOUNTS_SQL_URL`, not the
site's: without it, every account call is a quiet no.

A new field or section goes in the library: a component under `Components/` with its bUnit test in
`LouisAgent.Web.Accounts.Tests`, its call in `Client/AccountApi`, and its words in `Text/AccountText`. The chat learns
about accounts only through `IAccountMenu` in the framework's web. The server side is `LouisAgent.Accounts`
([its doc](../projects/LouisAgent.Accounts.md)).

## Change the styles or scripts

Where a rule goes depends on who draws what it styles. A rule one component draws goes beside it, in `X.razor.scss`
next to `X.razor`, and the build compiles it scoped and bundles it, so nothing is listed anywhere. A rule several
components draw goes in their nearest common ancestor's file, under `::deep`. A rule every page shares, one a site's
own pages draw too, or one a theme overrides, goes in a partial under `Styles/web/`. Colours come from variables only,
which `FixedColourTests` and `FrameworkStylesTests` hold, and a component reads the theme's names and never names a
theme. What repeats becomes one piece under `Styles/web/tools/` ([the rules](../../../.claude/rules/css.md)).

A component that needs the browser gets an ES module beside it, `X.razor.js`, with `export function`s and no global.
What several components share is a module under `wwwroot/js/`, its path named once in `Scripts/FrameworkScripts`. The
component holds a `JsModule` on that path, calls its functions, and lets it go in `DisposeAsync`. A button calls a
method with `@onclick`, never a global inline. A test plans the module with bUnit's `SetupModule` and sees the calls.

`FrameworkModulesTests` holds all of that: every module a class names is on disk, every function it invokes is
exported there, nothing but `first-paint.js` writes to `window`, and a site's `index.html` lists only `first-paint.js`
of ours. A script holds no words for the person. It names the problem, and `ChatText` says it.

*Decided in:* [ADR 0093](../adr/0093-a-script-is-a-module-beside-its-component.md),
[ADR 0094](../adr/0094-a-components-styles-sit-beside-it.md),
[ADR 0124](../adr/0124-a-sites-look-is-a-theme-and-a-style.md).

## Add an API call

`Api/AgentApi.cs` is the only code that calls the API. A call every site makes is a method there, built on
`GetAsync<T>`, `PostAsync` or `StreamLinesAsync` rather than a new `HttpClient` call. A call only one part makes is an
extension method beside what uses it, as the git pack's views have `AgentApiGit`.

A reply that isn't JSON comes back as the same error as a failed call, so a page shows it. A failed call throws
`AgentApiException`, and `ChatRunner` turns that into the site's words.

## Follow a new event in the stream

![Following a new event: map it in the session stream first, write the test in TurnStreamTests, fold it in TurnStream, then show it in a view](diagrams/change-the-web-flow.svg)

The API streams a turn as events, and the web folds them into a `Turn` the views draw. A new event has to exist in
the reader before the web can see it.

1. **Map it** to its record in `src/engine/LouisAgent.SessionStream/SessionEventMapper.cs`, with the record in
   `SessionEvents.cs` and a test in `src/tests/engine/LouisAgent.SessionStream.Tests`. The reader hands the web
   `SessionEvent`s, so the event exists there first.
2. **Write the test first** in `src/tests/web/LouisAgent.Web.Tests/Chats/TurnStreamTests.cs`, with the event as the
   API sends it ([the event stream](../AGENT_COMMUNICATION.md#the-event-stream)).
3. **Fold it** in `Chats/TurnStream.cs`, which builds the `Turn` and its `Block`s. A specialist's events arrive wrapped
   in `sub_session` and go to that call's `Delegation`. Keep an unknown event harmless: it's ignored, never an error.
4. **Show it**, if a view must: add it to the `Turn` or `Block` record and to the view (`TurnView`, `BlocksView` or
   `DelegationCard`).

## Add a component

1. **A `.razor` with markup only**, its C# in the `.razor.cs` code-behind, under `Components/` in the folder of its
   concern. Logic that needs no component goes in a plain class with NUnit tests. `RazorHasNoCodeTests` fails on an
   `@code` block.
2. **Its words** go in `Text/ChatText.cs`, as a property whose default is English. Puls sets its Afrikaans in
   `demos/puls/web/LouisAgent.Puls/Text/PulsText.cs`, so add the property there too, or Puls shows English.
3. **Slots** (`RenderFragment` parameters) go where a site adds its own parts, as `Composer` and `MessageList` have.

## Drop a chat into a page

`<ChatWidget Identity="the-identity" Title="…" Greeting="…" Placeholder="…" ButtonText="…" />` goes anywhere in a page
or a layout, and a site's frame has a `Corner` slot, fixed lower right, for it. The identity is one the host's
catalogue runs (`GET /v1/identities`). A visitor with no key or sign-in reaches it only through the host's open door,
`OPEN_IDENTITIES` and `OPEN_SESSIONS_PER_DAY` ([ADR 0099](../adr/0099-the-guide-answers-without-a-sign-in-within-a-cap.md)).

A site wraps the tag once, with its own words, as a component of its own, so a page places that and nothing is
configured twice. `ChatWidgetTests` pins it folded and open.

## Make a page move

A page in a site's frame gets the frame's motion for free. A block with the class `reveal` plays the reveal once,
when a quarter of it comes into view, and `data-delay="1"` to `"3"` staggers it by a tenth of a second each. A `Band`'s
title and summary already have it. The frame draws the progress line itself, and a `SectionIndex` with a link per area
when a page has more than one, which it keeps in step with the scroll. An in-page link lands its section centred.

The look is the framework's too: the keyframes `site-rise` and `site-unmask` are in `Styles/web/base/_elements.scss`.
When the visitor's system asks for less motion, every block shows at rest.

## Markdown and links

`Text/Markdown.cs` is the one Markdig pipeline, and it keeps a model's answer from doing anything a page didn't mean.
Raw HTML shows as text, and links survive only to `http(s)` and `mailto`. An image loads only when its address is
exactly one a tool returned in this turn, or when its host is one of the shops' own image hosts. Any other image
becomes a link, loaded only if the person clicks it, because a browser loads an image by itself and a model-written one
could send the chat's data anywhere. Widen any of these only with a test in `MarkdownTests` that shows the case is
safe.

## The link card

A link to a site pasted anywhere (Facebook, LinkedIn, Slack, a chat) shows a card: a title, a line and a picture. The
crawler that builds it runs no script, so the card is read from the site's `wwwroot/index.html`, never from a page.
It reads `<meta name="description">`, the Open Graph tags `og:title`, `og:description`, `og:url` and `og:image`, and
`twitter:card`.

The picture and the address are absolute, on HTTPS, because a relative path shows no picture. A large card wants a
picture of about 1200 × 630, and a small one a square icon. A site's picture is its own file under its `wwwroot`, a
copy, never a link into the framework's docs ([ADR 0074](../adr/0074-the-folders-say-what-ships.md)).
`SiteLinkCardTests` holds every site to it, and the starter's page has the tags with placeholders to fill.

## Test it

```bash
dotnet test src/tests/web/LouisAgent.Web.Tests
LOUIS_UPDATE_GOLDEN=1 dotnet test src/tests/web/LouisAgent.Web.Tests   # after a deliberate markup change
dotnet test demos/louis-agent/tests/LouisAgent.CodeReview.Tests demos/puls/tests/LouisAgent.Puls.Tests  # both apps render through it
```

The tests mirror the folders. `Chats/` holds the stream, the runner, delegations and a specialist's status and answer.
`Components/` holds the bUnit tests, each pinned to an `.html` golden beside it, drawn from the recorded Puls turn in
`src/tests/engine/LouisAgent.SessionStream.Tests/Fixtures/session-stream.sse`. Then there are `Text/` and `Usage/`.
What the web test projects share is in `src/tests/web/LouisAgent.Web.Testing`: `FixtureApi`, `SessionStreamFixture`,
`Snapshots` and `RecordingJSRuntime`.

`Usage/UsageFormat` repeats core's `UsageText` number rules, because the web doesn't reference core, so a change to
one is made in both. Then rebuild the image of each app that uses the library.

## Before you ask for a review

- [ ] It would be the same on every site; nothing of one site crept in.
- [ ] New words are in `ChatText`, and in `PulsText`.
- [ ] The apps' tests pass, and every golden diff is the change you meant.

---
[Guides](README.md) · [Docs index](../README.md)
