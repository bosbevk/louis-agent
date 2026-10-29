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
- **A chat** opens `POST /v1/sessions` with `toolsets: ["specialists", "web"]` where the instance has them (F29), `role: "puls/voice-{language}"` (`puls/voice-af` by default; F21, CL9, CL14) and, when the user shared it,
  `location` and `time_zone` (F22). Messages stream over SSE; `TurnStream` (shared) folds them, sub-session events
  included, into the turn the page shows.
- **Three panels:** the sidebar (search, Verken, Studio, favourites, projects, recent, archive, profile and settings),
  the chat (top bar, messages with actions, the message box), and the Werk panel (`WorkPanel.razor`: each delegation's
  specialist, toolsets, task, steps and cost, the chat's sources, Puls's and the specialists' cost). Below 1200px the
  Werk panel slides in; below 992px the sidebar does.
- **The admin portal** (F27, F31; English, the builders' screen): *Admin* in the profile menu opens `/admin`, its own
  layout (`AdminLayout`). `/admin/evals` lists every eval the runner has, an eval that never ran included
  (`Evals.razor`, F18), each as a card (CL26): its latest run as a scorecard, a ring per mode with pass@k in the
  middle and pass^k under it; the runs before it down a timeline, a bar per mode; each run tagged *recorded*,
  *replay of …* or *re-scored from …* and linked to its source (`StoredRunRow.ReplayOf`); its graders, categories and
  modes in a fold. `EvalCardView` (a Presentation Model) works out what the card shows, and `EvalText` its words.
  `/admin/benchmarks` lists the checks, runs one with live progress, and lists a check's runs; `/admin/benchmarks/{run}` is the explorer (`RunExplorer`: the run, its conversations side by side, a turn, a
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
| `Text/PulsWebRole.cs` | The roles the web app has words for (the cook's work phases), as an enum of its own (ADR 0043) |
| `Pages/Admin/Benchmarks.razor`, `BenchmarkRun.razor` | The checks, a live run and the runs list; one stored run |
| `Components/Admin/` | `RunExplorer`, `TrendChart`, `ChartInfo`: a run at every level, a check's trend, a chart's explanation |
| `Components/Admin/Grades/` | `GradeChips`, `GraderList`: a conversation's outcome and grades, the run's eval and graders, from the standard records (F18 step 4.5c) |
| `Components/Admin/Charts/` | `Ring`, `TokenStack`, `TokenLegend`: the explorer's donut, stacked token bar and token legend, markup only (F18 step 6a.3b) |
| `Components/Admin/Reviews/` | `ReviewPanel` (a conversation's blind review), `ReviewPill`, `AgreementPanel` (per judge, how often it agreed with the people), `CalibrationCard` (the judge check from the portal: the button, the latest report in sentences, the earlier ones folded; F18 step 6a.5c) |
| `Components/Admin/Summary/` | `SummaryPanel`, `SummaryTable`, `RateBar`: a run's results by category, a rate as a bar (F18 step 5c.3, 6a.3c) |
| `Admin/` | `RunModelFilter`: the runs list's model filter; `Grades/`: the records both components use (`GradeOutcome`, `GraderRow`, `GradeRow`, `RunGraders`, `ConversationGrades`) and their words (`GradesText`); `Grades/Reading/`: the outcome parsed once (`GradeOutcomes`); the run's graders and a conversation's grades come from their typed rows (`RunGraders.Of`, `ConversationGrades.Of`); `Grades/Chips/`: the chip list (`GradeChip`, `GradeChipList`) and an outcome's chip class (`ChipKind`); `Charts/`: the charts' colours and maths, no markup (`ChartPalette`, the six series colours for the trend chart and the explorer, `RingArcs`, `TokenKinds`, `ChartNumbers`); `Reviews/`: the review's rules and words in plain classes: `BlindReview` (the one place that knows `.human`: `IsReview`, `JudgeOf`; who answered: `OwnGrades`, `Answered`; which review a conversation shows: `ReviewOf`; what hides the judge's verdict; whether the person agreed: `Compare`), `ReviewStatus` (to review, reviewed, no review, and the list's filter `Keeps`), `ReviewText` and `AgreementText` (the sentences), `CalibrationText` and `CalibrationView` (the judge check), `ReviewerName` and `ReviewNote` (their limits), `ReviewerState` |
| `Text/PulsText.cs` | The shared components' words in Afrikaans (`ChatText`) |
| `Content/` | `Source` and `Sources` (a turn's sources: the answer's links and the pages its specialists read); `Template` and `Templates` (the starters on Verken and Studio) |
| `wwwroot/css/` | The dark theme (orange, green, charcoal, a charcoal-tinted white; Montserrat): `app.css` is the `@import` list in cascade order, the rules in area files at most 200 lines each (`settings/tokens.css`, `base/`, `layout/`, `chat/`, `landing/`, `admin/`; CL26, [ADR 0053](../adr/0053-stylesheets-by-concern.md)) |
| `wwwroot/js/chat.js` | Scrolling, the sidebar and panel toggles, copy, location (browser + Nominatim), focus, the pointer spotlight and tilt, the motion switch, costs counting up |
| `wwwroot/js/landing.js` | The landing page's motion (F24): progress line, section index, centred links, reveals, counters, the pinned tour, the demo, the accordion; started and stopped by `LandingPage.razor` |
| `wwwroot/index.html` | The start screen with Blazor's real loading count; the saved layout and the motion choice applied before the first paint |
| `wwwroot/brand/` | The logo set: mark, logo, stacked, icon tiles, colour versions |

## Design notes

What the look (F20) and the motion (F24) settled, so a change keeps it:

- **The landing page's demo is the app's own markup.** `LandingDemo` builds its turns from the app's classes
  (`user-message`, `delegation`, `work-item`, `work-step`, `work-cost`), so a change to the app's look changes the demo
  with it; only the frame's layout is its own (`demo-*`). Its turns are example data in C#, serialised into
  `data-turns`, and `landing.js` only plays them. The same goes for the page's copy: tiles, chips, chart heights and
  marquee tags are C# arrays rendered by Razor, so no script builds markup from literals.
- **Numbers on the page are measured ones,** each a line of data with what it is and how it was measured, so a new
  measurement changes one line.
- **The loading count is Blazor's own** (`--blazor-load-percentage-text` on `<html>`, shown in a `::after`); no script
  counts files.
- **One motion switch:** `data-motion="on"` on `<html>`, set by `index.html` before the first paint and changed by
  `pulsUi.setMotion` (the sidebar's checkbox and the landing's pill both call it); the stylesheets guard their reduced-motion
  blocks with `:root:not([data-motion="on"])`. Text arriving in order is content, not motion, so a reader who asked
  for less motion still sees the demo play at a reading pace; only the loop stops. Moving content that lasts over five
  seconds has a *Stop* or *Pouse* button (WCAG 2.2.2), and a closed FAQ answer is `inert`.
- **Costs count up without touching Blazor's node:** every `[data-count]` runs from the last number shown to the new
  one after a render, writing the text node's value (not `textContent`, which would replace the node Blazor holds),
  keeping the C# formatting the one source of the money text.
- **Traps found by looking:** Bootstrap paints an offcanvas transparent at its breakpoint (the sidebar and Werk panel
  use a gradient); one page on three addresses with the same parameters doesn't redraw (listen to `LocationChanged`);
  a landing class must not collide with a Bootstrap grid class (`cost-row`, not `row`); a finished animation with
  `fill-mode: both` keeps overriding `transform` (reveals rise on `translate`, so the tilt applies); a `filter` on
  `background-clip: text` makes Chrome drop the text (the gradient line rises without the blur); an element that
  rises in with a filled `transform` animation becomes the frame of its fixed children, so a modal inside it is
  clipped (the card that holds the explorer doesn't rise in, `.card-p.holds-modal`).
- **The admin portal is in English** (Louis, 2026-10-17: "afrikaans is just the language puls can be configured for"):
  it's the builders' screen; recorded questions and answers stay in the language they were recorded in. Its charts
  are our own SVG and CSS on Puls's variables, no chart package, and each explains on hover what good and bad look like
  (`ChartInfo`). A way into a row is a button named for where it goes, never a bare chevron or a bar.
- **The Werk panel shows Puls's own cost and the specialists' side by side,** and the turn's usage line includes the
  specialists through the sub-session roll-up (core, *Showing usage*).

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

[louis-agent.identity.puls](louis-agent.identity.puls.md) (who Puls is) · [louis-agent.web](louis-agent.web.md) ·
[api](louis-agent.api.md) · [evals](louis-agent.evals.md) · [Multi-agent write-up](../learning/10-multi-agent-orchestration.md)

---
[Projects](README.md)
