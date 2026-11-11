using LouisAgent.Diagrams.Cards.Links;

namespace LouisAgent.Diagrams.Cards.Placed;

/// <summary>
/// A link where the layout drew it: the path from one card's bottom middle to the next row's top middle.
/// </summary>
public sealed record PlacedLink(CardLink Link, string Path);
