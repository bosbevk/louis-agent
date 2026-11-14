using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Rows;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Rows;

/// <summary>
/// The rule a flow's boxes are placed by (F134.8): a box stands on the row level with the box that feeds it, in the
/// order the column has, and bands that stand beside each other share one row height so their rows line up.
/// </summary>
public class LevelRowsTests
{
    private static readonly LevelRows Rule = new();

    /// <summary>
    /// Columns of boxes named by their ids, in one band of rows 52 tall and 18 apart, with these arrows.
    /// </summary>
    private static RowGrid Grid(string[][] columns, params (string From, string To)[] arrows) => new(
        [.. columns.Select(column => column.Select(id => new GraphNode(id, id)).ToList())],
        _ => 0, [0], [52], [columns.Max(column => column.Length)], 18,
        [.. arrows.Select(arrow => new GraphEdge(arrow.From, arrow.To))]);

    /// <summary>
    /// Two bands that share no column stand beside each other, so both get the taller row height and their rows
    /// line up.
    /// </summary>
    [Test]
    public void BandsBesideEachOther_ShareTheTallerRowHeight()
    {
        BandShape[] bands = [new(68, new HashSet<int> { 0 }, 3), new(84, new HashSet<int> { 1, 2 }, 4)];

        Assert.That(Rule.RowHeights(bands, 18), Is.EqualTo(new[] { 84.0, 84.0 }));
    }

    /// <summary>
    /// One tall box beside nine rows of short ones doesn't make all nine as tall as itself: its row spans three of
    /// theirs, gaps included, so its middle is still the middle of one of their rows.
    /// </summary>
    [Test]
    public void ATallBoxBesideManyRows_SpansThreeOfThem()
    {
        BandShape[] bands = [new(90, new HashSet<int> { 0 }, 9), new(164, new HashSet<int> { 1 }, 1)];

        Assert.That(Rule.RowHeights(bands, 18), Is.EqualTo(new[] { 90.0, 3 * 90 + 2 * 18.0 }));
    }

    /// <summary>Two bands that share a column stand one under the other, so each keeps its own row height.</summary>
    [Test]
    public void BandsThatShareAColumn_KeepTheirOwnRowHeight()
    {
        BandShape[] bands = [new(68, new HashSet<int> { 0, 1 }, 3), new(84, new HashSet<int> { 1, 2 }, 4)];

        Assert.That(Rule.RowHeights(bands, 18), Is.EqualTo(new[] { 68.0, 84.0 }));
    }

    /// <summary>
    /// A band under another starts where that one ends, so its rows line up with nobody's. It keeps its own row
    /// height, though it shares no column with the band at the top of the next column.
    /// </summary>
    [Test]
    public void ABandUnderAnother_KeepsItsOwnRowHeight()
    {
        BandShape[] bands =
        [
            new(68, new HashSet<int> { 0 }, 1), new(84, new HashSet<int> { 1 }, 4), new(52, new HashSet<int> { 0 }, 5)
        ];

        Assert.That(Rule.RowHeights(bands, 18), Is.EqualTo(new[] { 84.0, 84.0, 52.0 }));
    }

    /// <summary>A box stands on the row of the box that feeds it, not in the middle of its band.</summary>
    [Test]
    public void ABox_StandsLevelWithTheBoxThatFeedsIt()
    {
        var tops = Rule.RowTops(Grid([["a", "b", "c"], ["x"]], ("c", "x")));

        Assert.That(tops["x"], Is.EqualTo(tops["c"]));
    }

    /// <summary>A box nothing feeds stands level with the box it feeds.</summary>
    [Test]
    public void ABoxNothingFeeds_StandsLevelWithWhatItFeeds()
    {
        var tops = Rule.RowTops(Grid([["s"], ["x", "y", "z"]], ("s", "z")));

        Assert.That(tops["s"], Is.EqualTo(tops["z"]));
    }

    /// <summary>One box feeding two: the first stands level with it, and the second on the row under.</summary>
    [Test]
    public void TwoBoxesFedByOne_StandLevelAndOnTheRowUnder()
    {
        var tops = Rule.RowTops(Grid([["a", "b", "c"], ["x", "y"]], ("a", "x"), ("a", "y")));

        Assert.Multiple(() =>
        {
            Assert.That(tops["x"], Is.EqualTo(tops["a"]));
            Assert.That(tops["y"], Is.EqualTo(tops["b"]));
        });
    }

    /// <summary>
    /// A column keeps its order whatever its boxes are fed by: the order is what keeps the arrows from crossing, so
    /// the upper box stays above though the lowest box feeds it.
    /// </summary>
    [Test]
    public void AColumn_KeepsItsOrder()
    {
        var tops = Rule.RowTops(Grid([["a", "b", "c", "d"], ["x", "y"]], ("d", "x"), ("a", "y")));

        Assert.That(tops["x"], Is.LessThan(tops["y"]));
    }

    /// <summary>
    /// The case Louis marked: a box in a group feeds a box outside it. With the rows of the two bands lined up, the
    /// line between them is one straight line, where the joined layout bends it.
    /// </summary>
    [Test]
    public void ALineAcrossAGroupsEdge_RunsStraight()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("program", "Program", Group: "site"), new GraphNode("page", "Page", Group: "site"),
                new GraphNode("layout", "Layout", Group: "site"), new GraphNode("frame", "Frame"),
                new GraphNode("runner", "Runner"), new GraphNode("views", "Views"), new GraphNode("menu", "Menu"),
                new GraphNode("screen", "Screen\nand its table\nor a view")
            ],
            [
                new GraphEdge("page", "frame"), new GraphEdge("frame", "runner"), new GraphEdge("frame", "views"),
                new GraphEdge("frame", "menu"), new GraphEdge("frame", "screen")
            ],
            Groups: [new GraphGroup("site", "A site")]);

        bool Straight(FlowRules rules) => !GraphLayout.Place(spec, rules: rules).Edges
            .Single(edge => edge.Edge.To == "frame").Path.Contains('Q');

        Assert.Multiple(() =>
        {
            Assert.That(Straight(FlowRules.Joined), Is.False, "joined, the two bands' rows don't line up");
            Assert.That(Straight(FlowRules.Straight), Is.True, "level, the page and the frame stand on one row");
        });
    }
}
