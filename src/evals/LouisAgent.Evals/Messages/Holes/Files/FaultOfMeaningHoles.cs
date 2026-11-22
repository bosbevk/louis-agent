namespace LouisAgent.Evals.Messages.Holes.Files;

/// <summary>A fault the reader's own checks found: its place and its reason.</summary>
public sealed record FaultOfMeaningHoles(string Place, string Reason);
