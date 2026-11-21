using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Site.Content.Trust;

/// <summary>
/// One fact on the trust page (F165): whether it is in place, a short title, a few words, and the file of the repo
/// that shows it. A fact that isn't built may have no file to point at, and then says so with none.
/// </summary>
/// <param name="Status">Done when it is in place today, open when it isn't built yet.</param>
/// <param name="Title">The fact in a few words, as the tile's heading.</param>
/// <param name="Text">What it means for the business, in a sentence or two.</param>
/// <param name="Source">The file, from the repo's root, that shows it, or none for a gap nothing in the code holds.</param>
public sealed record TrustFact(TileStatus Status, string Title, string Text, string? Source = null)
{
    /// <summary>What the tile's badge says for a fact that is in place.</summary>
    public const string InPlace = "In place";

    /// <summary>What the tile's badge says for a fact that isn't built yet.</summary>
    public const string NotYet = "Not yet";

    /// <summary>The badge's words for this fact.</summary>
    public string StatusText => Status == TileStatus.Done ? InPlace : NotYet;
}
