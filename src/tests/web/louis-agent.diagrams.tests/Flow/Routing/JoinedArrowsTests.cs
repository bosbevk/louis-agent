using System.Globalization;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Routing;
using LouisAgent.Diagrams.Flow.Rows;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Routing;

/// <summary>
/// The joined rule is how every picture was drawn before a flow got a rule of its own (F134.8): arrows into a box
/// meet at the middle of its side, and a gap has a track for each source only when several boxes feed several.
/// </summary>
public class JoinedArrowsTests
{
    private static readonly JoinedArrows Rule = new();

    /// <summary>
    /// Two boxes on the left, a and b, and two on the right, x and y, each level with one on the left.
    /// </summary>
    private static Dictionary<string, PlacedNode> Boxes() => new()
    {
        ["a"] = Box("a", 0, 0),
        ["b"] = Box("b", 0, 100),
        ["x"] = Box("x", 300, 0),
        ["y"] = Box("y", 300, 100)
    };

    /// <summary>A box of the site's size at a place.</summary>
    private static PlacedNode Box(string id, double x, double y) => new(new GraphNode(id, id), x, y, 170, 52);

    /// <summary>One box that feeds the rest has one line out, so the gap needs no second track.</summary>
    [Test]
    public void OneBoxFeedingTheRest_NeedsNoSecondTrack()
    {
        GraphEdge[] gap = [new("a", "x"), new("a", "y")];

        Assert.That(Rule.TracksRoom(gap, gap, Boxes()), Is.Zero);
    }

    /// <summary>When every arrow of a gap feeds the same box, they join on one track.</summary>
    [Test]
    public void EveryBoxFeedingOne_NeedsNoSecondTrack()
    {
        GraphEdge[] gap = [new("a", "x"), new("b", "x")];

        Assert.That(Rule.TracksRoom(gap, gap, Boxes()), Is.Zero);
    }

    /// <summary>
    /// Several boxes feeding several get a track each, a step apart, the bottom source on the track nearest the
    /// sources.
    /// </summary>
    [Test]
    public void SeveralFeedingSeveral_GetATrackEach()
    {
        GraphEdge[] gap = [new("a", "x"), new("b", "y")];
        var boxes = Boxes();

        Assert.Multiple(() =>
        {
            Assert.That(Rule.TracksRoom(gap, gap, boxes), Is.EqualTo(18), "one step for the second source");
            Assert.That(Rule.TrackOf(gap[1], gap, gap, boxes), Is.Zero, "the bottom source bends first");
            Assert.That(Rule.TrackOf(gap[0], gap, gap, boxes), Is.EqualTo(1), "the top source a step later");
        });
    }

    /// <summary>Every arrow arrives at the middle of its target's side, so two into one box meet there.</summary>
    [Test]
    public void EveryArrow_ArrivesAtTheMiddleOfItsTarget()
    {
        GraphEdge[] forward = [new("a", "x"), new("b", "x")];
        var boxes = Boxes();

        Assert.Multiple(() =>
        {
            Assert.That(Rule.EntryOf(forward[0], forward, boxes), Is.EqualTo(26));
            Assert.That(Rule.EntryOf(forward[1], forward, boxes), Is.EqualTo(26));
        });
    }

    /// <summary>
    /// A layout that is handed no rule is drawn joined, so nothing changes for a caller that names none.
    /// </summary>
    [Test]
    public void ALayoutHandedNoRule_IsDrawnJoined()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("x", "X"), new GraphNode("y", "Y")],
            [new GraphEdge("a", "x"), new GraphEdge("b", "x"), new GraphEdge("b", "y")]);

        var unnamed = GraphLayout.Place(spec).Edges.Select(edge => edge.Path);
        var joined = GraphLayout.Place(spec, rules: FlowRules.Joined).Edges.Select(edge => edge.Path);

        Assert.That(joined, Is.EqualTo(unnamed));
    }

    /// <summary>
    /// A joined long arrow never passes between two boxes of the column it crosses, as a flow's does: a class chart
    /// keeps its lines where they were until it gets the new look. Here the start stands level with the list it
    /// feeds, so its long arrow can run at neither of its own heights, and stays on its row above the list.
    /// </summary>
    [Test]
    public void ALongArrowWithItsLevelsBlocked_StaysOnItsRow()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("start", "Start", Column: 0), new GraphNode("list", "List", Column: 1),
                new GraphNode("stage", "Stage", Column: 1), new GraphNode("deck", "Deck", Column: 2),
                new GraphNode("dock", "Dock", Column: 2)
            ],
            [
                new GraphEdge("start", "list"), new GraphEdge("start", "stage"), new GraphEdge("start", "deck"),
                new GraphEdge("start", "dock"), new GraphEdge("list", "deck"), new GraphEdge("list", "dock")
            ]);
        var levelRows = new FlowRules(Rule, new LevelRows());

        var placed = GraphLayout.Place(spec, rules: levelRows);
        double top = placed.Nodes.Min(node => node.Y);

        Assert.That(placed.Edges.Single(edge => edge.Edge is { From: "start", To: "deck" }).Path,
            Does.Contain($" {(top - 44).ToString(CultureInfo.InvariantCulture)} L"),
            "it runs along the row above the first box");
    }
}
