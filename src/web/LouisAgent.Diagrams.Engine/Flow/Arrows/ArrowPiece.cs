namespace LouisAgent.Diagrams.Engine.Flow.Arrows;

/// <summary>
/// One place on an arrow's way: where it is, and what it is there, the start, the end of a straight piece, or
/// part of a rounded turn.
/// </summary>
/// <param name="Turn">What the place is on the way.</param>
/// <param name="X">How far across it is, as the picture writes it.</param>
/// <param name="Y">How far down it is, as the picture writes it.</param>
public sealed record ArrowPiece(ArrowTurn Turn, double X, double Y);
