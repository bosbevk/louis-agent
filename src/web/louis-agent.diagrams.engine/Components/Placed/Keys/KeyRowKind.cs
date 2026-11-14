namespace LouisAgent.Diagrams.Engine.Components.Placed.Keys;

/// <summary>
/// What one row of a component diagram's key explains (F134.7): a tone, a pin handed down, a pin said back up, a
/// line that only reads, the one thing a box holds, or a sentence about the picture.
/// </summary>
public enum KeyRowKind
{
    Tone,
    HandedDown,
    SaidBackUp,
    Reads,
    Holds,
    Sentence
}
