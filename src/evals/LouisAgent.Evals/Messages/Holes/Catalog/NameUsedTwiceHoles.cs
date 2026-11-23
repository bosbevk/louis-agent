namespace LouisAgent.Evals.Messages.Holes.Catalog;

/// <summary>The eval's name, and the two files that both carry it.</summary>
public sealed record NameUsedTwiceHoles(string Name, string First, string Second);
