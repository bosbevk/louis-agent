using LouisAgent.Web.Components.Site.Showroom.Stage;

namespace LouisAgent.Web.Tests.Components.Site.Showroom;

/// <summary>
/// F127.1: beside a demo, a few short cues of what to watch, each about a turn of the recording; they light as the
/// play reaches their turn, the newest pulsing, and a bar over them fills (the research: short captions in step with
/// what plays).
/// </summary>
public class DemoCuesTests
{
    private static readonly DemoCue[] Cues =
    [
        new(1, "It answers from the bakery's own hours."),
        new(2, "It asks before it books."),
        new(3, "It never takes a payment."),
    ];

    private static IRenderedComponent<DemoCues> Render(BunitContext ctx, int reached) =>
        ctx.Render<DemoCues>(parameters => parameters
            .Add(cues => cues.Cues, Cues)
            .Add(cues => cues.Reached, reached)
            .Add(cues => cues.Heading, "What to watch"));

    /// <summary>
    /// Every cue up to the turn reached is lit, the newest pulses, and the rest wait, numbered in order.
    /// </summary>
    [Test]
    public void TheCuesUpToTheTurnReached_AreLit_TheNewestPulsing()
    {
        using var ctx = new BunitContext();

        var items = Render(ctx, reached: 2).FindAll("ol.cues-list > li");

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(li => li.QuerySelector(".cue-n")!.TextContent),
                Is.EqualTo(new[] { "1", "2", "3" }));
            Assert.That(items.Select(li => li.ClassList.Contains("is-lit")), Is.EqualTo(new[] { true, true, false }));
            Assert.That(items.Select(li => li.ClassList.Contains("is-now")), Is.EqualTo(new[] { false, true, false }));
        });
    }

    /// <summary>Before the play reaches a turn nothing is lit, and the bar is empty.</summary>
    [Test]
    public void BeforeThePlay_NothingIsLit()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, reached: 0);

        Assert.That((cut.FindAll(".is-lit").Count, cut.Find(".cues-progress i").GetAttribute("style")),
            Is.EqualTo((0, "width: 0%")));
    }

    /// <summary>
    /// The bar fills as far as the turn reached, of the last turn a cue is about; the heading is the site's.
    /// </summary>
    [Test]
    public void TheBar_FillsAsFarAsTheTurnReached()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, reached: 2);

        Assert.That((cut.Find(".cues-progress i").GetAttribute("style"), cut.Find(".cues-heading").TextContent),
            Is.EqualTo(("width: 67%", "What to watch")));
    }
}
