using LouisAgent.Site.Components.Movies;

namespace LouisAgent.Site.Tests.Movies;

/// <summary>
/// The detailed film: ten scenes, the six slabs inside core in the chat pipeline's real order (outermost first, as
/// <c>ChatPipeline.cs</c> builds it), the parts numbered in the order the turn reaches them, and the markup pinned.
/// </summary>
public class DetailedMovieTests
{
    /// <summary>The middleware a turn runs through, outermost first (louis-agent.core/Agent/Turns/ChatPipeline.cs).</summary>
    private static readonly string[] Pipeline =
    [
        "Tool loop + guards", "Usage recording", "Cut-off marking", "Tracing", "Replay", "Prompt caching"
    ];

    /// <summary>Ten captions and ten cards, numbered 1 to 10 in order.</summary>
    [Test]
    public void TheFilm_HasTenScenes_EachWithItsCaptionAndCard()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DetailedMovie>();

        Assert.That(cut.FindAll(".cap b").Select(b => b.TextContent), Is.EqualTo(Enumerable.Range(1, 10).Select(n => $"{n} / 10")));
        Assert.That(cut.FindAll(".card b").Select(b => b.TextContent.Split(' ')[0]), Is.EqualTo(Enumerable.Range(1, 10).Select(n => $"{n}")));
    }

    /// <summary>Core's slabs are the pipeline's layers in the pipeline's order, top to bottom.</summary>
    [Test]
    public void CoresSlabs_AreThePipeline_OutermostFirst()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DetailedMovie>();

        var slabs = cut.FindAll(".part.slab").Select(s => s.ChildNodes.First(n => n.NodeType == AngleSharp.Dom.NodeType.Text).TextContent);
        Assert.That(slabs, Is.EqualTo(Pipeline));
    }

    /// <summary>The parts the turn reaches are numbered in that order, the specialist last at 7.</summary>
    [Test]
    public void TheParts_AreNumbered_InTheOrderTheTurnReachesThem()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DetailedMovie>();

        var numbered = cut.FindAll(".part").Where(p => p.QuerySelector("i") is not null)
            .Select(p => (p.QuerySelector("i")!.TextContent, p.ClassName));
        Assert.That(numbered, Is.EqualTo(new[]
        {
            ("1", "part p-host"), ("7", "part p-spec"), ("2", "part p-comp"), ("3", "part slab s1"),
            ("4", "part p-tools"), ("5", "part p-summ"), ("6", "part p-ledger")
        }));
    }

    [Test]
    public void TheFilm_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DetailedMovie>();

        Snapshots.Match("louis-agent.site.tests", "Movies/DetailedMovie.html", cut.Markup);
    }
}
