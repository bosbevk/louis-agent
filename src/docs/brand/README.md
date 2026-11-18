# The louis-agent brand

> **Reference.** What the mark is, how the name is written with it, which colours and type go with it, and where
> each file is used, so the site, the docs and anything we hand out agree.

## The mark: the ball with the l

<img src="mark.svg" width="96" alt="The louis-agent mark: a violet disc with a white l" />

The mark is a ball with a small l in it, the l of louis, drawn as the Geist typeface draws it at weight 650, with the
little curve at its foot. On the site it is the agent: the same mark sits in the agent's bar, breathing while it
waits, and it follows the look the visitor picked. In glass it is a glass bubble with the l in it, or a violet ball
inside clear glass. In flat, and everywhere the mark is small or printed, it is a flat disc in the brand's violet
with a white l. That flat disc is the one in this folder, and the one in the browser's tab and on a phone's home
screen. The card a link shows when it is shared is large, so it carries the glass ball, in the site's default look,
glass and light.

There is a one-ink version for paper and for anything that can only print one colour: a black disc with a white l.

## The name, with the mark as its o

The name is `louis-agent`, always lowercase and always with its hyphen. Where there is room, it is written in
Geist at weight 650, set tight, with the mark standing in for its o: `l`, the ball, then `uis-agent`. The ball is
about three quarters of a letter's height and sits on the line, like a letter. The site's header draws it that way
with the framework's `Wordmark`, which takes the letters before the mark and after it, and tells a screen reader
the whole name once.

Give the name or the mark clear space of half the mark's height on every side. The mark on its own is never smaller
than 16 pixels. Don't recolour the disc, tilt or stretch it, set the name in another font ("Louis Agent"), or put
the mark beside the name as well as inside it.

## The files

| File | What it is | Where it goes |
|---|---|---|
| [mark.svg](mark.svg) | The flat violet disc `#6b4dff` with the white l, on a 120 by 120 grid | The README, the docs, anything on screen |
| [mark-light.svg](mark-light.svg) | The one-ink mark: a black disc with a white l | Print, and anything that can only take one colour |
| [nuget-icon.png](nuget-icon.png) | 128 by 128, the disc on a clear ground | The NuGet packages' `PackageIcon`, when they ship (F74) |
| [avatar.png](avatar.png) | 512 by 512, the disc on a violet square, so a round crop shows it whole | The GitHub avatar, any square profile picture |
| [social-card.png](social-card.png) | 2400 by 1260, drawn at twice its 1200 by 630 points, the size a link preview asks for: the name with the glass ball as its o, the only ball on the card, then the slogan "The website that answers." and the site's address, on a light ground | The site's `og:image`, link previews |
| [social-card-light.png](social-card-light.png), [social-card-dark.png](social-card-dark.png) | The same card on a light ground and on a dark one | To choose between, and anything that wants the dark one |

The framework's site keeps its own copies, since a site references no docs folder: `wwwroot/favicon.svg` and
`wwwroot/brand/mark.svg` are `mark.svg`, `apple-touch-icon.png` is the disc at 180 by 180 on violet with no
transparency, as Apple asks, `icon-192.png` the disc at 192 by 192, and `social-card.png` the card.

The SVGs are drawn by hand: a circle of radius 60 at the middle of a 120 by 120 grid, and the l as Geist's own outline
at weight 650, moved with `translate(44 92) scale(.09 -.09)` to the middle of the disc. The PNGs are drawn from
`mark.svg` by headless Chromium, the browser Playwright drives: the SVG laid at the size wanted on a page of that
size, over the ground each row of the table names, and the page photographed.

The social card is drawn by the repo's own tool, `film-export`, with `social-card`. It starts the site, opens the
landing's brand section in glass and light, and photographs the ball alone on nothing, so the card's glass is
exactly the site's. It lays that photograph into the card's page, a template beside the tool
(`Cards/social-card.html`) with Geist and Geist Mono loaded as the site loads them, and photographs the card at twice
its size. Run it again whenever the ball's look changes; the guide to changing the site says how.

## Colour

The brand's colour is one purple, Ultraviolet, in five steps from light to dark. The middle one, `#6b4dff`, is the
disc and the main button. Both themes set the five as the framework's violet names, `--web-violet-0` to
`--web-violet-4`.

| Step | Hex | Used for |
|---|---|---|
| 0 | `#e2dfff` | The lightest touch: a glint, a soft ring |
| 1 | `#9282ff` | Hover on the main button; the purple on a dark page |
| 2 | `#6b4dff` | The mark's disc, the main button, what is the brand's to press |
| 3 | `#3a22c4` | Pressed, and the deep side of the ball |
| 4 | `#110a40` | The darkest: shade inside the ball |

Everything else is the page's own: black or near black with white words in the dark, a pale page with black
words in the light. There is no rainbow. A gradient stays within the one purple, white into it or the purple into a
lighter step of itself.

## Type

Geist, at 400 to 700, is for the words people read: headings at 600 and above, set tight, and body text at 400.
Geist Mono, at 400 and 600, is for numbers, code, the address and the small uppercase labels above headings. It is
never used for body text.

## Voice

The slogan is "The website that answers." It stands under the name in the header, in the footer and on the
social card, and it speaks to a business owner. Say things like "Every number here comes from the ledger or an eval run" and "Build agents, not infrastructure."
Use plain words, explain a term the first time, and only talk about what works today. Stay away from
"revolutionary", "magical" and "next-gen", from any number nobody measured, and from robots, brains and sparkles.

---
[Docs index](../README.md) · Related: [the themes' design doc](../projects/louis-agent.web.themes.md)
