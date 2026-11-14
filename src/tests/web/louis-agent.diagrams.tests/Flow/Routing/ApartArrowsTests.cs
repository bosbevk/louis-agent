using System.Globalization;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Routing;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Routing;

/// <summary>
/// The rule a flow is drawn by (F134.8): each arrow into a box arrives at its own point, a step from the next, the
/// upper source at the upper point, and an arrow that can run straight stays straight.
/// </summary>
public class ApartArrowsTests
{
    private static readonly ApartArrows Rule = new();

    /// <summary>A box of the site's size at a place.</summary>
    private static PlacedNode Box(string id, double x, double y) => new(new GraphNode(id, id), x, y, 170, 52);

    /// <summary>Three boxes on the left, one under the other, and the box at the right they feed.</summary>
    private static Dictionary<string, PlacedNode> Feeding(double targetTop) => new()
    {
        ["a"] = Box("a", 0, 0),
        ["b"] = Box("b", 0, 100),
        ["c"] = Box("c", 0, 200),
        ["x"] = Box("x", 300, targetTop)
    };

    /// <summary>Every point of a path, a curve's corner and its ends each as one.</summary>
    private static List<(double X, double Y)> Points(string path)
    {
        var numbers = path.Split(' ', 'L', 'M', 'Q').Where(part => part.Length > 0)
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).ToList();
        return [.. Enumerable.Range(0, numbers.Count / 2).Select(at => (numbers[2 * at], numbers[2 * at + 1]))];
    }

    /// <summary>One arrow into a box arrives at the middle of its side, as it always did.</summary>
    [Test]
    public void OneArrowAlone_ArrivesAtTheMiddle()
    {
        GraphEdge[] forward = [new("a", "x")];

        Assert.That(Rule.EntryOf(forward[0], forward, Feeding(50)), Is.EqualTo(76));
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
            Assert.That(Rule.EntryOf(forward[1], forward, boxes), Is.EqualTo(67), "a is the upper source");
            Assert.That(Rule.EntryOf(forward[0], forward, boxes), Is.EqualTo(85), "b arrives a step under it");
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
            Assert.That(Rule.EntryOf(forward[0], forward, boxes), Is.EqualTo(26), "level with a, so straight");
            Assert.That(Rule.EntryOf(forward[1], forward, boxes), Is.EqualTo(44), "b a step under it");
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

        var entries = forward.Select(edge => Rule.EntryOf(edge, forward, boxes));

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
            Assert.That(Rule.SideFor(1), Is.Zero, "one arrow asks for nothing");
            Assert.That(Rule.SideFor(3), Is.LessThanOrEqualTo(52), "three fit the site's box");
            Assert.That(Rule.SideFor(4), Is.EqualTo(84), "four need two steps either side of the middle, and a margin");
        });
    }

    /// <summary>A box four others feed grows to hold its four points, and joined it stays the size it was.</summary>
    [Test]
    public void ABoxWithMorePointsThanItsSideHolds_Grows()
    {
        var spec = new GraphSpec(
            [.. new[] { "a", "b", "c", "d", "x" }.Select(id => new GraphNode(id, id))],
            [.. new[] { "a", "b", "c", "d" }.Select(id => new GraphEdge(id, "x"))]);

        double joined = GraphLayout.Place(spec).Nodes.Single(node => node.Node.Id == "x").H;
        double apart = GraphLayout.Place(spec, rule: Rule).Nodes.Single(node => node.Node.Id == "x").H;

        Assert.Multiple(() =>
        {
            Assert.That(joined, Is.EqualTo(52));
            Assert.That(apart, Is.EqualTo(84));
        });
    }

    /// <summary>
    /// An arrow from a level box two columns away stays one straight line, though a second arrow arrives at the same
    /// box: the second takes the point a step beside it. Spread round the middle, the straight one would kink.
    /// </summary>
    [Test]
    public void ALongArrowFromALevelBox_StaysStraight()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A", Column: 0), new GraphNode("b", "B", Column: 1), new GraphNode("x", "X", null, 2)],
            [new GraphEdge("a", "x"), new GraphEdge("b", "x")]);

        var placed = GraphLayout.Place(spec, rule: Rule);
        var straight = placed.Edges.Single(edge => edge.Edge.From == "a");
        var beside = placed.Edges.Single(edge => edge.Edge.From == "b");

        Assert.Multiple(() =>
        {
            Assert.That(Points(straight.Path).Select(point => point.Y).Distinct().Count(), Is.EqualTo(1),
                "one height from end to end");
            Assert.That(Math.Abs(Points(beside.Path)[^1].Y - Points(straight.Path)[^1].Y), Is.EqualTo(18),
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

        var paths = GraphLayout.Place(spec, rule: Rule).Edges.Select(edge => Points(edge.Path)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(paths[1][^1].Y - paths[0][^1].Y, Is.EqualTo(18), "a step apart, a above b");
            foreach (var path in paths)
                Assert.That(path.TakeLast(3).Select(point => point.Y), Is.All.EqualTo(path[^1].Y),
                    "straight across the column between and into the box");
        });
    }
}
