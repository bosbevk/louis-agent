---
paths:
  - "**/*.css"
  - "**/*.scss"
---

# CSS and Sass: the how of every rule

Loaded when a `.css` or `.scss` file is read or edited.

The rules come in the order you meet them: what to do before a rule is written, which file it goes in, how a
colour is written, names and paths, what paper needs, how rules are moved, and what the tests hold.

## Before you write a rule

- **Always reuse.** Look first for a variable, a mixin or a partial that already does it. What repeats becomes
  one shared piece under the framework's `web/tools/`, which both places use. A site `@use`s the framework's Sass
  and never copies it. A step says what it reused and what new piece it added.
- **Edit the partial, never `app.css`.** Styles are Sass, under `Styles/`, on every site. The build makes
  `wwwroot/css/app.css`, which isn't in git.
- **Existing rules take nesting as they are touched**, never in a sweep.
- **Look at it in the browser.** A step that changes a style looks at the page at desktop and at phone width,
  390 px with no sideways scroll, before it is shown. Use `/check-a-page`.

*Decided in:* [ADR 0085](../../src/docs/adr/0085-styles-are-written-in-sass.md).

## Which file a rule goes in

A rule has one of two homes: beside the component that draws its element, or a global partial.

**Beside its component**, when one component draws the element:

- The rule goes in `X.razor.scss` next to `X.razor`. The build compiles it scoped, as `.card[b-k3x9z2m1qa]`, and
  bundles it into `{site}.styles.css`, which is linked once after `app.css`.
- The generated `X.razor.css` is not in git. Deleting the `.scss` means deleting it too.
- **The file is the component that draws the element** a rule lands on, not the one that asks for it. A class
  passed down as a parameter is styled in the child that draws it. A component that draws no element of its own
  holds no rules (FX63).
- A rule several components draw goes in their nearest common ancestor's file, under `::deep`. It comes after the
  compound the ancestor draws, or first when the ancestor draws none.

**A global partial**, when any of these is true:

- A site's own page draws its class.
- A site's Sass or a theme overrides it with no more weight. The scope adds a class of weight.
- It is a variant over a base that stays.
- It is a keyframe.
- It is a part many components draw, such as `.card-p`, `.page-head` or `.muted`.

`ScopedStylesTests` holds both homes.

**Among the global partials:**

- **A new rule goes in the partial of its concern.**
- **A partial or a `.css` file holds at most 200 lines of rules.** A comment line (`/* … */`, one line or many)
  and a blank line don't count. A rule with a comment beside it does. `FileSizeTests` holds it. A partial that
  grows past 200 lines is split, and its new partial is listed where its lines were.
- **`app.scss` is the list** of partials, one `@forward` a line, in folders by concern: `settings/`, `base/`,
  `layout/`, `chat/`, `landing/`, `admin/`.
- **Its order is the cascade.** The colours and the plain elements come first, and the rest in the order its
  rules always came, so a later rule still wins where it did. A move between partials that would change which
  rule wins is checked in the browser.
- **What is on the list, and what is not.** A name under `web/` is the framework's Sass, one under `evals/` the
  studio's shared rules, and one under `pack-` a pack's. They are found through `IncludePaths` in
  `sasscompiler.json`. `tools/` writes no CSS, so it isn't on the list. A component's own `X.razor.scss` never is:
  the bundle carries it.

*Decided in:* [ADR 0094](../../src/docs/adr/0094-a-components-styles-sit-beside-it.md),
[ADR 0053](../../src/docs/adr/0053-stylesheets-by-concern.md).

## Colours

- **A colour is a variable, never a value, outside the tokens.** The tokens are `settings/_tokens.scss`, and a
  theme in `Styles/themes/`. A rule reads Bootstrap's name, or a `--web-` name where Bootstrap has none.
  `FixedColourTests` holds the shared rules to it.
- **A see-through colour reads the numbers**, as in `rgba(var(--bs-primary-rgb), .4)`. `web/tools/_colours.scss`
  writes both forms from one value, so a colour is written once.
- **What a rule reads at run time stays a CSS variable**, since a Sass variable is fixed when the build runs.
- **A theme stays a file of its own**, loaded by the page.
- **No rainbow** (Louis, 2026-11-10: "no rainbow color", then "glows and gradient works just not rainbow"): one
  accent to an element. A gradient stays within one colour, white into it or the colour into a lighter shade.
  Several items may each keep their own colour. Their colours are never blended into one gradient, or set in a
  row that reads as one element.

## Names and paths

- **A class name says what it is and can't clash.** A state or a kind on a shared element takes a prefix, as
  `is-replay` and `is-recorded` do. A bare `.replay` was already the landing page's button.
- **A path in `url(…)` is from where the CSS ends up**, `wwwroot/css/app.css`, whichever partial holds the rule.

## On paper

- **Every link is a QR code** (Louis, 2026-11-13: "in a pdf hyperlinks is qr codes always"): in anything printed
  or saved as a PDF, a link is drawn by the framework's `PaperLink`, which prints its QR code, its words and its
  address written short. It shows on paper only, so the page keeps its ordinary link beside it and hides that one
  in print.
- **Text on paper is near black**, with dark greys only. He found the print's grey text "too light".

## Moving rules

A pure move, which is a split or a regrouping with no text changed, may copy whole line ranges with
`sed -b -n 'a,bp'`. Then it proves it: the new files joined in their order are `cmp`-identical to the old one, and
the step says so. Any change to the text itself is an edit.

*Decided in:* [ADR 0054](../../src/docs/adr/0054-a-move-may-copy-line-ranges.md).

## What the tests hold

- **Every partial is on the list, and everything on the list is there.** `PulsStylesheetTests` checks it through
  `SassStylesheet`, and each other site's stylesheet tests do the same. `SassStylesheet` is in
  `src/tests/web/louis-agent.web.testing`.
- **A style test finds its rule through `.Read()`**, never by opening one file. On Sass, that is the CSS the build
  made: `app.css` and the components' bundle, with the scope marks off. So the site must have been built.
