using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Stage;

/// <summary>
/// A demo page's stage in the showroom (F127.1): a strip in the demo's colour, what the demo is, its title as the
/// page's one h1, a line of what happens, then the player (a mock of the business's website, or an agent's work
/// window) with the cues beside it, and the site's offer under them.
/// </summary>
/// <remarks>
/// A page fills slots in a fixed order, so every demo reads the same way and a new one only writes its words. The
/// stage wears its colour as an accent class, so the strip, the title's fade, the cues and the offer all draw with
/// <c>--accent</c> and change together.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class DemoStage
{
    /// <summary>The demo's colour, the same it wears in the rail.</summary>
    [Parameter]
    public TileAccent Accent { get; set; }

    /// <summary>What the demo is, over the title: "A restaurant · Wheatfield Kitchen".</summary>
    [Parameter]
    public string Kicker { get; set; } = "";

    /// <summary>The demo's title, the page's main heading.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Title { get; set; } = null!;

    /// <summary>A line of what happens in it; none for a demo that needs none.</summary>
    [Parameter]
    public RenderFragment? Lede { get; set; }

    /// <summary>What plays: a business's mock website, or an agent's work window.</summary>
    [Parameter, EditorRequired]
    public RenderFragment Player { get; set; } = null!;

    /// <summary>What to watch for, beside the player; none when the player shows its own steps.</summary>
    [Parameter]
    public RenderFragment? Cues { get; set; }

    /// <summary>The site's offer, under the demo.</summary>
    [Parameter]
    public RenderFragment? Offer { get; set; }

    /// <summary>The stage's classes: a stage, in its demo's colour.</summary>
    private string Classes => $"demo-stage accent-{Accent.Wire()}";

    /// <summary>The body's classes: with no cues the player takes the whole width.</summary>
    private string BodyClasses => Cues is null ? "stage-body is-alone" : "stage-body";
}
