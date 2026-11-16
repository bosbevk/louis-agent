using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Looks;

/// <summary>
/// Holds a site's settings in a glass panel down the right of the screen: the look switches and whatever else the
/// site puts there, such as a CV. The frame opens and shuts it from its header's three-line button.
/// </summary>
/// <remarks>
/// Louis, 2026-11-16: "we should have a 3 line menu there which will open a sticky menu then we put those buttons
/// there". The panel is its own part so the frame's header keeps only the button, and the look switches have one
/// home on a site that has settings.
/// </remarks>
public partial class SiteSettings
{
    /// <summary>Whether the panel shows; the frame's button opens and shuts it.</summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>Whether the look switches stand at the panel's top, when the site offers looks.</summary>
    [Parameter] public bool Looks { get; set; }

    /// <summary>The site's own settings and lesser links.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
