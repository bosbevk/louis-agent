using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Geometry.DotNet;
using LouisAgent.Diagrams.Engine.Geometry.Generic;

namespace LouisAgent.Diagrams.Tests.Geometry;

/// <summary>
/// CL65.6: what a box and a point know is the same whichever family made them, so the same tests run over
/// each: ours as the engine uses it, ours in single precision, and the one on Microsoft's types. They ask
/// through the seam, as a layout does.
/// </summary>
[TestFixtureSource(nameof(Families))]
public class GeometryFamiliesTests(IGeometry geometry)
{
    /// <summary>The families, each by a name a failing test is read by.</summary>
    private static IEnumerable<TestFixtureData> Families()
    {
        yield return new TestFixtureData(new GenericGeometry<double>()).SetArgDisplayNames("ours");
        yield return new TestFixtureData(new GenericGeometry<float>()).SetArgDisplayNames("ours in float");
        yield return new TestFixtureData(new DotNetGeometry()).SetArgDisplayNames("Microsoft's");
    }

    /// <summary>A box 200 wide and 64 tall, its corner at 120 across and 40 down: numbers any family holds whole.</summary>
    private IBox Box() => geometry.Box(120, 40, 200, 64);

    /// <summary>A box keeps the place and the size it was made with.</summary>
    [Test]
    public void ABox_KeepsWhatItWasMadeWith()
    {
        Assert.That((Box().Left, Box().Top, Box().Width, Box().Height), Is.EqualTo((120.0, 40.0, 200.0, 64.0)));
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

    /// <summary>Grown, a box has the same room added on every side.</summary>
    [Test]
    public void ABoxGrown_HasRoomOnEverySide()
    {
        var grown = Box().Grown(8);

        Assert.That((grown.Left, grown.Top, grown.Width, grown.Height), Is.EqualTo((112.0, 32.0, 216.0, 80.0)));
    }

    /// <summary>Moved, a box is somewhere else and as big as it was, and the box it was moved from has not moved.</summary>
    [Test]
    public void ABoxMoved_KeepsItsSize_AndLeavesTheFirstWhereItWas()
    {
        var box = Box();
        var moved = box.Moved(10, -5);

        Assert.Multiple(() =>
        {
            Assert.That((moved.Left, moved.Top, moved.Width, moved.Height), Is.EqualTo((130.0, 35.0, 200.0, 64.0)));
            Assert.That((box.Left, box.Top), Is.EqualTo((120.0, 40.0)));
        });
    }

    /// <summary>Two boxes joined are the smallest box that holds both, as a group's frame holds its boxes.</summary>
    [Test]
    public void TwoBoxesJoined_AreTheSmallestBoxRoundBoth()
    {
        var joined = geometry.Box(0, 0, 10, 10).With(geometry.Box(20, 5, 10, 20));
        IBox box = Box(), inside = box.With(geometry.Box(130, 50, 10, 10));

        Assert.Multiple(() =>
        {
            Assert.That((joined.Left, joined.Top, joined.Right, joined.Bottom), Is.EqualTo((0.0, 0.0, 30.0, 25.0)));
            Assert.That(inside, Is.EqualTo(box), "a box that holds the other is itself");
        });
    }

    /// <summary>A box is its numbers: two made the same are the same box, and so are two points.</summary>
    [Test]
    public void TwoMadeTheSame_AreTheSame()
    {
        IBox box = Box(), again = geometry.Box(120, 40, 200, 64);
        IPoint point = geometry.Point(3, 4), there = geometry.Point(3, 4);

        Assert.Multiple(() =>
        {
            Assert.That(again, Is.EqualTo(box));
            Assert.That(box.Moved(1, 0), Is.Not.EqualTo(box));
            Assert.That(there, Is.EqualTo(point));
            Assert.That((point.X, point.Y), Is.EqualTo((3.0, 4.0)));
        });
    }

    /// <summary>
    /// What a box makes is of its own family: grown, moved or joined it is the same type of box, and its middle
    /// the same type of point as its geometry makes.
    /// </summary>
    [Test]
    public void WhatABoxMakes_IsOfItsOwnFamily()
    {
        Type box = Box().GetType(), point = geometry.Point(0, 0).GetType();

        Assert.Multiple(() =>
        {
            Assert.That(Box().Grown(1), Is.TypeOf(box));
            Assert.That(Box().Moved(1, 1), Is.TypeOf(box));
            Assert.That(Box().With(Box()), Is.TypeOf(box));
            Assert.That(Box().Middle, Is.TypeOf(point));
        });
    }

    /// <summary>
    /// Joined with a box of another family, a box still answers in its own: the other is read through the seam.
    /// </summary>
    [Test]
    public void JoinedWithABoxOfAnotherFamily_ABoxStaysOfItsOwn()
    {
        var other = new GenericGeometry<decimal>().Box(300, 100, 50, 50);
        var joined = Box().With(other);

        Assert.Multiple(() =>
        {
            Assert.That(joined, Is.TypeOf(Box().GetType()));
            Assert.That((joined.Left, joined.Top, joined.Right, joined.Bottom), Is.EqualTo((120.0, 40.0, 350.0, 150.0)));
        });
    }
}
