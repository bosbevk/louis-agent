using LouisAgent.Diagrams.Engine.Words;

namespace LouisAgent.Diagrams.Tests.Words;

/// <summary>
/// CL65.6: a face knows how wide words are, where twenty places multiplied a length by a number of their own
/// (ADR 0123). No font is measured: a face takes every character to be as wide as it was told.
/// </summary>
public class FaceTests
{
    /// <summary>
    /// For each of the eleven numbers the engine takes for a character, a face answers what the length times
    /// that number answered where it stood, to the last digit, so no picture changes.
    /// </summary>
    [TestCase(4)]
    [TestCase(5.9)]
    [TestCase(6.1)]
    [TestCase(6.6)]
    [TestCase(6.8)]
    [TestCase(7)]
    [TestCase(7.1)]
    [TestCase(7.2)]
    [TestCase(7.6)]
    [TestCase(8)]
    [TestCase(9.0)]
    public void AFace_AnswersTheSumWrittenByHand(double character)
    {
        const string words = "The engine: what it is made of";

        Assert.That(new Face(character).Width(words), Is.EqualTo(words.Length * character));
    }

    /// <summary>No words are no width, and two faces told the same are the same face.</summary>
    [Test]
    public void NoWords_AreNoWidth_AndAFaceIsItsNumber()
    {
        Face one = new(6.6), other = new(6.6);

        Assert.Multiple(() =>
        {
            Assert.That(one.Width(""), Is.Zero);
            Assert.That(other, Is.EqualTo(one));
        });
    }
}
