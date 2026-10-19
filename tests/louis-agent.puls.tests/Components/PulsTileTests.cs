using LouisAgent.Puls.Components.Brand;

namespace LouisAgent.Puls.Tests.Components;

// T1: the app icon inline (F24), pinned to PulsTile.html. Its SVG ids are per instance (a page may hold two), so the
// snapshot normalises them; the rings are the logo's (PulsMark.Rings).
public class PulsTileTests
{
    [Test]
    public void TheTile_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<PulsTile>();

        Assert.That(cut.FindAll("circle.puls-glow"), Has.Count.EqualTo(PulsMark.Rings.Length));
        Assert.That(cut.Find("svg").GetAttribute("class"), Is.EqualTo("puls-tile"));
        Snapshots.Match("Components/PulsTile.html", cut.Markup);
    }

    [Test]
    public void TwoTiles_KeepTheirIdsApart()
    {
        using var ctx = new BunitContext();

        string first = ctx.Render<PulsTile>().Find("mask").Id!;
        string second = ctx.Render<PulsTile>().Find("mask").Id!;

        Assert.That(first, Does.StartWith("tile-"));
        Assert.That(second, Is.Not.EqualTo(first));
    }
}
