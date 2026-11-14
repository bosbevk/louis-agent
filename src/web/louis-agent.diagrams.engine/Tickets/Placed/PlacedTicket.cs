using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Tickets.Placed;

/// <summary>
/// A ticket laid out: the card, its head with the kicker, title and subtitle, the status pill, each row with its
/// icon's box, the well its note stands in, and every line of words where it stands, so the renderer draws and a
/// test measures the same.
/// </summary>
/// <param name="Card">The whole card, from the picture's corner.</param>
/// <param name="Head">The card's head: from its top down to under its subtitle, where the rows start.</param>
/// <param name="Pill">The status pill at the head's top right. None when the ticket has no status.</param>
/// <param name="Rows">Its rows, from the top down.</param>
/// <param name="Note">The well the note stands in, at the card's foot. None when the ticket has no note.</param>
/// <param name="Words">Every line of words, where it stands.</param>
public sealed record PlacedTicket(
    IBox Card,
    IBox Head,
    PlacedPill? Pill,
    IReadOnlyList<PlacedRow> Rows,
    IBox? Note,
    IReadOnlyList<PlacedWords> Words);
