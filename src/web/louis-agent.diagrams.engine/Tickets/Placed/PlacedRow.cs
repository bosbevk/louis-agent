using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Engine.Tickets.Placed;

/// <summary>
/// One row's place: the row itself, as tall as every line of its value needs, and the icon's box at its top left.
/// </summary>
/// <param name="Icon">The row's icon.</param>
/// <param name="Box">The row: from its top, as wide as the card inside its edges.</param>
/// <param name="IconBox">The box its icon stands in.</param>
public sealed record PlacedRow(DiagramIcon Icon, IBox Box, IBox IconBox);
