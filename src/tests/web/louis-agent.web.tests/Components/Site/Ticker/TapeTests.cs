using LouisAgent.Web.Components.Site.Ticker;

namespace LouisAgent.Web.Tests.Components.Site.Ticker;

/// <summary>
/// The design's running tape (Louis: "also want this scrolling feature animation as a framework control"): one line
/// of what the agents did, each item's marked part in the accent, written twice so it loops without a seam.
/// </summary>
public class TapeTests
{
    /// <summary>Two items, one with a marked part and one without.</summary>
    private static readonly IReadOnlyList<TapeItem> Two =
    [
        new TapeItem("ShowPage", "/rooms/garden"),
        new TapeItem("Took a table for five")
    ];

    /// <summary>Each item stands twice, in order, its marked part marked, and a reader skips the tape.</summary>
    [Test]
    public void TheTape_RunsItsItemsTwice()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Tape>(p => p.Add(t => t.Items, Two));
        var items = cut.FindAll(".tape span");

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".tape").GetAttribute("aria-hidden"), Is.EqualTo("true"));
            Assert.That(items.Select(i => i.TextContent.Trim()), Is.EqualTo(new[]
            {
                "ShowPage /rooms/garden", "Took a table for five", "ShowPage /rooms/garden", "Took a table for five"
            }));
            Assert.That(items[0].QuerySelector("i")!.TextContent, Is.EqualTo("/rooms/garden"));
            Assert.That(items[1].QuerySelector("i"), Is.Null);
        });
    }
}
