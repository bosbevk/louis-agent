namespace LouisAgent.Evals.Messages.Holes.Catalog;

/// <summary>The file, and the reader's own reason for refusing it.</summary>
public sealed record FileRefusedHoles(string Path, string Reason);
