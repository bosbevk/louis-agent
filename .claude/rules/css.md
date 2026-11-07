---
paths:
  - "**/*.css"
  - "**/*.scss"
---

# CSS and Sass: the how of every rule

Loaded when a `.css` or `.scss` file is read or edited. The why: [ADR 0085](../../src/docs/adr/0085-styles-are-written-in-sass.md)
(Sass, and always reuse), [ADR 0053](../../src/docs/adr/0053-stylesheets-by-concern.md) (stylesheets by concern),
[ADR 0054](../../src/docs/adr/0054-a-move-may-copy-line-ranges.md) (a move may copy line ranges) and
[ADR 0094](../../src/docs/adr/0094-a-components-styles-sit-beside-it.md) (a component's styles sit beside it).

- **A rule one component draws sits beside it** (ADR 0094): in `X.razor.scss` next to `X.razor`, which the build
  compiles scoped (`.card[b-k3x9z2m1qa]`) and bundles into `{site}.styles.css`, linked once after `app.css`; the
  generated `X.razor.css` is not in git, and deleting the `.scss` means deleting it too. A rule several components
  draw goes in their nearest common ancestor's file under `::deep` (after the compound the ancestor draws, or first
  when it draws none). A rule stays a global partial when a site's own page draws its class, a site's Sass or a theme
  overrides it with no more weight (the scope adds a class of weight), it is a variant over a base that stays, a
  keyframe, or a part many components draw (`.card-p`, `.page-head`, `.muted`). **The file is the component that
  draws the element** a rule lands on, not the one that asks for it: a class passed down as a parameter is styled
  in the child that draws it, and a component that draws no element of its own holds no rules (FX63).
  `ScopedStylesTests` holds it.

- **Always reuse.** Before writing a rule, look for a variable, mixin or partial that already does it. What repeats
  becomes one shared piece under the framework's `web/tools/` that both places use, and a site `@use`s the framework's Sass and never copies
  it. A step says what it reused and what new piece it added.
- **Styles are Sass, under `Styles/`,** on every site. The build makes `wwwroot/css/app.css`, which isn't in git, so edit the
  partial, never that file. Existing rules take nesting as they're touched, never in a sweep.
- **A partial or `.css` file holds at most 200 lines of rules**; a comment line (`/* … */`, one line or many) and a blank
  line don't count, a rule with a comment beside it does. `FileSizeTests` holds it.
- **`app.scss` is the list**, one `@forward` a line, of partials in folders by concern (`settings/`, `base/`, `layout/`,
  `chat/`, `landing/`, `admin/`). **Its order is
  the cascade**: the colours and the plain elements first, the rest in the order its rules always came, so a later rule
  still wins where it did. A name under `web/` is the framework's Sass, one under `evals/` the studio's shared rules and one under `pack-` a pack's, found through `IncludePaths` in `sasscompiler.json`; `tools/` writes no CSS, so it isn't on the list, and a component's own `X.razor.scss` never is: the bundle carries it.
- **A new rule goes in the partial of its concern.** One that grows past 200 lines is split, its new partial listed where
  its lines were; a move between partials that would change which rule wins is checked in the browser.
- **A colour is a variable, never a value, outside the tokens** (`settings/_tokens.scss`, a theme in
  `Styles/themes/`): a rule reads Bootstrap's name, or a `--web-` name where it has none, and `FixedColourTests` holds the shared
  rules to it. A see-through colour reads the numbers, `rgba(var(--bs-primary-rgb), .4)`; `web/tools/_colours.scss` writes both
  forms from one value, so a colour is written once. What a rule reads at run time stays a CSS variable, since a Sass
  variable is fixed when the build runs. A theme stays a file of its own, loaded by the page.
- **A path in `url(…)` is from where the CSS ends up,** `wwwroot/css/app.css`, whichever partial holds the rule.
- **A class name says what it is and can't clash:** a state or a kind on a shared element takes a prefix
  (`is-replay`, `is-recorded`), since a bare `.replay` was already the landing page's button.
- **A pure move** (a split, a regrouping with no text changed) may copy whole line ranges with `sed -b -n 'a,bp'`,
  then proves it: the new files joined in their order are `cmp`-identical to the old one, and the step says so. Any
  change to the text itself is an edit.
- **Every partial is on the list and everything on the list is there:** `PulsStylesheetTests` checks it through
  `SassStylesheet`, and each other site's stylesheet tests the same way, in
  `src/tests/web/louis-agent.web.testing`. A style test finds its rule through `.Read()`, never by opening one file; on
  Sass, that's the CSS the build made: `app.css` and the components' bundle, scope marks off, so the site must have
  been built.
- **Checked in the browser:** a step that changes a style looks at the page, at desktop and at phone width (390 px,
  no sideways scroll), before it is shown.
