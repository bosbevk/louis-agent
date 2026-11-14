using LouisAgent.Diagrams.Components.Spec;
using LouisAgent.Diagrams.Engine.Geometry;

namespace LouisAgent.Diagrams.Engine.Components.Placed;

/// <summary>
/// A neighbour of the root where the layout put it (F134.7): in a column before or after the root, level with the
/// box it talks to.
/// </summary>
/// <param name="Neighbour">The neighbour as the JSON says it.</param>
/// <param name="Box">Where it stands and how large it is.</param>
/// <param name="NoteLines">Its note, a line at a time.</param>
public sealed record PlacedNeighbour(ComponentNeighbour Neighbour, IBox Box, IReadOnlyList<string> NoteLines);
