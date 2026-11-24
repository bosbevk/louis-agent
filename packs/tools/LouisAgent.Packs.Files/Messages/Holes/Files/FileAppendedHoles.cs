namespace LouisAgent.Packs.Files.Messages.Holes.Files;

/// <summary>The file added to, and how many characters.</summary>
public sealed record FileAppendedHoles(string Path, int Length);
