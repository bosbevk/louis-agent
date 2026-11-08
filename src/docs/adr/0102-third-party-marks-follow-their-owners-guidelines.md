# ADR 0102: Third-party marks follow their owners' guidelines

**Status:** Accepted (2026-11-08, FX82)

## Context

The Contact page got a WhatsApp button (FX75) with the word "WhatsApp" and no mark. Louis asked for the logo, and
for the rule behind it: "follow whatsapp guidelines, we should always follow 3rd party guidelines, they have
guidelines." A site that shows another company's name, mark or button is using that company's brand, and every
such company publishes how its brand may be used.

## Decision

When the site shows a third party's mark, name or button, it follows that party's own brand guidelines:

- **The official mark, unaltered.** The glyph as the owner publishes it (an inline SVG of the official path), never a
  redrawn or recoloured version, at a size that keeps its proportions and clear space.
- **Their colour and their ground.** WhatsApp's green (`#25D366`) with the mark in white, or the mark in white on a
  dark ground; a sign-in provider's button in that provider's colours.
- **Their spelling and their wording.** "WhatsApp", not "Whatsapp"; a call to action in their words ("Chat on
  WhatsApp"), not ours.
- **The guideline is named** in the component's comment, so the next change can check against it.

A third party's colour is a token in the site's settings (`--whatsapp`), like the site's own.

## Consequences

- The WhatsApp buttons carry the official glyph in WhatsApp green with "Chat on WhatsApp"; a test holds the mark,
  the words and the rule.
- A new third-party mark starts with reading its guidelines, not with drawing.
- A guideline can change; the comment names where it was read, so a review can re-read it.

## Links

- WhatsApp's brand guidelines, at Meta's brand resource centre (read 2026-11-08).
- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)

---
[Docs index](../README.md) · [ADRs](README.md)
