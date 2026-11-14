using LouisAgent.Diagrams.Engine.Flow;
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

    /// <summary>
    /// The boxes and arrows of a real picture, F36's "MCP servers as packs", where Louis found two long lines
    /// that wandered: a start-up path down the left with three decisions, a box in the middle, and two arrows
    /// from the top row that both arrive at the catalogue far below.
    /// </summary>
    private static GraphSpec Packs() => Spec(
        [
            "named", "settings", "approved", "unavailable", "connect", "dispose", "drift", "names", "pin", "guard",
            "catalogue", "sessions", "changed", "refresh"
        ],
        ("named", "settings"), ("settings", "approved"), ("approved", "unavailable"), ("approved", "connect"),
        ("connect", "dispose"), ("dispose", "unavailable"), ("connect", "drift"), ("drift", "dispose"),
        ("drift", "names"), ("names", "pin"), ("pin", "dispose"), ("pin", "guard"), ("guard", "catalogue"),
        ("catalogue", "sessions"), ("changed", "catalogue"), ("refresh", "catalogue"));

    /// <summary>How often the line from one box to another turns.</summary>
    private static int Turns(GraphSpec spec, string from, string to) =>
        GraphLayout.Place(spec, rules: FlowRules.Straight).Edges
            .Single(edge => edge.Edge.From == from && edge.Edge.To == to).Path.Count(part => part == 'Q');

    /// <summary>
    /// A long line whose arriving line is clear runs on it, though another long line stood there when it was
    /// looked at and has since moved to its own (F134.12, Louis: "can we fix this here"). It turns once out of
    /// its box's line and once into the box, and no more.
    /// </summary>
    [Test]
    public void ALongLine_RunsOnTheLineItArrivesOn_ThoughAnotherStoodThereFirst() =>
        Assert.That(Turns(Packs(), "changed", "catalogue"), Is.LessThanOrEqualTo(2));

    /// <summary>
    /// A long line that no single line lets through takes the shortest way through what is clear, row by row
    /// (F134.12, Louis: "why did it take a longer route?"). It does not swing out past its target and back.
    /// </summary>
    [Test]
    public void ALongLineNoOneLineLetsThrough_TakesTheShortestWay()
    {
        var placed = GraphLayout.Place(Packs(), rules: FlowRules.Straight);
        var from = placed.Nodes.Single(node => node.Node.Id == "approved");
        var to = placed.Nodes.Single(node => node.Node.Id == "unavailable");
        string path = placed.Edges.Single(edge => edge.Edge.From == "approved" && edge.Edge.To == "unavailable").Path;

        // Every height the line passes, in a picture that runs right: none above the higher of its two boxes or
        // under the lower, a box's own height allowed for.
        var heights = path.Split(' ').Where(part => double.TryParse(part, out _)).Select(double.Parse)
            .Where((_, at) => at % 2 == 1).ToList();
        double top = Math.Min(from.Y, to.Y), bottom = Math.Max(from.Y + from.H, to.Y + to.H);

        Assert.Multiple(() =>
        {
            Assert.That(heights.Min(), Is.GreaterThanOrEqualTo(top - 1), "it does not swing out above");
            Assert.That(heights.Max(), Is.LessThanOrEqualTo(bottom + 1), "nor under");
            Assert.That(path.Count(part => part == 'Q'), Is.LessThanOrEqualTo(4));
        });
    }
}
