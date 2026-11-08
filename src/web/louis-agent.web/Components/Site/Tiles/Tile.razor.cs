using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Tiles;

/// <summary>
/// One tile in a <see cref="TileRow"/> (F118): either a measured number with a note under it, or a title with a few
/// words, the site's in both cases. Tiles rise one after another as they enter, each a little later than the last.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class Tile
{
    /// <summary>The tile's heading, for a tile of words.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The big number, for a tile that shows a measurement.</summary>
    [Parameter] public string? Value { get; set; }

    /// <summary>What the number means, under it.</summary>
    [Parameter] public string? Note { get; set; }

    /// <summary>How many steps after the row's first tile this one rises; none for the first.</summary>
    [Parameter] public int Delay { get; set; }

    /// <summary>The tile's words, as a paragraph.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>A panel that rises; a number's tile also keeps its number and note close together.</summary>
    private string Classes => Value is null ? "panel reveal" : "panel reveal stat";

    /// <summary>The delay the site's reveal reads, left off the first tile.</summary>
    private string? DelayValue => Delay > 0 ? Delay.ToString(CultureInfo.InvariantCulture) : null;
}
