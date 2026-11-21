using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Geometry.Generic;

namespace LouisAgent.Diagrams.Tests.Geometry.Generic;

/// <summary>
/// CL65.6: what is our own family's alone. In <c>double</c> its sums are the ones the layouts wrote by hand, to
/// the last digit, and the same box works over another number type. What every family's box knows is held by
/// <see cref="GeometryFamiliesTests"/>.
/// </summary>
public class GenericGeometryTests
{
    /// <summary>Our family in the number type the engine uses.</summary>
    private static readonly IGeometry Ours = new GenericGeometry<double>();

    /// <summary>
    /// With numbers that have no exact form, a box answers what <c>x + w</c> and <c>x + w / 2</c> answered where
    /// they stood, so no picture changes.
    /// </summary>
    [Test]
    public void ABoxsSums_AreTheOnesWrittenByHand()
    {
        const double x = 0.1, y = 0.7, w = 0.2, h = 0.3, room = 0.6;
        var box = Ours.Box(x, y, w, h);

        Assert.Multiple(() =>
        {
            Assert.That(box.Right, Is.EqualTo(x + w), "0.30000000000000004, as written by hand");
            Assert.That(box.Bottom, Is.EqualTo(y + h));
            Assert.That(box.Middle.X, Is.EqualTo(x + w / 2));
            Assert.That(box.Middle.Y, Is.EqualTo(y + h / 2));
            Assert.That(box.Grown(room).Left, Is.EqualTo(x - room));
            Assert.That(box.Grown(room).Width, Is.EqualTo(w + 2 * room));
        });
    }

    /// <summary>
    /// The sums the flow's layout and router wrote by hand, each answered to the last digit: a frame's width, a
    /// box put on a line, and a place part of the way along a side.
    /// </summary>
    [Test]
    public void TheFlowsSums_AreTheOnesWrittenByHand()
    {
        const double x = 0.1, y = 0.7, w = 0.2, h = 0.3, left = 0.6, top = 1.3, pad = 0.9, line = 7.7, part = 0.4;
        var box = Ours.Box(x, y, w, h);
        var frame = box.Grown(left, top, pad, pad);

        Assert.Multiple(() =>
        {
            Assert.That(frame.Left, Is.EqualTo(x - left));
            Assert.That(frame.Top, Is.EqualTo(y - top));
            Assert.That(frame.Width, Is.EqualTo(w + left + pad));
            Assert.That(frame.Height, Is.EqualTo(h + top + pad));
            Assert.That(box.OnLine(line).Top, Is.EqualTo(line - h / 2));
            Assert.That(box.Across(part), Is.EqualTo(x + w * part));
            Assert.That(box.Down(part), Is.EqualTo(y + h * part));
            Assert.That(box.Moved(left, top).Left, Is.EqualTo(x + left));
        });
    }

    /// <summary>
    /// One type over any number: in <c>decimal</c> the same box answers 0.3 where <c>double</c> answers
    /// 0.30000000000000004, with not a line of the box written twice.
    /// </summary>
    [Test]
    public void TheSameBox_WorksOverAnotherNumberType()
    {
        var inDecimal = new GenericGeometry<decimal>().Box(0.1, 0, 0.2, 0);

        Assert.Multiple(() =>
        {
            Assert.That(inDecimal.Right, Is.EqualTo(0.3));
            Assert.That(Ours.Box(0.1, 0, 0.2, 0).Right, Is.Not.EqualTo(0.3));
        });
    }
}
