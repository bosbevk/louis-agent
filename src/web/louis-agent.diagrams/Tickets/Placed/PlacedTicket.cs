namespace LouisAgent.Diagrams.Tickets.Placed;

/// <summary>
/// A ticket laid out: its size, the band's height (where the torn edge runs), the status pill, each row's icon box,
/// the note's strip, and every line of words where it stands, so the renderer draws and a test measures the same.
/// </summary>
public sealed record PlacedTicket(
    double Width,
    double Height,
    double BandHeight,
    PlacedPill? Pill,
    IReadOnlyList<PlacedRow> Rows,
    double? NoteY,
    IReadOnlyList<PlacedWords> Words);
