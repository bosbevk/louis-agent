namespace LouisAgent.Diagrams.Components.Placed.Keys;

/// <summary>
/// The key of a component diagram, placed (F134.7): "how to read it", under the column before the root, with a row
/// for each thing the picture really uses.
/// </summary>
/// <param name="X">Its left edge.</param>
/// <param name="Y">Its top edge, where its heading stands.</param>
/// <param name="Width">How wide it is.</param>
/// <param name="Height">How tall it is, heading and rows.</param>
/// <param name="Rows">Its rows, from the top down.</param>
public sealed record PlacedKey(double X, double Y, double Width, double Height, IReadOnlyList<KeyRow> Rows)
{
    /// <summary>Its bottom edge.</summary>
    public double Bottom => Y + Height;
}
