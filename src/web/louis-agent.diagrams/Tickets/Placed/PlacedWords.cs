namespace LouisAgent.Diagrams.Tickets.Placed;

/// <summary>
/// One line of a ticket's words: where it starts, its baseline, how wide it runs in its font, and the style that
/// draws it (<c>ticket-title</c>, <c>ticket-value</c>…).
/// </summary>
public sealed record PlacedWords(double X, double Y, double Width, string Text, string Cls);
