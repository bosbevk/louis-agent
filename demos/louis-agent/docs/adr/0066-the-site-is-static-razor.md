# 0066. The engine's site is static Razor, and its films are CSS

**Status:** Superseded in part by F109.1 (2026-11-08): the site's pages are the code-review app's, Blazor WebAssembly on
the framework's web, in a frame the framework provides; the films stay CSS and their export stays as ADR 0077 says ·
**Date:** 2026-11-03 · **Feature:** F54

## Context

The engine needs a public website for .NET developers before it ships as a package (Phase 10). Louis asked for it on
2026-11-03: "this is for the engine Puls runs on", "black and white with metallic grays", "modern, sleek", and "we
need animations of how the engine works", "almost like a movie". Three ways to build the site were put to him (Blazor
static server rendering, DocFX, hand-written HTML) and three ways to make the films (CSS keyframes, a Three.js
scene, a rendered video).

The repo's web apps are Blazor WebAssembly with markup-only `.razor` files and stylesheets by concern (ADR 0034,
ADR 0053). Everything is C# on purpose. The site is content: no accounts, no API, no state.

## Decision

- The site is **`demos/louis-agent/web/louis-agent.site`, ASP.NET Core with static server-rendered Razor components**: no WebAssembly,
  no interactivity, no script of our own. A page is HTML and CSS.
- It **references no engine project**. It talks about the engine; it doesn't run it.
- It **shows only what works today**, as Puls's landing page does; what's planned is at most one honest line.
- Every sentence comes from the docs (the README, the architecture, the ADRs, the write-ups) and every number from
  the ledger or an eval run.
- **The films are CSS keyframes** over CSS 3D transforms: the parts as slabs on a floor, the turn as a travelling
  light, a flat card per scene. What both films share is one stylesheet; each film's positions, camera and timing are
  its own file under its own class, with keyframe names that end in the film's letter.
- An export is rendered from the same CSS by a headless browser, frame by frame; the CSS stays the source ([ADR 0077](0077-films-on-github-are-rendered-webp.md)).

## Consequences

- The site builds, tests and ships like the other hosts (bUnit, a golden per film, the stylesheet's import list
  tested, a Dockerfile), and stays C#.
- No JavaScript means no play, pause or scrubbing on a film, and no analytics without adding something.
- A film's timing is percentages of a loop in a stylesheet: changing a scene's length means re-cutting every window
  in that file. The how-to says how.
- The pipeline's order in the detailed film is a test, so a change to `ChatPipeline.cs` that reorders the middleware
  fails the site's test until the film follows.

## Links

- [louis-agent.code-review](../projects/louis-agent.code-review.md), [Change the code-review web app](../guides/change-the-web-app.md)
- [ADR 0034](../../../../src/docs/adr/0034-code-a-person-can-maintain.md), [ADR 0053](../../../../src/docs/adr/0053-stylesheets-by-concern.md)

---
[Docs index](../../../../src/docs/README.md) · Related: [ADRs](../../../../src/docs/adr/README.md)
