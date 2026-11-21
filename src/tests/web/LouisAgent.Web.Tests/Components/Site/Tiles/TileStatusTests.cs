using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Tests.Components.Site.Tiles;

/// <summary>
/// A tile can say whether what it describes is in place (F165, Louis's pick "Status on every tile"): the site gives
/// the words, the tile draws them as a badge above its title, done or open, and a tile given none draws none.
/// </summary>
public class TileStatusTests
{
    /// <summary>A done tile and an open tile each draw the site's words, marked with their status.</summary>
    [TestCase(TileStatus.Done, "In place", "done")]
    [TestCase(TileStatus.Open, "Not yet", "open")]
    public void ATile_DrawsItsStatus_AboveItsTitle(TileStatus status, string words, string mark)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Tile>(parameters => parameters
            .Add(tile => tile.Title, "Backups")
            .Add(tile => tile.Status, status)
            .Add(tile => tile.StatusText, words));

        var badge = cut.Find("article .tile-status");
        Assert.Multiple(() =>
        {
            Assert.That(badge.TextContent, Is.EqualTo(words));
            Assert.That(badge.GetAttribute("data-status"), Is.EqualTo(mark));
            Assert.That(badge.NextElementSibling!.TagName, Is.EqualTo("H3"));
        });
    }

    /// <summary>A tile with no status, or with a status and no words, draws no badge.</summary>
    [TestCase(TileStatus.None, "In place")]
    [TestCase(TileStatus.Done, null)]
    public void ATile_WithoutAStatus_DrawsNone(TileStatus status, string? words)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Tile>(parameters => parameters
            .Add(tile => tile.Title, "Backups")
            .Add(tile => tile.Status, status)
            .Add(tile => tile.StatusText, words));

        Assert.That(cut.FindAll(".tile-status"), Is.Empty);
    }
}
