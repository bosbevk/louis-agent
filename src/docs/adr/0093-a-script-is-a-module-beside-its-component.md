# 0093. A script is a module beside its component

**Status:** Accepted · **Date:** 2026-11-07 (Louis: "instead of JS is there like something else we can use to make it
easier, what javascript like jQuery is mainstream today?"; of three options he picked **A**, plain JavaScript as ES
modules beside each component, no library; TypeScript was picked and reverted the same day) · **Recorded:** 2026-11-07

## Context

Blazor draws the pages; JavaScript is only glue to what the browser alone can do: focus, the clipboard, scrolling,
`localStorage`, WebAuthn, the listeners a page hangs on the document. That glue was two files every site had to list
in its `index.html` (`chat.js`, 266 lines of globals on `window`, and `passkeys.js`), and one global a single site
defined for everyone: three framework components called `louisDownload`, which only Puls's `review.js` held, so the
starter's and the code-review app's admin downloads did nothing, and the rule meant to catch it only looked for two
other globals. jQuery and its successors answer a problem we don't have; the browsers' own APIs are the library.

## Decision

**A script is an ES module beside the component that needs it, imported by that component the first time it draws
and let go when the component is disposed; a site lists nothing of ours but `first-paint.js`.**

- A component that needs the browser has an `X.razor.js` next to it, with `export function`s and no global; a Razor
  class library serves it under its content path. What several components share (focus for dialogs, the download,
  what the browser remembers) is a module under the library's `wwwroot/js/`. The paths are named once, in the
  library's `Scripts` class (`FrameworkScripts`, `AccountScripts`).
- One small proxy, `JsModule`, carries the import: a component names a module's path, calls its functions, and the
  proxy imports the module on the first call, once, however many calls race for it, and disposes it. Nobody writes
  the `import` dance twice. A Virtual Proxy (Gang of Four) over Blazor's JavaScript isolation.
- The page-wide behaviour belongs to the chat shell: its module's `attach()` hangs the document listeners when the
  shell first draws and `detach()` takes them down when it goes; a page without the shell gets none of it.
- Buttons call Blazor (`@onclick` to a method), never a global inline; the method calls the module.
- `first-paint.js` stays a script a site lists, since it must run before anything draws; the site's own scripts
  (Puls's landing page) follow the same shape, as modules the page imports.
- The rule follows (`FrameworkModulesTests`): every module a class names is on disk, every function it invokes is
  exported by a module it names, no framework script but `first-paint.js` writes to `window`, and a site's page
  lists only `first-paint.js` of ours.

Weighed and not taken: a library (jQuery, Alpine, htmx: nothing for them to do under Blazor); TypeScript compiled in
the build (chosen, then reverted; `Microsoft.TypeScript.MSBuild` 7.0 bundles the native compiler, so it would need
no Node.js and is one package away if the scripts grow); JavaScript with JSDoc types (the editor's checking without
a build step; not taken either, the scripts being small).

## Consequences

- A site's `index.html` lists `first-paint.js`, the third-party scripts (highlight.js, Bootstrap, Blazor's loader)
  and its own; a new site spawned from the starter needs no line for the framework's scripts.
- A component's test plans its module with bUnit's `SetupModule` and sees the calls; a page test lists the imports
  and the module calls it made.
- Adding browser behaviour is a module beside the component and a `JsModule` in it, never a line in a shared file
  or on `window`; the rule refuses the rest.

## Links

[louis-agent.web](../projects/louis-agent.web.md) · [louis-agent.web.accounts](../projects/louis-agent.web.accounts.md) ·
[Change the framework's web](../guides/change-the-web.md) · [ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md) ·
[JavaScript isolation in Blazor (Microsoft)](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/javascript-isolation)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
