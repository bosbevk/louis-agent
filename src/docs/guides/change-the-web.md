# Change the framework's web

> **How-to.** Change `src/web/louis-agent.web`, the Razor class library every site is built on: decide what belongs here,
> add an API call, follow a new event in the stream, add a component, give it words, and test it. **Elsewhere:** how
> it works in [louis-agent.web](../projects/louis-agent.web.md); the two apps in
> [Change the code-review web app](../../../demos/louis-agent/docs/guides/change-the-web-app.md) and [Change the Puls web app](../../../demos/puls/docs/guides/change-the-puls-app.md).

The library holds what makes a chat work the same in both apps: the API client (`Api/`), the chat as data and a turn's
life (`Chats/`), the views of a turn and of a specialist's work (`Components/`), safe markdown (`Text/`), usage and
currency (`Usage/`), and what the browser keeps (`Storage/`). Each app keeps its own pages, look, JavaScript and words.

## Does it belong here?

Ask whether it would be the same on every site. If so it belongs here, even if only one site uses it today; what differs comes
in from the site, and the framework names no site ([ADR 0084](../adr/0084-every-site-is-built-on-the-frameworks-web.md)). For
now a component still emits class names; its rules are the framework's Sass under `Styles/web/`, and a site lists them in its `app.scss`.

## Change the chat's frame

`Components/Chat/Shell/ChatShell.razor` is the frame, and each part is a component in the folder of its concern. Something every site
should get is a change here. Something that differs per site is a slot or a parameter on `ChatShell`, which the site fills, never a check
of which site this is. A new word goes in `ChatText` in English. State the frame keeps goes in a small plain class in `Shell/`, with its
own test. A site's page redraws for its own reasons, which sets the frame's parameters again, so `OnParametersSet` there can't assume
the address changed.

## Change the styles or scripts

A rule every site should get goes in a partial under `Styles/web/`, with colours from variables only (`FixedColourTests` and
`FrameworkStylesTests` hold it). What repeats becomes one piece under `web/tools/` ([the rules](../../../.claude/rules/css.md)). A script a
component calls goes in `wwwroot/js/chat.js`, and `FrameworkScriptsTests` holds that it's there. A script holds no words for the
person: it names the problem, and `ChatText` says it.

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

## Markdown and links

`Text/Markdown.cs` is the one Markdig pipeline. It shows raw HTML as text, allows links only to `http(s)` and
`mailto`, and allows images only from the shops' own image hosts, so a model-written image can't send the chat's data
elsewhere. Widen any of these only with a test in `MarkdownTests` that shows the case is safe.

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
