using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Menu;

/// <summary>
/// A group of links in a site's menu (FX73, Louis: "if the menu gets too long we should group some items"): a label
/// in the bar, the links under it when the pointer or the keyboard reaches it.
/// </summary>
/// <remarks>
/// CSS does the opening (hover and focus-within), so the group works on every page with no script and the frame's
/// module has nothing to attach. The same on every site, so it is the framework's (ADR 0084); the words are the
/// site's, passed in.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class SiteMenu
{
    /// <summary>The word in the menu bar.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>The links the group holds.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
