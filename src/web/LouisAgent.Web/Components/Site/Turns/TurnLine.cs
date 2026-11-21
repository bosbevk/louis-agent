namespace LouisAgent.Web.Components.Site.Turns;

/// <summary>One line of a playing turn: what it is, its words, and how many seconds in it appears.</summary>
/// <param name="Kind">What the line is.</param>
/// <param name="Text">Its words; for a tool, the tool's name.</param>
/// <param name="At">How many seconds into the turn it appears.</param>
/// <param name="Argument">For a tool, what it was called with; none otherwise.</param>
public sealed record TurnLine(TurnLineKind Kind, string Text, double At, string? Argument = null);
