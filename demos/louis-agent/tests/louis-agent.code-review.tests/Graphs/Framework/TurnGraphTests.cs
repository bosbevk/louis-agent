using LouisAgent.CodeReview.Graphs.Framework;
using LouisAgent.CodeReview.Pages.Site;

namespace LouisAgent.CodeReview.Tests.Graphs.Framework;

/// <summary>
/// The How-it-works diagram's engine column is the real pipeline in its real order (F111), the same list the detailed
/// film is held to; every arrow joins boxes that exist, and the page shows the diagram once.
/// </summary>
public class TurnGraphTests
{
    /// <summary>The middleware a turn runs through, outermost first (core's <c>Agent/Turns/ChatPipeline.cs</c>).</summary>
    private static readonly string[] Pipeline =
    [
        "Tool loop + guards", "Usage recording", "Cut-off marking", "Tracing", "Replay", "Prompt caching"
    ];

    /// <summary>The engine's column is the pipeline, top to bottom.</summary>
    [Test]
    public void TheEnginesColumn_IsThePipeline_OutermostFirst()
    {
        var engine = TurnGraph.Spec.Nodes.Where(node => node.Column == 2).Select(node => node.Label).ToList();

        Assert.That(engine, Is.EqualTo(Pipeline));
    }

    /// <summary>Every arrow joins two boxes the diagram has.</summary>
    [Test]
    public void EveryArrow_JoinsTwoBoxes()
    {
        var ids = TurnGraph.Spec.Nodes.Select(node => node.Id).ToHashSet();

        Assert.That(TurnGraph.Spec.Edges.Where(edge => !ids.Contains(edge.From) || !ids.Contains(edge.To)), Is.Empty);
    }

    /// <summary>The page shows the diagram once.</summary>
    [Test]
    public void ThePage_ShowsIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<HowItWorksPage>();

        Assert.That(cut.FindAll("figure.graph g.node"), Has.Count.EqualTo(TurnGraph.Spec.Nodes.Count));
    }
}
