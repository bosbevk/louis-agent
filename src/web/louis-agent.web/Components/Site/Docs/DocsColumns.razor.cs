using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Docs;

/// <summary>
/// The docs layout's three columns (F162.1, Louis's pick "A, with B's hero on the Overview"): the site's menu on the
/// left, the page in a readable middle column, and the page's sections on the right as "On this page".
/// </summary>
/// <remarks>
/// The frame's third layout, drawn by the frame itself (Louis: "SiteShell gains a docs mode"), as a part of its own so
/// its rules sit beside it. "On this page" is the frame's section index docked, reading the same list of the page's
/// bands, so a page says its sections once and both places agree.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class DocsColumns
{
    /// <summary>The site's menu, down the left.</summary>
    [Parameter]
    public RenderFragment? Menu { get; set; }

    /// <summary>The page, in the middle.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
