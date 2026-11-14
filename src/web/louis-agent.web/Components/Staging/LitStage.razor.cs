using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Staging;

/// <summary>
/// A lit stage (F134.1): a light from above, a pool of it on the floor and the floor's lines, behind whatever stands
/// on it. It knows nothing of what that is, so any page can stand something in the light.
/// </summary>
/// <remarks>
/// A wrapper that draws only a backdrop (Louis: "things that can be shared, must go to framework", "like cool
/// components we could reuse later in other projects"). The showroom's start is the first to use it. Its light is
/// the accent's, the colour a tile or a demo already wears, so there is no second way to say a colour. With no
/// accent of its own it is lit in the colour of the place it stands in, which is how a page that already wears one
/// lights its stage without saying it twice.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public partial class LitStage
{
    /// <summary>The light's colour. With none, the stage is lit in the colour of the place it stands in.</summary>
    [Parameter]
    public TileAccent Accent { get; set; }

    /// <summary>What stands on the stage.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The stage's classes: itself, and its colour when it has one of its own.</summary>
    private string Classes => Accent == TileAccent.None ? "lit-stage" : $"lit-stage accent-{Accent.Wire()}";
}
