# The louis-agent brand

> **Reference.** What the logo is, which colours and type go with it, and how to use them. It names what the site
> already does (the framework's Console theme), so the logo and the site agree.

The canvas with the four concepts it was picked from, and the mock-ups, is
[louis-agent logo and brand](https://claude.ai/artifact/EdaD9zM6GivfF3AQhsh2vo).

## The mark: three slabs

<img src="mark.svg" width="96" alt="The louis-agent mark: three stacked slabs" />

Three stacked slabs: the **engine** at the bottom, **identities** in the middle, **packs** on top. They are the
framework's own three words, stacked the way the site's films stack them. The top slab is solid steel, and the two
below it are strokes in two grays, lighter going up.

| File | What it is | Where it goes |
|---|---|---|
| [mark.svg](mark.svg) | The mark on a dark ground: steel gradient `#ffffff` → `#b4b9c0` | The site, the README, anything dark |
| [mark-light.svg](mark-light.svg) | The mark on a light ground: solid Ink `#15171a`, no gradient | Docs, print, anything light |
| [nuget-icon.png](nuget-icon.png) | 128 × 128, the mark on the Floor colour | The NuGet packages' `PackageIcon`, when they ship (F74) |
| [avatar.png](avatar.png) | 512 × 512, the mark on the Floor colour | The GitHub avatar, any square profile picture |
| [social-card.png](social-card.png) | 1280 × 640, the lockup, the line, the site's address and the slogan (FX99: no repository address, the repository is private) | The site's `og:image`, link previews |

The coding agent's web app (the site) shows the lockup in its header (F110): its own copy of the mark,
`demos/louis-agent/web/louis-agent.code-review/wwwroot/brand/mark.svg`, the drawing alone without the export's
manifest, then the name in the mono face; and its own copy as its favicon, `wwwroot/favicon.svg`, ink on a light
browser tab and steel on a dark one. A demo references no framework folder, so it keeps copies (ADR 0074).

The SVGs are drawn on a 64 × 64 grid: the top slab is the diamond `32,9 54,20 32,31 10,20` and the two strokes below it
are 4.5 wide.

## The lockup

The mark, then the name `louis-agent` in **Geist Mono 500**, always lowercase and always with its hyphen. The name is a
little taller than the mark is wide, and the gap between them is about a third of the mark.

- **Clear space:** half the mark's height on every side.
- **Smallest:** the mark alone at 16 px; the lockup at 120 px wide.
- Use the lockup wherever there's room, and the mark alone for the favicon, avatars and the NuGet icon.
- **Don't:** colour it, tilt or stretch it, add a glow or shadow, or set the name in another font ("Louis Agent").

## Colour

These are the Console theme's colours (`src/web/louis-agent.web/Styles/web/settings/_colours.scss`); nothing here is new.

| Name | Hex | Used for |
|---|---|---|
| Floor | `#070708` | The page |
| Panel | `#0d0e10` | Cards and panels |
| Ink | `#15171a` | Menus; the mark on light |
| Hairline | `#1b1d21` | Borders and dividers |
| Steel | `#ffffff` → `#b4b9c0` | The mark; one or two things that matter on a page |
| White | `#ffffff` | Actions, headings |
| Soft text | `#c9ccd1` | Body copy, links |
| Small print | `#8a9098` | Labels under 24 px |
| Error | `#d9675f` | Failures only |
| Warning | `#d4a72c` | Warnings only |

The only colours with a hue are for errors and warnings. Steel is never a background wash.

The site's `--faint` (`#5c6168`) is too faint for small text on the Floor: it's about 3.3 to 1, and small text needs
4.5 to 1. Use Small print (`#8a9098`) for any text under 24 px.

## Type

- **Geist** (400 and 600) is for words people read: headings at 600 with tight tracking, body at 400.
- **Geist Mono** (400 and 500) is for the name, numbers, code and the small uppercase labels above headings
  ("MEASURED, NOT ASSUMED", tracked at 0.14 em). Never for body copy.

## Voice

- **Say:** "Every number here comes from the ledger or an eval run." "Build agents, not infrastructure." Plain words,
  a term explained the first time, and only what works today.
- **Avoid:** "revolutionary", "magical", "next-gen"; a number nobody measured; robots, brains, sparkles and glowing
  gradients.

---
[Docs index](../README.md) · Related: [the site's design doc](../../../demos/louis-agent/docs/projects/louis-agent.code-review.md)
