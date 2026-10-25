# louis-agent.puls — design

> **Kind:** Blazor WebAssembly app · **Path:** `src/louis-agent.puls/` · **Served by:** louis-agent.api built with
> `-p:WebApp=puls` (`docker/images/Dockerfile.puls`, `docker/docker-compose.puls.yml`, port 5070) · **References:**
> louis-agent.web.shared, Bootstrap 5.3 (CDN, SRI-pinned), highlight.js (CDN), Montserrat (Google Fonts)

## Purpose

Puls is the general assistant's web app: a South African AI assistant in everyday Afrikaans that answers by handing each
task to a specialist (F5) and shows that work as it happens. It's chat only: its sessions load `specialists` and `web`
(F29; `agents`, as it was called before CL9, alone before it), so Puls answers the web itself and delegates to a specialist on its own instance (`AGENT_PEERS` names itself, CL19) with a role
(F21) and only the toolsets the task needs; only a validated role is on (`ROLES_ENABLED`: the cook, F25). Pay
as you go: every answer shows what it cost, specialists included.

## How it works

- **Pages:** `/` is the landing page (`LandingPage.razor`). The app is one page, `ChatPage.razor`, at `/gesels`,
  `/gesels/{id}` (old `/chat/{id}` links move there), `/verken` and `/studio`; it redraws on every address change, since
  Blazor wouldn't for the same page with the same parameters.
- **A chat** opens `POST /sessions` with `toolsets: ["specialists", "web"]` where the instance has them (F29), `role: "puls/voice-{language}"` (`puls/voice-af` by default; F21, CL9, CL14) and, when the user shared it,
  `location` and `time_zone` (F22). Messages stream over SSE; `TurnStream` (shared) folds them, sub-session events
  included, into the turn the page shows.
- **Three panels:** the sidebar (search, Verken, Studio, favourites, projects, recent, archive, profile and settings),
  the chat (top bar, messages with actions, the message box), and the Werk panel (`WorkPanel.razor`: each delegation's
  specialist, toolsets, task, steps and cost, the chat's sources, Puls's and the specialists' cost). Below 1200px the
  Werk panel slides in; below 992px the sidebar does.
- **The admin portal** (F27, F31; English, the builders' screen): *Admin* in the profile menu opens `/admin`, its own
  layout (`AdminLayout`). `/admin/benchmarks` lists the checks, runs one with live progress, and lists a check's
  runs; `/admin/benchmarks/{run}` is the explorer (`RunExplorer`: the run, its conversations side by side, a turn, a
  request), the trend (`TrendChart`) and what the run ran with. Each chart explains itself on hover (`ChartInfo`).
  The old `/admin/maatstawwe` addresses still open. **The grades view** (F18 step 4.5c) shows every eval the same way, from
  the standard records alone: `GraderList` above the explorer (the run's eval, version, commit and graders, or one line saying
  it recorded none) and `GradeChips` on each conversation's row (its outcome and one chip per grade, in the run's grader order,
  the reason on hover; "no grades" for a run from before F18 step 2a, never 0). The readers and the chip list are plain classes in
  `Admin/Grades/`, the components in `Components/Admin/Grades/`; neither names anything of Puls (`PortalReadsNoScoreKeyTests`),
  so the admin can move out as a folder.
- **Placeholders** (share, edit, regenerate, feedback, voice, screenshots, image, deep research, other models, language,
  theme, profile, sign-out, buying credits) answer with a "kom binnekort" toast.
- **Stored in the browser:** chats (with favourite and archived), the API key, the currency, the location, whether
  the sidebar and the Werk panel are hidden, and whether the animations are on although the system asks for less
  (`puls.motion`, F24).

## Structure

| Path | What |
|---|---|
| `Pages/LandingPage.razor` | The landing page (F20, moving since F24): the water, the progress line and the section index, then the sections in order, and the JS start and stop |
| `Components/Landing/` | One component per section (W2): `LandingNav`, `Hero`, `Showcase`, `HowItWorks`, `InsideTheApp` (the pinned tour), `Features` (the Kenmerke bento and marquees), `Numbers` (Syfers), `Pricing`, `About`, `Faq` (the accordion), `CallToAction`, `LandingFooter`; a section's copy is a C# array in its own file. `MiniChat` is the little picture of the app the stages and the tour show (a question, a delegation card, the body, sources, usage), six panels from one component; `LandingBars` the example bar heights two sections share. Every section's copy and the brand components' per-instance ids are in `.razor.cs` files (W2b) |
| `Components/Landing/LandingDemo.razor` | The app's three panels in the landing page, playing three scripted turns (example data in `data-turns`, played by `landing.js`) |
| `Components/Brand/` | `PulsLogo` (the ripple logo inline, its rings glow outward), `PulsTile` (the app icon), `PulsMark` (the mark's geometry), `Water` (the four swells behind the landing page and an empty chat) |
| `Pages/ChatPage.razor`, `ChatPage.razor.cs` | The shell, the layout only since W2: sidebar, top bar, the chat (the shared `MessageList` and `Composer`), Verken and Studio views, the Werk panel; its own state and handlers in the code-behind, a turn's life in the shared `ChatRunner` |
| `Components/Chat/` | `EmptyChat` (the welcome with the message box in the middle) and `Toast` at the top; by concern (W2b): `Search/ChatSearch` (Ctrl/Cmd K over the chats in this browser, matching by `Search/ChatFinder`, tested alone), `Turns/UserActions` and `AnswerActions` (under a message and an answer), `Composing/MessageBox` (the shared Composer with Puls's toolbar, `ComposerTools`), `Panels/WorkPanel` (the Werk panel), `ChatViews`, `ExploreView`, `StudioView` and `TemplateCard` (Verken and Studio). Every component's C# is its `.razor.cs`; the page names its fragments as markup templates |
| `Components/Chat/Sidebar/` | `ChatSidebar` (the brand, a new chat, search, the views, the chats and the settings), `ChatListItem`, `ChatMenu`, `ProfileCard`, `LocationPicker`, `MotionToggle` |
| `Components/Chat/Header/` | `ChatHeader` (the top bar), `CreditsMenu`, `LanguageMenu`, `ProfileMenu` |
| `Layout/AdminLayout.razor` | The admin portal's sidebar and content column |
| `Pages/Admin/Benchmarks.razor`, `BenchmarkRun.razor` | The checks, a live run and the runs list; one stored run |
| `Components/Admin/` | `RunExplorer`, `TrendChart`, `ChartInfo`: a run at every level, a check's trend, a chart's explanation |
| `Components/Admin/Grades/` | `GradeChips`, `GraderList`: a conversation's outcome and grades, the run's eval and graders, from the standard records (F18 step 4.5c) |
| `Admin/` | `RunModelFilter`: the runs list's model filter; `Grades/`: the readers of the run's graders and a conversation's grades (`RunGradersReader`, `ConversationGradesReader`), the outcome parsed once (`GradeOutcomes`), the chip list (`GradeChipList`) and its words (`GradesText`) |
| `Text/PulsText.cs` | The shared components' words in Afrikaans (`ChatText`) |
| `Content/` | `Source` and `Sources` (a turn's sources: the answer's links and the pages its specialists read); `Template` and `Templates` (the starters on Verken and Studio) |
| `wwwroot/css/app.css` | The dark theme (orange, green, charcoal, a charcoal-tinted white; Montserrat) |
| `wwwroot/js/chat.js` | Scrolling, the sidebar and panel toggles, copy, location (browser + Nominatim), focus, the pointer spotlight and tilt, the motion switch, costs counting up |
| `wwwroot/js/landing.js` | The landing page's motion (F24): progress line, section index, centred links, reveals, counters, the pinned tour, the demo, the accordion; started and stopped by `LandingPage.razor` |
| `wwwroot/index.html` | The start screen with Blazor's real loading count; the saved layout and the motion choice applied before the first paint |
| `wwwroot/brand/` | The logo set: mark, logo, stacked, icon tiles, colour versions |

## Configuration

None of its own: the API it's served by holds the settings (`AGENT_PEERS` names itself: the specialists, and the minions
they hand pages to (F30), run on the same instance since CL19). The compose file runs `puls` (5070, the app and its API),
`puls-mongo` (the benchmark store, F31) and, on their own `web` network with `puls` (F32),
`puls-searxng` (8888: the search we run, for development and benchmarks; settings in `docker/searxng/settings.yml`, its
secret in `config/.env.searxng`) and `puls-browser` (9222: the headless Chrome that renders pages). Both images are
pinned by digest.

## Extending it

The steps are in [Change the Puls web app](../guides/change-the-puls-app.md). In short: a starter is a line in
`Content/Templates.cs`; a chat piece is a component under `Components/Chat/` by concern; a placeholder made real
replaces its "kom binnekort" toast and keeps the label. Puls's voice and roles are the Puls identity's prompts
([Build an identity](../guides/build-an-identity.md)); built in, so a change is a rebuild.

## Tests

`tests/louis-agent.puls.tests`, NUnit with bUnit: `Pages/ChatPageTests` renders the chat against `FixtureApi`, streams
the recorded Puls turn, lists the page's JavaScript calls and pins `ChatPage.empty.html` and `ChatPage.turn.html`;
`Pages/LandingPageTests` pins the landing page; `Components/` pins `WorkPanel`, `TemplateCard` and `PulsTile`;
`Search/ChatFinderTests` tests the search. The layout at 1440, 1100 and 420 px wide is still checked by hand.

## Limits and plans

- Usage figures are per browser (the chats saved here); a user's monthly total belongs to the server-side ledger (F23).
- Buying credits (F23 Credits), a light theme, accounts, and the placeholders above are to come.

## Related docs

[F20 Puls look](../features/F20-puls-look.md) · [F21 Personalities](../features/F21-personalities.md) ·
[F22 Role tools](../features/F22-role-tools.md) · [F31 Benchmarks in the admin portal](../features/F31-benchmarks-portal.md) ·
[louis-agent.web](louis-agent.web.md) · [api](louis-agent.api.md)

---
[Projects](README.md)
