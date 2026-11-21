using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Docs;

/// <summary>
/// The docs layout's three columns (F164.1, Louis's pick "A, with B's hero on the Overview"): the site's menu on the
/// left, the page in a readable middle column, and on the right what stands beside the page (F164.2), such as the
/// site's chat docked, with the page's sections as "On this page" while that is closed.
/// </summary>
/// <remarks>
/// The frame's third layout, drawn by the frame itself (Louis: "SiteShell gains a docs mode"), as a part of its own so
/// its rules sit beside it. "On this page" is the frame's section index docked, reading the same list of the page's
/// bands, so a page says its sections once and both places agree. The right column is drawn on every screen, so what
/// stands beside the page keeps its narrow screen's button; an open chat has the column by a rule of its styles, with
/// no state handed between the two.
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

    /// <summary>What stands beside the page, in the right column, such as the site's chat docked.</summary>
    [Parameter]
    public RenderFragment? Beside { get; set; }

    /// <summary>
    /// What the page offers to ask the site's agent about itself, at the top of "On this page" (F164.2, Louis's pick
    /// A), so it hides where that list hides; none, and nothing stands there.
    /// </summary>
    [Parameter]
    public PageAsk? Ask { get; set; }
}
