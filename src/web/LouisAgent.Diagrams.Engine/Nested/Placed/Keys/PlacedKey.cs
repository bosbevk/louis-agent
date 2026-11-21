using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Nested.Placed.Keys;

/// <summary>
/// The key of a nested picture, placed (F134.7): "how to read it", under the column before the root, with a row
/// for each thing the picture really uses.
/// </summary>
/// <param name="Box">Where it stands and how large it is, heading and rows. Its heading stands at its top.</param>
/// <param name="Rows">Its rows, from the top down.</param>
public sealed record PlacedKey(IBox Box, IReadOnlyList<KeyRow> Rows);
