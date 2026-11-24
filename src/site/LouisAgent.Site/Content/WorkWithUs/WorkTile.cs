using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Site.Content.WorkWithUs;

/// <summary>
/// One tile of the Work with us page (F177): its icon, a short title, a sentence or two, and the place it leads to
/// when it has one.
/// </summary>
/// <param name="Icon">The line icon in the tile's chip.</param>
/// <param name="Title">The tile in a few words, as its heading.</param>
/// <param name="Text">What it means for the client, in a sentence or two.</param>
/// <param name="Href">Where the tile's button leads, or none for a tile without one.</param>
/// <param name="LinkText">What the button says, starting with its verb.</param>
public sealed record WorkTile(DiagramIcon Icon, string Title, string Text, string? Href = null,
    string? LinkText = null)
{
    /// <summary>Whether the place is on another site, and so opens in a new tab.</summary>
    public bool LeavesTheSite => Href?.StartsWith("https://", StringComparison.Ordinal) ?? false;
}
