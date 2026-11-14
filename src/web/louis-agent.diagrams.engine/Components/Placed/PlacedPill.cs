using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// The pill that says the one thing a component holds (F134.7), where the layout put it: under the box's name and
/// note, in the middle, above the boxes it holds.
/// </summary>
/// <param name="Says">What the component holds, in a few words.</param>
/// <param name="Box">Where it stands and how large it is.</param>
public sealed record PlacedPill(string Says, IBox Box);
