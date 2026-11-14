using LouisAgent.Diagrams.Engine.Words;

namespace LouisAgent.Diagrams.Engine.Tickets.Placed;

/// <summary>
/// One kind of words on a ticket: its kicker, its title, a row's label. It knows the style that draws them and
/// the face that says how wide they run.
/// </summary>
/// <remarks>
/// Value Object (Fowler), as the cure for Primitive Obsession: a kind of words was its style's name, a bare
/// text, and the layout looked a character's width up by that text, with a width for any name it didn't know.
/// A kind is one of the seven here now, and each brings its own face.
/// </remarks>
/// <param name="Cls">The style that draws them, such as <c>ticket-title</c>.</param>
/// <param name="Face">How wide these words run.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record TicketWords(string Cls, Face Face)
{
    /// <summary>The small line over the title, the title, and the line under it.</summary>
    public static readonly TicketWords Kicker = new("ticket-kicker", new Face(7.4)),
        Title = new("ticket-title", new Face(13.2)),
        Subtitle = new("ticket-subtitle", new Face(6.6));

    /// <summary>A row's label and its value, the note at the foot, and the status in its pill.</summary>
    public static readonly TicketWords Label = new("ticket-label", new Face(6.3)),
        Value = new("ticket-value", new Face(7.8)),
        Note = new("ticket-note", new Face(6.2)),
        Status = new("ticket-pill-text", new Face(6.6));
}
