# louis-agent.site — design

> **Explanation.** What the engine's public website is, how it's built and how it's tested. **Elsewhere:** changing
> it in [Change the site](../guides/change-the-site.md); why it's a static Razor site in
> [ADR 0066](../adr/0066-the-site-is-static-razor.md).

> **Kind:** ASP.NET Core app (`net10.0`, static server-rendered Razor components) · **Path:** `demos/louis-agent/web/louis-agent.site/` ·
> **References:** nothing of ours, on purpose · **Used by:** nobody in the solution; a visitor's browser

## Purpose

The coding agent's landing page, and its demo: it tells a .NET developer what the louis-agent framework is, why they'd want it, how they'd build on
it, shows that it works, and leads to the agent itself at `/chat`, or wherever `CHAT_ADDRESS` says it runs. It's in the framework's Console colours (black, white and metallic grays). It talks about the engine; it doesn't run it. It shows only what works
today, the same rule as Puls's landing page: no feature tile for anything that hasn't shipped.

## How it works

Every page is rendered once on the server into plain HTML and sent; there is no WebAssembly, no interactivity and no
script of our own, so a page is its markup and `css/app.css`. Seven pages: `/` (the hero with the live turn, the
numbers, the short film, what's built on it, the contact strip), `/how-it-works` (the detailed film, the parts in
plain words, the rules every turn keeps), `/packs` (every pack, and the rules a pack keeps), `/build-your-own` (a real
program and catalogue, whose is what, where it stands), `/proof`, `/built-on-it`, `/contact`. `/health` answers a container's check.

**The two films** are CSS alone. `movie/_stage.scss` is what they share: a 1280×720 stage scaled to its container
(`tan(atan2(100cqw, 1280px))` turns the width ratio into a number), a floor seen from above (`perspective` on the
camera, the camera's moves a `transform` animation on the floor), each part a slab raised off the floor
(`translateZ` plus a stack of 1px shadows for its side), the turn as a light flying above them with its shadow on
the floor, a flat detail card top right, a caption strip at the bottom. `_short.scss` (36 s, six scenes) and
`_detail.scss` (60 s, ten scenes) add where each part stands, the camera's keyframes, when each part lights up, the
light's path and when each card and caption shows, under `.film-short` and `.film-detail`, every keyframe name ending
in the film's letter. A scene's window is a share of the loop (a sixth, a tenth); the camera is wide at the start and
the end, otherwise flown in on the part at work and panned to the next. The detailed film's six slabs inside core are
the chat pipeline's middleware in its real order, outermost first (`ChatPipeline.cs`), and `DetailedMovieTests` holds
that order.

**The colours** are the framework's, `web/settings/_colours.scss`, the one copy, read through `sasscompiler.json` when the site builds: one dark ground, white for what
matters, a gray scale, and "chrome" (a white-to-steel gradient clipped to the big words). `settings/_tokens.scss` holds only what's this site's own. No light mode: the films are drawn for a dark floor. Motion is
off under `prefers-reduced-motion` for the decoration only: the films are the content and play as a video would (FX35).

## Architecture

```mermaid
flowchart LR
    browser["A visitor's browser"] -->|GET /, /how-it-works, …| site["demos/louis-agent/web/louis-agent.site<br/>Kestrel · static server rendering"]
    site --> pages["Components/Pages<br/>Home · HowItWorks · Packs · BuildYourOwn · Proof · BuiltOnIt · Contact"]
    pages --> landing["Components/Landing<br/>Hero · LiveTurn · ProofTiles · BuiltOnIt · ContactStrip"]
    pages --> films["Components/Movies<br/>ShortMovie · DetailedMovie"]
    site --> css["Styles<br/>app.scss lists: the framework's colours · tokens · elements · shell · landing · movie · responsive"]
    docker["demos/louis-agent/docker/images/Dockerfile.site"] --> site
    site -. references no engine project .-> core["louis-agent.core"]
    tool["demos/louis-agent/tools/louis-agent.film-export"] -->|renders the films| site
    tool --> films2["demos/louis-agent/docs/films<br/>short.webp · detail.webp · stills · films.sha256"]
```

## Parts

| Folder | Holds |
|---|---|
| `Components/Layout` | `SiteLayout`: the header with the menu, the body, the footer |
| `Components/Pages` | One component per route |
| `Components/Landing` | The home page's parts: `Hero`, `LiveTurn` (the streaming turn in the hero), `ProofTiles`, `BuiltOnIt`, `ContactStrip` |
| `Components/Movies` | `ShortMovie`, `DetailedMovie`: markup only; their behaviour is in `Styles/movie/` |
| `Styles` | `app.scss` the list in cascade order; one partial per concern, each under 200 lines of rules. The build makes `wwwroot/css/app.css`, which isn't in git |

## Settings

None. `ASPNETCORE_URLS` as for any ASP.NET Core app; the container listens on 8080.

## Testing

`demos/louis-agent/tests/louis-agent.site.tests`: the site started in memory with `WebApplicationFactory` (every page answers with its
title and its film, no Blazor script, `/health`); bUnit renders of both films (the scenes counted, the parts numbered
in the order the turn reaches them, core's slabs in the pipeline's order, the markup pinned to a golden beside the
test, `LOUIS_UPDATE_GOLDEN=1` to rewrite); the stylesheet's list, the framework's colours first, each film's keyframes its own.

Three tests keep the words true of the code: every type a film's card names is in the repo (`FilmNamesTests`), the program
on *Build your own* is the real one (`BuildYourOwnTests`), and every folder under `packs/` is on the *Packs* page
(`PacksPageTests`). `TheExports_MatchTheFilms` fails when a film changes and nobody re-exported it.

## Limits

- The contact page holds a `[YOUR EMAIL]` placeholder until Louis sets the address.
- No light theme, no localisation (F46 will move the texts too), no analytics.
- The films are CSS: no play, pause or scrubbing. The exports for GitHub are animated WebP, rendered from the same CSS frame by frame
  ([ADR 0077](../adr/0077-films-on-github-are-rendered-webp.md)).
- *Build your own* says the packages aren't on NuGet yet, and has to change the day they are.

---
[Docs index](../../../../src/docs/README.md) · Related: [Projects](../../../../src/docs/projects/README.md) · [Change the site](../guides/change-the-site.md)
