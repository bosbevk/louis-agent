using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin;

/// <summary>The admin's frame: a sidebar and a content column, with slots for what's a site's own.</summary>
/// <remarks>
/// Template Method, in Razor (Gang of Four): the frame stays here, and a site's small layout fills in the steps that
/// differ, as ordinary slots the compiler checks. A settings object would only be checked when the page ran. A site
/// writes about fifteen lines rather than copying the frame, and a slot it leaves empty leaves no gap.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/template-method">Template Method (Refactoring.Guru)</seealso>
public partial class AdminShell
{
    /// <summary>The site's logo, in the top bar and the sidebar.</summary>
    [Parameter] public RenderFragment? Brand { get; set; }

    /// <summary>What the logo says when you hover over it.</summary>
    [Parameter] public string? BrandTitle { get; set; }

    /// <summary>The sidebar's links.</summary>
    [Parameter] public RenderFragment? Links { get; set; }

    /// <summary>What sits above every page, such as a site's live status.</summary>
    [Parameter] public RenderFragment? Top { get; set; }

    /// <summary>Where the way back out of the admin goes; with none, there's no way back shown.</summary>
    [Parameter] public string? BackHref { get; set; }

    /// <summary>What the way back says.</summary>
    [Parameter] public string? BackLabel { get; set; }

    /// <summary>The page.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
