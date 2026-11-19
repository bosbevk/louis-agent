using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Tiles;

/// <summary>
/// Whether what a tile describes is in place (F165): done, still open, or a tile that says neither. The site gives the
/// words a tile shows for it; the status picks only the badge's colour, the theme's green for done and its amber for
/// open. A plain enum, as every choice is (ADR 0024).
/// </summary>
public enum TileStatus
{
    /// <summary>The tile says nothing about whether it is in place, and draws no badge.</summary>
    [WireName("none")] None,

    /// <summary>What the tile describes is in place today.</summary>
    [WireName("done")] Done,

    /// <summary>What the tile describes isn't built yet.</summary>
    [WireName("open")] Open,
}
