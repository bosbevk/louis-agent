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

    /// <summary>
    /// A box 200 wide and 64 tall, its corner at 120 across and 40 down: numbers any family holds whole.
    /// </summary>
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

    /// <summary>
    /// Moved, a box is somewhere else and as big as it was, and the box it was moved from has not moved.
    /// </summary>
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
            Assert.That(Box().Middle, Is.TypeOf(point));
        });
    }

    /// <summary>
    /// Grown with its own room on each side, a box is what a group's frame is to its boxes: more room over the
    /// label than under.
    /// </summary>
    [Test]
    public void ABoxGrownSideBySide_HasEachSidesOwnRoom()
    {
        var frame = Box().Grown(12, 34, 12, 12);

        Assert.That((frame.Left, frame.Top, frame.Right, frame.Bottom), Is.EqualTo((108.0, 6.0, 332.0, 116.0)));
    }

    /// <summary>Put on a line, a box has its middle at that height and is where it was across.</summary>
    [Test]
    public void ABoxOnALine_HasItsMiddleThere()
    {
        var onLine = Box().OnLine(200);

        Assert.That((onLine.Left, onLine.Top, onLine.Middle.Y, onLine.Height), Is.EqualTo((120.0, 168.0, 200.0, 64.0)));
    }

    /// <summary>
    /// Turned over the diagonal, across becomes down for a box and for a point, and turned twice each is itself.
    /// </summary>
    [Test]
    public void Turned_AcrossBecomesDown()
    {
        IBox box = Box(), turned = box.Turned();
        IPoint point = geometry.Point(3, 4), over = point.Turned();

        Assert.Multiple(() =>
        {
            Assert.That((turned.Left, turned.Top, turned.Width, turned.Height), Is.EqualTo((40.0, 120.0, 64.0, 200.0)));
            Assert.That(turned.Turned(), Is.EqualTo(box));
            Assert.That((over.X, over.Y), Is.EqualTo((4.0, 3.0)));
            Assert.That(over.Turned(), Is.EqualTo(point));
            Assert.That(turned, Is.TypeOf(box.GetType()));
            Assert.That(over, Is.TypeOf(point.GetType()));
        });
    }

    /// <summary>
    /// The box round several is the smallest that holds them all, as a group's frame is drawn round its boxes,
    /// and half a box's height is how far its middle is from its top.
    /// </summary>
    [Test]
    public void TheBoxRoundSeveral_HoldsThemAll_AndABoxKnowsHalfItsHeight()
    {
        var around = geometry.Around([geometry.Box(20, 5, 10, 20), Box(), geometry.Box(0, 90, 10, 60)]);

        Assert.Multiple(() =>
        {
            Assert.That((around.Left, around.Top, around.Right, around.Bottom), Is.EqualTo((0.0, 5.0, 320.0, 150.0)));
            Assert.That(geometry.Around([Box()]), Is.EqualTo(Box()));
            Assert.That(around, Is.TypeOf(Box().GetType()));
            Assert.That(Box().HalfHeight, Is.EqualTo(32));
            Assert.That(Box().Top + Box().HalfHeight, Is.EqualTo(Box().Middle.Y));
        });
    }

    /// <summary>
    /// A box holds another that lies within it, and itself, and not one that reaches past any edge, as a box of
    /// a component diagram holds the boxes drawn inside it.
    /// </summary>
    [Test]
    public void ABox_HoldsWhatLiesWithinIt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Box().Holds(geometry.Box(130, 50, 10, 10)), Is.True);
            Assert.That(Box().Holds(Box()), Is.True);
            Assert.That(Box().Holds(geometry.Box(130, 50, 200, 10)), Is.False, "past its right edge");
            Assert.That(Box().Holds(geometry.Box(130, 30, 10, 20)), Is.False, "above its top edge");
        });
    }

    /// <summary>
    /// A stretch between two points passes through a box when it runs over its inside, whichever way it runs. One
    /// that runs along an edge does when asked with nothing, and does not when asked a pixel inside.
    /// </summary>
    [Test]
    public void AStretch_PassesThroughABox_OrOnlyAlongItsEdge()
    {
        IPoint Left(double y) => geometry.Point(100, y);
        IPoint Right(double y) => geometry.Point(400, y);

        Assert.Multiple(() =>
        {
            Assert.That(Box().CrossedBy(Left(70), Right(70), 0), Is.True, "through its middle");
            Assert.That(Box().CrossedBy(Right(70), Left(70), 1), Is.True, "the other way, a pixel inside");
            Assert.That(Box().CrossedBy(Left(20), Right(20), 0), Is.False, "above it");
            Assert.That(Box().CrossedBy(geometry.Point(330, 0), geometry.Point(330, 200), 0), Is.False, "beside it");
            Assert.That(Box().CrossedBy(Left(40.5), Right(40.5), 0), Is.True, "just under its top edge");
            Assert.That(Box().CrossedBy(Left(40.5), Right(40.5), 1), Is.False, "that is not a pixel inside");
        });
    }

    /// <summary>A place part of the way along a box, or down it, counts from its left and its top.</summary>
    [Test]
    public void APlaceAlongABox_CountsFromItsLeftAndItsTop()
    {
        Assert.Multiple(() =>
        {
            Assert.That((Box().Across(0), Box().Across(0.5), Box().Across(1)), Is.EqualTo((120.0, 220.0, 320.0)));
            Assert.That((Box().Down(0), Box().Down(0.25), Box().Down(1)), Is.EqualTo((40.0, 56.0, 104.0)));
        });
    }

    /// <summary>
    /// A line that runs only across and down goes as far as the two distances added, and stops a little short
    /// of a corner on the side it came from, which is where a rounded turn starts.
    /// </summary>
    [Test]
    public void APoint_KnowsHowFarAnotherIs_AndTheWayTowardsIt()
    {
        IPoint corner = geometry.Point(3, 4), level = geometry.Point(13, 4), above = geometry.Point(3, 0);
        IPoint short1 = corner.Toward(level, 2), short2 = corner.Toward(above, 1.5);

        Assert.Multiple(() =>
        {
            Assert.That(corner.StepsTo(geometry.Point(13, 1)), Is.EqualTo(13));
            Assert.That(corner.StepsTo(corner), Is.Zero);
            Assert.That((short1.X, short1.Y), Is.EqualTo((5.0, 4.0)));
            Assert.That((short2.X, short2.Y), Is.EqualTo((3.0, 2.5)));
            Assert.That(short1, Is.TypeOf(corner.GetType()));
        });
    }

    /// <summary>Moved, a point is somewhere else, and the point it was moved from has not moved.</summary>
    [Test]
    public void APointMoved_IsSomewhereElse()
    {
        IPoint point = geometry.Point(3, 4), moved = point.Moved(10, -1);

        Assert.Multiple(() =>
        {
            Assert.That((moved.X, moved.Y), Is.EqualTo((13.0, 3.0)));
            Assert.That((point.X, point.Y), Is.EqualTo((3.0, 4.0)));
            Assert.That(moved, Is.TypeOf(point.GetType()));
        });
    }

    /// <summary>
    /// Asked about a box of another family, a box still answers: the other is read through the seam.
    /// </summary>
    [Test]
    public void AskedAboutABoxOfAnotherFamily_ABoxStillAnswers()
    {
        var decimals = new GenericGeometry<decimal>();

        Assert.Multiple(() =>
        {
            Assert.That(Box().Holds(decimals.Box(130, 50, 10, 10)), Is.True);
            Assert.That(Box().Holds(decimals.Box(300, 100, 50, 50)), Is.False);
        });
    }
}
