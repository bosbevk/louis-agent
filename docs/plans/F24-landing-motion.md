# F24 · Landing motion: plan

> Approved 2026-10-15 · Feature: [F24 Landing motion](../features/F24-landing-motion.md) · Branch:
> `feature/F24-landing-motion` (off `main`)

## Context

The animated landing page Louis reviewed as an artifact (version 24) replaces F20's landing page, and the app at
`/gesels` gets the pieces of it that fit: the water behind the empty chat, the Werk panel's motion, a real loading
percentage, and an *Animasies* switch (Louis: "all of them"). Same colours, fonts and brand; no chat behaviour changes.
The web app has no automated tests ([tests/CLAUDE.md](../../tests/CLAUDE.md)), so each step is checked by build and
headless-Chrome screenshots, as F20 was, and the diff is shown before each commit.

## Design: where the artifact's parts land

| Artifact part | In the repo |
|---|---|
| Tokens, water, loader, index, nav, pills, reveals, focus, bento, stack, pin, stats, FAQ, CTA, phone and reduced-motion rules | `src/louis-agent.puls/wwwroot/css/app.css`, replacing F20's landing block (`.landing*`, `.showcase`, `.mock*`, `.steps`, `.features`, `.faq`, `.cta`) section by section; the app's existing tokens and keyframes (`rise`, `spin`, `ring`, `beat`, `heartbeat`, `pulse-ring`) are reused, not duplicated |
| The page markup | `Pages/LandingPage.razor` rewritten; features, steps, tabs, FAQ and the demo's turns stay C# arrays, as F20's `Features` and `Questions`, so the copy is in one place |
| The playing demo | `Components/LandingDemo.razor`: the three-panel frame; its turns serialised into a `data-turns` JSON attribute and played by JS |
| The JS (scroll spy, centred links, reveals, counters, glyph decode, magnetic pill, tilt, the pinned chips, the demo, the motion switch) | New `wwwroot/js/landing.js`, loaded from `index.html` after `chat.js`; started from the page in `OnAfterRenderAsync(firstRender)` with `JSRuntime.InvokeVoidAsync("pulsLanding.start")` and stopped in `Dispose` (a Blazor route: `DOMContentLoaded` would miss it). `chat.js` keeps the spotlight handler; the tilt joins it so both pages share one pointer handler |
| The brand mark and wordmark | `PulsLogo.razor` and `brand/puls-icon-tile.svg`, instead of the artifact's inline copies |
| Water | `Components/Water.razor` (the four swells and their gradients), used by the landing and by the empty chat |
| Loader | Blazor's real start screen (`index.html`'s `.loading`) gains the beating mark and a percentage from `Blazor.start({ loadBootResource })` counting the files fetched: real progress; the artifact's fake count goes |
| Section index | Landing only |

**The app (`/gesels`), step 6:**

- Empty chat: `<Water/>` low behind the messages and the composer; the orange rings stay as the brand's pulse behind the
  headline, smaller; the headline word by word; topic cards and suggestions get the tilt and spotlight.
- Werk panel: steps and sources open with the FAQ's grid-rows transition and the round plus; the delegation card's
  chevron becomes that control; `.work-cost` and the usage meter count up when a value arrives (`pulsUi.countTo`).
- Loading: the real percentage.
- *Animasies* switch in the sidebar settings: "Wys animasies al vra my stelsel minder" → `data-motion="on"` on `<html>`
  and `localStorage puls.motion`, read in `index.html`'s head script before paint (as the sidebar width is);
  `app.css`'s reduced-motion block gets the `:root:not([data-motion="on"])` guard; the landing's bottom pill reads the
  same key.

## Steps

One commit each (`feat(f24): step N - …`); the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc, this plan, the TODO line | — |
| 1 | `landing.js` skeleton, `Water.razor`, the CSS tokens and keyframes merged; the landing's water, nav, progress line, section index, centred links, reveals | build; `/` at 1440 / 1100 / 420 |
| 2 | Hero (news pill, word headline, proof, keywords) and `LandingDemo.razor` playing its turns, the frame flattening on scroll | build; screenshots; the demo seen playing in Chrome |
| 3 | *Hoe dit werk* (three cards with stages), *In die app* (pinned chips and panels, click and scroll), *Kenmerke* bento, the two marquees | build; screenshots |
| 4 | *Syfers* and *Pryse* counters, *Vrae* accordion, CTA, footer; F20's landing CSS removed | build; screenshots; every nav and index link clicked |
| 5 | Real loading percentage, *Animasies* switch, a reduced-motion pass, a phone pass | build; screenshots with and without reduced motion |
| 6 | The app: water behind the empty chat, Werk panel motion, counting costs | build; one delegated chat by hand (Louis, a few cents) |
| 7 | Docs: feature done, project doc, CHANGELOG, GOAL, the F20 write-up extended | — |

## Verification

`dotnet build louis-agent-solution.sln` each step; `dotnet test louis-agent-solution.sln` before the PR (core is
untouched). Headless Chrome screenshots at the three widths per step (free). Louis runs the Docker Puls
(`docker compose -f docker/docker-compose.puls.yml --env-file config/.env up -d --build`) for the by-hand checks: the
demo playing, the links, the switch, and in step 6 one delegated chat.

## Concepts

Motion as information (the delegation shown live is the page's thesis: GOAL skills 10 and 17); respecting
`prefers-reduced-motion` with an explicit opt-in; a Blazor page's JS lifecycle kept honest (start on first render,
stop on dispose); progressive enhancement (the page reads complete with no JS and no motion).

## Approved during the build

- **Step 2, the demo's pace (2026-10-15).** Louis, on seeing the turn appear in one go: "slow the animation down, and
  only start when reaching Die App". The turn starts once 60% of the frame is in view (was 30%), every beat is slower
  (typing 45–85 ms a character, a step 1.1–1.8 s, a word 70–150 ms, 5 s between turns), and reduced motion keeps that
  pace, playing one turn instead of looping, since text arriving in order is content and not movement.
- **Step 5, the loading count (2026-10-15).** Blazor's own `--blazor-load-percentage-text` instead of
  `loadBootResource`: the same real count, no script (reuse before building).
