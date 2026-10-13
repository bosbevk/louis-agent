# F20 · Puls look: plan

> Approved 2026-10-13 · Feature: [F20 Puls look](../features/F20-puls-look.md) · Branch: `feature/F20-puls-look` (off
> `feature/F5-toolset-profiles`)

## Context

Louis wants Puls to look like the *Sensai* AI template, in his own colours and dark, with a landing page and the chat
as a three-panel dashboard (both), folding in the placeholders of Preline's *AI Chat Interface*. The logo (ripple mark),
Montserrat, the Afrikaans text and the tagline stay. Branding may change where the design needs it; the current brand
files are then kept under `wwwroot/brand/legacy/`.

## Design

- **Dark theme:** deep charcoal page (`#1c1f22`), surfaces a step lighter, text the charcoal-tinted white (`#efefeb`),
  orange for actions, the light green (`#38b449`) for answers and done; soft orange and green glow blobs behind the hero
  and the empty chat. The logo's white versions.
- **Landing page at `/`:** floating pill nav, hero with the tagline, a glow and the pulse, two calls to action, a picture
  of the dashboard, *Hoe dit werk* in three steps, a features grid, FAQ and footer. The chat moves to `/gesels` and
  `/gesels/{id}` (the old `/chat/{id}` links redirect).
- **Three panels:** sidebar (logo, *Nuwe gesprek*, ⌘K search, *Tuis/Verken/Studio*, *Projekte/Gunstelinge/Geargiveer*,
  recent chats with a ⋯ menu, a profile card); the chat (top bar with title, *Kry Plus*, language and profile menu;
  empty state with a banner, topic cards and the composer centred; composer with Media, Tools, Model and Voice; message
  actions); the *Werk* panel on the right (the current delegation: specialist, toolsets, steps, sources, cost), which
  folds away.
- **Real vs placeholder:** as in the feature doc; a placeholder shows a "Kom binnekort" toast.
- **Our own code in Puls's style:** both templates are paid products; only their ideas are used.

## Steps

One commit each; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan | — |
| 1 | Dark theme across the app, with glows | build; desktop and phone screenshots |
| 2 | Landing page; the chat moves to `/gesels` | build; screenshots of `/` and `/gesels` |
| 3 | Three-panel layout; the *Werk* panel shows the live delegation | build; screenshots; a delegation by hand (Louis) |
| 4 | Sidebar contents; rename and archive; ⌘K search | build; screenshots; rename, archive, search by hand |
| 5 | Top bar, profile menu, the "Kom binnekort" toast | build; screenshots |
| 6 | Empty state and composer: banner, topic cards, Media/Tools/Model/Voice | build; screenshots |
| 7 | Message actions; sources in the *Werk* panel | build; copy by hand; sources from a real answer (Louis) |
| 8 | *Verken* and *Studio* placeholder pages | build; screenshots |
| 9 | Docs and the learning write-up | — |

## Verification

`dotnet build` per step; headless Chrome screenshots at desktop and phone widths (free). No model calls in the build;
Louis tries real chats (delegation, sources) himself.

## Concepts

User experience for agent transparency: making delegation, a sub-agent's work, its sources and its cost visible beside
the answer, building on F5's live delegation view (GOAL: observability of agents, and human oversight of what they did).

## Approved during the build

- **2026-10-13, pay as you go, with visible usage** (Louis: "on branding ... we have to advertise it as pay as you go,
  with visible usage"): the landing page says *Betaal soos jy gebruik* and shows a question's cost; the top bar's
  *Kry Plus* becomes a usage meter (this chat, this month) and the profile menu's *Opgradeer plan* becomes *Gebruik*. No
  plans or subscriptions anywhere. Built when F20 resumes (after F22).
