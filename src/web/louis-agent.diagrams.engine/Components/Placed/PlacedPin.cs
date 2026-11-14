namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// A pin on a box's top edge (F134.7): a parameter handed down to the box, or a callback it says back up.
/// </summary>
/// <param name="Box">The id of the box it stands on.</param>
/// <param name="Name">The pin's name, as the code has it.</param>
/// <param name="Up">Said back up, which is drawn hollow with an arrow up. Otherwise handed down.</param>
/// <param name="X">Its left edge.</param>
/// <param name="Y">The edge it stands on: its middle, top to bottom.</param>
/// <param name="Width">How wide it is.</param>
public sealed record PlacedPin(string Box, string Name, bool Up, double X, double Y, double Width)
{
    /// <summary>Its middle, left to right, where a line to it ends.</summary>
    public double Middle => X + Width / 2;
}
