using LouisAgent.Diagrams.Engine.Flow.Arrows;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;

namespace LouisAgent.Diagrams.Engine.Flow.Placed;

/// <summary>
/// An arrow with its way: straight pieces that bend at right angles, about how long it is (the view needs that to
/// make it draw itself), the point where a word on it sits, on a flat piece clear of the boxes, and how far right
/// it reaches, so the drawing can make room for a loop past the last column.
/// </summary>
/// <param name="Edge">The arrow as the flow's file has it.</param>
/// <param name="Way">The way it runs, as an SVG draws it.</param>
/// <param name="Length">About how long its way is.</param>
/// <param name="Word">The point its word sits at.</param>
/// <param name="Right">How far right it reaches.</param>
public sealed record PlacedEdge(GraphEdge Edge, ArrowPath Way, double Length, IPoint Word, double Right);
