namespace LouisAgent.Diagrams.Engine.Nested.Placed.Lines;

/// <summary>
/// What a line in a nested picture carries (F134.7), which decides how it is drawn: something handed to a box,
/// a service a box only reads, a call out and its answer back, or a state going down to a pin and coming back up.
/// </summary>
public enum LineKind
{
    Gives,
    Reads,
    Calls,
    Back,
    Down,
    Up
}
