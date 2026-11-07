# Change the Puls web app

> **How-to.** Change `demos/puls/web/louis-agent.puls`, the general assistant's web app: run it, change the chat, the landing page
> or the admin portal, add a starter, place its words, and test it. **Elsewhere:** how it works in
> [louis-agent.puls](../projects/louis-agent.puls.md); what it shares with the code-review app in
> [Change the framework's web](../../../../src/docs/guides/change-the-web.md); Puls's voice, roles and packs in
> [Build an identity](../../../../src/docs/guides/build-an-identity.md).

Puls is a Blazor WebAssembly app, served by Puls's program, `louis-agent.puls-api`, which references it. It has three parts: the
landing page at `/`, the chat at `/gesels` (with Verken and Studio), and the admin portal at `/admin` for the builders.
What Puls says and which specialists it calls are the Puls identity's prompts, not this app. The app shows the chat,
the specialists' work in the Werk panel, and what each answer cost.

## Run it

```bash
docker compose -f demos/puls/docker/docker-compose.puls.yml --env-file demos/puls/config/.env up -d --build          # http://127.0.0.1:5070
dotnet run --project demos/puls/hosts/louis-agent.puls-api         # http://127.0.0.1:5071
```

The launch profile gives the API Puls's settings (`IDENTITIES=puls`, its roles, SearXNG and the browser on the
compose stack's ports). The web app is the program's project reference. The compose stack also runs the store (`puls-mongo`), the search (`puls-searxng`) and
the headless browser (`puls-browser`) that the specialists use.

## Change the chat

- **The page** is `Pages/ChatPage.razor`, which only fills the framework's `ChatShell`: the logo, the links, the profile, the water, the
  empty chat and the cost line's words. `ChatPage.razor.cs` says which view shows and which toolsets a session asks for.
- **A change to the frame** (the sidebar, the top bar, the message box, the work panel, renaming, search) goes in `louis-agent.web`, since
  every site gets it ([Change the framework's web](../../../../src/docs/guides/change-the-web.md)). Only what's Puls's own is a component under
  `Components/Chat/`, with its C# in its `.razor.cs` ([src/CLAUDE.md](../../../../src/CLAUDE.md)).
- **Logic** that needs no component goes in a plain class with its test.
- **The session** opens with `["specialists", "web"]` where the instance has them, the role `puls/voice-af` (or the
  chosen language's mode), and the user's place and time zone. A new session option is the API's first
  ([Change the API](../../../../src/docs/guides/change-the-api.md#add-a-session-option)), then `AgentApi.CreateSessionAsync` in the shared library.
- **A placeholder made real:** replace its "kom binnekort" toast with the feature, and keep the label.

## Add a starter

A line in `Content/Templates.cs`: Verken and Studio show it as a card (`TemplateCard`).

## Change the landing page

`Pages/LandingPage.razor` lays out the sections in order. Each section is a component in `Components/Landing/` with
its copy as a C# array in its `.razor.cs`. The motion (the progress line, the reveals, the pinned tour, the demo) is
`wwwroot/js/landing.js`, started and stopped by the page. Keep a new animation behind the motion switch
(`chat.motion`), so a user who asks for less motion gets none.

## Change the admin portal

The pages are the framework's: [Change the eval pages](../../../../src/docs/guides/change-the-eval-pages.md). What's Puls's own is `Layout/AdminLayout.razor`, which
fills the framework's frame with its logo, way back, live strip and links, and `Content/PulsSites.cs`, which says which websites its sources chart sorts: add
a shop's address there.

## Words

Puls speaks Afrikaans. The shared components' words are `Text/PulsText.cs` (`PulsText.Afrikaans`, a `ChatText`). A
label in a `.razor` is markup and stays there. A text built in C# goes in `PulsText` rather than inline
([ADR 0009](../../../../src/docs/adr/0009-message-text-as-enum-templates.md)). A word the framework's frame shows is a property of
`ChatText` in English, and its Afrikaans goes in `PulsText`.

## Test it

```bash
dotnet test demos/puls/tests/louis-agent.puls.tests
LOUIS_UPDATE_GOLDEN=1 dotnet test demos/puls/tests/louis-agent.puls.tests    # after a deliberate markup change: then read the diff
```

- `Pages/ChatPageTests` renders the chat page with bUnit against `FixtureApi` and streams a recorded Puls turn (a
  pancake recipe handed to the cook). The empty page and the turn are pinned to `ChatPage.empty.html` and
  `ChatPage.turn.html`, and the test lists the page's JavaScript calls (`chatUi.*`, `louisChat.*`), so a new call is a
  review point.
- `Pages/LandingPageTests` pins the landing page (`LandingPage.html`). `Components/` pins `WorkPanel`,
  `TemplateCard` and `PulsTile`.

Then check it in a browser at a wide, a medium and a phone width (1440, 1100 and 420 px): the Werk panel slides in
below 1200 px and the sidebar below 992 px.

## Before you ask for a review

- [ ] No `@code` in a new `.razor`; new logic in a plain class with its test.
- [ ] New words in `PulsText` or in markup, none built inline in C#.
- [ ] Motion behind the motion switch; the golden files' diff is the change you meant.

---
[Guides](../../../../src/docs/guides/README.md) · [Docs index](../../../../src/docs/README.md)
