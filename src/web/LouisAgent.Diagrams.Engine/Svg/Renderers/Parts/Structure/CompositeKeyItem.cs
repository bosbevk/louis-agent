using LouisAgent.Diagrams.Engine.Messages;

namespace LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Structure;

/// <summary>One item of a composite's key: what it explains, how wide its sample is, and where it stands.</summary>
/// <param name="Means">What the item explains, as the words beside its sample.</param>
/// <param name="Sample">How wide its sample is.</param>
/// <param name="X">How far across the key it stands, once placed.</param>
/// <param name="Y">How far down the key it stands, once placed.</param>
internal sealed record CompositeKeyItem(DrawingMessage Means, double Sample, double X = 0, double Y = 0);
