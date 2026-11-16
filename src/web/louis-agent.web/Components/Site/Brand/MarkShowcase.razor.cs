using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Brand;

/// <summary>
/// Shows a site's mark large, the agent's orb with its letter, and, when the site asks, its name with the mark as its
/// letter on four grounds (FX169, Louis: "a slide dedicated to branding"). A site gives its name and letters.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class MarkShowcase
{
    /// <summary>The grounds the name is shown on, in order: black, white, the brand's violet and glass.</summary>
    private static readonly string[] Grounds = ["on-black", "on-white", "on-violet", "on-glass"];

    /// <summary>The site's name, as a screen reader hears the orb.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>The letter on the orb, such as the first of the name.</summary>
    [Parameter, EditorRequired]
    public string Letter { get; set; } = "";

    /// <summary>The name's letters before the mark, for the lockups.</summary>
    [Parameter]
    public string Before { get; set; } = "";

    /// <summary>The name's letters after the mark, for the lockups.</summary>
    [Parameter]
    public string After { get; set; } = "";

    /// <summary>Whether the name is shown on the four grounds under the orb.</summary>
    [Parameter]
    public bool Lockups { get; set; }
}
