using LouisAgent.Diagrams.Engine.Flow.Arrows;

namespace LouisAgent.Diagrams.Tests.Flow.Arrows;

/// <summary>
/// CL65.6: an arrow's way is a type that knows how it is turned, moved and joined, where it was a text four
/// classes read back number by number (ADR 0123). It writes the text the router wrote, so no picture changes.
/// </summary>
public class ArrowPathTests
{
    /// <summary>A way out of a box, round one corner and down: from, a line, a rounded turn, a line.</summary>
    private static ArrowPath Way() => ArrowPath.From(10, 20).To(40, 20).Round(50, 20, 50, 30).To(50, 80);

    /// <summary>
    /// Its text is a path's: a letter and its numbers, and a rounded turn as a curve through its corner.
    /// </summary>
    [Test]
    public void ItsText_IsAPaths_ATurnACurveThroughItsCorner() =>
        Assert.That(Way().Text, Is.EqualTo("M10 20L40 20Q50 20 50 30L50 80"));

    /// <summary>
    /// Its numbers are the layout's own, exact, and are rounded to one decimal only where its text is written. So
    /// a way that is moved is moved from where it stood, not from where it was written.
    /// </summary>
    [Test]
    public void ItsNumbers_AreExact_AndRoundedOnlyWhereItsTextIsWritten()
    {
        var way = ArrowPath.From(10.26, 0.04).To(40.24, 0.04);

        Assert.Multiple(() =>
        {
            Assert.That((way.Start.X, way.Start.Y), Is.EqualTo((10.26, 0.04)));
            Assert.That(way.Text, Is.EqualTo("M10.3 0L40.2 0"));
            Assert.That(way.Moved(0.05).Text, Is.EqualTo("M10.3 0L40.3 0"),
                "10.31 and 40.29, written");
        });
    }

    /// <summary>Turned over the diagonal, every place has its across and its down swapped, a corner's too.</summary>
    [Test]
    public void Turned_EveryPlaceSwapsAcrossAndDown()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Way().Turned().Text, Is.EqualTo("M20 10L20 40Q20 50 30 50L80 50"));
            Assert.That(Way().Turned().Turned(), Is.EqualTo(Way()));
        });
    }

    /// <summary>Moved, every place is further across and as far down as it was.</summary>
    [Test]
    public void Moved_EveryPlaceIsFurtherAcross() =>
        Assert.That(Way().Moved(5.5).Text, Is.EqualTo("M15.5 20L45.5 20Q55.5 20 55.5 30L55.5 80"));

    /// <summary>
    /// Joined, two ways are one: the second's start is a place the line runs straight through, as a long arrow's
    /// pieces are joined across each waypoint.
    /// </summary>
    [Test]
    public void TwoWaysJoined_AreOne_StraightThroughWhereTheyMeet()
    {
        var joined = ArrowPath.From(0, 5).To(30, 5).Then(ArrowPath.From(30, 5).To(60, 5));

        Assert.Multiple(() =>
        {
            Assert.That(joined.Text, Is.EqualTo("M0 5L30 5L30 5L60 5"));
            Assert.That(joined.Pieces.Select(piece => piece.Turn), Is.EqualTo(new[]
            {
                ArrowTurn.Start, ArrowTurn.Line, ArrowTurn.Line, ArrowTurn.Line
            }));
        });
    }

    /// <summary>
    /// Its places come in the order the line runs, a turn's corner among them, which is what a check of whether
    /// something lies on the line walks along.
    /// </summary>
    [Test]
    public void ItsPlaces_ComeInOrder_ACornerAmongThem()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Way().Pieces.Select(piece => (piece.X, piece.Y)), Is.EqualTo(new[]
            {
                (10.0, 20.0), (40.0, 20.0), (50.0, 20.0), (50.0, 30.0), (50.0, 80.0)
            }));
            Assert.That((Way().Start.X, Way().Start.Y), Is.EqualTo((10.0, 20.0)));
        });
    }

    /// <summary>
    /// Its stretches run from each place to the next, and each knows how far it reaches, whichever way it runs:
    /// what a check of whether a word lies on the line asks.
    /// </summary>
    [Test]
    public void ItsStretches_RunFromPlaceToPlace_AndKnowTheirReach()
    {
        var stretches = ArrowPath.From(50, 80).To(50, 30).To(10, 30).Stretches.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Way().Stretches.Count(), Is.EqualTo(4), "one fewer than its places");
            Assert.That((stretches[0].Left, stretches[0].Right, stretches[0].Top, stretches[0].Bottom),
                Is.EqualTo((50.0, 50.0, 30.0, 80.0)), "upwards, and still top before bottom");
            Assert.That((stretches[1].Left, stretches[1].Right), Is.EqualTo((10.0, 50.0)), "leftwards");
        });
    }

    /// <summary>A way is its places: two built the same are the same way, and one moved is another.</summary>
    [Test]
    public void TwoBuiltTheSame_AreTheSame()
    {
        ArrowPath one = Way(), other = Way();

        Assert.Multiple(() =>
        {
            Assert.That(other, Is.EqualTo(one));
            Assert.That(other.GetHashCode(), Is.EqualTo(one.GetHashCode()));
            Assert.That(one.Moved(1), Is.Not.EqualTo(one));
        });
    }
}
