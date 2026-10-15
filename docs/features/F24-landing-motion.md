# F24 · Landing motion: the landing page as a moving page, and the app with it

> **Status:** in progress (2026-10-15) · **Milestone:** — (Puls, outside the cost milestones) · **Depends on:** F20
> (the landing page and the three panels), F5 (live delegation)
>
> **In the TODO:** **F24 Landing motion** under *Sessions and memory* (the Puls lines) ([TODO](../../TODO.md)) ·
> **Plan:** [plans/F24](../plans/F24-landing-motion.md) · **Reference:** the artifact Louis reviewed, version 24
> (https://claude.ai/artifact/5eVDZwccRjgxc6XmqSy8cD; its source is kept as `docs/specs/images/puls-landing-v24.html`
> only if asked; otherwise the repo page is the source)

F20 gave Puls a landing page and the shape of a product. F24 makes that page move the way current one-page sites do,
on the brand's own idea: a *puls* is a single disturbance that travels away from its source, and a succession of
pulses forms a wave (af.wikipedia.org/wiki/Puls). The page tells that story and shows the app playing a real turn in
the page. The app at `/gesels` gets the same motion language where it fits, and nothing else about it changes.

Built from three references Louis picked, in Puls's colours: Screen Movie (a product frame that flattens as it
scrolls, a "latest" pill, content bright mid-screen), Grilled Pixels (a percentage loader, a vertical section index,
labels that decode from glyphs), Norma (a pinned section whose chips light up as you scroll, big-number tiles).

## User stories

**F24-S1 — Understand Puls from the page.** As a *visitor*, I want the landing page to show what Puls does rather than
tell me, so that I know what I'm starting before I chat.
- *Given* `/` *then* the hero states the thesis (the definition as the eyebrow, the headline word by word) over dark
  water; under it the app's three panels play a scripted turn (a question typed and sent, a delegation card busy, the
  Werk panel's steps ticking, the answer streaming, the cost counting up), three questions in turn, then *Speel weer*.
- *Given* I scroll *then* *Hoe dit werk* is three cards in one screen with a stage each; *In die app* pins and switches
  its screen as four chips light up (clicking a chip goes there); *Kenmerke* is a bento whose tiles each show a bit of
  their feature, over two marquees of the toolsets and tools; *Syfers* counts up the measured numbers; *Pryse*' example
  costs count up; *Vrae* is an accordion, one open at a time.
- *Given* any nav or index link *then* one smooth scroll lands the section centred in the screen when it fits, else
  just under the nav.

**F24-S2 — The app feels like the page.** As a *user*, I want the chat to share the page's motion, so that the product
feels like one thing.
- *Given* an empty chat *then* the water sits low behind it, the headline arrives word by word, and the topic cards tilt
  and light up under the pointer as the landing's cards do.
- *Given* a delegation *then* the Werk panel's steps and sources open with the accordion's motion, and the cost counts
  up when it arrives; the usage meter counts too.
- *Given* the app loading *then* the start screen shows the mark beating with the real percentage of the app's files
  fetched.

**F24-S3 — Less motion when asked, and a way back.** As a *user* whose system asks for less motion, I want a still
page and the choice to see the motion anyway, so that the setting is respected without hiding the page's point.
- *Given* `prefers-reduced-motion: reduce` *then* nothing moves, the demo shows its finished state, and a pill (landing)
  or a switch in the sidebar settings (app) turns the motion on, remembered in this browser.

## What's real and what's example data

Real: the app's own components and styles in the frames (delegation card, Werk panel, composer, chips), the numbers in
*Syfers* (the toolset-profile benchmark, the delegation check, the demo run, the README cost), the FAQ and pricing copy
from F20. Example data, marked as such in the code: the demo's three turns and their answers and costs, the commit list
in the *Geskiedenis* panel, the bars in the cost charts.

## Not in scope

A light theme; any new chat behaviour; the credits flow (F23); the artifact's fake loading count (the app shows the
real one).

## Done when

- The landing page matches the reviewed artifact at desktop (1440 px), tablet (1100 px) and phone (420 px) widths,
  checked by headless-Chrome screenshots, and plays the demo in a real Chrome.
- Every nav and index link lands as S1 says, clicked by hand.
- The app's four pieces (S2, S3) work in a real delegated chat (Louis, a few cents) and with Windows' animation
  effects off.
- `dotnet build` and `dotnet test louis-agent-solution.sln` pass (core is untouched).
- Then finish up ([Finishing a feature](README.md#finishing-a-feature)): the TODO line out, the CHANGELOG line in,
  *Status* done here and in the features table, `docs/projects/louis-agent.puls.md` and GOAL updated, the F20
  learning write-up extended.

## What the build changed

(filled in step by step)

- **Step 2, the demo is the app's own markup.** The playing frame (`Components/LandingDemo.razor`) builds its turn
  out of the app's classes (`user-message`, `delegation`, `work-item`, `work-step`, `work-cost`), so a change to the
  app's look changes the demo with it and nothing in the demo can drift from the real thing; only the frame's layout
  is its own (`demo-*`). The turns are example data in C#, serialised into a `data-turns` attribute, so the copy sits
  with the page's other copy and the script stays a player. The F20 picture (`.mock*`) and the hero's rings went: the
  water is the pulse now. The turn starts once most of the frame is in view and plays at a reading pace; a reader
  who asked for less motion still gets that pace, because text arriving in order is content and not movement, and
  only the auto-loop is dropped (Louis, on seeing it appear in one go: "slow the animation down, and only start when
  reaching Die App").
- **Step 3, the copy stays data.** The bento's tiles, the tour's chips, the chart heights and the marquee's tags are
  C# arrays on the page, rendered by Razor (the marquee twice over, so its loop is seamless), so no script builds
  markup from literals. The stages, the tour's panels and the tiles' extras are the app's own pieces again
  (`delegation`, `user-message`, `work-steps`). One trap: Bootstrap's `.row` is loaded in the app, so the tour's cost
  rows are `cost-row`; a landing class must not collide with a Bootstrap grid class.
- **Step 4, the numbers are the measured ones.** Syfers counts up four numbers from the learning write-ups (the
  toolset profiles' prompt saving, the puls check's 24/24, the demo's 10/10 with its cache saving, the README's cost);
  they are data on the page with what each one is and how it was measured, so a new measurement changes one line.
  The prices are the same cards as Kenmerke, the examples counting up. Vrae is an accordion built from the same
  `Questions` array as before, one open at a time, the first open; the band's rings are the app's own `pulse-bg`
  rather than a copy. The F20 landing CSS (`.mock*`, `.step`, `.features`, `.price-card*`, the `details` FAQ) is gone.
- **Step 5, the loading count is Blazor's own.** The plan had the start screen count files through
  `Blazor.start({ loadBootResource })`; the runtime already publishes its progress as
  `--blazor-load-percentage-text` on `<html>` as each file arrives, so the start screen's count is that variable in a
  `::after` and no script was written (reuse before building). The *Animasies* switch is one browser-side setting:
  `data-motion="on"` on `<html>`, applied by `index.html` before the first paint and lifted by `pulsUi.setMotion`,
  which the sidebar's checkbox and the landing's pill both call; `app.css` guards its two reduced-motion blocks with
  `:root:not([data-motion="on"])`, so one switch covers the landing and the app. Checked on Louis's Windows, where
  Animation effects is off and Chrome reports reduced motion: the pill shows, a press starts the page over with
  motion, the app's checkbox shows the choice, unticking it stills the rings again.
- **The gradient line comes in without blur.** The headline animates word by word with a blur, except the gradient
  line: a `filter` on text that is `background-clip: text` makes Chrome drop the text, so that line gets the same
  rise without the blur.

---
[Features](README.md) · [Plan](../plans/F24-landing-motion.md)
