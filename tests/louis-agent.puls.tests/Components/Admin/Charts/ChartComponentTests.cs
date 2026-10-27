using LouisAgent.Puls.Components.Admin.Charts;

namespace LouisAgent.Puls.Tests.Components.Admin.Charts;

// F18 step 6a.3b: the explorer's ring, token stack and token legend as components with the markup and classes RunExplorer's helpers drew (so the page looks the same). Written first:
// they were RenderFragment helpers inside RunExplorer.
public class ChartComponentTests
{
    [Test]
    public void ARing_DrawsAnArcPerNonZeroPart_InItsColour_WithTheTitleAndTheCentre()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Ring>(p => p.Add(x => x.Parts, [1.0, 0.0, 3.0]).Add(x => x.Colours, ["#111", "#222", "#333"]).Add(x => x.Titles, ["a 1", "b 0", "c 3"])
            .Add(x => x.Centre, "R4").Add(x => x.Unit, "requests"));

        var arcs = cut.FindAll("circle.donut-arc");
        Assert.That(arcs, Has.Count.EqualTo(2));
        Assert.That(arcs[0].GetAttribute("style"), Does.Contain("stroke: #111").And.Contain("--len: 25").And.Contain("stroke-dashoffset: 25"));
        Assert.That(arcs[1].GetAttribute("style"), Does.Contain("--i: 2").And.Contain("stroke: #333").And.Contain("--len: 75").And.Contain("stroke-dashoffset: 0"));
        Assert.That(arcs[1].QuerySelector("title")!.TextContent, Is.EqualTo("c 3"));
        Assert.That(cut.Find("svg.donut").GetAttribute("aria-label"), Is.EqualTo("a 1, b 0, c 3"));
        Assert.That(cut.Find(".donut-centre strong").TextContent, Is.EqualTo("R4"));
        Assert.That(cut.Find(".donut-centre strong").GetAttribute("data-count"), Is.EqualTo("R4"));
        Assert.That(cut.Find(".donut-centre small").TextContent, Is.EqualTo("requests"));
    }

    [Test]
    public void ARingOfNothing_IsAnEmptyTrack_WithItsCentreStill()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Ring>(p => p.Add(x => x.Parts, [0.0]).Add(x => x.Colours, ["#111"]).Add(x => x.Titles, ["no list"]).Add(x => x.Centre, "—").Add(x => x.Unit, "no list"));

        Assert.That(cut.FindAll("circle.donut-arc"), Is.Empty);
        Assert.That(cut.FindAll("circle.donut-track"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".donut-centre strong").TextContent, Is.EqualTo("—"));
    }

    [Test]
    public void TheTokenStack_ShowsASegmentPerKindThatHasTokens_AgainstTheTop_WithTheCountsOnHover()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<TokenStack>(p => p.Add(x => x.Parts, [50L, 0L, 25L, 25L]).Add(x => x.Top, 200L));

        var segments = cut.FindAll(".xseg");
        Assert.That(segments, Has.Count.EqualTo(3), "the kind with no tokens is not drawn");
        Assert.That(segments[0].GetAttribute("style"), Does.Contain("width: 25.0%").And.Contain("background: #3987e5"));
        Assert.That(segments[1].GetAttribute("style"), Does.Contain("--i: 2").And.Contain("width: 12.5%"));
        Assert.That(cut.Find(".xstack").GetAttribute("title"), Is.EqualTo("input 50 · cache written 0 · cache read 25 · output 25"));
    }

    [Test]
    public void TheTokenStack_AgainstATopOfZero_DoesNotDivideByIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<TokenStack>(p => p.Add(x => x.Parts, [0L, 0L, 0L, 0L]).Add(x => x.Top, 0L));

        Assert.That(cut.FindAll(".xseg"), Is.Empty);
    }

    [Test]
    public void TheTokenLegend_NamesTheFourKinds_EachWithItsSwatch()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<TokenLegend>();

        Assert.That(cut.FindAll(".legend > span").Select(s => s.TextContent), Is.EqualTo(new[] { "input", "cache written", "cache read", "output" }));
        Assert.That(cut.FindAll(".legend .sw").Select(s => s.GetAttribute("style")), Is.EqualTo(new[] { "background: #3987e5", "background: #d95926", "background: #199e70", "background: #c98500" }));
    }
}
