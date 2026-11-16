using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Brand;

/// <summary>
/// A brand's line with a link under it as its subtext, such as a site's tagline over the owner's LinkedIn (FX168). A
/// site gives the words, the address and the mark.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class BrandNote
{
    /// <summary>The line, such as the site's tagline.</summary>
    [Parameter, EditorRequired]
    public string Line { get; set; } = "";

    /// <summary>Where the link under it goes.</summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";

    /// <summary>The link's words, such as the address written short.</summary>
    [Parameter, EditorRequired]
    public string LinkText { get; set; } = "";

    /// <summary>A mark before the link's words, such as LinkedIn's.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }
}
