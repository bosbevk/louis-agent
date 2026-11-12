using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// A box with no column stands one past the furthest box that points at it (CL60.2), so a chart read from the docs
/// needs no column written; a column given is kept.
/// </summary>
public class RanksTests
{
    /// <summary>A diagram of boxes with no columns, and its arrows.</summary>
    private static GraphSpec Spec(string[] boxes, params (string From, string To)[] arrows) =>
        new([.. boxes.Select(id => new GraphNode(id, id))],
            [.. arrows.Select(arrow => new GraphEdge(arrow.From, arrow.To))]);

    /// <summary>A chain stands in the columns of its order.</summary>
    [Test]
    public void AChain_StandsInOrder() =>
        Assert.That(Ranks.Of(Spec(["a", "b", "c"], ("a", "b"), ("b", "c"))),
            Is.EquivalentTo(new Dictionary<string, int>
            {
                ["a"] = 0,
                ["b"] = 1,
                ["c"] = 2
            }));

    /// <summary>A box two paths reach stands past the longer.</summary>
    [Test]
    public void ABoxTwoPathsReach_StandsPastTheLonger() =>
        Assert.That(Ranks.Of(Spec(["a", "b", "c"], ("a", "c"), ("a", "b"), ("b", "c")))["c"], Is.EqualTo(2));

    /// <summary>A cycle is broken where it closes, so it ends, and its first box stays first.</summary>
    [Test]
    public void ACycle_IsBrokenWhereItCloses() =>
        Assert.That(Ranks.Of(Spec(["a", "b", "c"], ("a", "b"), ("b", "c"), ("c", "a"))),
            Is.EquivalentTo(new Dictionary<string, int>
            {
                ["a"] = 0,
                ["b"] = 1,
                ["c"] = 2
            }));

    /// <summary>A box with no arrow in stands first, wherever it is listed.</summary>
    [Test]
    public void ABoxNothingPointsAt_StandsFirst() =>
        Assert.That(Ranks.Of(Spec(["b", "a"], ("a", "b"))),
            Is.EquivalentTo(new Dictionary<string, int>
            {
                ["a"] = 0,
                ["b"] = 1
            }));

    /// <summary>A column given is kept, and a box after it stands one past it.</summary>
    [Test]
    public void AGivenColumn_IsKept_AndTheNextStandsPastIt()
    {
        var spec = new GraphSpec([new GraphNode("a", "a", Column: 3), new GraphNode("b", "b")],
            [new GraphEdge("a", "b")]);

        Assert.That(Ranks.Of(spec), Is.EquivalentTo(new Dictionary<string, int>
        {
            ["a"] = 3,
            ["b"] = 4
        }));
    }

    /// <summary>A diagram with no column written is laid out by its arrows, left to right.</summary>
    [Test]
    public void ALayout_WithNoColumns_RunsByItsArrows()
    {
        var placed = GraphLayout.Place(Spec(["a", "b", "c"], ("a", "b"), ("b", "c")));

        var x = placed.Nodes.ToDictionary(node => node.Node.Id, node => node.X);
        Assert.That(x["a"], Is.LessThan(x["b"]).And.LessThan(x["c"]));
        Assert.That(x["b"], Is.LessThan(x["c"]));
    }
}
