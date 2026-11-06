# louis-agent.puls — design

> **Kind:** Blazor WebAssembly app · **Path:** `demos/puls/web/louis-agent.puls/` · **Served by:** louis-agent.api built with
> `louis-agent.puls-api` (`demos/puls/docker/images/Dockerfile.puls`, `demos/puls/docker/docker-compose.puls.yml`, port 5070) · **References:**
> louis-agent.web, Bootstrap 5.3 (CDN, SRI-pinned), highlight.js (CDN), Montserrat (Google Fonts)

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
  layout (`AdminLayout`), organised by job (F53). `/admin` is the Evals page (F18, CL26, F53.2), in list/details shape:
  the list pane names every eval the runner has, an eval that never ran included, with its last run's status, when it
  ran and per mode the last pass@k with its move against the mode's run before (`EvalList`, `EvalListItem`; the
  numbers in `EvalListView`, a Presentation Model); the chosen eval is in the address (`?eval=`, `EvalAddress`; none
  opens the eval that ran last) and its details fill the pane beside it (`EvalDetails`): what it proves, the controls
  that run it (`EvalRunControls`: the repeats, the Run button with its estimate from the last run that asked the
  model, `RunEstimate`: not a re-score, not a replay; the Manual button, FX28), its latest run as a scorecard with a
  ring per mode, the runs before it down a timeline, each tagged *recorded*, *replay of …* or *re-scored from …* and
  linked to its source, its trend, its graders, categories and modes in a fold (`EvalCardView` works out the card,
  `EvalText` its words), and under the card every stored run as a table (`RunsList`, `RunListItem`: a run can be
  re-scored, a recorded one replayed through the page's one start path). The page starts and follows a run
  (`LiveRunPanel` above the details, kept while the reader opens another eval), and a thin strip in the layout says a
  run is going on every admin page (`LiveRunStrip`: it reads the runner's current run on open and every ten seconds
  while that run goes).
  `/admin/review` is the Review page (F53.3): the answers waiting for this reviewer's grade across the stored runs,
  one at a time, from the queue the API keeps (`GET v1/admin/evals/reviews/queue?reviewer=&limit=`, read for the
  kept name; a name typed or let go in the panel reads it again): where the answer is from (the eval, a link to its
  run, the mode, the repeat, the task; `ReviewItemHead`), its place in the queue and Skip, then the explorer's blind
  review panel unchanged (`ReviewPanel`, which reports a stored grade through `OnGraded`); a grade or Skip opens the
  next, past the last one handed over the queue is read again, and with nothing waiting the page says so by name.
  The keys 1, 2 and 3 grade pass, fail or unsure (`ReviewKeys`; the page's box takes the focus when an answer
  opens, and the panel's note and name fields keep their keys to themselves). The page's state is `ReviewQueueView`
  (a Presentation Model: the items, which is open, graded and left) with its words in `ReviewQueueText`; how many
  wait is `ReviewQueueState`, a scoped Observer the page sets and `ReviewCountBadge` beside *Review* in the sidebar
  draws, read once from the queue when the portal opens.
  `/admin/runs/{run}` is one run (F53.1, [ADR 0065](../../../../src/docs/adr/0065-the-view-is-in-the-address.md)): under its
  head the verdict (`RunVerdict`: per mode passed of graded and the change against the eval's run before, from the
  trend; the cost, the requests, the answers still to grade; the numbers in `RunVerdictView`, a Presentation Model),
  then one tab at a time, the tab in the address (`?tab=`: Summary with the results by category, the pass rates and
  the trends, `TrendChart`; Conversations with the explorer; Judge; Notes; Settings). The explorer (`RunExplorer`:
  the run, its conversations side by side, a turn, a request) reports every move as an `ExplorerPlace` and the page
  puts it in the address (`?view=…&task=…&turn=…`), so Back goes up a level and a link opens the same view;
  `RunAddress` is the one class that spells a run's address. Each chart explains itself on hover (`ChartInfo`).
  `/admin/prompts` lists every built-in prompt by its header (CL27); `/admin/imports` takes the packs' data uploads (F55); the old `/admin/benchmarks` and `/admin/maatstawwe`
  addresses are gone (ADR 0049). **The grades view** (F18 step 4.5c) shows every eval the same way, from
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
| `Layout/AdminLayout.razor` | Fills the framework's `AdminShell` with Puls's own: its logo, live strip and way back, and the admin portal's sidebar by job (*Measure*: Evals, Review with its count; *Build*: Studio, Prompts; F53.2, F53.3), the live-run strip and the content column |
| `Pages/Admin/Review.razor`, `Review.razor.cs` | The Review page at `/admin/review` (F53.3): the queue read for the kept name, one answer open with its head and the blind review panel, the keys 1, 2 and 3, the count kept current for the sidebar |
| `Pages/Admin/Prompts.razor`, `Prompts.razor.cs` | Every built-in prompt as its header says it (CL27, ADR 0063), from `GET /v1/admin/prompts`: one table sorted by path, a row each with its name and file, level, description and the toolsets it always loads (a dash for none); styles at the end of `css/admin/shell.css` |
| `Pages/Admin/Data/Reports.razor` | The packs' reports (F35.7, ADR 0083), from `GET /v1/admin/reports`: a `ReportCard` (from `louis-agent.web`) for each, under *Data* in the sidebar |
| `Pages/Admin/Imports.razor`, `Components/Admin/Imports/ImportCard.razor`, `Admin/Imports/ImportForm.cs` | The packs' uploads (F55, ADR 0071), from `GET /v1/admin/imports`: a card each with a file box per slot, Preview and the report's facts and problems, and Save only after a clean preview that changes something (`ImportForm` holds those rules); the sidebar's Data section; styles at the end of `css/admin/shell.css` |
| `Text/PulsWebRole.cs` | The roles the web app has words for (the cook's work phases), as an enum of its own (ADR 0043) |
| `Pages/Admin/Evals.razor`, `BenchmarkRun.razor` | The Evals page at `/admin`: the list, the chosen eval's details, the run it starts and follows (F53.2); one stored run: the head, the verdict, the tab bar and the open tab, the tab and the explorer's place read from the address and the run fetched only when its id changes (F53.1) |
| `Components/Admin/EvalsPage/`, `List/`, `Details/`, `Admin/Evals/`, `Admin/Evals/List/` | The Evals page's parts: `EvalList` and `EvalListItem` (the list pane; `EvalLine`, `ModeChange`, `EvalListView`, `EvalAddress`), `EvalDetails`, `EvalRunControls` and `ManualDialog` (the details pane), `EvalScorecard`, `EvalRunTimeline`, `EvalRunOrigin`, `EvalChecks` (`EvalCardView`, `RunLine`, `RunOrigin`, `GraderGroup`, `EvalText`) |
| `Components/Admin/LiveRun/`, `Components/Admin/Runs/`, `Runs/Table/`, `Components/Admin/Head/` | `LiveRunPanel` and `LiveTurnTable` (the run in progress, its turns as they come), `LiveRunStrip` (a run is going, on every page); a stored run's row and its buttons, `RunsList` (the runs table); `AdminGate` (the key) and `StoreBanner` (the store's state) |
| `Components/Admin/` | `RunExplorer`, `TrendChart`, `ChartInfo`: a run at every level (its place handed in as `Place` and reported on every move as `OnMoved`), a check's trend, a chart's explanation |
| `Components/Admin/RunPage/`, `Admin/RunPage/`, `Admin/Verdict/`, `Admin/Explorer/Place/` | The run page's parts (F53.1): `RunHead`, `RunVerdict` (the card under the head) and `RunTabBar` (a link per tab); `RunTab` and `RunTabs` (the tab, parsed once from `?tab=`, and the bar's entries), `RunAddress` (the one writer of a run's address); `RunVerdictView` and `ModeVerdict` (the verdict's numbers, worked out from the run and its trend) with `VerdictText`; `ExplorerView` and `ExplorerPlace` (where you are in the explorer, written to and read from the address) |
| `Components/Admin/Grades/` | `GradeChips`, `GraderList`: a conversation's outcome and grades, the run's eval and graders, from the standard records (F18 step 4.5c) |
| `Components/Admin/Charts/` | `Ring`, `TokenStack`, `TokenLegend`: the explorer's donut, stacked token bar and token legend, markup only (F18 step 6a.3b) |
| `Components/Admin/Reviews/` | `ReviewPanel` (a conversation's blind review; it reports a stored grade through `OnGraded` and grades through its public `GradeAsync`, F53.3), `Queue/` (`ReviewItemHead`, where the open answer is from and Skip; `ReviewCountBadge`, how many wait, beside *Review* in the sidebar), `ReviewPill`, `AgreementPanel` (per judge, how often it agreed with the people), `CalibrationCard` (the judge check from the portal: the button, the latest report in sentences, the earlier ones folded; F18 step 6a.5c) |
| `Components/Admin/Studio/`, `Admin/Studio/` | The studio's drafts (F49.4, [ADR 0061](../../../../src/docs/adr/0061-evals-grow-from-use.md)): `SampleDialog` under a conversation's card (the scenario it ran as the form's start, posted as a sample draft), `KeepButtons` under a graded review (a pass kept as a reference, a fail as a known-bad with its reason), `DraftList` on an eval's card (folded under its count, each draft with a delete, the page reloaded after); the rules in plain classes with their own tests (`SampleForm`, `KeepOffers`, `StudioText`); styles in `css/admin/studio.css` |
| `Pages/Admin/EvalStudio.razor`, `Components/Admin/Studio/Compose/`, `Components/Admin/Studio/Promptfoo/`, `Components/Admin/Studio/Check/`, `Components/Admin/Studio/Try/`, `Admin/Studio/`, `Admin/Studio/Compose/`, `Admin/Studio/Schema/` | The Studio page at `/admin/studio` (F49.5, [ADR 0062](../../../../src/docs/adr/0062-the-studio-writes-what-git-keeps.md); named `EvalStudio`, since the folders' `.Studio` namespaces would shadow `Studio`): the evals being composed on the left, the form on the right as four numbered steps in the order they are done (the admin review's finding 6): *1 Compose* with *Save* and *View JSON*, *2 Check* (the runner's problems in its words, `StudioVerdict`), *3 Try* (`TryStep`: a replay of a run picked from the recorded runs the Evals page lists, `ReplayChoices`, this eval's own first, on the samples typed, linked to its run page) and *4 Publish* (the branch named, or a zip downloaded); each action saves first. The form is plain classes the components change in place (`EvalComposition`, `ModeEntry`, `CheckEntry`, `RubricEntry`), edited by `ComposeHead`, `ComposeChecks` and `ComposeRubrics`; a check's fields come from its settings' schema (`SchemaFields`, `SchemaField`, `FieldKind`: a property named `reader` is a pick of the runner's readers). `PromptfooPanel` imports a pasted promptfoo file into a new draft, which the page opens, listing what was left out, and exports the open draft as `promptfooconfig.yaml` |
| `Components/Admin/Summary/` | `SummaryPanel`, `SummaryTable`, `RateBar`: a run's results by category, a rate as a bar (F18 step 5c.3, 6a.3c) |
| `Admin/` | `RunModelFilter`: the runs list's model filter; `Grades/`: the records both components use (`GradeOutcome`, `GraderRow`, `GradeRow`, `RunGraders`, `ConversationGrades`) and their words (`GradesText`); `Grades/Reading/`: the outcome parsed once (`GradeOutcomes`); the run's graders and a conversation's grades come from their typed rows (`RunGraders.Of`, `ConversationGrades.Of`); `Grades/Chips/`: the chip list (`GradeChip`, `GradeChipList`) and an outcome's chip class (`ChipKind`); `Charts/`: the charts' colours and maths, no markup (`ChartPalette`, the six series colours for the trend chart and the explorer, `RingArcs`, `TokenKinds`, `ChartNumbers`); `Reviews/`: the review's rules and words in plain classes: `BlindReview` (the one place that knows `.human`: `IsReview`, `JudgeOf`; who answered: `OwnGrades`, `Answered`; which review a conversation shows: `ReviewOf`; what hides the judge's verdict; whether the person agreed: `Compare`), `ReviewStatus` (to review, reviewed, no review, and the list's filter `Keeps`), `ReviewText` and `AgreementText` (the sentences), `CalibrationText` and `CalibrationView` (the judge check), `ReviewerName` and `ReviewNote` (their limits), `ReviewerState`; `Reviews/Queue/` (F53.3): `ReviewQueueView` (the Review page's model), `ReviewQueueText` (its words), `ReviewKeys` (1, 2, 3 as verdicts), `ReviewQueueState` (how many wait, shared with the badge) |
| `Text/PulsText.cs` | The shared components' words in Afrikaans (`ChatText`) |
| `Content/` | `Source` and `Sources` (a turn's sources: the answer's links and the pages its specialists read); `Template` and `Templates` (the starters on Verken and Studio) |
| `wwwroot/css/` | The dark theme (orange, green, charcoal, a charcoal-tinted white; Montserrat): `app.css` is the `@import` list in cascade order, the rules in area files at most 200 lines each (`settings/tokens.css`, `base/`, `layout/`, `chat/`, `landing/`, `admin/`; CL26, [ADR 0053](../../../../src/docs/adr/0053-stylesheets-by-concern.md)) |
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
`puls-searxng` (8888: the search we run, for development and benchmarks; settings in `demos/puls/docker/searxng/settings.yml`, its
secret in `config/.env.searxng`) and `puls-browser` (9222: the headless Chrome that renders pages). Both images are
pinned by digest.

## Extending it

The steps are in [Change the Puls web app](../guides/change-the-puls-app.md). In short: a starter is a line in
`Content/Templates.cs`; a chat piece is a component under `Components/Chat/` by concern; a placeholder made real
replaces its "kom binnekort" toast and keeps the label. Puls's voice and roles are the Puls identity's prompts
([Build an identity](../../../../src/docs/guides/build-an-identity.md)); built in, so a change is a rebuild.

## Tests

`demos/puls/tests/louis-agent.puls.tests`, NUnit with bUnit: `Pages/ChatPageTests` renders the chat against `FixtureApi`, streams
the recorded Puls turn, lists the page's JavaScript calls and pins `ChatPage.empty.html` and `ChatPage.turn.html`;
`Pages/LandingPageTests` pins the landing page; `Components/` pins `WorkPanel`, `TemplateCard` and `PulsTile`;
`Search/ChatFinderTests` tests the search. The layout at 1440, 1100 and 420 px wide is still checked by hand.

## Limits and plans

- Usage figures are per browser (the chats saved here); a user's monthly total belongs to the server-side ledger (F23).
- Buying credits (F23 Credits), a light theme, accounts, and the placeholders above are to come.

## Related docs

[louis-agent.identity.puls](louis-agent.identity.puls.md) (who Puls is) · [louis-agent.code-review](../../../louis-agent/docs/projects/louis-agent.code-review.md) ·
[api](../../../../src/docs/projects/louis-agent.api.md) · [evals](../../../../src/docs/projects/louis-agent.evals.md) · [Multi-agent write-up](../../../../src/docs/learning/10-multi-agent-orchestration.md)

---
[Projects](../../../../src/docs/projects/README.md)
