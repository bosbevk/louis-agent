namespace LouisAgent.Evals.Messages.Holes.Files;

/// <summary>A fault the serializer found: its place, its line from one, and its reason.</summary>
public sealed record FaultOfShapeHoles(string Place, long Line, string Reason);
