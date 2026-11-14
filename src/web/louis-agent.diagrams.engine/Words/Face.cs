namespace LouisAgent.Diagrams.Engine.Words;

/// <summary>
/// The words of one kind in a picture, a title or a note or a chip's, which knows how wide they are. No font is
/// measured: every character is taken to be as wide as the face was told, generously.
/// </summary>
/// <remarks>
/// Value Object (Fowler), as the cure for Primitive Obsession: a bare number stood in for a face in twenty
/// places, each multiplying a length by it. A layout now asks the face, so how words are measured is written
/// once, and a face is a name a reader can find where a number was.
/// </remarks>
/// <param name="Char">How wide a character of these words is taken to be.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record Face(double Char)
{
    /// <summary>How wide these words are.</summary>
    public double Width(string words) => words.Length * Char;
}
