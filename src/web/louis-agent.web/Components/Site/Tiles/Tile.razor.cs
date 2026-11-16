using System.Globalization;
using LouisAgent.Choices;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Pieces;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Tiles;

/// <summary>
/// One tile in a <see cref="TileRow"/> (F118): either a measured number with a note under it, or a title with a few
/// words, the site's in both cases. Tiles rise one after another as they enter, each a little later than the last.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0106-the-landing-is-a-business-website-from-the-framework.md">ADR 0106: the landing is a business website from the framework</seealso>
public partial class Tile
{
    /// <summary>The icon in the tile's glass chip, over its heading; none for a tile without one.</summary>
    [Parameter]
    public DiagramIcon? Icon { get; set; }

    /// <summary>The tile's heading, for a tile of words.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The big number, for a tile that shows a measurement.</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>What the number means, under it.</summary>
    [Parameter]
    public string? Note { get; set; }

    /// <summary>How many steps after the row's first tile this one rises; none for the first.</summary>
    [Parameter]
    public int Delay { get; set; }

    /// <summary>
    /// The tile's words in Markdown, so they may hold a link (ADR 0125); instead of words given as content.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>The tile's words, as a paragraph.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The tile's own buttons, side by side under its words, at their own width; none by default.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>A colour to set the tile apart, the eval screens' four; none by default.</summary>
    [Parameter]
    public TileAccent Accent { get; set; }

    /// <summary>Whether the theme's lens bends what is behind the tile under the pointer; on by default.</summary>
    [Parameter]
    public bool Lens { get; set; } = true;

    /// <summary>Whether the tile shows its reflection under it; off by default.</summary>
    [Parameter]
    public bool Reflect { get; set; }

    /// <summary>The tile's glass: its colour, how much, and what it does behind it; clear water by default.</summary>
    [Parameter]
    public GlassLook Glass { get; set; } = GlassLook.Default;

    /// <summary>Whether the tile leans towards the pointer, as far as the theme says; on by default.</summary>
    [Parameter]
    public bool Tilt { get; set; } = true;

    /// <summary>Whether a visitor or the guide can pick the tile, the theme drawing it picked; on by default.</summary>
    [Parameter]
    public bool Pick { get; set; } = true;

    /// <summary>
    /// A panel that rises; a number's tile also keeps its number and note close together, and an accented tile wears
    /// its colour's class.
    /// </summary>
    private string Classes => (Value is null ? "panel reveal" : "panel reveal stat") + AccentClass;

    /// <summary>
    /// Every tile is a piece a visitor or the guide can select (F127.7), a tile of words by its title and one of a
    /// number by its number. Razor leaves the attribute out when this is false, and out the id with it.
    /// </summary>
    private bool IsPiece => Pick && (Title ?? Value) is not null;

    /// <summary>The piece's id from its title or its number, or none for a tile that can't be picked.</summary>
    private string? PieceAddress => IsPiece ? PieceId.Of((Title ?? Value)!) : null;

    /// <summary>What the tile tells the lens: nothing while it is on, so only a switched-off lens is written.</summary>
    private string? LensSwitch => PaneSwitch.Lens(Lens);

    /// <summary>The class an accent's colour hangs on, named for the colour; none for a plain tile.</summary>
    private string AccentClass => Accent == TileAccent.None ? "" : $" accent-{Accent.Wire()}";

    /// <summary>The delay the site's reveal reads, left off the first tile.</summary>
    private string? DelayValue => Delay > 0 ? Delay.ToString(CultureInfo.InvariantCulture) : null;
}
