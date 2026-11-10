using LouisAgent.Web.Components.Site.Showroom.Stage;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Tests.Components.Site.Showroom;

/// <summary>
/// F127.1: a demo page is a stage in its demo's colour: a strip, the kicker, the title as the page's one h1, the lede,
/// then the player with the cues beside it, and the offer under them.
/// </summary>
public class DemoStageTests
{
    /// <summary>A stage in green with every slot filled, cues included unless told otherwise.</summary>
    private static IRenderedComponent<DemoStage> Stage(BunitContext ctx, bool cues = true) =>
        ctx.Render<DemoStage>(parameters =>
        {
            parameters
                .Add(stage => stage.Accent, TileAccent.Green)
                .Add(stage => stage.Kicker, "A bakery")
                .Add(stage => stage.Title, "Bread at dawn.")
                .Add(stage => stage.Lede, "A baker's agent answers before the oven is warm.")
                .Add(stage => stage.Player, "<div class=\"player\">The chat</div>")
                .Add(stage => stage.Offer, "<a href=\"/contact\">Ask for yours</a>");
            if (cues) parameters.Add(stage => stage.Cues, "<ol class=\"cues\"><li>One</li></ol>");
        });

    /// <summary>The stage wears its demo's colour, and its title is the page's one h1, under the kicker.</summary>
    [Test]
    public void TheStage_WearsItsColour_AndTheTitleIsTheH1()
    {
        using var ctx = new BunitContext();

        var cut = Stage(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("section.demo-stage").ClassList, Does.Contain("accent-green"));
            Assert.That(cut.Find(".stage-head .stage-kicker").TextContent, Is.EqualTo("A bakery"));
            Assert.That(cut.FindAll("h1").Select(h => h.TextContent), Is.EqualTo(new[] { "Bread at dawn." }));
            Assert.That(cut.Find(".stage-head .stage-lede").TextContent, Does.StartWith("A baker's agent"));
        });
    }

    /// <summary>The player comes first and the cues beside it; the offer comes after both.</summary>
    [Test]
    public void ThePlayer_HasTheCuesBesideIt_AndTheOfferUnder()
    {
        using var ctx = new BunitContext();

        var cut = Stage(ctx);

        var body = cut.Find(".stage-body");
        Assert.Multiple(() =>
        {
            Assert.That(body.Children.Select(c => c.ClassName), Is.EqualTo(new[] { "stage-player", "stage-cues" }));
            Assert.That(body.QuerySelector(".stage-player .player")!.TextContent, Is.EqualTo("The chat"));
            Assert.That(cut.Find(".stage-body + .stage-offer a").GetAttribute("href"), Is.EqualTo("/contact"));
        });
    }

    /// <summary>A demo without cues (its player shows its own steps) gives the player the whole width.</summary>
    [Test]
    public void WithoutCues_ThePlayerHasTheWholeWidth()
    {
        using var ctx = new BunitContext();

        var body = Stage(ctx, cues: false).Find(".stage-body");

        Assert.That((body.ClassList.Contains("is-alone"), body.QuerySelectorAll(".stage-cues").Length),
            Is.EqualTo((true, 0)));
    }
}
