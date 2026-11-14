using LouisAgent.Diagrams.Engine.Svg.Paths;

namespace LouisAgent.Diagrams.Tests.Svg.Paths;

/// <summary>
/// CL65.6: a path is written by a writer that knows SVG's letters, where twenty places built its text by hand
/// (ADR 0123). It writes the text those places wrote, so no picture changes.
/// </summary>
public class PathWriterTests
{
    /// <summary>Each piece of a path is its letter and its numbers, one after the other with nothing between.</summary>
    [Test]
    public void EachPiece_IsItsLetterAndItsNumbers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new PathWriter().From(10, 20).Text, Is.EqualTo("M10 20"));
            Assert.That(new PathWriter().From(10, 20).To(40, 25).Text, Is.EqualTo("M10 20L40 25"));
            Assert.That(new PathWriter().From(10, 20).Across(40).Down(60).Text, Is.EqualTo("M10 20H40V60"));
            Assert.That(new PathWriter().From(0, 0).Curve(40, 0, 40, 9).Text, Is.EqualTo("M0 0Q40 0 40 9"));
            Assert.That(new PathWriter().From(0, 0).Arc(8, 8, false, true, 8, 8).Text,
                Is.EqualTo("M0 0A8 8 0 0 1 8 8"));
            Assert.That(new PathWriter().From(0, 0).ArcBy(12, 6, true, false, 24, 0).Text,
                Is.EqualTo("M0 0a12 6 0 1 0 24 0"));
            Assert.That(new PathWriter().From(0, 0).To(4, 4).To(8, 0).Close().Text, Is.EqualTo("M0 0L4 4L8 0Z"));
        });
    }

    /// <summary>
    /// A number is written as an SVG's are: a point whatever the machine's language, and one decimal at most.
    /// </summary>
    [Test]
    public void ANumber_HasOneDecimalAtMost()
    {
        Assert.That(new PathWriter().From(10.26, 0.04).Across(1234.5).Text, Is.EqualTo("M10.3 0H1234.5"));
    }

    /// <summary>
    /// A flow's arrow is written with room round every letter, as its router wrote it, since other code reads
    /// that text back.
    /// </summary>
    [Test]
    public void ASpacedPath_HasRoomRoundEveryLetter()
    {
        var path = new PathWriter(spaced: true).From(10, 20).To(40, 20).Curve(50, 20, 50, 30).To(50, 80);

        Assert.That(path.Text, Is.EqualTo("M 10 20 L 40 20 Q 50 20 50 30 L 50 80"));
    }

    /// <summary>
    /// An oval round a point is two half turns from its left and back, the way the soft light behind a card is
    /// drawn: the text two drawings wrote by hand.
    /// </summary>
    [Test]
    public void AnOval_IsTwoHalfTurnsFromItsLeft() =>
        Assert.That(new PathWriter().Oval(50, 60, 20, 10).Text,
            Is.EqualTo("M30 60a20 10 0 1 0 40 0a20 10 0 1 0 -40 0Z"));

    /// <summary>A level line is from one place across to another at one height, whichever way it runs.</summary>
    [Test]
    public void ALevelLine_RunsAcrossAtOneHeight()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PathWriter.Level(120, 150, 348), Is.EqualTo("M120 348H150"));
            Assert.That(PathWriter.Level(150, 120.26, 348), Is.EqualTo("M150 348H120.3"));
        });
    }

    /// <summary>A path with nothing in it is no text.</summary>
    [Test]
    public void AnEmptyPath_IsNoText()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new PathWriter().Text, Is.Empty);
            Assert.That(new PathWriter(spaced: true).Text, Is.Empty);
        });
    }
}
