namespace LouisAgent.Evals.Messages.Holes.Files;

/// <summary>A fact about another agent whose address doesn't parse.</summary>
public sealed record FactNeedsAnAddressHoles(string Words, string Kind);
