using System.Reflection;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams.Tests.Messages;

/// <summary>
/// CL65.1: the texts are two enums. What the reader refuses with says what a diagram is, and stays with it. The
/// words a drawing writes, a key's heading, what a tone means, a type's name, belong to what builds a diagram.
/// </summary>
public class DrawingMessageTests
{
    /// <summary>A member's words, as its <c>[Description]</c> has them. Nothing when it has none.</summary>
    private static string? Words<TMessage>(string member) =>
        typeof(TMessage).GetField(member)!.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()
            ?.Description;

    /// <summary>
    /// The refusals hold no word a drawing writes, and no name is in both: the reader's enum is the 25 things it
    /// refuses with, and the key's heading and the missing renderer are the drawing's.
    /// </summary>
    [Test]
    public void TheRefusalsAndTheDrawingsWords_AreTwoEnums()
    {
        string[] refusals = Enum.GetNames<DiagramMessage>(), words = Enum.GetNames<DrawingMessage>();

        Assert.Multiple(() =>
        {
            Assert.That(refusals, Has.Length.EqualTo(25));
            Assert.That(refusals.Intersect(words), Is.Empty);
            Assert.That(words, Does.Contain("KeyHeading").And.Contain("NoRenderer").And.Contain("LegendGreen"));
            Assert.That(words, Has.Length.EqualTo(43));
        });
    }

    /// <summary>Every word a drawing writes has its text.</summary>
    [Test]
    public void EveryWord_HasItsText() =>
        Assert.That(
            Enum.GetNames<DrawingMessage>().Where(member => string.IsNullOrWhiteSpace(Words<DrawingMessage>(member))),
            Is.Empty);

    /// <summary>
    /// One formatter fills either enum: a word as it is written, and a hole from a record's property.
    /// </summary>
    [Test]
    public void OneFormatter_FillsEitherEnum() => Assert.Multiple(() =>
    {
        Assert.That(DiagramText.Format(DrawingMessage.KeyHeading), Is.EqualTo("How to read it"));
        Assert.That(DiagramText.Format(DiagramMessage.MissingTitle), Is.EqualTo(Words<DiagramMessage>("MissingTitle")));
    });
}
