using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Tickets.Placed;

/// <summary>
/// One line of a ticket's words: where it starts, on its baseline, what it says and which kind of words it is,
/// which says the style that draws it and how wide it runs.
/// </summary>
/// <param name="At">Where the line starts, on its baseline.</param>
/// <param name="Text">What it says.</param>
/// <param name="Kind">Which of the ticket's words it is.</param>
public sealed record PlacedWords(IPoint At, string Text, TicketWords Kind)
{
    /// <summary>How wide the line runs in its face.</summary>
    public double Width => Kind.Face.Width(Text);

    /// <summary>Where the line ends, on its baseline.</summary>
    public IPoint End => At.Moved(Width, 0);
}
