using LouisAgent.Diagrams.Engine.Nested;
using LouisAgent.Diagrams.Nested.Spec;

namespace LouisAgent.Diagrams.Tests.Nested.Layout;

/// <summary>
/// F162.1 (Louis, of the framework's map: "split this in two columns"): a box may hold its boxes in columns, side by
/// side in rows, read left to right, so a box holding many is wide rather than tall. One column is the default, so
/// every picture that names none is drawn as before.
/// </summary>
public class NestedColumnsTests
{
    /// <summary>A box holding four, in the columns given.</summary>
    private static NestedSpec Four(int columns) => new(new NestedBox("root", "Root", Columns: columns,
        Holds: [new("a", "A"), new("b", "B", Note: "taller, with a note"), new("c", "C"), new("d", "D")]));

    /// <summary>The box placed with this id.</summary>
    private static Diagrams.Engine.Geometry.IBox Box(NestedSpec spec, string id) =>
        NestedLayout.Of(spec).Boxes.Single(box => box.Component.Id == id).Box;

    /// <summary>In two columns, the first two stand side by side, the next two in the row under them.</summary>
    [Test]
    public void InTwoColumns_TheBoxesStandInRows()
    {
        var spec = Four(2);
        var (a, b, c, d) = (Box(spec, "a"), Box(spec, "b"), Box(spec, "c"), Box(spec, "d"));

        Assert.Multiple(() =>
        {
            Assert.That(b.Left, Is.GreaterThan(a.Right), "B beside A");
            Assert.That(b.Top, Is.EqualTo(a.Top), "on one row");
            Assert.That(c.Left, Is.EqualTo(a.Left), "C under A");
            Assert.That(c.Top, Is.GreaterThan(Math.Max(a.Bottom, b.Bottom)), "the next row under the taller");
            Assert.That(d.Left, Is.EqualTo(b.Left), "D under B");
            Assert.That(a.Width, Is.EqualTo(b.Width), "the columns are as wide as each other");
        });
    }

    /// <summary>Two columns make the box wider and shorter than one does.</summary>
    [Test]
    public void TwoColumns_AreWiderAndShorter()
    {
        var one = Box(Four(1), "root");
        var two = Box(Four(2), "root");

        Assert.Multiple(() =>
        {
            Assert.That(two.Width, Is.GreaterThan(one.Width));
            Assert.That(two.Height, Is.LessThan(one.Height));
        });
    }

    /// <summary>A box that names no columns stacks what it holds, one under another, as before.</summary>
    [Test]
    public void NoColumnsNamed_StacksAsBefore()
    {
        var spec = new NestedSpec(new NestedBox("root", "Root", Holds: [new("a", "A"), new("b", "B")]));

        Assert.That(Box(spec, "b").Top, Is.GreaterThan(Box(spec, "a").Bottom));
    }
}
