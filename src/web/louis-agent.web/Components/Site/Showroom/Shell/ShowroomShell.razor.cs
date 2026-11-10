using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Site.Showroom.Shell;

/// <summary>
/// A site's showroom for its demos (F127.1): a slim bar with the brand, the way back to the site and one offer, every
/// demo in a rail grouped by what a visitor comes for (chips on a phone), the demo on screen in the stage, and a
/// corner for the site's guide. The frame wears the colour of the demo on screen, so its light, the pill and all
/// within follow it.
/// </summary>
/// <remarks>
/// Template Method, in Razor (Gang of Four), as <see cref="SiteShell"/> is for the landing: the frame stays here and a
/// site's small layout fills the slots that differ. The demos and the words are data the site registers
/// (<see cref="ShowroomDemos"/>, <see cref="ShowroomText"/>), read from the services, so the framework names no
/// site and a site with no showroom pays nothing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/template-method">Template Method (Refactoring.Guru)</seealso>
public partial class ShowroomShell
{
    /// <summary>The site's name or mark, at the start of the bar.</summary>
    [Parameter]
    public RenderFragment? Brand { get; set; }

    /// <summary>Where the brand takes you; home unless the site says otherwise.</summary>
    [Parameter]
    public string BrandHref { get; set; } = "/";

    /// <summary>Where the way back leads: the rest of the site.</summary>
    [Parameter]
    public string BackHref { get; set; } = "/";

    /// <summary>The site's one offer, at the end of the bar.</summary>
    [Parameter]
    public RenderFragment? Offer { get; set; }

    /// <summary>What floats in the corner, such as the site's guide.</summary>
    [Parameter]
    public RenderFragment? Corner { get; set; }

    /// <summary>The colour of a page in the showroom that is no demo, such as its start.</summary>
    [Parameter]
    public TileAccent StartAccent { get; set; } = TileAccent.Purple;

    /// <summary>The page.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The site's demos; none when it registered none.</summary>
    private ShowroomDemos Demos => Services.GetService<ShowroomDemos>() ?? new ShowroomDemos([]);

    /// <summary>The site's words, else the framework's English.</summary>
    private ShowroomText Text => Services.GetService<ShowroomText>() ?? new ShowroomText();

    /// <summary>The demo on screen; none on the start.</summary>
    private ShowroomDemo? Current => Demos.At(Nav.ToAbsoluteUri(Nav.Uri).AbsolutePath);

    /// <summary>The frame's classes: the showroom, in the colour of the demo on screen or the start's.</summary>
    private string Classes => $"showroom accent-{(Current?.Accent ?? StartAccent).Wire()}";
}
