# Change the shared web library

> **How-to.** Change `src/louis-agent.web.shared`, the Razor class library both web apps use: decide what belongs here,
> add an API call, follow a new event in the stream, add a component, give it words, and test it. **Elsewhere:** how
> it works in [louis-agent.web.shared](../projects/louis-agent.web.shared.md); the two apps in
> [Change the code-review web app](change-the-web-app.md) and [Change the Puls web app](change-the-puls-app.md).

The library holds what makes a chat work the same in both apps: the API client (`Api/`), the chat as data and a turn's
life (`Chats/`), the views of a turn and of a specialist's work (`Components/`), safe markdown (`Text/`), usage and
currency (`Usage/`), and what the browser keeps (`Storage/`). Each app keeps its own pages, look, JavaScript and words.

## Does it belong here?

Only when both apps need it. A piece one app needs stays in that app, even if the other might want it one day. A
shared component emits class names and no CSS of its own: each app styles it in its `app.css`.

## Add an API call

`Api/AgentApi.cs` is the only code that calls the API. Add a method there for a call both apps make, using
`GetAsync<T>`, `PostAsync` or `StreamLinesAsync` rather than a new `HttpClient` call. A call one app makes is an
extension method in that app (the code-review app's `Workspace/AgentApiWorkspace.cs`). A failed call throws
`AgentApiException`, which `ChatRunner` turns into the app's words.

## Follow a new event in the stream

1. Map the event to its record in `louis-agent.session-stream/SessionEventMapper.cs` (and its record in `SessionEvents.cs`), with a test
   in `tests/louis-agent.session-stream.tests`. The reader hands the web apps `SessionEvent`s, so the event exists there first.
2. Write the test first in `tests/louis-agent.web.shared.tests/Chats/TurnStreamTests.cs`, with the event as the API
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
2. Its words go in `Text/ChatText.cs` as a property whose default is the code-review app's English. Puls sets its
   Afrikaans in `src/louis-agent.puls/Text/PulsText.cs`; add the property there too, or Puls shows English.
3. Leave slots (`RenderFragment` parameters) where an app adds its own parts, as `Composer` and `MessageList` do.

## Markdown and links

`Text/Markdown.cs` is the one Markdig pipeline. It shows raw HTML as text, allows links only to `http(s)` and
`mailto`, and allows images only from the shops' own image hosts, so a model-written image can't send the chat's data
elsewhere. Widen any of these only with a test in `MarkdownTests` that shows the case is safe.

## Test it

```bash
dotnet test tests/louis-agent.web.shared.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test tests/louis-agent.web.shared.tests   # after a deliberate markup change
dotnet test tests/louis-agent.web.tests tests/louis-agent.puls.tests  # both apps render through it
```

The tests mirror the folders: `Chats/` (the stream, the runner, delegations, a specialist's status and answer),
`Components/` (bUnit, each pinned to an `.html` golden beside its test, from the recorded Puls turn in
`tests/louis-agent.session-stream.tests/Fixtures/session-stream.sse`), `Text/` and `Usage/`. `tests/louis-agent.web.testing` holds what the three
web test projects share: `FixtureApi`, `SessionStreamFixture`, `Snapshots`, `FakeClock` and `RecordingJSRuntime`.

`Usage/UsageFormat` repeats core's `UsageText` number rules (the app doesn't reference core), so a change to one must
be made in both. Then rebuild the image of each app that uses the library.

## Before you ask for a review

- [ ] Both apps need it; nothing app-specific crept in.
- [ ] New words in `ChatText` and in `PulsText`.
- [ ] Both apps' tests pass, and every golden diff is the change you meant.

---
[Guides](README.md) · [Docs index](../README.md)
