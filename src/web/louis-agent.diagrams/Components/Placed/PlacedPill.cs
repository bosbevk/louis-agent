namespace LouisAgent.Diagrams.Components.Placed;

/// <summary>
/// The pill that says the one thing a component holds (F134.7), where the layout put it: under the box's name and
/// note, in the middle, above the boxes it holds.
/// </summary>
/// <param name="Says">What the component holds, in a few words.</param>
/// <param name="X">Its left edge.</param>
/// <param name="Y">Its top edge.</param>
/// <param name="Width">How wide it is.</param>
/// <param name="Height">How tall it is.</param>
public sealed record PlacedPill(string Says, double X, double Y, double Width, double Height);
