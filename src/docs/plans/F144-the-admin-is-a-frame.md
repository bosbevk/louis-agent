# F144 The admin is a frame, and each part of it is an area that plugs in

> **Plan**, after F127. Louis, 2026-11-13, having found "not found" at louis-agent.dev's `/admin`:
> - "should we not have an admin landing page?", "in the framework where areas plug in"; his pick of three, a home of
>   cards;
> - then "admin must be the main admin layout for admin areas to plugin into, overview is an overview area, evals is
>   the eval area inside admin layout. etc."
>
> This file changes only under *Approved during the build*.

## Context

`.Admin(layout, views, counts)` mixes three things: the frame, the packs' screens and their views, and (since F127.8)
the overview's counts. `/admin` belongs to the eval studio on the sites that have it, and is "not found" on the
others. The evals already plug in as an area of their own (`EvalArea`: its pages and its sidebar links, F127.6). The
aim is that the admin is only the frame and its home, and everything inside it is an area that plugs in the same way.

## Design

### What changes, in plain words

- **The frame.** `.Admin(layout)` names the admin's frame and its home at `/admin`, and nothing else. Its sidebar has
  "Home" first, then each admin area's links in the order the site named them.
- **The home** draws each admin area's card in a grid: a title, what the area has to say, and "Open →".
- **The areas inside it**, each with its pages, its sidebar links and its card:
  - the overview, `.Overview(counts)`;
  - the packs, `.Packs(views)`, for their screens and uploads;
  - the evals, `.Evals()`, whose studio moves from `/admin` to `/admin/evals`.
- **A guard.** An admin area named without the frame is refused at start-up, with the reason.

### Interface design

![The frame's slot for a card, the areas that fill it, and the home that draws them](diagrams/F144-the-admin-is-a-frame-interface.svg)

```csharp
// louis-agent.web, Components/Areas/ (step 2)
public sealed record SiteArea(string Name, Action<IServiceCollection> Services, IReadOnlyList<Assembly> Pages,
    Type? AdminLinks = null, Type? AccountLayout = null, Type? AdminCard = null);
public IReadOnlyList<Type> AdminCards { get; }                     // SiteAreas, in the order named
public SiteAreasBuilder Admin(Type layout);                        // the frame alone
public SiteAreasBuilder Overview(Action<OverviewCountsBuilder>? counts = null);   // step 3
public SiteAreasBuilder Packs(Action<PackViews>? views = null);                   // step 4
// louis-agent.web.evals (step 5): Evals() gains its card; the studio's page at /admin/evals
```

### Architecture

![The frame and its home in the framework's web; the overview and packs areas beside it; the evals area in its own library; each site names its own](diagrams/F144-the-admin-is-a-frame-architecture.svg)

### Communication: Louis opens `/admin`

![The home asks the site's areas for their cards, and each card fetches what it shows](diagrams/F144-the-admin-is-a-frame-communication.svg)

### Object diagram (code-review's admin)

![The areas code-review names, and the cards and links each brings](diagrams/F144-the-admin-is-a-frame-objects.svg)

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `SiteArea`, `SiteAreas`, `SiteAreasBuilder` (changed) | `src/web/louis-agent.web/Components/Areas/` | `LouisAgent.Web.Components.Areas` | 2 |
| `AdminCard` (the card's frame) | `src/web/louis-agent.web/Components/Admin/` | `LouisAgent.Web.Components.Admin` | 2 |
| `OverviewCard`, `OverviewLinks` | `src/web/louis-agent.web/Components/Admin/Overview/` | `LouisAgent.Web.Components.Admin.Overview` | 3 |
| `PacksCard`, `PackMenu` (moved as the packs' links) | `src/web/louis-agent.web/Components/Screens/` | `LouisAgent.Web.Components.Screens` | 4 |
| `EvalsCard`, `Evals` page (moved route), `EvalLinks` | `src/web/louis-agent.web.evals/` | `LouisAgent.Web.Evals…` | 5 |
| `AdminHome` | `src/web/louis-agent.web/Pages/Admin/` | `LouisAgent.Web.Pages.Admin` | 6 |
| each site's areas (changed) | the sites' Composition Roots | their own | 7 |

## Steps

Each step is test first and one commit, and stops for Louis's review.

1. **The plan**, its four pictures drawn, F144 in the TODO, and ADR 0120 amended.
2. **The slot and the frame**: `SiteArea.AdminCard`, `SiteAreas.AdminCards`, the `AdminCard` component,
   `.Admin(layout)` as the frame alone, and the refusal. Tests: the cards' order; the refusal.
3. **The overview area**: `.Overview(counts)` with its page, its link and `OverviewCard`. Tests: the counts moved; the
   card against a stub.
4. **The packs area**: `.Packs(views)` with the screens, the uploads, `PackMenu` as its links, and `PacksCard`. Tests:
   the admin shell's and screens' tests; the card.
5. **The evals area in the frame**: the studio at `/admin/evals`, `EvalLinks` and `RunHead` following, and
   `EvalsCard`. Tests: the studio's route, its sidebar links, the card.
6. **The home**: `AdminHome` at `/admin`, drawing the cards, with "Home" first. Tests: each named area's card in order;
   with none it says so.
7. **Every site names its admin areas**: louis-agent.dev, code-review, Puls, the starter and its template. Tests: each
   site's routes, sidebar order and goldens; `NoRoute_IsTwice`.
8. **Checked live, documented**:
   - louis-agent.dev and code-review, at desktop and phone width;
   - the web's design doc, the spawn-a-site guide, the work log, the write-up;
   - the plan deleted;
   - the full suite, and one PR.

## Verification

- Each step runs the tests of what it touches, plus `core.tests` for new folders and names.
- Live on the PC with a test admin key: louis-agent.dev's `/admin` shows the overview card. code-review's shows the
  overview, packs and evals cards, and its studio opens at `/admin/evals`.

## Concepts, and where they sit in GOAL.md

- **Extensibility** (#20): one slot a part fills, so a new area adds its pages, links and card without the frame
  changing.

## Approved during the build

---
[Docs index](../README.md) · [Plans](README.md)
