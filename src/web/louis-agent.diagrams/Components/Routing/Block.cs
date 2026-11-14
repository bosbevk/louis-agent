namespace LouisAgent.Diagrams.Components.Routing;

/// <summary>
/// A rectangle a state's line may not touch (F134.7): a box it doesn't join, another pin, or a box's own words.
/// </summary>
/// <param name="X">Its left edge.</param>
/// <param name="Y">Its top edge.</param>
/// <param name="Width">How wide it is.</param>
/// <param name="Height">How tall it is.</param>
internal readonly record struct Block(double X, double Y, double Width, double Height);
