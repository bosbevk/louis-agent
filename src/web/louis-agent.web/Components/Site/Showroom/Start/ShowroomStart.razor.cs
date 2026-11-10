using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Site.Showroom.Start;

/// <summary>
/// The showroom's start (F127.1, step 9): a big start for the site's main demo, in its colour, with a way in and a look
/// at it, then a glowing cover for every demo, grouped by what a visitor comes for.
/// </summary>
/// <remarks>
/// The research's demo centre: "a big start screen for the main one", then a cover per demo, "not just some product
/// screenshot" (DemoDash). The covers are drawn from the site's <see cref="ShowroomDemos"/>, the list the rail draws,
/// so a new demo is one line in the site and shows here too; the site says only which demo leads and its words.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class ShowroomStart
{
    /// <summary>The main demo's address: the one the start features.</summary>
    [Parameter, EditorRequired]
    public string Featured { get; set; } = "";

    /// <summary>The few words over the title: "Start here".</summary>
    [Parameter]
    public string Kicker { get; set; } = "";

    /// <summary>The start's title, the page's one heading.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Title { get; set; } = null!;

    /// <summary>A line under the title.</summary>
    [Parameter]
    public RenderFragment? Lede { get; set; }

    /// <summary>What the way in to the main demo says: "Start with the guesthouse".</summary>
    [Parameter, EditorRequired]
    public string Play { get; set; } = "";

    /// <summary>A look at the main demo: its website's scene.</summary>
    [Parameter]
    public RenderFragment? Preview { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The site's demos; none when it registered none.</summary>
    private ShowroomDemos Demos => Services.GetService<ShowroomDemos>() ?? new ShowroomDemos([]);

    /// <summary>The site's words, else the framework's English.</summary>
    private ShowroomText Text => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>The feature's classes: in the main demo's colour, the showroom's purple if the site has none.</summary>
    private string FeatureClasses =>
        $"start-feature accent-{(Demos.At(Featured)?.Accent ?? TileAccent.Purple).Wire()}";

    /// <summary>A cover's classes: in its demo's colour.</summary>
    private static string CoverClasses(ShowroomDemo demo) => $"start-cover accent-{demo.Accent.Wire()}";
}
