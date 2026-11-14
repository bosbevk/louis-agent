using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// A pin on a box's top edge (F134.7): a parameter handed down to the box, or a callback it says back up.
/// </summary>
/// <param name="On">The id of the box it stands on.</param>
/// <param name="Name">The pin's name, as the code has it.</param>
/// <param name="Up">Said back up, which is drawn hollow with an arrow up. Otherwise handed down.</param>
/// <param name="Box">Where it stands and how large it is: half above the edge it stands on, and half under.</param>
public sealed record PlacedPin(string On, string Name, bool Up, IBox Box)
{
    /// <summary>The edge it stands on: its middle, top to bottom.</summary>
    public double Edge => Box.Middle.Y;
}
