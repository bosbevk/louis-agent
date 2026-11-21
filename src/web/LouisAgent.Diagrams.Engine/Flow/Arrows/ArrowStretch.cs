namespace LouisAgent.Diagrams.Engine.Flow.Arrows;

/// <summary>
/// One stretch of an arrow's way, from a place to the next, which knows how far it reaches each way. A rounded
/// turn counts as the two stretches to and from its corner, which is the ground the line can cover.
/// </summary>
/// <param name="From">Where the stretch starts.</param>
/// <param name="To">Where it ends.</param>
public sealed record ArrowStretch(ArrowPiece From, ArrowPiece To)
{
    /// <summary>The furthest left it reaches.</summary>
    public double Left => Math.Min(From.X, To.X);

    /// <summary>The furthest right it reaches.</summary>
    public double Right => Math.Max(From.X, To.X);

    /// <summary>The highest it reaches.</summary>
    public double Top => Math.Min(From.Y, To.Y);

    /// <summary>The lowest it reaches.</summary>
    public double Bottom => Math.Max(From.Y, To.Y);
}
