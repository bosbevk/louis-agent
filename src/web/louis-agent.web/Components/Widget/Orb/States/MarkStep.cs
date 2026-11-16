namespace LouisAgent.Web.Components.Widget.Orb.States;

/// <summary>
/// One state the landing's mark plays: the state itself, its name for a visitor, and what the mark does in it.
/// </summary>
/// <param name="State">The orb's state.</param>
/// <param name="Name">The word on its button.</param>
/// <param name="Line">One sentence on what the mark does in it, and when.</param>
public sealed record MarkStep(OrbState State, string Name, string Line);
