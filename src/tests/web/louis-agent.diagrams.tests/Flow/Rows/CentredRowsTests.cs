using LouisAgent.Diagrams.Engine.Flow.Rows;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Tests.Flow.Rows;

/// <summary>
/// The centred rule is how every picture's boxes were placed before a flow got a rule of its own (F134.8): each
/// band's rows as tall as its own tallest box, and a column's boxes one after another, centred in their band.
/// </summary>
public class CentredRowsTests
{
    private static readonly CentredRows Rule = new();

    /// <summary>One box in the first column and three in the second, all in one band of three rows 52 tall.</summary>
    private static RowGrid OneBesideThree() => new(
        [[new GraphNode("a", "A")], [new GraphNode("x", "X"), new GraphNode("y", "Y"), new GraphNode("z", "Z")]],
        _ => 0, [0], [52], [3], 18, []);

    /// <summary>A band's rows are as tall as its own tallest box, whatever stands in the band beside it.</summary>
    [Test]
    public void EachBand_KeepsItsOwnRowHeight()
    {
        BandShape[] bands = [new(52, new HashSet<int> { 0 }, 3), new(84, new HashSet<int> { 1 }, 4)];

        Assert.That(Rule.RowHeights(bands, 18), Is.EqualTo(new[] { 52.0, 84.0 }));
    }

    /// <summary>The fullest column fills the band's rows from the top, a row and a gap apart.</summary>
    [Test]
    public void TheFullestColumn_FillsTheRows()
    {
        var tops = Rule.RowTops(OneBesideThree());

        Assert.That(new[] { tops["x"], tops["y"], tops["z"] }, Is.EqualTo(new[] { 0.0, 70.0, 140.0 }));
    }

    /// <summary>
    /// A shorter column stands in the middle of the band: one box beside three is level with the second.
    /// </summary>
    [Test]
    public void AShorterColumn_IsCentredInItsBand()
    {
        Assert.That(Rule.RowTops(OneBesideThree())["a"], Is.EqualTo(70));
    }
}
