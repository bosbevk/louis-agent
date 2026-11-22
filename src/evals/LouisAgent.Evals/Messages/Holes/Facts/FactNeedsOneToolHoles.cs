namespace LouisAgent.Evals.Messages.Holes.Facts;

/// <summary>A tool fact written with no tool, or with more than one word.</summary>
public sealed record FactNeedsOneToolHoles(string Words, string Kind);
