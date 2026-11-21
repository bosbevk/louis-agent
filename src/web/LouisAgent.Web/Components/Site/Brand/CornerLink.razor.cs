using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Brand;

/// <summary>
/// A small link that stands in the bottom-right corner of the screen, such as the owner's LinkedIn (FX173, Louis: "put
/// the linked in in the bottom right corner"). A site gives the address, the words and the mark.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class CornerLink
{
    /// <summary>Where it goes.</summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";

    /// <summary>Its words, such as "LinkedIn".</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    /// <summary>A mark before its words, such as LinkedIn's.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }
}
