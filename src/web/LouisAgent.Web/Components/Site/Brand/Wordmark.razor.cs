using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Brand;

/// <summary>
/// A site's name with the mark standing in for one of its letters (F151.6), as louis-agent's o. It draws what stands
/// before the mark, the mark with no letter on it, and what stands after, and tells a screen reader the whole name
/// once.
/// </summary>
/// <remarks>
/// A general part of the framework's web: it names no site, only the letters a site hands it, so any site can draw
/// its own name this way. The mark is the framework's orb, so it wears the theme's mark as the agent's does.
/// </remarks>
public partial class Wordmark
{
    /// <summary>The letters before the mark.</summary>
    [Parameter, EditorRequired]
    public string Before { get; set; } = "";

    /// <summary>The letters after the mark.</summary>
    [Parameter, EditorRequired]
    public string After { get; set; } = "";

    /// <summary>The letter the mark stands for, which is only read out.</summary>
    [Parameter]
    public string Letter { get; set; } = "o";

    /// <summary>The whole name, as a screen reader hears it.</summary>
    private string Name => Before + Letter + After;
}
