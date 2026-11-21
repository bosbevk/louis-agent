namespace LouisAgent.Diagrams.Engine.Flow.Arrows;

/// <summary>
/// What a place on an arrow's way is: where it starts, the end of a straight piece, or part of a corner.
/// </summary>
public enum ArrowTurn
{
    /// <summary>Where the arrow starts.</summary>
    Start,

    /// <summary>The end of a straight piece.</summary>
    Line,

    /// <summary>The corner a rounded turn bends through, which the line itself never touches.</summary>
    Corner,

    /// <summary>Where a rounded turn ends, past its corner.</summary>
    After
}
