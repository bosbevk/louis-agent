# 0077. The films on GitHub are animated WebP, rendered from the site's CSS

**Status:** Accepted · **Date:** 2026-11-05 (Louis: "we should show those animations on the github also"; he picked WebP from
three options, and 24 frames a second at quality 75 after seeing both on GitHub; the encoder was decided unattended, below) · **Feature:** F57

## Context

The site's films are CSS keyframes ([ADR 0066](0066-the-site-is-static-razor.md)). A README can't run them: GitHub strips CSS and
scripts from markdown. We measured the short film (36 s, 12 frames a second, 960 px wide):

| Format | Size | Plays by itself | Catch |
|---|---|---|---|
| Animated WebP, quality 75 | 2.67 MB | Yes | Slightly softer text |
| GIF | 21.95 MB | Yes | Eight times the size |
| MP4 | 1.25 MB | No | Must be uploaded by hand on github.com; GitHub removes autoplay and loop |
| SVG with the CSS inside | 41 KB | In Chrome | Reports conflict on GitHub keeping it, Safari unknown, loses the font |

## Decision

- **A film on GitHub is animated WebP,** at 24 frames a second, quality 75, 960 px wide. That makes the short film 6.28 MB and the long one 14.63 MB.
- **The CSS stays the only source.** `louis-agent.film-export` renders it in a headless browser, setting each animation's time so
  every frame lands exactly where it should, and encodes the frames.
- **The encoder is libwebp's `img2webp`, with a keyframe every half second,** from a pinned Docker image, so nobody installs it and
  it never reaches a product. Between keyframes the encoder treats a small change as no change, so a slow fade on low-contrast text
  never gets written: with ffmpeg, which can't force keyframes, a card kept a ghost of the last card's code line. Scored against the
  source frames on that line, lower being truer: ffmpeg at 75 gave 6.27, ffmpeg at 85 gave 2.10, `img2webp` at 75 with keyframes gave
  2.48, about 22% larger than ffmpeg at 75 and smaller than ffmpeg at 85. The ffmpeg version made no difference.
- **A still goes with each film,** through `<picture>` and `prefers-reduced-motion`, for people who asked for less motion. The first
  frame is the wide view with every part.
- **A stale export fails a test.** `films.sha256` holds a hash of each film's own files at its export; `TheExports_MatchTheFilms`
  compares it with today's, so a film can't change while the README shows the old one.
- **The short film is in the README, the detailed one in the architecture's runtime view,** so the README doesn't load two.

## Consequences

- Changing a film means running the tool, which takes minutes and needs Docker, and committing what it writes. Each re-export adds
  the films' size to git history, so it's worth grouping changes.
- The test checks the film's own files, not the WebP's pixels. A change outside them, such as the fonts, wouldn't trip it.
- Tested on real GitHub in Chrome and Edge, including the reduced-motion still. Firefox and Safari get checked on the pull request.

## Links

[ADR 0066](0066-the-site-is-static-razor.md) · [Change a film](../../../../src/docs/guides/change-the-site.md#change-a-film) ·
[louis-agent.code-review](../projects/louis-agent.code-review.md) · [ADR 0004](../../../../src/docs/adr/0004-arguments-as-data.md)

---
[Docs index](../../../../src/docs/README.md) · Related: [ADRs](../../../../src/docs/adr/README.md)
