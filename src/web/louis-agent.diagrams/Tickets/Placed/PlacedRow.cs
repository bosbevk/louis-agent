using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Tickets.Placed;

/// <summary>
/// One row's place: its icon box at the left, the row's top, and its height with every line of its value.
/// </summary>
public sealed record PlacedRow(DiagramIcon Icon, double X, double Y, double H);
