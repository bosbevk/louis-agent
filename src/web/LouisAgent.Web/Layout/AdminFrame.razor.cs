using LouisAgent.Web.Components.Frames;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Layout;

/// <summary>The layout a framework's admin page names, which hands over to the one the site set.</summary>
/// <remarks>
/// Bridge (Gang of Four). A page's layout is fixed when it's compiled, and a page in the framework can't name a layout
/// in a site, while a page naming none gets the site's default, which can be its chat's. So the page names this, and
/// which layout it really draws in stays a setting each site can change on its own. Without it every site would repeat
/// that choice in its router, or write a thin page for each of ours.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/bridge">Bridge (Refactoring.Guru)</seealso>
public partial class AdminFrame
{
    /// <summary>The site's layouts, set once by the site.</summary>
    [Inject]
    private SiteFrames Frames { get; set; } = null!;
}
