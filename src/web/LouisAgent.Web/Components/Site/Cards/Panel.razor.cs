using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Cards;

/// <summary>
/// A plain panel on the theme's pane: a few small label words over its heading, then what a site puts in it, such as
/// a few lines or a list of tags. It stands still, with no tilt and no rise, so a page of facts reads calmly.
/// </summary>
/// <remarks>
/// Template Method, in Razor: the panel's layout is fixed here and a site fills its slots, plain or edited in place.
/// A <see cref="Tiles.Tile"/> is a selling card that moves and takes only words; a panel holds anything.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Panel
{
    /// <summary>The small label words over the heading.</summary>
    [Parameter] public RenderFragment? Topic { get; set; }

    /// <summary>The panel's heading.</summary>
    [Parameter] public RenderFragment? Title { get; set; }

    /// <summary>What the panel holds.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
