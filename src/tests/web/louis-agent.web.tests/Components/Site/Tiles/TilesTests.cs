using LouisAgent.Web.Components.Site.Strip;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Tiles;

/// <summary>
/// A business site's tiles and its closing strip are the framework's (F118, ADR 0106): a row of tiles one to three
/// across, a tile that shows a measured number or a few words, and a strip that asks the visitor to act, each filled
/// with the site's words.
/// </summary>
public class TilesTests
{
    /// <summary>A row says how many tiles stand across, and its tiles rise one after another.</summary>
    [Test]
    public void ARow_HoldsItsTiles_AcrossTheWidthItAsks()
    {
        using var ctx = new BunitContext();
        RenderFragment tiles = builder =>
        {
            builder.OpenComponent<Tile>(0);
            builder.AddComponentParameter(1, nameof(Tile.Title), "Your identity");
            builder.AddComponentParameter(2, nameof(Tile.ChildContent), (RenderFragment)(b => b.AddContent(0, "Who the agent is.")));
            builder.CloseComponent();
            builder.OpenComponent<Tile>(3);
            builder.AddComponentParameter(4, nameof(Tile.Title), "Your packs");
            builder.AddComponentParameter(5, nameof(Tile.Delay), 1);
            builder.CloseComponent();
        };

        var cut = ctx.Render<TileRow>(parameters => parameters.Add(row => row.Columns, 2).AddChildContent(tiles));

        var row = cut.Find("div.tiles");
        var items = cut.FindAll("article.panel.reveal");
        Assert.Multiple(() =>
        {
            Assert.That(row.ClassList, Is.EquivalentTo(new[] { "tiles", "tiles-2" }));
            Assert.That(items, Has.Count.EqualTo(2));
            Assert.That(items[0].QuerySelector("h3")!.TextContent, Is.EqualTo("Your identity"));
            Assert.That(items[0].QuerySelector("p")!.TextContent, Is.EqualTo("Who the agent is."));
            Assert.That(items[0].HasAttribute("data-delay"), Is.False);
            Assert.That(items[1].GetAttribute("data-delay"), Is.EqualTo("1"));
        });
    }

    /// <summary>A tile that shows a measurement draws its number big and its note under it, and nothing it wasn't given.</summary>
    [Test]
    public void ANumbersTile_ShowsTheNumber_AndItsNote()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Tile>(parameters => parameters
            .Add(tile => tile.Value, "$0.66 → $0.13")
            .Add(tile => tile.Note, "a 10-fix benchmark run"));

        var tile = cut.Find("article");
        Assert.Multiple(() =>
        {
            Assert.That(tile.ClassList, Is.EquivalentTo(new[] { "panel", "reveal", "stat" }));
            Assert.That(cut.Find(".stat-value").TextContent, Is.EqualTo("$0.66 → $0.13"));
            Assert.That(cut.Find(".stat-note").TextContent, Is.EqualTo("a 10-fix benchmark run"));
            Assert.That(cut.FindAll("h3, p"), Is.Empty);
        });
    }

    /// <summary>The closing strip is a band of its own, named, with its heading, its line and the action.</summary>
    [Test]
    public void TheStrip_IsABand_WithItsHeading_ItsLine_AndTheAction()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<SiteStrip>(parameters => parameters
            .Add(strip => strip.Heading, "Building an agent for a .NET team?")
            .Add(strip => strip.Action, (RenderFragment)(b => b.AddMarkupContent(0, "<a class=\"btn btn-dark\" href=\"/contact\">Get in touch</a>")))
            .AddChildContent("Tell us what you're building."));

        var band = cut.Find("section.band");
        Assert.Multiple(() =>
        {
            Assert.That(band.Id, Is.EqualTo("contact"));
            Assert.That(band.ClassList, Does.Contain("band-strip"));
            Assert.That(cut.Find(".site-strip h2").TextContent, Is.EqualTo("Building an agent for a .NET team?"));
            Assert.That(cut.Find(".site-strip p").TextContent, Is.EqualTo("Tell us what you're building."));
            Assert.That(cut.Find(".site-strip a.btn").GetAttribute("href"), Is.EqualTo("/contact"));
        });
    }
}
