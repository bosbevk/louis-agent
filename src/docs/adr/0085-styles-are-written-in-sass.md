# 0085. Styles are written in Sass, and anything shared is reused

**Status:** Accepted · **Date:** 2026-11-06 (Louis, on the colours every site shares: "maybe we should chnage to less? or something
similar", then Sass over plain CSS, and "while building scss we must make sure to always reuse") · **Recorded:** 2026-11-06

## Context

Every site is built on the framework's web ([ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md)), so its rules can't hold one
site's colours. A rule that needs a colour see-through reads its numbers, `rgba(var(--bs-primary-rgb), .4)`, so each colour needs two
forms, and a site that set only one would get its own button with Bootstrap's blue glow.

Less was the first thought. Bootstrap itself moved from Less to Sass, so Sass is the one that fits what every site already loads. Plain
CSS with `color-mix` was the other option, and needs no build step. Louis picked Sass for its nesting, its shared pieces and one place for
each colour.

## Decision

**A web app's styles are Sass, under `Styles/`, and the build makes one CSS file from them.** `AspNetCore.SassCompiler` compiles them on
every build, with no Node. `app.scss` is the ordered list of partials, and that order is the cascade. The CSS isn't in git.

**CSS variables stay.** A Sass variable is fixed when the build runs, and the code-review app swaps themes while it runs, so what a rule
reads at run time is a CSS variable. Sass writes those variables from one value, through `tools/_colours.scss`.

**Sass covers our own styles.** Bootstrap stays on its CDN and reads the same variables, so we don't own its source or its upgrades.

**Always reuse.** Before writing a rule, look for a variable, mixin or partial that already does it. What repeats becomes one shared piece
that both places use, and a site `@use`s the framework's Sass and never copies it.

## Consequences

- A colour is written once, so its two forms can't disagree.
- A site that changes its colours needs a compile, which its build already does.
- A style test reads the CSS the build made, through `SassStylesheet`, so it holds what the browser really gets. A build error in a
  partial fails the build, rather than writing the error into the page.
- Existing rules keep their text and take nesting as they're touched, never in a sweep.
- Each image build compiles too, and the package carries Dart Sass's binaries, at build time only: none of it reaches the browser.
- The code-review app and its themes stay on plain CSS until it moves onto the frame in F59.7.

## Links

- [ADR 0053](0053-stylesheets-by-concern.md): stylesheets by concern. [ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md):
  every site is built on the framework's web. [ADR 0030](0030-build-or-adopt.md): build or adopt.
- `AspNetCore.SassCompiler` (MIT), `.claude/rules/css.md`, `SassStylesheet` in `src/tests/web/louis-agent.web.testing`.

---
[ADR index](README.md)
