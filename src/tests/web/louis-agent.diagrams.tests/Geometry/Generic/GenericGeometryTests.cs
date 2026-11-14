using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Geometry.Generic;

namespace LouisAgent.Diagrams.Tests.Geometry.Generic;

/// <summary>
/// CL65.6: a box and a point of our own family know their sums, so no layout works them out where it stands
/// (ADR 0123). They are asked through the seam, as a layout asks them.
/// </summary>
public class GenericGeometryTests
{
    /// <summary>Our family in the number type the engine uses.</summary>
    private static readonly IGeometry Ours = new GenericGeometry<double>();

    /// <summary>A box 200 wide and 64 tall, its corner at 120 across and 40 down.</summary>
    private static IBox Box() => Ours.Box(120, 40, 200, 64);

    /// <summary>A box keeps the place and the size it was made with.</summary>
    [Test]
    public void ABox_KeepsWhatItWasMadeWith()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Box().Left, Is.EqualTo(120));
            Assert.That(Box().Top, Is.EqualTo(40));
            Assert.That(Box().Width, Is.EqualTo(200));
            Assert.That(Box().Height, Is.EqualTo(64));
        });
    }

    /// <summary>A box knows its far edges and its middle, the three sums written out most often before.</summary>
    [Test]
    public void ABox_KnowsItsEdgesAndItsMiddle()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Box().Right, Is.EqualTo(320));
            Assert.That(Box().Bottom, Is.EqualTo(104));
            Assert.That((Box().Middle.X, Box().Middle.Y), Is.EqualTo((220.0, 72.0)));
        });
    }

    /// <summary>
    /// The sums are the ones the layouts wrote by hand, to the last digit: with numbers that have no exact form,
    /// a box answers what <c>x + w</c> and <c>x + w / 2</c> answered, so no picture changes.
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

    /// <summary>Grown, a box has the same room added on every side.</summary>
    [Test]
    public void ABoxGrown_HasRoomOnEverySide()
    {
        var grown = Box().Grown(8);

        Assert.That((grown.Left, grown.Top, grown.Width, grown.Height), Is.EqualTo((112.0, 32.0, 216.0, 80.0)));
    }

    /// <summary>Moved, a box is somewhere else and as big as it was.</summary>
    [Test]
    public void ABoxMoved_KeepsItsSize()
    {
        var moved = Box().Moved(10, -5);

        Assert.That((moved.Left, moved.Top, moved.Width, moved.Height), Is.EqualTo((130.0, 35.0, 200.0, 64.0)));
    }

    /// <summary>Two boxes joined are the smallest box that holds both, as a group's frame holds its boxes.</summary>
    [Test]
    public void TwoBoxesJoined_AreTheSmallestBoxRoundBoth()
    {
        var joined = Ours.Box(0, 0, 10, 10).With(Ours.Box(20, 5, 10, 20));
        var inside = Box().With(Ours.Box(130, 50, 10, 10));

        Assert.Multiple(() =>
        {
            Assert.That((joined.Left, joined.Top, joined.Right, joined.Bottom), Is.EqualTo((0.0, 0.0, 30.0, 25.0)));
            Assert.That(inside, Is.EqualTo(Box()), "a box that holds the other is itself");
        });
    }

    /// <summary>A box is its numbers: two made the same are the same box, and so are two points.</summary>
    [Test]
    public void TwoMadeTheSame_AreTheSame()
    {
        IBox box = Box(), again = Ours.Box(120, 40, 200, 64);
        IPoint point = Ours.Point(3, 4), there = Ours.Point(3, 4);

        Assert.Multiple(() =>
        {
            Assert.That(again, Is.EqualTo(box));
            Assert.That(box.Moved(1, 0), Is.Not.EqualTo(box));
            Assert.That(there, Is.EqualTo(point));
            Assert.That((point.X, point.Y), Is.EqualTo((3.0, 4.0)));
        });
    }

    /// <summary>
    /// What a box makes is of its own family: grown, moved or joined it is still one of ours, and so is its
    /// middle.
    /// </summary>
    [Test]
    public void WhatABoxMakes_IsOfItsOwnFamily()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Box().Grown(1), Is.InstanceOf<Box<double>>());
            Assert.That(Box().Moved(1, 1), Is.InstanceOf<Box<double>>());
            Assert.That(Box().With(Box()), Is.InstanceOf<Box<double>>());
            Assert.That(Box().Middle, Is.InstanceOf<Point<double>>());
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
