using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Hero;

/// <summary>
/// The top of a mocked business's home page: its line, a note and a call to action, its photo or its drawn scene, and
/// a band of facts under them. It is what a visitor sees first in the window, so it is drawn large.
/// </summary>
/// <remarks>
/// Split out of <see cref="SiteMock"/>, which keeps the window, the page and where the website is. The hero only
/// draws what it is handed, and takes its look from the class its mock wears, so a new look is styles and no code.
/// </remarks>
public partial class MockHero
{
    /// <summary>The hero's line: "Fresh bread before seven."</summary>
    [Parameter, EditorRequired]
    public string Line { get; set; } = "";

    /// <summary>A few words under the line; none for a hero of one line.</summary>
    [Parameter]
    public string? Note { get; set; }

    /// <summary>Its call to action: "Book a table"; none for a hero without one.</summary>
    [Parameter]
    public string? Action { get; set; }

    /// <summary>A real photo for the hero, from the site's own address; the scene when it has none.</summary>
    [Parameter]
    public string? Photo { get; set; }

    /// <summary>What the photo shows, for a screen reader.</summary>
    [Parameter]
    public string PhotoAlt { get; set; } = "";

    /// <summary>The scene drawn for it in SVG, shown when it has no photo.</summary>
    [Parameter]
    public RenderFragment? Scene { get; set; }

    /// <summary>The business's mark, on the photo's badge; none for a business with no mark.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The business's name, on the photo's badge.</summary>
    [Parameter]
    public string Business { get; set; } = "";

    /// <summary>A few facts in a band under the hero: "Open from six".</summary>
    [Parameter]
    public IReadOnlyList<string> Facts { get; set; } = [];
}
