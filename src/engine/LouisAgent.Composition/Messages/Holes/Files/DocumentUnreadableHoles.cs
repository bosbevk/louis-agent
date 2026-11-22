namespace LouisAgent.Composition.Messages.Holes.Files;

/// <summary>The document that couldn't be read, and why.</summary>
public sealed record DocumentUnreadableHoles(string Path, string Reason);
