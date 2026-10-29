---
paths:
  - "**/*.css"
---

# CSS: the how of every rule

Loaded when a `.css` file is read or edited. The why: [ADR 0053](../../docs/adr/0053-stylesheets-by-concern.md)
(stylesheets by concern) and [ADR 0054](../../docs/adr/0054-a-move-may-copy-line-ranges.md) (a move may copy line
ranges).

- **A `.css` file holds at most 200 lines of rules**; a comment line (`/* … */`, one line or many) and a blank line
  don't count, a rule with a comment beside it does. `FileSizeTests` holds it.
- **`app.css` is the `@import` list**, one area file a line, in folders by concern (Puls: `settings/`, `base/`,
  `layout/`, `chat/`, `landing/`, `admin/`; the code-review app: `base/`, `layout/`, `workspace/`, `git/`, `chat/`).
  **The import order is the cascade**: the tokens and the plain elements first, the rest in the order its rules always
  came, so a later rule still wins where it did.
- **A new rule goes in the file of its concern.** A file that grows past 200 lines is split, its new file imported
  where its lines were; a move between files that would change which rule wins is checked in the browser.
- **Colours, fonts and corners come from the tokens** (Puls's `settings/tokens.css`, a code-review theme in
  `themes/`); a new colour is a token first. A theme stays a file of its own, loaded by the page.
- **A class name says what it is and can't clash:** a state or a kind on a shared element takes a prefix
  (`is-replay`, `is-recorded`), since a bare `.replay` was already the landing page's button.
- **A pure move** (a split, a regrouping with no text changed) may copy whole line ranges with `sed -b -n 'a,bp'`,
  then proves it: the new files joined in their order are `cmp`-identical to the old one, and the step says so. Any
  change to the text itself is an edit.
- **Every area file is imported and every import is there:** each app's `StylesheetTests` (Puls's
  `PulsStylesheetTests`) checks it through `Stylesheet` in `tests/louis-agent.web.testing`; a style test finds its
  rule through `Stylesheet.Of(project).Read()`, never by opening one file.
- **Checked in the browser:** a step that changes a style looks at the page, at desktop and at phone width (390 px,
  no sideways scroll), before it is shown.
