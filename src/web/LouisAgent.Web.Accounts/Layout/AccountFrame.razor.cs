using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Layout;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Layout;

/// <summary>The layout a framework's account page names, which hands over to the one the site set.</summary>
/// <remarks>
/// Bridge (Gang of Four), as <see cref="AdminFrame"/>: a page's layout is fixed when it's compiled, and a page in the
/// framework can't name a layout in a site, so the page names this and which layout it really draws in is a setting
/// each site makes once.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class AccountFrame
{
    /// <summary>The site's layouts, set once by the site.</summary>
    [Inject]
    private SiteFrames Frames { get; set; } = null!;
}
