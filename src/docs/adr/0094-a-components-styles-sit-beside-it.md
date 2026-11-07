# 0094. A component's styles sit beside it

**Status:** Accepted · **Date:** 2026-11-07 (Louis: "can we do something similair to the styles? what does blazor do
it?"; of three options he picked **B**, Blazor's CSS isolation for a component's own rules, the globals kept as
partials a site forwards) · **Recorded:** 2026-11-07

## Context

Every site listed the framework's styles by hand: `app.scss` was a list of sixteen `@forward` lines, one per partial,
the account library's and the studio's among them, and a site that forgot one had pages without their look. The
partials were by concern (`chat/_bars.scss`, `evals/_run-page.scss`), not by component, so a rule and the markup it
styled sat in different places, and a change to one meant finding the other. CL31 had just done the same for the
scripts: a module beside its component, imported by the component, nothing for a site to list
([ADR 0093](0093-a-script-is-a-module-beside-its-component.md)). Blazor offers the same for styles: a file
`X.razor.css` beside `X.razor` is scoped (every selector gets the component's attribute, `.card[b-k3x9z2m1qa]`),
bundled by the build into one `{site}.styles.css`, and shipped with the library that holds the component.

The build was tried first (step 1): the Sass compiler we use compiles `X.razor.scss` to the `X.razor.css` Blazor
takes, in a library too, and the bundle reached the browser. Then the rules were moved, library by library, and the
moves taught what scoping does to the cascade, which this record keeps.

## Decision

1. **A rule a single component draws sits beside it,** in `X.razor.scss` next to `X.razor`, compiled scoped and
   bundled by the build; the generated `X.razor.css` is not in git. A site links the bundle once, after `app.css`,
   and lists nothing of a library's components.
2. **A rule that several components draw sits in the file of their nearest common ancestor under `::deep`,** when
   that ancestor is a component of the same library (the account shell holds the forms' rules; the admin frame a
   pack's split view). `::deep` goes after the compound the ancestor draws, since the scope lands there, or at the
   very start when the ancestor draws none of the selector's elements.
3. **A rule stays global, in a partial a site forwards,** when any of these holds, because a scoped rule would not
   reach, or would outweigh, what the rule serves today:
   - a razor file outside the library draws one of its classes (a site's own page copies the chat's markup; a pack's
     view draws the screen table's classes);
   - a site's own Sass or a theme overrides it with a selector of no higher weight: the scope attribute adds one
     class of weight, so a scoped base rule would beat the override it used to lose to;
   - it is a variant that names its subject through an ancestor (`.trend.compact .chart-head`) whose base rule has
     moved, or a keyframe (a keyframe in a scoped file is renamed with the scope);
   - a part many components draw and override (`.card-p`, `.admin-table`, `.page-head`, `.muted`): these are the page
     grid's, in `web/layout/_shell.scss` and `web/base/_elements.scss`.
4. **A move is checked three ways before it is shown:** the libraries' goldens change by the scope attribute alone
   (the snapshot helper normalises it to `b-SCOPE`), no scoped rule and global rule style the same element with the
   same weight (a scan over the compiled CSS, run once while the rules moved), and the page in the browser.
5. **The rules hold it:** `ScopedStylesTests` (a style file sits beside its component, holds a rule, lands on an
   element its component draws (FX63), a site's list names no account or admin partial), `FrameworkStylesTests` (the libraries' rules, partials and `.razor.scss`
   alike, hold no fixed colour), and `SassStylesheet.Read()` gives a style test `app.css` and the bundle together,
   scope marks off, so a test finds a rule by the selector its file wrote.

## Consequences

- The account library and the admin have no partial left; a site's list lost `accounts/shell`, `web/admin/shell`
  and `web/admin/screens`. The chat's and the studio's partials stay, smaller, holding what the sites' own pages
  draw and what a theme overrides: the sites copy the chat's markup for their landing demos, so most of the chat is
  a design system the sites compose, not a component's own look.
- A site's theme still overrides the globals with a bare class, as before; it cannot override a component's own rule
  that way, and shouldn't need to: what a theme changes is a global.
- Two generated things are not in git: `X.razor.css` beside each `X.razor.scss`, and the bundles under `obj/`. The
  size rule skips the first; a style test reads the second from `obj/`, so the site must have been built.
- Deleting an `X.razor.scss` leaves its `X.razor.css` behind, and Blazor keeps scoping the component by it until the
  stale file goes; the rule test catches a style file with no component, not a stale generated one.
- A rule's scope lands on the elements its own component renders, so the file that holds it is the component that
  draws the element, not the one that names the class: the message box's rules moved with CL58 into `MessageBox`,
  which only passes `input-box` to `Composer` and draws nothing, and Puls's box lost its look on mypuls.co.za until
  FX63 moved them into `Composer`; four of the studio's rules had the same mistake.
- Found and left for a TODO: the studio's partials hold rules nothing names any more (`.run-report`, `.tree-node`,
  `.live-chat`, `.eval-card` and more), dead since the orchestrator demo went.

## Links

- [ADR 0085](0085-styles-are-written-in-sass.md) (styles are Sass, always reuse), [ADR 0053](0053-stylesheets-by-concern.md)
  (stylesheets by concern), [ADR 0093](0093-a-script-is-a-module-beside-its-component.md) (the scripts' twin)
- [CSS isolation in Blazor](https://learn.microsoft.com/aspnet/core/blazor/components/css-isolation), the Sass
  compiler's `GenerateScopedCss` (AspNetCore.SassCompiler 1.107.1)
- The how: `.claude/rules/css.md`; the libraries: [louis-agent.web](../projects/louis-agent.web.md),
  [louis-agent.web.accounts](../projects/louis-agent.web.accounts.md), [louis-agent.web.evals](../projects/louis-agent.web.evals.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md) · [ADR 0093](0093-a-script-is-a-module-beside-its-component.md)
