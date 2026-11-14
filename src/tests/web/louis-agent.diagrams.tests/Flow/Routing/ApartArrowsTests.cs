using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Engine.Flow.Routing;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tests.Flow.Arrows;

namespace LouisAgent.Diagrams.Tests.Flow.Routing;

/// <summary>
/// The rule a flow is drawn by (F134.8): each arrow into a box arrives at its own point, a step from the next, the
/// upper source at the upper point, and an arrow that can run straight stays straight.
/// </summary>
public class ApartArrowsTests
{
    /// <summary>A box of the site's size at a place.</summary>
    private static PlacedNode Box(string id, double x, double y) =>
        new(new GraphNode(id, id), Geometries.InUse.Box(x, y, 170, 52));

    /// <summary>Three boxes on the left, one under the other, and the box at the right they feed.</summary>
    private static Dictionary<string, PlacedNode> Feeding(double targetTop) => new()
    {
        ["a"] = Box("a", 0, 0),
        ["b"] = Box("b", 0, 100),
        ["c"] = Box("c", 0, 200),
        ["x"] = Box("x", 300, targetTop)
    };

    /// <summary>One arrow into a box arrives at the middle of its side, as it always did.</summary>
    [Test]
    public void OneArrowAlone_ArrivesAtTheMiddle()
    {
        GraphEdge[] forward = [new("a", "x")];

        Assert.That(ApartArrows.EntryOf(forward[0], forward, Feeding(50)), Is.EqualTo(76));
    }

    /// <summary>
    /// Two arrows into one box arrive a step apart, either side of the middle, the upper source at the upper point,
    /// so the two lines come in beside each other and don't cross.
    /// </summary>
    [Test]
    public void TwoArrowsIntoOneBox_ArriveAStepApart_TheUpperSourceAbove()
    {
        GraphEdge[] forward = [new("b", "x"), new("a", "x")];
        var boxes = Feeding(50);

        Assert.Multiple(() =>
        {
            Assert.That(ApartArrows.EntryOf(forward[1], forward, boxes), Is.EqualTo(67), "a is the upper source");
            Assert.That(ApartArrows.EntryOf(forward[0], forward, boxes), Is.EqualTo(85), "b arrives a step under it");
        });
    }

    /// <summary>
    /// An arrow from a box level with its target runs straight in, and the other arrow takes the point a step
    /// beside it (Louis's pick: the straight one keeps its level).
    /// </summary>
    [Test]
    public void AnArrowThatRunsStraight_KeepsItsLevel()
    {
        GraphEdge[] forward = [new("a", "x"), new("b", "x")];
        var boxes = Feeding(0);

        Assert.Multiple(() =>
        {
            Assert.That(ApartArrows.EntryOf(forward[0], forward, boxes), Is.EqualTo(26), "level with a, so straight");
            Assert.That(ApartArrows.EntryOf(forward[1], forward, boxes), Is.EqualTo(44), "b a step under it");
        });
    }

    /// <summary>
    /// When keeping the straight one level would push another point off the side, the points are spread round the
    /// middle instead: a point off the box is worse than a line that bends.
    /// </summary>
    [Test]
    public void WhenAPointWouldLeaveTheSide_ThePointsAreCentred()
    {
        GraphEdge[] forward = [new("a", "x"), new("b", "x"), new("c", "x")];
        var boxes = Feeding(0);

        var entries = forward.Select(edge => ApartArrows.EntryOf(edge, forward, boxes));

        Assert.That(entries, Is.EqualTo(new[] { 8.0, 26.0, 44.0 }));
    }

    /// <summary>
    /// A side is as long as its points need: two or three arrows fit the site's box, a fourth makes it grow.
    /// </summary>
    [Test]
    public void ASide_IsAsLongAsItsPointsNeed()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ApartArrows.SideFor(1), Is.Zero, "one arrow asks for nothing");
            Assert.That(ApartArrows.SideFor(3), Is.LessThanOrEqualTo(52), "three fit the site's box");
            Assert.That(ApartArrows.SideFor(4), Is.EqualTo(84),
                "four need two steps either side of the middle, and a margin");
        });
    }

    /// <summary>A box four others feed grows to hold its four points.</summary>
    [Test]
    public void ABoxWithMorePointsThanItsSideHolds_Grows()
    {
        var spec = new GraphSpec(
            [.. new[] { "a", "b", "c", "d", "x" }.Select(id => new GraphNode(id, id))],
            [.. new[] { "a", "b", "c", "d" }.Select(id => new GraphEdge(id, "x"))]);

        double height = GraphLayout.Place(spec).Nodes.Single(node => node.Node.Id == "x").Box.Height;

        Assert.That(height, Is.EqualTo(84));
    }

    /// <summary>
    /// Two lines whose upright pieces are clear of each other turn on the same line (Louis: "parallel"), so the gap
    /// needs no second track: here each box feeds the one a little under it.
    /// </summary>
    [Test]
    public void LinesWhoseUprightsAreClear_TurnOnOneLine()
    {
        var boxes = new Dictionary<string, PlacedNode>
        {
            ["a"] = Box("a", 0, 0),
            ["b"] = Box("b", 0, 200),
            ["x"] = Box("x", 300, 50),
            ["y"] = Box("y", 300, 250)
        };
        GraphEdge[] gap = [new("a", "x"), new("b", "y")];

        Assert.Multiple(() =>
        {
            Assert.That(ApartArrows.TracksRoom(gap, gap, boxes), Is.Zero);
            Assert.That(ApartArrows.TrackOf(gap[0], gap, gap, boxes),
                Is.EqualTo(ApartArrows.TrackOf(gap[1], gap, gap, boxes)));
        });
    }

    /// <summary>
    /// Two boxes that feed one, from above it and from under it, turn on one line too: their points keep the two
    /// uprights a step apart, end to end.
    /// </summary>
    [Test]
    public void TwoBoxesFeedingOne_TurnOnOneLine()
    {
        var boxes = Feeding(100);
        GraphEdge[] gap = [new("a", "x"), new("c", "x")];

        Assert.That(ApartArrows.TracksRoom(gap, gap, boxes), Is.Zero);
    }

    /// <summary>An upright that would lie on another's gets the track a step beside it.</summary>
    [Test]
    public void UprightsThatWouldLieOnOneAnother_GetATrackEach()
    {
        var boxes = new Dictionary<string, PlacedNode>
        {
            ["a"] = Box("a", 0, 0),
            ["b"] = Box("b", 0, 100),
            ["x"] = Box("x", 300, 50),
            ["y"] = Box("y", 300, 250)
        };
        GraphEdge[] gap = [new("a", "y"), new("b", "x")];

        Assert.Multiple(() =>
        {
            Assert.That(ApartArrows.TracksRoom(gap, gap, boxes), Is.EqualTo(18), "one track beside the first");
            Assert.That(ApartArrows.TrackOf(gap[0], gap, gap, boxes),
                Is.Not.EqualTo(ApartArrows.TrackOf(gap[1], gap, gap, boxes)));
        });
    }

    /// <summary>
    /// A fan of boxes into one above them does not cross: the upper box turns first and takes the upper point, so
    /// each line runs inside the one after it.
    /// </summary>
    [Test]
    public void AFanIntoABoxAboveIt_DoesNotCross()
    {
        var boxes = new Dictionary<string, PlacedNode>
        {
            ["x"] = Box("x", 300, 0),
            ["a"] = Box("a", 0, 100),
            ["b"] = Box("b", 0, 200),
            ["c"] = Box("c", 0, 300)
        };
        GraphEdge[] gap = [new("a", "x"), new("b", "x"), new("c", "x")];

        var tracks = gap.Select(edge => ApartArrows.TrackOf(edge, gap, gap, boxes));

        Assert.That(tracks, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    /// <summary>A fan into a box under them is the mirror: the lowest box turns first.</summary>
    [Test]
    public void AFanIntoABoxUnderIt_DoesNotCross()
    {
        var boxes = Feeding(300);
        GraphEdge[] gap = [new("a", "x"), new("b", "x"), new("c", "x")];

        var tracks = gap.Select(edge => ApartArrows.TrackOf(edge, gap, gap, boxes));

        Assert.That(tracks, Is.EqualTo(new[] { 2, 1, 0 }));
    }

    /// <summary>
    /// A point never stands level with the line out of a box next door that feeds something else, since the arrow
    /// arriving there and that line out would lie on one another: it stands half a step aside, towards the box its
    /// own arrow comes from.
    /// </summary>
    [Test]
    public void APoint_StepsAsideFromTheLineOutOfABoxNextDoor()
    {
        var boxes = new Dictionary<string, PlacedNode>
        {
            ["a"] = Box("a", 0, 0),
            ["b"] = Box("b", 0, 100),
            ["x"] = Box("x", 300, 0),
            ["y"] = Box("y", 300, 200)
        };
        GraphEdge[] forward = [new("a", "y"), new("b", "x")];

        Assert.That(ApartArrows.EntryOf(forward[1], forward, boxes), Is.EqualTo(35),
            "x is level with a, and b is under it");
    }

    /// <summary>
    /// An arrow passes straight through a waypoint, so its point there can't step aside. Then the box level with
    /// that point turns first, so its line out ends before the other's line in begins.
    /// </summary>
    [Test]
    public void ABoxLevelWithAWaypoint_TurnsFirst()
    {
        var boxes = new Dictionary<string, PlacedNode>
        {
            ["a"] = Box("a", 0, 0),
            ["b"] = Box("b", 0, 100),
            ["→w"] = Box("→w", 300, 0),
            ["y"] = Box("y", 300, 200)
        };
        GraphEdge[] gap = [new("a", "y"), new("b", "→w")];

        Assert.Multiple(() =>
        {
            Assert.That(ApartArrows.EntryOf(gap[1], gap, boxes), Is.EqualTo(26),
                "straight through the waypoint's middle");
            Assert.That(ApartArrows.TrackOf(gap[0], gap, gap, boxes), Is.Zero, "a's line out is level with that point");
            Assert.That(ApartArrows.TrackOf(gap[1], gap, gap, boxes), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// A long arrow that can run neither at the height it arrives at nor at the height it leaves at, since a box of
    /// the column it passes stands at both, runs between two boxes of that column, not round the outside of them.
    /// Here the start stands level with the list it feeds, and its two long arrows pass under the list.
    /// </summary>
    [Test]
    public void ALongArrowWithItsLevelsBlocked_RunsBetweenTwoBoxes()
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

        var placed = GraphLayout.Place(spec);
        var middle = placed.Nodes.Where(node => node.Node.Column == 1).ToList();
        // Where the two long arrows pass the middle column: the points of their paths from its left side to its right.
        var through = placed.Edges.Where(edge => edge.Edge is { From: "start", To: "deck" or "dock" })
            .SelectMany(edge => Ways.Points(edge.Way))
            .Where(point => point.X > middle[0].Box.Left - 1 && point.X < middle[0].Box.Right + 1)
            .Select(point => point.Y).ToList();

        Assert.That(through, Has.Count.EqualTo(4), "each arrow enters the column and leaves it");
        Assert.That(through, Is.All.InRange(middle.Min(node => node.Box.Bottom), middle.Max(node => node.Box.Top)),
            "between the upper box's bottom and the lower box's top");
    }

    /// <summary>
    /// An arrow's word stands on its own line, the line through the middle of its pill. Above the line, as joined
    /// arrows have it, the word of the lower of two arrows into one box would sit on the upper one, a step away.
    /// </summary>
    [Test]
    public void AWord_StandsOnItsOwnLine()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("x", "X")],
            [new GraphEdge("a", "x"), new GraphEdge("b", "x", "writes")]);

        var worded = GraphLayout.Place(spec).Edges
            .Single(edge => edge.Edge.Word is not null);
        var ground = FlowText.WordBox(worded, Geometries.InUse);

        Assert.That(ground.Middle.Y, Is.EqualTo(Ways.Points(worded.Way)[0].Y), "level with where its arrow leaves");
    }

    /// <summary>
    /// A box fed from the next column and from two columns away stands level with the nearer one, whose arrow runs
    /// straight in. The long arrow runs level past that column, and arrives at the point a step beside the straight
    /// one. Spread round the middle, the straight one would kink.
    /// </summary>
    [Test]
    public void ALongArrowBesideALevelFeeder_ArrivesAStepBesideIt()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", Column: 0), new GraphNode("b", "B", Column: 1), new GraphNode("x", "X", null, 2)],
            [new GraphEdge("a", "x"), new GraphEdge("b", "x")]);

        var placed = GraphLayout.Place(spec);
        var straight = placed.Edges.Single(edge => edge.Edge.From == "b");
        var beside = placed.Edges.Single(edge => edge.Edge.From == "a");
        var column = placed.Nodes.Single(node => node.Node.Id == "b").Box;

        Assert.Multiple(() =>
        {
            Assert.That(Ways.Points(straight.Way).Select(point => point.Y).Distinct().Count(), Is.EqualTo(1),
                "one height from end to end");
            Assert.That(Ways.Points(beside.Way).Where(point => point.X <= column.Right).Select(point => point.Y)
                .Distinct().Count(), Is.EqualTo(1), "level past the column between");
            Assert.That(Math.Abs(Ways.Points(beside.Way)[^1].Y - Ways.Points(straight.Way)[^1].Y), Is.EqualTo(18),
                "the other arrives a step beside it");
        });
    }

    /// <summary>
    /// Two long arrows into one box each run straight into their own point: after its one bend, every point of an
    /// arrow is at the height where it arrives, and the two arrive a step apart.
    /// </summary>
    [Test]
    public void ALongArrow_RunsStraightIntoItsOwnPoint()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", Column: 0), new GraphNode("b", "B", Column: 0), new GraphNode("x", "X", null, 2)],
            [new GraphEdge("a", "x"), new GraphEdge("b", "x")]);

        var paths = GraphLayout.Place(spec).Edges.Select(edge => Ways.Points(edge.Way)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(paths[1][^1].Y - paths[0][^1].Y, Is.EqualTo(18), "a step apart, a above b");
            foreach (var path in paths)
                Assert.That(path.TakeLast(3).Select(point => point.Y), Is.All.EqualTo(path[^1].Y),
                    "straight across the column between and into the box");
        });
    }
}
